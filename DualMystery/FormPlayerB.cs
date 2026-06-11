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
        private Font itemFont = Theme.GetFont(9f);
        private Font dialogueFont = Theme.GetFont(10f);
        private SolidBrush bgBrush = new SolidBrush(Theme.BgDark);
        // 场景绘制缓存（每帧高频使用）
        private SolidBrush bgFillBrushB = new SolidBrush(Color.FromArgb(48, 44, 40));
        private Pen floorLinePenB = new Pen(Color.FromArgb(100, 80, 50), 2);
        private SolidBrush hintBgBrushB = new SolidBrush(Color.FromArgb(160, 0, 0, 0));

        // 装饰动画
        private int animFrame = 0;

        // 地板纹理
        private Bitmap floorTileB;
        private Pen wallBrickPen = new Pen(Color.FromArgb(30, 0x3A, 0x3A, 0x3A), 1);

        // 鼠标悬停高亮
        private int hoveredItemIndex = -1;

        // 点击反馈动画
        private int feedbackItemIndex = -1;
        private Timer tmrFeedback;

        public FormPlayerB()
        {
            InitializeComponent();
            GenerateCharacterBitmaps(out charIdle, out charStomp);
            GenerateNPCBitmaps(out npcEdgar, out npcMorris);

            // 点击反馈计时器（必须在 InitializeCustomUI 之前初始化，避免 Paint 事件触发时 tmrFeedback 为 null）
            tmrFeedback = new Timer { Interval = 500 };
            tmrFeedback.Tick += (s, e) => { tmrFeedback.Stop(); feedbackItemIndex = -1; canvas.Invalidate(); };

            InitializeCustomUI();
            BuildScene();
            BuildNPCs();
            // 地板棋盘纹理（64×64 贴图，4px 格子）
            floorTileB = Theme.CreateCheckerTile(64, 4,
                Color.FromArgb(0x4A, 0x2A, 0x2A), Color.FromArgb(0x3E, 0x22, 0x22));
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
            f1 = PixelCharacters.CreatePlayerB_Idle();
            f2 = PixelCharacters.CreatePlayerB_Stomp();
        }

        private void GenerateNPCBitmaps(out Bitmap edgar, out Bitmap morris)
        {
            edgar  = PixelCharacters.CreateEdgar();
            morris = PixelCharacters.CreateMorris();
        }

        // ========== UI ==========
        private void InitializeCustomUI()
        {
            this.Text = "走廊";
            this.BackColor = Theme.BgMain;
            this.ClientSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(800, 0);
            this.DoubleBuffered = true;

            Label lblTitle = new Label
            {
                Text = Theme.DecorateTitle("🚪 走  廊"),
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
                Text = "方向键移动 | P键调查/对话/拨打电话",
                Font = Theme.GetFont(9f),
                ForeColor = Theme.TextMain,
                BackColor = Color.FromArgb(180, Theme.BgPanel),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(8, 2, 8, 2)
            };
            lblHint.Location = new Point((this.ClientSize.Width - lblHint.PreferredWidth) / 2, lblTitle.Bottom + 4);
            this.Controls.Add(lblHint);

            Panel rightPanel = new Panel { Width = 200, Dock = DockStyle.Right, BackColor = Theme.BgPanel };
            Theme.ApplyTextureBackground(rightPanel, Theme.WoodTexture);
            Theme.StylePanelWithBorder(rightPanel);

            Label lblStory = new Label
            {
                Text = "📜 走廊侦探\n照片、当票、钥匙、日历…\n询问埃德加和莫里斯。",
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgDark,
                ForeColor = Theme.TextMain,
                Font = Theme.GetFont(9f)
            };
            rightPanel.Controls.Add(lblStory);

            GroupBox gbNotes = new GroupBox { Text = "线索笔记", Height = 150, Dock = DockStyle.Top, ForeColor = Theme.Accent, BackColor = Theme.BgPanel };
            Theme.StyleGroupBoxPixel(gbNotes);
            lstCluesB = new ListBox { Dock = DockStyle.Fill };
            Theme.StyleListBox(lstCluesB);
            Theme.ApplyTextureBackground(lstCluesB, Theme.WoodTexture);
            lstCluesB.MouseClick += (s, ev) => ShowItemDetail(lstCluesB);
            gbNotes.Controls.Add(lstCluesB);
            rightPanel.Controls.Add(gbNotes);

            GroupBox gbTimeline = new GroupBox { Text = "时间线", Height = 150, Dock = DockStyle.Top, ForeColor = Theme.Accent, BackColor = Theme.BgPanel };
            Theme.StyleGroupBoxPixel(gbTimeline);
            lstTimeline = new ListBox { Dock = DockStyle.Fill };
            Theme.StyleListBox(lstTimeline);
            Theme.ApplyTextureBackground(lstTimeline, Theme.WoodTexture);
            lstTimeline.MouseClick += (s, ev) => ShowItemDetail(lstTimeline);
            gbTimeline.Controls.Add(lstTimeline);
            rightPanel.Controls.Add(gbTimeline);

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

            canvas = new PictureBox { Dock = DockStyle.Fill, BackColor = Theme.BgDark };
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

        // ========== 鼠标悬停检测 ==========
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            int vw = canvas.Width, vh = canvas.Height;
            float ox = playerPos.X - vw / 2f;
            float oy = playerPos.Y - vh / 2f;
            ox = Math.Max(0, Math.Min(ox, mapWidth - vw));
            oy = Math.Max(0, Math.Min(oy, mapHeight - vh));

            int prev = hoveredItemIndex;
            hoveredItemIndex = -1;

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

            if (prev != hoveredItemIndex)
                canvas.Invalidate();
        }

        /// <summary>触发调查成功反馈动画（✓ 图标 500ms）</summary>
        private void TriggerFeedback(int itemIndex)
        {
            feedbackItemIndex = itemIndex;
            tmrFeedback.Stop();
            tmrFeedback.Start();
            canvas.Invalidate();
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
                    TriggerFeedback(sceneItems.IndexOf(item));
                    SoundManager.PlayDiscovery();
                    PixelMessageBox.Show(clue.Description, clue.Name);
                }
                else SoundManager.PlayError();
            }
            canvas.Invalidate();
        }

        // 场景绘制已迁移至 FormPlayerB_Paint.cs

        private void BtnAccuse_Click(object sender, EventArgs e)
        {
            if (hasAccused) { PixelMessageBox.Show("你已经指认过了，请等待对方。"); return; }
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
            tmrAnimate = new Timer { Interval = 300 }; tmrAnimate.Tick += (s, e) => { picCharacterAnimFrame = !picCharacterAnimFrame; canvas.Invalidate(); SoundManager.PlayRingTick(); };
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
            btnAccept.Click += (s, e) => { gameClient.AcceptCall("B"); };
            btnDecline.Click += (s, e) => { gameClient.DeclineCall("B"); pnlIncoming.Visible = false; StopRingingUI(); };
            pnlIncoming.Controls.Add(lblIncoming); pnlIncoming.Controls.Add(btnAccept); pnlIncoming.Controls.Add(btnDecline); pnlIncoming.Controls.Add(pgbTimeout);

            this.Controls.Add(pnlIncoming);
            pnlIncoming.Location = new Point((this.ClientSize.Width - 160) / 2, this.ClientSize.Height / 2 - 40);
            pnlIncoming.BringToFront();

            lblBubble = new Label { Text = "无人接听...", ForeColor = Theme.TextMain, BackColor = Theme.BgDark, Visible = false, AutoSize = true };
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
        private void PhoneManager_OnCallEstablished() { if (InvokeRequired) { Invoke(new Action(PhoneManager_OnCallEstablished)); return; } StopRingingUI(); SoundManager.PlayCallAccept(); currentChatForm = new FormChat("B", gameClient); currentChatForm.FormClosed += (s, e) => gameClient.HangUp("B"); currentChatForm.Show(); }
        private void PhoneManager_OnCallEnded() { if (InvokeRequired) { Invoke(new Action(PhoneManager_OnCallEnded)); return; } StopRingingUI(); SoundManager.PlayCallEnd(); if (currentChatForm != null && !currentChatForm.IsDisposed) { currentChatForm.Close(); currentChatForm = null; } }
        private void PhoneManager_OnRingTimeout(string caller) { if (InvokeRequired) { Invoke(new Action<string>(PhoneManager_OnRingTimeout), caller); return; } StopRingingUI(); if (caller == "B") { lblBubble.Visible = true; tmrBubble.Start(); } }

        // ========== 网络事件处理 ==========
        private void FormPlayerB_Load(object sender, EventArgs e)
        {
            MusicManager.StartBgm();
            foreach (var c in gameClient.ClueCache)
            {
                bool isMine = c.IsDiscovered && (c.DiscoveredBy == "B" || c.SharedTo == "B");
                if (isMine && !lstCluesB.Items.Contains(c.Name))
                {
                    // 小钥匙已分享给A时，B的笔记本显示特殊名称
                    string displayName = c.Name;
                    if (c.Id == "key" && c.SharedTo == "A")
                        displayName = "小钥匙（已传递给A）";
                    if (!lstCluesB.Items.Contains(displayName))
                        lstCluesB.Items.Add(displayName);
                }
            }
        }
        private void GameClient_OnClueDiscovered(string clueId, string player)
        {
            if (InvokeRequired) { Invoke(new Action(() => GameClient_OnClueDiscovered(clueId, player))); return; }
            var cl = gameClient.ClueCache.FirstOrDefault(c => c.Id == clueId);
            if (cl != null && cl.DiscoveredBy == "B" && !lstCluesB.Items.Contains(cl.Name))
                lstCluesB.Items.Add(cl.Name);
        }

        private void GameClient_OnClueShared(string clueId, string fromPlayer, string toPlayer, string clueName)
        {
            if (InvokeRequired) { Invoke(new Action(() => GameClient_OnClueShared(clueId, fromPlayer, toPlayer, clueName))); return; }
            if (fromPlayer == "B" && clueId == "key")
            {
                // B 分享了小钥匙给 A，B 的笔记本条目变更为已传递状态
                int idx = lstCluesB.FindString("小钥匙");
                if (idx >= 0)
                {
                    lstCluesB.Items[idx] = "小钥匙（已传递给A）";
                }
            }
            else if (toPlayer == "B")
            {
                // B 收到了 A 分享的线索
                if (!lstCluesB.Items.Contains(clueName))
                    lstCluesB.Items.Add(clueName);
            }
        }
        private void GameClient_OnSafeUnlocked()
        {
            if (InvokeRequired) { Invoke(new Action(GameClient_OnSafeUnlocked)); return; }
            // 防止两个客户端同时弹窗
            if (GameManager.SafeMessageShown) return;
            GameManager.SafeMessageShown = true;
            SoundManager.PlaySafeUnlock();
            PixelMessageBox.Show("书房传来了金属响声，保险箱打开了！遗嘱和举报信已自动记录。", "保险箱已开");
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

        private void FormPlayerB_FormClosing(object sender, FormClosingEventArgs e)
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
            bgFillBrushB?.Dispose();
            floorLinePenB?.Dispose();
            hintBgBrushB?.Dispose();
            floorTileB?.Dispose();
            wallBrickPen?.Dispose();
        }

        /// <summary>点击线索列表或时间线项时弹出详情</summary>
        private void ShowItemDetail(ListBox lb)
        {
            if (lb.SelectedItem == null) return;
            string selectedText = lb.SelectedItem.ToString();
            var clue = GameManager.AllClues.FirstOrDefault(c => c.Name == selectedText);
            if (clue != null)
                PixelMessageBox.Show(clue.Description, clue.Name);
            else
                PixelMessageBox.Show(selectedText, "时间线事件");
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