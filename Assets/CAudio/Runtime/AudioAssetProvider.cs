using System;
using UnityEngine;

namespace CAudio
{
    /// <summary>音频资源解析接口。</summary>
    public interface IAudioClipProvider
    {
        /// <summary>同步尝试解析音频剪辑。</summary>
        bool TryResolveClip(AudioClipReference reference, out AudioClip clip);

        /// <summary>异步解析音频剪辑。</summary>
        void LoadClipAsync(AudioClipReference reference, Action<AudioClip> onSuccess, Action<string> onFailure);
    }

    /// <summary>可选的音频资源释放接口，适用于 Addressables 或自定义资源系统。</summary>
    public interface IAudioClipReleaseProvider
    {
        /// <summary>释放先前解析出的音频剪辑。</summary>
        void ReleaseClip(AudioClipReference reference, AudioClip clip);
    }

    /// <summary>直连资源解析器。</summary>
    public sealed class DirectAudioClipProvider : IAudioClipProvider, IAudioClipReleaseProvider
    {
        /// <summary>同步尝试解析音频剪辑。</summary>
        public bool TryResolveClip(AudioClipReference reference, out AudioClip clip)
        {
            clip = reference != null ? reference.DirectClip : null;
            return clip != null;
        }

        /// <summary>异步解析音频剪辑。</summary>
        public void LoadClipAsync(AudioClipReference reference, Action<AudioClip> onSuccess, Action<string> onFailure)
        {
            if (TryResolveClip(reference, out AudioClip clip))
            {
                onSuccess?.Invoke(clip);
                return;
            }

            onFailure?.Invoke("未找到可用的直连音频剪辑。");
        }

        /// <summary>直连剪辑由 Unity 资产系统管理，无需释放。</summary>
        public void ReleaseClip(AudioClipReference reference, AudioClip clip)
        {
        }
    }
}
