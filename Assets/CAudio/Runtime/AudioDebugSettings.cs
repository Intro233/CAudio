using UnityEngine;

namespace CAudio
{
    /// <summary>音频调试配置。</summary>
    [System.Serializable]
    public sealed class AudioDebugSettings
    {
        [SerializeField] private AudioLogLevel logLevel = AudioLogLevel.Warning;
        [SerializeField] private bool logSuccessfulPlay;

        /// <summary>获取日志级别。</summary>
        public AudioLogLevel LogLevel => logLevel;

        /// <summary>获取是否记录成功播放。</summary>
        public bool LogSuccessfulPlay => logSuccessfulPlay;
    }
}
