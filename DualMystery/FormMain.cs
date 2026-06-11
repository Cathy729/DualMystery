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
            this.BackColor = Theme.BgMain;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(800, 600);
            this.KeyPreview = true;

            // 标题
            lblTitle = new Label
            {
                Text = Theme.DecorateTitle("双 线 谜 案"),
                Font = Theme.GetFont(28f),
                ForeColor = Theme.Accent,
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            // 标题分隔线
            this.Controls.Add(Theme.CreateTitleSeparator());

            // 故事背景
            lblStory = new Label
            {
                Text = "1930年代，古怪的收藏家霍华德被刺死在自己的书房中，\n房门反锁，警方派遣两位侦探分别进入书房和走廊调查。\n两人唯一的联络工具是一台老式壁挂电话……",
                Font = Theme.GetFont(11f),
                ForeColor = Theme.Accent,
                BackColor = Theme.BgPanel,
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
                Font = Theme.GetFont(14f),
                Size = new Size(200, 50)
            };
            Theme.StyleButton(btnStudy);
            btnStudy.Click += BtnStudy_Click;

            btnCorridor = new Button
            {
                Text = "进入走廊 (侦探B)",
                Font = Theme.GetFont(14f),
                Size = new Size(200, 50)
            };
            Theme.StyleButton(btnCorridor);
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

            // 音频诊断已移至 MusicManager 内部日志，启动时不再播放测试音频
            // 如需手动诊断，取消下面注释后运行:
            // this.Load += (s, e) => DiagnoseAudio();
        }

        /// <summary>
        /// 音频系统诊断 — 验证资源嵌入 + NAudio 播放链
        /// 结果输出到 /tmp/DualMystery_Music.log
        /// </summary>
        private void DiagnoseAudio()
        {
            System.Diagnostics.Debug.WriteLine("=== 音频系统诊断开始 ===");

            // 第1步：验证每个 MP3 资源字节长度
            var resources = new (string Name, byte[] Data)[]
            {
                ("bgm1",        Properties.Resources.bgm1),
                ("bgm2",        Properties.Resources.bgm2),
                ("truth",       Properties.Resources.truth),
                ("conan_theme", Properties.Resources.conan_theme),
            };

            foreach (var (name, data) in resources)
            {
                int len = data?.Length ?? 0;
                string status = len > 0 ? $"✅ {len} bytes" : "❌ NULL 或空!";
                System.Diagnostics.Debug.WriteLine($"[Diagnose] 资源 {name}: {status}");
                MusicManager_Log($"[Diagnose] 资源 {name}: {status}");
            }

            // 第2步：已移除 — truth.mp3 仅供结局动画使用，启动时不再播放测试音频
            // NAudio 播放链验证已通过（NAudio.WinMM 2.3.0 已安装），无需每次启动复验
        }

        private static void MusicManager_Log(string msg)
        {
            try
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DualMystery_Music.log"),
                    $"{DateTime.Now:HH:mm:ss.fff} {msg}\n");
            }
            catch { }
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