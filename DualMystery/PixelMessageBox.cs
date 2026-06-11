using System;
using System.Drawing;
using System.Windows.Forms;

namespace DualMystery
{
    /// <summary>
    /// 像素风格消息框 — 替代 MessageBox，统一游戏视觉
    /// 羊皮纸背景 + 2px 铜色边框 + 像素字体 + 像素按钮
    /// </summary>
    public class PixelMessageBox : Form
    {
        private Label lblMessage;
        private Button btnOk;

        private PixelMessageBox(string message, string title)
        {
            this.Text = title;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(253, 245, 230); // #FDF5E6 羊皮纸
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.ShowInTaskbar = false;

            // 像素边框绘制
            this.Paint += (s, e) =>
            {
                Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                // 外边框 2px
                using (Pen outer = new Pen(Theme.Border, 2))
                    e.Graphics.DrawRectangle(outer, rect);
                // 内边框 1px — 缩进 3px
                using (Pen inner = new Pen(Theme.BorderDark, 1))
                    e.Graphics.DrawRectangle(inner, 3, 3, this.Width - 7, this.Height - 7);
                // 标题栏背景
                using (Brush titleBg = new SolidBrush(Theme.BorderDark))
                    e.Graphics.FillRectangle(titleBg, 2, 2, this.Width - 4, 28);
                using (Font titleFont = Theme.GetFont(11f))
                using (Brush titleBrush = new SolidBrush(Color.FromArgb(253, 245, 230)))
                {
                    SizeF ts = e.Graphics.MeasureString(title, titleFont);
                    e.Graphics.DrawString(title, titleFont, titleBrush,
                        (this.Width - ts.Width) / 2, 4);
                }
            };

            // 消息文字 — 自动换行 + 动态尺寸
            lblMessage = new Label
            {
                Text = message,
                Font = Theme.GetFont(10f),
                ForeColor = Color.FromArgb(40, 30, 20),
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 4, 8, 4)
            };

            // 计算消息所需尺寸
            using (Graphics g = this.CreateGraphics())
            using (Font f = Theme.GetFont(10f))
            {
                SizeF textSize = g.MeasureString(message, f, 360);
                int msgW = Math.Min(400, (int)textSize.Width + 40);
                int msgH = Math.Max(50, (int)textSize.Height + 20);
                lblMessage.Size = new Size(msgW - 20, msgH);
                lblMessage.Location = new Point(10, 34);
            }

            // 确定按钮
            btnOk = new Button
            {
                Text = "✓  确  定",
                Font = Theme.GetFont(11f),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 36),
                DialogResult = DialogResult.OK
            };
            Theme.StyleButton(btnOk);
            btnOk.Click += (s, e) => this.DialogResult = DialogResult.OK;

            this.Controls.Add(lblMessage);
            this.Controls.Add(btnOk);

            // 窗口尺寸 = 消息区 + 按钮区 + 边框
            int formW = lblMessage.Width + 20;
            int formH = lblMessage.Bottom + btnOk.Height + 16;
            this.ClientSize = new Size(formW, formH);
            btnOk.Location = new Point((formW - btnOk.Width) / 2, lblMessage.Bottom + 4);

            // 快捷键
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape || e.KeyCode == Keys.Space)
                    this.DialogResult = DialogResult.OK;
            };
            this.AcceptButton = btnOk;

            // 可拖拽标题栏
            bool dragging = false; Point dragStart = Point.Empty;
            this.MouseDown += (s, e) => { if (e.Y < 30) { dragging = true; dragStart = e.Location; } };
            this.MouseMove += (s, e) => { if (dragging) this.Location = new Point(
                this.Location.X + e.X - dragStart.X,
                this.Location.Y + e.Y - dragStart.Y); };
            this.MouseUp += (s, e) => dragging = false;
        }

        /// <summary>显示像素风格消息框（模态）</summary>
        public static void Show(string message, string title = "提示")
        {
            using (var box = new PixelMessageBox(message, title))
            {
                box.ShowDialog();
            }
        }
    }
}
