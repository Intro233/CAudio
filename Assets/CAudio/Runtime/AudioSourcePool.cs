using System.Collections.Generic;
using UnityEngine;

namespace CAudio
{
    /// <summary>音源对象池。</summary>
    public sealed class AudioSourcePool
    {
        private readonly Transform root;
        private readonly Stack<AudioSource> cache = new Stack<AudioSource>();
        private readonly List<AudioSource> allSources = new List<AudioSource>();
        private int maxSourceCount = 64;

        /// <summary>创建对象池。</summary>
        public AudioSourcePool(Transform root)
        {
            this.root = root;
        }

        /// <summary>预热音源池。</summary>
        public void Prewarm(int count)
        {
            int targetCount = Mathf.Max(0, count);
            while (allSources.Count < targetCount && CanCreateSource())
            {
                AudioSource source = CreateSource();
                Release(source);
            }
        }

        /// <summary>配置音源池容量，maxSourceCount 为 0 时表示不限制。</summary>
        public void Configure(int maxSourceCount)
        {
            this.maxSourceCount = Mathf.Max(0, maxSourceCount);
        }

        /// <summary>获取一个音源。</summary>
        public AudioSource Rent()
        {
            if (cache.Count > 0)
            {
                AudioSource source = cache.Pop();
                if (source != null)
                {
                    ResetSource(source);
                    source.gameObject.SetActive(true);
                    return source;
                }
            }

            return CanCreateSource() ? CreateSource() : null;
        }

        /// <summary>归还一个音源。</summary>
        public void Release(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            ResetSource(source);
            source.transform.SetParent(root, false);
            source.gameObject.SetActive(false);
            cache.Push(source);
        }

        /// <summary>清空对象池。</summary>
        public void Clear()
        {
            for (int i = 0; i < allSources.Count; i++)
            {
                if (allSources[i] != null)
                {
                    Object.Destroy(allSources[i].gameObject);
                }
            }

            allSources.Clear();
            cache.Clear();
        }

        /// <summary>创建新的音源。</summary>
        private AudioSource CreateSource()
        {
            GameObject go = new GameObject("AudioSource");
            go.transform.SetParent(root, false);
            go.hideFlags = HideFlags.HideInHierarchy;

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            allSources.Add(source);
            return source;
        }

        /// <summary>读取是否还可以创建新音源。</summary>
        private bool CanCreateSource()
        {
            return maxSourceCount == 0 || allSources.Count < maxSourceCount;
        }

        /// <summary>重置音源，避免复用时继承上一次播放状态。</summary>
        private void ResetSource(AudioSource source)
        {
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.loop = false;
            source.volume = 1f;
            source.pitch = 1f;
            source.priority = 128;
            source.spatialBlend = 0f;
            source.minDistance = 1f;
            source.maxDistance = 500f;
            source.transform.localPosition = Vector3.zero;
            source.transform.localRotation = Quaternion.identity;
            source.transform.localScale = Vector3.one;
        }
    }
}
