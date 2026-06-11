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

        private static int C(int v) => Math.Max(0, Math.Min(255, v));
        private static Color S(Color c, float f) => Color.FromArgb(c.A, C((int)(c.R * f)), C((int)(c.G * f)), C((int)(c.B * f)));

        // ==================== 线索图标 ====================

        public static Bitmap CreateKnife()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 50, 40)), 5, 4, 3, 11);
                // 刀柄 — 木纹
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 65, 35)), 4, 0, 4, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, 80, 45)), 4, 0, 2, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 50, 25)), 6, 2, 2, 2);
                // 护手 — 黄铜
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 150, 80)), 3, 3, 6, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 180, 100)), 3, 3, 3, 1);
                // 刀身
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 185, 195)), 5, 5, 3, 9);
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 205, 215)), 5, 5, 1, 8);
                g.FillRectangle(new SolidBrush(Color.FromArgb(150, 155, 165)), 7, 5, 1, 8);
                // 刀尖
                g.FillRectangle(new SolidBrush(Color.FromArgb(190, 195, 205)), 6, 3, 1, 2);
                // 刀刃高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(230, 235, 245)), 5, 6, 1, 2);
                // 字母 M
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 40, 20)), 5, 1, 2, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 40, 20)), 6, 2, 1, 1);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateLetter()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(170, 155, 130)), 4, 5, 10, 9);
                // 信封主体
                g.FillRectangle(new SolidBrush(Color.FromArgb(248, 238, 210)), 3, 4, 10, 9);
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 250, 235)), 3, 4, 3, 9);
                // 信封边框
                g.DrawRectangle(new Pen(Color.FromArgb(180, 160, 130)), 3, 4, 10, 9);
                // 封口三角
                Point[] flap = { new Point(8, 4), new Point(3, 8), new Point(13, 8) };
                g.FillPolygon(new SolidBrush(Color.FromArgb(240, 225, 190)), flap);
                g.DrawPolygon(new Pen(Color.FromArgb(180, 160, 130)), flap);
                // 封蜡
                g.FillEllipse(new SolidBrush(Color.FromArgb(185, 30, 35)), 6, 6, 4, 4);
                g.FillEllipse(new SolidBrush(Color.FromArgb(220, 50, 55)), 7, 7, 2, 2);
                // 烧焦边缘
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 35, 15)), 12, 4, 1, 9);
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 55, 25)), 11, 5, 1, 2);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateBook()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 40, 25)), 3, 3, 10, 12);
                // 封底
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, 55, 30)), 3, 2, 9, 12);
                // 封面
                g.FillRectangle(new SolidBrush(Color.FromArgb(145, 70, 35)), 2, 2, 9, 12);
                // 封面高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(170, 95, 55)), 2, 2, 2, 12);
                // 书脊
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 45, 25)), 2, 2, 1, 12);
                // 内页（侧面看）
                g.FillRectangle(new SolidBrush(Color.FromArgb(245, 240, 230)), 6, 3, 4, 10);
                g.DrawLine(new Pen(Color.FromArgb(200, 195, 185)), 7, 3, 7, 13);
                g.DrawLine(new Pen(Color.FromArgb(200, 195, 185)), 9, 3, 9, 13);
                // 封面十字架装饰
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 170, 100)), 3, 4, 3, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 170, 100)), 4, 3, 1, 5);
                // 书签红丝带
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 40, 40)), 4, 8, 1, 6);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreatePhone()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(30, 25, 20)), 5, 6, 8, 9);
                // 机身
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 42, 35)), 4, 5, 8, 9);
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 60, 50)), 4, 5, 8, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(35, 28, 22)), 4, 12, 8, 2);
                // 边框
                g.DrawRectangle(new Pen(Color.FromArgb(30, 25, 20)), 4, 5, 8, 9);
                // 听筒
                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 35, 30)), 3, 1, 10, 5);
                g.FillRectangle(new SolidBrush(Color.FromArgb(55, 48, 40)), 3, 1, 10, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(30, 25, 20)), 3, 4, 10, 1);
                // 听筒凹槽
                g.FillRectangle(new SolidBrush(Color.FromArgb(25, 20, 18)), 4, 2, 2, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(25, 20, 18)), 10, 2, 2, 2);
                // 转盘
                g.FillEllipse(new SolidBrush(Color.FromArgb(60, 50, 40)), 7, 6, 3, 5);
                g.FillEllipse(new SolidBrush(Color.FromArgb(80, 68, 55)), 7, 6, 3, 2);
                // 转盘孔
                g.FillEllipse(new SolidBrush(Color.FromArgb(30, 25, 20)), 8, 7, 1, 1);
                g.FillEllipse(new SolidBrush(Color.FromArgb(30, 25, 20)), 8, 9, 1, 1);
                // 听筒线
                g.DrawLine(new Pen(Color.FromArgb(40, 35, 30)), 8, 6, 8, 14);
                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 35, 30)), 7, 14, 2, 1);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateSafe()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(65, 65, 65)), 3, 4, 12, 11);
                // 箱体
                g.FillRectangle(new SolidBrush(Color.FromArgb(95, 100, 100)), 2, 3, 12, 11);
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, 125, 125)), 2, 3, 12, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 75, 75)), 2, 11, 12, 3);
                // 边框铆钉
                g.DrawRectangle(new Pen(Color.FromArgb(50, 50, 50)), 2, 3, 12, 11);
                for (int ty = 4; ty < 14; ty += 3)
                    for (int tx = 3; tx < 14; tx += 3)
                        g.FillEllipse(new SolidBrush(Color.FromArgb(160, 160, 160)), tx, ty, 2, 2);
                // 转盘
                g.FillEllipse(new SolidBrush(Color.FromArgb(80, 80, 80)), 5, 5, 6, 6);
                g.FillEllipse(new SolidBrush(Color.FromArgb(180, 180, 180)), 6, 6, 4, 4);
                g.FillEllipse(new SolidBrush(Color.FromArgb(220, 220, 220)), 7, 7, 2, 2);
                // 转盘刻度
                g.DrawLine(new Pen(Color.FromArgb(160, 160, 160)), 8, 5, 8, 7);
                // 把手
                g.FillRectangle(new SolidBrush(Color.FromArgb(130, 130, 130)), 11, 7, 2, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 180, 180)), 11, 7, 2, 1);
                // 合页
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 70, 70)), 1, 5, 1, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 70, 70)), 1, 10, 1, 3);
                // 门缝
                g.DrawLine(new Pen(Color.FromArgb(50, 50, 50)), 8, 3, 8, 14);
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
                g.FillRectangle(new SolidBrush(Color.FromArgb(190, 180, 170)), 3, 3, 12, 12);
                // 手帕主体 — 折叠感
                g.FillRectangle(new SolidBrush(Color.FromArgb(250, 245, 238)), 2, 2, 12, 12);
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 252, 248)), 2, 2, 6, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(235, 228, 220)), 2, 10, 12, 4);
                // 边框
                g.DrawRectangle(new Pen(Color.FromArgb(200, 190, 180)), 2, 2, 12, 12);
                // 花边装饰
                g.DrawLine(new Pen(Color.FromArgb(210, 180, 180)), 3, 3, 10, 10);
                g.DrawLine(new Pen(Color.FromArgb(210, 180, 180)), 13, 13, 3, 3);
                // 血迹（更大，更逼真）
                g.FillEllipse(new SolidBrush(Color.FromArgb(155, 20, 20)), 6, 5, 5, 4);
                g.FillEllipse(new SolidBrush(Color.FromArgb(185, 25, 25)), 7, 6, 3, 2);
                g.FillEllipse(new SolidBrush(Color.FromArgb(130, 15, 15)), 8, 8, 3, 2);
                // 绣字 EB
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 35, 35)), 3, 9, 3, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 35, 35)), 3, 11, 3, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 35, 35)), 3, 9, 1, 3);
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
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 160, 160)), 3, 5, 12, 10);
                // 日历本体
                g.FillRectangle(new SolidBrush(Color.FromArgb(248, 248, 248)), 2, 4, 12, 10);
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255)), 2, 4, 5, 2);
                // 边框
                g.DrawRectangle(new Pen(Color.FromArgb(60, 60, 60)), 2, 4, 12, 10);
                // 红色标题区
                g.FillRectangle(new SolidBrush(Color.FromArgb(195, 30, 30)), 2, 4, 12, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(225, 50, 50)), 2, 4, 6, 1);
                // 日期数字
                g.DrawString("25", new Font("Arial", 4, FontStyle.Bold), Brushes.White, 5, 3);
                // 红色圆圈标记
                g.DrawEllipse(new Pen(Color.FromArgb(220, 30, 30), 1), 5, 5, 8, 8);
                g.DrawEllipse(new Pen(Color.FromArgb(240, 60, 60), 0.5f), 6, 6, 6, 6);
                // 挂环
                g.FillEllipse(new SolidBrush(Color.FromArgb(180, 180, 180)), 6, 2, 3, 3);
                g.FillEllipse(new SolidBrush(Color.FromArgb(220, 220, 220)), 7, 3, 1, 1);
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
                using (Pen sp = new Pen(Color.FromArgb(100, 75, 20), 2))
                {
                    g.DrawEllipse(sp, 5, 3, 6, 6);
                    g.DrawLine(sp, 9, 9, 9, 15);
                }
                // 钥匙头（圆环）
                g.FillEllipse(new SolidBrush(Color.FromArgb(200, 160, 40)), 4, 2, 6, 6);
                g.FillEllipse(new SolidBrush(Color.FromArgb(230, 190, 60)), 5, 3, 4, 3);
                g.FillEllipse(new SolidBrush(Color.FromArgb(60, 45, 15)), 6, 4, 2, 2);
                // 钥匙杆
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 160, 40)), 6, 8, 2, 7);
                g.FillRectangle(new SolidBrush(Color.FromArgb(230, 190, 60)), 6, 8, 1, 5);
                // 钥匙齿
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 160, 40)), 8, 11, 2, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 160, 40)), 8, 14, 2, 1);
                // 高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(250, 220, 120)), 6, 2, 1, 1);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreatePhoto()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(140, 110, 80)), 3, 3, 12, 12);
                // 金色相框
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 170, 110)), 2, 2, 12, 12);
                g.FillRectangle(new SolidBrush(Color.FromArgb(235, 200, 140)), 2, 2, 5, 12);
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 140, 80)), 10, 3, 4, 10);
                // 内边框
                g.DrawRectangle(new Pen(Color.FromArgb(90, 60, 30)), 2, 2, 12, 12);
                g.DrawRectangle(new Pen(Color.FromArgb(130, 95, 50)), 3, 3, 10, 10);
                // 照片内容
                g.FillRectangle(new SolidBrush(Color.FromArgb(190, 175, 150)), 4, 4, 8, 8);
                // 人物1
                g.FillEllipse(new SolidBrush(Color.FromArgb(55, 45, 35)), 5, 5, 3, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(55, 45, 35)), 6, 8, 2, 3);
                // 人物2
                g.FillEllipse(new SolidBrush(Color.FromArgb(55, 45, 35)), 9, 6, 3, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(55, 45, 35)), 10, 9, 2, 2);
                // 铅笔字 19
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 70, 70)), 7, 12, 2, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 70, 70)), 7, 12, 1, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 70, 70)), 9, 12, 1, 2);
                // 相框支架
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 140, 80)), 8, 14, 1, 2);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateDesk()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 55, 30)), 4, 6, 10, 7);
                // 桌面（木纹）
                g.FillRectangle(new SolidBrush(Color.FromArgb(150, 85, 45)), 2, 4, 12, 7);
                g.FillRectangle(new SolidBrush(Color.FromArgb(170, 105, 60)), 2, 4, 12, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(135, 75, 40)), 2, 9, 12, 2);
                // 木纹纹理
                g.DrawLine(new Pen(Color.FromArgb(130, 75, 40)), 3, 6, 12, 6);
                g.DrawLine(new Pen(Color.FromArgb(130, 75, 40)), 4, 8, 13, 8);
                // 桌腿
                g.FillRectangle(new SolidBrush(Color.FromArgb(90, 60, 35)), 3, 10, 3, 6);
                g.FillRectangle(new SolidBrush(Color.FromArgb(90, 60, 35)), 10, 10, 3, 6);
                g.FillRectangle(new SolidBrush(Color.FromArgb(105, 75, 45)), 3, 10, 2, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(105, 75, 45)), 10, 10, 2, 2);
                // 横梁
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 55, 30)), 3, 13, 10, 1);
                // 抽屉
                g.FillRectangle(new SolidBrush(Color.FromArgb(140, 80, 42)), 6, 8, 5, 2);
                g.DrawRectangle(new Pen(Color.FromArgb(110, 65, 35)), 6, 8, 5, 2);
                // 抽屉把手
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 180, 100)), 7, 8, 3, 1);
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
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 185, 165)), 3, 3, 12, 12);
                // 纸张
                g.FillRectangle(new SolidBrush(Color.FromArgb(253, 237, 210)), 2, 2, 12, 12);
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 248, 232)), 2, 2, 6, 3);
                // 边框
                g.DrawRectangle(new Pen(Color.FromArgb(140, 120, 90)), 2, 2, 12, 12);
                // 手写文字行
                for (int i = 0; i < 6; i++)
                {
                    int y = 4 + i * 2;
                    int w = 7 + (i * 3) % 5;
                    using (Pen p = new Pen(Color.FromArgb(70, 50, 35)))
                        g.DrawLine(p, 4, y, 4 + w, y);
                }
                // 撕边效果
                g.FillRectangle(new SolidBrush(Color.FromArgb(190, 170, 145)), 13, 2, 1, 6);
                g.FillRectangle(new SolidBrush(Color.FromArgb(190, 170, 145)), 12, 3, 1, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(190, 170, 145)), 13, 10, 1, 4);
            }
            return ScaleUp(bmp);
        }

        // ==================== 场景装饰图标 ====================

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
                    g.FillRectangle(new SolidBrush(Color.FromArgb(60, 48, 35)), 3, 4, 11, 11);
                    // 炉体外框 — 砖石纹理
                    g.FillRectangle(new SolidBrush(Color.FromArgb(130, 100, 80)), 2, 3, 12, 12);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(155, 125, 100)), 2, 3, 12, 2);
                    // 砖缝
                    g.DrawLine(new Pen(Color.FromArgb(100, 80, 70)), 2, 9, 14, 9);
                    g.DrawLine(new Pen(Color.FromArgb(100, 80, 70)), 2, 12, 14, 12);
                    // 炉口（深色凹陷）
                    g.FillRectangle(new SolidBrush(Color.FromArgb(25, 18, 12)), 4, 6, 8, 8);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(15, 10, 8)), 5, 7, 6, 7);
                    // 炉口拱形
                    g.DrawArc(new Pen(Color.FromArgb(100, 80, 70), 1), 4, 2, 8, 6, 180, 180);
                    // 多层火焰
                    int fy = 8 + (fi % 2);
                    Color fire1 = Color.FromArgb(255, 200, 40);
                    Color fire2 = Color.FromArgb(255, 140, 15);
                    Color fire3 = Color.FromArgb(255, 70, 5);
                    g.FillRectangle(new SolidBrush(fire1), 7, fy - 1, 2, 5);
                    g.FillRectangle(new SolidBrush(fire2), 6, fy + 1, 4, 3);
                    g.FillRectangle(new SolidBrush(fire3), 7, fy + 3, 2, 2);
                    // 火焰高光
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 240, 150)), 7, fy - 1, 2, 1);
                    // 火星
                    int sparkX = 6 + (fi * 2) % 5;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 200, 80)), sparkX, fy - 2, 1, 1);
                    // 炉台
                    g.FillRectangle(new SolidBrush(Color.FromArgb(140, 115, 90)), 1, 2, 14, 2);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(165, 140, 110)), 1, 2, 14, 1);
                }
                frames[fi] = bmp;
            }
            return frames;
        }

        private static Bitmap cachedFireplace = null;
        public static Bitmap CreateFireplace() { if (cachedFireplace == null) cachedFireplace = ScaleUp(CreateFireplaceFrames()[0]); return cachedFireplace; }

        public static Bitmap[] CreateChandelierFrames()
        {
            Bitmap[] frames = new Bitmap[3];
            for (int fi = 0; fi < 3; fi++)
            {
                int sway = fi - 1;
                Bitmap bmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    // 天花板底座
                    g.FillRectangle(new SolidBrush(Color.FromArgb(80, 70, 60)), 5, 0, 6, 2);
                    // 链条
                    g.DrawLine(new Pen(Color.FromArgb(110, 100, 85)), 8, 2, 8 + sway, 3);
                    g.DrawLine(new Pen(Color.FromArgb(110, 100, 85)), 8 + sway, 3, 8 + sway, 4);
                    // 灯架横梁
                    g.FillRectangle(new SolidBrush(Color.FromArgb(150, 130, 100)), 2 + sway, 4, 12, 1);
                    // 左灯臂
                    g.DrawLine(new Pen(Color.FromArgb(150, 130, 100)), 2 + sway, 5, 3 + sway, 8);
                    // 右灯臂
                    g.DrawLine(new Pen(Color.FromArgb(150, 130, 100)), 13 + sway, 5, 12 + sway, 8);
                    // 中灯
                    g.FillEllipse(new SolidBrush(Color.FromArgb(180, 160, 120)), 6 + sway, 7, 4, 3);
                    g.FillEllipse(new SolidBrush(Color.FromArgb(220, 200, 150)), 6 + sway, 7, 4, 1);
                    // 左灯泡
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 245, 180)), 2 + sway, 8, 2, 2);
                    g.FillEllipse(new SolidBrush(Color.FromArgb(255, 245, 200, 140)), 1 + sway, 7, 4, 4);
                    // 右灯泡
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 245, 180)), 11 + sway, 8, 2, 2);
                    g.FillEllipse(new SolidBrush(Color.FromArgb(255, 245, 200, 140)), 10 + sway, 7, 4, 4);
                    // 整体光晕
                    g.FillEllipse(new SolidBrush(Color.FromArgb(255, 240, 220, 70)), 4 + sway, 3, 8, 10);
                }
                frames[fi] = bmp;
            }
            return frames;
        }
        private static Bitmap cachedChandelier = null;
        public static Bitmap CreateChandelier() { if (cachedChandelier == null) cachedChandelier = ScaleUp(CreateChandelierFrames()[1]); return cachedChandelier; }

        public static Bitmap CreateWallLamp()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 底座
                g.FillRectangle(new SolidBrush(Color.FromArgb(90, 75, 55)), 5, 0, 6, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(110, 90, 70)), 6, 0, 4, 1);
                // 灯臂曲线
                g.DrawLine(new Pen(Color.FromArgb(100, 85, 65), 2), 8, 1, 13, 5);
                g.DrawLine(new Pen(Color.FromArgb(120, 100, 75), 1), 8, 1, 13, 5);
                // 灯罩
                g.FillRectangle(new SolidBrush(Color.FromArgb(170, 150, 110)), 10, 4, 5, 6);
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 180, 135)), 10, 4, 5, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(145, 125, 90)), 11, 7, 4, 2);
                // 灯光
                g.FillEllipse(new SolidBrush(Color.FromArgb(120, 255, 240, 160)), 9, 7, 7, 6);
                g.FillEllipse(new SolidBrush(Color.FromArgb(200, 255, 220, 100)), 10, 8, 5, 4);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateCarpet()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 底色 — 深红
                g.FillRectangle(new SolidBrush(Color.FromArgb(145, 28, 28)), 0, 0, 16, 16);
                // 外边框
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 80, 30)), 0, 0, 16, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 80, 30)), 0, 15, 16, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 80, 30)), 0, 0, 1, 16);
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 80, 30)), 15, 0, 1, 16);
                // 内边框
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 140, 50)), 2, 1, 12, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 140, 50)), 2, 14, 12, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 140, 50)), 1, 2, 1, 12);
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 140, 50)), 14, 2, 1, 12);
                // 菱形花纹
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 130, 45)), 8, 7, 1, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 130, 45)), 6, 8, 5, 1);
            }
            return bmp;
        }

        public static Bitmap CreateCurtain()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillRectangle(new SolidBrush(Color.FromArgb(55, 45, 65)), 1, 1, 14, 14);
                // 主体 — 深紫红
                g.FillRectangle(new SolidBrush(Color.FromArgb(110, 75, 95)), 0, 0, 14, 15);
                // 褶皱高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(140, 105, 125)), 2, 0, 3, 15);
                g.FillRectangle(new SolidBrush(Color.FromArgb(130, 95, 115)), 9, 0, 3, 15);
                // 褶皱暗面
                g.FillRectangle(new SolidBrush(Color.FromArgb(90, 60, 80)), 6, 1, 2, 14);
                // 窗帘杆
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 55, 35)), 0, 0, 16, 1);
                g.FillEllipse(new SolidBrush(Color.FromArgb(100, 80, 50)), 0, 0, 1, 1);
                g.FillEllipse(new SolidBrush(Color.FromArgb(100, 80, 50)), 15, 0, 1, 1);
                // 束带
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 120, 80)), 6, 10, 4, 1);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateBodyOutline()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (Pen p = new Pen(Color.FromArgb(225, 225, 215), 1))
                {
                    g.DrawEllipse(p, 4, 1, 5, 5);       // 头
                    g.DrawLine(p, 6, 6, 6, 12);          // 身体
                    g.DrawLine(p, 6, 7, 2, 10);          // 左臂
                    g.DrawLine(p, 6, 7, 11, 9);          // 右臂
                    g.DrawLine(p, 6, 12, 3, 16);         // 左腿
                    g.DrawLine(p, 6, 12, 10, 16);        // 右腿
                }
                // 头部高光
                using (Pen hp = new Pen(Color.FromArgb(245, 245, 240), 0.5f))
                    g.DrawArc(hp, 5, 2, 3, 3, 220, 100);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateBloodStain()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                Color dk = Color.FromArgb(110, 10, 10);
                Color mid = Color.FromArgb(150, 15, 15);
                Color lt = Color.FromArgb(180, 20, 20);
                g.FillEllipse(new SolidBrush(dk), 3, 5, 7, 5);
                g.FillEllipse(new SolidBrush(mid), 4, 4, 5, 4);
                g.FillEllipse(new SolidBrush(lt), 5, 5, 3, 3);
                g.FillEllipse(new SolidBrush(Color.FromArgb(95, 8, 8)), 7, 6, 5, 3);
                g.FillRectangle(new SolidBrush(dk), 6, 5, 1, 3);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateVase()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillEllipse(new SolidBrush(Color.FromArgb(50, 50, 60)), 3, 12, 10, 4);
                // 瓶身 — 青花瓷风格
                Color baseColor = Color.FromArgb(70, 95, 145);
                g.FillRectangle(new SolidBrush(baseColor), 4, 4, 8, 9);
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 125, 175)), 5, 4, 3, 8);
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 75, 120)), 9, 5, 3, 7);
                // 瓶颈
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 80, 130)), 5, 2, 6, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(85, 110, 155)), 5, 2, 3, 2);
                // 瓶口
                g.FillRectangle(new SolidBrush(Color.FromArgb(55, 75, 120)), 4, 1, 8, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(75, 95, 140)), 4, 1, 8, 1);
                // 青花装饰
                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 55, 100)), 7, 6, 2, 5);
                g.DrawLine(new Pen(Color.FromArgb(40, 55, 100)), 5, 8, 11, 8);
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 175, 210)), 6, 6, 1, 1);
            }
            return ScaleUp(bmp);
        }

        // ==================== NPC 道具图标 ====================

        public static Bitmap CreateMedicalBag()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillEllipse(new SolidBrush(Color.FromArgb(35, 35, 35)), 2, 13, 12, 3);
                // 箱体 — 深棕皮
                g.FillRectangle(new SolidBrush(Color.FromArgb(85, 55, 40)), 3, 4, 10, 9);
                g.FillRectangle(new SolidBrush(Color.FromArgb(105, 72, 55)), 3, 4, 10, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(70, 42, 30)), 4, 10, 8, 3);
                // 边框金属角
                g.DrawRectangle(new Pen(Color.FromArgb(50, 35, 25)), 3, 4, 10, 9);
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 140, 80)), 2, 3, 2, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 140, 80)), 12, 3, 2, 2);
                // 红十字
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 35, 35)), 7, 6, 2, 5);
                g.FillRectangle(new SolidBrush(Color.FromArgb(210, 35, 35)), 5, 7, 6, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 55, 55)), 6, 6, 1, 1);
                // 提手
                g.FillRectangle(new SolidBrush(Color.FromArgb(130, 100, 70)), 5, 2, 6, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(150, 120, 85)), 6, 2, 4, 1);
                // 扣锁
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 180, 80)), 7, 5, 2, 2);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateBroom()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 柄 — 木纹
                using (Pen p = new Pen(Color.FromArgb(185, 145, 95), 2))
                    g.DrawLine(p, 8, 0, 8, 9);
                using (Pen hp = new Pen(Color.FromArgb(210, 170, 120), 1))
                    g.DrawLine(hp, 8, 0, 8, 5);
                // 金属箍
                g.FillRectangle(new SolidBrush(Color.FromArgb(170, 165, 160)), 6, 8, 4, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 195, 190)), 7, 8, 2, 1);
                // 刷头
                g.FillRectangle(new SolidBrush(Color.FromArgb(145, 115, 65)), 4, 9, 8, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(125, 95, 50)), 4, 10, 8, 2);
                // 刷毛
                for (int i = 0; i < 6; i++)
                    g.DrawLine(new Pen(Color.FromArgb(100, 75, 40)), 4 + i * 2, 11, 4 + i * 2, 15);
                // 刷毛渐变
                for (int i = 0; i < 6; i++)
                    g.DrawLine(new Pen(Color.FromArgb(130, 105, 65)), 4 + i * 2, 11, 4 + i * 2, 12);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateWineGlass()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillEllipse(new SolidBrush(Color.FromArgb(25, 20, 18)), 5, 13, 6, 3);
                // 底座
                g.FillEllipse(new SolidBrush(Color.FromArgb(160, 160, 170)), 5, 12, 6, 2);
                g.FillEllipse(new SolidBrush(Color.FromArgb(190, 190, 200)), 6, 12, 2, 1);
                // 杯柱
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 160, 170)), 7, 7, 2, 6);
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 180, 190)), 7, 7, 1, 3);
                // 杯身
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 35, 55)), 4, 2, 8, 6);
                g.FillRectangle(new SolidBrush(Color.FromArgb(190, 50, 70)), 5, 2, 4, 5);
                // 红酒
                g.FillRectangle(new SolidBrush(Color.FromArgb(130, 20, 35, 180)), 5, 3, 6, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(170, 30, 45, 150)), 6, 3, 3, 2);
                // 杯口
                g.DrawLine(new Pen(Color.FromArgb(190, 160, 170)), 4, 2, 12, 2);
                // 高光
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255, 80)), 5, 3, 1, 3);
            }
            return ScaleUp(bmp);
        }

        public static Bitmap CreateKeyRing()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 阴影
                g.FillEllipse(new SolidBrush(Color.FromArgb(40, 35, 28)), 4, 12, 8, 4);
                // 大环
                g.DrawEllipse(new Pen(Color.FromArgb(185, 160, 80), 1), 5, 2, 6, 6);
                g.DrawEllipse(new Pen(Color.FromArgb(215, 190, 105), 0.5f), 6, 3, 4, 4);
                // 钥匙1（大）
                g.DrawLine(new Pen(Color.FromArgb(170, 140, 45), 1), 8, 4, 8, 12);
                g.DrawLine(new Pen(Color.FromArgb(170, 140, 45), 0.5f), 8, 10, 10, 12);
                g.DrawLine(new Pen(Color.FromArgb(170, 140, 45), 0.5f), 8, 12, 7, 14);
                // 钥匙1 高光
                g.DrawLine(new Pen(Color.FromArgb(210, 180, 90), 0.3f), 8, 4, 8, 8);
                // 钥匙2（小）
                g.DrawLine(new Pen(Color.FromArgb(150, 120, 35), 1), 6, 5, 6, 13);
                g.DrawLine(new Pen(Color.FromArgb(150, 120, 35), 0.5f), 6, 11, 4, 13);
                // 钥匙2 高光
                g.DrawLine(new Pen(Color.FromArgb(190, 155, 70), 0.3f), 6, 5, 6, 9);
            }
            return ScaleUp(bmp);
        }
    }
}
