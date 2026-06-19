using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CAudio
{
    /// <summary>音频通道分类。</summary>
    public enum AudioChannel
    {
        Master,
        Music,
        Sfx,
        Voice,
        Ambience,
        Ui,
        Custom
    }

    /// <summary>音频剪辑的选择方式。</summary>
    public enum AudioSelectionMode
    {
        Random,
        WeightedRandom,
        Sequential,
        Shuffle,
        NoImmediateRepeat
    }

    /// <summary>音频系统日志级别。</summary>
    public enum AudioLogLevel
    {
        None,
        Error,
        Warning,
        Verbose
    }

    /// <summary>音频播放失败原因。</summary>
    public enum AudioPlayFailureReason
    {
        None,
        NotInitialized,
        MissingDatabase,
        EmptyKey,
        CueNotFound,
        MissingCue,
        MissingClip,
        ProviderFailed,
        Cancelled,
        Cooldown,
        MaxSimultaneous,
        PoolLimitReached
    }

    /// <summary>音频播放结果。</summary>
    public readonly struct AudioPlayResult
    {
        public readonly AudioPlaybackHandle Handle;
        public readonly AudioPlayFailureReason FailureReason;
        public readonly string Message;

        /// <summary>获取是否播放成功。</summary>
        public bool Success => Handle != null && FailureReason == AudioPlayFailureReason.None;

        /// <summary>创建播放结果。</summary>
        public AudioPlayResult(AudioPlaybackHandle handle, AudioPlayFailureReason failureReason, string message)
        {
            Handle = handle;
            FailureReason = failureReason;
            Message = message;
        }
    }

    /// <summary>音频数据库校验结果。</summary>
    public readonly struct AudioDatabaseValidationIssue
    {
        public readonly AudioLogLevel Level;
        public readonly string Message;
        public readonly string CueKey;
        public readonly UnityEngine.Object Context;

        /// <summary>创建校验结果。</summary>
        public AudioDatabaseValidationIssue(AudioLogLevel level, string message)
            : this(level, message, null, null)
        {
        }

        /// <summary>创建带定位信息的校验结果。</summary>
        public AudioDatabaseValidationIssue(AudioLogLevel level, string message, string cueKey, UnityEngine.Object context)
        {
            Level = level;
            Message = message;
            CueKey = cueKey;
            Context = context;
        }
    }

    /// <summary>音源池配置。</summary>
    [Serializable]
    public sealed class AudioPoolSettings
    {
        [SerializeField] private int prewarmCount = 8;
        [SerializeField] private int maxSourceCount = 64;

        /// <summary>获取预热音源数量。</summary>
        public int PrewarmCount => Mathf.Max(0, prewarmCount);

        /// <summary>获取最大音源数量，0 表示不限制。</summary>
        public int MaxSourceCount => Mathf.Max(0, maxSourceCount);
    }

    /// <summary>音频剪辑引用，当前优先使用直连剪辑，后续可平滑切到地址化资源。</summary>
    [Serializable]
    public sealed class AudioClipReference
    {
        [SerializeField] private AudioClip directClip;
        [SerializeField] private string addressKey;

        /// <summary>获取直连剪辑。</summary>
        public AudioClip DirectClip => directClip;

        /// <summary>获取预留的资源键。</summary>
        public string AddressKey => addressKey;

        /// <summary>创建空引用。</summary>
        public AudioClipReference()
        {
        }

        /// <summary>创建直连剪辑引用。</summary>
        public AudioClipReference(AudioClip directClip)
        {
            this.directClip = directClip;
        }

        /// <summary>创建地址化资源引用。</summary>
        public AudioClipReference(string addressKey)
        {
            this.addressKey = addressKey;
        }
    }

    /// <summary>异步播放请求。</summary>
    public sealed class AudioAsyncPlayRequest
    {
        private readonly Action<AudioPlayResult> onComplete;

        /// <summary>获取是否完成。</summary>
        public bool IsDone { get; private set; }

        /// <summary>获取是否已取消。</summary>
        public bool IsCancelled { get; private set; }

        /// <summary>获取播放结果。</summary>
        public AudioPlayResult Result { get; private set; }

        /// <summary>创建异步播放请求。</summary>
        public AudioAsyncPlayRequest(Action<AudioPlayResult> onComplete = null)
        {
            this.onComplete = onComplete;
        }

        /// <summary>取消请求。</summary>
        public void Cancel()
        {
            if (IsDone)
            {
                return;
            }

            IsCancelled = true;
            Complete(new AudioPlayResult(null, AudioPlayFailureReason.Cancelled, "异步播放请求已取消。"));
        }

        /// <summary>完成请求。</summary>
        internal void Complete(AudioPlayResult result)
        {
            if (IsDone)
            {
                return;
            }

            Result = result;
            IsDone = true;
            onComplete?.Invoke(result);
        }
    }

    /// <summary>单个可播放剪辑选项。</summary>
    [Serializable]
    public sealed class AudioClipOption
    {
        [SerializeField] private AudioClipReference clip = new AudioClipReference();
        [SerializeField] private float weight = 1f;

        /// <summary>获取剪辑引用。</summary>
        public AudioClipReference Clip => clip;

        /// <summary>获取权重。</summary>
        public float Weight => weight;

        /// <summary>创建默认剪辑选项。</summary>
        public AudioClipOption()
        {
        }

        /// <summary>创建直连剪辑选项。</summary>
        public AudioClipOption(AudioClip clip, float weight = 1f)
            : this(new AudioClipReference(clip), weight)
        {
        }

        /// <summary>创建剪辑引用选项。</summary>
        public AudioClipOption(AudioClipReference clip, float weight = 1f)
        {
            this.clip = clip ?? new AudioClipReference();
            this.weight = Mathf.Max(0f, weight);
        }
    }

    /// <summary>播放时的临时覆盖参数。</summary>
    public sealed class AudioPlayOptions
    {
        public Vector3? Position;
        public Transform FollowTarget;
        public float Volume = 1f;
        public float Pitch = 1f;
        public float Delay;
        public bool? Loop;
        public float? FadeIn;
        public float? FadeOut;
        public AudioChannel? Channel;
        public AudioMixerGroup OutputGroup;
        public bool? ReplaceSameChannel;
        public bool SpatialOverride;
        public float SpatialBlend = 1f;
        public float? MinDistance;
        public float? MaxDistance;
        public int? Priority;
        public bool ApplyVoiceDucking = true;

        /// <summary>创建一份可安全修改的副本。</summary>
        public AudioPlayOptions Clone()
        {
            return new AudioPlayOptions
            {
                Position = Position,
                FollowTarget = FollowTarget,
                Volume = Volume,
                Pitch = Pitch,
                Delay = Delay,
                Loop = Loop,
                FadeIn = FadeIn,
                FadeOut = FadeOut,
                Channel = Channel,
                OutputGroup = OutputGroup,
                ReplaceSameChannel = ReplaceSameChannel,
                SpatialOverride = SpatialOverride,
                SpatialBlend = SpatialBlend,
                MinDistance = MinDistance,
                MaxDistance = MaxDistance,
                Priority = Priority,
                ApplyVoiceDucking = ApplyVoiceDucking
            };
        }

        /// <summary>获取一份可安全修改的选项。</summary>
        public static AudioPlayOptions CopyOrDefault(AudioPlayOptions options)
        {
            return options != null ? options.Clone() : new AudioPlayOptions();
        }
    }
}
