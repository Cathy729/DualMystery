using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DualMystery
{
    public partial class FormPlayerB : Form
    {
        private Bitmap charIdle, charStomp;
        private Bitmap npcEdgar, npcMorris;
        private PictureBox canvas;
        private int mapWidth = 1200, mapHeight = 800;
        private PointF playerPos = new PointF(600, 400);
        private float moveSpeed = 4f;
        private bool moveUp, moveDown, moveLeft, moveRight;
        private Timer gameLoop;

        private List<SceneItem> sceneItems;
        private List<NPCData> npcList;
        private int phoneItemIndex = -1;
        private ListBox lstCluesB;
        private ListBox lstTimeline;
        private Button btnAccuse;
        private bool hasAccused = false;

        private bool isCallingOut = false;
        private DateTime callStartTime;
        private Timer tmrAnimate, tmrTimeout, tmrProgress, tmrBubble;
        private Panel pnlIncoming;
        private Label lblIncoming, lblBubble;
        private Button btnAccept, btnDecline;
        private Panel pgbTimeout;
        private FormChat currentChatForm;
        private bool picCharacterAnimFrame = false;

        private string dialogueText = null;
        private bool isLastDialogue = false;
        private Timer tmrDialogue;

        // TCP 网络客户端
        private GameClient gameClient;

        // 缓存 GDI 对象，避免 Paint 中反复创建
        private Font itemFont = new Font("Georgia", 9);
        private Font dialogueFont = new Font("Georgia", 10);
        private SolidBrush bgBrush = new SolidBrush(Color.FromArgb(35, 35, 38));

        // 装饰动画
        private int animFrame = 0;

        public FormPlayerB()
        {
            InitializeComponent();
            GenerateCharacterBitmaps(out charIdle, out charStomp);
            GenerateNPCBitmaps(out npcEdgar, out npcMorris);
            InitializeCustomUI();
            BuildScene();
            BuildNPCs();
            // TCP 客户端初始化
            gameClient = new GameClient();
            gameClient.OnClueDiscovered += GameClient_OnClueDiscovered;
            gameClient.OnSafeUnlocked += GameClient_OnSafeUnlocked;
            gameClient.OnAccusationResult += GameClient_OnAccusationResult;
            gameClient.OnCallRequest += PhoneManager_OnCallRequest;
            gameClient.OnCallEstablished += PhoneManager_OnCallEstablished;
            gameClient.OnCallEnded += PhoneManager_OnCallEnded;
            gameClient.OnRingTimeout += PhoneManager_OnRingTimeout;
            gameClient.OnError += (msg) => Invoke(new Action(() => MessageBox.Show(msg, "网络错误")));
            gameClient.Connect("127.0.0.1", GameServer.PORT, "B");

            this.Load += FormPlayerB_Load;
            this.FormClosing += FormPlayerB_FormClosing;
            this.KeyDown += FormPlayerB_KeyDown;
            this.KeyUp += FormPlayerB_KeyUp;
            this.KeyPreview = true;
            this.LostFocus += (s, e) => { moveUp = moveDown = moveLeft = moveRight = false; };
        }

        // ========== 生成小人 ==========
        private void GenerateCharacterBitmaps(out Bitmap f1, out Bitmap f2)
        {
            Bitmap b1 = new Bitmap(16, 16), b2 = new Bitmap(16, 16);
            Color skin = Color.FromArgb(255, 224, 189), body = Color.DarkRed, pants = Color.FromArgb(40, 40, 40);
            using (Graphics g1 = Graphics.FromImage(b1), g2 = Graphics.FromImage(b2))
            {
                g1.Clear(Color.Transparent); g2.Clear(Color.Transparent);
                using (Brush sb = new SolidBrush(skin), bb = new SolidBrush(body), pb = new SolidBrush(pants))
                {
                    g1.FillRectangle(sb, 4, 2, 8, 6); g2.FillRectangle(sb, 4, 2, 8, 6);
                    g1.FillRectangle(Brushes.White, 6, 3, 2, 2); g1.FillRectangle(Brushes.Black, 7, 3, 1, 2);
                    g1.FillRectangle(Brushes.White, 10, 3, 2, 2); g1.FillRectangle(Brushes.Black, 11, 3, 1, 2);
                    g2.FillRectangle(Brushes.White, 6, 3, 2, 2); g2.FillRectangle(Brushes.Black, 7, 3, 1, 2);
                    g2.FillRectangle(Brushes.White, 10, 3, 2, 2); g2.FillRectangle(Brushes.Black, 11, 3, 1, 2);
                    g1.FillRectangle(bb, 5, 8, 6, 6); g2.FillRectangle(bb, 5, 8, 6, 6);
                    g1.FillRectangle(pb, 6, 14, 3, 2); g1.FillRectangle(pb, 9, 14, 3, 2);
                    g2.FillRectangle(pb, 5, 14, 3, 2); g2.FillRectangle(pb, 10, 14, 3, 2);
                }
            }
            f1 = new Bitmap(48, 48); f2 = new Bitmap(48, 48);
            using (Graphics g1 = Graphics.FromImage(f1), g2 = Graphics.FromImage(f2))
            {
                g1.InterpolationMode = InterpolationMode.NearestNeighbor;
                g2.InterpolationMode = InterpolationMode.NearestNeighbor;
                g1.DrawImage(b1, new Rectangle(0, 0, 48, 48), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
                g2.DrawImage(b2, new Rectangle(0, 0, 48, 48), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
            }
            b1.Dispose(); b2.Dispose();
        }

        private void GenerateNPCBitmaps(out Bitmap edgar, out Bitmap morris)
        {
            edgar = new Bitmap(48, 48); morris = new Bitmap(48, 48);
            Bitmap bE = new Bitmap(16, 16), bM = new Bitmap(16, 16);
            Color skin = Color.FromArgb(255, 224, 189);
            Color suit = Color.FromArgb(50, 50, 80); // 埃德加深蓝西装
            Color butler = Color.FromArgb(80, 80, 90); // 莫里斯管家服
            using (Graphics g1 = Graphics.FromImage(bE), g2 = Graphics.FromImage(bM))
            {
                g1.Clear(Color.Transparent); g2.Clear(Color.Transparent);
                using (Brush sb = new SolidBrush(skin), sub = new SolidBrush(suit), bub = new SolidBrush(butler))
                {
                    // 埃德加
                    g1.FillRectangle(sb, 4, 2, 8, 6);
                    g1.FillRectangle(Brushes.White, 6, 3, 2, 2); g1.FillRectangle(Brushes.Black, 7, 3, 1, 2);
                    g1.FillRectangle(Brushes.White, 10, 3, 2, 2); g1.FillRectangle(Brushes.Black, 11, 3, 1, 2);
                    g1.FillRectangle(sub, 5, 8, 6, 6);
                    g1.FillRectangle(Brushes.Black, 6, 14, 3, 2);
                    g1.FillRectangle(Brushes.Black, 9, 14, 3, 2);
                    // 莫里斯
                    g2.FillRectangle(sb, 4, 2, 8, 6);
                    g2.FillRectangle(Brushes.White, 6, 3, 2, 2); g2.FillRectangle(Brushes.Black, 7, 3, 1, 2);
                    g2.FillRectangle(Brushes.White, 10, 3, 2, 2); g2.FillRectangle(Brushes.Black, 11, 3, 1, 2);
                    g2.FillRectangle(bub, 5, 8, 6, 6);
                    g2.FillRectangle(Brushes.Black, 6, 14, 3, 2);
                    g2.FillRectangle(Brushes.Black, 9, 14, 3, 2);
                }
            }
            using (Graphics g1 = Graphics.FromImage(edgar), g2 = Graphics.FromImage(morris))
            {
                g1.InterpolationMode = InterpolationMode.NearestNeighbor;
                g2.InterpolationMode = InterpolationMode.NearestNeighbor;
                g1.DrawImage(bE, new Rectangle(0, 0, 48, 48), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
                g2.DrawImage(bM, new Rectangle(0, 0, 48, 48), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
            }
            bE.Dispose(); bM.Dispose();
        }

        // ========== UI ==========
        private void InitializeCustomUI()
        {
            this.Text = "走廊";
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ClientSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(800, 0);
            this.DoubleBuffered = true;

            Label lblTitle = new Label
            {
                Text = "🚪 走廊",
                Font = new Font("Georgia", 16f),
                ForeColor = Color.FromArgb(201, 169, 110),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            Label lblHint = new Label
            {
                Text = "方向键移动 | P键调查/对话/拨打电话",
                Font = new Font("Georgia", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 200),
                BackColor = Color.FromArgb(180, 40, 40, 40),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(8, 2, 8, 2)
            };
            lblHint.Location = new Point((this.ClientSize.Width - lblHint.PreferredWidth) / 2, lblTitle.Bottom + 2);
            this.Controls.Add(lblHint);

            Panel rightPanel = new Panel { Width = 200, Dock = DockStyle.Right, BackColor = Color.FromArgb(35, 35, 40) };

            Label lblStory = new Label
            {
                Text = "📜 走廊侦探\n照片、当票、钥匙、日历…\n询问埃德加和莫里斯。",
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.LightGray,
                Font = new Font("Georgia", 9f)
            };
            rightPanel.Controls.Add(lblStory);

            GroupBox gbNotes = new GroupBox { Text = "线索笔记", Height = 150, Dock = DockStyle.Top, ForeColor = Color.White };
            lstCluesB = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.None };
            gbNotes.Controls.Add(lstCluesB);
            rightPanel.Controls.Add(gbNotes);

            GroupBox gbTimeline = new GroupBox { Text = "时间线", Height = 150, Dock = DockStyle.Top, ForeColor = Color.White };
            lstTimeline = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.FromArgb(201, 169, 110), BorderStyle = BorderStyle.None, Font = new Font("Georgia", 9f) };
            gbTimeline.Controls.Add(lstTimeline);
            rightPanel.Controls.Add(gbTimeline);

            btnAccuse = new Button
            {
                Text = "🔍 指认真凶",
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(184, 115, 51),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Georgia", 11f, FontStyle.Bold)
            };
            btnAccuse.Click += BtnAccuse_Click;
            rightPanel.Controls.Add(btnAccuse);

            this.Controls.Add(rightPanel);

            canvas = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 26, 30) };
            canvas.Paint += Canvas_Paint;
            canvas.Click += (s, e2) => { if (!string.IsNullOrEmpty(dialogueText) && !isLastDialogue) { dialogueText = null; isLastDialogue = false; canvas.Invalidate(); } };
            this.Controls.Add(canvas);
            lblHint.Parent = canvas;
            lblHint.BringToFront();

            gameLoop = new Timer { Interval = 16 };
            gameLoop.Tick += GameLoop_Tick;
            gameLoop.Start();

            tmrDialogue = new Timer { Interval = 4000 };
            tmrDialogue.Tick += (s, e) => { dialogueText = null; tmrDialogue.Stop(); canvas.Invalidate(); };

            InitializePhoneSystem();
        }

        // ========== 场景 & NPC ==========
        private void BuildScene()
        {
            sceneItems = new List<SceneItem>
            {
                new SceneItem { Name = "壁挂电话", Rect = new Rectangle(80, 400, 40, 50), ClueId = "phone", IsPhone = true, Icon = PixelIcons.CreatePhone() },
                new SceneItem { Name = "家族照片", Rect = new Rectangle(350, 220, 50, 40), ClueId = "photo", IsPhone = false, Icon = PixelIcons.CreatePhoto() },
                new SceneItem { Name = "当票",     Rect = new Rectangle(700, 500, 30, 20), ClueId = "pawn_ticket", IsPhone = false, Icon = PixelIcons.CreateLetter() },
                new SceneItem { Name = "小钥匙",   Rect = new Rectangle(500, 550, 20, 20), ClueId = "key", IsPhone = false, Icon = PixelIcons.CreateKey() },
                new SceneItem { Name = "旧日历",   Rect = new Rectangle(900, 250, 30, 40), ClueId = "calendar", IsPhone = false, Icon = PixelIcons.CreateCalendar() }
            };
            phoneItemIndex = 0;
        }

        private void BuildNPCs()
        {
            npcList = new List<NPCData>
            {
                new NPCData
                {
                    Name = "埃德加",
                    Rect = new Rectangle(250, 320, 40, 50),
                    Icon = npcEdgar,
                    Dialogues = new List<string>
                    {
                        "埃德加：我和哥哥在晚餐后吵了一架，关于遗产。",
                        "埃德加：但我从没想过杀他！他后来还写信说愿意分我一部分。",
                        "埃德加：那封信应该在保险箱里，可以证明我的清白。"
                    }
                },
                new NPCData
                {
                    Name = "莫里斯",
                    Rect = new Rectangle(700, 360, 40, 50),
                    Icon = npcMorris,
                    Dialogues = new List<string>
                    {
                        "莫里斯：我承认我偷过古董，但那晚我只是想归还。",
                        "莫里斯：钥匙？我弄丢了，可能是有人捡到了。",
                        "莫里斯：格雷医生的药物才更可疑，不是吗？"
                    }
                }
            };
        }

        // ========== 移动 ==========
        private void GameLoop_Tick(object sender, EventArgs e)
        {
            float dx = 0, dy = 0;
            if (moveLeft) dx -= moveSpeed;
            if (moveRight) dx += moveSpeed;
            if (moveUp) dy -= moveSpeed;
            if (moveDown) dy += moveSpeed;
            if (dx != 0 || dy != 0)
            {
                playerPos.X = Math.Max(12, Math.Min(mapWidth - 12, playerPos.X + dx));
                playerPos.Y = Math.Max(12, Math.Min(mapHeight - 12, playerPos.Y + dy));
            }
            animFrame = (animFrame + 1) % 90;
            canvas.Invalidate();
        }

        private void FormPlayerB_KeyDown(object sender, KeyEventArgs e)
        {
            // 对话气泡显示时，按P关闭气泡（含最后一句）
            if (e.KeyCode == Keys.P)
            {
                if (!string.IsNullOrEmpty(dialogueText))
                {
                    // 若为最后一句对话，重置对应NPC的对话进度，下次P从头开始
                    if (isLastDialogue)
                    {
                        foreach (var npc in npcList)
                        {
                            if (npc.DialogueIndex == npc.Dialogues.Count - 1)
                                npc.DialogueIndex = -1;
                        }
                    }
                    dialogueText = null;
                    isLastDialogue = false;
                    canvas.Invalidate();
                    return;
                }
                Interact();
                return;
            }
            switch (e.KeyCode)
            {
                case Keys.Up: moveUp = true; break;
                case Keys.Down: moveDown = true; break;
                case Keys.Left: moveLeft = true; break;
                case Keys.Right: moveRight = true; break;
            }
        }

        private void FormPlayerB_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up: moveUp = false; break;
                case Keys.Down: moveDown = false; break;
                case Keys.Left: moveLeft = false; break;
                case Keys.Right: moveRight = false; break;
            }
        }

        // NPC 交互检测距离（矩形外扩像素）
        private const int NPC_INTERACT_RANGE = 30;
        // 物品交互检测距离
        private const int ITEM_INTERACT_RANGE = 25;

        /// <summary>检查玩家是否在 NPC 交互范围内</summary>
        private bool IsNearNPC(NPCData npc)
        {
            Rectangle r = npc.Rect;
            r.Inflate(NPC_INTERACT_RANGE, NPC_INTERACT_RANGE);
            return r.Contains((int)playerPos.X, (int)playerPos.Y);
        }

        /// <summary>检查玩家是否在物品交互范围内</summary>
        private bool IsNearItem(SceneItem item)
        {
            Rectangle r = item.Rect;
            r.Inflate(ITEM_INTERACT_RANGE, ITEM_INTERACT_RANGE);
            return r.Contains((int)playerPos.X, (int)playerPos.Y);
        }

        private void Interact()
        {
            // 先检查NPC（矩形向外扩展 NPC_INTERACT_RANGE px）
            foreach (var npc in npcList)
            {
                if (IsNearNPC(npc))
                {
                    ShowNPCDialogue(npc);
                    return;
                }
            }
            // 再检查物品（矩形向外扩展 ITEM_INTERACT_RANGE px）
            foreach (var item in sceneItems)
            {
                if (IsNearItem(item))
                {
                    HandleItemInteraction(item);
                    break;
                }
            }
            // 若走到这里说明范围内无可交互对象——播放轻微提示音
            System.Media.SystemSounds.Beep.Play();
        }

        private void ShowNPCDialogue(NPCData npc)
        {
            npc.DialogueIndex = (npc.DialogueIndex + 1) % npc.Dialogues.Count;
            dialogueText = npc.Dialogues[npc.DialogueIndex];
            isLastDialogue = (npc.DialogueIndex == npc.Dialogues.Count - 1);
            // 手动关闭，不再自动消失
            canvas.Invalidate();
            if (npc.Name.Contains("埃德加") && npc.DialogueIndex == 1)
                AddTimeline("埃德加声称死者曾写信愿分遗产");
            if (npc.Name.Contains("莫里斯") && npc.DialogueIndex == 0)
                AddTimeline("莫里斯承认偷窃，但否认杀人");
        }

        private void AddTimeline(string entry)
        {
            if (!lstTimeline.Items.Contains(entry))
                lstTimeline.Items.Add(entry);
        }

        /// <summary>从本地缓存或 GameManager 静态列表查找线索（防止 StateSync 未到达时静默失败）</summary>
        private Clue FindClueData(string clueId)
        {
            var cached = gameClient.ClueCache.FirstOrDefault(c => c.Id == clueId);
            if (cached != null)
                return new Clue { Id = cached.Id, Name = cached.Name, Description = cached.Description, IsDiscovered = cached.IsDiscovered, DiscoveredBy = cached.DiscoveredBy };
            return GameManager.AllClues.FirstOrDefault(c => c.Id == clueId);
        }

        private void HandleItemInteraction(SceneItem item)
        {
            if (item.IsPhone)
            {
                gameClient.RequestCall("B");
                isCallingOut = true;
                tmrAnimate.Start();
                tmrTimeout.Start();
                callStartTime = DateTime.Now;
            }
            else
            {
                gameClient.DiscoverClue(item.ClueId, "B");
                var clue = FindClueData(item.ClueId);
                if (clue != null)
                {
                    if (!lstCluesB.Items.Contains(clue.Name)) lstCluesB.Items.Add(clue.Name);
                    MessageBox.Show(clue.Description, clue.Name);
                }
                else System.Media.SystemSounds.Beep.Play();
            }
            canvas.Invalidate();
        }

        // 场景绘制已迁移至 FormPlayerB_Paint.cs

        private void BtnAccuse_Click(object sender, EventArgs e)
        {
            if (hasAccused) { MessageBox.Show("你已经指认过了，请等待对方。"); return; }
            string[] suspects = { "埃德加", "莫里斯", "贝蒂", "格雷医生" };
            using (Form f = new Form())
            {
                f.Text = "指认真凶"; f.Width = 300; f.Height = 200; f.StartPosition = FormStartPosition.CenterParent;
                ComboBox cmb = new ComboBox() { Left = 30, Top = 30, Width = 220, DataSource = suspects, DropDownStyle = ComboBoxStyle.DropDownList };
                Button btn = new Button() { Text = "确认指认", Left = 80, Top = 80, DialogResult = DialogResult.OK };
                f.Controls.Add(cmb); f.Controls.Add(btn);
                if (f.ShowDialog() == DialogResult.OK)
                {
                    string accused = cmb.SelectedItem.ToString();
                    hasAccused = true;
                    btnAccuse.Enabled = false;
                    gameClient.SubmitAccusation("B", accused);
                }
            }
        }

        // ========== 电话 ==========
        private void InitializePhoneSystem()
        {
            tmrAnimate = new Timer { Interval = 300 }; tmrAnimate.Tick += (s, e) => { picCharacterAnimFrame = !picCharacterAnimFrame; canvas.Invalidate(); System.Media.SystemSounds.Beep.Play(); };
            tmrTimeout = new Timer { Interval = 3000 }; tmrTimeout.Tick += TmrTimeout_Tick;
            tmrProgress = new Timer { Interval = 30 }; tmrProgress.Tick += TmrProgress_Tick;
            tmrBubble = new Timer { Interval = 1500 }; tmrBubble.Tick += (s, e) => { tmrBubble.Stop(); lblBubble.Visible = false; };

            pnlIncoming = new Panel { Size = new Size(160, 80), BackColor = Color.AntiqueWhite, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            lblIncoming = new Label { Text = "有来电", Location = new Point(5, 5), AutoSize = true, ForeColor = Color.Black };
            btnAccept = new Button { Text = "接听", Size = new Size(60, 30), Location = new Point(10, 30) };
            btnDecline = new Button { Text = "拒绝", Size = new Size(60, 30), Location = new Point(80, 30) };
            pgbTimeout = new Panel { Size = new Size(160, 5), Location = new Point(0, 75), BackColor = Color.Brown };
            btnAccept.Click += (s, e) => { gameClient.AcceptCall("B"); };
            btnDecline.Click += (s, e) => { gameClient.DeclineCall("B"); pnlIncoming.Visible = false; StopRingingUI(); };
            pnlIncoming.Controls.Add(lblIncoming); pnlIncoming.Controls.Add(btnAccept); pnlIncoming.Controls.Add(btnDecline); pnlIncoming.Controls.Add(pgbTimeout);

            this.Controls.Add(pnlIncoming);
            pnlIncoming.Location = new Point((this.ClientSize.Width - 160) / 2, this.ClientSize.Height / 2 - 40);
            pnlIncoming.BringToFront();

            lblBubble = new Label { Text = "无人接听...", ForeColor = Color.White, BackColor = Color.Black, Visible = false, AutoSize = true };
            this.Controls.Add(lblBubble);
            lblBubble.Location = new Point((this.ClientSize.Width - lblBubble.Width) / 2, pnlIncoming.Top - 30);
            lblBubble.BringToFront();

            this.Resize += (s, e) => { if (pnlIncoming.Visible) { pnlIncoming.Location = new Point((this.ClientSize.Width - 160) / 2, this.ClientSize.Height / 2 - 40); lblBubble.Location = new Point((this.ClientSize.Width - lblBubble.Width) / 2, pnlIncoming.Top - 30); } };

            // 电话事件订阅已迁移至 gameClient（构造函数中绑定）
        }

        private void StopRingingUI() { tmrAnimate.Stop(); tmrTimeout.Stop(); tmrProgress.Stop(); isCallingOut = false; pnlIncoming.Visible = false; lblBubble.Visible = false; }
        private void TmrTimeout_Tick(object sender, EventArgs e) { tmrTimeout.Stop(); if (isCallingOut) gameClient.HangUp("B"); else gameClient.DeclineCall("B"); StopRingingUI(); }
        private void TmrProgress_Tick(object sender, EventArgs e) { float r = 1f - (float)(DateTime.Now - callStartTime).TotalSeconds / 3f; if (r < 0) r = 0; pgbTimeout.Width = (int)(160 * r); }
        private void PhoneManager_OnCallRequest(string caller, string callee) { if (InvokeRequired) { Invoke(new Action<string, string>(PhoneManager_OnCallRequest), caller, callee); return; } if (callee == "B") { pnlIncoming.Visible = true; callStartTime = DateTime.Now; tmrProgress.Start(); tmrTimeout.Start(); } }
        private void PhoneManager_OnCallEstablished() { if (InvokeRequired) { Invoke(new Action(PhoneManager_OnCallEstablished)); return; } StopRingingUI(); currentChatForm = new FormChat("B", gameClient); currentChatForm.FormClosed += (s, e) => gameClient.HangUp("B"); currentChatForm.Show(); }
        private void PhoneManager_OnCallEnded() { if (InvokeRequired) { Invoke(new Action(PhoneManager_OnCallEnded)); return; } StopRingingUI(); if (currentChatForm != null && !currentChatForm.IsDisposed) { currentChatForm.Close(); currentChatForm = null; } }
        private void PhoneManager_OnRingTimeout(string caller) { if (InvokeRequired) { Invoke(new Action<string>(PhoneManager_OnRingTimeout), caller); return; } StopRingingUI(); if (caller == "B") { lblBubble.Visible = true; tmrBubble.Start(); } }

        // ========== 网络事件处理 ==========
        private void FormPlayerB_Load(object sender, EventArgs e)
        {
            foreach (var c in gameClient.ClueCache)
                if (c.IsDiscovered && c.DiscoveredBy == "B" && !lstCluesB.Items.Contains(c.Name))
                    lstCluesB.Items.Add(c.Name);
        }
        private void GameClient_OnClueDiscovered(string clueId, string player)
        {
            if (InvokeRequired) { Invoke(new Action(() => GameClient_OnClueDiscovered(clueId, player))); return; }
            var cl = gameClient.ClueCache.FirstOrDefault(c => c.Id == clueId);
            if (cl != null && cl.DiscoveredBy == "B" && !lstCluesB.Items.Contains(cl.Name))
                lstCluesB.Items.Add(cl.Name);
        }
        private void GameClient_OnSafeUnlocked()
        {
            if (InvokeRequired) { Invoke(new Action(GameClient_OnSafeUnlocked)); return; }
            MessageBox.Show("书房传来了金属响声，保险箱打开了！遗嘱和举报信已自动记录。", "保险箱已开");
        }
        private void GameClient_OnAccusationResult(string accA, string accB, bool bothCorrect)
        {
            if (InvokeRequired) { Invoke(new Action(() => GameClient_OnAccusationResult(accA, accB, bothCorrect))); return; }

            if (bothCorrect)
            {
                if (!GameManager.ResultMessageShown)
                {
                    GameManager.ResultMessageShown = true;
                    var ending = new FormEnding();
                    ending.FormClosed += (s2, e2) =>
                    {
                        foreach (Form f in Application.OpenForms)
                            if (f is FormPlayerA || f is FormPlayerB)
                                f.Close();
                    };
                    ending.Show();
                }
            }
            else
            {
                if (!GameManager.ResultMessageShown)
                {
                    GameManager.ResultMessageShown = true;
                    MessageBox.Show(
                        $"指认结果不一致！\n侦探A指认：{accA}\n侦探B指认：{accB}\n\n请通过电话沟通后重新指认。",
                        "指认失败");
                }
                hasAccused = false;
                btnAccuse.Enabled = true;
                GameManager.ResetAccusation();
            }
        }
        private void FormPlayerB_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (gameClient != null)
            {
                gameClient.OnClueDiscovered -= GameClient_OnClueDiscovered;
                gameClient.OnSafeUnlocked -= GameClient_OnSafeUnlocked;
                gameClient.OnAccusationResult -= GameClient_OnAccusationResult;
                gameClient.OnCallRequest -= PhoneManager_OnCallRequest;
                gameClient.OnCallEstablished -= PhoneManager_OnCallEstablished;
                gameClient.OnCallEnded -= PhoneManager_OnCallEnded;
                gameClient.OnRingTimeout -= PhoneManager_OnRingTimeout;
                gameClient.Disconnect();
            }
            gameLoop.Stop();
            tmrAnimate?.Stop();
            tmrTimeout?.Stop();
            tmrProgress?.Stop();
            tmrBubble?.Stop();
            tmrDialogue?.Stop();
            gameLoop?.Dispose();
            tmrAnimate?.Dispose();
            tmrTimeout?.Dispose();
            tmrProgress?.Dispose();
            tmrBubble?.Dispose();
            tmrDialogue?.Dispose();
            itemFont?.Dispose();
            dialogueFont?.Dispose();
            bgBrush?.Dispose();
        }

        private class SceneItem
        {
            public string Name;
            public Rectangle Rect;
            public string ClueId;
            public bool IsPhone;
            public Bitmap Icon;
        }
        private class NPCData
        {
            public string Name;
            public Rectangle Rect;
            public Bitmap Icon;
            public List<string> Dialogues;
            public int DialogueIndex = -1;
        }
    }
}