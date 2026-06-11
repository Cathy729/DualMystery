using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DualMystery
{
    /// <summary>
    /// 全局星露谷主题 — 配色、像素字体、控件统一样式
    /// </summary>
    public static class Theme
    {
        // ==================== 配色方案 ====================
        public static readonly Color BgMain   = Color.FromArgb(43, 58, 66);   // #2B3A42 主背景
        public static readonly Color BgPanel  = Color.FromArgb(30, 42, 46);   // #1E2A2E 面板背景
        public static readonly Color TextMain = Color.FromArgb(245, 240, 230); // #F5F0E6 主文字
        public static readonly Color Accent   = Color.FromArgb(201, 169, 110); // #C9A96E 高亮/金色
        public static readonly Color Border   = Color.FromArgb(184, 115, 51);  // #B87333 边框铜色
        public static readonly Color BgButton = Color.FromArgb(58, 76, 84);   // #3A4C54 按钮背景
        public static readonly Color BgInput  = Color.FromArgb(26, 37, 40);   // #1A2528 输入框背景
        public static readonly Color BgDark   = Color.FromArgb(25, 35, 38);   // 暗色面板
        public static readonly Color BorderDark  = Color.FromArgb(130, 76, 28);  // 深铜色（双线外边框）
        public static readonly Color BorderLight = Color.FromArgb(210, 145, 75);  // 浅铜色（双线内边框）

        // ==================== 字体 ====================
        private static FontFamily _pixelFamily;
        public static FontFamily PixelFamily => _pixelFamily;

        /// <summary>初始化主题（Program.Main 中调用一次）</summary>
        public static void Initialize()
        {
            LoadPixelFont();
        }

        /// <summary>
        /// 获取像素字体（调用方负责 Dispose）
        /// </summary>
        public static Font GetFont(float size, FontStyle style = FontStyle.Regular)
        {
            var family = _pixelFamily ?? FontFamily.GenericMonospace;
            // Press Start 2P 不支持 Bold/Italic 变体，统一使用 Regular
            return new Font(family, size, FontStyle.Regular, GraphicsUnit.Point);
        }

        // ==================== 字体加载 ====================
        private static void LoadPixelFont()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("DualMystery.Resources.PressStart2P.ttf"))
                {
                    if (stream != null)
                    {
                        byte[] fontData = new byte[stream.Length];
                        stream.Read(fontData, 0, fontData.Length);

                        IntPtr fontPtr = Marshal.AllocCoTaskMem(fontData.Length);
                        try
                        {
                            Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
                            var collection = new PrivateFontCollection();
                            collection.AddMemoryFont(fontPtr, fontData.Length);
                            if (collection.Families.Length > 0)
                            {
                                _pixelFamily = collection.Families[0];
                                System.Diagnostics.Debug.WriteLine("[Theme] Press Start 2P 字体加载成功");
                                return;
                            }
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(fontPtr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Theme] 像素字体加载失败: {ex.Message}");
            }

            // Fallback: 使用 Courier New（Windows 内置等宽字体）
            try
            {
                _pixelFamily = new FontFamily("Courier New");
                System.Diagnostics.Debug.WriteLine("[Theme] 使用 Courier New 作为备选字体");
            }
            catch
            {
                _pixelFamily = FontFamily.GenericMonospace;
                System.Diagnostics.Debug.WriteLine("[Theme] 使用 GenericMonospace 兜底");
            }
        }

        // ==================== 像素纹理生成 ====================

        private static Bitmap _woodTexture;
        private static Bitmap _stoneTexture;

        /// <summary>木质像素纹理 (8×8 交错色块)</summary>
        public static Bitmap WoodTexture
        {
            get
            {
                if (_woodTexture == null)
                    _woodTexture = GeneratePixelTexture(8,
                        new Color[] {
                            Color.FromArgb(38, 33, 28),
                            Color.FromArgb(35, 30, 26),
                            Color.FromArgb(40, 35, 30),
                            Color.FromArgb(33, 28, 24)
                        });
                return _woodTexture;
            }
        }

        /// <summary>石质像素纹理 (8×8 交错色块)</summary>
        public static Bitmap StoneTexture
        {
            get
            {
                if (_stoneTexture == null)
                    _stoneTexture = GeneratePixelTexture(8,
                        new Color[] {
                            Color.FromArgb(32, 37, 40),
                            Color.FromArgb(35, 40, 43),
                            Color.FromArgb(30, 34, 37),
                            Color.FromArgb(37, 42, 45)
                        });
                return _stoneTexture;
            }
        }

        /// <summary>生成像素风格平铺纹理</summary>
        private static Bitmap GeneratePixelTexture(int size, Color[] palette)
        {
            Bitmap bmp = new Bitmap(size, size);
            // 使用简单确定性公式产生交错感
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    bmp.SetPixel(x, y, palette[(x * 3 + y * 7 + (x ^ y)) % palette.Length]);
            return bmp;
        }

        /// <summary>生成棋盘格平铺纹理，用于地板像素图案</summary>
        /// <param name="tileSize">整张贴图尺寸（像素）</param>
        /// <param name="checkSize">每个格子尺寸（像素），推荐 4</param>
        /// <param name="c1">深色</param>
        /// <param name="c2">浅色</param>
        public static Bitmap CreateCheckerTile(int tileSize, int checkSize, Color c1, Color c2)
        {
            Bitmap bmp = new Bitmap(tileSize, tileSize);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                int checks = tileSize / checkSize;
                for (int y = 0; y < checks; y++)
                    for (int x = 0; x < checks; x++)
                    {
                        Color c = ((x + y) % 2 == 0) ? c1 : c2;
                        using (SolidBrush brush = new SolidBrush(c))
                            g.FillRectangle(brush, x * checkSize, y * checkSize, checkSize, checkSize);
                    }
            }
            return bmp;
        }

        // ==================== 像素边框绘制 ====================

        /// <summary>绘制像素风格矩形边框（2px，四角加粗）</summary>
        public static void DrawPixelBorder(Graphics g, Rectangle bounds, Color color)
        {
            int t = 2; // 线宽
            using (Pen pen = new Pen(color, t))
            {
                pen.Alignment = PenAlignment.Inset;
                g.DrawRectangle(pen, bounds);
            }
            // 四角加粗模拟像素风
            int cl = Math.Min(8, Math.Min(bounds.Width, bounds.Height) / 4);
            if (cl < 2) return;
            using (Pen cp = new Pen(color, t + 1))
            {
                cp.Alignment = PenAlignment.Center;
                int x = bounds.X, y = bounds.Y, r = bounds.Right - 1, b = bounds.Bottom - 1;
                // 左上
                g.DrawLine(cp, x, y, x + cl, y);
                g.DrawLine(cp, x, y, x, y + cl);
                // 右上
                g.DrawLine(cp, r - cl, y, r, y);
                g.DrawLine(cp, r, y, r, y + cl);
                // 左下
                g.DrawLine(cp, x, b - cl, x, b);
                g.DrawLine(cp, x, b, x + cl, b);
                // 右下
                g.DrawLine(cp, r - cl, b, r, b);
                g.DrawLine(cp, r, b - cl, r, b);
            }
        }

        /// <summary>绘制像素风格双线边框（外深内浅）</summary>
        public static void DrawDoubleLineBorder(Graphics g, Rectangle bounds, Color outerColor, Color innerColor)
        {
            using (Pen outer = new Pen(outerColor, 1))
            using (Pen inner = new Pen(innerColor, 1))
            {
                outer.Alignment = PenAlignment.Inset;
                inner.Alignment = PenAlignment.Inset;
                // 外线
                g.DrawRectangle(outer, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                // 内线（缩进 2px）
                g.DrawRectangle(inner, bounds.X + 2, bounds.Y + 2, bounds.Width - 5, bounds.Height - 5);
            }
        }

        // ==================== 控件样式 ====================

        /// <summary>统一 Flat 按钮样式：2px 铜色边框、暗色背景、浅色文字</summary>
        public static void StyleButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = BgButton;
            btn.ForeColor = TextMain;
            btn.FlatAppearance.BorderColor = Border;
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(BgButton, 0.25f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(BgButton, 0.15f);
        }

        /// <summary>设置 ListBox 为 OwnerDrawVariable 模式（动态行高适配文字），绑定绘制和测量事件</summary>
        public static void StyleListBox(ListBox lb)
        {
            lb.BackColor = Color.FromArgb(28, 38, 42);
            lb.ForeColor = TextMain;
            lb.BorderStyle = BorderStyle.None;
            lb.Font = GetFont(10f);
            lb.DrawMode = DrawMode.OwnerDrawVariable;
            lb.ItemHeight = 28; // 默认高度，MeasureItem 会动态覆盖
            lb.DrawItem += DrawListBoxItem;
            lb.MeasureItem += MeasureListBoxItem;
        }

        /// <summary>MeasureItem：根据文字内容和可用宽度动态计算每项高度</summary>
        private static void MeasureListBoxItem(object sender, MeasureItemEventArgs e)
        {
            if (e.Index < 0) return;
            var lb = sender as ListBox;
            if (lb == null) return;
            string text = lb.Items[e.Index]?.ToString() ?? "";
            // 可用宽度 = 控件宽度 - 左右边距(12) - 滚动条预留(8)
            int maxWidth = lb.ClientSize.Width - 20;
            if (maxWidth < 40) maxWidth = 160;
            SizeF size = e.Graphics.MeasureString(text, lb.Font, maxWidth);
            e.ItemHeight = Math.Max(28, (int)Math.Ceiling(size.Height) + 8);
        }

        /// <summary>OwnerDraw ListBox 共用的绘制逻辑：半透明背景让底层纹理透出，支持多行文本</summary>
        public static void DrawListBoxItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var lb = sender as ListBox;
            if (lb == null) return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            string text = lb.Items[e.Index]?.ToString() ?? "";

            // 半透明背景让底层纹理透出
            Color backColor;
            if (isSelected)
                backColor = Color.FromArgb(210, Accent);
            else if (e.Index % 2 == 0)
                backColor = Color.FromArgb(210, 28, 38, 42);
            else
                backColor = Color.FromArgb(170, 35, 46, 50);

            using (Brush bgBrush = new SolidBrush(backColor))
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

            // 文字绘制（使用动态行高 e.Bounds.Height，支持多行）
            Color textColor = isSelected ? BgMain : TextMain;
            using (Brush textBrush = new SolidBrush(textColor))
            {
                using (Font f = new Font(lb.Font.FontFamily, lb.Font.Size > 0 ? lb.Font.Size : 9f))
                using (StringFormat sf = new StringFormat(StringFormat.GenericDefault))
                {
                    // 启用自动换行，防止长文本被裁剪
                    sf.FormatFlags &= ~StringFormatFlags.NoWrap;
                    var rect = new Rectangle(e.Bounds.X + 6, e.Bounds.Y + 3,
                        e.Bounds.Width - 12, e.Bounds.Height - 6);
                    e.Graphics.DrawString(text, f, textBrush, rect, sf);
                }
            }

            // 聚焦框
            if (isSelected)
            {
                using (Pen focusPen = new Pen(Border, 1))
                    e.Graphics.DrawRectangle(focusPen,
                        e.Bounds.X + 1, e.Bounds.Y + 1,
                        e.Bounds.Width - 3, e.Bounds.Height - 3);
            }

            // 鼠标悬停虚线框
            if ((e.State & DrawItemState.HotLight) == DrawItemState.HotLight)
            {
                using (Pen hotPen = new Pen(Accent, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                    e.Graphics.DrawRectangle(hotPen,
                        e.Bounds.X + 1, e.Bounds.Y + 1,
                        e.Bounds.Width - 3, e.Bounds.Height - 3);
            }
        }

        /// <summary>设置 ListBox 的平铺纹理背景</summary>
        public static void ApplyTextureBackground(Control ctrl, Bitmap texture)
        {
            ctrl.BackgroundImage = texture;
            ctrl.BackgroundImageLayout = ImageLayout.Tile;
        }

        /// <summary>像素风格 GroupBox：自定义 Paint 绘制 2px 边框 + 四角加粗</summary>
        public static void StyleGroupBoxPixel(GroupBox gb)
        {
            gb.ForeColor = Accent;
            gb.BackColor = BgPanel;
            // 订阅 Paint 事件绘制像素边框
            gb.Paint += (sender, e) =>
            {
                GroupBox g = sender as GroupBox;
                if (g == null) return;
                Graphics gfx = e.Graphics;
                gfx.InterpolationMode = InterpolationMode.NearestNeighbor;

                // 测量标题
                SizeF titleSize = gfx.MeasureString(g.Text, g.Font);
                float titleH = titleSize.Height + 2;

                // 像素边框（从标题中间高度开始）
                int bw = 1;
                Rectangle borderRect = new Rectangle(
                    0, (int)titleH / 2,
                    g.Width - 1, g.Height - (int)titleH / 2 - 1);

                // 绘制双线边框（外深内浅）
                using (Pen outer = new Pen(BorderDark, bw))
                using (Pen inner = new Pen(Border, bw))
                {
                    outer.Alignment = PenAlignment.Inset;
                    inner.Alignment = PenAlignment.Inset;
                    gfx.DrawRectangle(outer, borderRect);
                    gfx.DrawRectangle(inner,
                        borderRect.X + 2, borderRect.Y + 2,
                        borderRect.Width - 4, borderRect.Height - 4);
                }

                // 四角加粗
                int cl = 6;
                int x = borderRect.X, y = borderRect.Y;
                int r = borderRect.Right, b = borderRect.Bottom;
                using (Pen cp = new Pen(Border, 3))
                {
                    cp.Alignment = PenAlignment.Center;
                    gfx.DrawLine(cp, x, y, x + cl, y);
                    gfx.DrawLine(cp, x, y, x, y + cl);
                    gfx.DrawLine(cp, r - cl, y, r, y);
                    gfx.DrawLine(cp, r, y, r, y + cl);
                    gfx.DrawLine(cp, x, b - cl, x, b);
                    gfx.DrawLine(cp, x, b, x + cl, b);
                    gfx.DrawLine(cp, r - cl, b, r, b);
                    gfx.DrawLine(cp, r, b - cl, r, b);
                }

                // 标题文字（遮盖顶部边框线）
                float titleX = 10;
                using (Brush titleBg = new SolidBrush(g.BackColor))
                    gfx.FillRectangle(titleBg, titleX - 3, 0, titleSize.Width + 6, titleSize.Height);
                using (Brush titleBrush = new SolidBrush(Accent))
                    gfx.DrawString(g.Text, g.Font, titleBrush, titleX, 0);
            };
        }

        /// <summary>为 Panel 添加像素风格双线边框绘制</summary>
        public static void StylePanelWithBorder(Panel pnl)
        {
            pnl.Paint += (sender, e) =>
            {
                Panel p = sender as Panel;
                if (p == null) return;
                Rectangle rect = new Rectangle(0, 0, p.Width, p.Height);
                DrawDoubleLineBorder(e.Graphics, rect, BorderDark, BorderLight);
            };
        }

        // ==================== 标题装饰 ====================

        /// <summary>为标题文字添加像素装饰符号</summary>
        public static string DecorateTitle(string title)
        {
            return $"◆  {title}  ◆";
        }

        /// <summary>创建一个 2px 高的铜色分隔线 Panel（Dock=Top）</summary>
        public static Panel CreateTitleSeparator()
        {
            return new Panel
            {
                Height = 2,
                BackColor = Border,
                Dock = DockStyle.Top
            };
        }
    }
}
