using UnityEngine;

namespace CAudio
{
    /// <summary>独立音频配置资产。</summary>
    [CreateAssetMenu(menuName = "CAudio/Audio Cue", fileName = "AudioCue")]
    public sealed class AudioCueAsset : ScriptableObject
    {
        [SerializeField] private AudioCueData data = new AudioCueData();

        /// <summary>获取配置数据。</summary>
        public AudioCueData Data => data;
    }
}
