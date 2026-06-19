using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CAudio
{
    /// <summary>音频运行时服务。</summary>
    public sealed class AudioService
    {
        private sealed class QueuedMusic
        {
            public string Key;
            public AudioClip Clip;
            public float FadeSeconds;
            public AudioPlayOptions Options;
        }

        private sealed class MixerParameterTransition
        {
            public string Parameter;
            public float StartValue;
            public float TargetValue;
            public float Duration;
            public float Elapsed;
        }

        private readonly AudioSourcePool sourcePool;
        private readonly List<AudioPlaybackHandle> handles = new List<AudioPlaybackHandle>();
        private readonly Queue<QueuedMusic> musicQueue = new Queue<QueuedMusic>();
        private readonly List<MixerParameterTransition> mixerTransitions = new List<MixerParameterTransition>();
        private readonly Dictionary<AudioChannel, float> channelVolumes = new Dictionary<AudioChannel, float>();
        private readonly Dictionary<string, float> lastPlayTimes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<AudioChannel> mutedChannels = new HashSet<AudioChannel>();

        private AudioDatabase database;
        private IAudioClipProvider clipProvider;
        private AudioChannel? soloChannel;
        private float masterVolume = 1f;
        private float duckMultiplier = 1f;
        private float serviceTime;
        private int nextHandleId = 1;

        /// <summary>创建音频服务。</summary>
        public AudioService(Transform root, AudioDatabase database, IAudioClipProvider clipProvider)
        {
            this.database = database;
            this.clipProvider = clipProvider ?? new DirectAudioClipProvider();
            sourcePool = new AudioSourcePool(root);
            ApplyPoolSettings();
        }

        /// <summary>设置数据库。</summary>
        public void SetDatabase(AudioDatabase value)
        {
            database = value;
            ApplyPoolSettings();
            ApplyMixerVolumes();
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
            ApplyMixerVolume(AudioChannel.Master, masterVolume);
        }

        /// <summary>设置指定通道音量。</summary>
        public void SetChannelVolume(AudioChannel channel, float volume)
        {
            channelVolumes[channel] = Mathf.Clamp01(volume);
            ApplyMixerVolume(channel, ResolveChannelVolume(channel));
        }

        /// <summary>设置通道静音。</summary>
        public void SetChannelMute(AudioChannel channel, bool mute)
        {
            if (mute)
            {
                mutedChannels.Add(channel);
            }
            else
            {
                mutedChannels.Remove(channel);
            }

            ApplyMixerVolume(channel, ResolveChannelVolume(channel));
        }

        /// <summary>设置独奏通道，传入 null 表示清除独奏。</summary>
        public void SetSoloChannel(AudioChannel? channel)
        {
            soloChannel = channel;
            ApplyMixerVolumes();
        }

        /// <summary>播放数据库中的音频。</summary>
        public AudioPlaybackHandle Play(string key, AudioPlayOptions options = null)
        {
            return TryPlay(key, options).Handle;
        }

        /// <summary>尝试播放数据库中的音频。</summary>
        public AudioPlayResult TryPlay(string key, AudioPlayOptions options = null)
        {
            if (database == null)
            {
                return Fail(AudioPlayFailureReason.MissingDatabase, "音频数据库未设置。");
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                return Fail(AudioPlayFailureReason.EmptyKey, "播放Key为空。");
            }

            if (!database.TryGetCue(key, out AudioCueData cue))
            {
                return Fail(AudioPlayFailureReason.CueNotFound, $"找不到音频Key：{key}。");
            }

            return TryPlay(cue, options);
        }

        /// <summary>异步播放数据库中的音频。</summary>
        public AudioAsyncPlayRequest PlayAsync(string key, AudioPlayOptions options = null, Action<AudioPlayResult> onComplete = null)
        {
            AudioAsyncPlayRequest request = new AudioAsyncPlayRequest(onComplete);
            if (database == null)
            {
                request.Complete(Fail(AudioPlayFailureReason.MissingDatabase, "音频数据库未设置。"));
                return request;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                request.Complete(Fail(AudioPlayFailureReason.EmptyKey, "播放Key为空。"));
                return request;
            }

            if (!database.TryGetCue(key, out AudioCueData cue))
            {
                request.Complete(Fail(AudioPlayFailureReason.CueNotFound, $"找不到音频Key：{key}。"));
                return request;
            }

            PlayAsync(cue, options, request);
            return request;
        }

        /// <summary>播放直连剪辑。</summary>
        public AudioPlaybackHandle Play(AudioClip clip, AudioPlayOptions options = null)
        {
            return TryPlay(clip, options).Handle;
        }

        /// <summary>尝试播放直连剪辑。</summary>
        public AudioPlayResult TryPlay(AudioClip clip, AudioPlayOptions options = null)
        {
            if (clip == null)
            {
                return Fail(AudioPlayFailureReason.MissingClip, "直连音频剪辑为空。");
            }

            AudioPlayOptions resolved = AudioPlayOptions.CopyOrDefault(options);
            AudioPlaybackHandle handle = CreatePlayback(clip, null, null, clipProvider as IAudioClipReleaseProvider, resolved, resolved.Channel ?? AudioChannel.Sfx, resolved.OutputGroup);
            if (handle == null)
            {
                return Fail(AudioPlayFailureReason.PoolLimitReached, "音源池已达到最大数量。");
            }

            return Succeed(handle, $"播放直连音频：{clip.name}。");
        }

        /// <summary>播放数据库配置。</summary>
        public AudioPlaybackHandle Play(AudioCueData cue, AudioPlayOptions options = null)
        {
            return TryPlay(cue, options).Handle;
        }

        /// <summary>尝试播放数据库配置。</summary>
        public AudioPlayResult TryPlay(AudioCueData cue, AudioPlayOptions options = null)
        {
            if (cue == null || !cue.HasClips())
            {
                return Fail(cue == null ? AudioPlayFailureReason.MissingCue : AudioPlayFailureReason.MissingClip, "音频配置为空或没有可用Clip。");
            }

            if (TryCreatePlaybackGateFailure(cue, out AudioPlayResult gateFailure))
            {
                return gateFailure;
            }

            AudioClipOption selected = cue.PickClip();
            if (selected == null)
            {
                return Fail(AudioPlayFailureReason.MissingClip, $"音频配置 {cue.Key} 未选出可用Clip。");
            }

            AudioClipReference reference = selected.Clip;
            if (!clipProvider.TryResolveClip(reference, out AudioClip clip))
            {
                return Fail(AudioPlayFailureReason.ProviderFailed, $"音频配置 {cue.Key} 无法解析Clip。");
            }

            AudioPlayOptions resolved = MergeOptions(cue, options);
            float clipVolume = cue.GetVolumeMultiplier();
            float clipPitch = cue.GetPitchMultiplier();
            AudioPlaybackHandle handle = CreatePlayback(clip, cue, reference, clipProvider as IAudioClipReleaseProvider, resolved, resolved.Channel ?? cue.Channel, resolved.OutputGroup, clipVolume, clipPitch);
            if (handle == null)
            {
                ReleaseClip(clipProvider as IAudioClipReleaseProvider, reference, clip);
                return Fail(AudioPlayFailureReason.PoolLimitReached, $"音频配置 {cue.Key} 无法播放，音源池已达到最大数量。");
            }

            return Succeed(handle, $"播放音频：{cue.Key}。");
        }

        /// <summary>异步播放数据库配置。</summary>
        public AudioAsyncPlayRequest PlayAsync(AudioCueData cue, AudioPlayOptions options = null, Action<AudioPlayResult> onComplete = null)
        {
            AudioAsyncPlayRequest request = new AudioAsyncPlayRequest(onComplete);
            PlayAsync(cue, options, request);
            return request;
        }

        /// <summary>播放音乐。</summary>
        public AudioPlaybackHandle PlayMusic(string key, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = AudioPlayOptions.CopyOrDefault(options);
            resolved.Channel = AudioChannel.Music;
            resolved.ReplaceSameChannel = true;
            return Play(key, resolved);
        }

        /// <summary>交叉淡入淡出播放音乐。</summary>
        public AudioPlaybackHandle CrossfadeMusic(string key, float fadeSeconds, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = AudioPlayOptions.CopyOrDefault(options);
            float fade = Mathf.Max(0f, fadeSeconds);
            resolved.Channel = AudioChannel.Music;
            resolved.ReplaceSameChannel = true;
            resolved.FadeIn = fade;
            resolved.FadeOut = fade;
            return Play(key, resolved);
        }

        /// <summary>播放音乐剪辑。</summary>
        public AudioPlaybackHandle PlayMusic(AudioClip clip, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = AudioPlayOptions.CopyOrDefault(options);
            resolved.Channel = AudioChannel.Music;
            resolved.ReplaceSameChannel = true;
            return Play(clip, resolved);
        }

        /// <summary>交叉淡入淡出播放音乐剪辑。</summary>
        public AudioPlaybackHandle CrossfadeMusic(AudioClip clip, float fadeSeconds, AudioPlayOptions options = null)
        {
            AudioPlayOptions resolved = AudioPlayOptions.CopyOrDefault(options);
            float fade = Mathf.Max(0f, fadeSeconds);
            resolved.Channel = AudioChannel.Music;
            resolved.ReplaceSameChannel = true;
            resolved.FadeIn = fade;
            resolved.FadeOut = fade;
            return Play(clip, resolved);
        }

        /// <summary>将音乐加入播放队列。</summary>
        public void QueueMusic(string key, float fadeSeconds = 0f, AudioPlayOptions options = null)
        {
            musicQueue.Enqueue(new QueuedMusic
            {
                Key = key,
                FadeSeconds = Mathf.Max(0f, fadeSeconds),
                Options = AudioPlayOptions.CopyOrDefault(options)
            });

            TryPlayNextQueuedMusic();
        }

        /// <summary>将音乐剪辑加入播放队列。</summary>
        public void QueueMusic(AudioClip clip, float fadeSeconds = 0f, AudioPlayOptions options = null)
        {
            musicQueue.Enqueue(new QueuedMusic
            {
                Clip = clip,
                FadeSeconds = Mathf.Max(0f, fadeSeconds),
                Options = AudioPlayOptions.CopyOrDefault(options)
            });

            TryPlayNextQueuedMusic();
        }

        /// <summary>清空音乐队列。</summary>
        public void ClearMusicQueue()
        {
            musicQueue.Clear();
        }

        /// <summary>过渡到混音器快照。</summary>
        public void TransitionToSnapshot(AudioMixerSnapshot snapshot, float transitionSeconds)
        {
            if (snapshot == null)
            {
                return;
            }

            snapshot.TransitionTo(Mathf.Max(0f, transitionSeconds));
        }

        /// <summary>平滑设置混音器浮点参数。</summary>
        public bool TransitionMixerParameter(string parameter, float targetValue, float duration)
        {
            AudioMixer mixer = database != null && database.MixerSettings != null ? database.MixerSettings.Mixer : null;
            if (mixer == null || string.IsNullOrWhiteSpace(parameter))
            {
                return false;
            }

            if (!mixer.GetFloat(parameter, out float startValue))
            {
                startValue = targetValue;
            }

            duration = Mathf.Max(0f, duration);
            if (duration <= 0f)
            {
                mixer.SetFloat(parameter, targetValue);
                RemoveMixerTransition(parameter);
                return true;
            }

            RemoveMixerTransition(parameter);
            mixerTransitions.Add(new MixerParameterTransition
            {
                Parameter = parameter,
                StartValue = startValue,
                TargetValue = targetValue,
                Duration = duration
            });
            return true;
        }

        /// <summary>停止所有音频。</summary>
        public void StopAll(float fadeOutSeconds = 0f)
        {
            for (int i = 0; i < handles.Count; i++)
            {
                handles[i].BeginStop(fadeOutSeconds);
            }
        }

        /// <summary>停止指定Key的音频。</summary>
        public void Stop(string key, float fadeOutSeconds = 0f)
        {
            for (int i = 0; i < handles.Count; i++)
            {
                if (string.Equals(handles[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    handles[i].BeginStop(fadeOutSeconds);
                }
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

        /// <summary>停止指定Key前缀的音频。</summary>
        public void StopByKeyPrefix(string keyPrefix, float fadeOutSeconds = 0f)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                return;
            }

            for (int i = 0; i < handles.Count; i++)
            {
                string key = handles[i].Key;
                if (!string.IsNullOrWhiteSpace(key) && key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    handles[i].BeginStop(fadeOutSeconds);
                }
            }
        }

        /// <summary>停止指定分组音频。</summary>
        public void StopGroup(string group, float fadeOutSeconds = 0f)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                return;
            }

            for (int i = 0; i < handles.Count; i++)
            {
                if (string.Equals(handles[i].Group, group, StringComparison.OrdinalIgnoreCase))
                {
                    handles[i].BeginStop(fadeOutSeconds);
                }
            }
        }

        /// <summary>暂停指定通道音频。</summary>
        public void PauseChannel(AudioChannel channel)
        {
            for (int i = 0; i < handles.Count; i++)
            {
                if (handles[i].Channel == channel)
                {
                    handles[i].Pause();
                }
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

        /// <summary>恢复指定通道音频。</summary>
        public void ResumeChannel(AudioChannel channel)
        {
            for (int i = 0; i < handles.Count; i++)
            {
                if (handles[i].Channel == channel)
                {
                    handles[i].Resume();
                }
            }
        }

        /// <summary>更新运行时播放状态。</summary>
        public void Tick(float deltaTime)
        {
            serviceTime += Mathf.Max(0f, deltaTime);
            UpdateMixerTransitions(deltaTime);
            UpdateDucking(deltaTime);

            for (int i = handles.Count - 1; i >= 0; i--)
            {
                AudioPlaybackHandle handle = handles[i];
                float masterPlaybackVolume = HasMixerVolumeParameter(AudioChannel.Master) ? 1f : masterVolume;
                float channelVolume = ResolvePlaybackChannelVolume(handle.Channel);
                if (handle.Tick(deltaTime, masterPlaybackVolume, channelVolume))
                {
                    bool wasMusic = handle.Channel == AudioChannel.Music;
                    ReleaseHandleClip(handle);
                    sourcePool.Release(handle.Source);
                    handles.RemoveAt(i);
                    if (wasMusic)
                    {
                        TryPlayNextQueuedMusic();
                    }
                }
            }
        }

        /// <summary>创建播放句柄。</summary>
        private AudioPlaybackHandle CreatePlayback(AudioClip clip, AudioCueData cue, AudioPlayOptions options, AudioChannel channel, AudioMixerGroup outputGroup, float clipVolume = 1f, float clipPitch = 1f)
        {
            return CreatePlayback(clip, cue, null, clipProvider as IAudioClipReleaseProvider, options, channel, outputGroup, clipVolume, clipPitch);
        }

        /// <summary>创建播放句柄。</summary>
        private AudioPlaybackHandle CreatePlayback(AudioClip clip, AudioCueData cue, AudioClipReference clipReference, IAudioClipReleaseProvider releaseProvider, AudioPlayOptions options, AudioChannel channel, AudioMixerGroup outputGroup, float clipVolume = 1f, float clipPitch = 1f)
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
            if (source == null)
            {
                return null;
            }

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
                Key = cue != null ? cue.Key : null,
                Group = cue != null ? cue.Group : null,
                Clip = clip,
                ClipReference = clipReference,
                ReleaseProvider = releaseProvider,
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
                ApplyVoiceDucking = options.ApplyVoiceDucking,
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
            if (cue != null && !string.IsNullOrWhiteSpace(cue.Key))
            {
                lastPlayTimes[cue.Key] = serviceTime;
            }

            ApplyMixerVolumes();
            return handle;
        }

        /// <summary>尝试播放下一首队列音乐。</summary>
        private void TryPlayNextQueuedMusic()
        {
            if (musicQueue.Count == 0 || HasActiveMusic())
            {
                return;
            }

            while (musicQueue.Count > 0 && !HasActiveMusic())
            {
                QueuedMusic queued = musicQueue.Dequeue();
                AudioPlayOptions options = AudioPlayOptions.CopyOrDefault(queued.Options);
                options.Channel = AudioChannel.Music;
                options.ReplaceSameChannel = true;
                options.FadeIn = queued.FadeSeconds;
                options.FadeOut = queued.FadeSeconds;

                AudioPlayResult result = queued.Clip != null ? TryPlay(queued.Clip, options) : TryPlay(queued.Key, options);
                if (result.Success)
                {
                    return;
                }
            }
        }

        /// <summary>判断是否存在活动音乐。</summary>
        private bool HasActiveMusic()
        {
            for (int i = 0; i < handles.Count; i++)
            {
                AudioPlaybackHandle handle = handles[i];
                if (handle.Channel == AudioChannel.Music && !handle.HasStopped)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>异步播放数据库配置。</summary>
        private void PlayAsync(AudioCueData cue, AudioPlayOptions options, AudioAsyncPlayRequest request)
        {
            if (request == null || request.IsDone)
            {
                return;
            }

            if (cue == null || !cue.HasClips())
            {
                request.Complete(Fail(cue == null ? AudioPlayFailureReason.MissingCue : AudioPlayFailureReason.MissingClip, "音频配置为空或没有可用Clip。"));
                return;
            }

            if (TryCreatePlaybackGateFailure(cue, out AudioPlayResult gateFailure))
            {
                request.Complete(gateFailure);
                return;
            }

            AudioClipOption selected = cue.PickClip();
            if (selected == null)
            {
                request.Complete(Fail(AudioPlayFailureReason.MissingClip, $"音频配置 {cue.Key} 未选出可用Clip。"));
                return;
            }

            AudioClipReference reference = selected.Clip;
            AudioPlayOptions resolved = MergeOptions(cue, options);
            float clipVolume = cue.GetVolumeMultiplier();
            float clipPitch = cue.GetPitchMultiplier();
            IAudioClipProvider provider = clipProvider ?? new DirectAudioClipProvider();
            IAudioClipReleaseProvider releaseProvider = provider as IAudioClipReleaseProvider;
            provider.LoadClipAsync(
                reference,
                clip =>
                {
                    if (request.IsDone)
                    {
                        ReleaseClip(releaseProvider, reference, clip);
                        return;
                    }

                    if (clip == null)
                    {
                        request.Complete(Fail(AudioPlayFailureReason.MissingClip, $"音频配置 {cue.Key} 异步解析到空Clip。"));
                        return;
                    }

                    if (TryCreatePlaybackGateFailure(cue, out AudioPlayResult lateGateFailure))
                    {
                        ReleaseClip(releaseProvider, reference, clip);
                        request.Complete(lateGateFailure);
                        return;
                    }

                    AudioPlaybackHandle handle = CreatePlayback(clip, cue, reference, releaseProvider, resolved, resolved.Channel ?? cue.Channel, resolved.OutputGroup, clipVolume, clipPitch);
                    if (handle == null)
                    {
                        ReleaseClip(releaseProvider, reference, clip);
                        request.Complete(Fail(AudioPlayFailureReason.PoolLimitReached, $"音频配置 {cue.Key} 无法播放，音源池已达到最大数量。"));
                        return;
                    }

                    request.Complete(Succeed(handle, $"异步播放音频：{cue.Key}。"));
                },
                error =>
                {
                    if (request.IsDone)
                    {
                        return;
                    }

                    request.Complete(Fail(AudioPlayFailureReason.ProviderFailed, string.IsNullOrWhiteSpace(error) ? $"音频配置 {cue.Key} 无法解析Clip。" : error));
                });
        }

        /// <summary>检查播放门禁条件。</summary>
        private bool TryCreatePlaybackGateFailure(AudioCueData cue, out AudioPlayResult result)
        {
            if (cue == null || string.IsNullOrWhiteSpace(cue.Key))
            {
                result = default;
                return false;
            }

            if (cue.Cooldown > 0f &&
                lastPlayTimes.TryGetValue(cue.Key, out float lastPlayTime) &&
                serviceTime - lastPlayTime < cue.Cooldown)
            {
                result = Fail(AudioPlayFailureReason.Cooldown, $"音频配置 {cue.Key} 仍在冷却中。");
                return true;
            }

            if (cue.MaxSimultaneous > 0 && CountActiveHandles(cue.Key) >= cue.MaxSimultaneous)
            {
                result = Fail(AudioPlayFailureReason.MaxSimultaneous, $"音频配置 {cue.Key} 已达到最大同时播放数。");
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>统计指定Key的活动句柄数。</summary>
        private int CountActiveHandles(string key)
        {
            int count = 0;
            for (int i = 0; i < handles.Count; i++)
            {
                AudioPlaybackHandle handle = handles[i];
                if (!handle.HasStopped && string.Equals(handle.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>合并数据库配置和临时参数。</summary>
        private AudioPlayOptions MergeOptions(AudioCueData cue, AudioPlayOptions options)
        {
            AudioPlayOptions merged = AudioPlayOptions.CopyOrDefault(options);
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
            if (channel != AudioChannel.Master && soloChannel.HasValue && soloChannel.Value != channel)
            {
                return 0f;
            }

            if (mutedChannels.Contains(channel))
            {
                return 0f;
            }

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

        /// <summary>创建失败结果。</summary>
        private AudioPlayResult Fail(AudioPlayFailureReason reason, string message)
        {
            Log(AudioLogLevel.Warning, message);
            return new AudioPlayResult(null, reason, message);
        }

        /// <summary>创建成功结果。</summary>
        private AudioPlayResult Succeed(AudioPlaybackHandle handle, string message)
        {
            if (database != null && database.DebugSettings != null && database.DebugSettings.LogSuccessfulPlay)
            {
                Log(AudioLogLevel.Verbose, message);
            }

            return new AudioPlayResult(handle, AudioPlayFailureReason.None, message);
        }

        /// <summary>应用数据库中的音源池配置。</summary>
        private void ApplyPoolSettings()
        {
            AudioPoolSettings settings = database != null ? database.PoolSettings : null;
            int prewarmCount = settings != null ? settings.PrewarmCount : 8;
            int maxSourceCount = settings != null ? settings.MaxSourceCount : 64;
            sourcePool.Configure(maxSourceCount);
            sourcePool.Prewarm(prewarmCount);
        }

        /// <summary>释放播放句柄关联的资源剪辑。</summary>
        private void ReleaseHandleClip(AudioPlaybackHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            ReleaseClip(handle.ReleaseProvider, handle.ClipReference, handle.Clip);
            handle.ReleaseProvider = null;
            handle.ClipReference = null;
        }

        /// <summary>释放由资源系统提供的音频剪辑。</summary>
        private void ReleaseClip(IAudioClipReleaseProvider releaseProvider, AudioClipReference reference, AudioClip clip)
        {
            if (releaseProvider == null || clip == null)
            {
                return;
            }

            releaseProvider.ReleaseClip(reference, clip);
        }

        /// <summary>输出日志。</summary>
        private void Log(AudioLogLevel level, string message)
        {
            AudioLogLevel configured = database != null && database.DebugSettings != null ? database.DebugSettings.LogLevel : AudioLogLevel.Warning;
            if (configured == AudioLogLevel.None || (int)level > (int)configured)
            {
                return;
            }

            if (level == AudioLogLevel.Error)
            {
                Debug.LogError($"[CAudio] {message}");
            }
            else if (level == AudioLogLevel.Warning)
            {
                Debug.LogWarning($"[CAudio] {message}");
            }
            else
            {
                Debug.Log($"[CAudio] {message}");
            }
        }

        /// <summary>更新语音压低音乐状态。</summary>
        private void UpdateDucking(float deltaTime)
        {
            AudioMixerControlSettings settings = database != null ? database.MixerSettings : null;
            if (settings == null || !settings.EnableVoiceDucking)
            {
                duckMultiplier = 1f;
                return;
            }

            bool hasVoice = false;
            for (int i = 0; i < handles.Count; i++)
            {
                AudioPlaybackHandle handle = handles[i];
                if (handle.Channel == AudioChannel.Voice && handle.ApplyVoiceDucking && !handle.HasStopped)
                {
                    hasVoice = true;
                    break;
                }
            }

            float target = hasVoice ? Mathf.Clamp01(settings.DuckMusicVolume) : 1f;
            duckMultiplier = Mathf.MoveTowards(duckMultiplier, target, Mathf.Max(0.01f, settings.DuckFadeSpeed) * deltaTime);
            ApplyMixerVolume(AudioChannel.Music, ResolveChannelVolume(AudioChannel.Music));
        }

        /// <summary>应用全部混音器音量。</summary>
        private void ApplyMixerVolumes()
        {
            ApplyMixerVolume(AudioChannel.Master, masterVolume);
            ApplyMixerVolume(AudioChannel.Music, ResolveChannelVolume(AudioChannel.Music));
            ApplyMixerVolume(AudioChannel.Sfx, ResolveChannelVolume(AudioChannel.Sfx));
            ApplyMixerVolume(AudioChannel.Voice, ResolveChannelVolume(AudioChannel.Voice));
            ApplyMixerVolume(AudioChannel.Ambience, ResolveChannelVolume(AudioChannel.Ambience));
            ApplyMixerVolume(AudioChannel.Ui, ResolveChannelVolume(AudioChannel.Ui));
        }

        /// <summary>应用指定混音器音量。</summary>
        private void ApplyMixerVolume(AudioChannel channel, float volume)
        {
            AudioMixerControlSettings settings = database != null ? database.MixerSettings : null;
            if (settings == null || settings.Mixer == null || settings.VolumeParameters == null)
            {
                return;
            }

            string parameter = null;
            for (int i = 0; i < settings.VolumeParameters.Length; i++)
            {
                AudioMixerParameter item = settings.VolumeParameters[i];
                if (item != null && item.Channel == channel)
                {
                    parameter = item.ExposedVolumeParameter;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }

            float resolvedVolume = channel == AudioChannel.Music ? Mathf.Clamp01(volume * duckMultiplier) : Mathf.Clamp01(volume);
            settings.Mixer.SetFloat(parameter, VolumeToDecibel(resolvedVolume));
        }

        /// <summary>读取播放用通道音量。</summary>
        private float ResolvePlaybackChannelVolume(AudioChannel channel)
        {
            float volume = ResolveChannelVolume(channel);
            if (HasMixerVolumeParameter(channel))
            {
                return 1f;
            }

            if (channel == AudioChannel.Music)
            {
                return Mathf.Clamp01(volume * duckMultiplier);
            }

            return volume;
        }

        /// <summary>判断是否存在混音器音量参数。</summary>
        private bool HasMixerVolumeParameter(AudioChannel channel)
        {
            AudioMixerControlSettings settings = database != null ? database.MixerSettings : null;
            if (settings == null || settings.Mixer == null || settings.VolumeParameters == null)
            {
                return false;
            }

            for (int i = 0; i < settings.VolumeParameters.Length; i++)
            {
                AudioMixerParameter item = settings.VolumeParameters[i];
                if (item != null && item.Channel == channel && !string.IsNullOrWhiteSpace(item.ExposedVolumeParameter))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>线性音量转分贝。</summary>
        public static float VolumeToDecibel(float volume)
        {
            return volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;
        }

        /// <summary>更新混音器参数过渡。</summary>
        private void UpdateMixerTransitions(float deltaTime)
        {
            AudioMixer mixer = database != null && database.MixerSettings != null ? database.MixerSettings.Mixer : null;
            if (mixer == null || mixerTransitions.Count == 0)
            {
                mixerTransitions.Clear();
                return;
            }

            for (int i = mixerTransitions.Count - 1; i >= 0; i--)
            {
                MixerParameterTransition transition = mixerTransitions[i];
                transition.Elapsed += Mathf.Max(0f, deltaTime);
                float t = transition.Duration > 0f ? Mathf.Clamp01(transition.Elapsed / transition.Duration) : 1f;
                mixer.SetFloat(transition.Parameter, Mathf.Lerp(transition.StartValue, transition.TargetValue, t));
                if (t >= 1f)
                {
                    mixerTransitions.RemoveAt(i);
                }
            }
        }

        /// <summary>移除指定混音器参数过渡。</summary>
        private void RemoveMixerTransition(string parameter)
        {
            for (int i = mixerTransitions.Count - 1; i >= 0; i--)
            {
                if (string.Equals(mixerTransitions[i].Parameter, parameter, StringComparison.Ordinal))
                {
                    mixerTransitions.RemoveAt(i);
                }
            }
        }
    }
}
