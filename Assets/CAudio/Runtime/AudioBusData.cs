using System;
using UnityEngine;
using UnityEngine.Audio;

namespace CAudio
{
    /// <summary>音频总线配置。</summary>
    [Serializable]
    public sealed class AudioBusData
    {
        [SerializeField] private AudioChannel channel = AudioChannel.Sfx;
        [SerializeField] private AudioMixerGroup outputGroup;
        [SerializeField] private float volume = 1f;
        [SerializeField] private bool mute;

        /// <summary>获取通道。</summary>
        public AudioChannel Channel => channel;

        /// <summary>获取输出组。</summary>
        public AudioMixerGroup OutputGroup => outputGroup;

        /// <summary>获取音量。</summary>
        public float Volume => volume;

        /// <summary>获取静音状态。</summary>
        public bool Mute => mute;
    }
}
