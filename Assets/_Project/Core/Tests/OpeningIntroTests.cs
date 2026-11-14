using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class OpeningIntroTests
    {
        [Test]
        public void Opening_TimingFromScript_Is30To45Seconds()
        {
            NarrationPlayer.ResetCache();
            float[] s = OpeningIntro.LineStarts();
            Assert.AreEqual(NarrationPlayer.OpeningIds.Length + 1, s.Length);
            float total = s[s.Length - 1];
            Assert.GreaterOrEqual(total, 30f, "opening shorter than 30 s");
            Assert.LessOrEqual(total, 45f, "opening longer than 45 s");
            for (int i = 1; i < s.Length; i++) Assert.Greater(s[i], s[i - 1]);
        }

        [Test]
        public void SkipIntroSetting_RoundTrips()
        {
            bool before = OpeningIntro.SkipSetting;
            try
            {
                OpeningIntro.SkipSetting = true;
                Assert.IsTrue(OpeningIntro.SkipSetting);
                OpeningIntro.SkipSetting = false;
                Assert.IsFalse(OpeningIntro.SkipSetting);
            }
            finally { OpeningIntro.SkipSetting = before; }
        }

        [Test]
        public void Intro_NeverPlaysInBatchMode()
        {
            if (!Application.isBatchMode) Assert.Ignore("only meaningful in a batch-mode test run");
            Assert.IsFalse(OpeningIntro.ShouldPlay());
        }
    }
}
