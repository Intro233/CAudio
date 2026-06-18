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

        /// <summary>创建对象池。</summary>
        public AudioSourcePool(Transform root)
        {
            this.root = root;
        }

        /// <summary>预热音源池。</summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                AudioSource source = CreateSource();
                Release(source);
            }
        }

        /// <summary>获取一个音源。</summary>
        public AudioSource Rent()
        {
            if (cache.Count > 0)
            {
                AudioSource source = cache.Pop();
                if (source != null)
                {
                    source.gameObject.SetActive(true);
                    return source;
                }
            }

            return CreateSource();
        }

        /// <summary>归还一个音源。</summary>
        public void Release(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
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
    }
}
