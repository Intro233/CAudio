using NUnit.Framework;
using UnityEngine;

namespace CAudio.Tests
{
    public sealed class AudioCueDataTests
    {
        [Test]
        public void PickClipReturnsNullWhenNoClipsExist()
        {
            AudioCueData cue = new AudioCueData();

            Assert.IsFalse(cue.HasClips());
            Assert.IsNull(cue.PickClip());
        }

        [Test]
        public void AddClipCreatesPlayableClipOption()
        {
            AudioClip clip = CreateClip("test_clip");
            try
            {
                AudioCueData cue = new AudioCueData();
                cue.AddClip(clip, 2f);

                AudioClipOption selected = cue.PickClip();

                Assert.IsTrue(cue.HasClips());
                Assert.NotNull(selected);
                Assert.AreSame(clip, selected.Clip.DirectClip);
                Assert.AreEqual(2f, selected.Weight);
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void WeightedSelectionReturnsOnlyConfiguredOptionWhenThereIsOneOption()
        {
            AudioClip clip = CreateClip("weighted_clip");
            try
            {
                AudioCueData cue = new AudioCueData();
                cue.SetSelectionMode(AudioSelectionMode.WeightedRandom);
                cue.AddClip(clip, 10f);

                Assert.AreSame(clip, cue.PickClip().Clip.DirectClip);
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void FixedVolumeAndPitchRangesReturnFixedValues()
        {
            AudioCueData cue = new AudioCueData();
            cue.SetVolumeRange(0.6f, 0.6f);
            cue.SetPitchRange(1.2f, 1.2f);

            Assert.AreEqual(0.6f, cue.GetVolumeMultiplier());
            Assert.AreEqual(1.2f, cue.GetPitchMultiplier());
        }

        [Test]
        public void SequentialSelectionAdvancesInOrder()
        {
            AudioClip first = CreateClip("first");
            AudioClip second = CreateClip("second");
            try
            {
                AudioCueData cue = new AudioCueData();
                cue.SetSelectionMode(AudioSelectionMode.Sequential);
                cue.AddClip(first);
                cue.AddClip(second);

                Assert.AreSame(first, cue.PickClip().Clip.DirectClip);
                Assert.AreSame(second, cue.PickClip().Clip.DirectClip);
                Assert.AreSame(first, cue.PickClip().Clip.DirectClip);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void NoImmediateRepeatAvoidsPreviousClip()
        {
            AudioClip first = CreateClip("first");
            AudioClip second = CreateClip("second");
            try
            {
                AudioCueData cue = new AudioCueData();
                cue.SetSelectionMode(AudioSelectionMode.NoImmediateRepeat);
                cue.AddClip(first);
                cue.AddClip(second);

                AudioClip previous = cue.PickClip().Clip.DirectClip;
                for (int i = 0; i < 8; i++)
                {
                    AudioClip next = cue.PickClip().Clip.DirectClip;
                    Assert.AreNotSame(previous, next);
                    previous = next;
                }
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        private static AudioClip CreateClip(string name)
        {
            return AudioClip.Create(name, 8, 1, 44100, false);
        }
    }
}
