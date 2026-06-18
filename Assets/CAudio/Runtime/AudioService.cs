using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CAudio
{
    /// <summary>音频运行时服务。</summary>
    public sealed class AudioService
    {
        private readonly AudioSourcePool sourcePool;
        private readonly List<AudioPlaybackHandle> handles = new List<AudioPlaybackHandle>();
        private readonly Dictionary<AudioChannel, float> channelVolumes = new Dictionary<AudioChannel, float>();

        private AudioDatabase database;
        private IAudioClipProvider clipProvider;
        private float masterVolume = 1f;
        private int nextHandleId = 1;

        /// <summary>创建音频服务。</summary>
        public AudioService(Transform root, AudioDatabase database, IAudioClipProvider clipProvider)
        {
            this.database = database;
            this.clipProvider = clipProvider ?? new DirectAudioClipProvider();
            sourcePool = new AudioSourcePool(root);
            sourcePool.Prewarm(8);
        }

        /// <summary>设置数据库。</summary>
        public void SetDatabase(AudioDatabase value)
        {
            database = value;
        }

        /// <summary>设置资源解析器。</summary>
        public void SetClipProvider(IAudioClipProvider provider)
        {
            clipProvider = provider ?? new DirectAudioClipProvider();
        }

        /// <summary>设置主音量。</summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }

        /// <summary>设置指定通道音量。</summary>
        public void SetChannelVolume(AudioChannel channel, float volume)
        {
            channelVolumes[channel] = Mathf.Clamp01(volume);
        }

        /// <summary>播放数据库中的音频。</summary>
        public AudioPlaybackHandle Play(string key, AudioPlayOptions options = null)
        {
            if (database == null)
            {
                return null;
            }

            if (!database.TryGetCue(key, out AudioCueData cue))
            {
                return null;
            }

            return Play(cue, options);
        }

        /// <summary>播放直连剪辑。</summary>
        public AudioPlaybackHandle Play(AudioClip clip, AudioPlayOptions options = null)
        {
            if (clip == null)
            {
                return null;
            }

            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            return CreatePlayback(clip, null, resolved, resolved.Channel ?? AudioChannel.Sfx, resolved.OutputGroup);
        }

        /// <summary>播放数据库配置。</summary>
        public AudioPlaybackHandle Play(AudioCueData cue, AudioPlayOptions options = null)
        {
            if (cue == null || !cue.HasClips())
            {
                return null;
            }

            AudioClipOption selected = cue.PickClip();
            if (selected == null)
            {
                return null;
            }

            AudioClipReference reference = selected.Clip;
            if (!clipProvider.TryResolveClip(reference, out AudioClip clip))
            {
                return null;
            }

            AudioPlayOptions resolved = MergeOptions(cue, options);
            float clipVolume = cue.GetVolumeMultiplier();
            float clipPitch = cue.GetPitchMultiplier();
            return CreatePlayback(clip, cue, resolved, resolved.Channel ?? cue.Channel, resolved.OutputGroup, clipVolume, clipPitch);
        }

        /// <summary>播放音乐。</summary>
        public AudioPlaybackHandle PlayMusic(string key, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            resolved.Channel = AudioChannel.Music;
            resolved.ReplaceSameChannel = true;
            return Play(key, resolved);
        }

        /// <summary>播放音乐剪辑。</summary>
        public AudioPlaybackHandle PlayMusic(AudioClip clip, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = options ?? new AudioPlayOptions();
            resolved.Channel = AudioChannel.Music;
            resolved.ReplaceSameChannel = true;
            return Play(clip, resolved);
        }

        /// <summary>停止所有音频。</summary>
        public void StopAll(float fadeOutSeconds = 0f)
        {
            for (int i = 0; i < handles.Count; i++)
            {
                handles[i].BeginStop(fadeOutSeconds);
            }
        }

        /// <summary>停止指定通道音频。</summary>
        public void StopChannel(AudioChannel channel, float fadeOutSeconds = 0f)
        {
            for (int i = 0; i < handles.Count; i++)
            {
                if (handles[i].Channel == channel)
                {
                    handles[i].BeginStop(fadeOutSeconds);
                }
            }
        }

        /// <summary>停止指定播放句柄。</summary>
        internal void StopHandle(AudioPlaybackHandle handle, float fadeOutSeconds)
        {
            if (handle == null || handle.HasStopped)
            {
                return;
            }

            float resolvedFadeOut = fadeOutSeconds >= 0f ? fadeOutSeconds : handle.FadeOutTime;
            handle.BeginStop(resolvedFadeOut);
        }

        /// <summary>暂停全部音频。</summary>
        public void PauseAll()
        {
            for (int i = 0; i < handles.Count; i++)
            {
                handles[i].Pause();
            }
        }

        /// <summary>恢复全部音频。</summary>
        public void ResumeAll()
        {
            for (int i = 0; i < handles.Count; i++)
            {
                handles[i].Resume();
            }
        }

        /// <summary>更新运行时播放状态。</summary>
        public void Tick(float deltaTime)
        {
            for (int i = handles.Count - 1; i >= 0; i--)
            {
                AudioPlaybackHandle handle = handles[i];
                float channelVolume = ResolveChannelVolume(handle.Channel);
                if (handle.Tick(deltaTime, masterVolume, channelVolume))
                {
                    sourcePool.Release(handle.Source);
                    handles.RemoveAt(i);
                }
            }
        }

        /// <summary>创建播放句柄。</summary>
        private AudioPlaybackHandle CreatePlayback(AudioClip clip, AudioCueData cue, AudioPlayOptions options, AudioChannel channel, AudioMixerGroup outputGroup, float clipVolume = 1f, float clipPitch = 1f)
        {
            if (clip == null)
            {
                return null;
            }

            if (options.ReplaceSameChannel == true)
            {
                StopChannel(channel, options.FadeOut ?? (cue != null ? cue.FadeOutTime : 0f));
            }

            AudioSource source = sourcePool.Rent();
            source.clip = clip;
            source.loop = options.Loop ?? (cue != null && cue.Loop);
            source.priority = options.Priority ?? (cue != null ? cue.Priority : 128);
            source.outputAudioMixerGroup = ResolveOutputGroup(channel, outputGroup ?? (cue != null ? cue.OutputGroup : null));
            source.spatialBlend = options.SpatialOverride ? options.SpatialBlend : cue != null ? cue.SpatialBlend : 0f;
            source.minDistance = options.MinDistance ?? (cue != null ? cue.MinDistance : 1f);
            source.maxDistance = options.MaxDistance ?? (cue != null ? cue.MaxDistance : 50f);
            source.transform.position = options.Position ?? Vector3.zero;
            source.pitch = options.Pitch * clipPitch;

            AudioPlaybackHandle handle = new AudioPlaybackHandle
            {
                Id = nextHandleId++,
                Service = this,
                Source = source,
                Channel = channel,
                Clip = clip,
                FollowTarget = options.FollowTarget,
                WorldPosition = options.Position ?? Vector3.zero,
                UseWorldPosition = options.Position.HasValue,
                IsLoop = source.loop,
                ReplaceSameChannel = options.ReplaceSameChannel ?? (cue != null && cue.ReplaceSameChannel),
                BaseVolume = Mathf.Max(0f, options.Volume * clipVolume),
                BasePitch = options.Pitch * clipPitch,
                FadeInTime = Mathf.Max(0f, options.FadeIn ?? (cue != null ? cue.FadeInTime : 0f)),
                FadeOutTime = Mathf.Max(0f, options.FadeOut ?? (cue != null ? cue.FadeOutTime : 0f)),
                Delay = Mathf.Max(0f, options.Delay),
                WaitingForDelay = options.Delay > 0f,
                Started = options.Delay <= 0f,
                Priority = options.Priority ?? (cue != null ? cue.Priority : 128),
                StartVolume = Mathf.Max(0f, options.Volume * clipVolume),
                StopStartVolume = Mathf.Max(0f, options.Volume * clipVolume)
            };

            if (handle.FollowTarget != null)
            {
                source.transform.position = handle.FollowTarget.position;
            }

            if (handle.Delay > 0f)
            {
                source.volume = 0f;
                source.PlayDelayed(handle.Delay);
            }
            else
            {
                source.volume = 0f;
                source.Play();
            }

            handles.Add(handle);
            return handle;
        }

        /// <summary>合并数据库配置和临时参数。</summary>
        private AudioPlayOptions MergeOptions(AudioCueData cue, AudioPlayOptions options)
        {
            AudioPlayOptions merged = options ?? new AudioPlayOptions();
            if (merged.Channel == null)
            {
                merged.Channel = cue.Channel;
            }

            if (merged.FadeIn == null)
            {
                merged.FadeIn = cue.FadeInTime;
            }

            if (merged.FadeOut == null)
            {
                merged.FadeOut = cue.FadeOutTime;
            }

            if (merged.Loop == null)
            {
                merged.Loop = cue.Loop;
            }

            if (merged.ReplaceSameChannel == null)
            {
                merged.ReplaceSameChannel = cue.ReplaceSameChannel;
            }

            if (!merged.SpatialOverride)
            {
                merged.SpatialOverride = true;
                merged.SpatialBlend = cue.SpatialBlend;
            }

            if (merged.MinDistance == null)
            {
                merged.MinDistance = cue.MinDistance;
            }

            if (merged.MaxDistance == null)
            {
                merged.MaxDistance = cue.MaxDistance;
            }

            if (merged.OutputGroup == null)
            {
                merged.OutputGroup = cue.OutputGroup;
            }

            if (merged.Priority == null)
            {
                merged.Priority = cue.Priority;
            }

            return merged;
        }

        /// <summary>读取输出混音组。</summary>
        private AudioMixerGroup ResolveOutputGroup(AudioChannel channel, AudioMixerGroup directGroup)
        {
            if (directGroup != null)
            {
                return directGroup;
            }

            AudioBusData bus = database != null ? database.GetBus(channel) : null;
            return bus != null ? bus.OutputGroup : null;
        }

        /// <summary>读取通道音量。</summary>
        private float ResolveChannelVolume(AudioChannel channel)
        {
            if (channelVolumes.TryGetValue(channel, out float value))
            {
                return Mathf.Clamp01(value);
            }

            AudioBusData bus = database != null ? database.GetBus(channel) : null;
            if (bus == null)
            {
                return 1f;
            }

            return bus.Mute ? 0f : Mathf.Clamp01(bus.Volume);
        }
    }
}
