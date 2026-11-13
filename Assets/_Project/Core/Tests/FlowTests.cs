using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class FlowTests
    {
        [Test]
        public void MissionOrder_IsBootToDesignMode()
        {
            Assert.AreEqual(MissionIds.M1, MissionIds.Next(MissionIds.Boot));
            Assert.AreEqual(MissionIds.M2, MissionIds.Next(MissionIds.M1));
            Assert.AreEqual(MissionIds.M3, MissionIds.Next(MissionIds.M2));
            Assert.AreEqual(MissionIds.M4, MissionIds.Next(MissionIds.M3));
            Assert.AreEqual(MissionIds.Final, MissionIds.Next(MissionIds.M4));
            Assert.AreEqual(MissionIds.DesignMode, MissionIds.Next(MissionIds.Final));
            Assert.IsNull(MissionIds.Next(MissionIds.DesignMode));
        }

        [Test]
        public void PlayerState_CaptureRestore_RoundTrips()
        {
            var s = ScriptableObject.CreateInstance<PlayerState>();
            s.hull = 42f; s.signalPathUp = true; s.heading = new Vector3(1, 2, 3);
            var snap = s.Capture();
            s.ResetToDefaults();
            Assert.AreEqual(100f, s.hull);
            s.Restore(snap);
            Assert.AreEqual(42f, s.hull);
            Assert.IsTrue(s.signalPathUp);
            Assert.AreEqual(new Vector3(1, 2, 3), s.heading);
        }

        [Test]
        public void MissionEvents_DeliverComplete_AndFail()
        {
            MissionEvents.ClearAll();
            string done = null, failed = null, why = null;
            MissionEvents.OnMissionComplete += id => done = id;
            MissionEvents.OnMissionFail += (id, r) => { failed = id; why = r; };
            MissionEvents.RaiseComplete("M1");
            MissionEvents.RaiseFail("M2", "boom");
            Assert.AreEqual("M1", done);
            Assert.AreEqual("M2", failed);
            Assert.AreEqual("boom", why);
            MissionEvents.ClearAll();
        }

        [Test]
        public void TotalMass_CountsRedundancy()
        {
            var p = new PlanConfig();
            p.subsystems.Add(new Subsystem("A", 0.9f, 2, 3f));
            Assert.AreEqual(6f, p.TotalMass(), 1e-5f);
        }
    }
}
