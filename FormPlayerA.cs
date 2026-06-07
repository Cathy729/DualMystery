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
        private Font itemFont = new Font("Georgia", 9);
        private Font dialogueFont = new Font("Georgia", 10);
        private SolidBrush bgBrush = new SolidBrush(Color.FromArgb(30, 30, 34));

        // 装饰动画
        private int animFrame = 0;
        private Bitmap[] fireplaceFrames;
        private Bitmap[] chandelierFrames;

        public FormPlayerA()
        {
            InitializeComponent();
            GenerateCharacterBitmaps(out charIdle, out charStomp);
            GenerateNPCBitmaps(out npcBetty, out npcGrey);
            InitializeCustomUI();
            BuildScene();
            BuildNPCs();
            fireplaceFrames = new Bitmap[3];
            var fpSrc = PixelIcons.CreateFireplaceFrames();
            for (int i = 0; i < 3; i++) fireplaceFrames[i] = new Bitmap(fpSrc[i], 64, 64);
            chandelierFrames = new Bitmap[3];
            var chSrc = PixelIcons.CreateChandelierFrames();
            for (int i = 0; i < 3; i++) chandelierFrames[i] = new Bitmap(chSrc[i], 48, 48);
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
            Bitmap base1 = new Bitmap(16, 16), base2 = new Bitmap(16, 16);
            Color skin = Color.FromArgb(255, 224, 189), body = Color.DarkBlue, pants = Color.FromArgb(40, 40, 40);
            using (Graphics g1 = Graphics.FromImage(base1), g2 = Graphics.FromImage(base2))
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
            frame1 = new Bitmap(48, 48); frame2 = new Bitmap(48, 48);
            using (Graphics g1 = Graphics.FromImage(frame1), g2 = Graphics.FromImage(frame2))
            {
                g1.InterpolationMode = InterpolationMode.NearestNeighbor;
                g2.InterpolationMode = InterpolationMode.NearestNeighbor;
                g1.DrawImage(base1, new Rectangle(0, 0, 48, 48), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
                g2.DrawImage(base2, new Rectangle(0, 0, 48, 48), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
            }
            base1.Dispose(); base2.Dispose();
        }

        private void GenerateNPCBitmaps(out Bitmap betty, out Bitmap grey)
        {
            betty = new Bitmap(48, 48); grey = new Bitmap(48, 48);
            Bitmap baseB = new Bitmap(16, 16), baseG = new Bitmap(16, 16);
            Color skin = Color.FromArgb(255, 224, 189);
            Color dress = Color.FromArgb(180, 140, 160); // 女仆裙
            Color suit = Color.FromArgb(60, 60, 70);     // 医生西装
            using (Graphics g1 = Graphics.FromImage(baseB), g2 = Graphics.FromImage(baseG))
            {
                g1.Clear(Color.Transparent); g2.Clear(Color.Transparent);
                using (Brush sb = new SolidBrush(skin), db = new SolidBrush(dress), sub = new SolidBrush(suit))
                {
                    // 贝蒂
                    g1.FillRectangle(sb, 4, 2, 8, 6);
                    g1.FillRectangle(Brushes.White, 6, 3, 2, 2); g1.FillRectangle(Brushes.Black, 7, 3, 1, 2);
                    g1.FillRectangle(Brushes.White, 10, 3, 2, 2); g1.FillRectangle(Brushes.Black, 11, 3, 1, 2);
                    g1.FillRectangle(db, 5, 8, 6, 6);
                    g1.FillRectangle(Brushes.Black, 6, 14, 3, 2);
                    g1.FillRectangle(Brushes.Black, 9, 14, 3, 2);
                    // 格雷
                    g2.FillRectangle(sb, 4, 2, 8, 6);
                    g2.FillRectangle(Brushes.White, 6, 3, 2, 2); g2.FillRectangle(Brushes.Black, 7, 3, 1, 2);
                    g2.FillRectangle(Brushes.White, 10, 3, 2, 2); g2.FillRectangle(Brushes.Black, 11, 3, 1, 2);
                    g2.FillRectangle(sub, 5, 8, 6, 6);
                    g2.FillRectangle(Brushes.Black, 6, 14, 3, 2);
                    g2.FillRectangle(Brushes.Black, 9, 14, 3, 2);
                }
            }
            using (Graphics g1 = Graphics.FromImage(betty), g2 = Graphics.FromImage(grey))
            {
                g1.InterpolationMode = InterpolationMode.NearestNeighbor;
                g2.InterpolationMode = InterpolationMode.NearestNeighbor;
                g1.DrawImage(baseB, new Rectangle(0, 0, 48, 48), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
                g2.DrawImage(baseG, new Rectangle(0, 0, 48, 48), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
            }
            baseB.Dispose(); baseG.Dispose();
        }

        // ==================== UI 初始化 ====================
        private void InitializeCustomUI()
        {
            this.Text = "书房";
            this.BackColor = Color.FromArgb(30, 42, 46);
            this.ClientSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
            this.DoubleBuffered = true;

            Label lblTitle = new Label
            {
                Text = "📖 书房",
                Font = new Font("Georgia", 16f),
                ForeColor = Color.FromArgb(201, 169, 110),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            Label lblHint = new Label
            {
                Text = "WASD移动 | E键调查/对话/拨打电话",
                Font = new Font("Georgia", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 200),
                BackColor = Color.FromArgb(180, 40, 40, 40),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(8, 2, 8, 2)
            };
            lblHint.Location = new Point((this.ClientSize.Width - lblHint.PreferredWidth) / 2, lblTitle.Bottom + 2);
            this.Controls.Add(lblHint);

            // 右侧面板
            Panel rightPanel = new Panel
            {
                Width = 200,
                Dock = DockStyle.Right,
                BackColor = Color.FromArgb(25, 35, 38)
            };

            // 剧情简介
            Label lblStory = new Label
            {
                Text = "📖 书房侦探\n尸体、刀、书桌、保险箱…\n询问贝蒂和格雷医生。",
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(35, 40, 38),
                ForeColor = Color.LightGray,
                Font = new Font("Georgia", 9f)
            };
            rightPanel.Controls.Add(lblStory);

            // 线索笔记
            GroupBox gbNotes = new GroupBox
            {
                Text = "线索笔记",
                Height = 150,
                Dock = DockStyle.Top,
                ForeColor = Color.White
            };
            lstCluesA = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 42, 46),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            gbNotes.Controls.Add(lstCluesA);
            rightPanel.Controls.Add(gbNotes);

            // 时间线
            GroupBox gbTimeline = new GroupBox
            {
                Text = "时间线",
                Height = 150,
                Dock = DockStyle.Top,
                ForeColor = Color.White
            };
            lstTimeline = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 42, 46),
                ForeColor = Color.FromArgb(201, 169, 110),
                BorderStyle = BorderStyle.None,
                Font = new Font("Georgia", 9f)
            };
            gbTimeline.Controls.Add(lstTimeline);
            rightPanel.Controls.Add(gbTimeline);

            // 指认按钮
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

            canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 30, 34)
            };
            canvas.Paint += Canvas_Paint;
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
            System.Media.SystemSounds.Beep.Play();
        }

        private void ShowNPCDialogue(NPCData npc)
        {
            npc.DialogueIndex = (npc.DialogueIndex + 1) % npc.Dialogues.Count;
            dialogueText = npc.Dialogues[npc.DialogueIndex];
            isLastDialogue = (npc.DialogueIndex == npc.Dialogues.Count - 1);
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
                            MessageBox.Show(clue.Description, clue.Name);
                        }
                        else System.Media.SystemSounds.Beep.Play();
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
                                MessageBox.Show(diary.Description, diary.Name);
                            }
                            AddTimeline("23:00 - 莫里斯偷窃古董被死者发现");
                        }
                        else MessageBox.Show("书桌抽屉锁着，需要一把细小钥匙");
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
                            else if (!string.IsNullOrEmpty(input)) MessageBox.Show("密码错误", "保险箱");
                        }
                        else MessageBox.Show("需要更多线索");
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
            if (hasAccused) { MessageBox.Show("你已经指认过了，请等待对方。"); return; }
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
            tmrAnimate.Tick += (s, e) => { picCharacterAnimFrame = !picCharacterAnimFrame; canvas.Invalidate(); System.Media.SystemSounds.Beep.Play(); };
            tmrTimeout = new Timer { Interval = 3000 }; tmrTimeout.Tick += TmrTimeout_Tick;
            tmrProgress = new Timer { Interval = 30 }; tmrProgress.Tick += TmrProgress_Tick;
            tmrBubble = new Timer { Interval = 1500 }; tmrBubble.Tick += (s, e) => { tmrBubble.Stop(); lblBubble.Visible = false; };

            pnlIncoming = new Panel { Size = new Size(160, 80), BackColor = Color.AntiqueWhite, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            lblIncoming = new Label { Text = "有来电", Location = new Point(5, 5), AutoSize = true, ForeColor = Color.Black };
            btnAccept = new Button { Text = "接听", Size = new Size(60, 30), Location = new Point(10, 30) };
            btnDecline = new Button { Text = "拒绝", Size = new Size(60, 30), Location = new Point(80, 30) };
            pgbTimeout = new Panel { Size = new Size(160, 5), Location = new Point(0, 75), BackColor = Color.Brown };
            btnAccept.Click += (s, e) => { gameClient.AcceptCall("A"); };
            btnDecline.Click += (s, e) => { gameClient.DeclineCall("A"); pnlIncoming.Visible = false; StopRingingUI(); };
            pnlIncoming.Controls.Add(lblIncoming);
            pnlIncoming.Controls.Add(btnAccept);
            pnlIncoming.Controls.Add(btnDecline);
            pnlIncoming.Controls.Add(pgbTimeout);

            this.Controls.Add(pnlIncoming);
            pnlIncoming.Location = new Point((this.ClientSize.Width - 160) / 2, this.ClientSize.Height / 2 - 40);
            pnlIncoming.BringToFront();

            lblBubble = new Label { Text = "无人接听...", ForeColor = Color.White, BackColor = Color.Black, Visible = false, AutoSize = true };
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
            StopRingingUI(); currentChatForm = new FormChat("A", gameClient); currentChatForm.FormClosed += (s, e) => gameClient.HangUp("A"); currentChatForm.Show();
        }
        private void PhoneManager_OnCallEnded()
        {
            if (InvokeRequired) { Invoke(new Action(PhoneManager_OnCallEnded)); return; }
            StopRingingUI(); if (currentChatForm != null && !currentChatForm.IsDisposed) { currentChatForm.Close(); currentChatForm = null; }
        }
        private void PhoneManager_OnRingTimeout(string caller)
        {
            if (InvokeRequired) { Invoke(new Action<string>(PhoneManager_OnRingTimeout), caller); return; }
            StopRingingUI(); if (caller == "A") { lblBubble.Visible = true; tmrBubble.Start(); }
        }

        // ==================== 网络事件处理 ====================
        private void FormPlayerA_Load(object sender, EventArgs e)
        {
            // 从本地缓存加载已有线索（StateSync 已到达则直接显示）
            foreach (var c in gameClient.ClueCache)
                if (c.IsDiscovered && c.DiscoveredBy == "A" && !lstCluesA.Items.Contains(c.Name))
                    lstCluesA.Items.Add(c.Name);
        }

        private void GameClient_OnClueDiscovered(string clueId, string player)
        {
            if (InvokeRequired) { Invoke(new Action(() => GameClient_OnClueDiscovered(clueId, player))); return; }
            var cl = gameClient.ClueCache.FirstOrDefault(c => c.Id == clueId);
            if (cl != null && cl.DiscoveredBy == "A" && !lstCluesA.Items.Contains(cl.Name))
                lstCluesA.Items.Add(cl.Name);
        }

        private void GameClient_OnSafeUnlocked()
        {
            if (InvokeRequired) { Invoke(new Action(GameClient_OnSafeUnlocked)); return; }
            MessageBox.Show("保险箱打开了！里面有一份遗嘱和一封举报信，已自动记录为线索。请继续调查并指认真凶。", "保险箱已开");
        }

        private void GameClient_OnAccusationResult(string accA, string accB, bool bothCorrect)
        {
            if (InvokeRequired) { Invoke(new Action(() => GameClient_OnAccusationResult(accA, accB, bothCorrect))); return; }

            if (bothCorrect)
            {
                if (!GameManager.ResultMessageShown)
                {
                    GameManager.ResultMessageShown = true;
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
        private void FormPlayerA_FormClosing(object sender, FormClosingEventArgs e)
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

        private string PromptInputBox(string prompt, string title)
        {
            using (Form f = new Form())
            {
                f.Width = 300; f.Height = 150; f.Text = title; f.StartPosition = FormStartPosition.CenterParent;
                Label l = new Label() { Left = 20, Top = 20, Text = prompt, AutoSize = true };
                TextBox t = new TextBox() { Left = 20, Top = 50, Width = 240 };
                Button b = new Button() { Text = "确定", Left = 160, Width = 100, Top = 80, DialogResult = DialogResult.OK };
                f.Controls.Add(l); f.Controls.Add(t); f.Controls.Add(b); f.AcceptButton = b;
                return f.ShowDialog() == DialogResult.OK ? t.Text : string.Empty;
            }
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