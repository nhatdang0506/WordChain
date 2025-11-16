using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WordChain.Common.Messaging;

namespace WordChain.Client.WinForms;

public partial class MainForm : Form
{
    private readonly NetworkClient _net = new();
    private string _myName = "";
    private bool _isMyTurn = false;
    private string? _nextRequired = null;
    private int _remainingSec = 0;
    private readonly System.Windows.Forms.Timer _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
    private Dictionary<string,int> _lives = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _players = new();
    private string _currentTurn = "";

    public MainForm()
    {
        InitializeComponent();

       
        btnConnect.Click += btnConnect_Click;
        btnStart.Click += btnStart_Click;
        btnSend.Click += btnSend_Click;

        this.AcceptButton = btnSend;
        UpdateUiState(false);

        _net.LineReceived += OnLine;
        _net.Info += msg => Append(msg);
        _net.Error += msg => Append("⚠️ " + msg);

        _uiTimer.Tick += (s,e) => { if (_remainingSec > 0) { _remainingSec--; UpdateTimerLabel(); } };
        _uiTimer.Start();
    }

    private void UpdateUiState(bool connected)
    {
        btnConnect.Text = connected ? "Disconnect" : "Connect";
        txtHost.Enabled = txtPort.Enabled = txtName.Enabled = !connected;
        btnStart.Enabled = connected;
        btnSend.Enabled = connected && _isMyTurn;
        txtWord.Enabled = connected;
        SetTurnStatus();
        UpdateTimerLabel();
    }

    private void Append(string s)
    {
        if (txtLog.InvokeRequired) { txtLog.BeginInvoke(new Action<string>(Append), s); return; }
        txtLog.AppendText($"{s}\r\n");
    }

    private void SetTurnStatus()
    {
        if (lblTurn.InvokeRequired) { lblTurn.BeginInvoke(new Action(SetTurnStatus)); return; }
        var t = _isMyTurn ? "▶ ĐẾN LƯỢT BẠN" : $"⟳ Lượt: {_currentTurn}";
        var hint = _nextRequired != null ? $"  |  Chuỗi cần: {_nextRequired}" : "";
        lblTurn.Text = t + hint;
        btnSend.Enabled = _isMyTurn && _net.Connected;
    }

    private void UpdatePlayersWithLives(List<string> players, Dictionary<string,int> lives, string turnName)
    {
        if (lstPlayers.InvokeRequired) { lstPlayers.BeginInvoke(new Action<List<string>,Dictionary<string,int>,string>(UpdatePlayersWithLives), players, lives, turnName); return; }
        _players = players;
        _lives = lives;
        lstPlayers.BeginUpdate();
        try
        {
            lstPlayers.Items.Clear();
            foreach (var p in players)
            {
                lives.TryGetValue(p, out var lv);
                var hearts = Hearts(lv);
                var prefix = string.Equals(p, turnName, StringComparison.OrdinalIgnoreCase) ? "▶ " : "   ";
                lstPlayers.Items.Add($"{prefix}{p}  {hearts}");
            }
        }
        finally { lstPlayers.EndUpdate(); }
    }

    private static string Hearts(int lives)
    {
        lives = Math.Max(0, Math.Min(3, lives));
        return new string('❤', lives) + new string('♡', 3 - lives);
    }

    private void UpdateUsed(string[] used)
    {
        if (lstUsed.InvokeRequired) { lstUsed.BeginInvoke(new Action<string[]>(UpdateUsed), new object[] { used }); return; }
        lstUsed.BeginUpdate();
        try { lstUsed.Items.Clear(); foreach (var w in used) lstUsed.Items.Add(w); }
        finally { lstUsed.EndUpdate(); }
    }

    private void SetLastWord(string? last)
    {
        if (lblLast.InvokeRequired) { lblLast.BeginInvoke(new Action<string?>(SetLastWord), last); return; }
        lblLast.Text = $"LastWord: {last ?? "(none)"}";
    }

    private void UpdateTimerLabel()
    {
        if (lblTimer.InvokeRequired) { lblTimer.BeginInvoke(new Action(UpdateTimerLabel)); return; }
        lblTimer.Text = _remainingSec > 0 ? $"🕒 {_remainingSec}s" : "🕒 --";
    }

    private void Connect()
    {
        try
        {
            var host = txtHost.Text.Trim();
            var port = int.TryParse(txtPort.Text, out var p) ? p : 5000;
            _net.Connect(host, port);
            _net.Send(new JoinMessage { Name = txtName.Text.Trim() });
            Append($"⚪ Đã kết nối. ⦿ Đang gửi Join...");
            UpdateUiState(true);
        }
        catch (Exception ex)
        {
            Append("⚠️ Lỗi kết nối: " + ex.Message);
            Disconnect();
        }
    }

    private void Disconnect()
    {
        _net.Disconnect();
        _isMyTurn = false;
        _nextRequired = null;
        _myName = "";
        _remainingSec = 0;
        UpdateUiState(false);
        lblTurn.Text = "Đã ngắt kết nối.";
    }

    private void SendWord()
    {
        var word = txtWord.Text.Trim();
        if (string.IsNullOrWhiteSpace(word)) return;
        if (!_net.Connected) { Append("⚠️ Chưa kết nối."); return; }
        _net.Send(new SubmitWordMessage { Word = word });
        txtWord.Clear();
    }

    private void OnLine(string jsonLine)
    {
        try
        {
            var type = MessageSerializer.PeekType(jsonLine);
            switch (type)
            {
                case MessageTypes.Joined:
                    var m1 = MessageSerializer.Deserialize<JoinedMessage>(jsonLine);
                    _myName = m1.AssignedName;
                    Append($"🟢 Bạn đã tham gia với tên: {m1.AssignedName}.");
                    UpdatePlayersWithLives(m1.Players, _lives, turnName: "");
                    break;

                case MessageTypes.Info:
                    var mi = MessageSerializer.Deserialize<InfoMessage>(jsonLine);
                    // Ignore join broadcast if it's ourselves (đã có Joined msg rồi)
                    if (mi.Message.Contains("đã tham gia") && mi.Message.Contains(_myName)) break;
                    Append(mi.Message);
                    break;

                case MessageTypes.Error:
                    var me = MessageSerializer.Deserialize<ErrorMessage>(jsonLine);
                    Append("⚠️ " + me.Message);
                    break;

                case MessageTypes.Players:
                    var mp = MessageSerializer.Deserialize<PlayersMessage>(jsonLine);
                    UpdatePlayersWithLives(mp.Players, _lives, turnName: "");
                    break;

                case MessageTypes.GameState:
                    var mg = MessageSerializer.Deserialize<GameStateMessage>(jsonLine);
                    SetLastWord(mg.LastWord);
                    _nextRequired = mg.NextRequired;
                    _currentTurn = mg.Turn;
                    _isMyTurn = string.Equals(_myName, mg.Turn, StringComparison.OrdinalIgnoreCase);
                    _remainingSec = mg.TurnRemainingSeconds;
                    UpdateUsed(new System.Collections.Generic.List<string>(mg.UsedWords).ToArray());
                    UpdatePlayersWithLives(mg.Players, mg.Lives, mg.Turn);
                    SetTurnStatus();
                    UpdateTimerLabel();
                    break;

                case MessageTypes.WordResult:
                    var mw = MessageSerializer.Deserialize<WordResultMessage>(jsonLine);
                    var icon = mw.Ok ? "✅" : "⚠️";
                    Append($"{icon} {mw.Player} ► {mw.Word}. {mw.Message}");
                    _isMyTurn = string.Equals(_myName, mw.NextTurn, StringComparison.OrdinalIgnoreCase);
                    _nextRequired = mw.NextRequired;
                    _remainingSec = mw.TurnRemainingSeconds;
                    SetTurnStatus();
                    UpdateTimerLabel();
                    break;

                case MessageTypes.GameEnd:
                    var ge = MessageSerializer.Deserialize<GameEndMessage>(jsonLine);
                    Append("━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    if (string.Equals(_myName, ge.Winner, StringComparison.OrdinalIgnoreCase))
                        {
                        Append("🎉🏆 CHIẾN THẮNG! 🏆🎉");
                        Append($"Chúc mừng {_myName} đã thắng cuộc!");
                        MessageBox.Show($"🏆 Chúc mừng!\nBạn đã chiến thắng!", "Thắng cuộc", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                     else
                        {
                             Append($"☠️ BẠN ĐÃ THUA!");
                        Append($"Người thắng cuộc: {ge.Winner}");
                         MessageBox.Show($"☠️ Bạn đã thua!\nNgười chiến thắng: {ge.Winner}", "Thua cuộc", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    Append("━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    _isMyTurn = false;
                    _remainingSec = 0;
                    SetTurnStatus();
                UpdateTimerLabel();
                break;

                default:
                    Append("⚠️ Không biết message: " + type);
                    break;
            }
        }
        catch (Exception ex)
        {
            Append("⚠️ Parse error: " + ex.Message);
        }
    }

    private void btnConnect_Click(object? sender, EventArgs e) { if (_net.Connected) Disconnect(); else Connect(); }
    private void btnStart_Click(object? sender, EventArgs e) { if (_net.Connected) _net.Send(new StartMessage()); }
    private void btnSend_Click(object? sender, EventArgs e) { SendWord(); }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        _net.Dispose();
    }
}
