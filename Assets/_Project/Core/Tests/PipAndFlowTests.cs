using NUnit.Framework;

namespace LastSignal.Core.Tests
{
    public class PipAndFlowTests
    {
        [Test]
        public void EveryMissionHasAPipHint()
        {
            string[] ids = { MissionIds.M1, MissionIds.M2, MissionIds.M3, MissionIds.M4, MissionIds.Final, MissionIds.DesignMode };
            for (int i = 0; i < ids.Length; i++)
            {
                StringAssert.DoesNotContain("do not have a line", Pip.Lookup("hint." + ids[i]));
            }
        }

        [Test]
        public void PipSaySendsLineToListeners()
        {
            string heard = null;
            System.Action<string> listener = delegate(string s) { heard = s; };
            Pip.Spoke += listener;
            Pip.Say("hello");
            Pip.Spoke -= listener;
            Assert.AreEqual("hello", heard);
        }

        [Test]
        public void FlowOrderIsBootToDesignMode()
        {
            string id = MissionIds.Boot;
            string[] expected = { MissionIds.M1, MissionIds.M2, MissionIds.M3, MissionIds.M4, MissionIds.Final, MissionIds.DesignMode };
            for (int i = 0; i < expected.Length; i++)
            {
                id = MissionIds.Next(id);
                Assert.AreEqual(expected[i], id);
            }
        }

        [Test]
        public void FatherLogAddsAndClears()
        {
            FatherLog.Clear();
            FatherLog.Add("test");
            Assert.AreEqual(1, FatherLog.Entries.Count);
            FatherLog.Clear();
            Assert.AreEqual(0, FatherLog.Entries.Count);
        }
    }
}
