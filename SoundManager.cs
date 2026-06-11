using System;
using System.Threading.Tasks;

namespace DualMystery
{
    /// <summary>
    /// 像素风格音效管理器，使用 Console.Beep 生成简单音效。
    /// 所有播放方法均异步执行，不阻塞 UI 线程。
    /// </summary>
    public static class SoundManager
    {
        /// <summary>发现线索 / 调查物品 — 短促叮咚声</summary>
        public static void PlayDiscovery()
        {
            Task.Run(() =>
            {
                try { Console.Beep(1500, 80); }
                catch { /* 某些系统不支持 Beep */ }
            });
        }

        /// <summary>保险箱解锁 — 三连升调</summary>
        public static void PlaySafeUnlock()
        {
            Task.Run(() =>
            {
                try
                {
                    Console.Beep(800, 120);
                    System.Threading.Thread.Sleep(80);
                    Console.Beep(1200, 120);
                    System.Threading.Thread.Sleep(80);
                    Console.Beep(1600, 200);
                }
                catch { }
            });
        }

        /// <summary>电话振铃 — 双音交替</summary>
        public static void PlayRing()
        {
            Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Console.Beep(880, 150);
                        System.Threading.Thread.Sleep(100);
                        Console.Beep(1100, 150);
                        System.Threading.Thread.Sleep(200);
                    }
                }
                catch { }
            });
        }

        /// <summary>指认成功 / 结局 — C-E-G-C' 上行庆祝</summary>
        public static void PlaySuccess()
        {
            Task.Run(() =>
            {
                try
                {
                    Console.Beep(523, 150);  // C
                    System.Threading.Thread.Sleep(100);
                    Console.Beep(659, 150);  // E
                    System.Threading.Thread.Sleep(100);
                    Console.Beep(784, 150);  // G
                    System.Threading.Thread.Sleep(100);
                    Console.Beep(1047, 300); // C'
                }
                catch { }
            });
        }

        /// <summary>操作失败 / 错误 — 低沉短鸣</summary>
        public static void PlayError()
        {
            Task.Run(() =>
            {
                try { Console.Beep(250, 200); }
                catch { }
            });
        }

        /// <summary>电话接听 — 短促确认音</summary>
        public static void PlayCallAccept()
        {
            Task.Run(() =>
            {
                try
                {
                    Console.Beep(600, 80);
                    System.Threading.Thread.Sleep(60);
                    Console.Beep(900, 80);
                }
                catch { }
            });
        }

        /// <summary>电话挂断 — 降调</summary>
        public static void PlayCallEnd()
        {
            Task.Run(() =>
            {
                try
                {
                    Console.Beep(900, 80);
                    System.Threading.Thread.Sleep(60);
                    Console.Beep(600, 80);
                }
                catch { }
            });
        }

        /// <summary>NPC 对话开始 — 轻声提示</summary>
        public static void PlayDialogue()
        {
            Task.Run(() =>
            {
                try { Console.Beep(1000, 50); }
                catch { }
            });
        }

        /// <summary>电话振铃单次嘀声（Timer 驱动，每 300ms 一次）</summary>
        public static void PlayRingTick()
        {
            Task.Run(() =>
            {
                try { Console.Beep(1000, 60); }
                catch { }
            });
        }

        /// <summary>无可交互对象时的反馈</summary>
        public static void PlayNoInteraction()
        {
            Task.Run(() =>
            {
                try { Console.Beep(200, 100); }
                catch { }
            });
        }
    }
}
