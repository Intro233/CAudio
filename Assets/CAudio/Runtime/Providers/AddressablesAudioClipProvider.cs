#if CAUDIO_ADDRESSABLES
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CAudio
{
    /// <summary>Addressables 音频资源解析器。</summary>
    public sealed class AddressablesAudioClipProvider : IAudioClipProvider
    {
        /// <summary>同步解析不适用于 Addressables，始终返回 false。</summary>
        public bool TryResolveClip(AudioClipReference reference, out AudioClip clip)
        {
            clip = null;
            return false;
        }

        /// <summary>异步加载 Addressables 音频剪辑。</summary>
        public void LoadClipAsync(AudioClipReference reference, Action<AudioClip> onSuccess, Action<string> onFailure)
        {
            if (reference == null || string.IsNullOrWhiteSpace(reference.AddressKey))
            {
                onFailure?.Invoke("Addressables 音频地址为空。");
                return;
            }

            AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(reference.AddressKey);
            handle.Completed += operation =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded && operation.Result != null)
                {
                    onSuccess?.Invoke(operation.Result);
                    return;
                }

                onFailure?.Invoke($"Addressables 音频加载失败：{reference.AddressKey}。");
            };
        }
    }
}
#endif
