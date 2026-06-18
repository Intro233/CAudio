using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CAudio.Tests
{
    public sealed class AudioDatabaseTests
    {
        [Test]
        public void TryGetCueFindsKeysCaseInsensitively()
        {
            AudioDatabase database = ScriptableObject.CreateInstance<AudioDatabase>();
            try
            {
                AudioCueData cue = CreateCue("Ui_Click");
                database.AddCue(cue);

                Assert.IsTrue(database.TryGetCue("ui_click", out AudioCueData found));
                Assert.AreSame(cue, found);
            }
            finally
            {
                Object.DestroyImmediate(database);
            }
        }

        [Test]
        public void RebuildCacheKeepsFirstCueWhenDuplicateKeysExist()
        {
            AudioDatabase database = ScriptableObject.CreateInstance<AudioDatabase>();
            try
            {
                AudioCueData first = CreateCue("shared_key");
                AudioCueData second = CreateCue("SHARED_KEY");
                database.AddCue(first);
                database.AddCue(second);

                Assert.IsTrue(database.TryGetCue("shared_key", out AudioCueData found));
                Assert.AreSame(first, found);
            }
            finally
            {
                Object.DestroyImmediate(database);
            }
        }

        [Test]
        public void ValidateReportsDuplicateKeysAndMissingClips()
        {
            AudioDatabase database = ScriptableObject.CreateInstance<AudioDatabase>();
            try
            {
                database.AddCue(CreateCue("missing_clip"));
                database.AddCue(CreateCue("missing_clip"));

                List<AudioDatabaseValidationIssue> issues = database.Validate();

                Assert.That(issues, Has.Some.Matches<AudioDatabaseValidationIssue>(issue => issue.Message.Contains("Key重复")));
                Assert.That(issues, Has.Some.Matches<AudioDatabaseValidationIssue>(issue => issue.Message.Contains("没有配置Clip")));
            }
            finally
            {
                Object.DestroyImmediate(database);
            }
        }

        [Test]
        public void ValidateReportsClipOptionsWithoutResolvableReferences()
        {
            AudioDatabase database = ScriptableObject.CreateInstance<AudioDatabase>();
            try
            {
                AudioCueData cue = CreateCue("empty_reference");
                cue.AddClip(new AudioClipReference(), 1f);
                database.AddCue(cue);

                List<AudioDatabaseValidationIssue> issues = database.Validate();

                Assert.That(issues, Has.Some.Matches<AudioDatabaseValidationIssue>(issue => issue.Message.Contains("没有可解析的Clip引用")));
            }
            finally
            {
                Object.DestroyImmediate(database);
            }
        }

        private static AudioCueData CreateCue(string key)
        {
            AudioCueData cue = new AudioCueData();
            cue.SetIdentity(key, key);
            return cue;
        }
    }
}
