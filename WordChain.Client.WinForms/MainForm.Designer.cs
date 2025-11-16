
using System.Windows.Forms;
using System.Drawing;

namespace WordChain.Client.WinForms
{
    public partial class MainForm
    {
        private Panel pnlTop;
        private SplitContainer splitMain;
        private Label labelHost;
        private Label labelPort;
        private Label labelName;

        private TextBox txtHost;
        private TextBox txtPort;
        private TextBox txtName;
        private Button btnConnect;
        private Button btnStart;
        private Label lblTurn;
        private Label lblTimer;

        private Label lblLast;
        private TextBox txtLog;
        private TextBox txtWord;
        private Button btnSend;

        private ListBox lstPlayers;
        private ListBox lstUsed;
        private Label lblPlayers;
        private Label lblUsedWords;

        private void InitializeComponent()
        {
            pnlTop = new Panel();
            labelHost = new Label();
            labelPort = new Label();
            labelName = new Label();
            txtHost = new TextBox();
            txtPort = new TextBox();
            txtName = new TextBox();
            btnConnect = new Button();
            btnStart = new Button();
            lblTurn = new Label();
            lblTimer = new Label();
            splitMain = new SplitContainer();
            lblLast = new Label();
            txtLog = new TextBox();
            txtWord = new TextBox();
            btnSend = new Button();
            lblPlayers = new Label();
            lstPlayers = new ListBox();
            lblUsedWords = new Label();
            lstUsed = new ListBox();
            pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTop
            // 
            pnlTop.Controls.Add(labelHost);
            pnlTop.Controls.Add(labelPort);
            pnlTop.Controls.Add(labelName);
            pnlTop.Controls.Add(txtHost);
            pnlTop.Controls.Add(txtPort);
            pnlTop.Controls.Add(txtName);
            pnlTop.Controls.Add(btnConnect);
            pnlTop.Controls.Add(btnStart);
            pnlTop.Controls.Add(lblTurn);
            pnlTop.Controls.Add(lblTimer);
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Location = new Point(0, 0);
            pnlTop.Margin = new Padding(3, 4, 3, 4);
            pnlTop.Name = "pnlTop";
            pnlTop.Size = new Size(1133, 64);
            pnlTop.TabIndex = 1;
            // 
            // labelHost
            // 
            labelHost.Location = new Point(9, 19);
            labelHost.Name = "labelHost";
            labelHost.Size = new Size(46, 27);
            labelHost.TabIndex = 0;
            labelHost.Text = "Host:";
            // 
            // labelPort
            // 
            labelPort.Location = new Point(199, 19);
            labelPort.Name = "labelPort";
            labelPort.Size = new Size(41, 27);
            labelPort.TabIndex = 1;
            labelPort.Text = "Port:";
            // 
            // labelName
            // 
            labelName.Location = new Point(309, 19);
            labelName.Name = "labelName";
            labelName.Size = new Size(55, 27);
            labelName.TabIndex = 2;
            labelName.Text = "Name:";
            // 
            // txtHost
            // 
            txtHost.Location = new Point(57, 13);
            txtHost.Margin = new Padding(3, 4, 3, 4);
            txtHost.Name = "txtHost";
            txtHost.Size = new Size(137, 27);
            txtHost.TabIndex = 3;
            txtHost.Text = "127.0.0.1";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(240, 13);
            txtPort.Margin = new Padding(3, 4, 3, 4);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(68, 27);
            txtPort.TabIndex = 4;
            txtPort.Text = "5000";
            // 
            // txtName
            // 
            txtName.Location = new Point(363, 13);
            txtName.Margin = new Padding(3, 4, 3, 4);
            txtName.Name = "txtName";
            txtName.Size = new Size(137, 27);
            txtName.TabIndex = 5;
            txtName.Text = "Player";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(507, 11);
            btnConnect.Margin = new Padding(3, 4, 3, 4);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(105, 37);
            btnConnect.TabIndex = 6;
            btnConnect.Text = "Connect";
            // 
            // btnStart
            // 
            btnStart.Location = new Point(619, 11);
            btnStart.Margin = new Padding(3, 4, 3, 4);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(114, 37);
            btnStart.TabIndex = 7;
            btnStart.Text = "Start/Reset";
            // 
            // lblTurn
            // 
            lblTurn.AutoSize = true;
            lblTurn.Location = new Point(743, 11);
            lblTurn.Name = "lblTurn";
            lblTurn.Size = new Size(95, 20);
            lblTurn.TabIndex = 8;
            lblTurn.Text = "Chưa kết nối.";
            // 
            // lblTimer
            // 
            lblTimer.AutoSize = true;
            lblTimer.Location = new Point(743, 35);
            lblTimer.Name = "lblTimer";
            lblTimer.Size = new Size(46, 20);
            lblTimer.TabIndex = 9;
            lblTimer.Text = "🕒 --";
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(0, 64);
            splitMain.Margin = new Padding(3, 4, 3, 4);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(lblLast);
            splitMain.Panel1.Controls.Add(txtLog);
            splitMain.Panel1.Controls.Add(txtWord);
            splitMain.Panel1.Controls.Add(btnSend);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(lblPlayers);
            splitMain.Panel2.Controls.Add(lstPlayers);
            splitMain.Panel2.Controls.Add(lblUsedWords);
            splitMain.Panel2.Controls.Add(lstUsed);
            splitMain.Size = new Size(1133, 789);
            splitMain.SplitterDistance = 912;
            splitMain.SplitterWidth = 5;
            splitMain.TabIndex = 0;
            // 
            // lblLast
            // 
            lblLast.AutoSize = true;
            lblLast.Location = new Point(9, 8);
            lblLast.Name = "lblLast";
            lblLast.Size = new Size(121, 20);
            lblLast.TabIndex = 0;
            lblLast.Text = "LastWord: (none)";
            // 
            // txtLog
            // 
            txtLog.Location = new Point(9, 37);
            txtLog.Margin = new Padding(3, 4, 3, 4);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(708, 585);
            txtLog.TabIndex = 1;
            // 
            // txtWord
            // 
            txtWord.Location = new Point(9, 637);
            txtWord.Margin = new Padding(3, 4, 3, 4);
            txtWord.Name = "txtWord";
            txtWord.PlaceholderText = "Nhập cụm từ... (phải có trong từ điển)";
            txtWord.Size = new Size(617, 27);
            txtWord.TabIndex = 2;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(635, 635);
            btnSend.Margin = new Padding(3, 4, 3, 4);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(82, 40);
            btnSend.TabIndex = 3;
            btnSend.Text = "Gửi";
            // 
            // lblPlayers
            // 
            lblPlayers.AutoSize = true;
            lblPlayers.Location = new Point(7, 8);
            lblPlayers.Name = "lblPlayers";
            lblPlayers.Size = new Size(200, 20);
            lblPlayers.TabIndex = 0;
            lblPlayers.Text = "Players (❤❤❤ = 3 mạng)";
            // 
            // lstPlayers
            // 
            lstPlayers.Location = new Point(7, 28);
            lstPlayers.Margin = new Padding(3, 4, 3, 4);
            lstPlayers.Name = "lstPlayers";
            lstPlayers.Size = new Size(200, 304);
            lstPlayers.TabIndex = 1;
            // 
            // lblUsedWords
            // 
            lblUsedWords.AutoSize = true;
            lblUsedWords.Location = new Point(7, 360);
            lblUsedWords.Name = "lblUsedWords";
            lblUsedWords.Size = new Size(86, 20);
            lblUsedWords.TabIndex = 2;
            lblUsedWords.Text = "Used words";
            // 
            // lstUsed
            // 
            lstUsed.Location = new Point(7, 383);
            lstUsed.Margin = new Padding(3, 4, 3, 4);
            lstUsed.Name = "lstUsed";
            lstUsed.Size = new Size(200, 284);
            lstUsed.TabIndex = 3;
            // 
            // MainForm
            // 
            AcceptButton = btnSend;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1133, 853);
            Controls.Add(splitMain);
            Controls.Add(pnlTop);
            Font = new Font("Segoe UI", 9F);
            Margin = new Padding(3, 4, 3, 4);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WordChain Client ";
            pnlTop.ResumeLayout(false);
            pnlTop.PerformLayout();
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel1.PerformLayout();
            splitMain.Panel2.ResumeLayout(false);
            splitMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
