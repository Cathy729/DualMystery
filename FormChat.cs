using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DualMystery
{
    public partial class FormChat : Form
    {
        private string _playerIdentity;
        private RichTextBox rtbChat;
        private TextBox txtInput;
        private Button btnSend;
        private Button btnShareClue;
        private Button btnHangUp;

        public FormChat(string playerIdentity)
        {
            InitializeComponent();
            _playerIdentity = playerIdentity;
            InitializeCustomUI();

            LoadHistory();
            ChatService.OnMessageReceived += ChatService_OnMessageReceived;
            PhoneManager.OnCallEnded += PhoneManager_OnCallEnded;
            this.FormClosing += FormChat_FormClosing;
            this.Shown += (s, e) =>
            {
                rtbChat.ScrollToCaret();
                rtbChat.PerformLayout();
                rtbChat.Invalidate();
            };
        }

        private void InitializeCustomUI()
        {
            this.Text = $"{_playerIdentity} 的电话";
            this.BackColor = Color.FromArgb(253, 245, 230); // 旧纸色
            this.ClientSize = new Size(500, 400);            // 稍微加大
            this.StartPosition = FormStartPosition.Manual;
            if (_playerIdentity == "A") this.Location = new Point(100, 100);
            else this.Location = new Point(850, 100);

            Color darkBrown = Color.FromArgb(92, 64, 51);

            // 顶部标题
            Label lblTitle = new Label();
            lblTitle.Text = "☎ 壁挂电话";
            lblTitle.Font = new Font("Georgia", 14f);
            lblTitle.ForeColor = darkBrown;
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Height = 40;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitle);

            // 底部面板
            Panel pnlBottom = new Panel();
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 55;
            pnlBottom.BackColor = this.BackColor;

            txtInput = new TextBox();
            txtInput.Location = new Point(10, 12);
            txtInput.Width = 160;
            txtInput.Font = new Font(this.Font.FontFamily, 10f);

            btnSend = new Button();
            btnSend.Text = "发送";
            btnSend.Location = new Point(180, 10);
            btnSend.Width = 65;
            btnSend.Height = 30;
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.ForeColor = darkBrown;
            btnSend.Font = new Font(this.Font.FontFamily, 10f);
            btnSend.Click += BtnSend_Click;

            btnShareClue = new Button();
            btnShareClue.Text = "分享线索";
            btnShareClue.Location = new Point(255, 10);
            btnShareClue.Width = 95;
            btnShareClue.Height = 30;
            btnShareClue.FlatStyle = FlatStyle.Flat;
            btnShareClue.ForeColor = darkBrown;
            btnShareClue.Font = new Font(this.Font.FontFamily, 10f);
            btnShareClue.Click += BtnShareClue_Click;

            btnHangUp = new Button();
            btnHangUp.Text = "挂断";
            btnHangUp.Location = new Point(360, 10);
            btnHangUp.Width = 65;
            btnHangUp.Height = 30;
            btnHangUp.FlatStyle = FlatStyle.Flat;
            btnHangUp.ForeColor = darkBrown;
            btnHangUp.Font = new Font(this.Font.FontFamily, 10f);
            btnHangUp.Click += BtnHangUp_Click;

            pnlBottom.Controls.Add(txtInput);
            pnlBottom.Controls.Add(btnSend);
            pnlBottom.Controls.Add(btnShareClue);
            pnlBottom.Controls.Add(btnHangUp);
            this.Controls.Add(pnlBottom);

            // 聊天框 (占满中间余下部分)
            rtbChat = new RichTextBox();
            rtbChat.Dock = DockStyle.Fill;
            rtbChat.Font = new Font("Courier New", 10f);
            rtbChat.ForeColor = darkBrown;
            rtbChat.BackColor = this.BackColor;
            rtbChat.ReadOnly = true;
            rtbChat.BorderStyle = BorderStyle.None;
            rtbChat.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbChat.HideSelection = false;
            rtbChat.WordWrap = true;
            this.Controls.Add(rtbChat);
            rtbChat.BringToFront();

            this.AcceptButton = btnSend; // 按回车可快捷发送
        }

        private void LoadHistory()
        {
            foreach (var msg in ChatService.History)
            {
                AppendMessage(msg);
            }
        }

        private void ChatService_OnMessageReceived(ChatMessage msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ChatMessage>(ChatService_OnMessageReceived), msg);
                return;
            }
            AppendMessage(msg);
        }

        private void AppendMessage(ChatMessage msg)
        {
            string senderName = msg.Sender == "System" ? "[系统]" : $"[{msg.Sender}]";
            rtbChat.AppendText($"{senderName} {msg.Text}\n");

            // 强制刷新，确保内容立即可见
            rtbChat.SelectionStart = rtbChat.Text.Length;
            rtbChat.ScrollToCaret();
            rtbChat.PerformLayout();
            rtbChat.Invalidate();
            rtbChat.Update();
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtInput.Text))
            {
                ChatService.SendMessage(_playerIdentity, txtInput.Text);
                txtInput.Clear();
            }
        }

        private void BtnShareClue_Click(object sender, EventArgs e)
        {
            var myClues = GameManager.AllClues
                .Where(c => c.IsDiscovered && c.DiscoveredBy == _playerIdentity)
                .ToList();

            if (myClues.Count == 0)
            {
                MessageBox.Show("还没有线索可分享", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new FormSelectClue(myClues))
            {
                if (dialog.ShowDialog() == DialogResult.OK && dialog.SelectedClue != null)
                {
                    string targetIdentity = _playerIdentity == "A" ? "B" : "A";
                    GameManager.DiscoverClue(dialog.SelectedClue.Id, targetIdentity);

                    // 分享时带上原始描述
                    ChatService.SendMessage("System",
                        $"{_playerIdentity} 分享了线索：{dialog.SelectedClue.Name} —— {dialog.SelectedClue.Description}");
                }
            }
        }

        private void BtnHangUp_Click(object sender, EventArgs e)
        {
            PhoneManager.OnCallEnded -= PhoneManager_OnCallEnded;
            PhoneManager.HangUp(_playerIdentity);
            this.Close();
        }

        private void PhoneManager_OnCallEnded()
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(PhoneManager_OnCallEnded));
                return;
            }
            this.Close();
        }

        private void FormChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            ChatService.OnMessageReceived -= ChatService_OnMessageReceived;
            PhoneManager.OnCallEnded -= PhoneManager_OnCallEnded;
        }
    }

    public class FormSelectClue : Form
    {
        public Clue SelectedClue { get; private set; }
        private ComboBox cmbClues;

        public FormSelectClue(List<Clue> clues)
        {
            this.Text = "分享线索";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lbl = new Label();
            lbl.Text = "请选择你要分享的线索：";
            lbl.Location = new Point(20, 15);
            lbl.AutoSize = true;

            cmbClues = new ComboBox();
            cmbClues.Location = new Point(20, 40);
            cmbClues.Width = 240;
            cmbClues.DropDownStyle = ComboBoxStyle.DropDownList;

            // 绑定数据源
            cmbClues.DisplayMember = "Name";
            cmbClues.DataSource = clues;

            Button btnOk = new Button();
            btnOk.Text = "确定";
            btnOk.Location = new Point(80, 75);
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Click += (s, e) => { SelectedClue = cmbClues.SelectedItem as Clue; };

            Button btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Location = new Point(170, 75);
            btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lbl);
            this.Controls.Add(cmbClues);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}