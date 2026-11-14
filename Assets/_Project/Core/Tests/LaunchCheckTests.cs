using NUnit.Framework;

namespace LastSignal.Tests
{
    public class LaunchCheckTests
    {
        static PlanConfig HeavyPlan(float tonnesPerSubsystem)
        {
            var p = new PlanConfig();
            foreach (var n in new[] { "Propulsion", "Power", "Life support", "Navigation", "Comms" })
                p.subsystems.Add(new Subsystem(n, 0.9f, 1, tonnesPerSubsystem));
            return p;
        }

        [Test]
        public void PartsLibrary_GivesNasaPayloads()
        {
            var parts = DataTable.Load("parts_library");
            var moon = LaunchCheck.VehiclesFor("moon", parts);
            var deep = LaunchCheck.VehiclesFor("mars", parts);
            Assert.AreEqual(1, moon.Count);
            Assert.AreEqual(27f, moon[0].payloadTonnes, 0.01f);   // SLS Block 1, parts_library.csv
            Assert.AreEqual(2, deep.Count);
            Assert.AreEqual(38f, deep[0].payloadTonnes, 0.01f);   // SLS Block 1B
            Assert.AreEqual(46f, deep[1].payloadTonnes, 0.01f);   // SLS Block 2
        }

        [Test]
        public void Check_BlocksWhenOverPayload_AndExplains()
        {
            var moon = LaunchCheck.VehiclesFor("moon", DataTable.Load("parts_library"))[0];
            var heavy = HeavyPlan(6f);          // 30 t > 27 t
            var r = LaunchCheck.Check(heavy, moon, "The Moon");
            Assert.IsFalse(r.ok);
            StringAssert.Contains("over", r.reason);
            StringAssert.Contains("27.0 t", r.reason);
            Assert.IsTrue(LaunchCheck.Check(HeavyPlan(5f), moon, "The Moon").ok); // 25 t fits
        }

        [Test]
        public void Check_DoesNotChangeRiskMath()
        {
            var plan = PlanConfig.SonsOriginalPlan();
            float before = RiskModel.ComputeSuccess(plan);
            LaunchCheck.Check(plan, null, "x"); LaunchCheck.PowerUsed(plan); LaunchCheck.PowerAvailable(plan);
            Assert.AreEqual(before, RiskModel.ComputeSuccess(plan), 1e-6f);
            Assert.AreEqual(0.63f * 0.63f * 0.63f * 0.63f * 0.63f, before, 1e-5f);
        }

        [Test]
        public void Power_ChangesWithRedundancy()
        {
            var p = PlanConfig.SonsOriginalPlan();
            float avail1 = LaunchCheck.PowerAvailable(p), used1 = LaunchCheck.PowerUsed(p);
            p.subsystems.Find(s => s.name == "Power").redundancy = 2;
            Assert.Greater(LaunchCheck.PowerAvailable(p), avail1);
            p.subsystems.Find(s => s.name == "Comms").redundancy = 3;
            Assert.Greater(LaunchCheck.PowerUsed(p), used1);
        }
    }
}
