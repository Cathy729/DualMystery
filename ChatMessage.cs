using System;

namespace DualMystery
{
    public class ChatMessage
    {
        public string Sender { get; set; } // "A", "B", 或 "System"
        public string Text { get; set; }
        public DateTime Time { get; set; }
    }
}