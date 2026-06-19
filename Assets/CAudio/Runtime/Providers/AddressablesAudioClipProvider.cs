#if CAUDIO_ADDRESSABLES
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CAudio
{
    /// <summary>Addressables 音频资源解析器。</summary>
    public sealed class AddressablesAudioClipProvider : IAudioClipProvider, IAudioClipReleaseProvider
    {
        private readonly Dictionary<AudioClip, Stack<AsyncOperationHandle<AudioClip>>> loadedHandles = new Dictionary<AudioClip, Stack<AsyncOperationHandle<AudioClip>>>();

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
                    RegisterLoadedHandle(operation.Result, operation);
                    onSuccess?.Invoke(operation.Result);
                    return;
                }

                Addressables.Release(operation);
                onFailure?.Invoke($"Addressables 音频加载失败：{reference.AddressKey}。");
            };
        }

        /// <summary>释放 Addressables 音频剪辑。</summary>
        public void ReleaseClip(AudioClipReference reference, AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (!loadedHandles.TryGetValue(clip, out Stack<AsyncOperationHandle<AudioClip>> handles) || handles.Count == 0)
            {
                return;
            }

            AsyncOperationHandle<AudioClip> handle = handles.Pop();
            Addressables.Release(handle);
            if (handles.Count == 0)
            {
                loadedHandles.Remove(clip);
            }
        }

        /// <summary>记录可释放的 Addressables 句柄。</summary>
        private void RegisterLoadedHandle(AudioClip clip, AsyncOperationHandle<AudioClip> handle)
        {
            if (!loadedHandles.TryGetValue(clip, out Stack<AsyncOperationHandle<AudioClip>> handles))
            {
                handles = new Stack<AsyncOperationHandle<AudioClip>>();
                loadedHandles.Add(clip, handles);
            }

            handles.Push(handle);
        }
    }
}
#endif
