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
        WeightedRandom
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
        ProviderFailed
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

        /// <summary>创建校验结果。</summary>
        public AudioDatabaseValidationIssue(AudioLogLevel level, string message)
        {
            Level = level;
            Message = message;
        }
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
    }
}
