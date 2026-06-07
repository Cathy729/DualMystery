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
        private Timer tmrDialogue;

        // 缓存 GDI 对象，避免 Paint 中反复创建
        private Font itemFont = new Font("Georgia", 7);
        private Font dialogueFont = new Font("Georgia", 8);
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
                Font = new Font("Georgia", 14f),
                ForeColor = Color.FromArgb(201, 169, 110),
                Dock = DockStyle.Top,
                Height = 36,
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
                Height = 50,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.LightGray,
                Font = new Font("Georgia", 7f)
            };
            rightPanel.Controls.Add(lblStory);

            GroupBox gbNotes = new GroupBox { Text = "线索笔记", Height = 150, Dock = DockStyle.Top, ForeColor = Color.White };
            lstCluesB = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.None };
            gbNotes.Controls.Add(lstCluesB);
            rightPanel.Controls.Add(gbNotes);

            GroupBox gbTimeline = new GroupBox { Text = "时间线", Height = 150, Dock = DockStyle.Top, ForeColor = Color.White };
            lstTimeline = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.FromArgb(201, 169, 110), BorderStyle = BorderStyle.None, Font = new Font("Georgia", 7f) };
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
                Font = new Font("Georgia", 10f, FontStyle.Bold)
            };
            btnAccuse.Click += BtnAccuse_Click;
            rightPanel.Controls.Add(btnAccuse);

            this.Controls.Add(rightPanel);

            canvas = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 26, 30) };
            canvas.Paint += Canvas_Paint;
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
            switch (e.KeyCode)
            {
                case Keys.Up: moveUp = true; break;
                case Keys.Down: moveDown = true; break;
                case Keys.Left: moveLeft = true; break;
                case Keys.Right: moveRight = true; break;
                case Keys.P: Interact(); break;
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

        private void Interact()
        {
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
            foreach (var item in sceneItems)
            {
                Rectangle d = item.Rect;
                d.Inflate(40, 40);
                if (d.Contains((int)playerPos.X, (int)playerPos.Y))
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

        private void HandleItemInteraction(SceneItem item)
        {
            if (item.IsPhone)
            {
                PhoneManager.RequestCall("B");
                isCallingOut = true;
                tmrAnimate.Start();
                tmrTimeout.Start();
                callStartTime = DateTime.Now;
            }
            else
            {
                GameManager.DiscoverClue(item.ClueId, "B");
                var clue = GameManager.AllClues.FirstOrDefault(c => c.Id == item.ClueId);
                if (clue != null) MessageBox.Show(clue.Description, clue.Name);
                if (item.ClueId == "photo") AddTimeline("照片背后写有数字19");
                if (item.ClueId == "calendar") AddTimeline("日历圈起12月25日");
                if (item.ClueId == "pawn_ticket") AddTimeline("当票签名Edgar Blackwood");
                if (item.ClueId == "key") AddTimeline("发现一把小钥匙");
            }
        }

        // ========== 绘制 ==========
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            int vw = canvas.Width, vh = canvas.Height;
            float ox = playerPos.X - vw / 2f, oy = playerPos.Y - vh / 2f;
            ox = Math.Max(0, Math.Min(ox, mapWidth - vw));
            oy = Math.Max(0, Math.Min(oy, mapHeight - vh));

            // ---- 背景 ----
            g.FillRectangle(new SolidBrush(Color.FromArgb(48, 44, 40)), 0, 0, mapWidth, mapHeight);
            // 墙裙
            g.FillRectangle(new SolidBrush(Color.FromArgb(38, 34, 32)), 0, 360 - (int)oy, mapWidth, 440);
            g.DrawLine(new Pen(Color.FromArgb(100, 80, 50), 2), 0, 360 - (int)oy, mapWidth, 360 - (int)oy);

            // ---- 地毯通道 ----
            using (Bitmap carpetTile = PixelIcons.CreateCarpet())
            {
                for (int cx = 100; cx < 1100; cx += 32)
                    g.DrawImage(carpetTile, cx - (int)ox, 540 - (int)oy, 32, 20);
            }

            // ---- 像素壁灯（沿走廊两侧分布，暖黄光晕闪烁） ----
            int lampFlicker = (animFrame % 30 < 15 ? 0 : 40); // 缓慢闪烁
            for (int lx = 150; lx < 1100; lx += 250)
            {
                int lsx = lx - (int)ox, lsy = 190 - (int)oy;
                if (lsx + 32 < 0 || lsx > vw) continue;
                // 灯光辉晕（半透明暖黄色，向外扩散）
                using (Brush glow1 = new SolidBrush(Color.FromArgb(40 + lampFlicker / 2, 255, 240, 180)))
                    g.FillEllipse(glow1, lsx - 8, lsy - 2, 44, 40);
                using (Brush glow2 = new SolidBrush(Color.FromArgb(70 + lampFlicker, 255, 220, 140)))
                    g.FillEllipse(glow2, lsx - 2, lsy + 4, 32, 28);
                // 壁灯底座（金属托架）
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 50, 40)), lsx + 2, lsy + 20, 24, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 70, 55)), lsx + 4, lsy + 20, 10, 3);
                // 灯体（玻璃灯罩，梯形）
                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 35, 30)), lsx + 6, lsy + 8, 16, 14);
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 55, 45)), lsx + 4, lsy + 6, 20, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 45, 35)), lsx + 8, lsy + 20, 12, 2);
                // 蜡烛光（内部发光核心）
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 220, 140)), lsx + 10, lsy + 12, 8, 6);
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 180, 60)), lsx + 12, lsy + 14, 4, 4);
                // 灯体高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, 110, 90)), lsx + 6, lsy + 8, 3, 10);
                // 顶部装饰
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 65, 50)), lsx + 8, lsy + 2, 12, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 85, 65)), lsx + 10, lsy, 8, 4);
                // 挂钩/链条
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 50, 40)), lsx + 12, lsy - 4, 4, 6);
            }

            // ---- 多幅挂画 ----
            for (int px2 = 350; px2 < 1000; px2 += 280)
            {
                int paintX = px2 - (int)ox, paintY = 140 - (int)oy;
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 160, 110)), paintX, paintY, 40, 50);
                g.DrawRectangle(new Pen(Color.FromArgb(60, 50, 30), 2), paintX, paintY, 40, 50);
                // 画中内容（简单风景色块）
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 140, 180)), paintX + 4, paintY + 6, 32, 15);
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 120, 60)), paintX + 4, paintY + 21, 32, 12);
                g.FillEllipse(new SolidBrush(Color.FromArgb(240, 220, 140)), paintX + 22, paintY + 8, 10, 10);
            }

            // ---- 花瓶（走廊尽头） ----
            int vaseX = 1050 - (int)ox, vaseY = 420 - (int)oy;
            using (Bitmap vase = PixelIcons.CreateVase())
                g.DrawImage(vase, vaseX, vaseY, 32, 40);
            // 底座
            g.FillRectangle(new SolidBrush(Color.FromArgb(80, 60, 40)), vaseX - 4, vaseY + 38, 40, 10);

            // ---- 绘制物品 ----
            foreach (var item in sceneItems)
            {
                int sx = item.Rect.X - (int)ox, sy = item.Rect.Y - (int)oy;
                if (sx + item.Rect.Width < 0 || sx > vw || sy + item.Rect.Height < 0 || sy > vh) continue;
                if (item.Icon != null) g.DrawImage(item.Icon, sx, sy, item.Rect.Width, item.Rect.Height);
                else { g.FillRectangle(Brushes.DarkOliveGreen, sx, sy, item.Rect.Width, item.Rect.Height); g.DrawRectangle(Pens.Black, sx, sy, item.Rect.Width, item.Rect.Height); }
                g.DrawString(item.Name, itemFont, Brushes.White, sx, sy - 12);
            }

            // ---- 绘制 NPC + 道具 ----
            foreach (var npc in npcList)
            {
                int sx = npc.Rect.X - (int)ox, sy = npc.Rect.Y - (int)oy;
                if (npc.Icon != null) g.DrawImage(npc.Icon, sx, sy, npc.Rect.Width, npc.Rect.Height);
                g.DrawString(npc.Name, itemFont, Brushes.Yellow, sx, sy - 12);

                if (npc.Name.Contains("埃德加"))
                {
                    using (Bitmap wine = PixelIcons.CreateWineGlass())
                        g.DrawImage(wine, sx + 40, sy + 14, 18, 22);
                }
                else if (npc.Name.Contains("莫里斯"))
                {
                    using (Bitmap keys = PixelIcons.CreateKeyRing())
                        g.DrawImage(keys, sx - 20, sy + 12, 20, 22);
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
                    GameManager.SubmitAccusation("B", accused);
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
            btnAccept.Click += (s, e) => { PhoneManager.AcceptCall("B"); };
            btnDecline.Click += (s, e) => { PhoneManager.DeclineCall("B"); pnlIncoming.Visible = false; StopRingingUI(); };
            pnlIncoming.Controls.Add(lblIncoming); pnlIncoming.Controls.Add(btnAccept); pnlIncoming.Controls.Add(btnDecline); pnlIncoming.Controls.Add(pgbTimeout);

            this.Controls.Add(pnlIncoming);
            pnlIncoming.Location = new Point((this.ClientSize.Width - 160) / 2, this.ClientSize.Height / 2 - 40);
            pnlIncoming.BringToFront();

            lblBubble = new Label { Text = "无人接听...", ForeColor = Color.White, BackColor = Color.Black, Visible = false, AutoSize = true };
            this.Controls.Add(lblBubble);
            lblBubble.Location = new Point((this.ClientSize.Width - lblBubble.Width) / 2, pnlIncoming.Top - 30);
            lblBubble.BringToFront();

            this.Resize += (s, e) => { if (pnlIncoming.Visible) { pnlIncoming.Location = new Point((this.ClientSize.Width - 160) / 2, this.ClientSize.Height / 2 - 40); lblBubble.Location = new Point((this.ClientSize.Width - lblBubble.Width) / 2, pnlIncoming.Top - 30); } };

            PhoneManager.OnCallRequest += PhoneManager_OnCallRequest;
            PhoneManager.OnCallEstablished += PhoneManager_OnCallEstablished;
            PhoneManager.OnCallEnded += PhoneManager_OnCallEnded;
            PhoneManager.OnRingTimeout += PhoneManager_OnRingTimeout;
        }

        private void StopRingingUI() { tmrAnimate.Stop(); tmrTimeout.Stop(); tmrProgress.Stop(); isCallingOut = false; pnlIncoming.Visible = false; lblBubble.Visible = false; }
        private void TmrTimeout_Tick(object sender, EventArgs e) { tmrTimeout.Stop(); PhoneManager.TimeoutCall(); }
        private void TmrProgress_Tick(object sender, EventArgs e) { float r = 1f - (float)(DateTime.Now - callStartTime).TotalSeconds / 3f; if (r < 0) r = 0; pgbTimeout.Width = (int)(160 * r); }
        private void PhoneManager_OnCallRequest(string caller, string callee) { if (InvokeRequired) { Invoke(new Action<string, string>(PhoneManager_OnCallRequest), caller, callee); return; } if (callee == "B") { pnlIncoming.Visible = true; callStartTime = DateTime.Now; tmrProgress.Start(); tmrTimeout.Start(); } }
        private void PhoneManager_OnCallEstablished() { if (InvokeRequired) { Invoke(new Action(PhoneManager_OnCallEstablished)); return; } StopRingingUI(); currentChatForm = new FormChat("B"); currentChatForm.FormClosed += (s, e) => PhoneManager.HangUp("B"); currentChatForm.Show(); }
        private void PhoneManager_OnCallEnded() { if (InvokeRequired) { Invoke(new Action(PhoneManager_OnCallEnded)); return; } StopRingingUI(); if (currentChatForm != null && !currentChatForm.IsDisposed) { currentChatForm.Close(); currentChatForm = null; } }
        private void PhoneManager_OnRingTimeout(string caller) { if (InvokeRequired) { Invoke(new Action<string>(PhoneManager_OnRingTimeout), caller); return; } StopRingingUI(); if (caller == "B") { lblBubble.Visible = true; tmrBubble.Start(); } }

        // ========== 事件订阅 ==========
        private void FormPlayerB_Load(object sender, EventArgs e)
        {
            GameManager.OnClueDiscovered += GameManager_OnClueDiscovered;
            GameManager.OnSafeUnlocked += GameManager_OnSafeUnlocked;
            GameManager.OnAccusationResult += GameManager_OnAccusationResult;
            foreach (var c in GameManager.AllClues) if (c.IsDiscovered && c.DiscoveredBy == "B") lstCluesB.Items.Add(c.Name);
        }
        private void GameManager_OnClueDiscovered(string id)
        {
            if (InvokeRequired) { Invoke(new Action<string>(GameManager_OnClueDiscovered), id); return; }
            var cl = GameManager.AllClues.FirstOrDefault(c => c.Id == id);
            if (cl != null && cl.DiscoveredBy == "B" && !lstCluesB.Items.Contains(cl.Name)) lstCluesB.Items.Add(cl.Name);
        }
        private void GameManager_OnSafeUnlocked()
        {
            if (InvokeRequired) { Invoke(new Action(GameManager_OnSafeUnlocked)); return; }
            MessageBox.Show("书房传来了金属响声，保险箱打开了！遗嘱和举报信已自动记录。", "保险箱已开");
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
                // 无论是否弹窗，双方都重置为可重新指认状态
                hasAccused = false;
                btnAccuse.Enabled = true;
                GameManager.ResetAccusation();
            }
        }
        private void FormPlayerB_FormClosing(object sender, FormClosingEventArgs e)
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