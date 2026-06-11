using System.Collections.Generic;

namespace DualMystery
{
    public static class ChatService
    {
        public static List<ChatMessage> History = new List<ChatMessage>();
        public static event System.Action<ChatMessage> OnMessageReceived;

        public static void SendMessage(string sender, string text)
        {
            var msg = new ChatMessage { Sender = sender, Text = text, Time = System.DateTime.Now };
            History.Add(msg);
            OnMessageReceived?.Invoke(msg);
        }
    }
}