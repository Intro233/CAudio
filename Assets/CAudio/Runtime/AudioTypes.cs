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
    }
}
