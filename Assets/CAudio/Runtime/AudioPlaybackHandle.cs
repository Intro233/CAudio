using UnityEngine;

namespace CAudio
{
    /// <summary>音频播放句柄。</summary>
    public sealed class AudioPlaybackHandle
    {
        internal int Id;
        internal AudioService Service;
        internal AudioSource Source;
        internal AudioChannel Channel;
        internal string Key;
        internal string Group;
        internal AudioClip Clip;
        internal AudioClipReference ClipReference;
        internal IAudioClipReleaseProvider ReleaseProvider;
        internal Transform FollowTarget;
        internal Vector3 WorldPosition;
        internal bool UseWorldPosition;
        internal bool IsLoop;
        internal bool ReplaceSameChannel;
        internal float BaseVolume;
        internal float BasePitch;
        internal float FadeInTime;
        internal float FadeOutTime;
        internal float Delay;
        internal bool WaitingForDelay;
        internal bool Started;
        internal bool ApplyVoiceDucking;
        internal float Priority;
        internal float Elapsed;
        internal float StartVolume;
        internal float StopStartVolume;
        internal bool StopRequested;
        internal bool IsPaused;
        internal float StopFadeOutTime;
        internal float StopFadeOutDuration;
        internal bool HasStopped;

        /// <summary>获取是否仍在播放。</summary>
        public bool IsPlaying => Source != null && Source.isPlaying && !HasStopped;

        /// <summary>获取是否已停止并等待回收。</summary>
        public bool IsStopped => HasStopped;

        /// <summary>获取是否已暂停。</summary>
        public bool Paused => IsPaused;

        /// <summary>获取当前 AudioSource 音量，主要用于调试和验证。</summary>
        public float CurrentVolume => Source != null ? Source.volume : 0f;

        /// <summary>获取播放Key。</summary>
        public string PlaybackKey => Key;

        /// <summary>获取播放分组。</summary>
        public string PlaybackGroup => Group;

        /// <summary>获取播放通道。</summary>
        public AudioChannel PlaybackChannel => Channel;

        /// <summary>停止播放。</summary>
        public void Stop(float fadeOutSeconds = -1f)
        {
            if (HasStopped)
            {
                return;
            }

            if (Service != null)
            {
                Service.StopHandle(this, fadeOutSeconds);
            }
        }

        /// <summary>暂停播放。</summary>
        public void Pause()
        {
            if (Source == null || HasStopped)
            {
                return;
            }

            IsPaused = true;
            Source.Pause();
        }

        /// <summary>继续播放。</summary>
        public void Resume()
        {
            if (Source == null || HasStopped)
            {
                return;
            }

            IsPaused = false;
            Source.UnPause();
        }

        /// <summary>设置基础音量。</summary>
        public void SetVolume(float volume)
        {
            BaseVolume = Mathf.Max(0f, volume);
        }

        /// <summary>设置基础音调。</summary>
        public void SetPitch(float pitch)
        {
            BasePitch = pitch;
        }

        /// <summary>更新播放状态。</summary>
        internal bool Tick(float deltaTime, float masterVolume, float channelVolume)
        {
            if (HasStopped)
            {
                return true;
            }

            if (Source == null)
            {
                HasStopped = true;
                return true;
            }

            if (FollowTarget != null)
            {
                WorldPosition = FollowTarget.position;
                Source.transform.position = WorldPosition;
            }

            if (StopRequested)
            {
                StopFadeOutTime -= deltaTime;
                float t = StopFadeOutDuration > 0f ? Mathf.Clamp01(StopFadeOutTime / StopFadeOutDuration) : 0f;
                Source.volume = StopStartVolume * t;
                if (StopFadeOutTime <= 0f)
                {
                    Finish();
                    return true;
                }

                return false;
            }

            if (IsPaused)
            {
                return false;
            }

            if (WaitingForDelay)
            {
                Delay -= deltaTime;
                if (Delay > 0f)
                {
                    return false;
                }

                if (!Source.isPlaying)
                {
                    return false;
                }

                WaitingForDelay = false;
                Started = true;
            }

            Elapsed += deltaTime;

            if (Source.isPlaying)
            {
                Started = true;
            }

            if (Started && !Source.loop && !Source.isPlaying)
            {
                Finish();
                return true;
            }

            float fadeInFactor = 1f;
            if (FadeInTime > 0f && Elapsed < FadeInTime)
            {
                fadeInFactor = Mathf.Clamp01(Elapsed / FadeInTime);
            }

            Source.volume = BaseVolume * fadeInFactor * masterVolume * channelVolume;
            Source.pitch = BasePitch;
            return false;
        }

        /// <summary>开始渐隐停止。</summary>
        internal void BeginStop(float fadeOutSeconds)
        {
            if (HasStopped)
            {
                return;
            }

            if (StopRequested && fadeOutSeconds >= StopFadeOutTime)
            {
                return;
            }

            StopRequested = true;
            StopFadeOutTime = Mathf.Max(0f, fadeOutSeconds);
            StopFadeOutDuration = StopFadeOutTime;
            if (StopFadeOutTime <= 0f)
            {
                Finish();
            }
            else if (Source != null)
            {
                StopStartVolume = Source.volume;
                if (WaitingForDelay && !Source.isPlaying)
                {
                    Source.Stop();
                }
            }
        }

        /// <summary>完成回收。</summary>
        internal void Finish()
        {
            if (HasStopped)
            {
                return;
            }

            HasStopped = true;
            if (Source != null)
            {
                Source.Stop();
                Source.clip = null;
            }
        }
    }
}
