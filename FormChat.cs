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
        private GameClient gameClient;
        private RichTextBox rtbChat;
        private TextBox txtInput;
        private Button btnSend;
        private Button btnShareClue;
        private Button btnHangUp;

        // 数据包可视化
        private Panel pnlPacket;
        private Timer pktAnimTimer;
        private List<PacketData> activePackets = new List<PacketData>();
        private int pktAnimTick = 0;

        private class PacketData
        {
            public int Seq { get; set; }
            public string Data { get; set; }
            public string Checksum { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public bool IsAck { get; set; }
            public bool Arrived { get; set; }
            public Color Color { get; set; }
        }

        public FormChat(string playerIdentity, GameClient client)
        {
            InitializeComponent();
            _playerIdentity = playerIdentity;
            gameClient = client;
            InitializeCustomUI();

            LoadHistory();
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
            this.BackColor = Color.FromArgb(253, 245, 230); // 旧纸色
            this.ClientSize = new Size(500, 480);            // 加大高度容纳可视化面板
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
            lblTitle.Height = 36;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitle);

            // ---- 数据包可视化面板 ----
            pnlPacket = new Panel();
            pnlPacket.Dock = DockStyle.Top;
            pnlPacket.Height = 70;
            pnlPacket.BackColor = Color.FromArgb(30, 30, 36);
            pnlPacket.Paint += PnlPacket_Paint;
            this.Controls.Add(pnlPacket);

            pktAnimTimer = new Timer { Interval = 50 };
            pktAnimTimer.Tick += (s, e) => { pktAnimTick++; pnlPacket.Invalidate(); };

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

        // ==================== 数据包可视化绘制 ====================
        private void PnlPacket_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = pnlPacket.Width, h = pnlPacket.Height;
            int midY = h / 2;

            // 背景 —— 模拟"传输信道"
            g.FillRectangle(new SolidBrush(Color.FromArgb(20, 20, 26)), 0, 0, w, h);

            // 发送端 / 接收端 标签
            using (Font labelFont = new Font("Courier New", 7f))
            {
                g.DrawString("发送端", labelFont, Brushes.Gray, 5, 2);
                g.DrawString("接收端", labelFont, Brushes.Gray, w - 45, 2);
            }

            // 中心线（信道）
            g.DrawLine(new Pen(Color.FromArgb(60, 60, 70)), 0, midY, w, midY);

            // 如果没有数据包，显示空闲状态
            if (activePackets.Count == 0)
            {
                using (Font idleFont = new Font("Courier New", 8f, FontStyle.Italic))
                {
                    string status = pktAnimTimer.Enabled ? "⏳ 等待确认..." : "📡 网络空闲";
                    SizeF sz = g.MeasureString(status, idleFont);
                    g.DrawString(status, idleFont, Brushes.DimGray, w / 2 - sz.Width / 2, midY - sz.Height / 2);
                }
                return;
            }

            // 绘制数据包和ACK
            foreach (var pkt in activePackets)
            {
                int pktW = pkt.IsAck ? 36 : 60;
                int pktH = 22;
                int x = (int)pkt.X;
                int y = pkt.IsAck ? midY - pktH - 4 : midY + 4;

                // 超过边界的不绘制
                if (x + pktW < -20 || x > w + 20) continue;

                // 矩形（数据包/ACK帧）
                using (Brush pktBrush = new SolidBrush(pkt.Color))
                    g.FillRectangle(pktBrush, x, y, pktW, pktH);
                g.DrawRectangle(new Pen(Color.FromArgb(100, 100, 120)), x, y, pktW, pktH);

                // 包序号和数据
                using (Font pktFont = new Font("Courier New", 6f, FontStyle.Bold))
                {
                    if (pkt.IsAck)
                    {
                        g.DrawString($"ACK{pkt.Seq}", pktFont, Brushes.White, x + 4, y + 5);
                    }
                    else
                    {
                        g.DrawString($"#{pkt.Seq}", pktFont, Brushes.White, x + 2, y + 2);
                        g.DrawString($"\"{Truncate(pkt.Data, 3)}\"", pktFont, Brushes.LightGray, x + 2, y + 11);
                        g.DrawString($"CHK:{pkt.Checksum}", new Font("Courier New", 5f), Brushes.YellowGreen, x + 2, y - 6);
                    }

                    // 到达标记
                    if (pkt.Arrived)
                    {
                        g.DrawString("✓", new Font("Courier New", 9f, FontStyle.Bold), Brushes.Lime, x + pktW + 2, y + 4);
                    }
                }
            }
        }

        private string Truncate(string s, int maxLen)
            => s.Length <= maxLen ? s : s.Substring(0, maxLen);

        // ==================== 数据包发送动画 ====================
        private void StartPacketTransmission(string message)
        {
            // 将消息拆成包（每包3-4个字符）
            int chunkSize = 3;
            var packets = new List<PacketData>();
            int seq = 0;
            for (int i = 0; i < message.Length; i += chunkSize)
            {
                seq++;
                string chunk = message.Substring(i, Math.Min(chunkSize, message.Length - i));
                // 简单校验和：字符ASCII和 mod 256
                int checksum = 0;
                foreach (char c in chunk) checksum += (int)c;
                packets.Add(new PacketData
                {
                    Seq = seq,
                    Data = chunk,
                    Checksum = (checksum % 256).ToString("X2"),
                    X = -80,
                    Y = 0,
                    Color = Color.FromArgb(40 + (seq * 30) % 180, 80, 160),
                    IsAck = false
                });
            }

            activePackets = packets;
            pktAnimTick = 0;
            pktAnimTimer.Start();

            int w = pnlPacket.Width;
            int totalPackets = packets.Count;
            int ticksPerPkt = 20; // 每个包飞行时间
            int ackTicks = 12;    // ACK返回时间
            int gapTicks = 8;     // 包间间隔

            // 用新线程管理动画状态（不阻塞UI）
            var thread = new System.Threading.Thread(() =>
            {
                int currentPkt = 0;
                int tick = 0;
                bool sending = true;

                while (currentPkt < totalPackets && !this.IsDisposed)
                {
                    var pkt = packets[currentPkt];

                    if (sending)
                    {
                        // 数据包从左向右飞行
                        float progress = Math.Min(1f, (float)tick / ticksPerPkt);
                        pkt.X = -80 + (w + 160) * progress * 0.7f; // 飞到70%处

                        if (tick >= ticksPerPkt)
                        {
                            pkt.Arrived = true;
                            sending = false;
                            tick = 0;
                        }
                    }
                    else
                    {
                        // ACK从右向左返回
                        float progress = Math.Min(1f, (float)tick / ackTicks);

                        // 创建或更新ACK包
                        if (activePackets.Count <= totalPackets + currentPkt || currentPkt + totalPackets >= activePackets.Count)
                        {
                            // ACK包
                            if (tick == 0)
                            {
                                var ack = new PacketData
                                {
                                    Seq = pkt.Seq,
                                    X = w * 0.7f,
                                    Y = 0,
                                    IsAck = true,
                                    Arrived = false,
                                    Color = Color.FromArgb(80, 160, 60)
                                };
                                this.Invoke(new Action(() => { if (!this.IsDisposed) activePackets.Add(ack); }));
                            }
                        }

                        // 更新ACK位置
                        this.Invoke(new Action(() =>
                        {
                            if (this.IsDisposed) return;
                            foreach (var a in activePackets)
                            {
                                if (a.IsAck && a.Seq == pkt.Seq && !a.Arrived)
                                {
                                    a.X = w * 0.7f - (w * 0.7f + 36) * progress;
                                    if (tick >= ackTicks) a.Arrived = true;
                                }
                            }
                        }));

                        if (tick >= ackTicks)
                        {
                            sending = true;
                            currentPkt++;
                            tick = 0;
                            // 包间间隔
                            var temp = gapTicks;
                            while (temp-- > 0) { System.Threading.Thread.Sleep(50); }
                            continue;
                        }
                    }

                    tick++;
                    System.Threading.Thread.Sleep(50);
                }

                // 传输完成，清除包（保留0.5秒后清空）
                System.Threading.Thread.Sleep(500);
                this.Invoke(new Action(() =>
                {
                    if (this.IsDisposed) return;
                    activePackets.Clear();
                    pktAnimTimer.Stop();
                    pnlPacket.Invalidate();
                }));
            }) { IsBackground = true };
            thread.Start();
        }

        private void LoadHistory()
        {
            // 聊天历史在服务器端维护，客户端不保存本地历史
        }

        private void GameClient_OnChatMessageReceived(string sender, string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => GameClient_OnChatMessageReceived(sender, text)));
                return;
            }
            // 收到消息时显示接收方数据包可视化
            if (sender != _playerIdentity)
            {
                ShowReceivingAnimation(sender, text);
            }
            string senderName = sender == "System" ? "[系统]" : $"[{sender}]";
            rtbChat.AppendText($"{senderName} {text}\n");

            rtbChat.SelectionStart = rtbChat.Text.Length;
            rtbChat.ScrollToCaret();
            rtbChat.PerformLayout();
            rtbChat.Invalidate();
            rtbChat.Update();
        }

        private void ShowReceivingAnimation(string sender, string text)
        {
            // 收到消息时的"接收中"可视化
            activePackets.Clear();
            pktAnimTimer.Start();

            int chunkSize = 4;
            var packets = new List<PacketData>();
            int seq = 0;
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                seq++;
                string chunk = text.Substring(i, Math.Min(chunkSize, text.Length - i));
                int checksum = 0;
                foreach (char c in chunk) checksum += (int)c;
                packets.Add(new PacketData
                {
                    Seq = seq, Data = chunk, Checksum = (checksum % 256).ToString("X2"),
                    X = 0, Y = 0, Color = Color.FromArgb(120, 80, 60 + seq * 20), IsAck = false
                });
            }

            // 用异步方式播放接收动画
            var thread = new System.Threading.Thread(() =>
            {
                int w = pnlPacket.Width;
                for (int i = 0; i < packets.Count; i++)
                {
                    float x = w * 0.7f; // 从右侧（信道中间偏右）出现
                    var pkt = packets[i];
                    for (int t = 0; t < 15 && !this.IsDisposed; t++)
                    {
                        float progress = (float)t / 15f;
                        pkt.X = w * 0.7f + (w * 0.25f) * progress;
                        if (progress > 0.8f) pkt.Arrived = true;
                        this.Invoke(new Action(() => { if (!this.IsDisposed) { activePackets = new List<PacketData>(packets.Take(i + 1)); pnlPacket.Invalidate(); } }));
                        System.Threading.Thread.Sleep(40);
                    }
                    // ACK
                    for (int t = 0; t < 10 && !this.IsDisposed; t++)
                    {
                        float progress = (float)t / 10f;
                        pkt.X = w - (w * 0.3f) * progress;
                        this.Invoke(new Action(() => pnlPacket.Invalidate()));
                        System.Threading.Thread.Sleep(40);
                    }
                    System.Threading.Thread.Sleep(100);
                }
                System.Threading.Thread.Sleep(400);
                this.Invoke(new Action(() => { if (!this.IsDisposed) { activePackets.Clear(); pktAnimTimer.Stop(); pnlPacket.Invalidate(); } }));
            }) { IsBackground = true };
            thread.Start();
        }

        private void AppendMessage(ChatMessage msg)
        {
            string senderName = msg.Sender == "System" ? "[系统]" : $"[{msg.Sender}]";
            rtbChat.AppendText($"{senderName} {msg.Text}\n");

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
                // 发送端数据包可视化
                StartPacketTransmission(msg);
                // 实际通过网络发送
                gameClient.SendChatMessage(_playerIdentity, msg);
                txtInput.Clear();
            }
        }

        private void BtnShareClue_Click(object sender, EventArgs e)
        {
            var myClues = gameClient.GetMyClues(_playerIdentity);

            if (myClues.Count == 0)
            {
                MessageBox.Show("还没有线索可分享", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new FormSelectClue(myClues.Select(c => new Clue { Id = c.Id, Name = c.Name, Description = c.Description }).ToList()))
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
            pktAnimTimer?.Stop();
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