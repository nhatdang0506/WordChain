using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using WordChain.Common;
using WordChain.Common.Messaging;

namespace WordChain.Server;

public class GameServer
{
    public const int InitialLives = 3;
    public const int TurnLimitSeconds = 15;

    private readonly Socket _listener;
    private volatile bool _running;
    private readonly List<ClientSession> _clients = new();
    private readonly object _lock = new();

    private string? _lastWord;
    private readonly HashSet<string> _used = new();
    private int _turnIndex = -1;
    private DateTime _deadlineUtc = DateTime.UtcNow;
    private bool _gameActive = false; // only true after Start

    private readonly HashSet<string> _dict;
    private Thread? _timerThread;

    public GameServer(IPAddress bindAddress, int port, string dictionaryPath)
    {
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(bindAddress, port));
        _listener.Listen(100);

        _dict = LoadDictionary(dictionaryPath);
        Console.WriteLine($"Loaded dictionary entries: {_dict.Count}");
    }

    private static HashSet<string> LoadDictionary(string path)
    {
        var hs = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (var line in File.ReadLines(path))
            {
                var s = WordRules.Normalize(line);
                if (!string.IsNullOrWhiteSpace(s))
                    hs.Add(s);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("WARNING: Không thể mở dictionary: " + ex.Message);
        }
        return hs;
    }

    public void Start()
    {
        _running = true;
        var t = new Thread(AcceptLoop) { IsBackground = true };
        t.Start();
        _timerThread = new Thread(TimerLoop) { IsBackground = true };
        _timerThread.Start();
    }

    public void Stop()
    {
        _running = false;
        try { _listener.Close(); } catch { }
        try { _timerThread?.Join(200); } catch { }
        lock (_lock)
        {
            foreach (var c in _clients.ToList()) c.Close();
            _clients.Clear();
            _turnIndex = -1;
            _used.Clear();
            _lastWord = null;
            _gameActive = false;
        }
    }

    private void AcceptLoop()
    {
        while (_running)
        {
            try
            {
                var socket = _listener.Accept();
                socket.NoDelay = true;
                var session = new ClientSession(socket, this);
                lock (_lock) _clients.Add(session);
                Console.WriteLine($"Client connected: {socket.RemoteEndPoint}");

                var t = new Thread(session.Run) { IsBackground = true };
                t.Start();
            }
            catch (SocketException se)
            {
                if (_running) Console.WriteLine("Accept error: " + se.Message);
            }
            catch (ObjectDisposedException) { }
        }
    }

    private void TimerLoop()
    {
        while (_running)
        {
            Thread.Sleep(200);
            ClientSession? current = null;
            lock (_lock)
            {
                if (!_gameActive) { current = null; }
                else if (_turnIndex >= 0 && _turnIndex < _clients.Count)
                {
                    int safety = _clients.Count + 1;
                    while (safety-- > 0 && _clients.Count > 0 && !_clients[_turnIndex].Alive)
                    {
                        _turnIndex = (_turnIndex + 1) % _clients.Count;
                    }
                    if (_clients.Count > 0)
                        current = _clients[_turnIndex];
                }
            }

            if (current == null) continue;

            bool timeout = false;
            lock (_lock)
            {
                timeout = DateTime.UtcNow >= _deadlineUtc;
            }
            if (!timeout) continue;

            HandleTimeout(current);
        }
    }

    private void HandleTimeout(ClientSession cur)
    {
        ClientSession? next = null;
        bool eliminated = false;
        string timeoutMsg = "";
        lock (_lock)
        {
            if (_clients.Count == 0 || _turnIndex < 0) return;
            if (!ReferenceEquals(_clients[_turnIndex], cur))
            {
                return;
            }

            cur.Lives--;
            eliminated = cur.Lives <= 0;
            var curName = cur.PlayerName ?? "?";

            if (eliminated)
         {
         cur.Alive = false; // Đánh dấu người chơi đã bị loại
          timeoutMsg = $"⏰ Hết giờ! {curName} mất 1 mạng và đã bị loại.";
   }
            else
            timeoutMsg = $"⏰ Hết giờ! {curName} mất 1 mạng.";

            if (eliminated)
 {
                int safety = _clients.Count + 1;
       int ix = _turnIndex;
   do
  {
 ix = (ix + 1) % _clients.Count;
   if (_clients[ix].Alive) { _turnIndex = ix; next = _clients[_turnIndex]; break; }
       } while (--safety > 0);

           if (next == null) { _turnIndex = -1; _gameActive = false; }
   }
            else
            {
      _turnIndex = (_turnIndex + 1) % _clients.Count;
       int safety = _clients.Count + 1;
     while (safety-- > 0 && !_clients[_turnIndex].Alive)
    _turnIndex = (_turnIndex + 1) % _clients.Count;
                next = _clients[_turnIndex];
         }

    _deadlineUtc = DateTime.UtcNow.AddSeconds(TurnLimitSeconds);
       // rs chữ sau khi m
     _lastWord = null;
            _used.Clear();
 }

     Broadcast(new InfoMessage { Message = timeoutMsg + " 🔄 Reset chuỗi." + (next != null ? $" ⟳ Qua lượt: {next.PlayerName}" : "") });
      if (eliminated)
        {
 Broadcast(new InfoMessage { Message = $"☠️ {cur.PlayerName} bị loại khỏi ván." });
        }

        // Kiểm tra điều kiện thắng/thua
        string? winner = null;
      lock (_lock)
        {
       var alive = _clients.Where(c => c.Alive).ToList();
            if (alive.Count == 1) 
        winner = alive[0].PlayerName;
     else if (alive.Count == 0)
  {
      // Không còn ai sống -> hòa
           Broadcast(new InfoMessage { Message = $"🏳️ Ván kết thúc! Tất cả đều thua. ⏹️ Chờ Start/Reset để chơi lại." });
       ResetGame(keepLives:false, startTimer:false);
       return;
            }
        }
 
        if (winner != null)
     {
            Broadcast(new GameEndMessage { Winner = winner, Message = $"🏆 Ván kết thúc! Chiến thắng: {winner}" });
            ResetGame(keepLives:false, startTimer:false);
            return;
        }

        BroadcastState();
  }

    internal void OnClientDisconnected(ClientSession s)
    {
    string? winner = null;
        lock (_lock)
  {
     var ix = _clients.IndexOf(s);
            if (ix >= 0) _clients.RemoveAt(ix);
         if (_clients.Count == 0) { _turnIndex = -1; _gameActive = false; }
            else if (_turnIndex >= _clients.Count) _turnIndex = 0;

            // Kiểm tra nếu game đang active và chỉ còn 1 người sống
          if (_gameActive)
      {
   var alive = _clients.Where(c => c.Alive).ToList();
 if (alive.Count == 1)
   winner = alive[0].PlayerName;
         }
        }
        
        if (!string.IsNullOrWhiteSpace(s.PlayerName))
    {
            Broadcast(new InfoMessage { Message = $"⚪ {s.PlayerName} rời trò chơi." });
            
   if (winner != null)
            {
     Broadcast(new GameEndMessage { Winner = winner, Message = $"🏆 {winner} chiến thắng do đối thủ rời game!" });
              ResetGame(keepLives: false, startTimer: false);
            }
            else
   {
       BroadcastPlayers();
        BroadcastState();
         }
        }
     Console.WriteLine($"Client disconnected: {s.RemoteEndPoint}");
    }

    internal void HandleLine(ClientSession s, string jsonLine)
    {
        var type = MessageSerializer.PeekType(jsonLine);
        try
        {
            switch (type)
            {
                case MessageTypes.Join: HandleJoin(s, MessageSerializer.Deserialize<JoinMessage>(jsonLine)); break;
                case MessageTypes.SubmitWord: HandleSubmitWord(s, MessageSerializer.Deserialize<SubmitWordMessage>(jsonLine)); break;
                case MessageTypes.Start:
                    ResetGame(keepLives:false, startTimer:true);
                    Broadcast(new InfoMessage { Message = $"🏁 Bắt đầu ván! Người đi đầu tiên: {GetTurnName()}" });
                    BroadcastState();
                    break;
                default:
                    s.Send(new ErrorMessage { Message = $"Unsupported type: {type}" });
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Handle error: " + ex.Message);
            s.Send(new ErrorMessage { Message = "Invalid message." });
        }
    }

    private void HandleJoin(ClientSession s, JoinMessage msg)
    {
        string baseName = string.IsNullOrWhiteSpace(msg.Name) ? "Player" : msg.Name.Trim();
        string finalName = baseName;
        lock (_lock)
        {
            var exists = _clients.Where(c => !string.IsNullOrWhiteSpace(c.PlayerName)).Select(c => c.PlayerName!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int i = 1;
            while (exists.Contains(finalName)) finalName = $"{baseName}{++i}";
            s.PlayerName = finalName;
            s.Lives = InitialLives;
            s.Alive = true;
            if (!_gameActive && _turnIndex == -1) _turnIndex = 0; // set for display only; timer still off
        }
        s.Send(new JoinedMessage { Ok = true, Message = $"🟢 Chào {finalName}!", AssignedName = finalName, Players = GetPlayerNames() });
        BroadcastExcept(new InfoMessage { Message = $"🟢 {finalName} đã tham gia." }, s);
        BroadcastPlayers();
        BroadcastState();
    }

    private void HandleSubmitWord(ClientSession s, SubmitWordMessage msg)
    {
        if (string.IsNullOrWhiteSpace(s.PlayerName)) { s.Send(new ErrorMessage { Message = "Bạn chưa tham gia." }); return; }

        lock (_lock)
        {
            if (_clients.Count == 0) { s.Send(new ErrorMessage { Message = "Chưa có người chơi." }); return; }
            if (!_gameActive) { s.Send(new ErrorMessage { Message = "Ván chưa bắt đầu. Hãy bấm Start/Reset." }); return; }

            var cur = _clients[_turnIndex];
            if (!ReferenceEquals(cur, s))
            {
                s.Send(new ErrorMessage { Message = $"Chưa đến lượt bạn. Hiện tại: {cur.PlayerName}" });
                return;
            }

            string? nextReq;
            bool ok = WordRules.IsValidChainByWord(_lastWord, msg.Word, out nextReq);
            var normalized = WordRules.Normalize(msg.Word);
            if (ok && _used.Contains(normalized)) ok = false;

            if (ok && _dict.Count > 0 && !_dict.Contains(normalized))
            {
                s.Send(new WordResultMessage {
                    Ok = false,
                    Message = "⚠️ Cụm từ không có trong từ điển.",
                    Player = s.PlayerName!,
                    Word = msg.Word,
                    NextRequired = WordRules.LastWordNormalized(_lastWord ?? "") ?? "",
                    NextTurn = cur.PlayerName!,
                    TurnRemainingSeconds = Math.Max(0, (int)(_deadlineUtc - DateTime.UtcNow).TotalSeconds)
                });
                return;
            }

            if (!ok)
            {
                s.Send(new WordResultMessage {
                    Ok = false,
                    Message = "⚠️ Không hợp lệ theo luật nối TỪ CUỐI hoặc đã dùng.",
                    Player = s.PlayerName!,
                    Word = msg.Word,
                    NextRequired = WordRules.LastWordNormalized(_lastWord ?? "") ?? "",
                    NextTurn = cur.PlayerName!,
                    TurnRemainingSeconds = Math.Max(0, (int)(_deadlineUtc - DateTime.UtcNow).TotalSeconds)
                });
                return;
            }

            _lastWord = msg.Word;
            _used.Add(normalized);

            _turnIndex = (_turnIndex + 1) % _clients.Count;
            int safety = _clients.Count + 1;
            while (safety-- > 0 && !_clients[_turnIndex].Alive)
                _turnIndex = (_turnIndex + 1) % _clients.Count;
            var nextTurn = _clients[_turnIndex];
            _deadlineUtc = DateTime.UtcNow.AddSeconds(TurnLimitSeconds);

            Broadcast(new WordResultMessage {
                Ok = true,
                Message = "✅ Hợp lệ.",
                Player = s.PlayerName!,
                Word = msg.Word,
                NextRequired = nextReq,
                NextTurn = nextTurn.PlayerName!,
                TurnRemainingSeconds = TurnLimitSeconds
            });
        }
        BroadcastState();
    }

    private void ResetGame(bool keepLives, bool startTimer)
    {
        lock (_lock)
        {
            _lastWord = null; _used.Clear();
            if (!keepLives)
            {
                foreach (var c in _clients) { c.Lives = InitialLives; c.Alive = true; }
            }
            if (startTimer && _clients.Any(c => c.Alive))
            {
                _turnIndex = 0;
                int safety2 = _clients.Count + 1;
                while (safety2-- > 0 && !_clients[_turnIndex].Alive)
                    _turnIndex = (_turnIndex + 1) % _clients.Count;
                _deadlineUtc = DateTime.UtcNow.AddSeconds(TurnLimitSeconds);
                _gameActive = true;
            }
            else
            {
                _turnIndex = -1;
                _gameActive = false;
                _deadlineUtc = DateTime.UtcNow.AddYears(10);
            }
        }
        Broadcast(new InfoMessage { Message = "🔁 Reset ván." });
        BroadcastState();
    }

    private List<string> GetPlayerNames()
    {
        lock (_lock) return _clients.Where(c => !string.IsNullOrWhiteSpace(c.PlayerName)).Select(c => c.PlayerName!).ToList();
    }

    private string GetTurnName()
    {
        lock (_lock)
        {
            if (_turnIndex >= 0 && _turnIndex < _clients.Count) return _clients[_turnIndex].PlayerName ?? "";
            return "";
        }
    }

    private Dictionary<string,int> GetLives()
    {
        lock (_lock)
        {
            var d = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in _clients)
            {
                if (!string.IsNullOrWhiteSpace(c.PlayerName))
                    d[c.PlayerName!] = c.Lives;
            }
            return d;
        }
    }

    private int GetRemainingSeconds()
    {
        lock (_lock)
        {
            if (!_gameActive) return 0;
            return Math.Max(0, (int)(_deadlineUtc - DateTime.UtcNow).TotalSeconds);
        }
    }

    private void BroadcastPlayers() => Broadcast(new PlayersMessage { Players = GetPlayerNames() });

    private void BroadcastState()
    {
        string turn = "";
        string? nextReq = null;
        lock (_lock)
        {
            if (_clients.Count > 0 && _turnIndex >= 0 && _turnIndex < _clients.Count) turn = _clients[_turnIndex].PlayerName ?? "";
            if (!string.IsNullOrWhiteSpace(_lastWord)) nextReq = WordRules.LastWordNormalized(_lastWord);
        }
        Broadcast(new GameStateMessage { 
            LastWord = _lastWord, 
            NextRequired = nextReq, 
            Turn = turn, 
            Players = GetPlayerNames(), 
            UsedWords = new HashSet<string>(_used),
            Lives = GetLives(),
            TurnRemainingSeconds = GetRemainingSeconds()
        });
    }

    internal void Broadcast<T>(T msg)
    {
        var json = MessageSerializer.Serialize(msg) + "\n";
        var data = Encoding.UTF8.GetBytes(json);
        lock (_lock)
        {
            foreach (var c in _clients.ToList())
            {
                try { c.SendRaw(data); } catch {}
            }
        }
    }

    internal void BroadcastExcept<T>(T msg, ClientSession except)
    {
        var json = MessageSerializer.Serialize(msg) + "\n";
        var data = Encoding.UTF8.GetBytes(json);
        lock (_lock)
        {
            foreach (var c in _clients.ToList())
            {
                if (object.ReferenceEquals(c, except)) continue;
                try { c.SendRaw(data); } catch {}
            }
        }
    }
}

public class ClientSession
{
    private readonly Socket _socket;
    private readonly GameServer _server;
    private readonly byte[] _buf = new byte[8192];
    private readonly StringBuilder _sb = new();
    private volatile bool _running = true;
    private readonly object _sendLock = new();
    private volatile bool _disposed = false;
    private readonly string _remoteEndPoint;

    public string? PlayerName { get; set; }
public int Lives { get; set; } = GameServer.InitialLives;
 public bool Alive { get; set; } = true;
    public string RemoteEndPoint => _remoteEndPoint;

    public ClientSession(Socket socket, GameServer server) 
    { 
        _socket = socket; 
        _server = server;
        _remoteEndPoint = socket.RemoteEndPoint?.ToString() ?? "Unknown";
    }

    public void Run()
    {
        try
        {
 while (_running)
   {
  var line = ReadLine();
 if (line == null) break;
                _server.HandleLine(this, line);
 }
    }
    catch (Exception ex) { Console.WriteLine("Client loop error: " + ex.Message); }
        finally { Close(); _server.OnClientDisconnected(this); }
    }

    private string? ReadLine()
    {
      while (_running)
        {
  for (int i = 0; i < _sb.Length; i++)
            {
             if (_sb[i] == '\n')
            {
   var s = _sb.ToString(0, i).TrimEnd('\r');
           _sb.Remove(0, i + 1);
     return s;
        }
            }
     int read;
            try { read = _socket.Receive(_buf); } catch { return null; }
   if (read <= 0) return null;
  _sb.Append(Encoding.UTF8.GetString(_buf, 0, read));
        }
    return null;
    }

    public void Send<T>(T message) => SendRaw(Encoding.UTF8.GetBytes(MessageSerializer.Serialize(message) + "\n"));

    public void SendRaw(byte[] data)
  {
        if (_disposed) return;
      
        lock (_sendLock)
        {
          try
     {
         if (_disposed || !_socket.Connected) return;
                
int sent = 0;
       while (sent < data.Length)
           sent += _socket.Send(data, sent, data.Length - sent, SocketFlags.None);
            }
catch (ObjectDisposedException) { }
            catch (SocketException) { }
        }
    }

    public void Close()
    {
      if (_disposed) return;
    
        _running = false;
        _disposed = true;
        
 try { _socket.Shutdown(SocketShutdown.Both); } catch { }
        try { _socket.Close(); } catch { }
    }
}
