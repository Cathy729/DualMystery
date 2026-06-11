using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace DualMystery
{
    /// <summary>
    /// TCP 游戏服务器 — 管理所有游戏状态，在 Client 之间路由消息
    /// 监听端口 8888，接受最多 2 个客户端（A / B）
    /// </summary>
    public class GameServer : IDisposable
    {
        private TcpListener listener;
        private Thread acceptThread;
        private volatile bool running;
        private readonly object clientsLock = new object();
        private readonly List<ClientConnection> clients = new List<ClientConnection>();
        private readonly JavaScriptSerializer json = new JavaScriptSerializer();
        private readonly AutoResetEvent clientConnected = new AutoResetEvent(false);

        public const int PORT = 8888;
        public static GameServer Instance { get; private set; }

        /// <summary>两个客户端都连接后触发</summary>
        public event Action OnBothConnected;

        public GameServer()
        {
            Instance = this;
        }

        public void Start()
        {
            if (running) return;
            running = true;
            listener = new TcpListener(IPAddress.Loopback, PORT);
            listener.Start();
            acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "GameServer" };
            acceptThread.Start();

            // 订阅静态事件——自动广播给所有客户端
            GameManager.OnClueDiscovered += OnClueDiscovered_Proxy;
            GameManager.OnSafeUnlocked += OnSafeUnlocked_Proxy;

            System.Diagnostics.Debug.WriteLine("[Server] Started on port " + PORT);
        }

        public void Stop()
        {
            running = false;
            GameManager.OnClueDiscovered -= OnClueDiscovered_Proxy;
            GameManager.OnSafeUnlocked -= OnSafeUnlocked_Proxy;
            listener?.Stop();
            lock (clientsLock)
            {
                foreach (var c in clients) c.Dispose();
                clients.Clear();
            }
        }

        // 事件代理——将静态事件转为广播
        private void OnClueDiscovered_Proxy(string clueId)
        {
            var clue = GameManager.AllClues.FirstOrDefault(c => c.Id == clueId);
            if (clue != null)
                Broadcast("ClueDiscovered", new Dictionary<string, string> { { "ClueId", clueId }, { "Player", clue.DiscoveredBy ?? "" } });
        }

        private void OnSafeUnlocked_Proxy()
        {
            Broadcast("SafeUnlocked", new Dictionary<string, string>());
        }

        public void Dispose() => Stop();

        // ==================== Accept Loop ====================
        private void AcceptLoop()
        {
            while (running)
            {
                try
                {
                    var tcpClient = listener.AcceptTcpClient();
                    tcpClient.NoDelay = true;
                    var conn = new ClientConnection(tcpClient, this);
                    lock (clientsLock)
                    {
                        if (clients.Count >= 2) { conn.SendError("服务器已满"); conn.Dispose(); continue; }
                        System.Diagnostics.Debug.WriteLine($"[Server] New connection waiting to register");
                    }
                    // 启动读取线程（第一条消息必须是 Register）
                    var readThread = new Thread(() => ReadLoop(conn)) { IsBackground = true };
                    readThread.Start();
                }
                catch (SocketException) { break; }
                catch (ObjectDisposedException) { break; }
            }
        }

        // ==================== Read Loop ====================
        private void ReadLoop(ClientConnection conn)
        {
            try
            {
                using (var reader = new StreamReader(conn.Stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        ProcessMessage(conn, line);
                    }
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally { OnDisconnect(conn); }
        }

        private void OnDisconnect(ClientConnection conn)
        {
            lock (clientsLock)
            {
                clients.Remove(conn);
                System.Diagnostics.Debug.WriteLine($"[Server] Player {conn.PlayerId} disconnected");
                // 通知另一方
                Broadcast("PlayerDisconnected", new Dictionary<string, string> { { "PlayerId", conn.PlayerId } });
            }
            conn.Dispose();
        }

        // ==================== Message Processing ====================
        private void ProcessMessage(ClientConnection from, string line)
        {
            try
            {
                var dict = json.Deserialize<Dictionary<string, object>>(line);
                if (dict == null || !dict.ContainsKey("Type")) return;
                string type = dict["Type"] as string ?? "";
                var data = dict.ContainsKey("Data") ? dict["Data"] as Dictionary<string, object> ?? new Dictionary<string, object>() : new Dictionary<string, object>();

                System.Diagnostics.Debug.WriteLine($"[Server] ← {from.PlayerId ?? "?"}: {type}");

                switch (type)
                {
                    case "Register":
                        HandleRegister(from, data);
                        return; // Register is always the first message, handled specially

                    case "DiscoverClue":
                        HandleDiscoverClue(from, data);
                        break;
                    case "UnlockSafe":
                        HandleUnlockSafe(from, data);
                        break;
                    case "RequestCall":
                        HandleRequestCall(from, data);
                        break;
                    case "AcceptCall":
                        HandleAcceptCall(from, data);
                        break;
                    case "DeclineCall":
                        HandleDeclineCall(from, data);
                        break;
                    case "HangUp":
                        HandleHangUp(from, data);
                        break;
                    case "SendChatMessage":
                        HandleChatMessage(from, data);
                        break;
                    case "SubmitAccusation":
                        HandleAccusation(from, data);
                        break;
                    case "ShareClue":
                        HandleShareClue(from, data);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Server] Parse error: " + ex.Message);
            }
        }

        private void HandleRegister(ClientConnection from, Dictionary<string, object> data)
        {
            string requested = data.GetOrDefault("RequestPlayer", "") as string ?? "";
            if (requested != "A" && requested != "B")
            {
                from.SendError("无效的玩家身份");
                from.Dispose();
                return;
            }
            lock (clientsLock)
            {
                // 检查是否已被占用
                foreach (var c in clients)
                {
                    if (c != from && c.PlayerId == requested)
                    {
                        from.SendError($"玩家 {requested} 已存在");
                        return;
                    }
                }
                from.PlayerId = requested;
                if (!clients.Contains(from)) clients.Add(from);
                System.Diagnostics.Debug.WriteLine($"[Server] Player {requested} registered");

                from.Send("Welcome", new Dictionary<string, string> { { "PlayerId", requested } });
                SendStateSync(from);

                if (clients.Count == 2)
                {
                    clientConnected.Set();
                    OnBothConnected?.Invoke();
                    System.Diagnostics.Debug.WriteLine("[Server] Both players connected");
                }
            }
        }

        private Dictionary<string, string> ToStrDict(Dictionary<string, object> data)
        {
            return data.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");
        }

        private void HandleDiscoverClue(ClientConnection from, Dictionary<string, object> data)
        {
            string clueId = data.GetOrDefault("ClueId", "") as string ?? "";
            string player = data.GetOrDefault("Player", "") as string ?? "";
            GameManager.DiscoverClue(clueId, player);
            // 广播由 OnClueDiscovered 事件代理自动处理
        }

        private void HandleUnlockSafe(ClientConnection from, Dictionary<string, object> data)
        {
            GameManager.UnlockSafe();
            // 广播由事件代理自动处理（SafeUnlocked + ClueDiscovered x4）
        }

        private void HandleRequestCall(ClientConnection from, Dictionary<string, object> data)
        {
            string caller = from.PlayerId;
            // 如果已在通话中则忽略
            if (PhoneManager.IsRinging) return;
            PhoneManager.RequestCall(caller);
            Broadcast("CallRequest", new Dictionary<string, string> { { "Caller", caller }, { "Callee", PhoneManager.Callee ?? "" } });
        }

        private void HandleAcceptCall(ClientConnection from, Dictionary<string, object> data)
        {
            string player = from.PlayerId;
            if (!PhoneManager.IsRinging || PhoneManager.Callee != player) return;
            PhoneManager.AcceptCall(player);
            Broadcast("CallEstablished", new Dictionary<string, string>());
        }

        private void HandleDeclineCall(ClientConnection from, Dictionary<string, object> data)
        {
            string player = from.PlayerId;
            if (!PhoneManager.IsRinging || PhoneManager.Callee != player) return;
            string caller = PhoneManager.Caller;
            PhoneManager.DeclineCall(player);
            Broadcast("CallEnded", new Dictionary<string, string>());
            Broadcast("RingTimeout", new Dictionary<string, string> { { "Caller", caller ?? "" } });
        }

        private void HandleHangUp(ClientConnection from, Dictionary<string, object> data)
        {
            PhoneManager.HangUp(from.PlayerId);
            Broadcast("CallEnded", new Dictionary<string, string>());
        }

        private void HandleChatMessage(ClientConnection from, Dictionary<string, object> data)
        {
            string sender = from.PlayerId;
            string text = data.GetOrDefault("Text", "") as string ?? "";
            // 服务器添加历史记录
            var msg = new ChatMessage { Sender = sender, Text = text, Time = DateTime.Now };
            ChatService.History.Add(msg);
            // 广播给两个客户端（包括发送方，用于可视化确认）
            Broadcast("ChatMessage", new Dictionary<string, string> { { "Sender", sender }, { "Text", text } });
        }

        private void HandleAccusation(ClientConnection from, Dictionary<string, object> data)
        {
            string suspect = data.GetOrDefault("Suspect", "") as string ?? "";
            GameManager.SubmitAccusation(from.PlayerId, suspect);

            if (GameManager.LastAccusationA != null && GameManager.LastAccusationB != null)
            {
                // 两人都已指认，发送结果
                bool bothCorrect = (GameManager.LastAccusationA == "莫里斯" && GameManager.LastAccusationB == "莫里斯");
                if (!bothCorrect)
                {
                    // 不一致时立即清空服务器端指认记录，确保双方必须重新指认
                    GameManager.ResetAccusation();
                }
                Broadcast("AccusationResult", new Dictionary<string, string>
                {
                    { "BothCorrect", bothCorrect.ToString() },
                    { "AccusationA", GameManager.LastAccusationA ?? "" },
                    { "AccusationB", GameManager.LastAccusationB ?? "" }
                });
            }
        }

        private void HandleShareClue(ClientConnection from, Dictionary<string, object> data)
        {
            string clueId = data.GetOrDefault("ClueId", "") as string ?? "";
            string targetPlayer = from.PlayerId == "A" ? "B" : "A";
            var clue = GameManager.AllClues.FirstOrDefault(c => c.Id == clueId);
            if (clue != null && !clue.IsShared)
            {
                // 标记分享，但不改变 DiscoveredBy（保留原始发现者）
                clue.SharedTo = targetPlayer;

                // 小钥匙特殊规则：通知双方更新笔记本显示名称
                string sharedClueName = clue.Name;
                if (clue.Id == "key")
                {
                    sharedClueName = "小钥匙";  // A 收到时显示的名称
                }

                Broadcast("ClueShared", new Dictionary<string, string>
                {
                    { "ClueId", clueId },
                    { "FromPlayer", from.PlayerId },
                    { "ToPlayer", targetPlayer },
                    { "ClueName", sharedClueName },
                    { "ClueDescription", clue.Description }
                });
                Broadcast("ChatMessage", new Dictionary<string, string>
                {
                    { "Sender", "System" },
                    { "Text", $"{from.PlayerId} 分享了线索：{clue.Name} —— {clue.Description}" }
                });
            }
        }

        // ==================== Broadcasting ====================
        public void Broadcast(string type, Dictionary<string, string> data)
        {
            string msg = json.Serialize(new { Type = type, Data = data });
            System.Diagnostics.Debug.WriteLine($"[Server] → ALL: {type}");
            lock (clientsLock)
            {
                foreach (var c in clients)
                    c.SendRaw(msg);
            }
        }

        public void BroadcastExcept(string type, Dictionary<string, string> data, string exceptPlayerId)
        {
            string msg = json.Serialize(new { Type = type, Data = data });
            lock (clientsLock)
            {
                foreach (var c in clients)
                    if (c.PlayerId != exceptPlayerId)
                        c.SendRaw(msg);
            }
        }

        private void SendStateSync(ClientConnection conn)
        {
            var clueStates = new List<Dictionary<string, object>>();
            foreach (var c in GameManager.AllClues)
            {
                clueStates.Add(new Dictionary<string, object>
                {
                    { "Id", c.Id },
                    { "Name", c.Name },
                    { "Description", c.Description },
                    { "IsDiscovered", c.IsDiscovered },
                    { "DiscoveredBy", c.DiscoveredBy ?? "" },
                    { "SharedTo", c.SharedTo ?? "" }
                });
            }
            var msg = json.Serialize(new
            {
                Type = "StateSync",
                Data = new Dictionary<string, object>
                {
                    { "Clues", clueStates }
                }
            });
            conn.SendRaw(msg);
        }

        // ==================== Client Connection ====================
        private class ClientConnection : IDisposable
        {
            public TcpClient TcpClient { get; }
            public NetworkStream Stream { get; }
            public string PlayerId { get; set; }
            private readonly GameServer server;
            private readonly StreamWriter writer;
            private readonly object writeLock = new object();

            public ClientConnection(TcpClient client, GameServer server)
            {
                this.TcpClient = client;
                this.Stream = client.GetStream();
                this.server = server;
                this.writer = new StreamWriter(Stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\n" };
            }

            public void Send(string type, Dictionary<string, string> data)
            {
                string msg = server.json.Serialize(new { Type = type, Data = data });
                SendRaw(msg);
            }

            public void SendRaw(string jsonMessage)
            {
                lock (writeLock)
                {
                    try { writer.WriteLine(jsonMessage); }
                    catch (IOException) { }
                    catch (ObjectDisposedException) { }
                }
            }

            public void SendError(string message)
            {
                Send("Error", new Dictionary<string, string> { { "Message", message } });
            }

            public void Dispose()
            {
                try { writer?.Dispose(); } catch { }
                try { Stream?.Dispose(); } catch { }
                try { TcpClient?.Close(); } catch { }
            }
        }
    }

    // ==================== Extension Helper ====================
    internal static class DictExtensions
    {
        public static object GetOrDefault(this Dictionary<string, object> dict, string key, object defaultValue)
        {
            return dict.TryGetValue(key, out var val) ? val : defaultValue;
        }
    }
}
