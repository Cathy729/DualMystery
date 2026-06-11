using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DualMystery
{
    /// <summary>
    /// 像素角色生成器 — 16×24 网格，3x 放大至 48×72
    /// 统一比例: 头发0-2 | 脸3-9 | 颈10-11 | 身体12-17 | 腰带18 | 腿19-22 | 鞋23
    /// </summary>
    public static class PixelCharacters
    {
        // ==================== 通用工具 ====================
        private const int GW = 16, GH = 24;
        private const int OUT_W = 48, OUT_H = 72; // 3x

        private static Bitmap RenderCharacter(Color[,] pixels)
        {
            Bitmap src = new Bitmap(GW, GH);
            for (int y = 0; y < GH; y++)
                for (int x = 0; x < GW; x++)
                    if (pixels[y, x].A > 0)
                        src.SetPixel(x, y, pixels[y, x]);

            Bitmap result = new Bitmap(OUT_W, OUT_H);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(src, new Rectangle(0, 0, OUT_W, OUT_H), new Rectangle(0, 0, GW, GH), GraphicsUnit.Pixel);
            }
            src.Dispose();
            return result;
        }

        private static Color[,] NewGrid() => new Color[GH, GW];

        private static void Rect(Color[,] g, int x, int y, int w, int h, Color c)
        {
            for (int dy = 0; dy < h; dy++)
                for (int dx = 0; dx < w; dx++)
                    if (x + dx >= 0 && x + dx < GW && y + dy >= 0 && y + dy < GH)
                        g[y + dy, x + dx] = c;
        }

        private static void HLine(Color[,] g, int x, int y, int w, Color c)
        {
            for (int dx = 0; dx < w; dx++)
                if (x + dx >= 0 && x + dx < GW && y >= 0 && y < GH)
                    g[y, x + dx] = c;
        }

        private static void VLine(Color[,] g, int x, int y, int h, Color c)
        {
            for (int dy = 0; dy < h; dy++)
                if (x >= 0 && x < GW && y + dy >= 0 && y + dy < GH)
                    g[y + dy, x] = c;
        }

        private static void Px(Color[,] g, int x, int y, Color c)
        {
            if (x >= 0 && x < GW && y >= 0 && y < GH)
                g[y, x] = c;
        }

        // ==================== 配色 ====================
        private static readonly Color SkinBase   = Color.FromArgb(255, 224, 189);
        private static readonly Color SkinShadow = Color.FromArgb(240, 200, 160);
        private static readonly Color SkinLight  = Color.FromArgb(255, 240, 215);

        private static readonly Color EyeWhite  = Color.FromArgb(255, 255, 255);
        private static readonly Color IrisDark  = Color.FromArgb(40, 35, 30);
        private static readonly Color IrisGreen = Color.FromArgb(70, 120, 60);
        private static readonly Color Mouth     = Color.FromArgb(180, 130, 110);
        private static readonly Color BeltBrown = Color.FromArgb(120, 80, 40);
        private static readonly Color Buckle    = Color.FromArgb(220, 200, 80);
        private static readonly Color EyeShine  = Color.FromArgb(255, 255, 255);

        // ==================== 统一比例绘制部件 ====================

        /// <summary>脸 (行3-9): 皮肤 + 耳朵 + 眉毛 + 眼 + 鼻 + 嘴</summary>
        private static void DrawFace(Color[,] g, Color hairCol, Color? irisCol = null)
        {
            Color iris = irisCol ?? IrisDark;
            Rect(g, 4, 3, 8, 7, SkinBase);       // 脸 7px 高
            Rect(g, 4, 3, 8, 1, SkinLight);       // 额头高光
            Rect(g, 4, 9, 8, 1, SkinShadow);       // 下巴阴影
            // 耳朵
            Px(g, 3, 5, SkinBase); Px(g, 3, 6, SkinBase); Px(g, 3, 7, SkinBase);
            Px(g, 12, 5, SkinBase); Px(g, 12, 6, SkinBase); Px(g, 12, 7, SkinBase);
            // 眉毛
            HLine(g, 5, 4, 3, hairCol);
            HLine(g, 9, 4, 3, hairCol);
            // 眼睛 (行5-6)
            Rect(g, 5, 5, 3, 2, EyeWhite);
            Px(g, 6, 5, iris); Px(g, 6, 6, iris); Px(g, 6, 5, EyeShine);
            Rect(g, 9, 5, 3, 2, EyeWhite);
            Px(g, 10, 5, iris); Px(g, 10, 6, iris); Px(g, 10, 5, EyeShine);
            // 鼻子 (行8)
            Px(g, 7, 8, SkinShadow); Px(g, 8, 8, SkinShadow);
            // 嘴 (行9)
            HLine(g, 6, 9, 4, Mouth);
        }

        /// <summary>身体 + 手臂 + 腰带 (行12-18)，统一宽度 10px (col 3-13)</summary>
        private static void DrawBody(Color[,] g, Color bodyCol, Color bodyHi, Color bodySh)
        {
            // 上身 6px 高
            Rect(g, 3, 12, 10, 6, bodyCol);
            Rect(g, 4, 12, 8, 1, bodyHi);          // 肩高光
            Rect(g, 3, 13, 2, 3, bodySh);          // 左暗面
            Rect(g, 11, 13, 2, 3, bodySh);         // 右暗面
            // 手臂
            Rect(g, 1, 12, 2, 4, bodySh);
            Px(g, 1, 16, SkinBase); Px(g, 2, 16, SkinBase);
            Rect(g, 13, 12, 2, 4, bodySh);
            Px(g, 13, 16, SkinBase); Px(g, 14, 16, SkinBase);
            // 腰带 (行18)
            Rect(g, 3, 18, 10, 1, BeltBrown);
            Px(g, 7, 18, Buckle); Px(g, 8, 18, Buckle);
        }

        /// <summary>腿 + 鞋 (行19-23): 大腿2 + 小腿3 + 鞋1</summary>
        private static void DrawLegs(Color[,] g, Color pants, Color pantsHi, Color shoe, Color shoeHi, bool stomp = false)
        {
            // 大腿 (行19-20)
            Rect(g, 4, 19, 8, 2, pants);
            Rect(g, 5, 19, 6, 1, pantsHi);
            // 小腿 (行21-22) + 鞋 (行23)
            if (stomp)
            {
                Rect(g, 3, 21, 3, 3, pants);
                Rect(g, 10, 21, 3, 3, pants);
                Rect(g, 2, 23, 4, 1, shoe);
                Rect(g, 2, 23, 4, 1, shoeHi);
                Rect(g, 10, 23, 4, 1, shoe);
                Rect(g, 10, 23, 4, 1, shoeHi);
            }
            else
            {
                Rect(g, 4, 21, 3, 3, pants);
                Rect(g, 9, 21, 3, 3, pants);
                Rect(g, 3, 23, 4, 1, shoe);
                Rect(g, 3, 23, 4, 1, shoeHi);
                Rect(g, 9, 23, 4, 1, shoe);
                Rect(g, 9, 23, 4, 1, shoeHi);
            }
        }

        // ==================== 玩家A — 蓝衣侦探 ====================
        private static Bitmap CreatePlayerA(bool stomp)
        {
            var g = NewGrid();
            var Hair   = Color.FromArgb(80, 50, 30);
            var HairHi = Color.FromArgb(110, 75, 50);
            var Coat   = Color.FromArgb(40, 55, 80);
            var CoatHi = Color.FromArgb(60, 75, 100);
            var CoatSh = Color.FromArgb(30, 42, 60);
            var Pants  = Color.FromArgb(55, 55, 60);
            var PantsHi = Color.FromArgb(70, 70, 75);
            var Shoe   = Color.FromArgb(70, 45, 25);
            var ShoeHi = Color.FromArgb(100, 70, 45);
            var Shirt  = Color.FromArgb(220, 225, 235);
            var Tie    = Color.FromArgb(130, 40, 40);

            Rect(g, 3, 0, 10, 2, HairHi);
            Rect(g, 2, 1, 12, 2, Hair);
            Rect(g, 3, 0, 5, 1, HairHi);
            DrawFace(g, Hair);
            Rect(g, 6, 10, 4, 1, SkinShadow);
            // 衬衫 + 领带
            Rect(g, 5, 11, 6, 1, Shirt);
            Px(g, 7, 11, Tie); Px(g, 8, 11, Tie);
            VLine(g, 7, 12, 4, Tie); VLine(g, 8, 12, 4, Tie);
            DrawBody(g, Coat, CoatHi, CoatSh);
            DrawLegs(g, Pants, PantsHi, Shoe, ShoeHi, stomp);
            return RenderCharacter(g);
        }
        public static Bitmap CreatePlayerA_Idle()  => CreatePlayerA(false);
        public static Bitmap CreatePlayerA_Stomp() => CreatePlayerA(true);

        // ==================== 玩家B — 红棕衣侦探 ====================
        private static Bitmap CreatePlayerB(bool stomp)
        {
            var g = NewGrid();
            var Hair   = Color.FromArgb(200, 170, 80);
            var HairHi = Color.FromArgb(230, 200, 110);
            var HairSh = Color.FromArgb(170, 140, 60);
            var Coat   = Color.FromArgb(120, 45, 35);
            var CoatHi = Color.FromArgb(150, 65, 50);
            var CoatSh = Color.FromArgb(90, 30, 25);
            var Pants  = Color.FromArgb(65, 55, 45);
            var PantsHi = Color.FromArgb(80, 70, 58);
            var Shoe   = Color.FromArgb(45, 35, 30);
            var ShoeHi = Color.FromArgb(70, 55, 45);
            var Scarf  = Color.FromArgb(60, 100, 80);
            var ScarfHi = Color.FromArgb(80, 130, 100);

            Rect(g, 3, 0, 10, 2, HairHi);
            Rect(g, 2, 1, 12, 2, Hair);
            Rect(g, 2, 2, 3, 2, Hair);
            Rect(g, 11, 2, 3, 2, Hair);
            Rect(g, 3, 1, 5, 1, HairHi);
            DrawFace(g, HairSh, IrisGreen);
            Rect(g, 6, 10, 4, 1, SkinShadow);
            Rect(g, 5, 10, 6, 2, Scarf);
            Rect(g, 5, 10, 6, 1, ScarfHi);
            DrawBody(g, Coat, CoatHi, CoatSh);
            DrawLegs(g, Pants, PantsHi, Shoe, ShoeHi, stomp);
            return RenderCharacter(g);
        }
        public static Bitmap CreatePlayerB_Idle()  => CreatePlayerB(false);
        public static Bitmap CreatePlayerB_Stomp() => CreatePlayerB(true);

        // ==================== 女仆贝蒂 ====================
        public static Bitmap CreateBetty()
        {
            var g = NewGrid();
            var Hair   = Color.FromArgb(100, 60, 40);
            var HairHi = Color.FromArgb(130, 85, 60);
            var Dress  = Color.FromArgb(50, 50, 60);
            var DressHi = Color.FromArgb(65, 65, 75);
            var Apron  = Color.FromArgb(240, 238, 230);
            var Shoe   = Color.FromArgb(45, 40, 45);
            var ShoeHi = Color.FromArgb(70, 60, 70);
            var Bow    = Color.FromArgb(200, 180, 200);
            var Lips   = Color.FromArgb(220, 130, 130);

            Rect(g, 3, 0, 10, 2, HairHi);
            Rect(g, 2, 1, 12, 2, Hair);
            Rect(g, 3, 1, 2, 2, Hair);
            Rect(g, 11, 1, 2, 2, Hair);
            Rect(g, 5, 0, 6, 1, HairHi);
            DrawFace(g, Hair);
            Px(g, 4, 6, Color.FromArgb(255, 180, 170));  // 腮红
            Px(g, 11, 6, Color.FromArgb(255, 180, 170));
            HLine(g, 6, 9, 4, Lips);                     // 口红覆盖
            // 颈 + 蝴蝶结
            Rect(g, 6, 10, 4, 1, SkinShadow);
            Rect(g, 6, 11, 4, 1, Bow);
            // 裙身 (6px高，统一宽10)
            Rect(g, 3, 12, 10, 6, Dress);
            Rect(g, 4, 12, 8, 1, DressHi);
            Rect(g, 5, 12, 6, 6, Apron);
            Rect(g, 5, 12, 6, 1, Color.FromArgb(250, 250, 245));
            // 手臂
            Px(g, 2, 12, Dress); Px(g, 2, 13, Dress);
            Px(g, 1, 14, SkinBase); Px(g, 2, 14, SkinBase);
            Px(g, 13, 12, Dress); Px(g, 13, 13, Dress);
            Px(g, 13, 14, SkinBase); Px(g, 14, 14, SkinBase);
            // 腰带
            Rect(g, 3, 18, 10, 1, Color.FromArgb(40, 40, 50));
            // 裙摆 + 腿
            Rect(g, 3, 19, 10, 1, Dress);
            HLine(g, 5, 19, 6, Apron);
            Rect(g, 4, 20, 3, 4, Dress);
            Rect(g, 9, 20, 3, 4, Dress);
            Rect(g, 3, 23, 4, 1, Shoe);
            Rect(g, 3, 23, 4, 1, ShoeHi);
            Rect(g, 9, 23, 4, 1, Shoe);
            Rect(g, 9, 23, 4, 1, ShoeHi);
            return RenderCharacter(g);
        }

        // ==================== 格雷医生 — 宽度统一 + 暗面收窄 ====================
        public static Bitmap CreateGrey()
        {
            var g = NewGrid();
            var Hair   = Color.FromArgb(160, 155, 150);
            var HairHi = Color.FromArgb(190, 185, 180);
            var CoatW  = Color.FromArgb(235, 235, 240);   // 白大褂主体
            var CoatWhi = Color.FromArgb(250, 250, 252);  // 高光
            var CoatWsh = Color.FromArgb(200, 200, 208);  // 暗面（加深以收窄视觉）
            var CoatWedge = Color.FromArgb(185, 185, 195); // 边缘更深
            var Pants  = Color.FromArgb(50, 50, 58);
            var PantsHi = Color.FromArgb(65, 65, 73);
            var Shoe   = Color.FromArgb(40, 35, 35);
            var ShoeHi = Color.FromArgb(60, 55, 55);
            var Glasses = Color.FromArgb(180, 160, 100);
            var Steth  = Color.FromArgb(100, 100, 110);

            Rect(g, 4, 0, 8, 2, HairHi);
            Rect(g, 3, 1, 10, 1, Hair);
            Rect(g, 3, 2, 2, 1, Hair);
            Rect(g, 11, 2, 2, 1, Hair);
            DrawFace(g, Hair);
            // 眼镜
            Rect(g, 4, 5, 4, 3, Glasses);
            Px(g, 5, 6, SkinBase); Px(g, 6, 6, SkinBase);
            Rect(g, 8, 5, 4, 3, Glasses);
            Px(g, 9, 6, SkinBase); Px(g, 10, 6, SkinBase);
            HLine(g, 4, 5, 8, Glasses);
            HLine(g, 4, 7, 8, Glasses);
            Px(g, 5, 6, IrisDark); Px(g, 6, 6, IrisDark);
            Px(g, 9, 6, IrisDark); Px(g, 10, 6, IrisDark);
            Px(g, 5, 6, EyeShine); Px(g, 9, 6, EyeShine);
            HLine(g, 7, 9, 2, Mouth);
            // 颈 + 衬衫
            Rect(g, 6, 10, 4, 1, SkinShadow);
            Rect(g, 5, 11, 6, 1, Color.FromArgb(200, 210, 215));
            VLine(g, 7, 12, 3, Color.FromArgb(50, 70, 100));
            VLine(g, 8, 12, 3, Color.FromArgb(50, 70, 100));
            // 白大褂 — 统一宽度 10px (col 3-13)，用深色边缘收窄视觉
            Rect(g, 3, 12, 10, 6, CoatW);
            Rect(g, 4, 12, 8, 1, CoatWhi);
            // 两侧深色边缘，制造收窄效果
            VLine(g, 3, 13, 4, CoatWedge);
            VLine(g, 12, 13, 4, CoatWedge);
            Rect(g, 4, 13, 1, 3, CoatWsh);
            Rect(g, 11, 13, 1, 3, CoatWsh);
            // 听诊器
            VLine(g, 5, 14, 3, Steth);
            // 手臂
            Rect(g, 1, 12, 2, 4, CoatWsh);
            Px(g, 1, 16, SkinBase); Px(g, 2, 16, SkinBase);
            Rect(g, 13, 12, 2, 4, CoatWsh);
            Px(g, 13, 16, SkinBase); Px(g, 14, 16, SkinBase);
            // 腰带
            Rect(g, 3, 18, 10, 1, BeltBrown);
            DrawLegs(g, Pants, PantsHi, Shoe, ShoeHi);
            return RenderCharacter(g);
        }

        // ==================== 埃德加 ====================
        public static Bitmap CreateEdgar()
        {
            var g = NewGrid();
            var Hair   = Color.FromArgb(65, 45, 30);
            var HairHi = Color.FromArgb(90, 65, 45);
            var Suit   = Color.FromArgb(35, 40, 70);
            var SuitHi = Color.FromArgb(50, 55, 90);
            var SuitSh = Color.FromArgb(25, 30, 55);
            var Pants  = Color.FromArgb(45, 45, 55);
            var PantsHi = Color.FromArgb(60, 60, 70);
            var Shoe   = Color.FromArgb(50, 40, 35);
            var ShoeHi = Color.FromArgb(75, 60, 50);
            var Wine   = Color.FromArgb(100, 25, 30);
            var TieG   = Color.FromArgb(140, 120, 60);

            Rect(g, 4, 0, 8, 2, HairHi);
            Rect(g, 3, 1, 10, 2, Hair);
            Px(g, 5, 0, HairHi); Px(g, 11, 0, HairHi);
            DrawFace(g, Hair);
            Px(g, 4, 6, Color.FromArgb(255, 195, 180));
            Px(g, 11, 6, Color.FromArgb(255, 195, 180));
            // 半睁眼（微醺）
            Rect(g, 5, 5, 3, 1, EyeWhite);
            Px(g, 6, 5, IrisDark); Px(g, 6, 5, EyeShine);
            Rect(g, 9, 5, 3, 1, EyeWhite);
            Px(g, 10, 5, IrisDark); Px(g, 10, 5, EyeShine);
            HLine(g, 6, 9, 3, Mouth);
            // 颈 + 衬衫 + 领带
            Rect(g, 6, 10, 4, 1, SkinShadow);
            Rect(g, 5, 11, 6, 1, Color.FromArgb(220, 220, 230));
            VLine(g, 7, 12, 4, TieG); VLine(g, 8, 12, 4, TieG);
            DrawBody(g, Suit, SuitHi, SuitSh);
            Px(g, 5, 13, Wine); Px(g, 6, 13, Wine); Px(g, 5, 14, Wine);
            DrawLegs(g, Pants, PantsHi, Shoe, ShoeHi);
            return RenderCharacter(g);
        }

        // ==================== 莫里斯（管家） ====================
        public static Bitmap CreateMorris()
        {
            var g = NewGrid();
            var Hair   = Color.FromArgb(30, 25, 25);
            var HairHi = Color.FromArgb(60, 55, 55);
            var Suit   = Color.FromArgb(45, 45, 50);
            var SuitHi = Color.FromArgb(60, 60, 65);
            var SuitSh = Color.FromArgb(32, 32, 38);
            var Vest   = Color.FromArgb(70, 70, 75);
            var Pants  = Color.FromArgb(55, 50, 55);
            var PantsHi = Color.FromArgb(70, 65, 70);
            var Shoe   = Color.FromArgb(30, 28, 30);
            var ShoeHi = Color.FromArgb(50, 45, 50);
            var Bowtie = Color.FromArgb(160, 40, 40);
            var KeyGold = Color.FromArgb(210, 180, 50);

            Rect(g, 4, 0, 8, 2, HairHi);
            Rect(g, 3, 1, 10, 2, Hair);
            Rect(g, 4, 0, 8, 1, HairHi);
            DrawFace(g, Hair);
            HLine(g, 4, 4, 4, Hair); HLine(g, 8, 4, 4, Hair);
            HLine(g, 7, 9, 2, Mouth);
            // 颈 + 领结
            Rect(g, 6, 10, 4, 1, SkinShadow);
            Px(g, 6, 10, Bowtie); Px(g, 7, 10, Bowtie); Px(g, 8, 10, Bowtie); Px(g, 9, 10, Bowtie);
            Px(g, 7, 11, Bowtie); Px(g, 8, 11, Bowtie);
            // 衬衫
            Rect(g, 5, 11, 6, 1, Color.FromArgb(235, 235, 240));
            // 外套 + 马甲
            Rect(g, 3, 12, 10, 6, Suit);
            Rect(g, 4, 12, 8, 1, SuitHi);
            Rect(g, 4, 13, 8, 3, Vest);
            Rect(g, 5, 13, 6, 1, Color.FromArgb(80, 80, 85));
            // 手臂
            Rect(g, 1, 12, 2, 4, SuitSh);
            Px(g, 1, 16, SkinBase); Px(g, 2, 16, SkinBase);
            Rect(g, 13, 12, 2, 4, SuitSh);
            Px(g, 13, 16, SkinBase); Px(g, 14, 16, SkinBase);
            // 腰带 + 钥匙
            Rect(g, 3, 18, 10, 1, BeltBrown);
            Px(g, 3, 18, KeyGold); Px(g, 4, 18, KeyGold);
            Px(g, 3, 19, KeyGold);
            DrawLegs(g, Pants, PantsHi, Shoe, ShoeHi);
            return RenderCharacter(g);
        }
    }
}
