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
        private Timer tmrDialogue;

        // 缓存 GDI 对象，避免 Paint 中反复创建
        private Font itemFont = new Font("Georgia", 7);
        private Font dialogueFont = new Font("Georgia", 8);
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
                Font = new Font("Georgia", 14f),
                ForeColor = Color.FromArgb(201, 169, 110),
                Dock = DockStyle.Top,
                Height = 36,
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
                Text = "📜 书房侦探\n尸体、刀、书桌、保险箱…\n询问贝蒂和格雷医生。",
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(35, 40, 38),
                ForeColor = Color.LightGray,
                Font = new Font("Georgia", 7f)
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
                Font = new Font("Georgia", 7f)
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
                Font = new Font("Georgia", 10f, FontStyle.Bold)
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
            switch (e.KeyCode)
            {
                case Keys.W: moveUp = true; break;
                case Keys.S: moveDown = true; break;
                case Keys.A: moveLeft = true; break;
                case Keys.D: moveRight = true; break;
                case Keys.E: Interact(); break;
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
        private void Interact()
        {
            // 先检查NPC
            foreach (var npc in npcList)
            {
                Rectangle r = npc.Rect;
                r.Inflate(50, 50);
                if (r.Contains((int)playerPos.X, (int)playerPos.Y))
                {
                    ShowNPCDialogue(npc);
                    return;
                }
            }
            // 再检查物品
            foreach (var item in sceneItems)
            {
                Rectangle detectRect = item.Rect;
                detectRect.Inflate(40, 40);
                if (detectRect.Contains((int)playerPos.X, (int)playerPos.Y))
                {
                    HandleItemInteraction(item);
                    break;
                }
            }
        }

        private void ShowNPCDialogue(NPCData npc)
        {
            npc.DialogueIndex = (npc.DialogueIndex + 1) % npc.Dialogues.Count;
            dialogueText = npc.Dialogues[npc.DialogueIndex];
            tmrDialogue.Stop();
            tmrDialogue.Start();
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

        private void HandleItemInteraction(SceneItem item)
        {
            switch (item.Type)
            {
                case ItemType.Normal:
                    GameManager.DiscoverClue(item.ClueId, "A");
                    var clue = GameManager.AllClues.FirstOrDefault(c => c.Id == item.ClueId);
                    if (clue != null) MessageBox.Show(clue.Description, clue.Name);
                    // 添加对应时间线
                    if (item.ClueId == "knife") AddTimeline("23:30 - 凶器匕首，刻有字母M");
                    if (item.ClueId == "handkerchief") AddTimeline("案发后 - 带血手帕，绣E.B.");
                    break;
                case ItemType.Desk:
                    var keyClue = GameManager.AllClues.FirstOrDefault(c => c.Id == "key");
                    if (keyClue != null && keyClue.IsDiscovered)
                    {
                        string newId = "diary_page";
                        var diary = GameManager.AllClues.FirstOrDefault(c => c.Id == newId);
                        if (diary == null)
                        {
                            diary = new Clue { Id = newId, Name = "日记残页", Description = "你用那把细小钥匙打开了抽屉，找到一张日记残页：霍华德发现莫里斯偷窃古董，准备揭发。" };
                            GameManager.AllClues.Add(diary);
                        }
                        GameManager.DiscoverClue(newId, "A");
                        MessageBox.Show(diary.Description, diary.Name);
                        AddTimeline("23:00 - 莫里斯偷窃古董被死者发现");
                    }
                    else MessageBox.Show("书桌抽屉锁着，需要一把细小钥匙");
                    break;
                case ItemType.Safe:
                    var p1 = GameManager.AllClues.FirstOrDefault(c => c.Id == "bible_note");
                    var p2 = GameManager.AllClues.FirstOrDefault(c => c.Id == "calendar");
                    if (p1?.IsDiscovered == true && p2?.IsDiscovered == true)
                    {
                        string input = PromptInputBox("请输入4位数字密码：", "保险箱");
                        if (input == "1225") GameManager.UnlockSafe();
                        else if (!string.IsNullOrEmpty(input)) MessageBox.Show("密码错误", "保险箱");
                    }
                    else MessageBox.Show("需要更多线索");
                    break;
                case ItemType.Phone:
                    PhoneManager.RequestCall("A");
                    isCallingOut = true;
                    tmrAnimate.Start();
                    tmrTimeout.Start();
                    callStartTime = DateTime.Now;
                    break;
            }
        }

        // ==================== 场景绘制 ====================
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            int vw = canvas.Width, vh = canvas.Height;
            float ox = playerPos.X - vw / 2f;
            float oy = playerPos.Y - vh / 2f;
            ox = Math.Max(0, Math.Min(ox, mapWidth - vw));
            oy = Math.Max(0, Math.Min(oy, mapHeight - vh));

            // ---- 背景 ----
            g.FillRectangle(new SolidBrush(Color.FromArgb(45, 40, 36)), 0, 0, mapWidth, mapHeight);
            // 深色墙裙（下半墙）
            g.FillRectangle(new SolidBrush(Color.FromArgb(35, 30, 28)), 0, 350 - (int)oy, mapWidth, 450);
            // 墙裙线
            g.DrawLine(new Pen(Color.FromArgb(80, 60, 40), 2), 0, 350 - (int)oy, mapWidth, 350 - (int)oy);

            // ---- 地毯（暗红色打底+花纹） ----
            int carpetY = 520 - (int)oy;
            g.FillRectangle(new SolidBrush(Color.FromArgb(120, 30, 30)), 290 - (int)ox, carpetY, 620, 30);
            g.FillRectangle(new SolidBrush(Color.FromArgb(80, 20, 20)), 290 - (int)ox, carpetY, 620, 3);
            g.FillRectangle(new SolidBrush(Color.FromArgb(80, 20, 20)), 290 - (int)ox, carpetY + 27, 620, 3);
            using (Bitmap carpetTile = PixelIcons.CreateCarpet())
            {
                for (int cx = 300; cx < 900; cx += 32)
                    g.DrawImage(carpetTile, cx - (int)ox, carpetY, 32, 24);
            }

            // ---- 壁炉（左墙，带动画） ----
            int fireplaceX = 80 - (int)ox, fireplaceY = 280 - (int)oy;
            int fpFrameIdx = (animFrame / 30) % 3;
            if (fireplaceFrames != null && fireplaceFrames[fpFrameIdx] != null)
                g.DrawImage(fireplaceFrames[fpFrameIdx], fireplaceX, fireplaceY, 80, 80);
            // 壁炉台装饰
            g.FillRectangle(new SolidBrush(Color.FromArgb(90, 60, 30)), fireplaceX + 60, fireplaceY + 30, 30, 4);

            // ---- 吊灯（顶部中央，带摆动） ----
            int chandelierX = 500 - (int)ox, chandelierY = 10 - (int)oy;
            int chFrameIdx = (animFrame / 20) % 3;
            if (chandelierFrames != null && chandelierFrames[chFrameIdx] != null)
                g.DrawImage(chandelierFrames[chFrameIdx], chandelierX, chandelierY, 60, 60);

            // ---- 书架（更丰富的书籍颜色） ----
            int shelfX = 200 - (int)ox, shelfY = 150 - (int)oy;
            g.FillRectangle(new SolidBrush(Color.FromArgb(100, 70, 50)), shelfX, shelfY, 80, 200);
            g.FillRectangle(new SolidBrush(Color.FromArgb(120, 80, 55)), shelfX, shelfY, 80, 4);
            Color[] bookColors = { Color.DarkRed, Color.DarkGreen, Color.DarkBlue, Color.SaddleBrown, Color.DarkGoldenrod, Color.Indigo };
            for (int row = 0; row < 4; row++)
            {
                int by = shelfY + 10 + row * 45;
                for (int col = 0; col < 8; col++)
                {
                    int bh = 35 + (row * 7) % 10;
                    using (Brush bb = new SolidBrush(bookColors[(row * 3 + col) % bookColors.Length]))
                        g.FillRectangle(bb, shelfX + 4 + col * 9, by, 8, bh);
                }
                g.DrawLine(new Pen(Color.FromArgb(60, 40, 20)), shelfX, by + 39, shelfX + 80, by + 39);
            }

            // ---- 窗户 + 窗帘 ----
            int winX = 700 - (int)ox, winY = 120 - (int)oy;
            g.FillRectangle(new SolidBrush(Color.FromArgb(130, 180, 210)), winX, winY, 60, 80);
            g.DrawRectangle(new Pen(Color.FromArgb(60, 50, 40), 2), winX, winY, 60, 80);
            g.DrawLine(new Pen(Color.FromArgb(60, 50, 40), 1), winX + 30, winY, winX + 30, winY + 80);
            g.DrawLine(new Pen(Color.FromArgb(60, 50, 40), 1), winX, winY + 40, winX + 60, winY + 40);
            // 窗帘
            using (Bitmap curtain = PixelIcons.CreateCurtain())
            {
                g.DrawImage(curtain, winX - 14, winY - 4, 16, 88);
                g.DrawImage(curtain, winX + 58, winY - 4, 16, 88);
            }

            // ---- 尸体（像素小人风格，肉色皮肤+深色外套） ----
            int bodyX = 400 - (int)ox, bodyY = 380 - (int)oy;
            // 阴影
            g.FillEllipse(new SolidBrush(Color.FromArgb(60, 0, 0, 0)), bodyX + 4, bodyY + 42, 32, 8);
            // 腿（深色裤子）
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 40, 40)), bodyX + 8, bodyY + 32, 10, 12);
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 40, 40)), bodyX + 22, bodyY + 32, 10, 12);
            // 身体（深色外套）
            g.FillRectangle(new SolidBrush(Color.FromArgb(65, 55, 45)), bodyX + 4, bodyY + 12, 32, 24);
            // 衬衫（浅色）
            g.FillRectangle(new SolidBrush(Color.FromArgb(210, 200, 190)), bodyX + 10, bodyY + 14, 20, 10);
            // 血迹（胸口致命伤）
            g.FillEllipse(new SolidBrush(Color.FromArgb(180, 20, 20)), bodyX + 14, bodyY + 16, 12, 8);
            g.FillEllipse(new SolidBrush(Color.FromArgb(120, 10, 10)), bodyX + 16, bodyY + 18, 6, 5);
            // 头（肉色）
            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 224, 189)), bodyX + 10, bodyY + 2, 20, 14);
            // 头发
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 30, 20)), bodyX + 8, bodyY, 24, 5);
            // 闭眼
            g.DrawLine(new Pen(Color.FromArgb(40, 30, 20)), bodyX + 13, bodyY + 7, bodyX + 17, bodyY + 7);
            g.DrawLine(new Pen(Color.FromArgb(40, 30, 20)), bodyX + 21, bodyY + 7, bodyX + 25, bodyY + 7);
            // 手臂（自然下垂，袖子）
            g.FillRectangle(new SolidBrush(Color.FromArgb(65, 55, 45)), bodyX - 2, bodyY + 14, 6, 16);
            g.FillRectangle(new SolidBrush(Color.FromArgb(65, 55, 45)), bodyX + 36, bodyY + 12, 6, 18);
            // 手（肉色）
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 224, 189)), bodyX - 1, bodyY + 28, 4, 4);
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 224, 189)), bodyX + 37, bodyY + 28, 4, 4);

            // ---- 绘制物品 ----
            foreach (var item in sceneItems)
            {
                int sx = item.Rect.X - (int)ox, sy = item.Rect.Y - (int)oy;
                if (sx + item.Rect.Width < 0 || sx > vw || sy + item.Rect.Height < 0 || sy > vh) continue;
                if (item.Icon != null)
                    g.DrawImage(item.Icon, sx, sy, item.Rect.Width, item.Rect.Height);
                else
                {
                    g.FillRectangle(Brushes.DarkOliveGreen, sx, sy, item.Rect.Width, item.Rect.Height);
                    g.DrawRectangle(Pens.Black, sx, sy, item.Rect.Width, item.Rect.Height);
                }
                g.DrawString(item.Name, itemFont, Brushes.White, sx, sy - 12);
            }

            // ---- 绘制 NPC + 道具 ----
            foreach (var npc in npcList)
            {
                int sx = npc.Rect.X - (int)ox, sy = npc.Rect.Y - (int)oy;
                if (npc.Icon != null)
                    g.DrawImage(npc.Icon, sx, sy, npc.Rect.Width, npc.Rect.Height);
                g.DrawString(npc.Name, itemFont, Brushes.Yellow, sx, sy - 12);

                // NPC 身份道具
                if (npc.Name.Contains("贝蒂"))
                {
                    using (Bitmap broom = PixelIcons.CreateBroom())
                        g.DrawImage(broom, sx + 40, sy + 10, 20, 26);
                }
                else if (npc.Name.Contains("格雷"))
                {
                    using (Bitmap bag = PixelIcons.CreateMedicalBag())
                        g.DrawImage(bag, sx - 22, sy + 10, 22, 24);
                }
            }

            // ---- 绘制玩家 ----
            int px = (int)(playerPos.X - ox) - 24;
            int py = (int)(playerPos.Y - oy) - 36;
            if (isCallingOut && tmrAnimate != null && tmrAnimate.Enabled)
                g.DrawImage(picCharacterAnimFrame ? charStomp : charIdle, px, py, 48, 48);
            else
                g.DrawImage(charIdle, px, py, 48, 48);

            // ---- 对话气泡 ----
            if (!string.IsNullOrEmpty(dialogueText))
            {
                const int maxBubbleWidth = 280;
                SizeF size = g.MeasureString(dialogueText, dialogueFont, maxBubbleWidth);
                float bubbleW = size.Width + 10;
                float bubbleH = size.Height + 6;
                float bubbleX = px - 5;
                float bubbleY = py - bubbleH - 10;

                if (bubbleX < 0) bubbleX = 0;
                if (bubbleX + bubbleW > vw) bubbleX = vw - bubbleW;
                if (bubbleY < 0) bubbleY = py + 48;

                g.FillRectangle(Brushes.White, bubbleX, bubbleY, bubbleW, bubbleH);
                g.DrawRectangle(Pens.Black, bubbleX, bubbleY, bubbleW, bubbleH);
                g.DrawString(dialogueText, dialogueFont, Brushes.Black,
                    new RectangleF(bubbleX + 5, bubbleY + 3, bubbleW - 10, bubbleH - 6));
            }
        }

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
                    GameManager.SubmitAccusation("A", accused);
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
            btnAccept.Click += (s, e) => { PhoneManager.AcceptCall("A"); };
            btnDecline.Click += (s, e) => { PhoneManager.DeclineCall("A"); pnlIncoming.Visible = false; StopRingingUI(); };
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

            PhoneManager.OnCallRequest += PhoneManager_OnCallRequest;
            PhoneManager.OnCallEstablished += PhoneManager_OnCallEstablished;
            PhoneManager.OnCallEnded += PhoneManager_OnCallEnded;
            PhoneManager.OnRingTimeout += PhoneManager_OnRingTimeout;
        }

        private void StopRingingUI() { tmrAnimate.Stop(); tmrTimeout.Stop(); tmrProgress.Stop(); isCallingOut = false; pnlIncoming.Visible = false; lblBubble.Visible = false; }
        private void TmrTimeout_Tick(object sender, EventArgs e) { tmrTimeout.Stop(); PhoneManager.TimeoutCall(); }
        private void TmrProgress_Tick(object sender, EventArgs e) { float ratio = 1f - (float)(DateTime.Now - callStartTime).TotalSeconds / 3f; if (ratio < 0) ratio = 0; pgbTimeout.Width = (int)(160 * ratio); }

        private void PhoneManager_OnCallRequest(string caller, string callee)
        {
            if (InvokeRequired) { Invoke(new Action<string, string>(PhoneManager_OnCallRequest), caller, callee); return; }
            if (callee == "A") { pnlIncoming.Visible = true; callStartTime = DateTime.Now; tmrProgress.Start(); tmrTimeout.Start(); }
        }
        private void PhoneManager_OnCallEstablished()
        {
            if (InvokeRequired) { Invoke(new Action(PhoneManager_OnCallEstablished)); return; }
            StopRingingUI(); currentChatForm = new FormChat("A"); currentChatForm.FormClosed += (s, e) => PhoneManager.HangUp("A"); currentChatForm.Show();
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

        // ==================== 线索同步 ====================
        private void FormPlayerA_Load(object sender, EventArgs e)
        {
            GameManager.OnClueDiscovered += GameManager_OnClueDiscovered;
            GameManager.OnSafeUnlocked += GameManager_OnSafeUnlocked;
            GameManager.OnAccusationResult += GameManager_OnAccusationResult;
            foreach (var c in GameManager.AllClues)
                if (c.IsDiscovered && c.DiscoveredBy == "A")
                    lstCluesA.Items.Add(c.Name);
        }
        private void GameManager_OnClueDiscovered(string id)
        {
            if (InvokeRequired) { Invoke(new Action<string>(GameManager_OnClueDiscovered), id); return; }
            var cl = GameManager.AllClues.FirstOrDefault(c => c.Id == id);
            if (cl != null && cl.DiscoveredBy == "A" && !lstCluesA.Items.Contains(cl.Name))
                lstCluesA.Items.Add(cl.Name);
        }
        private void GameManager_OnSafeUnlocked()
        {
            if (InvokeRequired) { Invoke(new Action(GameManager_OnSafeUnlocked)); return; }
            MessageBox.Show("保险箱打开了！里面有一份遗嘱和一封举报信，已自动记录为线索。请继续调查并指认真凶。", "保险箱已开");
            // 不再关闭窗口，游戏继续
        }
        private void GameManager_OnAccusationResult(bool bothCorrect)
        {
            if (InvokeRequired) { Invoke(new Action<bool>(GameManager_OnAccusationResult), bothCorrect); return; }

            // 立即缓存本次指认值（后续 ResetAccusation 不会影响这两个本地变量）
            string accA = GameManager.LastAccusationA;
            string accB = GameManager.LastAccusationB;

            if (bothCorrect)
            {
                if (!GameManager.ResultMessageShown)
                {
                    GameManager.ResultMessageShown = true;
                    // 显示全屏结局画面（TopMost 会覆盖一切）
                    var ending = new FormEnding();
                    ending.FormClosed += (s2, e2) =>
                    {
                        // 结局播放完毕，关闭所有游戏窗口
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
                // 无论是否弹窗，双方都重置为可重新指认状态
                hasAccused = false;
                btnAccuse.Enabled = true;
                GameManager.ResetAccusation();
            }
        }
        private void FormPlayerA_FormClosing(object sender, FormClosingEventArgs e)
        {
            GameManager.OnClueDiscovered -= GameManager_OnClueDiscovered;
            GameManager.OnSafeUnlocked -= GameManager_OnSafeUnlocked;
            GameManager.OnAccusationResult -= GameManager_OnAccusationResult;
            PhoneManager.OnCallRequest -= PhoneManager_OnCallRequest;
            PhoneManager.OnCallEstablished -= PhoneManager_OnCallEstablished;
            PhoneManager.OnCallEnded -= PhoneManager_OnCallEnded;
            PhoneManager.OnRingTimeout -= PhoneManager_OnRingTimeout;
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