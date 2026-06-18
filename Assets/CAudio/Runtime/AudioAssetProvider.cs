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

    /// <summary>直连资源解析器。</summary>
    public sealed class DirectAudioClipProvider : IAudioClipProvider
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
    }
}
