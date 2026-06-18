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
        [SerializeField] private List<AudioBusData> buses = new List<AudioBusData>();

        [NonSerialized] private Dictionary<string, AudioCueData> cueLookup;
        [NonSerialized] private Dictionary<AudioChannel, AudioBusData> busLookup;

        /// <summary>获取所有音频配置。</summary>
        public IReadOnlyList<AudioCueData> Cues => cues;

        /// <summary>重建运行时缓存。</summary>
        public void RebuildCache()
        {
            cueLookup = new Dictionary<string, AudioCueData>(StringComparer.OrdinalIgnoreCase);
            busLookup = new Dictionary<AudioChannel, AudioBusData>();

            for (int i = 0; i < cues.Count; i++)
            {
                AudioCueData cue = cues[i];
                if (cue == null || string.IsNullOrWhiteSpace(cue.Key) || cueLookup.ContainsKey(cue.Key))
                {
                    continue;
                }

                cueLookup.Add(cue.Key, cue);
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
    }
}
