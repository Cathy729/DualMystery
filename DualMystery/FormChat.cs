using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DualMystery
{
    public partial class FormChat : Form
    {
        private string _playerIdentity;
        private GameClient gameClient;
        private RichTextBox rtbChat;
        private TextBox txtInput;
        private Button btnSend;
        private Button btnShareClue;
        private Button btnHangUp;

        public FormChat(string playerIdentity, GameClient client)
        {
            InitializeComponent();
            _playerIdentity = playerIdentity;
            gameClient = client;
            InitializeCustomUI();

            gameClient.OnChatMessageReceived += GameClient_OnChatMessageReceived;
            gameClient.OnCallEnded += GameClient_OnCallEnded;
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
            this.BackColor = Theme.BgMain;
            this.ClientSize = new Size(420, 360);
            this.StartPosition = FormStartPosition.Manual;
            if (_playerIdentity == "A") this.Location = new Point(100, 100);
            else this.Location = new Point(850, 100);

            Label lblTitle = new Label
            {
                Text = Theme.DecorateTitle("☎ 壁挂电话"),
                Font = Theme.GetFont(14f),
                ForeColor = Theme.Accent,
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            // 标题分隔线
            this.Controls.Add(Theme.CreateTitleSeparator());

            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Theme.BgMain
            };
            Theme.StylePanelWithBorder(pnlBottom);

            txtInput = new TextBox
            {
                Location = new Point(10, 12),
                Width = 160,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = Theme.GetFont(10f)
            };

            btnSend = new Button
            {
                Text = "发送",
                Location = new Point(180, 10),
                Width = 65,
                Height = 30,
                Font = Theme.GetFont(10f)
            };
            Theme.StyleButton(btnSend);
            btnSend.Click += BtnSend_Click;

            btnShareClue = new Button
            {
                Text = "分享线索",
                Location = new Point(255, 10),
                Width = 95,
                Height = 30,
                Font = Theme.GetFont(10f)
            };
            Theme.StyleButton(btnShareClue);
            btnShareClue.Click += BtnShareClue_Click;

            btnHangUp = new Button
            {
                Text = "挂断",
                Location = new Point(360, 10),
                Width = 65,
                Height = 30,
                Font = Theme.GetFont(10f)
            };
            Theme.StyleButton(btnHangUp);
            btnHangUp.Click += BtnHangUp_Click;

            pnlBottom.Controls.Add(txtInput);
            pnlBottom.Controls.Add(btnSend);
            pnlBottom.Controls.Add(btnShareClue);
            pnlBottom.Controls.Add(btnHangUp);
            this.Controls.Add(pnlBottom);

            rtbChat = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = Theme.GetFont(10f),
                ForeColor = Theme.TextMain,
                BackColor = Color.FromArgb(28, 38, 42),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                HideSelection = false,
                WordWrap = true
            };
            Theme.ApplyTextureBackground(rtbChat, Theme.StoneTexture);
            this.Controls.Add(rtbChat);
            rtbChat.BringToFront();

            this.AcceptButton = btnSend;
        }

        private void GameClient_OnChatMessageReceived(string sender, string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => GameClient_OnChatMessageReceived(sender, text)));
                return;
            }
            string senderName = sender == "System" ? "[系统]" : $"[{sender}]";
            rtbChat.AppendText($"{senderName} {text}\n");

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
                string msg = txtInput.Text;
                gameClient.SendChatMessage(_playerIdentity, msg);
                txtInput.Clear();
            }
        }

        private void BtnShareClue_Click(object sender, EventArgs e)
        {
            var myClues = gameClient.GetMyClues(_playerIdentity);

            // 筛选出尚未分享的线索（自己发现的且 SharedTo 为空的）
            var shareable = myClues
                .Where(c => string.IsNullOrEmpty(c.SharedTo))
                .Select(c => new Clue { Id = c.Id, Name = c.Name, Description = c.Description })
                .ToList();

            if (shareable.Count == 0)
            {
                PixelMessageBox.Show("还没有线索可分享", "提示");
                return;
            }

            using (var dialog = new FormSelectClue(shareable))
            {
                if (dialog.ShowDialog() == DialogResult.OK && dialog.SelectedClue != null)
                {
                    gameClient.ShareClue(dialog.SelectedClue.Id);
                }
            }
        }

        private void BtnHangUp_Click(object sender, EventArgs e)
        {
            gameClient.OnCallEnded -= GameClient_OnCallEnded;
            gameClient.HangUp(_playerIdentity);
            this.Close();
        }

        private void GameClient_OnCallEnded()
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(GameClient_OnCallEnded));
                return;
            }
            this.Close();
        }

        private void FormChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            gameClient.OnChatMessageReceived -= GameClient_OnChatMessageReceived;
            gameClient.OnCallEnded -= GameClient_OnCallEnded;
        }
    }

}
