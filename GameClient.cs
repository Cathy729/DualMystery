using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace DualMystery
{
    /// <summary>
    /// TCP 客户端 — 连接 GameServer，发送动作，接收事件
    /// 每个玩家窗体持有一个实例
    /// </summary>
    public class GameClient : IDisposable
    {
        private TcpClient tcpClient;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread readThread;
        private volatile bool connected;
        private readonly object writeLock = new object();
        private readonly JavaScriptSerializer json = new JavaScriptSerializer();

        /// <summary>服务器分配的玩家身份 (A/B)</summary>
        public string PlayerId { get; private set; }

        /// <summary>是否已连接并完成握手</summary>
        public bool Connected => connected;

        // ==================== 事件 ====================
        public event Action<string> OnWelcome;                    // playerId
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string, string> OnClueDiscovered;    // clueId, player
        public event Action<string, string> OnCallRequest;       // caller, callee
        public event Action OnCallEstablished;
        public event Action OnCallEnded;
        public event Action<string> OnRingTimeout;                // caller
        public event Action<string, string> OnChatMessageReceived; // sender, text
        public event Action OnSafeUnlocked;
        public event Action<string, string, bool> OnAccusationResult; // accA, accB, bothCorrect
        public event Action<string, string, string, string> OnClueShared; // clueId, fromPlayer, toPlayer, clueName
        public event Action<string> OnError;                      // errorMessage
        public event Action<string> OnPlayerDisconnected;          // playerId

        /// <summary>客户端线索缓存（从 StateSync 和 ClueDiscovered 更新）</summary>
        public List<ClueCacheEntry> ClueCache { get; private set; } = new List<ClueCacheEntry>();

        public class ClueCacheEntry
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public bool IsDiscovered { get; set; }
            public string DiscoveredBy { get; set; }
            public string SharedTo { get; set; }
        }

        // ==================== 连接 ====================
        public void Connect(string host, int port, string requestPlayer)
        {
            if (connected) return;
            try
            {
                tcpClient = new TcpClient();
                tcpClient.NoDelay = true;
                tcpClient.Connect(host, port);
                stream = tcpClient.GetStream();
                reader = new StreamReader(stream, Encoding.UTF8);
                writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\n" };

                connected = true;

                readThread = new Thread(ReadLoop) { IsBackground = true, Name = "GameClient" };
                readThread.Start();

                // 发送注册消息——请求指定玩家身份
                Send("Register", new Dictionary<string, string> { { "RequestPlayer", requestPlayer } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Client] Connection failed: " + ex.Message);
                OnError?.Invoke("无法连接到服务器: " + ex.Message);
            }
        }

        public void Disconnect()
        {
            connected = false;
            Dispose();
            OnDisconnected?.Invoke();
        }

        // ==================== 读取循环 ====================
        private void ReadLoop()
        {
            try
            {
                string line;
                while (connected && (line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    ProcessMessage(line);
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally { if (connected) { connected = false; OnDisconnected?.Invoke(); } }
        }

        private void ProcessMessage(string line)
        {
            try
            {
                var dict = json.Deserialize<Dictionary<string, object>>(line);
                if (dict == null || !dict.ContainsKey("Type")) return;
                string type = dict["Type"] as string ?? "";
                var rawData = dict.ContainsKey("Data") ? dict["Data"] as Dictionary<string, object> : null;
                var data = rawData?.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "") ?? new Dictionary<string, string>();

                switch (type)
                {
                    case "Welcome":
                        PlayerId = data.Get("PlayerId");
                        System.Diagnostics.Debug.WriteLine($"[Client] Assigned player: {PlayerId}");
                        OnWelcome?.Invoke(PlayerId);
                        break;

                    case "StateSync":
                        HandleStateSync(rawData);
                        break;

                    case "ClueDiscovered":
                        {
                            string cId = data.Get("ClueId");
                            string cPlayer = data.Get("Player");
                            OnClueDiscovered?.Invoke(cId, cPlayer);
                            // 更新本地缓存
                            var ce = ClueCache.FirstOrDefault(c => c.Id == cId);
                            if (ce != null)
                            {
                                ce.IsDiscovered = true;
                                ce.DiscoveredBy = cPlayer;
                            }
                            else
                            {
                                // 动态添加的线索（如遗嘱、举报信）不在初始 StateSync 中，从 GameManager 补登
                                var gmClue = GameManager.AllClues.FirstOrDefault(gc => gc.Id == cId);
                                if (gmClue != null)
                                {
                                    ClueCache.Add(new ClueCacheEntry
                                    {
                                        Id = gmClue.Id,
                                        Name = gmClue.Name,
                                        Description = gmClue.Description,
                                        IsDiscovered = true,
                                        DiscoveredBy = cPlayer
                                    });
                                }
                            }
                        }
                        break;

                    case "CallRequest":
                        OnCallRequest?.Invoke(data.Get("Caller"), data.Get("Callee"));
                        break;

                    case "CallEstablished":
                        OnCallEstablished?.Invoke();
                        break;

                    case "CallEnded":
                        OnCallEnded?.Invoke();
                        break;

                    case "RingTimeout":
                        OnRingTimeout?.Invoke(data.Get("Caller"));
                        break;

                    case "ChatMessage":
                        OnChatMessageReceived?.Invoke(data.Get("Sender"), data.Get("Text"));
                        break;

                    case "SafeUnlocked":
                        OnSafeUnlocked?.Invoke();
                        break;

                    case "AccusationResult":
                        bool bothCorrect = data.Get("BothCorrect") == "True";
                        OnAccusationResult?.Invoke(data.Get("AccusationA"), data.Get("AccusationB"), bothCorrect);
                        break;

                    case "ClueShared":
                        {
                            string sharedId = data.Get("ClueId");
                            string fromP = data.Get("FromPlayer");
                            string toP = data.Get("ToPlayer");
                            string sharedName = data.Get("ClueName");
                            string sharedDesc = data.Get("ClueDescription");
                            // 更新本地缓存
                            var entry = ClueCache.FirstOrDefault(c => c.Id == sharedId);
                            if (entry != null)
                            {
                                entry.SharedTo = toP;
                            }
                            else
                            {
                                // 缓存缺失时从 GameManager 补登
                                var gmClue = GameManager.AllClues.FirstOrDefault(gc => gc.Id == sharedId);
                                if (gmClue != null)
                                {
                                    ClueCache.Add(new ClueCacheEntry
                                    {
                                        Id = gmClue.Id,
                                        Name = sharedName,
                                        Description = gmClue.Description,
                                        IsDiscovered = true,
                                        DiscoveredBy = fromP,
                                        SharedTo = toP
                                    });
                                }
                            }
                            OnClueShared?.Invoke(sharedId, fromP, toP, sharedName);
                        }
                        break;

                    case "Error":
                        OnError?.Invoke(data.Get("Message"));
                        break;

                    case "PlayerDisconnected":
                        OnPlayerDisconnected?.Invoke(data.Get("PlayerId"));
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Client] Parse error: " + ex.Message);
            }
        }

        private void HandleStateSync(Dictionary<string, object> rawData)
        {
            if (rawData == null) return;
            if (rawData.TryGetValue("Clues", out var cluesObj) && cluesObj is object[] clueArray)
            {
                ClueCache.Clear();
                foreach (var item in clueArray)
                {
                    if (item is Dictionary<string, object> c)
                    {
                        ClueCache.Add(new ClueCacheEntry
                        {
                            Id = c.GetOrDefault("Id", "") as string ?? "",
                            Name = c.GetOrDefault("Name", "") as string ?? "",
                            Description = c.GetOrDefault("Description", "") as string ?? "",
                            IsDiscovered = (c.GetOrDefault("IsDiscovered", false) as bool?) ?? false,
                            DiscoveredBy = c.GetOrDefault("DiscoveredBy", "") as string ?? "",
                            SharedTo = c.GetOrDefault("SharedTo", "") as string ?? ""
                        });
                    }
                }
            }
        }

        // ==================== 发送动作 ====================
        private void Send(string type, Dictionary<string, string> data)
        {
            if (!connected) return;
            string msg = json.Serialize(new { Type = type, Data = data });
            lock (writeLock)
            {
                try { writer.WriteLine(msg); }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
            }
        }

        public void DiscoverClue(string clueId, string player)
            => Send("DiscoverClue", new Dictionary<string, string> { { "ClueId", clueId }, { "Player", player } });

        public void RequestCall(string caller)
            => Send("RequestCall", new Dictionary<string, string> { { "Caller", caller } });

        public void AcceptCall(string player)
            => Send("AcceptCall", new Dictionary<string, string> { { "Player", player } });

        public void DeclineCall(string player)
            => Send("DeclineCall", new Dictionary<string, string> { { "Player", player } });

        public void HangUp(string player)
            => Send("HangUp", new Dictionary<string, string> { { "Player", player } });

        public void SendChatMessage(string sender, string text)
            => Send("SendChatMessage", new Dictionary<string, string> { { "Sender", sender }, { "Text", text } });

        public void SubmitAccusation(string player, string suspect)
            => Send("SubmitAccusation", new Dictionary<string, string> { { "Player", player }, { "Suspect", suspect } });

        public void ShareClue(string clueId)
            => Send("ShareClue", new Dictionary<string, string> { { "ClueId", clueId } });

        public void UnlockSafe()
            => Send("UnlockSafe", new Dictionary<string, string>());

        // ==================== 查询本地缓存 ====================
        public bool IsClueDiscovered(string clueId)
        {
            var c = ClueCache.FirstOrDefault(ce => ce.Id == clueId);
            return c?.IsDiscovered ?? false;
        }

        public string GetClueDiscoveredBy(string clueId)
        {
            var c = ClueCache.FirstOrDefault(ce => ce.Id == clueId);
            return c?.DiscoveredBy ?? "";
        }

        public List<ClueCacheEntry> GetMyClues(string playerId)
        {
            // 自己的线索：自己发现的 + 对方分享给我的
            var result = ClueCache.FindAll(c =>
                c.IsDiscovered && (c.DiscoveredBy == playerId || c.SharedTo == playerId));

            // 补充 GameManager 中已发现但缓存未同步的线索（动态线索 + 时序兜底）
            var cachedIds = new HashSet<string>(ClueCache.Select(c => c.Id));
            foreach (var gmClue in GameManager.AllClues)
            {
                if (gmClue.IsDiscovered && (gmClue.DiscoveredBy == playerId || gmClue.SharedTo == playerId))
                {
                    if (!cachedIds.Contains(gmClue.Id))
                    {
                        // 完全缺失的条目（如动态添加的遗嘱、举报信）
                        result.Add(new ClueCacheEntry
                        {
                            Id = gmClue.Id,
                            Name = gmClue.Name,
                            Description = gmClue.Description,
                            IsDiscovered = true,
                            DiscoveredBy = playerId
                        });
                    }
                    else
                    {
                        // 条目存在但缓存中 IsDiscovered 尚未更新（TCP 广播时序滞后）
                        var existing = ClueCache.First(c => c.Id == gmClue.Id);
                        if (!existing.IsDiscovered)
                        {
                            existing.IsDiscovered = true;
                            existing.DiscoveredBy = playerId;
                            result.Add(existing);
                        }
                    }
                }
            }
            return result;
        }

        public List<ClueCacheEntry> GetAllDiscoveredClues()
        {
            return ClueCache.FindAll(c => c.IsDiscovered);
        }

        // ==================== 生命周期 ====================
        public void Dispose()
        {
            connected = false;
            try { writer?.Dispose(); } catch { }
            try { reader?.Dispose(); } catch { }
            try { stream?.Dispose(); } catch { }
            try { tcpClient?.Close(); } catch { }
            try { readThread?.Abort(); } catch { }
        }
    }

    internal static class StringDictExtensions
    {
        public static string Get(this Dictionary<string, string> dict, string key)
        {
            return dict.TryGetValue(key, out var val) ? val : "";
        }
    }
}
