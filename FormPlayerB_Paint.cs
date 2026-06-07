using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DualMystery
{
    public partial class FormPlayerB
    {
        // ========== 场景绘制 ==========
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            int vw = canvas.Width, vh = canvas.Height;
            float ox = playerPos.X - vw / 2f, oy = playerPos.Y - vh / 2f;
            ox = Math.Max(0, Math.Min(ox, mapWidth - vw));
            oy = Math.Max(0, Math.Min(oy, mapHeight - vh));

            g.FillRectangle(new SolidBrush(Color.FromArgb(48, 44, 40)), 0, 0, mapWidth, mapHeight);
            g.FillRectangle(new SolidBrush(Color.FromArgb(38, 34, 32)), 0, 360 - (int)oy, mapWidth, 440);
            g.DrawLine(new Pen(Color.FromArgb(100, 80, 50), 2), 0, 360 - (int)oy, mapWidth, 360 - (int)oy);

            using (Bitmap carpetTile = PixelIcons.CreateCarpet())
                for (int cx = 100; cx < 1100; cx += 32)
                    g.DrawImage(carpetTile, cx - (int)ox, 540 - (int)oy, 32, 20);

            int lampFlicker = (animFrame % 30 < 15 ? 0 : 40);
            for (int lx = 150; lx < 1100; lx += 250)
            {
                int lsx = lx - (int)ox, lsy = 190 - (int)oy;
                if (lsx + 32 < 0 || lsx > vw) continue;
                using (Brush glow1 = new SolidBrush(Color.FromArgb(40 + lampFlicker / 2, 255, 240, 180)))
                    g.FillEllipse(glow1, lsx - 8, lsy - 2, 44, 40);
                using (Brush glow2 = new SolidBrush(Color.FromArgb(70 + lampFlicker, 255, 220, 140)))
                    g.FillEllipse(glow2, lsx - 2, lsy + 4, 32, 28);
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 50, 40)), lsx + 2, lsy + 20, 24, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 70, 55)), lsx + 4, lsy + 20, 10, 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 35, 30)), lsx + 6, lsy + 8, 16, 14);
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 55, 45)), lsx + 4, lsy + 6, 20, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 45, 35)), lsx + 8, lsy + 20, 12, 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 220, 140)), lsx + 10, lsy + 12, 8, 6);
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 180, 60)), lsx + 12, lsy + 14, 4, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, 110, 90)), lsx + 6, lsy + 8, 3, 10);
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 65, 50)), lsx + 8, lsy + 2, 12, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 85, 65)), lsx + 10, lsy, 8, 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 50, 40)), lsx + 12, lsy - 4, 4, 6);
            }

            for (int px2 = 350; px2 < 1000; px2 += 280)
            {
                int paintX = px2 - (int)ox, paintY = 140 - (int)oy;
                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 160, 110)), paintX, paintY, 40, 50);
                g.DrawRectangle(new Pen(Color.FromArgb(60, 50, 30), 2), paintX, paintY, 40, 50);
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 140, 180)), paintX + 4, paintY + 6, 32, 15);
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 120, 60)), paintX + 4, paintY + 21, 32, 12);
                g.FillEllipse(new SolidBrush(Color.FromArgb(240, 220, 140)), paintX + 22, paintY + 8, 10, 10);
            }

            int vaseX = 1050 - (int)ox, vaseY = 420 - (int)oy;
            using (Bitmap vase = PixelIcons.CreateVase())
                g.DrawImage(vase, vaseX, vaseY, 32, 40);
            g.FillRectangle(new SolidBrush(Color.FromArgb(80, 60, 40)), vaseX - 4, vaseY + 38, 40, 10);

            using (Font hintFont = new Font("Georgia", 7, FontStyle.Bold))
            using (SolidBrush hintBrush = new SolidBrush(Color.Yellow))
            using (SolidBrush doneBrush = new SolidBrush(Color.Gray))
            {
                foreach (var item in sceneItems)
                {
                    int sx = item.Rect.X - (int)ox, sy = item.Rect.Y - (int)oy;
                    if (sx + item.Rect.Width < 0 || sx > vw || sy + item.Rect.Height < 0 || sy > vh) continue;
                    if (item.Icon != null) g.DrawImage(item.Icon, sx, sy, item.Rect.Width, item.Rect.Height);
                    else { g.FillRectangle(Brushes.DarkOliveGreen, sx, sy, item.Rect.Width, item.Rect.Height); g.DrawRectangle(Pens.Black, sx, sy, item.Rect.Width, item.Rect.Height); }
                    g.DrawString(item.Name, itemFont, Brushes.White, sx, sy - 12);

                    // 交互距离视觉提示
                    if (IsNearItem(item))
                    {
                        bool discovered = false;
                        if (!string.IsNullOrEmpty(item.ClueId))
                        {
                            var gmClue = GameManager.AllClues.FirstOrDefault(c => c.Id == item.ClueId);
                            if (gmClue != null && gmClue.IsDiscovered) discovered = true;
                        }
                        string hintText;
                        Color hintColor;
                        if (discovered)
                        {
                            hintText = "（已调查）";
                            hintColor = Color.Gray;
                        }
                        else if (item.IsPhone)
                        {
                            hintText = "按 P 拨打电话";
                            hintColor = Color.Cyan;
                        }
                        else
                        {
                            hintText = "按 P 调查";
                            hintColor = Color.Yellow;
                        }
                        SizeF hintSize = g.MeasureString(hintText, hintFont);
                        float hintX = sx + item.Rect.Width / 2 - hintSize.Width / 2;
                        float hintY = sy - 24;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(160, 0, 0, 0)),
                            hintX - 2, hintY, hintSize.Width + 4, hintSize.Height + 2);
                        hintBrush.Color = hintColor;
                        g.DrawString(hintText, hintFont, hintBrush, hintX, hintY);
                    }
                }
            }

            using (Font hintFont = new Font("Georgia", 7, FontStyle.Bold))
            using (SolidBrush hintBrush = new SolidBrush(Color.Cyan))
            {
                foreach (var npc in npcList)
                {
                    int sx = npc.Rect.X - (int)ox, sy = npc.Rect.Y - (int)oy;
                    if (npc.Icon != null) g.DrawImage(npc.Icon, sx, sy, npc.Rect.Width, npc.Rect.Height);
                    g.DrawString(npc.Name, itemFont, Brushes.Yellow, sx, sy - 12);
                    if (npc.Name.Contains("埃德加"))
                        using (Bitmap wine = PixelIcons.CreateWineGlass())
                            g.DrawImage(wine, sx + 40, sy + 14, 18, 22);
                    else if (npc.Name.Contains("莫里斯"))
                        using (Bitmap keys = PixelIcons.CreateKeyRing())
                            g.DrawImage(keys, sx - 20, sy + 12, 20, 22);

                    // 交互距离视觉提示
                    if (IsNearNPC(npc))
                    {
                        string hintText = "按 P 对话";
                        SizeF hintSize = g.MeasureString(hintText, hintFont);
                        float hintX = sx + npc.Rect.Width / 2 - hintSize.Width / 2;
                        float hintY = sy - 24;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(160, 0, 0, 0)),
                            hintX - 2, hintY, hintSize.Width + 4, hintSize.Height + 2);
                        hintBrush.Color = Color.Cyan;
                        g.DrawString(hintText, hintFont, hintBrush, hintX, hintY);
                    }
                }
            }

            int px = (int)(playerPos.X - ox) - 24;
            int py = (int)(playerPos.Y - oy) - 36;
            if (isCallingOut && tmrAnimate != null && tmrAnimate.Enabled)
                g.DrawImage(picCharacterAnimFrame ? charStomp : charIdle, px, py, 48, 48);
            else
                g.DrawImage(charIdle, px, py, 48, 48);

            DrawDialogueBubble(g, px, py, vw);
        }

        private void DrawDialogueBubble(Graphics g, int px, int py, int vw)
        {
            if (string.IsNullOrEmpty(dialogueText)) return;
            const int maxBubbleWidth = 280;
            string hint = isLastDialogue ? "鼠标单击 / 按P关闭对话" : "鼠标单击 / 按P继续对话";
            using (Font hintFont = new Font("Georgia", 7f, FontStyle.Regular))
            {
                SizeF textSize = g.MeasureString(dialogueText, dialogueFont, maxBubbleWidth);
                float bubbleW = textSize.Width + 12;
                float textH = textSize.Height + 4;
                float bubbleH = textH + 6;
                // 始终显示提示（最后一句提示"关闭"）
                SizeF hintSize = g.MeasureString(hint, hintFont, maxBubbleWidth);
                bubbleW = Math.Max(bubbleW, hintSize.Width + 12);
                float hintH = hintSize.Height + 2;
                bubbleH += hintH;

                float bubbleX = px - 5;
                float bubbleY = py - bubbleH - 10;
                if (bubbleX < 0) bubbleX = 0;
                if (bubbleX + bubbleW > vw) bubbleX = vw - bubbleW;
                if (bubbleY < 0) bubbleY = py + 48;
                g.FillRectangle(Brushes.White, bubbleX, bubbleY, bubbleW, bubbleH);
                g.DrawRectangle(Pens.Black, bubbleX, bubbleY, bubbleW, bubbleH);
                g.DrawString(dialogueText, dialogueFont, Brushes.Black,
                    new RectangleF(bubbleX + 5, bubbleY + 2, bubbleW - 10, textH));
                g.DrawString(hint, hintFont, Brushes.DarkRed,
                    new RectangleF(bubbleX + 5, bubbleY + 2 + textH, bubbleW - 10, hintH));
            }
        }
    }
}
