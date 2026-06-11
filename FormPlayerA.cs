using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DualMystery
{
    public partial class FormPlayerA : Form
    {
        // 像素小人素材
        private Bitmap charIdle, charStomp;
        // NPC 小人素材
        private Bitmap npcBetty, npcGrey;

        // 场景绘制
        private PictureBox canvas;
        private int mapWidth = 1200, mapHeight = 800;
        private PointF playerPos = new PointF(600, 400);
        private float moveSpeed = 4f;
        private bool moveUp, moveDown, moveLeft, moveRight;
        private Timer gameLoop;

        // 场景物品
        private List<SceneItem> sceneItems;
        private List<NPCData> npcList;
        private int phoneItemIndex = -1;

        // 线索笔记本 + 时间线 + 指认
        private ListBox lstCluesA;
        private ListBox lstTimeline;
        private Button btnAccuse;
        private bool hasAccused = false;

        // 电话系统状态
        private bool isCallingOut = false;
        private DateTime callStartTime;
        private Timer tmrAnimate, tmrTimeout, tmrProgress, tmrBubble;
        private Panel pnlIncoming;
        private Label lblIncoming, lblBubble;
        private Button btnAccept, btnDecline;
        private Panel pgbTimeout;
        private FormChat currentChatForm;
        private bool picCharacterAnimFrame = false;

        // 对话气泡
        private string dialogueText = null;
        private bool isLastDialogue = false;
        private Timer tmrDialogue;

        // TCP 网络客户端
        private GameClient gameClient;

        // 缓存 GDI 对象，避免 Paint 中反复创建
        private Font itemFont = Theme.GetFont(9f);
        private Font dialogueFont = Theme.GetFont(10f);
        private SolidBrush bgBrush = new SolidBrush(Theme.BgDark);
        // 场景绘制缓存（每帧高频使用）
        private SolidBrush bgFillBrush = new SolidBrush(Color.FromArgb(45, 40, 36));
        private Pen floorLinePenA = new Pen(Color.FromArgb(80, 60, 40), 2);
        private SolidBrush hintBgBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0));

        // 装饰动画
        private int animFrame = 0;
        private Bitmap[] fireplaceFrames;
        private Bitmap[] chandelierFrames;

        // 地板纹理
        private Bitmap floorTileA;
        private Pen wallStripePen = new Pen(Color.FromArgb(30, 0x3A, 0x3A, 0x3A), 1);

        // 鼠标悬停高亮
        private int hoveredItemIndex = -1;

        // 点击反馈动画
        private int feedbackItemIndex = -1;
        private DateTime feedbackStartTime;
        private Timer tmrFeedback;

        public FormPlayerA()
        {
            InitializeComponent();
            GenerateCharacterBitmaps(out charIdle, out charStomp);
            GenerateNPCBitmaps(out npcBetty, out npcGrey);

            // 点击反馈计时器（必须在 InitializeCustomUI 之前初始化，避免 Paint 事件触发时 tmrFeedback 为 null）
            tmrFeedback = new Timer { Interval = 500 };
            tmrFeedback.Tick += (s, e) => { tmrFeedback.Stop(); feedbackItemIndex = -1; canvas.Invalidate(); };

            InitializeCustomUI();
            BuildScene();
            BuildNPCs();
            fireplaceFrames = new Bitmap[3];
            var fpSrc = PixelIcons.CreateFireplaceFrames();
            for (int i = 0; i < 3; i++) fireplaceFrames[i] = new Bitmap(fpSrc[i], 64, 64);
            chandelierFrames = new Bitmap[3];
            var chSrc = PixelIcons.CreateChandelierFrames();
            for (int i = 0; i < 3; i++) chandelierFrames[i] = new Bitmap(chSrc[i], 48, 48);
            // 地板棋盘纹理（64×64 贴图，4px 格子 → 16×16 格）
            floorTileA = Theme.CreateCheckerTile(64, 4,
                Color.FromArgb(0x3E, 0x2B, 0x1F), Color.FromArgb(0x4A, 0x35, 0x25));
            // TCP 客户端初始化
            gameClient = new GameClient();
            gameClient.OnClueDiscovered += GameClient_OnClueDiscovered;
            gameClient.OnClueShared += GameClient_OnClueShared;
            gameClient.OnSafeUnlocked += GameClient_OnSafeUnlocked;
            gameClient.OnAccusationResult += GameClient_OnAccusationResult;
            gameClient.OnCallRequest += PhoneManager_OnCallRequest;
            gameClient.OnCallEstablished += PhoneManager_OnCallEstablished;
            gameClient.OnCallEnded += PhoneManager_OnCallEnded;
            gameClient.OnRingTimeout += PhoneManager_OnRingTimeout;
            gameClient.OnError += (msg) => Invoke(new Action(() => PixelMessageBox.Show(msg, "网络错误")));
            gameClient.Connect("127.0.0.1", GameServer.PORT, "A");

            this.Load += FormPlayerA_Load;
            this.FormClosing += FormPlayerA_FormClosing;
            this.KeyDown += FormPlayerA_KeyDown;
            this.KeyUp += FormPlayerA_KeyUp;
            this.KeyPreview = true;

            // 失去焦点时停止移动
            this.LostFocus += (s, e) =>
            {
                moveUp = false; moveDown = false;
                moveLeft = false; moveRight = false;
            };
        }

        // ==================== 生成像素小人 ====================
        private void GenerateCharacterBitmaps(out Bitmap frame1, out Bitmap frame2)
        {
            frame1 = PixelCharacters.CreatePlayerA_Idle();
            frame2 = PixelCharacters.CreatePlayerA_Stomp();
        }

        private void GenerateNPCBitmaps(out Bitmap betty, out Bitmap grey)
        {
            betty = PixelCharacters.CreateBetty();
            grey  = PixelCharacters.CreateGrey();
        }

        // ==================== UI 初始化 ====================
        private void InitializeCustomUI()
        {
            this.Text = "书房";
            this.BackColor = Theme.BgMain;
            this.ClientSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
            this.DoubleBuffered = true;

            Label lblTitle = new Label
            {
                Text = Theme.DecorateTitle("📖 书  房"),
                Font = Theme.GetFont(16f),
                ForeColor = Theme.Accent,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            // 标题分隔线
            this.Controls.Add(Theme.CreateTitleSeparator());

            Label lblHint = new Label
            {
                Text = "WASD移动 | E键调查/对话/拨打电话",
                Font = Theme.GetFont(9f),
                ForeColor = Theme.TextMain,
                BackColor = Color.FromArgb(180, Theme.BgPanel),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(8, 2, 8, 2)
            };
            lblHint.Location = new Point((this.ClientSize.Width - lblHint.PreferredWidth) / 2, lblTitle.Bottom + 4);
            this.Controls.Add(lblHint);

            // 右侧面板（像素纹理 + 双线边框）
            Panel rightPanel = new Panel
            {
                Width = 200,
                Dock = DockStyle.Right,
                BackColor = Theme.BgPanel
            };
            Theme.ApplyTextureBackground(rightPanel, Theme.WoodTexture);
            Theme.StylePanelWithBorder(rightPanel);

            // 剧情简介
            Label lblStory = new Label
            {
                Text = "📖 书房侦探\n尸体、刀、书桌、保险箱…\n询问贝蒂和格雷医生。",
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgDark,
                ForeColor = Theme.TextMain,
                Font = Theme.GetFont(9f)
            };
            rightPanel.Controls.Add(lblStory);

            // 线索笔记
            GroupBox gbNotes = new GroupBox
            {
                Text = "线索笔记",
                Height = 150,
                Dock = DockStyle.Top,
                ForeColor = Theme.Accent,
                BackColor = Theme.BgPanel
            };
            Theme.StyleGroupBoxPixel(gbNotes);
            lstCluesA = new ListBox { Dock = DockStyle.Fill };
            Theme.StyleListBox(lstCluesA);
            Theme.ApplyTextureBackground(lstCluesA, Theme.WoodTexture);
            lstCluesA.MouseClick += (s, ev) => ShowItemDetail(lstCluesA);
            gbNotes.Controls.Add(lstCluesA);
            rightPanel.Controls.Add(gbNotes);

            // 时间线
            GroupBox gbTimeline = new GroupBox
            {
                Text = "时间线",
                Height = 150,
                Dock = DockStyle.Top,
                ForeColor = Theme.Accent,
                BackColor = Theme.BgPanel
            };
            Theme.StyleGroupBoxPixel(gbTimeline);
            lstTimeline = new ListBox { Dock = DockStyle.Fill };
            Theme.StyleListBox(lstTimeline);
            Theme.ApplyTextureBackground(lstTimeline, Theme.WoodTexture);
            lstTimeline.MouseClick += (s, ev) => ShowItemDetail(lstTimeline);
            gbTimeline.Controls.Add(lstTimeline);
            rightPanel.Controls.Add(gbTimeline);

            // 指认按钮
            btnAccuse = new Button
            {
                Text = "🔍 指认真凶",
                Dock = DockStyle.Top,
                Height = 40,
                Font = Theme.GetFont(11f)
            };
            Theme.StyleButton(btnAccuse);
            btnAccuse.Click += BtnAccuse_Click;
            rightPanel.Controls.Add(btnAccuse);

            this.Controls.Add(rightPanel);

            canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgDark
            };
            canvas.Paint += Canvas_Paint;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseLeave += (s, e2) => { hoveredItemIndex = -1; canvas.Invalidate(); };
            canvas.Click += (s, e2) => { if (!string.IsNullOrEmpty(dialogueText) && !isLastDialogue) { dialogueText = null; isLastDialogue = false; canvas.Invalidate(); } };
            this.Controls.Add(canvas);
            lblHint.Parent = canvas;
            lblHint.BringToFront();

            gameLoop = new Timer { Interval = 16 };
            gameLoop.Tick += GameLoop_Tick;
            gameLoop.Start();

            // 对话气泡定时器
            tmrDialogue = new Timer { Interval = 4000 };
            tmrDialogue.Tick += (s, e) => { dialogueText = null; tmrDialogue.Stop(); canvas.Invalidate(); };

            InitializePhoneSystem();
        }

        // ==================== 场景物品 ====================
        private void BuildScene()
        {
            sceneItems = new List<SceneItem>
            {
                new SceneItem { Name = "壁挂电话", Rect = new Rectangle(80, 380, 40, 50), ClueId = "phone", Type = ItemType.Phone, Icon = PixelIcons.CreatePhone() },
                new SceneItem { Name = "凶器刀", Rect = new Rectangle(360, 420, 30, 20), ClueId = "knife", Type = ItemType.Normal, Icon = PixelIcons.CreateKnife() },
                new SceneItem { Name = "烧毁的信", Rect = new Rectangle(780, 450, 30, 20), ClueId = "burnt_letter", Type = ItemType.Normal, Icon = PixelIcons.CreateLetter() },
                new SceneItem { Name = "圣经暗格纸条", Rect = new Rectangle(520, 480, 30, 30), ClueId = "bible_note", Type = ItemType.Normal, Icon = PixelIcons.CreateBook() },
                new SceneItem { Name = "带血手帕", Rect = new Rectangle(620, 430, 30, 30), ClueId = "handkerchief", Type = ItemType.Normal, Icon = PixelIcons.CreateHandkerchief() },
                new SceneItem { Name = "保险箱", Rect = new Rectangle(950, 500, 50, 60), ClueId = "safe", Type = ItemType.Safe, Icon = PixelIcons.CreateSafe() },
                new SceneItem { Name = "书桌", Rect = new Rectangle(450, 420, 100, 60), ClueId = "desk", Type = ItemType.Desk, Icon = PixelIcons.CreateDesk() }
            };
            phoneItemIndex = 0;
        }

        private void BuildNPCs()
        {
            npcList = new List<NPCData>
            {
                new NPCData
                {
                    Name = "女仆贝蒂",
                    Rect = new Rectangle(250, 300, 40, 50),
                    Icon = npcBetty,
                    Dialogues = new List<string>
                    {
                        "贝蒂：昨晚21:30我在书房打扫，老爷因为花瓶的事训斥了我…",
                        "贝蒂：但我22:00就回仆人房了，格雷医生可以作证！",
                        "贝蒂：我离开时看到管家莫里斯在走廊里张望，神色很慌张。"
                    }
                },
                new NPCData
                {
                    Name = "格雷医生",
                    Rect = new Rectangle(850, 350, 40, 50),
                    Icon = npcGrey,
                    Dialogues = new List<string>
                    {
                        "格雷：22:00我给老爷服了安眠药，剂量是安全的。",
                        "格雷：老爷最近失眠严重，但他坚决要求加大剂量…",
                        "格雷：不过我以职业操守保证，那剂量不足以致命，死因是刀伤。"
                    }
                }
            };
        }

        // ==================== 鼠标悬停检测 ====================
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            int vw = canvas.Width, vh = canvas.Height;
            float ox = playerPos.X - vw / 2f;
            float oy = playerPos.Y - vh / 2f;
            ox = Math.Max(0, Math.Min(ox, mapWidth - vw));
            oy = Math.Max(0, Math.Min(oy, mapHeight - vh));

            int prev = hoveredItemIndex;
            hoveredItemIndex = -1;

            // 检测鼠标是否在某个物品上
            for (int i = 0; i < sceneItems.Count; i++)
            {
                var item = sceneItems[i];
                int sx = item.Rect.X - (int)ox;
                int sy = item.Rect.Y - (int)oy;
                Rectangle screenRect = new Rectangle(sx, sy, item.Rect.Width, item.Rect.Height);
                if (screenRect.Contains(e.Location))
                {
                    hoveredItemIndex = i;
                    break;
                }
            }

            // 仅当悬停状态变化时才刷新
            if (prev != hoveredItemIndex)
                canvas.Invalidate();
        }

        /// <summary>触发调查成功反馈动画（✓ 图标 500ms）</summary>
        private void TriggerFeedback(int itemIndex)
        {
            feedbackItemIndex = itemIndex;
            feedbackStartTime = DateTime.Now;
            tmrFeedback.Stop();
            tmrFeedback.Start();
            canvas.Invalidate();
        }

        // ==================== 游戏循环 & 移动 ====================
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
            // 装饰动画帧推进（~2fps 速度感）
            animFrame = (animFrame + 1) % 90;
            canvas.Invalidate();
        }

        private void FormPlayerA_KeyDown(object sender, KeyEventArgs e)
        {
            // 对话气泡显示时，按E关闭气泡（含最后一句）
            if (e.KeyCode == Keys.E)
            {
                if (!string.IsNullOrEmpty(dialogueText))
                {
                    // 若为最后一句对话，重置对应NPC的对话进度，下次E从头开始
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
                case Keys.W: moveUp = true; break;
                case Keys.S: moveDown = true; break;
                case Keys.A: moveLeft = true; break;
                case Keys.D: moveRight = true; break;
            }
        }

        private void FormPlayerA_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: moveUp = false; break;
                case Keys.S: moveDown = false; break;
                case Keys.A: moveLeft = false; break;
                case Keys.D: moveRight = false; break;
            }
        }

        // ==================== 交互检测 ====================
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
            SoundManager.PlayNoInteraction();
        }

        private void ShowNPCDialogue(NPCData npc)
        {
            npc.DialogueIndex = (npc.DialogueIndex + 1) % npc.Dialogues.Count;
            dialogueText = npc.Dialogues[npc.DialogueIndex];
            isLastDialogue = (npc.DialogueIndex == npc.Dialogues.Count - 1);
            SoundManager.PlayDialogue();
            // 手动关闭，不再自动消失
            canvas.Invalidate();

            // 根据对话添加时间线
            if (npc.Name.Contains("贝蒂") && npc.DialogueIndex == 2)
                AddTimeline("21:40 - 贝蒂看到莫里斯在走廊张望");
            if (npc.Name.Contains("格雷") && npc.DialogueIndex == 1)
                AddTimeline("22:00 - 格雷给死者服安眠药");
        }

        private void AddTimeline(string entry)
        {
            if (!lstTimeline.Items.Contains(entry))
                lstTimeline.Items.Add(entry);
        }

        /// <summary>从本地缓存或 GameManager 静态列表查找线索（防止 StateSync 未到达时静默失败）</summary>
        private Clue FindClueData(string clueId)
        {
            // 优先从本地同步缓存获取
            var cached = gameClient.ClueCache.FirstOrDefault(c => c.Id == clueId);
            if (cached != null)
                return new Clue { Id = cached.Id, Name = cached.Name, Description = cached.Description, IsDiscovered = cached.IsDiscovered, DiscoveredBy = cached.DiscoveredBy };
            // 缓存未同步时直接从 GameManager 静态列表获取（同进程内可访问）
            return GameManager.AllClues.FirstOrDefault(c => c.Id == clueId);
        }

        private void HandleItemInteraction(SceneItem item)
        {
            switch (item.Type)
            {
                case ItemType.Normal:
                    gameClient.DiscoverClue(item.ClueId, "A");
                    {
                        var clue = FindClueData(item.ClueId);
                        if (clue != null)
                        {
                            if (!lstCluesA.Items.Contains(clue.Name)) lstCluesA.Items.Add(clue.Name);
                            TriggerFeedback(sceneItems.IndexOf(item));
                            SoundManager.PlayDiscovery();
                            PixelMessageBox.Show(clue.Description, clue.Name);
                        }
                        else SoundManager.PlayError();
                    }
                    break;
                case ItemType.Desk:
                    {
                        bool hasKey = gameClient.IsClueDiscovered("key")
                                       || GameManager.AllClues.Any(c => c.Id == "key" && c.IsDiscovered);
                        if (hasKey)
                        {
                            string newId = "diary_page";
                            gameClient.DiscoverClue(newId, "A");
                            var diary = FindClueData(newId);
                            if (diary != null)
                            {
                                if (!lstCluesA.Items.Contains(diary.Name)) lstCluesA.Items.Add(diary.Name);
                                TriggerFeedback(sceneItems.IndexOf(item));
                                SoundManager.PlayDiscovery();
                                PixelMessageBox.Show(diary.Description, diary.Name);
                            }
                            AddTimeline("23:00 - 莫里斯偷窃古董被死者发现");
                        }
                        else PixelMessageBox.Show("书桌抽屉锁着，需要一把细小钥匙");
                    }
                    break;
                case ItemType.Safe:
                    {
                        bool hasBible = gameClient.IsClueDiscovered("bible_note")
                                        || GameManager.AllClues.Any(c => c.Id == "bible_note" && c.IsDiscovered);
                        bool hasCalendar = gameClient.IsClueDiscovered("calendar")
                                           || GameManager.AllClues.Any(c => c.Id == "calendar" && c.IsDiscovered);
                        if (hasBible && hasCalendar)
                        {
                            string input = PromptInputBox("请输入4位数字密码：", "保险箱");
                            if (input == "1225") gameClient.UnlockSafe();
                            else if (!string.IsNullOrEmpty(input)) PixelMessageBox.Show("密码错误", "保险箱");
                        }
                        else PixelMessageBox.Show("需要更多线索");
                    }
                    break;
                case ItemType.Phone:
                    gameClient.RequestCall("A");
                    isCallingOut = true;
                    tmrAnimate.Start();
                    tmrTimeout.Start();
                    callStartTime = DateTime.Now;
                    break;
            }
            // 交互后刷新场景，更新“已调查”状态
            canvas.Invalidate();
        }

        // 场景绘制已迁移至 FormPlayerA_Paint.cs

        // ==================== 指认真凶 ====================
        private void BtnAccuse_Click(object sender, EventArgs e)
        {
            if (hasAccused) { PixelMessageBox.Show("你已经指认过了，请等待对方。"); return; }
            string[] suspects = { "埃德加", "莫里斯", "贝蒂", "格雷医生" };
            using (Form f = new Form())
            {
                f.Text = "指认真凶"; f.Width = 300; f.Height = 200;
                f.StartPosition = FormStartPosition.CenterParent;
                ComboBox cmb = new ComboBox() { Left = 30, Top = 30, Width = 220, DataSource = suspects, DropDownStyle = ComboBoxStyle.DropDownList };
                Button btn = new Button() { Text = "确认指认", Left = 80, Top = 80, DialogResult = DialogResult.OK };
                f.Controls.Add(cmb); f.Controls.Add(btn);
                if (f.ShowDialog() == DialogResult.OK)
                {
                    string accused = cmb.SelectedItem.ToString();
                    hasAccused = true;
                    btnAccuse.Enabled = false;
                    gameClient.SubmitAccusation("A", accused);
                }
            }
        }

        // ==================== 电话系统 ====================
        private void InitializePhoneSystem()
        {
            tmrAnimate = new Timer { Interval = 300 };
            tmrAnimate.Tick += (s, e) => { picCharacterAnimFrame = !picCharacterAnimFrame; canvas.Invalidate(); SoundManager.PlayRingTick(); };
            tmrTimeout = new Timer { Interval = 3000 }; tmrTimeout.Tick += TmrTimeout_Tick;
            tmrProgress = new Timer { Interval = 30 }; tmrProgress.Tick += TmrProgress_Tick;
            tmrBubble = new Timer { Interval = 1500 }; tmrBubble.Tick += (s, e) => { tmrBubble.Stop(); lblBubble.Visible = false; };

            pnlIncoming = new Panel { Size = new Size(160, 80), BackColor = Theme.BgPanel, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            lblIncoming = new Label { Text = "有来电", Location = new Point(5, 5), AutoSize = true, ForeColor = Theme.TextMain };
            btnAccept = new Button { Text = "接听", Size = new Size(60, 30), Location = new Point(10, 30) };
            btnDecline = new Button { Text = "拒绝", Size = new Size(60, 30), Location = new Point(80, 30) };
            Theme.StyleButton(btnAccept);
            Theme.StyleButton(btnDecline);
            pgbTimeout = new Panel { Size = new Size(160, 5), Location = new Point(0, 75), BackColor = Theme.Border };
            btnAccept.Click += (s, e) => { gameClient.AcceptCall("A"); };
            btnDecline.Click += (s, e) => { gameClient.DeclineCall("A"); pnlIncoming.Visible = false; StopRingingUI(); };
            pnlIncoming.Controls.Add(lblIncoming);
            pnlIncoming.Controls.Add(btnAccept);
            pnlIncoming.Controls.Add(btnDecline);
            pnlIncoming.Controls.Add(pgbTimeout);

            this.Controls.Add(pnlIncoming);
            pnlIncoming.Location = new Point((this.ClientSize.Width - 160) / 2, this.ClientSize.Height / 2 - 40);
            pnlIncoming.BringToFront();

            lblBubble = new Label { Text = "无人接听...", ForeColor = Theme.TextMain, BackColor = Theme.BgDark, Visible = false, AutoSize = true };
            this.Controls.Add(lblBubble);
            lblBubble.Location = new Point((this.ClientSize.Width - lblBubble.Width) / 2, pnlIncoming.Top - 30);
            lblBubble.BringToFront();

            this.Resize += (s, e) =>
            {
                if (pnlIncoming.Visible)
                {
                    pnlIncoming.Location = new Point((this.ClientSize.Width - 160) / 2, this.ClientSize.Height / 2 - 40);
                    lblBubble.Location = new Point((this.ClientSize.Width - lblBubble.Width) / 2, pnlIncoming.Top - 30);
                }
            };

            // 电话事件订阅已迁移至 gameClient（构造函数中绑定）
        }

        private void StopRingingUI() { tmrAnimate.Stop(); tmrTimeout.Stop(); tmrProgress.Stop(); isCallingOut = false; pnlIncoming.Visible = false; lblBubble.Visible = false; }
        private void TmrTimeout_Tick(object sender, EventArgs e) { tmrTimeout.Stop(); if (isCallingOut) gameClient.HangUp("A"); else gameClient.DeclineCall("A"); StopRingingUI(); }
        private void TmrProgress_Tick(object sender, EventArgs e) { float ratio = 1f - (float)(DateTime.Now - callStartTime).TotalSeconds / 3f; if (ratio < 0) ratio = 0; pgbTimeout.Width = (int)(160 * ratio); }

        private void PhoneManager_OnCallRequest(string caller, string callee)
        {
            if (InvokeRequired) { Invoke(new Action<string, string>(PhoneManager_OnCallRequest), caller, callee); return; }
            if (callee == "A") { pnlIncoming.Visible = true; callStartTime = DateTime.Now; tmrProgress.Start(); tmrTimeout.Start(); }
        }
        private void PhoneManager_OnCallEstablished()
        {
            if (InvokeRequired) { Invoke(new Action(PhoneManager_OnCallEstablished)); return; }
            StopRingingUI(); SoundManager.PlayCallAccept(); currentChatForm = new FormChat("A", gameClient); currentChatForm.FormClosed += (s, e) => gameClient.HangUp("A"); currentChatForm.Show();
        }
        private void PhoneManager_OnCallEnded()
        {
            if (InvokeRequired) { Invoke(new Action(PhoneManager_OnCallEnded)); return; }
            StopRingingUI(); SoundManager.PlayCallEnd(); if (currentChatForm != null && !currentChatForm.IsDisposed) { currentChatForm.Close(); currentChatForm = null; }
        }
        private void PhoneManager_OnRingTimeout(string caller)
        {
            if (InvokeRequired) { Invoke(new Action<string>(PhoneManager_OnRingTimeout), caller); return; }
            StopRingingUI(); if (caller == "A") { lblBubble.Visible = true; tmrBubble.Start(); }
        }

        // ==================== 网络事件处理 ====================
        private void FormPlayerA_Load(object sender, EventArgs e)
        {
            MusicManager.StartBgm();
            // 从本地缓存加载已有线索和分享给我的线索
            foreach (var c in gameClient.ClueCache)
            {
                if (c.IsDiscovered && (c.DiscoveredBy == "A" || c.SharedTo == "A")
                    && !lstCluesA.Items.Contains(c.Name))
                    lstCluesA.Items.Add(c.Name);
            }
        }

        private void GameClient_OnClueDiscovered(string clueId, string player)
        {
            if (InvokeRequired) { Invoke(new Action(() => GameClient_OnClueDiscovered(clueId, player))); return; }
            var cl = gameClient.ClueCache.FirstOrDefault(c => c.Id == clueId);
            if (cl != null && cl.DiscoveredBy == "A" && !lstCluesA.Items.Contains(cl.Name))
                lstCluesA.Items.Add(cl.Name);
        }

        private void GameClient_OnClueShared(string clueId, string fromPlayer, string toPlayer, string clueName)
        {
            if (InvokeRequired) { Invoke(new Action(() => GameClient_OnClueShared(clueId, fromPlayer, toPlayer, clueName))); return; }
            if (toPlayer == "A")
            {
                // A 收到了对方分享的线索
                if (!lstCluesA.Items.Contains(clueName))
                    lstCluesA.Items.Add(clueName);
            }
        }

        private void GameClient_OnSafeUnlocked()
        {
            if (InvokeRequired) { Invoke(new Action(GameClient_OnSafeUnlocked)); return; }
            // 防止两个客户端同时弹窗
            if (GameManager.SafeMessageShown) return;
            GameManager.SafeMessageShown = true;
            SoundManager.PlaySafeUnlock();
            PixelMessageBox.Show("保险箱打开了！里面有一份遗嘱和一封举报信，已自动记录为线索。请继续调查并指认真凶。", "保险箱已开");
        }

        private void GameClient_OnAccusationResult(string accA, string accB, bool bothCorrect)
        {
            if (InvokeRequired) { Invoke(new Action(() => GameClient_OnAccusationResult(accA, accB, bothCorrect))); return; }

            if (bothCorrect)
            {
                if (!GameManager.ResultMessageShown)
                {
                    GameManager.ResultMessageShown = true;
                    SoundManager.PlaySuccess();
                    MusicManager.StopBgm(); // 停止探案背景音乐
                    // 显示全屏结局画面
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
                // 立即恢复本地按钮状态（两个客户端各自执行）
                hasAccused = false;
                btnAccuse.Enabled = true;

                // 弹窗只展示一次（服务器端已清空指认记录）
                if (!GameManager.ResultMessageShown)
                {
                    GameManager.ResultMessageShown = true;
                    SoundManager.PlayError();
                    PixelMessageBox.Show(
                        "两位侦探指认结果不一致，请重新沟通后再次指认。",
                        "指认失败");
                }
            }
        }
        /// <summary>外部调用：停止探案背景音乐（进入结局前调用）</summary>
        public void StopBackgroundMusic()
        {
            MusicManager.StopBgm();
        }

        private void FormPlayerA_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (gameClient != null)
            {
                gameClient.OnClueDiscovered -= GameClient_OnClueDiscovered;
                gameClient.OnClueShared -= GameClient_OnClueShared;
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
            tmrFeedback?.Stop();
            gameLoop?.Dispose();
            tmrAnimate?.Dispose();
            tmrTimeout?.Dispose();
            tmrProgress?.Dispose();
            tmrBubble?.Dispose();
            tmrDialogue?.Dispose();
            tmrFeedback?.Dispose();
            itemFont?.Dispose();
            dialogueFont?.Dispose();
            bgBrush?.Dispose();
            bgFillBrush?.Dispose();
            floorLinePenA?.Dispose();
            hintBgBrush?.Dispose();
            floorTileA?.Dispose();
            wallStripePen?.Dispose();
        }

        private string PromptInputBox(string prompt, string title)
        {
            using (Form f = new Form())
            {
                f.Text = title;
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.None;
                f.BackColor = Color.FromArgb(253, 245, 230); // 羊皮纸
                f.ClientSize = new Size(320, 150);
                f.KeyPreview = true;
                f.ShowInTaskbar = false;

                // 像素边框
                f.Paint += (s, e) =>
                {
                    Rectangle r = new Rectangle(0, 0, f.Width - 1, f.Height - 1);
                    using (Pen outer = new Pen(Theme.Border, 2))
                        e.Graphics.DrawRectangle(outer, r);
                    using (Pen inner = new Pen(Theme.BorderDark, 1))
                        e.Graphics.DrawRectangle(inner, 3, 3, f.Width - 7, f.Height - 7);
                    // 标题栏
                    using (Brush tb = new SolidBrush(Theme.BorderDark))
                        e.Graphics.FillRectangle(tb, 2, 2, f.Width - 4, 24);
                    using (Font tf = Theme.GetFont(10f))
                    using (Brush tbr = new SolidBrush(Color.FromArgb(253, 245, 230)))
                    {
                        SizeF ts = e.Graphics.MeasureString(title, tf);
                        e.Graphics.DrawString(title, tf, tbr, (f.Width - ts.Width) / 2, 3);
                    }
                };

                Label l = new Label
                {
                    Left = 20, Top = 32, Text = prompt, AutoSize = true,
                    Font = Theme.GetFont(9f),
                    ForeColor = Color.FromArgb(40, 30, 20),
                    BackColor = Color.Transparent
                };

                TextBox t = new TextBox
                {
                    Left = 20, Top = 56, Width = 280,
                    Font = Theme.GetFont(11f),
                    BackColor = Color.FromArgb(245, 240, 230),
                    ForeColor = Color.FromArgb(40, 30, 20),
                    BorderStyle = BorderStyle.FixedSingle
                };

                Button b = new Button
                {
                    Text = "✓  确  定",
                    Left = 200, Width = 100, Top = 90,
                    Font = Theme.GetFont(10f),
                    DialogResult = DialogResult.OK
                };
                Theme.StyleButton(b);

                f.Controls.Add(l);
                f.Controls.Add(t);
                f.Controls.Add(b);
                f.AcceptButton = b;

                // 拖拽
                bool dragging = false; Point dragStart = Point.Empty;
                f.MouseDown += (s, e) => { if (e.Y < 26) { dragging = true; dragStart = e.Location; } };
                f.MouseMove += (s, e) => { if (dragging) f.Location = new Point(
                    f.Location.X + e.X - dragStart.X, f.Location.Y + e.Y - dragStart.Y); };
                f.MouseUp += (s, e) => dragging = false;
                f.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) f.DialogResult = DialogResult.Cancel; };

                return f.ShowDialog() == DialogResult.OK ? t.Text : string.Empty;
            }
        }

        /// <summary>点击线索列表或时间线项时弹出详情</summary>
        private void ShowItemDetail(ListBox lb)
        {
            if (lb.SelectedItem == null) return;
            string selectedText = lb.SelectedItem.ToString();
            // 在全部线索中按 Name 查找
            var clue = GameManager.AllClues.FirstOrDefault(c => c.Name == selectedText);
            if (clue != null)
                PixelMessageBox.Show(clue.Description, clue.Name);
            else
                PixelMessageBox.Show(selectedText, "时间线事件");
        }

        private enum ItemType { Normal, Desk, Safe, Phone }
        private class SceneItem
        {
            public string Name;
            public Rectangle Rect;
            public string ClueId;
            public ItemType Type;
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