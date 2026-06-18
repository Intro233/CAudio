using UnityEngine;

namespace CAudio
{
    /// <summary>场景中的音频系统初始化器。</summary>
    public sealed class AudioSystemBootstrap : MonoBehaviour
    {
        [SerializeField] private AudioDatabase database;

        /// <summary>启动时初始化音频系统。</summary>
        private void Awake()
        {
            AudioManager.Initialize(database);
        }
    }
}
