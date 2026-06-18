using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CAudio
{
    /// <summary>数据库中的单条音频配置。</summary>
    [Serializable]
    public sealed class AudioCueData
    {
        [SerializeField] private string key;
        [SerializeField] private string displayName;
        [SerializeField] private AudioChannel channel = AudioChannel.Sfx;
        [SerializeField] private AudioSelectionMode selectionMode = AudioSelectionMode.Random;
        [SerializeField] private List<AudioClipOption> clips = new List<AudioClipOption>();
        [SerializeField] private Vector2 volumeRange = new Vector2(1f, 1f);
        [SerializeField] private Vector2 pitchRange = new Vector2(1f, 1f);
        [SerializeField] private bool loop;
        [SerializeField] private bool replaceSameChannel;
        [SerializeField] private float fadeInTime;
        [SerializeField] private float fadeOutTime = 0.1f;
        [SerializeField] private float spatialBlend;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 50f;
        [SerializeField] private int priority = 128;
        [SerializeField] private AudioMixerGroup outputGroup;

        /// <summary>获取键值。</summary>
        public string Key => key;

        /// <summary>获取显示名。</summary>
        public string DisplayName => displayName;

        /// <summary>获取通道。</summary>
        public AudioChannel Channel => channel;

        /// <summary>获取是否循环。</summary>
        public bool Loop => loop;

        /// <summary>获取是否替换同通道播放。</summary>
        public bool ReplaceSameChannel => replaceSameChannel;

        /// <summary>获取淡入时间。</summary>
        public float FadeInTime => fadeInTime;

        /// <summary>获取淡出时间。</summary>
        public float FadeOutTime => fadeOutTime;

        /// <summary>获取空间混合值。</summary>
        public float SpatialBlend => spatialBlend;

        /// <summary>获取最小距离。</summary>
        public float MinDistance => minDistance;

        /// <summary>获取最大距离。</summary>
        public float MaxDistance => maxDistance;

        /// <summary>获取优先级。</summary>
        public int Priority => priority;

        /// <summary>获取输出组。</summary>
        public AudioMixerGroup OutputGroup => outputGroup;

        /// <summary>选择一个剪辑选项。</summary>
        public AudioClipOption PickClip()
        {
            if (clips == null || clips.Count == 0)
            {
                return null;
            }

            if (selectionMode == AudioSelectionMode.WeightedRandom)
            {
                return PickByWeight();
            }

            return clips[UnityEngine.Random.Range(0, clips.Count)];
        }

        /// <summary>读取音量倍率。</summary>
        public float GetVolumeMultiplier()
        {
            return UnityEngine.Random.Range(volumeRange.x, volumeRange.y);
        }

        /// <summary>读取音调倍率。</summary>
        public float GetPitchMultiplier()
        {
            return UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        }

        /// <summary>读取是否存在可用剪辑。</summary>
        public bool HasClips()
        {
            return clips != null && clips.Count > 0;
        }

        /// <summary>生成音频播放的默认覆盖参数。</summary>
        public AudioPlayOptions CreateDefaultOptions()
        {
            return new AudioPlayOptions
            {
                Channel = channel,
                FadeIn = fadeInTime,
                FadeOut = fadeOutTime,
                Loop = loop,
                ReplaceSameChannel = replaceSameChannel,
                SpatialOverride = true,
                SpatialBlend = spatialBlend,
                MinDistance = minDistance,
                MaxDistance = maxDistance,
                Priority = priority,
                OutputGroup = outputGroup
            };
        }

        /// <summary>选择当前权重最高的剪辑。</summary>
        private AudioClipOption PickByWeight()
        {
            float totalWeight = 0f;
            for (int i = 0; i < clips.Count; i++)
            {
                totalWeight += Mathf.Max(0f, clips[i] != null ? clips[i].Weight : 0f);
            }

            if (totalWeight <= 0f)
            {
                return clips[UnityEngine.Random.Range(0, clips.Count)];
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cursor = 0f;
            for (int i = 0; i < clips.Count; i++)
            {
                AudioClipOption option = clips[i];
                float weight = Mathf.Max(0f, option != null ? option.Weight : 0f);
                cursor += weight;
                if (roll <= cursor)
                {
                    return option;
                }
            }

            return clips[clips.Count - 1];
        }
    }
}
