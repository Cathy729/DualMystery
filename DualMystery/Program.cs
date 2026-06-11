using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DualMystery
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 初始化全局主题（加载像素字体）
            Theme.Initialize();

            // 启动 TCP 游戏服务器（后台线程）
            var server = new GameServer();
            server.Start();

            Application.Run(new FormMain());

            // 主窗口关闭后停止服务器
            server.Stop();
        }
    }
}
