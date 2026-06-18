using System;
using UnityEngine;
using UnityEngine.Audio;

namespace CAudio
{
    /// <summary>混音器参数映射。</summary>
    [Serializable]
    public sealed class AudioMixerParameter
    {
        [SerializeField] private AudioChannel channel = AudioChannel.Master;
        [SerializeField] private string exposedVolumeParameter;

        /// <summary>获取通道。</summary>
        public AudioChannel Channel => channel;

        /// <summary>获取暴露参数名。</summary>
        public string ExposedVolumeParameter => exposedVolumeParameter;
    }

    /// <summary>混音器控制配置。</summary>
    [Serializable]
    public sealed class AudioMixerControlSettings
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerParameter[] volumeParameters = Array.Empty<AudioMixerParameter>();
        [SerializeField] private bool enableVoiceDucking = true;
        [SerializeField] private float duckMusicVolume = 0.35f;
        [SerializeField] private float duckFadeSpeed = 8f;

        /// <summary>获取混音器。</summary>
        public AudioMixer Mixer => mixer;

        /// <summary>获取音量参数映射。</summary>
        public AudioMixerParameter[] VolumeParameters => volumeParameters;

        /// <summary>获取是否开启语音压低音乐。</summary>
        public bool EnableVoiceDucking => enableVoiceDucking;

        /// <summary>获取压低后的音乐音量。</summary>
        public float DuckMusicVolume => duckMusicVolume;

        /// <summary>获取压低淡入淡出速度。</summary>
        public float DuckFadeSpeed => duckFadeSpeed;
    }
}
