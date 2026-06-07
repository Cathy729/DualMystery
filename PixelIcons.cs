using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DualMystery
{
    public static class PixelIcons
    {
        // ==================== 通用工具 ====================
        private static Bitmap ScaleUp(Bitmap source)
        {
            Bitmap result = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(source, new Rectangle(0, 0, 32, 32), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
            }
            return result;
        }

        /// <summary>用像素点阵创建图标，支持阴影和高光</summary>
        private static Bitmap BuildIcon(Color[,] pixels)
        {
            int h = pixels.GetLength(0), w = pixels.GetLength(1);
            Bitmap bmp = new Bitmap(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (pixels[y, x].A > 0)
                        bmp.SetPixel(x, y, pixels[y, x]);
            return bmp;
        }

        private static Color Shade(Color c, float factor) =>
            Color.FromArgb(c.A,
                Clamp((int)(c.R * factor)), Clamp((int)(c.G * factor)), Clamp((int)(c.B * factor)));
        private static int Clamp(int v) => Math.Max(0, Math.Min(255, v));

        // ==================== 线索图标（精致化 + 阴影高光） ====================

        public static Bitmap CreateKnife()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 刀身阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, 120, 120)), 4, 2, 3, 10);
                // 刀身高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(220, 220, 230)), 4, 1, 1, 10);
                // 刀身主体
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 180, 190)), 5, 1, 2, 10);
                // 刀柄
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 60, 30)), 3, 0, 5, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(130, 80, 40)), 3, 0, 3, 1);
                // 护手
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 140, 80)), 2, 1, 7, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 180, 100)), 2, 1, 3, 1);
                // 刀尖高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 240, 255)), 4, 1, 1, 1);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateLetter()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 信封阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 170, 150)), 3, 4, 12, 10);
                // 信封主体
                g.FillRectangle(new SolidBrush(Color.FromArgb(250, 240, 220)), 2, 3, 12, 10);
                // 信封高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 245)), 2, 3, 3, 10);
                // 边框
                g.DrawRectangle(new Pen(Color.FromArgb(160, 140, 120)), 2, 3, 12, 10);
                // 封蜡
                g.FillEllipse(new SolidBrush(Color.FromArgb(200, 30, 30)), 6, 5, 4, 4);
                g.FillEllipse(new SolidBrush(Color.FromArgb(240, 60, 60)), 7, 6, 2, 2);
                // 烧焦边缘
                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 25, 10)), 13, 3, 1, 10);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateBook()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 封面阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 40, 20)), 4, 3, 10, 12);
                // 封面
                g.FillRectangle(new SolidBrush(Color.FromArgb(139, 69, 19)), 3, 2, 10, 12);
                // 封面高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 90, 40)), 3, 2, 2, 12);
                // 内页
                g.FillRectangle(new SolidBrush(Color.FromArgb(245, 240, 230)), 5, 3, 6, 10);
                // 页缝
                g.DrawLine(new Pen(Color.FromArgb(80, 60, 40)), 8, 3, 8, 13);
                // 书脊高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 110, 50)), 3, 3, 1, 10);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreatePhone()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 机身阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(20, 20, 20)), 5, 5, 8, 9);
                // 机身
                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 35, 30)), 4, 4, 8, 9);
                // 机身顶部高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 55, 50)), 4, 4, 8, 2);
                // 听筒
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 75, 70)), 3, 1, 10, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 95, 90)), 3, 1, 10, 1);
                // 转盘
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 45, 40)), 7, 6, 2, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 75, 65)), 7, 6, 2, 1);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateSafe()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 箱体阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 60)), 3, 3, 12, 12);
                // 箱体
                g.FillRectangle(new SolidBrush(Color.FromArgb(105, 105, 105)), 2, 2, 12, 12);
                // 顶部高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(140, 140, 140)), 2, 2, 12, 3);
                // 边框
                g.DrawRectangle(new Pen(Color.FromArgb(40, 40, 40)), 2, 2, 12, 12);
                // 转盘
                g.FillEllipse(new SolidBrush(Color.FromArgb(180, 180, 180)), 6, 6, 4, 4);
                g.FillEllipse(new SolidBrush(Color.FromArgb(220, 220, 220)), 7, 7, 2, 2);
                // 门缝
                g.DrawLine(new Pen(Color.FromArgb(60, 60, 60)), 8, 2, 8, 14);
                // 铰链
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), 1, 4, 1, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), 1, 10, 1, 3);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateHandkerchief()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 190, 180)), 3, 3, 12, 12);
                // 主体
                g.FillRectangle(new SolidBrush(Color.FromArgb(250, 245, 240)), 2, 2, 12, 12);
                // 高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255)), 2, 2, 5, 3);
                // 边框
                g.DrawRectangle(new Pen(Color.FromArgb(200, 190, 180)), 2, 2, 12, 12);
                // 血迹
                g.FillEllipse(new SolidBrush(Color.FromArgb(160, 20, 20)), 7, 6, 4, 3);
                g.FillEllipse(new SolidBrush(Color.FromArgb(200, 30, 30)), 8, 7, 2, 2);
                // 字母 EB
                g.DrawString("EB", new Font("Arial", 3, FontStyle.Bold), new SolidBrush(Color.FromArgb(120, 20, 20)), 3, 5);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateCalendar()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 180, 180)), 3, 5, 12, 10);
                // 日历本体
                g.FillRectangle(new SolidBrush(Color.FromArgb(245, 245, 245)), 2, 4, 12, 10);
                // 高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255)), 2, 4, 4, 3);
                // 边框
                g.DrawRectangle(new Pen(Color.FromArgb(40, 40, 40)), 2, 4, 12, 10);
                // 红色标题区
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 30, 30)), 2, 4, 12, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(230, 60, 60)), 2, 4, 6, 1);
                // 日期
                g.DrawString("25", new Font("Arial", 4, FontStyle.Bold), Brushes.White, 5, 3);
                // 红色圆圈标记
                g.DrawEllipse(new Pen(Color.FromArgb(220, 30, 30), 1), 3, 7, 10, 6);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateKey()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                using (Pen shadowPen = new Pen(Color.FromArgb(120, 90, 20), 2))
                {
                    g.DrawEllipse(shadowPen, 5, 3, 6, 6);
                    g.DrawLine(shadowPen, 9, 9, 9, 15);
                }
                // 主体
                using (Pen pen = new Pen(Color.FromArgb(184, 134, 11), 2))
                {
                    g.DrawEllipse(pen, 4, 2, 6, 6);
                    g.DrawLine(pen, 8, 8, 8, 14);
                    g.DrawLine(pen, 8, 12, 11, 14);
                    g.DrawLine(pen, 8, 14, 6, 15);
                }
                // 高光点
                g.FillEllipse(new SolidBrush(Color.FromArgb(240, 210, 100)), 6, 4, 2, 2);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreatePhoto()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 相框阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 130, 100)), 3, 3, 12, 12);
                // 相框
                g.FillRectangle(new SolidBrush(Color.FromArgb(222, 184, 135)), 2, 2, 12, 12);
                // 相框高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 210, 165)), 2, 2, 4, 12);
                // 边框
                g.DrawRectangle(new Pen(Color.FromArgb(100, 70, 40)), 2, 2, 12, 12);
                // 照片内区域
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 190, 170)), 4, 4, 8, 8);
                // 人像1
                g.FillEllipse(new SolidBrush(Color.FromArgb(50, 40, 30)), 5, 5, 3, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 40, 30)), 6, 8, 2, 3);
                // 人像2
                g.FillEllipse(new SolidBrush(Color.FromArgb(50, 40, 30)), 9, 5, 3, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 40, 30)), 10, 8, 2, 3);
                // 背面铅笔字
                g.DrawString("19", new Font("Arial", 3, FontStyle.Italic), new SolidBrush(Color.FromArgb(80, 80, 80)), 7, 12);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateDesk()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 桌面阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 70, 50)), 3, 5, 12, 8);
                // 桌面
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 82, 45)), 2, 4, 12, 8);
                // 桌面高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 100, 60)), 2, 4, 12, 2);
                // 桌腿
                g.DrawLine(new Pen(Color.FromArgb(40, 40, 40)), 2, 8, 14, 8);
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 40, 30)), 4, 11, 2, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 40, 30)), 10, 11, 2, 4);
                // 抽屉把手
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 160, 80)), 6, 9, 4, 1);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateDiaryPage()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 200, 180)), 3, 3, 12, 12);
                // 纸
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 239, 213)), 2, 2, 12, 12);
                // 高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 250, 235)), 2, 2, 5, 2);
                // 边框
                g.DrawRectangle(new Pen(Color.FromArgb(120, 100, 80)), 2, 2, 12, 12);
                // 文字行（模拟手写）
                for (int i = 0; i < 5; i++)
                {
                    int y = 5 + i * 2;
                    using (Pen p = new Pen(Color.FromArgb(80, 60, 40)))
                        g.DrawLine(p, 4, y, 9 + (i % 3), y);
                }
                // 撕边效果
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 180, 150)), 13, 2, 1, 12);
            }
            return ScaleUp(bmp);
        }

        // ==================== 场景装饰图标 ====================

        /// <summary>壁炉（含火焰动画帧）</summary>
        public static Bitmap[] CreateFireplaceFrames()
        {
            Bitmap[] frames = new Bitmap[3];
            for (int fi = 0; fi < 3; fi++)
            {
                Bitmap bmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    // 炉体阴影
                    g.FillRectangle(new SolidBrush(Color.FromArgb(60, 50, 40)), 3, 4, 11, 11);
                    // 炉体
                    g.FillRectangle(new SolidBrush(Color.FromArgb(120, 100, 80)), 2, 3, 12, 12);
                    // 炉体高光
                    g.FillRectangle(new SolidBrush(Color.FromArgb(150, 130, 110)), 2, 3, 12, 2);
                    // 炉口
                    g.FillRectangle(new SolidBrush(Color.FromArgb(30, 20, 10)), 4, 6, 8, 8);
                    // 火焰（每帧不同）
                    Color[] flameColors = { Color.FromArgb(255, 180, 30), Color.FromArgb(255, 120, 10), Color.FromArgb(255, 60, 5) };
                    int[] flameH = { 5 + fi, 6 - fi % 2, 4 + (fi + 1) % 3 };
                    for (int i = 0; i < 3; i++)
                        g.FillRectangle(new SolidBrush(flameColors[i]), 6 + i, 8 + 2 - flameH[i] / 2, 2, flameH[i]);
                    // 火焰高光
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 240, 180)), 7, 7, 1, 2);
                    // 炉台装饰
                    g.FillRectangle(new SolidBrush(Color.FromArgb(140, 120, 100)), 1, 2, 14, 1);
                }
                frames[fi] = bmp;
            }
            return frames;
        }
        private static Bitmap cachedFireplace = null;
        public static Bitmap CreateFireplace() { if (cachedFireplace == null) cachedFireplace = ScaleUp(CreateFireplaceFrames()[0]); return cachedFireplace; }

        /// <summary>吊灯（含摆动帧）</summary>
        public static Bitmap[] CreateChandelierFrames()
        {
            Bitmap[] frames = new Bitmap[3];
            for (int fi = 0; fi < 3; fi++)
            {
                int sway = fi - 1; // -1, 0, 1
                Bitmap bmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    // 链条
                    g.DrawLine(new Pen(Color.FromArgb(100, 90, 70)), 8, 0, 8 + sway, 3);
                    // 灯体阴影
                    g.FillEllipse(new SolidBrush(Color.FromArgb(80, 70, 50)), 4 + sway, 7, 9, 5);
                    // 灯体
                    g.FillEllipse(new SolidBrush(Color.FromArgb(200, 180, 120)), 3 + sway, 6, 10, 6);
                    // 高光
                    g.FillEllipse(new SolidBrush(Color.FromArgb(240, 220, 160)), 5 + sway, 6, 4, 3);
                    // 灯泡发光
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 250, 200)), 5 + sway, 8, 6, 3);
                    // 顶部装饰
                    g.FillRectangle(new SolidBrush(Color.FromArgb(160, 140, 100)), 6, 2, 4, 2);
                }
                frames[fi] = bmp;
            }
            return frames;
        }
        private static Bitmap cachedChandelier = null;
        public static Bitmap CreateChandelier() { if (cachedChandelier == null) cachedChandelier = ScaleUp(CreateChandelierFrames()[1]); return cachedChandelier; }

        /// <summary>壁灯</summary>
        public static Bitmap CreateWallLamp()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 底座
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 80, 50)), 6, 0, 4, 2);
                // 灯臂
                g.DrawLine(new Pen(Color.FromArgb(120, 100, 70), 1), 8, 2, 12, 5);
                // 灯罩阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 80, 50)), 11, 4, 5, 6);
                // 灯罩
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 160, 100)), 10, 3, 6, 7);
                // 高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 190, 130)), 10, 3, 6, 1);
                // 灯光
                g.FillEllipse(new SolidBrush(Color.FromArgb(255, 250, 180, 180)), 9, 7, 8, 6);
            }
            return ScaleUp(bmp);
        }

        /// <summary>地毯（大尺寸装饰，返回原始16x16以供拉伸）</summary>
        public static Bitmap CreateCarpet()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 底色
                g.FillRectangle(new SolidBrush(Color.FromArgb(140, 30, 30)), 0, 0, 16, 16);
                // 边框花纹
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 80, 20)), 0, 0, 16, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 80, 20)), 0, 14, 16, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 80, 20)), 0, 0, 2, 16);
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 80, 20)), 14, 0, 2, 16);
                // 中心菱形花纹
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 120, 40)), 7, 6, 2, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 120, 40)), 5, 7, 6, 2);
            }
            return bmp;
        }

        /// <summary>窗帘</summary>
        public static Bitmap CreateCurtain()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 50, 70)), 1, 1, 14, 14);
                // 主体
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 70, 90)), 0, 0, 14, 15);
                // 褶皱高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(130, 100, 120)), 2, 0, 3, 15);
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, 90, 110)), 9, 0, 3, 15);
                // 窗帘杆
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 60, 40)), 0, 0, 16, 1);
            }
            return ScaleUp(bmp);
        }

        /// <summary>尸体轮廓（粉笔线）</summary>
        public static Bitmap CreateBodyOutline()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (Pen p = new Pen(Color.FromArgb(220, 220, 210), 1))
                {
                    // 头
                    g.DrawEllipse(p, 5, 1, 4, 4);
                    // 身体
                    g.DrawLine(p, 7, 5, 7, 11);
                    // 手臂
                    g.DrawLine(p, 7, 6, 3, 9);
                    g.DrawLine(p, 7, 6, 12, 8);
                    // 腿
                    g.DrawLine(p, 7, 11, 4, 15);
                    g.DrawLine(p, 7, 11, 10, 15);
                }
            }
            return ScaleUp(bmp);
        }

        /// <summary>血迹</summary>
        public static Bitmap CreateBloodStain()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                Color darkBlood = Color.FromArgb(120, 10, 10);
                Color midBlood = Color.FromArgb(160, 15, 15);
                // 不规则血迹
                g.FillEllipse(new SolidBrush(darkBlood), 3, 4, 6, 5);
                g.FillEllipse(new SolidBrush(midBlood), 4, 3, 4, 4);
                g.FillEllipse(new SolidBrush(Color.FromArgb(100, 8, 8)), 6, 6, 7, 4);
                g.FillRectangle(new SolidBrush(darkBlood), 7, 5, 2, 2);
            }
            return ScaleUp(bmp);
        }

        /// <summary>花瓶</summary>
        public static Bitmap CreateVase()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillEllipse(new SolidBrush(Color.FromArgb(60, 60, 70)), 3, 12, 10, 4);
                // 瓶身
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 100, 140)), 5, 4, 6, 9);
                // 高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, 140, 180)), 5, 4, 2, 8);
                // 瓶颈
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 85, 120)), 6, 2, 4, 3);
                // 瓶口
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 75, 110)), 5, 1, 6, 2);
                // 花纹
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 180, 200)), 8, 7, 2, 3);
            }
            return ScaleUp(bmp);
        }

        // ==================== NPC 道具图标 ====================

        /// <summary>医药箱（格雷医生）</summary>
        public static Bitmap CreateMedicalBag()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillEllipse(new SolidBrush(Color.FromArgb(40, 40, 40)), 2, 13, 12, 3);
                // 箱体
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 50, 40)), 3, 5, 10, 8);
                // 高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 70, 55)), 3, 5, 10, 2);
                // 红十字
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 30, 30)), 7, 7, 2, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 30, 30)), 5, 8, 6, 2);
                // 把手
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 80, 60)), 6, 3, 4, 2);
                // 扣锁
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 160, 80)), 7, 6, 2, 1);
            }
            return ScaleUp(bmp);
        }

        /// <summary>扫帚（贝蒂）</summary>
        public static Bitmap CreateBroom()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 柄
                using (Pen p = new Pen(Color.FromArgb(180, 140, 90), 1))
                    g.DrawLine(p, 8, 1, 8, 9);
                // 柄高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 160, 110)), 8, 1, 1, 4);
                // 刷头
                g.FillRectangle(new SolidBrush(Color.FromArgb(140, 110, 60)), 4, 9, 8, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, 90, 50)), 4, 11, 8, 2);
                // 刷毛
                for (int i = 0; i < 5; i++)
                    g.DrawLine(new Pen(Color.FromArgb(100, 75, 40)), 5 + i * 2, 12, 5 + i * 2, 14);
            }
            return ScaleUp(bmp);
        }

        /// <summary>酒杯（埃德加）</summary>
        public static Bitmap CreateWineGlass()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillEllipse(new SolidBrush(Color.FromArgb(30, 25, 20)), 5, 13, 6, 3);
                // 底座
                g.FillEllipse(new SolidBrush(Color.FromArgb(150, 150, 160)), 5, 12, 6, 2);
                // 杯柱
                g.FillRectangle(new SolidBrush(Color.FromArgb(150, 150, 160)), 7, 8, 2, 5);
                // 杯身
                g.FillRectangle(new SolidBrush(Color.FromArgb(140, 30, 50, 180)), 4, 3, 8, 6);
                // 高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255, 80)), 5, 4, 1, 4);
                // 杯口
                g.DrawLine(new Pen(Color.FromArgb(180, 150, 160)), 4, 3, 12, 3);
            }
            return ScaleUp(bmp);
        }

        /// <summary>钥匙串（莫里斯）</summary>
        public static Bitmap CreateKeyRing()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillEllipse(new SolidBrush(Color.FromArgb(40, 40, 40)), 4, 12, 8, 3);
                // 环
                g.DrawEllipse(new Pen(Color.FromArgb(180, 160, 80), 1), 5, 3, 6, 6);
                g.DrawEllipse(new Pen(Color.FromArgb(200, 180, 100), 0.5f), 6, 4, 4, 4);
                // 钥匙1
                g.DrawLine(new Pen(Color.FromArgb(160, 130, 40), 1), 8, 5, 8, 12);
                g.DrawLine(new Pen(Color.FromArgb(160, 130, 40), 0.5f), 8, 10, 10, 12);
                // 钥匙2
                g.DrawLine(new Pen(Color.FromArgb(140, 110, 30), 1), 6, 6, 6, 13);
                g.DrawLine(new Pen(Color.FromArgb(140, 110, 30), 0.5f), 6, 11, 4, 13);
            }
            return ScaleUp(bmp);
        }
    }
}
