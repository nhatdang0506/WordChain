
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WordChain.Common.Messaging;

namespace WordChain.Client.WinForms;

public class NetworkClient : IDisposable
{
    private Socket? _socket;
    private Thread? _recvThread;
    private volatile bool _running;
    private readonly byte[] _buf = new byte[8192];
    private readonly StringBuilder _sb = new();

    public event Action<string>? LineReceived;
    public event Action<string>? Info;
    public event Action<string>? Error;
    public bool Connected => _socket != null;

    //kết nối tới server
    public void Connect(string host, int port)
    {
        if (_socket != null) throw new InvalidOperationException("Đã kết nối.");
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.NoDelay = true;
        _socket.Connect(host, port);

        _running = true;
        _recvThread = new Thread(RecvLoop) { IsBackground = true };
        _recvThread.Start();

        Info?.Invoke($"⚪ Đã kết nối {host}:{port}.");
    }
    // Ngắt kết nối với server:
    public void Disconnect()
    {
        _running = false;
        if (_socket != null)
        {
            try { _socket.Shutdown(SocketShutdown.Both); } catch { }
            try { _socket.Close(); } catch { }
            _socket = null;
        }
        try { _recvThread?.Join(100); } catch { }
        Info?.Invoke("⚪ Đã ngắt kết nối.");
    }
    // Gửi một message C# lên server: Serialize thành JSON + '\n'.
    public void Send<T>(T message)
    {
        if (_socket == null) throw new InvalidOperationException("Chưa kết nối.");
        var json = MessageSerializer.Serialize(message) + "\n";
        var data = Encoding.UTF8.GetBytes(json);
        int sent = 0;
        while (sent < data.Length)
            sent += _socket.Send(data, sent, data.Length - sent, SocketFlags.None);
    }
    // Vòng lặp nhận dữ liệu từ server:
    private void RecvLoop()
    {
        try
        {
            while (_running && _socket != null)
            {
                for (int i = 0; i < _sb.Length; i++)
                {
                    if (_sb[i] == '\n')
                    {
                        var str = _sb.ToString(0, i).TrimEnd('\r');
                        _sb.Remove(0, i + 1);
                        LineReceived?.Invoke(str);
                        goto next;
                    }
                }
                int read;
                try { read = _socket.Receive(_buf); } catch { break; }
                if (read <= 0) break;
                _sb.Append(Encoding.UTF8.GetString(_buf, 0, read));
                next: ;
            }
        }
        finally
        {
            Disconnect();
        }
    }

    public void Dispose() => Disconnect();
}
