using System;
using System.Collections.Generic;
using UnityEngine;

namespace CAudio
{
    /// <summary>音频数据库资产。</summary>
    [CreateAssetMenu(menuName = "CAudio/Audio Database", fileName = "AudioDatabase")]
    public sealed class AudioDatabase : ScriptableObject
    {
        [SerializeField] private List<AudioCueData> cues = new List<AudioCueData>();
        [SerializeField] private List<AudioCueAsset> cueAssets = new List<AudioCueAsset>();
        [SerializeField] private List<AudioBusData> buses = new List<AudioBusData>();
        [SerializeField] private AudioDebugSettings debugSettings = new AudioDebugSettings();
        [SerializeField] private AudioMixerControlSettings mixerSettings = new AudioMixerControlSettings();

        [NonSerialized] private Dictionary<string, AudioCueData> cueLookup;
        [NonSerialized] private Dictionary<AudioChannel, AudioBusData> busLookup;

        /// <summary>获取所有音频配置。</summary>
        public IReadOnlyList<AudioCueData> Cues => cues;

        /// <summary>获取所有独立音频配置资产。</summary>
        public IReadOnlyList<AudioCueAsset> CueAssets => cueAssets;

        /// <summary>获取调试配置。</summary>
        public AudioDebugSettings DebugSettings => debugSettings;

        /// <summary>获取混音器配置。</summary>
        public AudioMixerControlSettings MixerSettings => mixerSettings;

        /// <summary>添加内嵌音频配置。</summary>
        public void AddCue(AudioCueData cue)
        {
            if (cue == null)
            {
                return;
            }

            cues.Add(cue);
            RebuildCache();
        }

        /// <summary>清空内嵌音频配置。</summary>
        public void ClearCues()
        {
            cues.Clear();
            RebuildCache();
        }

        /// <summary>添加独立音频配置资产。</summary>
        public void AddCueAsset(AudioCueAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            cueAssets.Add(asset);
            RebuildCache();
        }

        /// <summary>清空独立音频配置资产引用。</summary>
        public void ClearCueAssets()
        {
            cueAssets.Clear();
            RebuildCache();
        }

        /// <summary>重建运行时缓存。</summary>
        public void RebuildCache()
        {
            cueLookup = new Dictionary<string, AudioCueData>(StringComparer.OrdinalIgnoreCase);
            busLookup = new Dictionary<AudioChannel, AudioBusData>();

            for (int i = 0; i < cues.Count; i++)
            {
                AudioCueData cue = cues[i];
                AddCueToLookup(cue);
            }

            for (int i = 0; i < cueAssets.Count; i++)
            {
                AudioCueAsset asset = cueAssets[i];
                AddCueToLookup(asset != null ? asset.Data : null);
            }

            for (int i = 0; i < buses.Count; i++)
            {
                AudioBusData bus = buses[i];
                if (bus == null)
                {
                    continue;
                }

                busLookup[bus.Channel] = bus;
            }
        }

        /// <summary>校验数据库配置。</summary>
        public List<AudioDatabaseValidationIssue> Validate()
        {
            List<AudioDatabaseValidationIssue> issues = new List<AudioDatabaseValidationIssue>();
            HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ValidateCueList(cues, keys, issues, "内嵌条目");

            for (int i = 0; i < cueAssets.Count; i++)
            {
                AudioCueAsset asset = cueAssets[i];
                if (asset == null)
                {
                    issues.Add(new AudioDatabaseValidationIssue(AudioLogLevel.Warning, $"独立Cue资产第 {i} 项为空。"));
                    continue;
                }

                ValidateCue(asset.Data, keys, issues, asset.name);
            }

            for (int i = 0; i < buses.Count; i++)
            {
                AudioBusData bus = buses[i];
                if (bus == null)
                {
                    issues.Add(new AudioDatabaseValidationIssue(AudioLogLevel.Warning, $"总线第 {i} 项为空。"));
                    continue;
                }

                for (int j = i + 1; j < buses.Count; j++)
                {
                    AudioBusData other = buses[j];
                    if (other != null && other.Channel == bus.Channel)
                    {
                        issues.Add(new AudioDatabaseValidationIssue(AudioLogLevel.Warning, $"总线 {bus.Channel} 存在重复配置。"));
                        break;
                    }
                }
            }

            return issues;
        }

        /// <summary>尝试按键查找配置。</summary>
        public bool TryGetCue(string key, out AudioCueData cue)
        {
            if (cueLookup == null)
            {
                RebuildCache();
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                cue = null;
                return false;
            }

            return cueLookup.TryGetValue(key, out cue);
        }

        /// <summary>按键获取配置。</summary>
        public AudioCueData GetCue(string key)
        {
            TryGetCue(key, out AudioCueData cue);
            return cue;
        }

        /// <summary>按通道获取总线。</summary>
        public AudioBusData GetBus(AudioChannel channel)
        {
            if (busLookup == null)
            {
                RebuildCache();
            }

            busLookup.TryGetValue(channel, out AudioBusData bus);
            return bus;
        }

        /// <summary>添加配置到缓存。</summary>
        private void AddCueToLookup(AudioCueData cue)
        {
            if (cue == null || string.IsNullOrWhiteSpace(cue.Key) || cueLookup.ContainsKey(cue.Key))
            {
                return;
            }

            cueLookup.Add(cue.Key, cue);
        }

        /// <summary>校验配置列表。</summary>
        private void ValidateCueList(List<AudioCueData> cueList, HashSet<string> keys, List<AudioDatabaseValidationIssue> issues, string label)
        {
            for (int i = 0; i < cueList.Count; i++)
            {
                ValidateCue(cueList[i], keys, issues, $"{label}第 {i} 项");
            }
        }

        /// <summary>校验单条配置。</summary>
        private void ValidateCue(AudioCueData cue, HashSet<string> keys, List<AudioDatabaseValidationIssue> issues, string label)
        {
            if (cue == null)
            {
                issues.Add(new AudioDatabaseValidationIssue(AudioLogLevel.Warning, $"{label}为空。"));
                return;
            }

            if (string.IsNullOrWhiteSpace(cue.Key))
            {
                issues.Add(new AudioDatabaseValidationIssue(AudioLogLevel.Warning, $"{label}缺少Key。"));
                return;
            }

            if (!keys.Add(cue.Key))
            {
                issues.Add(new AudioDatabaseValidationIssue(AudioLogLevel.Warning, $"Key重复：{cue.Key}。"));
            }

            if (!cue.HasClips())
            {
                issues.Add(new AudioDatabaseValidationIssue(AudioLogLevel.Warning, $"{cue.Key} 没有配置Clip。"));
            }
            else if (!cue.HasResolvableClipReference())
            {
                issues.Add(new AudioDatabaseValidationIssue(AudioLogLevel.Warning, $"{cue.Key} 没有可解析的Clip引用。"));
            }
        }
    }
}
