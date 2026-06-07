using System;
using System.Collections.Generic;
using System.Linq;

namespace DualMystery
{
    public static class GameManager
    {
        public static List<Clue> AllClues = new List<Clue>();

        public static event System.Action<string> OnClueDiscovered;
        public static event System.Action OnSafeUnlocked;
        // 指认相关字段
        private static string accusationA = null;
        private static string accusationB = null;
        // 缓存最近一次指认结果，防止 handler 中 ResetAccusation 清空后第二个 handler 读到 null
        public static string LastAccusationA { get; private set; } = null;
        public static string LastAccusationB { get; private set; } = null;
        // 标记本次结果是否已有窗口展示过弹窗，避免双方同时弹窗
        public static bool ResultMessageShown { get; set; } = false;
        public static event Action<bool> OnAccusationResult; // true = 两人都指认莫里斯

        public static void SubmitAccusation(string player, string suspect)
        {
            if (player == "A") accusationA = suspect;
            else accusationB = suspect;

            if (accusationA != null && accusationB != null)
            {
                // 缓存本次结果，即使后续 ResetAccusation 清空也不影响第二个 handler
                LastAccusationA = accusationA;
                LastAccusationB = accusationB;
                ResultMessageShown = false;
                bool bothCorrect = (accusationA == "莫里斯" && accusationB == "莫里斯");
                OnAccusationResult?.Invoke(bothCorrect);
            }
        }

        public static void ResetAccusation()
        {
            accusationA = null;
            accusationB = null;
            // 注意：LastAccusationA/B 和 ResultMessageShown 不在此处清理，
            // 因为两个窗体的 handler 都会读取它们；由下次 SubmitAccusation 覆盖。
        }

        static GameManager()
        {
            // 书房线索
            AllClues.Add(new Clue { Id = "knife", Name = "凶器刀", Description = "一把锋利的短刀，刀柄刻着模糊的字母“M”。" });
            AllClues.Add(new Clue { Id = "burnt_letter", Name = "烧毁的信", Description = "信纸边缘烧焦，残留文字：“…今晚别告诉任何人，否则你的秘密…”" });
            AllClues.Add(new Clue { Id = "bible_note", Name = "圣经暗格纸条", Description = "一张泛黄的纸条，写着：“密码是老爷最厌恶的日子。”" });
            AllClues.Add(new Clue { Id = "handkerchief", Name = "带血手帕", Description = "白色手帕上绣着两个字母“E.B.”，沾有血迹。" });
            AllClues.Add(new Clue { Id = "safe", Name = "保险箱", Description = "一个沉重的老式保险箱，需要4位数字密码。" });

            // 走廊线索
            AllClues.Add(new Clue { Id = "photo", Name = "家族照片", Description = "相框背后用铅笔淡淡地写着“19”。" });
            AllClues.Add(new Clue { Id = "pawn_ticket", Name = "当票", Description = "一张当票，当品是钻石胸针，签名是“Edgar Blackwood”。" });
            AllClues.Add(new Clue { Id = "key", Name = "小钥匙", Description = "一把细小的黄铜钥匙，看起来能打开书桌抽屉。" });
            AllClues.Add(new Clue { Id = "calendar", Name = "旧日历", Description = "12月25日被红圈圈起，旁边潦草地写着“该死的圣诞节”。" });
        }

        public static void DiscoverClue(string clueId, string discoverer)
        {
            var clue = AllClues.FirstOrDefault(c => c.Id == clueId);
            if (clue != null && !clue.IsDiscovered)
            {
                clue.IsDiscovered = true;
                clue.DiscoveredBy = discoverer;
                OnClueDiscovered?.Invoke(clueId);
            }
        }

        public static void UnlockSafe()
        {
            var will = AllClues.FirstOrDefault(c => c.Id == "will");
            if (will == null)
            {
                will = new Clue { Id = "will", Name = "遗嘱", Description = "霍华德的遗嘱，将大部分财产留给了管家莫里斯。" };
                AllClues.Add(will);
            }
            // 分别通知双方：A 先发现，重置后再通知 B
            DiscoverClue("will", "A");
            will.IsDiscovered = false;
            DiscoverClue("will", "B");

            var letter = AllClues.FirstOrDefault(c => c.Id == "report_letter");
            if (letter == null)
            {
                letter = new Clue { Id = "report_letter", Name = "举报信", Description = "霍华德未寄出的信，揭发管家莫里斯盗窃钻石胸针。" };
                AllClues.Add(letter);
            }
            DiscoverClue("report_letter", "A");
            letter.IsDiscovered = false;
            DiscoverClue("report_letter", "B");

            OnSafeUnlocked?.Invoke();
        }
    }

}

