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
        [SerializeField] private string group;
        [SerializeField] private AudioChannel channel = AudioChannel.Sfx;
        [SerializeField] private AudioSelectionMode selectionMode = AudioSelectionMode.Random;
        [SerializeField] private List<AudioClipOption> clips = new List<AudioClipOption>();
        [SerializeField] private Vector2 volumeRange = new Vector2(1f, 1f);
        [SerializeField] private Vector2 pitchRange = new Vector2(1f, 1f);
        [SerializeField] private bool loop;
        [SerializeField] private bool replaceSameChannel;
        [SerializeField] private float fadeInTime;
        [SerializeField] private float fadeOutTime = 0.1f;
        [SerializeField] private float cooldown;
        [SerializeField] private int maxSimultaneous;
        [SerializeField] private float spatialBlend;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 50f;
        [SerializeField] private int priority = 128;
        [SerializeField] private AudioMixerGroup outputGroup;

        [NonSerialized] private int sequentialIndex;
        [NonSerialized] private int lastClipIndex = -1;
        [NonSerialized] private List<int> shuffleBag;

        /// <summary>获取键值。</summary>
        public string Key => key;

        /// <summary>获取显示名。</summary>
        public string DisplayName => displayName;

        /// <summary>获取分组。</summary>
        public string Group => group;

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

        /// <summary>获取播放冷却时间。</summary>
        public float Cooldown => cooldown;

        /// <summary>获取最大同时播放数，0 表示不限制。</summary>
        public int MaxSimultaneous => maxSimultaneous;

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

        /// <summary>获取所有剪辑选项。</summary>
        public IReadOnlyList<AudioClipOption> Clips => clips;

        /// <summary>设置基础标识。</summary>
        public void SetIdentity(string valueKey, string valueDisplayName)
        {
            key = valueKey;
            displayName = valueDisplayName;
        }

        /// <summary>设置分组。</summary>
        public void SetGroup(string value)
        {
            group = value;
        }

        /// <summary>设置剪辑选择方式。</summary>
        public void SetSelectionMode(AudioSelectionMode mode)
        {
            selectionMode = mode;
        }

        /// <summary>设置随机音量倍率范围。</summary>
        public void SetVolumeRange(float min, float max)
        {
            volumeRange = CreateOrderedRange(min, max);
        }

        /// <summary>设置随机音调倍率范围。</summary>
        public void SetPitchRange(float min, float max)
        {
            pitchRange = CreateOrderedRange(min, max);
        }

        /// <summary>设置播放冷却时间。</summary>
        public void SetCooldown(float seconds)
        {
            cooldown = Mathf.Max(0f, seconds);
        }

        /// <summary>设置最大同时播放数，0 表示不限制。</summary>
        public void SetMaxSimultaneous(int count)
        {
            maxSimultaneous = Mathf.Max(0, count);
        }

        /// <summary>添加直连剪辑。</summary>
        public void AddClip(AudioClip clip, float weight = 1f)
        {
            AddClip(new AudioClipReference(clip), weight);
        }

        /// <summary>添加剪辑引用。</summary>
        public void AddClip(AudioClipReference clip, float weight = 1f)
        {
            if (clips == null)
            {
                clips = new List<AudioClipOption>();
            }

            clips.Add(new AudioClipOption(clip, weight));
        }

        /// <summary>清空剪辑列表。</summary>
        public void ClearClips()
        {
            if (clips == null)
            {
                clips = new List<AudioClipOption>();
            }
            else
            {
                clips.Clear();
            }

            ResetSelectionState();
        }

        /// <summary>选择一个剪辑选项。</summary>
        public AudioClipOption PickClip()
        {
            if (clips == null || clips.Count == 0)
            {
                return null;
            }

            switch (selectionMode)
            {
                case AudioSelectionMode.WeightedRandom:
                    return PickByWeight();
                case AudioSelectionMode.Sequential:
                    return PickSequentially();
                case AudioSelectionMode.Shuffle:
                    return PickFromShuffleBag();
                case AudioSelectionMode.NoImmediateRepeat:
                    return PickWithoutImmediateRepeat();
                default:
                    return RememberClip(UnityEngine.Random.Range(0, clips.Count));
            }
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

        /// <summary>读取是否存在可解析的剪辑引用。</summary>
        public bool HasResolvableClipReference()
        {
            if (clips == null)
            {
                return false;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                AudioClipOption option = clips[i];
                AudioClipReference reference = option != null ? option.Clip : null;
                if (reference != null &&
                    (reference.DirectClip != null || !string.IsNullOrWhiteSpace(reference.AddressKey)))
                {
                    return true;
                }
            }

            return false;
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
                return RememberClip(UnityEngine.Random.Range(0, clips.Count));
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
                    return RememberClip(i);
                }
            }

            return RememberClip(clips.Count - 1);
        }

        /// <summary>顺序选择剪辑。</summary>
        private AudioClipOption PickSequentially()
        {
            int index = sequentialIndex % clips.Count;
            sequentialIndex = (sequentialIndex + 1) % clips.Count;
            return RememberClip(index);
        }

        /// <summary>从洗牌袋中选择剪辑。</summary>
        private AudioClipOption PickFromShuffleBag()
        {
            if (shuffleBag == null || shuffleBag.Count == 0 || HasInvalidShuffleIndex())
            {
                RefillShuffleBag();
            }

            int bagIndex = UnityEngine.Random.Range(0, shuffleBag.Count);
            int clipIndex = shuffleBag[bagIndex];
            if (shuffleBag.Count > 1 && clipIndex == lastClipIndex)
            {
                bagIndex = (bagIndex + 1) % shuffleBag.Count;
                clipIndex = shuffleBag[bagIndex];
            }

            shuffleBag.RemoveAt(bagIndex);
            return RememberClip(clipIndex);
        }

        /// <summary>选择一个不与上一次重复的剪辑。</summary>
        private AudioClipOption PickWithoutImmediateRepeat()
        {
            if (clips.Count <= 1)
            {
                return RememberClip(0);
            }

            if (lastClipIndex < 0)
            {
                return RememberClip(UnityEngine.Random.Range(0, clips.Count));
            }

            int index = UnityEngine.Random.Range(0, clips.Count - 1);
            if (index >= lastClipIndex)
            {
                index++;
            }

            return RememberClip(index);
        }

        /// <summary>重填洗牌袋。</summary>
        private void RefillShuffleBag()
        {
            if (shuffleBag == null)
            {
                shuffleBag = new List<int>();
            }

            shuffleBag.Clear();
            for (int i = 0; i < clips.Count; i++)
            {
                shuffleBag.Add(i);
            }
        }

        /// <summary>记录并返回剪辑。</summary>
        private AudioClipOption RememberClip(int index)
        {
            lastClipIndex = Mathf.Clamp(index, 0, clips.Count - 1);
            return clips[lastClipIndex];
        }

        /// <summary>判断洗牌袋是否包含失效索引。</summary>
        private bool HasInvalidShuffleIndex()
        {
            if (shuffleBag == null)
            {
                return false;
            }

            for (int i = 0; i < shuffleBag.Count; i++)
            {
                if (shuffleBag[i] < 0 || shuffleBag[i] >= clips.Count)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>重置选择状态。</summary>
        private void ResetSelectionState()
        {
            sequentialIndex = 0;
            lastClipIndex = -1;
            shuffleBag?.Clear();
        }

        /// <summary>创建有序范围。</summary>
        private Vector2 CreateOrderedRange(float min, float max)
        {
            return min <= max ? new Vector2(min, max) : new Vector2(max, min);
        }
    }
}
