using UnityEngine;
using UnityEngine.Audio;

namespace CAudio
{
    /// <summary>音频系统对外入口。</summary>
    public static class AudioManager
    {
        private static AudioManagerHost host;
        private static AudioService service;
        private static AudioDatabase database;
        private static IAudioClipProvider clipProvider = new DirectAudioClipProvider();
        private static bool initialized;

        /// <summary>获取是否已初始化。</summary>
        public static bool IsInitialized => initialized;

        /// <summary>初始化音频系统。</summary>
        public static void Initialize(AudioDatabase db = null)
        {
            database = db;
            EnsureHost();
            if (service == null)
            {
                service = new AudioService(host.transform, database, clipProvider);
            }
            else
            {
                service.SetDatabase(database);
                service.SetClipProvider(clipProvider);
            }

            initialized = true;
        }

        /// <summary>设置数据库。</summary>
        public static void SetDatabase(AudioDatabase db)
        {
            database = db;
            if (service != null)
            {
                service.SetDatabase(db);
            }
        }

        /// <summary>设置资源解析器。</summary>
        public static void SetClipProvider(IAudioClipProvider provider)
        {
            clipProvider = provider ?? new DirectAudioClipProvider();
            if (service != null)
            {
                service.SetClipProvider(clipProvider);
            }
        }

        /// <summary>播放数据库音频。</summary>
        public static AudioPlaybackHandle Play(string key, AudioPlayOptions options = null)
        {
            EnsureInitialized();
            return service.Play(key, options);
        }

        /// <summary>尝试播放数据库音频。</summary>
        public static AudioPlayResult TryPlay(string key, AudioPlayOptions options = null)
        {
            EnsureInitialized();
            return service.TryPlay(key, options);
        }

        /// <summary>播放直连剪辑。</summary>
        public static AudioPlaybackHandle Play(AudioClip clip, AudioPlayOptions options = null)
        {
            EnsureInitialized();
            return service.Play(clip, options);
        }

        /// <summary>尝试播放直连剪辑。</summary>
        public static AudioPlayResult TryPlay(AudioClip clip, AudioPlayOptions options = null)
        {
            EnsureInitialized();
            return service.TryPlay(clip, options);
        }

        /// <summary>在世界坐标播放数据库音频。</summary>
        public static AudioPlaybackHandle PlayAt(string key, Vector3 position, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            resolved.Position = position;
            resolved.SpatialOverride = true;
            resolved.SpatialBlend = 1f;
            return Play(key, resolved);
        }

        /// <summary>在世界坐标播放直连剪辑。</summary>
        public static AudioPlaybackHandle PlayAt(AudioClip clip, Vector3 position, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            resolved.Position = position;
            resolved.SpatialOverride = true;
            resolved.SpatialBlend = 1f;
            return Play(clip, resolved);
        }

        /// <summary>跟随目标播放数据库音频。</summary>
        public static AudioPlaybackHandle PlayFollow(string key, Transform target, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            resolved.FollowTarget = target;
            resolved.SpatialOverride = true;
            resolved.SpatialBlend = 1f;
            return Play(key, resolved);
        }

        /// <summary>跟随目标播放直连剪辑。</summary>
        public static AudioPlaybackHandle PlayFollow(AudioClip clip, Transform target, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            resolved.FollowTarget = target;
            resolved.SpatialOverride = true;
            resolved.SpatialBlend = 1f;
            return Play(clip, resolved);
        }

        /// <summary>播放数据库配置。</summary>
        public static AudioPlaybackHandle Play(AudioCueData cue, AudioPlayOptions options = null)
        {
            EnsureInitialized();
            return service.Play(cue, options);
        }

        /// <summary>尝试播放数据库配置。</summary>
        public static AudioPlayResult TryPlay(AudioCueData cue, AudioPlayOptions options = null)
        {
            EnsureInitialized();
            return service.TryPlay(cue, options);
        }

        /// <summary>播放音效。</summary>
        public static AudioPlaybackHandle PlaySfx(string key, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            resolved.Channel = AudioChannel.Sfx;
            return Play(key, resolved);
        }

        /// <summary>播放界面音效。</summary>
        public static AudioPlaybackHandle PlayUi(string key, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            resolved.Channel = AudioChannel.Ui;
            return Play(key, resolved);
        }

        /// <summary>播放语音。</summary>
        public static AudioPlaybackHandle PlayVoice(string key, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            resolved.Channel = AudioChannel.Voice;
            return Play(key, resolved);
        }

        /// <summary>播放环境音。</summary>
        public static AudioPlaybackHandle PlayAmbience(string key, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            resolved.Channel = AudioChannel.Ambience;
            return Play(key, resolved);
        }

        /// <summary>播放音乐。</summary>
        public static AudioPlaybackHandle PlayMusic(string key, AudioPlayOptions options = null)
        {
            EnsureInitialized();
            return service.PlayMusic(key, options);
        }

        /// <summary>播放音乐剪辑。</summary>
        public static AudioPlaybackHandle PlayMusic(AudioClip clip, AudioPlayOptions options = null)
        {
            EnsureInitialized();
            return service.PlayMusic(clip, options);
        }

        /// <summary>停止全部音频。</summary>
        public static void StopAll(float fadeOutSeconds = 0f)
        {
            if (!initialized)
            {
                return;
            }

            service.StopAll(fadeOutSeconds);
        }

        /// <summary>停止指定Key音频。</summary>
        public static void Stop(string key, float fadeOutSeconds = 0f)
        {
            if (!initialized)
            {
                return;
            }

            service.Stop(key, fadeOutSeconds);
        }

        /// <summary>停止指定播放句柄。</summary>
        public static void Stop(AudioPlaybackHandle handle, float fadeOutSeconds = -1f)
        {
            if (handle == null)
            {
                return;
            }

            handle.Stop(fadeOutSeconds);
        }

        /// <summary>停止指定通道音频。</summary>
        public static void StopChannel(AudioChannel channel, float fadeOutSeconds = 0f)
        {
            if (!initialized)
            {
                return;
            }

            service.StopChannel(channel, fadeOutSeconds);
        }

        /// <summary>暂停全部音频。</summary>
        public static void PauseAll()
        {
            if (!initialized)
            {
                return;
            }

            service.PauseAll();
        }

        /// <summary>恢复全部音频。</summary>
        public static void ResumeAll()
        {
            if (!initialized)
            {
                return;
            }

            service.ResumeAll();
        }

        /// <summary>设置主音量。</summary>
        public static void SetMasterVolume(float volume)
        {
            EnsureInitialized();
            service.SetMasterVolume(volume);
        }

        /// <summary>设置通道音量。</summary>
        public static void SetChannelVolume(AudioChannel channel, float volume)
        {
            EnsureInitialized();
            service.SetChannelVolume(channel, volume);
        }

        /// <summary>尝试获取音频配置。</summary>
        public static bool TryGetCue(string key, out AudioCueData cue)
        {
            EnsureInitialized();
            if (database == null)
            {
                cue = null;
                return false;
            }

            return database.TryGetCue(key, out cue);
        }

        /// <summary>每帧驱动音频服务。</summary>
        internal static void Tick(float deltaTime)
        {
            if (!initialized)
            {
                return;
            }

            service.Tick(deltaTime);
        }

        /// <summary>确保系统已初始化。</summary>
        private static void EnsureInitialized()
        {
            if (!initialized)
            {
                Initialize(database);
            }
        }

        /// <summary>确保宿主对象存在。</summary>
        private static void EnsureHost()
        {
            if (host != null)
            {
                return;
            }

            GameObject go = new GameObject("[CAudio] AudioManager");
            Object.DontDestroyOnLoad(go);
            host = go.AddComponent<AudioManagerHost>();
        }

        /// <summary>音频宿主组件。</summary>
        private sealed class AudioManagerHost : MonoBehaviour
        {
            /// <summary>初始化宿主。</summary>
            private void Awake()
            {
                Object.DontDestroyOnLoad(gameObject);
            }

            /// <summary>驱动音频服务。</summary>
            private void Update()
            {
                AudioManager.Tick(Time.deltaTime);
            }
        }
    }
}
