using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace CAudio.Tests
{
    public sealed class AudioServiceTests
    {
        private GameObject root;
        private AudioDatabase database;
        private AudioService service;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("AudioServiceTests");
            database = ScriptableObject.CreateInstance<AudioDatabase>();
            service = new AudioService(root.transform, database, new DirectAudioClipProvider());
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(database);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void TryPlayReturnsMissingDatabaseWhenDatabaseIsNull()
        {
            AudioService nullDatabaseService = new AudioService(root.transform, null, new DirectAudioClipProvider());

            LogAssert.Expect(LogType.Warning, "[CAudio] 音频数据库未设置。");
            AudioPlayResult result = nullDatabaseService.TryPlay("any_key");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(AudioPlayFailureReason.MissingDatabase, result.FailureReason);
        }

        [Test]
        public void TryPlayReturnsEmptyKeyWhenKeyIsBlank()
        {
            LogAssert.Expect(LogType.Warning, "[CAudio] 播放Key为空。");
            AudioPlayResult result = service.TryPlay(" ");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(AudioPlayFailureReason.EmptyKey, result.FailureReason);
        }

        [Test]
        public void TryPlayReturnsCueNotFoundWhenKeyDoesNotExist()
        {
            LogAssert.Expect(LogType.Warning, "[CAudio] 找不到音频Key：missing。");
            AudioPlayResult result = service.TryPlay("missing");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(AudioPlayFailureReason.CueNotFound, result.FailureReason);
        }

        [Test]
        public void TryPlayReturnsProviderFailedWhenReferenceCannotResolve()
        {
            AudioCueData cue = new AudioCueData();
            cue.SetIdentity("address_only", "Address Only");
            cue.AddClip(new AudioClipReference("missing/address"), 1f);
            database.AddCue(cue);

            LogAssert.Expect(LogType.Warning, "[CAudio] 音频配置 address_only 无法解析Clip。");
            AudioPlayResult result = service.TryPlay("address_only");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(AudioPlayFailureReason.ProviderFailed, result.FailureReason);
        }

        [Test]
        public void VolumeToDecibelConvertsExpectedValues()
        {
            Assert.AreEqual(0f, AudioService.VolumeToDecibel(1f), 0.0001f);
            Assert.AreEqual(-6.0206f, AudioService.VolumeToDecibel(0.5f), 0.0001f);
            Assert.AreEqual(-80f, AudioService.VolumeToDecibel(0f), 0.0001f);
        }

        [Test]
        public void TryPlayReturnsCooldownWhenCueIsStillCoolingDown()
        {
            AudioClip clip = CreateClip("cooldown_clip");
            try
            {
                AudioCueData cue = CreateCueWithClip("cooldown", clip);
                cue.SetCooldown(1f);
                database.AddCue(cue);

                Assert.IsTrue(service.TryPlay("cooldown").Success);

                LogAssert.Expect(LogType.Warning, "[CAudio] 音频配置 cooldown 仍在冷却中。");
                AudioPlayResult result = service.TryPlay("cooldown");

                Assert.IsFalse(result.Success);
                Assert.AreEqual(AudioPlayFailureReason.Cooldown, result.FailureReason);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void TryPlayReturnsMaxSimultaneousWhenActiveHandleLimitIsReached()
        {
            AudioClip clip = CreateClip("limit_clip");
            try
            {
                AudioCueData cue = CreateCueWithClip("limited", clip);
                cue.SetMaxSimultaneous(1);
                database.AddCue(cue);

                Assert.IsTrue(service.TryPlay("limited", new AudioPlayOptions { Loop = true }).Success);

                LogAssert.Expect(LogType.Warning, "[CAudio] 音频配置 limited 已达到最大同时播放数。");
                AudioPlayResult result = service.TryPlay("limited");

                Assert.IsFalse(result.Success);
                Assert.AreEqual(AudioPlayFailureReason.MaxSimultaneous, result.FailureReason);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void PlayAsyncCanBeCancelledBeforeProviderCompletes()
        {
            DelayedClipProvider provider = new DelayedClipProvider();
            AudioService asyncService = new AudioService(root.transform, database, provider);
            AudioCueData cue = new AudioCueData();
            cue.SetIdentity("async_cancel", "Async Cancel");
            cue.AddClip(new AudioClipReference("async/cancel"));
            database.AddCue(cue);

            AudioAsyncPlayRequest request = asyncService.PlayAsync("async_cancel");
            request.Cancel();
            provider.Complete(CreateClip("late_clip"));

            Assert.IsTrue(request.IsDone);
            Assert.IsTrue(request.IsCancelled);
            Assert.AreEqual(AudioPlayFailureReason.Cancelled, request.Result.FailureReason);
            UnityEngine.Object.DestroyImmediate(provider.LastClip);
        }

        [Test]
        public void PlayAsyncCompletesSuccessfullyWithDirectProvider()
        {
            AudioClip clip = CreateClip("async_success_clip");
            try
            {
                AudioCueData cue = CreateCueWithClip("async_success", clip);
                database.AddCue(cue);

                AudioAsyncPlayRequest request = service.PlayAsync("async_success");

                Assert.IsTrue(request.IsDone);
                Assert.IsTrue(request.Result.Success);
                Assert.NotNull(request.Result.Handle);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void StopGroupStopsOnlyMatchingGroupHandles()
        {
            AudioClip firstClip = CreateClip("group_first");
            AudioClip secondClip = CreateClip("group_second");
            try
            {
                AudioCueData first = CreateCueWithClip("group_a", firstClip);
                first.SetGroup("combat");
                AudioCueData second = CreateCueWithClip("group_b", secondClip);
                second.SetGroup("ui");
                database.AddCue(first);
                database.AddCue(second);

                AudioPlaybackHandle combatHandle = service.Play("group_a", new AudioPlayOptions { Loop = true });
                AudioPlaybackHandle uiHandle = service.Play("group_b", new AudioPlayOptions { Loop = true });

                service.StopGroup("combat");

                Assert.AreEqual("combat", combatHandle.PlaybackGroup);
                Assert.IsTrue(combatHandle.IsStopped);
                Assert.IsFalse(uiHandle.IsStopped);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstClip);
                UnityEngine.Object.DestroyImmediate(secondClip);
            }
        }

        [Test]
        public void PauseDoesNotFinishNonLoopingPlayback()
        {
            AudioClip clip = CreateClip("pause_clip");
            try
            {
                AudioPlaybackHandle handle = service.Play(clip);

                handle.Pause();
                service.Tick(1f);

                Assert.IsTrue(handle.Paused);
                Assert.IsFalse(handle.IsStopped);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void MusicQueueSkipsFailedEntriesAndContinues()
        {
            AudioClip currentClip = CreateClip("queue_current");
            AudioClip nextClip = CreateClip("queue_next");
            try
            {
                AudioCueData current = CreateCueWithClip("queue_current", currentClip);
                AudioCueData next = CreateCueWithClip("queue_next", nextClip);
                next.SetMaxSimultaneous(1);
                database.AddCue(current);
                database.AddCue(next);

                AudioPlaybackHandle currentHandle = service.PlayMusic("queue_current", new AudioPlayOptions { Loop = true });
                service.QueueMusic("queue_missing");
                service.QueueMusic("queue_next", 0f, new AudioPlayOptions { Loop = true });

                LogAssert.Expect(LogType.Warning, "[CAudio] 找不到音频Key：queue_missing。");
                currentHandle.Stop();
                service.Tick(0.1f);

                LogAssert.Expect(LogType.Warning, "[CAudio] 音频配置 queue_next 已达到最大同时播放数。");
                AudioPlayResult result = service.TryPlay("queue_next");
                Assert.IsFalse(result.Success);
                Assert.AreEqual(AudioPlayFailureReason.MaxSimultaneous, result.FailureReason);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(currentClip);
                UnityEngine.Object.DestroyImmediate(nextClip);
            }
        }

        [Test]
        public void TryPlayReturnsPoolLimitReachedWhenPoolIsFull()
        {
            SetPoolSettings(database, 1, 1);
            service = new AudioService(root.transform, database, new DirectAudioClipProvider());
            AudioClip first = CreateClip("pool_first");
            AudioClip second = CreateClip("pool_second");
            try
            {
                Assert.IsTrue(service.TryPlay(first, new AudioPlayOptions { Loop = true }).Success);

                LogAssert.Expect(LogType.Warning, "[CAudio] 音源池已达到最大数量。");
                AudioPlayResult result = service.TryPlay(second, new AudioPlayOptions { Loop = true });

                Assert.IsFalse(result.Success);
                Assert.AreEqual(AudioPlayFailureReason.PoolLimitReached, result.FailureReason);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void CancelledAsyncRequestReleasesLoadedClipWhenProviderSupportsRelease()
        {
            ReleasingDelayedClipProvider provider = new ReleasingDelayedClipProvider();
            AudioService asyncService = new AudioService(root.transform, database, provider);
            AudioCueData cue = new AudioCueData();
            cue.SetIdentity("async_release", "Async Release");
            cue.AddClip(new AudioClipReference("async/release"));
            database.AddCue(cue);
            AudioClip clip = CreateClip("released_late_clip");
            try
            {
                AudioAsyncPlayRequest request = asyncService.PlayAsync("async_release");
                request.Cancel();
                provider.Complete(clip);

                Assert.IsTrue(request.IsCancelled);
                Assert.AreEqual(1, provider.ReleaseCount);
                Assert.AreSame(clip, provider.ReleasedClip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void StopFadeUsesCurrentAudibleVolumeWithoutDoubleApplyingChannelVolume()
        {
            AudioClip clip = CreateClip("fade_clip");
            try
            {
                AudioPlaybackHandle handle = service.Play(clip, new AudioPlayOptions { Loop = true, Channel = AudioChannel.Sfx });
                service.SetMasterVolume(0.5f);
                service.SetChannelVolume(AudioChannel.Sfx, 0.5f);
                service.Tick(0.1f);

                Assert.AreEqual(0.25f, handle.CurrentVolume, 0.0001f);

                handle.Stop(1f);
                service.Tick(0.5f);

                Assert.AreEqual(0.125f, handle.CurrentVolume, 0.0001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        private static AudioCueData CreateCueWithClip(string key, AudioClip clip)
        {
            AudioCueData cue = new AudioCueData();
            cue.SetIdentity(key, key);
            cue.AddClip(clip);
            return cue;
        }

        private static AudioClip CreateClip(string name)
        {
            return AudioClip.Create(name, 8, 1, 44100, false);
        }

        private static void SetPoolSettings(AudioDatabase database, int prewarmCount, int maxSourceCount)
        {
            AudioPoolSettings settings = database.PoolSettings;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(AudioPoolSettings).GetField("prewarmCount", flags).SetValue(settings, prewarmCount);
            typeof(AudioPoolSettings).GetField("maxSourceCount", flags).SetValue(settings, maxSourceCount);
        }

        private sealed class DelayedClipProvider : IAudioClipProvider
        {
            private Action<AudioClip> onSuccess;

            public AudioClip LastClip { get; private set; }

            public bool TryResolveClip(AudioClipReference reference, out AudioClip clip)
            {
                clip = null;
                return false;
            }

            public void LoadClipAsync(AudioClipReference reference, Action<AudioClip> onSuccess, Action<string> onFailure)
            {
                this.onSuccess = onSuccess;
            }

            public void Complete(AudioClip clip)
            {
                LastClip = clip;
                onSuccess?.Invoke(clip);
            }
        }

        private sealed class ReleasingDelayedClipProvider : IAudioClipProvider, IAudioClipReleaseProvider
        {
            private Action<AudioClip> onSuccess;

            public int ReleaseCount { get; private set; }
            public AudioClip ReleasedClip { get; private set; }

            public bool TryResolveClip(AudioClipReference reference, out AudioClip clip)
            {
                clip = null;
                return false;
            }

            public void LoadClipAsync(AudioClipReference reference, Action<AudioClip> onSuccess, Action<string> onFailure)
            {
                this.onSuccess = onSuccess;
            }

            public void ReleaseClip(AudioClipReference reference, AudioClip clip)
            {
                ReleaseCount++;
                ReleasedClip = clip;
            }

            public void Complete(AudioClip clip)
            {
                onSuccess?.Invoke(clip);
            }
        }
    }
}
