using UnityEngine;

namespace CAudio.Samples
{
    /// <summary>CAudio 示例控制器，可挂到任意场景物体上体验核心 API。</summary>
    public sealed class CAudioSampleController : MonoBehaviour
    {
        [SerializeField] private AudioDatabase database;
        [SerializeField] private Transform followTarget;
        [SerializeField] private AudioClip directClip;
        [SerializeField] private string sfxKey = "sfx_click";
        [SerializeField] private string uiKey = "ui_click";
        [SerializeField] private string musicKey = "music_theme";
        [SerializeField] private string voiceKey = "voice_line";
        [SerializeField] private string ambienceKey = "ambience_loop";

        private AudioPlaybackHandle loopingHandle;

        /// <summary>初始化示例音频系统。</summary>
        private void Awake()
        {
            AudioManager.Initialize(database);
        }

        /// <summary>播放音效。</summary>
        public void PlaySfx()
        {
            AudioManager.PlaySfx(sfxKey);
        }

        /// <summary>播放 UI 音效。</summary>
        public void PlayUi()
        {
            AudioManager.PlayUi(uiKey);
        }

        /// <summary>播放音乐。</summary>
        public void PlayMusic()
        {
            AudioManager.CrossfadeMusic(musicKey, 1f);
        }

        /// <summary>播放语音并触发 Ducking。</summary>
        public void PlayVoice()
        {
            AudioManager.PlayVoice(voiceKey);
        }

        /// <summary>播放循环环境音。</summary>
        public void PlayAmbienceLoop()
        {
            loopingHandle = AudioManager.PlayAmbience(ambienceKey, new AudioPlayOptions
            {
                Loop = true,
                FadeIn = 0.5f,
                FadeOut = 0.5f
            });
        }

        /// <summary>在当前位置播放 3D 音效。</summary>
        public void PlayAtPosition()
        {
            AudioManager.PlayAt(sfxKey, transform.position);
        }

        /// <summary>播放跟随目标音效。</summary>
        public void PlayFollow()
        {
            if (followTarget != null)
            {
                AudioManager.PlayFollow(sfxKey, followTarget);
            }
        }

        /// <summary>播放直连剪辑。</summary>
        public void PlayDirectClip()
        {
            AudioManager.Play(directClip);
        }

        /// <summary>停止示例循环音频。</summary>
        public void StopLoop()
        {
            
            loopingHandle?.Stop();
        }
    }
}
