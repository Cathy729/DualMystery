using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DualMystery
{
    /// <summary>
    /// 结局播片窗体 —— 全屏覆盖，逐帧像素动画 + 对话剧情
    /// </summary>
    public partial class FormEnding : Form
    {
        private PictureBox canvas;
        private Panel pnlDialogue;
        private Label lblDialogue;
        private Label lblSpeaker;
        private Timer beatTimer;
        private Timer animTimer;

        // 结局角色像素图 (48×48 放大到 96×96)
        private Bitmap sprDetectiveA, sprDetectiveB;
        private Bitmap sprMorris, sprBetty, sprGrey, sprEdgar;
        private Bitmap sprPolice1, sprPolice2;

        // 动画状态
        private int currentBeat = 0;
        private float animProgress = 0f;     // 0~1 当前 Beat 内的动画进度
        private float fadeAlpha = 0f;         // 淡入淡出
        private float policeX = 1200f;        // 警察 X 坐标（从右侧滑入）
        private float morrisTargetX = 0f;      // 莫里斯被带走的目标偏移
        private float detectiveScale = 0.8f;   // 侦探庆祝缩放

        private readonly List<StoryBeat> beats;

        public FormEnding()
        {
            InitializeComponent();
            GenerateSprites();
            beats = BuildStoryBeats();
            InitializeCustomUI();
            this.Load += (s, e) => StartEnding();
        }

        // ==================== 故事节拍定义 ====================
        private List<StoryBeat> BuildStoryBeats()
        {
            return new List<StoryBeat>
            {
                new StoryBeat { Speaker = "", Text = "", Duration = 2.0f, Type = BeatType.FadeIn },
                new StoryBeat { Speaker = "警察局长", Text = "经过两位侦探的缜密调查，霍华德·布莱克伍德谋杀案的真相终于水落石出。", Duration = 4.5f, Type = BeatType.Gather },
                new StoryBeat { Speaker = "警察局长", Text = "凶手就在这间屋子里。莫里斯管家，你有什么想说的吗？", Duration = 3.5f, Type = BeatType.Gather },
                new StoryBeat { Speaker = "莫里斯", Text = "……是我做的。我承认。", Duration = 3.0f, Type = BeatType.MorrisStep },
                new StoryBeat { Speaker = "莫里斯", Text = "霍华德老爷发现了我在当铺销赃的事。那晚他把我叫到书房，说要明天一早就报警……", Duration = 5.0f, Type = BeatType.MorrisConfess },
                new StoryBeat { Speaker = "莫里斯", Text = "我慌了。桌上的刀……我只是想让他闭嘴。等我回过神，他已经倒在血泊里了。", Duration = 5.0f, Type = BeatType.MorrisConfess },
                new StoryBeat { Speaker = "警察", Text = "莫里斯，你因涉嫌谋杀霍华德·布莱克伍德被正式逮捕。带走。", Duration = 3.5f, Type = BeatType.PoliceEnter },
                new StoryBeat { Speaker = "", Text = "两名警官给莫里斯戴上手铐，将他押出了房间。", Duration = 4.0f, Type = BeatType.Arrest },
                new StoryBeat { Speaker = "侦探A", Text = "看来我们的推理是正确的——保险箱里的遗嘱和举报信，还有走廊的当票，都指向了同一个人。", Duration = 5.0f, Type = BeatType.Celebrate },
                new StoryBeat { Speaker = "侦探B", Text = "没有你的配合，我也不可能找到走廊里的那些关键线索。这是我们共同的胜利。", Duration = 5.0f, Type = BeatType.Celebrate },
                new StoryBeat { Speaker = "贝蒂", Text = "谢谢你们……我终于不用再害怕了。那晚我确实看到了不该看的东西，但我不敢说。", Duration = 5.0f, Type = BeatType.Epilogue },
                new StoryBeat { Speaker = "格雷医生", Text = "我的安眠药是无辜的。职业操守终究没有被辜负。", Duration = 4.0f, Type = BeatType.Epilogue },
                new StoryBeat { Speaker = "埃德加", Text = "哥哥……他虽然脾气古怪，但不该落得如此下场。至少真相大白了。", Duration = 4.5f, Type = BeatType.Epilogue },
                new StoryBeat { Speaker = "", Text = "案件告破。正义或许会迟到，但从不缺席。\n\n—— 双线谜案 · 完 ——", Duration = 5.0f, Type = BeatType.Finale },
            };
        }

        // ==================== 生成结局像素角色 (32×32 → 96×96 放大) ====================
        private void GenerateSprites()
        {
            sprDetectiveA = MakeCharacter(48, Color.DarkBlue, Color.FromArgb(255, 224, 189));
            sprDetectiveB = MakeCharacter(48, Color.DarkRed, Color.FromArgb(255, 224, 189));
            sprMorris = MakeCharacter(48, Color.FromArgb(80, 80, 90), Color.FromArgb(255, 224, 189));
            sprBetty = MakeCharacter(48, Color.FromArgb(180, 140, 160), Color.FromArgb(255, 224, 189));
            sprGrey = MakeCharacter(48, Color.FromArgb(60, 60, 70), Color.FromArgb(255, 224, 189));
            sprEdgar = MakeCharacter(48, Color.FromArgb(50, 50, 80), Color.FromArgb(255, 224, 189));
            sprPolice1 = MakePolice(48);
            sprPolice2 = MakePolice(48);
        }

        private Bitmap MakeCharacter(int size, Color bodyColor, Color skinColor)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                int s = size / 3; // 基础单元
                // 阴影 (脚底)
                using (Brush sh = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                    g.FillEllipse(sh, s / 2, size - s / 3, s * 2, s / 3);
                // 腿
                using (Brush lb = new SolidBrush(Color.FromArgb(40, 40, 40)))
                {
                    g.FillRectangle(lb, s, size - s, s / 2, s / 2);
                    g.FillRectangle(lb, s + s / 2, size - s, s / 2, s / 2);
                }
                // 身体
                using (Brush bb = new SolidBrush(bodyColor))
                    g.FillRectangle(bb, s / 2, size / 2, s * 2, s);
                // 身体高光
                using (Brush hl = new SolidBrush(ControlPaint.Light(bodyColor, 0.3f)))
                    g.FillRectangle(hl, s / 2, size / 2, s / 3, s);
                // 头
                using (Brush sb = new SolidBrush(skinColor))
                    g.FillRectangle(sb, s / 2 + s / 4, size / 4, s + s / 2, s);
                // 眼睛
                g.FillRectangle(Brushes.White, s, size / 3, s / 4, s / 4);
                g.FillRectangle(Brushes.Black, s + s / 6, size / 3, s / 5, s / 4);
                g.FillRectangle(Brushes.White, s + s / 2, size / 3, s / 4, s / 4);
                g.FillRectangle(Brushes.Black, s + s / 2 + s / 6, size / 3, s / 5, s / 4);
                // 帽子 (简单的深色方块)
                using (Brush hb = new SolidBrush(ControlPaint.Dark(bodyColor, 0.2f)))
                    g.FillRectangle(hb, s / 2, size / 5, s * 2, s / 3);
            }
            // 放大到 96×96
            return ScaleSprite(bmp, 96);
        }

        private Bitmap MakePolice(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                int s = size / 3;
                Color uniform = Color.FromArgb(30, 50, 100);
                Color skin = Color.FromArgb(255, 224, 189);
                using (Brush sh = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                    g.FillEllipse(sh, s / 2, size - s / 3, s * 2, s / 3);
                using (Brush lb = new SolidBrush(Color.FromArgb(20, 30, 60)))
                {
                    g.FillRectangle(lb, s, size - s, s / 2, s / 2);
                    g.FillRectangle(lb, s + s / 2, size - s, s / 2, s / 2);
                }
                using (Brush ub = new SolidBrush(uniform))
                    g.FillRectangle(ub, s / 2, size / 2, s * 2, s);
                using (Brush hl = new SolidBrush(ControlPaint.Light(uniform, 0.3f)))
                    g.FillRectangle(hl, s / 2, size / 2, s / 3, s);
                using (Brush sb = new SolidBrush(skin))
                    g.FillRectangle(sb, s / 2 + s / 4, size / 4, s + s / 2, s);
                // 警帽 (带帽檐)
                using (Brush hb = new SolidBrush(Color.FromArgb(20, 35, 70)))
                {
                    g.FillRectangle(hb, s / 2, size / 5, s * 2, s / 3);
                    g.FillRectangle(hb, s / 4, size / 5 + s / 4, s * 2 + s / 2, s / 5);
                }
                // 金色警徽
                g.FillEllipse(Brushes.Gold, s + s / 3, size / 2 + s / 6, s / 2, s / 2);
                g.FillRectangle(Brushes.White, s, size / 3, s / 4, s / 4);
                g.FillRectangle(Brushes.Black, s + s / 6, size / 3, s / 5, s / 4);
                g.FillRectangle(Brushes.White, s + s / 2, size / 3, s / 4, s / 4);
                g.FillRectangle(Brushes.Black, s + s / 2 + s / 6, size / 3, s / 5, s / 4);
            }
            return ScaleSprite(bmp, 96);
        }

        private Bitmap ScaleSprite(Bitmap src, int targetSize)
        {
            Bitmap result = new Bitmap(targetSize, targetSize);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(src, new Rectangle(0, 0, targetSize, targetSize),
                    new Rectangle(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);
            }
            return result;
        }

        // ==================== UI ====================
        private void InitializeCustomUI()
        {
            this.Text = "双线谜案 · 结局";
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;

            // 主画布
            canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            canvas.Paint += Canvas_Paint;
            this.Controls.Add(canvas);

            // 底部对话面板
            pnlDialogue = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 140,
                BackColor = Color.FromArgb(220, 15, 15, 20)
            };
            lblSpeaker = new Label
            {
                Location = new Point(60, 10),
                AutoSize = true,
                ForeColor = Color.FromArgb(201, 169, 110),
                Font = new Font("Georgia", 13f, FontStyle.Bold),
                Text = ""
            };
            lblDialogue = new Label
            {
                Location = new Point(60, 45),
                Size = new Size(pnlDialogue.Width - 120, 80),
                ForeColor = Color.FromArgb(245, 240, 230),
                Font = new Font("Georgia", 12f),
                Text = ""
            };
            pnlDialogue.Controls.Add(lblSpeaker);
            pnlDialogue.Controls.Add(lblDialogue);
            this.Controls.Add(pnlDialogue);
            pnlDialogue.BringToFront();

            // 点击任意位置推进
            this.Click += (s, e) => AdvanceBeat();
            canvas.Click += (s, e) => AdvanceBeat();
            pnlDialogue.Click += (s, e) => AdvanceBeat();
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) AdvanceBeat(); };

            // 动画计时器 30fps
            animTimer = new Timer { Interval = 33 };
            animTimer.Tick += AnimTimer_Tick;

            // 节拍计时器
            beatTimer = new Timer { Interval = 100 };
            beatTimer.Tick += (s, e) =>
            {
                if (currentBeat < beats.Count &&
                    beats[currentBeat].Elapsed >= beats[currentBeat].Duration)
                {
                    AdvanceBeat();
                }
                else if (currentBeat < beats.Count)
                {
                    beats[currentBeat].Elapsed += 0.1f;
                }
            };

            this.Resize += (s, e) =>
            {
                lblDialogue.Size = new Size(pnlDialogue.Width - 120, 80);
                canvas.Invalidate();
            };
        }

        private void StartEnding()
        {
            animTimer.Start();
            beatTimer.Start();
        }

        private void AdvanceBeat()
        {
            currentBeat++;
            if (currentBeat >= beats.Count)
            {
                // 结局结束
                beatTimer.Stop();
                animTimer.Stop();
                var t = new Timer { Interval = 2000 };
                t.Tick += (s, e) => { t.Stop(); this.Close(); };
                t.Start();
                return;
            }
            // 重置当前 Beat 计时
            beats[currentBeat].Elapsed = 0f;
            animProgress = 0f;
            canvas.Invalidate();
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (currentBeat >= beats.Count) return;
            var beat = beats[currentBeat];
            animProgress = Math.Min(1f, beat.Elapsed / (float)beat.Duration);

            int cw = canvas.Width;
            int cx = cw / 2;

            // 根据不同 Beat 类型驱动动画参数
            switch (beat.Type)
            {
                case BeatType.FadeIn:
                    fadeAlpha = Math.Min(1f, animProgress * 2f);
                    break;
                case BeatType.Gather:
                case BeatType.MorrisStep:
                case BeatType.MorrisConfess:
                    // 所有人就位，无动画
                    policeX = cw + 200f; // 警察在屏幕右侧外等待
                    morrisTargetX = 0f;
                    detectiveScale = 0.8f;
                    break;
                case BeatType.PoliceEnter:
                    // 警察从右侧滑入，走到莫里斯身后位置
                    policeX = cw + 200f - animProgress * (cw + 200f - (cx + 300f));
                    break;
                case BeatType.Arrest:
                    // 莫里斯被警察押送向右移动，逐步移出屏幕
                    morrisTargetX = animProgress * 600f; // 莫里斯向右走600px
                    policeX = cx + 280f + animProgress * 500f; // 警察紧随押送
                    break;
                case BeatType.Celebrate:
                case BeatType.Epilogue:
                    // 莫里斯已被带走，不再出现；警察站在右侧
                    detectiveScale = 0.8f + (float)Math.Sin(animProgress * Math.PI * 2) * 0.1f;
                    policeX = cx + 500f;
                    // morrisTargetX 保持上次 Arrest 的值，不重置
                    break;
                case BeatType.Finale:
                    fadeAlpha = 1f - animProgress * 0.3f;
                    policeX = cx + 500f;
                    break;
            }
            canvas.Invalidate();
        }

        // ==================== 场景绘制 ====================
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            int cw = canvas.Width, ch = canvas.Height;

            // 背景
            g.Clear(Color.FromArgb(20, 18, 22));

            if (currentBeat >= beats.Count) return;

            var beat = beats[currentBeat];

            // 绘制地面
            int floorY = ch - 250;
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 35, 30)), 0, floorY, cw, 250);
            g.DrawLine(new Pen(Color.FromArgb(60, 50, 40), 2), 0, floorY, cw, floorY);

            // 中心线
            int cx = cw / 2;

            if (beat.Type == BeatType.FadeIn)
            {
                // 仅显示标题
                using (Font tf = new Font("Georgia", 48f, FontStyle.Bold))
                {
                    string title = "案 件 告 破";
                    SizeF ts = g.MeasureString(title, tf);
                    int alpha = (int)(fadeAlpha * 255);
                    using (Brush tb = new SolidBrush(Color.FromArgb(alpha, 201, 169, 110)))
                        g.DrawString(title, tf, tb, cx - ts.Width / 2, ch / 2 - ts.Height);
                }
                return;
            }

            // 通用：绘制所有角色
            // 布局说明：两位侦探在左侧，NPC 在右侧，间距充足避免重叠
            bool isArrested = beat.Type >= BeatType.Celebrate; // 莫里斯是否已被逮捕

            // 侦探（左侧）
            float detScale = (beat.Type == BeatType.Celebrate || beat.Type == BeatType.Epilogue) ? detectiveScale : 0.8f;
            int detSize = (int)(96 * detScale);
            int detY = floorY - detSize;
            DrawCharacterScaled(g, sprDetectiveA, cx - 520, detY, detSize, detSize, beat.Type == BeatType.Celebrate);
            DrawCharacterScaled(g, sprDetectiveB, cx - 360, detY, detSize, detSize, beat.Type == BeatType.Celebrate);

            // NPC（右侧，从左到右排列）
            DrawCharacter(g, sprBetty, cx - 140, floorY - 96, "贝蒂");
            DrawCharacter(g, sprGrey, cx + 10, floorY - 96, "格雷医生");
            DrawCharacter(g, sprEdgar, cx + 150, floorY - 96, "埃德加");

            // 莫里斯 —— 仅在逮捕前显示；Arrest 阶段向右移动移出屏幕
            if (!isArrested || beat.Type == BeatType.Arrest)
            {
                float morrisX = cx + 320f + morrisTargetX;
                DrawCharacter(g, sprMorris, (int)morrisX, floorY - 96, "莫里斯");
            }

            // 警察 —— PoliceEnter 之后各阶段均显示
            if (beat.Type == BeatType.PoliceEnter || beat.Type == BeatType.Arrest || isArrested)
            {
                float p1x = policeX;
                float p2x = policeX + 110f;
                DrawCharacter(g, sprPolice1, (int)p1x, floorY - 96, "");
                DrawCharacter(g, sprPolice2, (int)p2x, floorY - 96, "");
            }

            // 上方场景标签
            string sceneHint = "";
            switch (beat.Type)
            {
                case BeatType.Gather: sceneHint = "—— 布莱克伍德庄园 · 大厅 ——"; break;
                case BeatType.MorrisStep: case BeatType.MorrisConfess: sceneHint = "—— 莫里斯颤抖着走上前 ——"; break;
                case BeatType.PoliceEnter: sceneHint = "—— 警察破门而入 ——"; break;
                case BeatType.Arrest: sceneHint = "—— 莫里斯被押走 ——"; break;
                case BeatType.Celebrate: sceneHint = "—— 两位侦探握手庆祝 ——"; break;
                case BeatType.Epilogue: sceneHint = "—— 真相大白，各人各归其位 ——"; break;
                case BeatType.Finale: sceneHint = ""; break;
            }
            if (!string.IsNullOrEmpty(sceneHint))
            {
                using (Font sf = new Font("Georgia", 10f, FontStyle.Italic))
                using (Brush sb = new SolidBrush(Color.FromArgb(150, 150, 150)))
                    g.DrawString(sceneHint, sf, sb, cx - 200, floorY - 130);
            }

            // 结局文字叠加
            if (beat.Type == BeatType.Finale)
            {
                int alpha = (int)((1f - animProgress * 0.2f) * 255);
                using (Font ff = new Font("Georgia", 28f, FontStyle.Bold))
                using (Brush fb = new SolidBrush(Color.FromArgb(alpha, 201, 169, 110)))
                {
                    string fin = "—— 双线谜案 · 完 ——";
                    SizeF fs = g.MeasureString(fin, ff);
                    g.DrawString(fin, ff, fb, cx - fs.Width / 2, floorY - 200);
                }
            }

            // 更新对话文本
            if (lblSpeaker.Text != beat.Speaker || lblDialogue.Text != beat.Text)
            {
                lblSpeaker.Text = beat.Speaker;
                lblDialogue.Text = beat.Text;
            }
        }

        private void DrawCharacter(Graphics g, Bitmap sprite, int x, int y, string name)
        {
            if (sprite != null)
                g.DrawImage(sprite, x, y, 96, 96);
            if (!string.IsNullOrEmpty(name))
            {
                using (Font nf = new Font("Georgia", 8f))
                using (Brush nb = new SolidBrush(Color.FromArgb(200, 200, 200)))
                {
                    SizeF ns = g.MeasureString(name, nf);
                    g.DrawString(name, nf, nb, x + 48 - ns.Width / 2, y + 96);
                }
            }
        }

        private void DrawCharacterScaled(Graphics g, Bitmap sprite, int x, int y, int w, int h, bool bouncing)
        {
            if (sprite != null)
                g.DrawImage(sprite, x, y, w, h);
        }

        // ==================== 内部类型 ====================
        private class StoryBeat
        {
            public string Speaker;
            public string Text;
            public float Duration;   // 秒
            public float Elapsed;
            public BeatType Type;
        }

        private enum BeatType
        {
            FadeIn,          // 标题淡入
            Gather,          // 众人聚集
            MorrisStep,      // 莫里斯上前
            MorrisConfess,   // 莫里斯坦白
            PoliceEnter,     // 警察入场
            Arrest,          // 逮捕带走
            Celebrate,       // 侦探庆祝
            Epilogue,        // 尾声
            Finale           // 结局
        }
    }
}
