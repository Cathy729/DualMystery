using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DualMystery
{
    /// <summary>
    /// 游戏音乐管理器 — 使用 NAudio 播放嵌入的 MP3 资源。
    /// BGM 循环（bgm1⇄bgm2），结局序列（truth→conan_theme）。
    /// 所有播放操作通过 UI 线程 Timer 驱动，避免 DirectSound COM 线程问题。
    /// </summary>
    public static class MusicManager
    {
        // BGM 播放器
        private static DirectSoundOut _bgmPlayer;
        private static WaveStream _bgmReader;
        private static MemoryStream _bgmStream;
        private static bool _bgmStopped = true;
        private static bool _playBgm1; // true = bgm1, false = bgm2
        private static bool _bgmNaturalEnd = true;

        // 结局音乐播放器（独立于 BGM）
        private static WaveOutEvent _endingPlayer;
        private static WaveStream _endingReader;
        private static MemoryStream _endingStream;

        // 结局音乐过渡控制
        private static Timer _endingPollTimer;     // 轮询播放状态，可靠检测播放结束
        private static bool _endingLocked;          // 重入锁，防止重复触发过渡
        private static string _endingPhase = "";    // 当前阶段: "truth" | "conan_theme" | ""

        // MediaFoundationReader 产生的临时文件（需在清理时删除）
        private static readonly System.Collections.Generic.List<string> _tempFiles =
            new System.Collections.Generic.List<string>();

        // UI 线程调度 Timer — 仅用于 BGM 循环切换（BGM 仍使用 DirectSoundOut）
        private static Timer _dispatchTimer;
        private static bool _pendingBgmNext;

        // 日志路径
        private static readonly string LogPath =
            Path.Combine(Path.GetTempPath(), "DualMystery_Music.log");

        private static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogPath,
                    $"{DateTime.Now:HH:mm:ss.fff} [Music] {msg}\n");
            }
            catch { }
        }

        // ==================== WaveStream 工厂 ====================

        /// <summary>
        /// 创建音频读取器：优先使用 MediaFoundationReader（Win10+ 自带，
        /// 自动处理声道/采样率变化），失败时回退到 Mp3FileReader。
        /// MediaFoundationReader 仅接受文件路径，故先将 Stream 写入临时 .mp3 文件。
        /// </summary>
        private static WaveStream _CreateWaveReader(Stream stream, string trackName)
        {
            try
            {
                // 写入临时文件（MediaFoundationReader 只接受文件路径）
                stream.Position = 0;
                string tempFile = Path.Combine(Path.GetTempPath(),
                    $"DualMystery_{trackName}_{Guid.NewGuid():N}.mp3");
                using (var fs = File.Create(tempFile))
                {
                    stream.CopyTo(fs);
                }
                _tempFiles.Add(tempFile);
                Console.WriteLine($"[MusicManager] Temp file written: {tempFile} ({new FileInfo(tempFile).Length} bytes)");
                Log($"Temp file for {trackName}: {tempFile}");

                var mfReader = new MediaFoundationReader(tempFile);
                Console.WriteLine($"[MusicManager] MediaFoundationReader created for {trackName}");
                Log($"MediaFoundationReader created for {trackName}");
                return mfReader;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicManager] MediaFoundationReader failed: {ex.Message}, fallback to Mp3FileReader");
                Log($"MediaFoundationReader failed for {trackName}: {ex.Message}, fallback to Mp3FileReader");
                try
                {
                    stream.Position = 0; // 重置流位置
                    return new Mp3FileReader(stream);
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"[MusicManager] Mp3FileReader also failed: {ex2.Message}");
                    Log($"Mp3FileReader also failed for {trackName}: {ex2.Message}");
                    throw;
                }
            }
        }

        /// <summary>删除所有临时文件</summary>
        private static void _DeleteTempFiles()
        {
            foreach (var f in _tempFiles)
            {
                try
                {
                    if (File.Exists(f)) File.Delete(f);
                    Console.WriteLine($"[MusicManager] Temp file deleted: {f}");
                }
                catch (Exception ex)
                {
                    Log($"Failed to delete temp file {f}: {ex.Message}");
                }
            }
            _tempFiles.Clear();
        }

        // ==================== 初始化 ====================

        /// <summary>确保 UI 调度 Timer 已启动（首次 StartBgm 或 PlayEndingMusic 时创建）</summary>
        private static void EnsureDispatchTimer()
        {
            if (_dispatchTimer != null) return;

            Log("Creating dispatch timer on UI thread...");
            _dispatchTimer = new Timer { Interval = 200 };
            _dispatchTimer.Tick += (s, e) =>
            {
                // 处理待处理的 BGM 下一曲请求
                if (_pendingBgmNext)
                {
                    _pendingBgmNext = false;
                    Log("Dispatch: executing pending _PlayNextBgm on UI thread");
                    _PlayNextBgm();
                }
                // 注：conan_theme 切换不再通过 dispatch timer，
                // 而是由 WaveOutEvent.PlaybackStopped 在 UI 线程直接调用
            };
            _dispatchTimer.Start();
            Log("Dispatch timer started.");
        }

        // ==================== BGM 循环 ====================

        /// <summary>开始背景音乐循环（随机选择 bgm1 或 bgm2）。若已在播放则忽略。</summary>
        public static void StartBgm()
        {
            Log($"StartBgm() player={_bgmPlayer != null} stopped={_bgmStopped}");

            // 确保 UI 调度器就绪（Form_Load 在 UI 线程调用，Timer 将在此线程触发）
            EnsureDispatchTimer();

            if (_bgmPlayer != null && !_bgmStopped)
            {
                Log("BGM already playing, skip.");
                return;
            }

            _bgmStopped = false;
            _playBgm1 = new Random().Next(2) == 0;
            Log($"Starting first track: bgm{(_playBgm1 ? "1" : "2")}");
            _PlayNextBgm();
        }

        /// <summary>停止背景音乐并释放资源</summary>
        public static void StopBgm()
        {
            Log("StopBgm()");
            _bgmStopped = true;
            _bgmNaturalEnd = false;
            _pendingBgmNext = false; // 取消待处理的切换
            _StopBgmInternal();
        }

        private static void _StopBgmInternal()
        {
            try
            {
                if (_bgmPlayer != null)
                {
                    _bgmPlayer.PlaybackStopped -= _OnBgmPlaybackStopped;
                    _bgmPlayer.Stop();
                    _bgmPlayer.Dispose();
                    _bgmPlayer = null;
                    Log("BGM player disposed.");
                }
            }
            catch (Exception ex) { Log($"Error disposing BGM player: {ex.Message}"); }

            try { _bgmReader?.Dispose(); } catch (Exception ex) { Log($"Error disposing reader: {ex.Message}"); }
            _bgmReader = null;
            try { _bgmStream?.Dispose(); } catch (Exception ex) { Log($"Error disposing stream: {ex.Message}"); }
            _bgmStream = null;
            _DeleteTempFiles();
        }

        private static void _PlayNextBgm()
        {
            if (_bgmStopped)
            {
                Log("_PlayNextBgm: stopped=true, abort");
                return;
            }

            // 先清理上一首的播放器
            _StopBgmInternal();
            if (_bgmStopped) return;

            byte[] mp3Data = _playBgm1 ? Properties.Resources.bgm1 : Properties.Resources.bgm2;
            string trackName = _playBgm1 ? "bgm1" : "bgm2";
            _playBgm1 = !_playBgm1; // 切换下一曲

            Log($"_PlayNextBgm: {trackName} = {mp3Data?.Length ?? 0} bytes");

            if (mp3Data == null || mp3Data.Length == 0)
            {
                Log($"ERROR: {trackName} resource is null or empty!");
                return;
            }

            try
            {
                _bgmStream = new MemoryStream(mp3Data);
                Log($"Stream created, len={_bgmStream.Length}");
                _bgmReader = _CreateWaveReader(_bgmStream, trackName);
                _bgmPlayer = new DirectSoundOut();
                Log("DirectSoundOut created");
                _bgmNaturalEnd = true;
                _bgmPlayer.PlaybackStopped += _OnBgmPlaybackStopped;
                _bgmPlayer.Init(_bgmReader);
                _bgmPlayer.Play();
                Log($"BGM Play() called, state={_bgmPlayer.PlaybackState}");
            }
            catch (Exception ex)
            {
                Log($"ERROR in _PlayNextBgm: {ex.GetType().Name}: {ex.Message}");
                _bgmStopped = true; // 播放失败时标记为已停止，保持状态一致性
                _StopBgmInternal();
            }
        }

        /// <summary>
        /// BGM 自然播放完毕回调。注意：此回调在 DirectSound 内部线程触发。
        /// 不直接创建新播放器，而是设置标志由 UI 线程 Timer 安全创建。
        /// </summary>
        private static void _OnBgmPlaybackStopped(object sender, StoppedEventArgs e)
        {
            Log($"_OnBgmPlaybackStopped: naturalEnd={_bgmNaturalEnd} stopped={_bgmStopped}");
            if (!_bgmNaturalEnd || _bgmStopped) return;

            // 设置标志，UI 线程 Timer 将拾取并创建下一曲播放器
            _pendingBgmNext = true;
            Log("Flagged _pendingBgmNext for dispatch timer");
        }

        // ==================== 结局音乐序列 ====================

        /// <summary>播放结局音乐：先停 BGM，再 truth → conan_theme</summary>
        public static void PlayEndingMusic()
        {
            Console.WriteLine("[MusicManager] === PlayEndingMusic() called ===");
            Log("=== PlayEndingMusic() called ===");

            if (_endingLocked)
            {
                Console.WriteLine("[MusicManager] WARNING: PlayEndingMusic re-entry blocked!");
                Log("WARNING: PlayEndingMusic re-entry blocked!");
                return;
            }
            _endingLocked = true;

            EnsureDispatchTimer();
            StopBgm();
            _endingPhase = "truth";
            _PlayTruth();
        }

        // ==================== 播放 truth ====================

        private static void _PlayTruth()
        {
            byte[] data = Properties.Resources.truth;
            Console.WriteLine($"[MusicManager] _PlayTruth: resource = {data?.Length ?? 0} bytes");
            Log($"_PlayTruth: resource = {data?.Length ?? 0} bytes");

            if (data == null || data.Length == 0)
            {
                Console.WriteLine("[MusicManager] ERROR: truth resource is null or empty!");
                Log("ERROR: truth resource is null or empty!");
                _endingLocked = false;
                return;
            }

            try
            {
                // 先清理可能残留的旧播放器
                _CleanupEndingResources();

                _endingStream = new MemoryStream(data);
                _endingReader = _CreateWaveReader(_endingStream, "truth");
                _endingPlayer = new WaveOutEvent();
                Console.WriteLine("[MusicManager] truth: WaveOutEvent created, subscribing PlaybackStopped");
                Log("truth: WaveOutEvent created");

                // 方案A: PlaybackStopped 事件（主要路径，在 UI 线程触发）
                _endingPlayer.PlaybackStopped += _OnEndingPlaybackStopped;

                _endingPlayer.Init(_endingReader);
                _endingPlayer.Play();
                Console.WriteLine($"[MusicManager] truth: Play() returned, state={_endingPlayer.PlaybackState}");
                Log($"truth: Play() returned, state={_endingPlayer.PlaybackState}");

                // 方案B: 启动轮询 Timer 作为双保险（250ms 间隔检查 PlaybackState）
                _StartEndingPollTimer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicManager] ERROR in _PlayTruth: {ex.GetType().Name}: {ex.Message}");
                Log($"ERROR in _PlayTruth: {ex.GetType().Name}: {ex.Message}");
                _endingLocked = false;
            }
        }

        // ==================== 播放 conan_theme ====================

        private static void _PlayConanTheme()
        {
            byte[] data = Properties.Resources.conan_theme;
            Console.WriteLine($"[MusicManager] _PlayConanTheme: resource = {data?.Length ?? 0} bytes");
            Log($"_PlayConanTheme: resource = {data?.Length ?? 0} bytes");

            if (data == null || data.Length == 0)
            {
                Console.WriteLine("[MusicManager] ERROR: conan_theme resource is null or empty!");
                Log("ERROR: conan_theme resource is null or empty!");
                _endingLocked = false;
                return;
            }

            try
            {
                _endingPhase = "conan_theme";

                // 使用全新的流、读取器、播放器实例
                _endingStream = new MemoryStream(data);
                _endingReader = _CreateWaveReader(_endingStream, "conan_theme");
                _endingPlayer = new WaveOutEvent();
                Console.WriteLine("[MusicManager] conan_theme: WaveOutEvent created, subscribing PlaybackStopped");
                Log("conan_theme: WaveOutEvent created");

                _endingPlayer.PlaybackStopped += _OnEndingPlaybackStopped;

                _endingPlayer.Init(_endingReader);
                _endingPlayer.Play();
                Console.WriteLine($"[MusicManager] conan_theme: Play() returned, state={_endingPlayer.PlaybackState}");
                Log($"conan_theme: Play() returned, state={_endingPlayer.PlaybackState}");

                // 启动轮询（检测 conan_theme 播放完毕后的清理）
                _StartEndingPollTimer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicManager] ERROR in _PlayConanTheme: {ex.GetType().Name}: {ex.Message}");
                Log($"ERROR in _PlayConanTheme: {ex.GetType().Name}: {ex.Message}");
                _endingLocked = false;
            }
        }

        // ==================== 播放结束回调（PlaybackStopped 事件） ====================

        /// <summary>
        /// 结局音乐 PlaybackStopped 事件处理器（WaveOutEvent 在 UI 线程触发）。
        /// 根据当前阶段决定是否切换到下一曲。
        /// </summary>
        private static void _OnEndingPlaybackStopped(object sender, StoppedEventArgs e)
        {
            Console.WriteLine($"[MusicManager] _OnEndingPlaybackStopped: phase={_endingPhase}");
            Log($"_OnEndingPlaybackStopped: phase={_endingPhase}");

            // 停止轮询（事件已触发，不需要 Timer 再检测）
            _StopEndingPollTimer();

            if (_endingPhase == "truth")
            {
                Console.WriteLine("[MusicManager] truth ended (event) → switching to conan_theme");
                Log("truth ended (event) → switching to conan_theme");

                // 清理 truth 播放器
                _CleanupEndingResources();

                // 播放下一曲
                _PlayConanTheme();
            }
            else if (_endingPhase == "conan_theme")
            {
                Console.WriteLine("[MusicManager] conan_theme ended (event) → final cleanup");
                Log("conan_theme ended (event) → final cleanup");
                _CleanupEndingResources();
                _endingLocked = false;
                _endingPhase = "";
            }
        }

        // ==================== 轮询 Timer（双保险） ====================

        /// <summary>
        /// 启动播放状态轮询 Timer，作为 PlaybackStopped 事件的双保险。
        /// 如果事件未触发（NAudio 兼容性问题等），Timer 会检测到 PlaybackState.Stopped 并接管过渡。
        /// </summary>
        private static void _StartEndingPollTimer()
        {
            _StopEndingPollTimer(); // 先停旧的

            _endingPollTimer = new Timer { Interval = 250 };
            _endingPollTimer.Tick += (s, e) =>
            {
                if (_endingPlayer == null)
                {
                    _StopEndingPollTimer();
                    return;
                }

                try
                {
                    var state = _endingPlayer.PlaybackState;
                    if (state == PlaybackState.Stopped)
                    {
                        Console.WriteLine($"[MusicManager] Poll detected Stopped: phase={_endingPhase}");
                        Log($"Poll detected Stopped: phase={_endingPhase}");

                        _StopEndingPollTimer();

                        if (_endingPhase == "truth")
                        {
                            Console.WriteLine("[MusicManager] truth ended (poll) → switching to conan_theme");
                            Log("truth ended (poll) → switching to conan_theme");
                            _CleanupEndingResources();
                            _PlayConanTheme();
                        }
                        else if (_endingPhase == "conan_theme")
                        {
                            Console.WriteLine("[MusicManager] conan_theme ended (poll) → final cleanup");
                            Log("conan_theme ended (poll) → final cleanup");
                            _CleanupEndingResources();
                            _endingLocked = false;
                            _endingPhase = "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MusicManager] Poll error: {ex.Message}");
                    Log($"Poll error: {ex.Message}");
                    _StopEndingPollTimer();
                }
            };
            _endingPollTimer.Start();
            Console.WriteLine("[MusicManager] Poll timer started (250ms)");
            Log("Poll timer started (250ms)");
        }

        private static void _StopEndingPollTimer()
        {
            if (_endingPollTimer == null) return;
            try
            {
                _endingPollTimer.Stop();
                _endingPollTimer.Dispose();
            }
            catch { }
            _endingPollTimer = null;
            Console.WriteLine("[MusicManager] Poll timer stopped");
            Log("Poll timer stopped");
        }

        // ==================== 资源清理 ====================

        /// <summary>安全释放结局音乐播放器资源</summary>
        private static void _CleanupEndingResources()
        {
            Console.WriteLine("[MusicManager] _CleanupEndingResources()");
            Log("_CleanupEndingResources()");

            try
            {
                if (_endingPlayer != null)
                {
                    // 先解绑事件（使用具名方法，可以正确解绑）
                    _endingPlayer.PlaybackStopped -= _OnEndingPlaybackStopped;
                    try { _endingPlayer.Stop(); } catch { }
                    try { _endingPlayer.Dispose(); } catch { }
                    _endingPlayer = null;
                    Console.WriteLine("[MusicManager] Ending player disposed");
                    Log("Ending player disposed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicManager] Error disposing ending player: {ex.Message}");
                Log($"Error disposing ending player: {ex.Message}");
            }

            try { _endingReader?.Dispose(); } catch (Exception ex) { Log($"Error disposing ending reader: {ex.Message}"); }
            _endingReader = null;
            try { _endingStream?.Dispose(); } catch (Exception ex) { Log($"Error disposing ending stream: {ex.Message}"); }
            _endingStream = null;
            _DeleteTempFiles();
        }

        // ==================== 资源释放 ====================

        /// <summary>释放所有音频资源（程序退出前调用）</summary>
        public static void DisposeAll()
        {
            Console.WriteLine("[MusicManager] DisposeAll()");
            Log("DisposeAll()");
            _bgmNaturalEnd = false;
            StopBgm();
            _StopEndingPollTimer();
            _CleanupEndingResources();
            _endingLocked = false;
            _endingPhase = "";
            _DeleteTempFiles(); // 兜底清理
            // 清理调度 Timer
            try { _dispatchTimer?.Stop(); } catch { }
            try { _dispatchTimer?.Dispose(); } catch { }
            _dispatchTimer = null;
        }
    }
}
