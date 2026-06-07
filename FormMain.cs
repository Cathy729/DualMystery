using System;
using System.Drawing;
using System.Windows.Forms;

namespace DualMystery
{
    public partial class FormMain : Form
    {
        private Label lblTitle;
        private Label lblStory;
        private Button btnStudy;
        private Button btnCorridor;

        public FormMain()
        {
            InitializeComponent();
            InitializeCustomUI();
        }

        private void InitializeCustomUI()
        {
            this.Text = "双线谜案";
            this.BackColor = Color.FromArgb(43, 58, 66);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(800, 600);
            this.KeyPreview = true;

            // 标题
            lblTitle = new Label
            {
                Text = "双线谜案",
                Font = new Font("Georgia", 28f),
                ForeColor = Color.FromArgb(245, 240, 230),
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            // 故事背景
            lblStory = new Label
            {
                Text = "1930年代，古怪的收藏家霍华德被刺死在自己的书房中，\n房门反锁，警方派遣两位侦探分别进入书房和走廊调查。\n两人唯一的联络工具是一台老式壁挂电话……",
                Font = new Font("Georgia", 11f),
                ForeColor = Color.FromArgb(201, 169, 110),
                BackColor = Color.FromArgb(35, 48, 54),
                AutoSize = false,
                Size = new Size(600, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.ClientSize.Width - 600) / 2, lblTitle.Bottom + 20)
            };
            this.Controls.Add(lblStory);

            // 按钮
            btnStudy = new Button
            {
                Text = "进入书房 (侦探A)",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(58, 76, 84),
                ForeColor = Color.FromArgb(245, 240, 230),
                FlatAppearance = { BorderColor = Color.FromArgb(184, 115, 51) },
                Font = new Font(this.Font.FontFamily, 14f),
                Size = new Size(200, 50)
            };
            btnStudy.Click += BtnStudy_Click;

            btnCorridor = new Button
            {
                Text = "进入走廊 (侦探B)",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(58, 76, 84),
                ForeColor = Color.FromArgb(245, 240, 230),
                FlatAppearance = { BorderColor = Color.FromArgb(184, 115, 51) },
                Font = new Font(this.Font.FontFamily, 14f),
                Size = new Size(200, 50)
            };
            btnCorridor.Click += BtnCorridor_Click;

            // 居中布局
            int buttonY = lblStory.Bottom + 60;
            int totalWidth = btnStudy.Width + 40 + btnCorridor.Width;
            int startX = (this.ClientSize.Width - totalWidth) / 2;

            btnStudy.Location = new Point(startX, buttonY);
            btnCorridor.Location = new Point(startX + btnStudy.Width + 40, buttonY);

            this.Controls.Add(btnStudy);
            this.Controls.Add(btnCorridor);

            // 窗口大小变化时动态居中
            this.Resize += (s, e) =>
            {
                lblStory.Location = new Point((this.ClientSize.Width - 600) / 2, lblTitle.Bottom + 20);
                int y = lblStory.Bottom + 60;
                int tw = btnStudy.Width + 40 + btnCorridor.Width;
                int sx = (this.ClientSize.Width - tw) / 2;
                btnStudy.Location = new Point(sx, y);
                btnCorridor.Location = new Point(sx + btnStudy.Width + 40, y);
            };

            // 关闭主窗口时彻底退出程序
            this.FormClosing += (s, e) => Application.Exit();
            // 按 Esc 键退出
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Application.Exit();
                }
            };
        }

        private void BtnStudy_Click(object sender, EventArgs e)
        {
            var formPlayerA = new FormPlayerA();
            formPlayerA.FormClosed += (s, args) => this.Show();
            formPlayerA.Show();
        }

        private void BtnCorridor_Click(object sender, EventArgs e)
        {
            var formPlayerB = new FormPlayerB();
            formPlayerB.FormClosed += (s, args) => this.Show();
            formPlayerB.Show();
        }
    }
}