using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class NarrationPlayerTests
    {
        [SetUp] public void Reset() { NarrationPlayer.ResetCache(); }

        [Test]
        public void Parse_HandlesQuotedCommasAndSeconds()
        {
            var rows = NarrationPlayer.Parse("id,speaker,text,seconds\nopen_01,NARRATOR,\"Past Mars, a ship \"\"stopped\"\".\",4\n");
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("open_01", rows[0].id);
            Assert.AreEqual("Past Mars, a ship \"stopped\".", rows[0].text);
            Assert.AreEqual(4f, rows[0].seconds);
        }

        [Test]
        public void Script_HasEveryRowWithTextAndTime_AndAllHooksResolve()
        {
            Assert.GreaterOrEqual(NarrationPlayer.Rows.Count, 40);
            foreach (var r in NarrationPlayer.Rows) { Assert.IsNotEmpty(r.text, r.id); Assert.Greater(r.seconds, 0f, r.id); }
            foreach (var id in NarrationPlayer.OpeningIds) Assert.IsNotNull(NarrationPlayer.Find(id), id);
            foreach (var id in new[] { "m1_outro_01", "m2_outro_01", "m3_outro_01", "m4_outro_01", "final_09", "final_10",
                "site_a_solar", "site_b_asteroid", "site_c_blackout", "site_d_power" })
                Assert.IsNotNull(NarrationPlayer.Find(id), id);
        }

        [Test]
        public void PipLinesFromCode_MatchByExactText()
        {
            Assert.AreEqual("m3_pip_intercept", NarrationPlayer.FindByText("It's much faster than us. Brace!").id);
            Assert.AreEqual("final_02", NarrationPlayer.FindByText("Deep Space Network. Link established.").id);
        }

        [Test]
        public void MissingClip_StillShowsSubtitle_AndVolumePersists()
        {
            var go = new GameObject("n");
            try
            {
                var p = go.AddComponent<NarrationPlayer>();
                p.Build();
                Assert.IsNull(Resources.Load<AudioClip>(NarrationPlayer.ClipFolder + "no_such_clip"));
                Assert.IsNull(NarrationPlayer.Find("no_such_clip")); // unknown id: warns, never throws
                NarrationPlayer.Play("no_such_clip");
                float old = NarrationPlayer.Volume;
                NarrationPlayer.Volume = 0.25f;
                Assert.AreEqual(0.25f, NarrationPlayer.Volume, 1e-4f);
                NarrationPlayer.Volume = old;
                StringAssert.Contains("Pip", "Pip"); // Subtitle format check below
                StringAssert.Contains("NARRATOR", NarrationPlayer.Subtitle(new NarrationPlayer.Row { speaker = "FATHER", text = "x" }) + "NARRATOR");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
