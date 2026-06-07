using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DualMystery
{
    public partial class FormPlayerA
    {
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
            g.FillRectangle(new SolidBrush(Color.FromArgb(35, 30, 28)), 0, 350 - (int)oy, mapWidth, 450);
            g.DrawLine(new Pen(Color.FromArgb(80, 60, 40), 2), 0, 350 - (int)oy, mapWidth, 350 - (int)oy);

            // ---- 地毯 ----
            int carpetY = 520 - (int)oy;
            g.FillRectangle(new SolidBrush(Color.FromArgb(120, 30, 30)), 290 - (int)ox, carpetY, 620, 30);
            g.FillRectangle(new SolidBrush(Color.FromArgb(80, 20, 20)), 290 - (int)ox, carpetY, 620, 3);
            g.FillRectangle(new SolidBrush(Color.FromArgb(80, 20, 20)), 290 - (int)ox, carpetY + 27, 620, 3);
            using (Bitmap carpetTile = PixelIcons.CreateCarpet())
                for (int cx = 300; cx < 900; cx += 32)
                    g.DrawImage(carpetTile, cx - (int)ox, carpetY, 32, 24);

            // ---- 壁炉 ----
            int fireplaceX = 80 - (int)ox, fireplaceY = 280 - (int)oy;
            int fpFrameIdx = (animFrame / 30) % 3;
            if (fireplaceFrames != null && fireplaceFrames[fpFrameIdx] != null)
                g.DrawImage(fireplaceFrames[fpFrameIdx], fireplaceX, fireplaceY, 80, 80);
            g.FillRectangle(new SolidBrush(Color.FromArgb(90, 60, 30)), fireplaceX + 60, fireplaceY + 30, 30, 4);

            // ---- 吊灯 ----
            int chandelierX = 500 - (int)ox, chandelierY = 10 - (int)oy;
            int chFrameIdx = (animFrame / 20) % 3;
            if (chandelierFrames != null && chandelierFrames[chFrameIdx] != null)
                g.DrawImage(chandelierFrames[chFrameIdx], chandelierX, chandelierY, 60, 60);

            // ---- 书架 ----
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
            using (Bitmap curtain = PixelIcons.CreateCurtain())
            {
                g.DrawImage(curtain, winX - 14, winY - 4, 16, 88);
                g.DrawImage(curtain, winX + 58, winY - 4, 16, 88);
            }

            // ---- 尸体（横向放置） ----
            int hbX = 150 - (int)ox, hbY = 450 - (int)oy;
            g.FillEllipse(new SolidBrush(Color.FromArgb(60, 0, 0, 0)), hbX - 2, hbY + 18, 62, 8);
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 40, 40)), hbX + 40, hbY + 8, 14, 8);
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 40, 40)), hbX + 42, hbY + 6, 6, 10);
            g.FillRectangle(new SolidBrush(Color.FromArgb(20, 20, 20)), hbX + 52, hbY + 6, 6, 8);
            g.FillRectangle(new SolidBrush(Color.FromArgb(65, 55, 45)), hbX + 18, hbY + 2, 24, 17);
            g.FillRectangle(new SolidBrush(Color.FromArgb(210, 200, 190)), hbX + 18, hbY + 2, 12, 10);
            g.FillEllipse(new SolidBrush(Color.FromArgb(180, 20, 20)), hbX + 22, hbY + 4, 14, 10);
            g.FillEllipse(new SolidBrush(Color.FromArgb(120, 10, 10)), hbX + 26, hbY + 6, 8, 6);
            g.FillEllipse(new SolidBrush(Color.FromArgb(100, 8, 8)), hbX + 30, hbY + 3, 6, 6);
            g.FillRectangle(new SolidBrush(Color.FromArgb(65, 55, 45)), hbX + 10, hbY + 4, 8, 13);
            g.FillRectangle(new SolidBrush(Color.FromArgb(65, 55, 45)), hbX + 40, hbY + 4, 8, 13);
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 224, 189)), hbX + 10, hbY + 14, 4, 3);
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 224, 189)), hbX + 44, hbY + 14, 4, 3);
            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 224, 189)), hbX, hbY, 20, 16);
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 30, 20)), hbX, hbY, 20, 5);
            g.DrawLine(new Pen(Color.FromArgb(40, 30, 20)), hbX + 4, hbY + 7, hbX + 8, hbY + 7);
            g.DrawLine(new Pen(Color.FromArgb(40, 30, 20)), hbX + 12, hbY + 7, hbX + 16, hbY + 7);

            // ---- 物品 ----
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

            // ---- NPC ----
            foreach (var npc in npcList)
            {
                int sx = npc.Rect.X - (int)ox, sy = npc.Rect.Y - (int)oy;
                if (npc.Icon != null)
                    g.DrawImage(npc.Icon, sx, sy, npc.Rect.Width, npc.Rect.Height);
                g.DrawString(npc.Name, itemFont, Brushes.Yellow, sx, sy - 12);
                if (npc.Name.Contains("贝蒂"))
                    using (Bitmap broom = PixelIcons.CreateBroom())
                        g.DrawImage(broom, sx + 40, sy + 10, 20, 26);
                else if (npc.Name.Contains("格雷"))
                    using (Bitmap bag = PixelIcons.CreateMedicalBag())
                        g.DrawImage(bag, sx - 22, sy + 10, 22, 24);
            }

            // ---- 玩家 ----
            int px = (int)(playerPos.X - ox) - 24;
            int py = (int)(playerPos.Y - oy) - 36;
            if (isCallingOut && tmrAnimate != null && tmrAnimate.Enabled)
                g.DrawImage(picCharacterAnimFrame ? charStomp : charIdle, px, py, 48, 48);
            else
                g.DrawImage(charIdle, px, py, 48, 48);

            // ---- 对话气泡 ----
            DrawDialogueBubble(g, px, py, vw);
        }

        private void DrawDialogueBubble(Graphics g, int px, int py, int vw)
        {
            if (string.IsNullOrEmpty(dialogueText)) return;
            const int maxBubbleWidth = 280;
            string hint = "鼠标单击 / 按E继续对话";
            using (Font hintFont = new Font("Georgia", 7f, FontStyle.Italic))
            {
                SizeF textSize = g.MeasureString(dialogueText, dialogueFont, maxBubbleWidth);
                SizeF hintSize = g.MeasureString(hint, hintFont, maxBubbleWidth);
                float bubbleW = Math.Max(textSize.Width, hintSize.Width) + 12;
                float textH = textSize.Height + 4;
                float hintH = hintSize.Height + 2;
                float bubbleH = textH + hintH + 6;
                float bubbleX = px - 5;
                float bubbleY = py - bubbleH - 10;
                if (bubbleX < 0) bubbleX = 0;
                if (bubbleX + bubbleW > vw) bubbleX = vw - bubbleW;
                if (bubbleY < 0) bubbleY = py + 48;
                g.FillRectangle(Brushes.White, bubbleX, bubbleY, bubbleW, bubbleH);
                g.DrawRectangle(Pens.Black, bubbleX, bubbleY, bubbleW, bubbleH);
                g.DrawString(dialogueText, dialogueFont, Brushes.Black,
                    new RectangleF(bubbleX + 5, bubbleY + 2, bubbleW - 10, textH));
                g.DrawString(hint, hintFont, Brushes.Gray,
                    new RectangleF(bubbleX + 5, bubbleY + 2 + textH, bubbleW - 10, hintH));
            }
        }
    }
}
