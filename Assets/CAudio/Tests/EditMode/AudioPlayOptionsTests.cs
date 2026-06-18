using NUnit.Framework;
using UnityEngine;

namespace CAudio.Tests
{
    public sealed class AudioPlayOptionsTests
    {
        [Test]
        public void CloneCopiesAllValueFields()
        {
            AudioPlayOptions options = new AudioPlayOptions
            {
                Position = new Vector3(1f, 2f, 3f),
                Volume = 0.5f,
                Pitch = 1.25f,
                Delay = 0.2f,
                Loop = true,
                FadeIn = 0.1f,
                FadeOut = 0.3f,
                Channel = AudioChannel.Ui,
                ReplaceSameChannel = true,
                SpatialOverride = true,
                SpatialBlend = 0.8f,
                MinDistance = 2f,
                MaxDistance = 30f,
                Priority = 64,
                ApplyVoiceDucking = false
            };

            AudioPlayOptions clone = options.Clone();

            Assert.AreNotSame(options, clone);
            Assert.AreEqual(options.Position, clone.Position);
            Assert.AreEqual(options.Volume, clone.Volume);
            Assert.AreEqual(options.Pitch, clone.Pitch);
            Assert.AreEqual(options.Delay, clone.Delay);
            Assert.AreEqual(options.Loop, clone.Loop);
            Assert.AreEqual(options.FadeIn, clone.FadeIn);
            Assert.AreEqual(options.FadeOut, clone.FadeOut);
            Assert.AreEqual(options.Channel, clone.Channel);
            Assert.AreEqual(options.ReplaceSameChannel, clone.ReplaceSameChannel);
            Assert.AreEqual(options.SpatialOverride, clone.SpatialOverride);
            Assert.AreEqual(options.SpatialBlend, clone.SpatialBlend);
            Assert.AreEqual(options.MinDistance, clone.MinDistance);
            Assert.AreEqual(options.MaxDistance, clone.MaxDistance);
            Assert.AreEqual(options.Priority, clone.Priority);
            Assert.AreEqual(options.ApplyVoiceDucking, clone.ApplyVoiceDucking);
        }

        [Test]
        public void CopyOrDefaultCreatesNewInstanceWhenNull()
        {
            AudioPlayOptions options = AudioPlayOptions.CopyOrDefault(null);

            Assert.NotNull(options);
            Assert.AreEqual(1f, options.Volume);
            Assert.AreEqual(1f, options.Pitch);
            Assert.AreEqual(1f, options.SpatialBlend);
            Assert.IsTrue(options.ApplyVoiceDucking);
        }

        [Test]
        public void CopyOrDefaultDoesNotReturnOriginalInstance()
        {
            AudioPlayOptions original = new AudioPlayOptions
            {
                Channel = AudioChannel.Music,
                ReplaceSameChannel = true
            };

            AudioPlayOptions copy = AudioPlayOptions.CopyOrDefault(original);
            copy.Channel = AudioChannel.Sfx;
            copy.ReplaceSameChannel = false;

            Assert.AreEqual(AudioChannel.Music, original.Channel);
            Assert.IsTrue(original.ReplaceSameChannel);
        }
    }
}
