using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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

            // ---- 背景：天花板 + 墙壁底色 ----
            g.FillRectangle(bgFillBrush, 0, 0, mapWidth, mapHeight);

            // ---- 墙壁竖条纹壁纸（每 20px 一条浅色竖线） ----
            int wallBottom = 350 - (int)oy;
            if (wallBottom > 0)
            {
                int stripeStartX = ((int)ox / 20) * 20;
                for (int wx = stripeStartX; wx < mapWidth && wx - (int)ox < vw; wx += 20)
                {
                    int sx = wx - (int)ox;
                    if (sx >= 0)
                        g.DrawLine(wallStripePen, sx, 0, sx, wallBottom);
                }
            }

            // ---- 地板纹理（4×4 棋盘格，从 y=400 开始） ----
            int floorStartY = 400;
            int tileSize = 64;
            int startTileX = ((int)ox / tileSize) * tileSize;
            int startTileY = (floorStartY / tileSize) * tileSize;
            for (int ty = startTileY; ty < mapHeight; ty += tileSize)
            {
                int sy = ty - (int)oy;
                if (sy + tileSize < 0 || sy > vh) continue;
                for (int tx = startTileX; tx < mapWidth; tx += tileSize)
                {
                    int sx = tx - (int)ox;
                    if (sx + tileSize < 0 || sx > vw) continue;
                    g.DrawImage(floorTileA, sx, sy, tileSize, tileSize);
                }
            }

            // 墙-地分界线
            g.DrawLine(floorLinePenA, 0, 350 - (int)oy, mapWidth, 350 - (int)oy);

            // ---- 壁炉火焰闪烁 ----
            int fpX = 80 - (int)ox, fpY = 280 - (int)oy;
            // 每 30 帧（~500ms）更新火焰亮度
            int flickerPhase = (animFrame / 30) % 5;
            int flickerBrightness = 160 + flickerPhase * 15;
            int flameR = Math.Min(255, flickerBrightness + 60);
            int flameG = Math.Min(255, flickerBrightness / 2 + 20);
            int flameB = Math.Min(50, flickerPhase * 8);
            using (SolidBrush flameBrush = new SolidBrush(Color.FromArgb(flameR, flameG, flameB)))
            {
                // 在壁炉内部绘制火焰像素块
                g.FillRectangle(flameBrush, fpX + 18, fpY + 42, 20, 14);
                g.FillRectangle(flameBrush, fpX + 22, fpY + 38, 12, 10);
                // 第二团小火焰
                int flicker2 = (flickerPhase + 2) % 5;
                int r2 = Math.Min(255, 150 + flicker2 * 18);
                int g2 = Math.Min(255, flicker2 * 20 + 15);
                using (SolidBrush flame2 = new SolidBrush(Color.FromArgb(r2, g2, 0)))
                    g.FillRectangle(flame2, fpX + 30, fpY + 44, 10, 8);
            }

            // ---- 像素地毯（书桌下方，暗红 #6B2D2D） ----
            int rugX = 430 - (int)ox, rugY = 500 - (int)oy;
            int rugW = 140, rugH = 50;
            // 地毯主体
            g.FillRectangle(new SolidBrush(Color.FromArgb(0x6B, 0x2D, 0x2D)), rugX, rugY, rugW, rugH);
            // 边缘深色像素点装饰（上下边）
            using (SolidBrush edgeBrush = new SolidBrush(Color.FromArgb(0x3A, 0x15, 0x15)))
            {
                for (int ex = rugX; ex < rugX + rugW; ex += 4)
                {
                    g.FillRectangle(edgeBrush, ex, rugY, 3, 2);
                    g.FillRectangle(edgeBrush, ex, rugY + rugH - 2, 3, 2);
                }
                for (int ey = rugY; ey < rugY + rugH; ey += 4)
                {
                    g.FillRectangle(edgeBrush, rugX, ey, 2, 3);
                    g.FillRectangle(edgeBrush, rugX + rugW - 2, ey, 2, 3);
                }
            }
            // 地毯中心菱形装饰
            using (SolidBrush diamondBrush = new SolidBrush(Color.FromArgb(0x8B, 0x3D, 0x3D)))
            {
                PointF[] diamond = {
                    new PointF(rugX + rugW/2f, rugY + 4),
                    new PointF(rugX + rugW - 6, rugY + rugH/2f),
                    new PointF(rugX + rugW/2f, rugY + rugH - 4),
                    new PointF(rugX + 6, rugY + rugH/2f)
                };
                g.FillPolygon(diamondBrush, diamond);
            }

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
            using (Font hintFont = Theme.GetFont(7f))
            using (SolidBrush hintBrush = new SolidBrush(Color.Yellow))
            using (SolidBrush doneBrush = new SolidBrush(Color.Gray))
            {
                for (int i = 0; i < sceneItems.Count; i++)
                {
                    var item = sceneItems[i];
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

                    // 鼠标悬停高亮边框
                    if (i == hoveredItemIndex)
                    {
                        using (Pen highlightPen = new Pen(Theme.Accent, 2))
                            g.DrawRectangle(highlightPen, sx - 2, sy - 2,
                                item.Rect.Width + 4, item.Rect.Height + 4);
                        // 像素小箭头指示
                        DrawPixelArrow(g, sx + item.Rect.Width / 2, sy - 6);
                    }

                    // 点击反馈 ✓ 图标
                    if (i == feedbackItemIndex && tmrFeedback != null && tmrFeedback.Enabled)
                    {
                        using (Font checkFont = Theme.GetFont(14f))
                        using (Brush checkBrush = new SolidBrush(Color.LimeGreen))
                            g.DrawString("✓", checkFont, checkBrush, sx + item.Rect.Width, sy - 10);
                    }

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
                        else if (item.Type == ItemType.Phone)
                        {
                            hintText = "按 E 拨打电话";
                            hintColor = Color.Cyan;
                        }
                        else
                        {
                            hintText = "按 E 调查";
                            hintColor = Color.Yellow;
                        }
                        SizeF hintSize = g.MeasureString(hintText, hintFont);
                        float hintX = sx + item.Rect.Width / 2 - hintSize.Width / 2;
                        float hintY = sy - 24;
                        // 绘制提示背景
                        g.FillRectangle(hintBgBrush,
                            hintX - 2, hintY, hintSize.Width + 4, hintSize.Height + 2);
                        hintBrush.Color = hintColor;
                        g.DrawString(hintText, hintFont, hintBrush, hintX, hintY);
                    }
                }
            }

            // ---- NPC ----
            using (Font hintFont = Theme.GetFont(7f))
            using (SolidBrush hintBrush = new SolidBrush(Color.Cyan))
            {
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

                    // 交互距离视觉提示
                    if (IsNearNPC(npc))
                    {
                        string hintText = "按 E 对话";
                        SizeF hintSize = g.MeasureString(hintText, hintFont);
                        float hintX = sx + npc.Rect.Width / 2 - hintSize.Width / 2;
                        float hintY = sy - 24;
                        g.FillRectangle(hintBgBrush,
                            hintX - 2, hintY, hintSize.Width + 4, hintSize.Height + 2);
                        hintBrush.Color = Color.Cyan;
                        g.DrawString(hintText, hintFont, hintBrush, hintX, hintY);
                    }
                }
            }

            // ---- 玩家 ----
            int px = (int)(playerPos.X - ox) - 24;
            int py = (int)(playerPos.Y - oy) - 60;
            if (isCallingOut && tmrAnimate != null && tmrAnimate.Enabled)
                g.DrawImage(picCharacterAnimFrame ? charStomp : charIdle, px, py, 48, 72);
            else
                g.DrawImage(charIdle, px, py, 48, 72);

            // ---- 对话气泡 ----
            DrawDialogueBubble(g, px, py, vw);

            // ---- 像素双线边框 ----
            Theme.DrawDoubleLineBorder(g, new Rectangle(0, 0, vw, vh), Theme.BorderDark, Theme.BorderLight);
        }

        /// <summary>绘制像素风格小箭头指示器</summary>
        private static void DrawPixelArrow(Graphics g, float cx, float topY)
        {
            // 倒三角箭头 ▼
            PointF[] arrow = {
                new PointF(cx - 4, topY + 4),
                new PointF(cx + 4, topY + 4),
                new PointF(cx, topY - 2)
            };
            using (Brush ab = new SolidBrush(Theme.Accent))
                g.FillPolygon(ab, arrow);
            using (Pen ap = new Pen(Theme.BorderDark, 1))
                g.DrawPolygon(ap, arrow);
        }

        private void DrawDialogueBubble(Graphics g, int px, int py, int vw)
        {
            if (string.IsNullOrEmpty(dialogueText)) return;
            const int maxBubbleWidth = 280;
            string hint = isLastDialogue ? "鼠标单击 / 按E关闭对话" : "鼠标单击 / 按E继续对话";
            using (Font hintFont = Theme.GetFont(7f))
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
