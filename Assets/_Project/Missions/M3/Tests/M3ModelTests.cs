using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.M3;
using NUnit.Framework;

namespace LastSignal.M3.Tests
{
    public class SignalPathDataTests
    {
        const string Csv =
            "# comment\n" +
            "link,from,to,epoch_utc,distance_au,distance_km,one_way_light_time_min,source\n" +
            "ship_relay,Ship,Relay,2026-11-13,,299792.458,,JPL Horizons\n" +
            "relay_earth,Relay,Earth,2026-11-13,1,,,JPL Horizons\n";

        [Test]
        public void ParsesBothLinks()
        {
            SignalPathData d = SignalPathData.Parse(Csv, "test.csv");
            Assert.IsTrue(d.IsValid, string.Join(" ", d.errors.ToArray()));
            Assert.IsFalse(d.isSample);
        }

        [Test]
        public void ComputesLightTimeFromDistance()
        {
            SignalPathData d = SignalPathData.Parse(Csv);
            Assert.AreEqual(1.0, d.Get(SignalPathData.ShipRelay).oneWaySeconds, 1e-6);
            // 1 AU is about 499 light seconds.
            Assert.AreEqual(499.0, d.Get(SignalPathData.RelayEarth).oneWaySeconds, 0.1);
            Assert.AreEqual(500.0, d.ShipToEarthSeconds, 0.1);
        }

        [Test]
        public void LightTimeColumnWinsOverDistance()
        {
            string csv = "link,distance_km,one_way_light_time_min\nship_relay,1,2\nrelay_earth,1,3\n";
            SignalPathData d = SignalPathData.Parse(csv);
            Assert.AreEqual(120.0, d.Get(SignalPathData.ShipRelay).oneWaySeconds, 1e-6);
            Assert.AreEqual(180.0, d.Get(SignalPathData.RelayEarth).oneWaySeconds, 1e-6);
        }

        [Test]
        public void FlagsSampleData()
        {
            string csv = "link,distance_au,source\nship_relay,1,SAMPLE\nrelay_earth,1,x\n";
            Assert.IsTrue(SignalPathData.Parse(csv).isSample);
        }

        [Test]
        public void ReportsMissingLinks()
        {
            SignalPathData d = SignalPathData.Parse("link,distance_km\nship_relay,10\n");
            Assert.IsFalse(d.IsValid);
            Assert.IsNotEmpty(d.errors);
        }

        [Test]
        public void FormatsDelays()
        {
            Assert.AreEqual("3.2 s", SignalPathData.FormatDelay(3.2));
            Assert.AreEqual("12 m 30 s", SignalPathData.FormatDelay(750));
            Assert.AreEqual("8 h 12 m", SignalPathData.FormatDelay(8 * 3600 + 12 * 60));
        }

        [Test]
        public void HandlesQuotedCells()
        {
            List<string> cells = SignalPathData.SplitCsvLine("a,\"b, c\",\"d \"\"q\"\"\"");
            Assert.AreEqual(3, cells.Count);
            Assert.AreEqual("b, c", cells[1]);
            Assert.AreEqual("d \"q\"", cells[2]);
        }
    }

    public class TriageModelTests
    {
        [Test]
        public void ScannerIsRequired()
        {
            List<RepairOption> o = TriageModel.DefaultOptions();
            string reason;
            Assert.IsFalse(TriageModel.CanConfirm(o, 45, out reason));
            TriageModel.Toggle(o, 0, 45);
            Assert.IsTrue(TriageModel.CanConfirm(o, 45, out reason));
        }

        [Test]
        public void CannotGoOverBudget()
        {
            List<RepairOption> o = TriageModel.DefaultOptions();
            Assert.IsTrue(TriageModel.Toggle(o, 0, 45));   // scanner 20
            Assert.IsTrue(TriageModel.Toggle(o, 3, 45));   // comms 25 -> 45
            Assert.IsFalse(TriageModel.Toggle(o, 1, 45));  // engine 15 would be 60
            Assert.AreEqual(45, TriageModel.Used(o));
        }

        [Test]
        public void BestChoiceFitsTheDefaultBudget()
        {
            List<RepairOption> o = TriageModel.DefaultOptions();
            TriageModel.Toggle(o, 0, 45);
            TriageModel.Toggle(o, 1, 45);
            TriageModel.Toggle(o, 2, 45);
            TriageResult r = TriageModel.Result(o);
            Assert.IsTrue(r.scanner && r.engine && r.lifeSupport);
            Assert.AreEqual(45, r.powerUsed);
        }
    }

    public class RerouteModelTests
    {
        static Destination Get(string id)
        {
            foreach (Destination d in RerouteModel.Default()) if (d.id == id) return d;
            return null;
        }

        [Test]
        public void AfterTheftOnlyThePlanetIsReachable()
        {
            for (float before = 0f; before <= 100f; before += 5f)
            {
                float after = RerouteModel.SuppliesAfterTheft(before);
                float m = RerouteModel.CostMultiplier(true, true);
                Assert.IsFalse(RerouteModel.Reachable(Get(RerouteModel.Earth), after, m), "Earth at " + before);
                Assert.IsFalse(RerouteModel.Reachable(Get(RerouteModel.Outpost), after, m), "Outpost at " + before);
                if (before >= RerouteModel.MinSuppliesLeft)
                {
                    Assert.IsTrue(RerouteModel.Reachable(Get(RerouteModel.Planet), after, m), "Planet at " + before);
                }
            }
        }

        [Test]
        public void TheftTakesHalfWhenThereIsEnough()
        {
            Assert.AreEqual(45f, RerouteModel.SuppliesAfterTheft(90f), 0.001f);
            Assert.AreEqual(36f, RerouteModel.SuppliesAfterTheft(50f), 0.001f);
            Assert.AreEqual(20f, RerouteModel.SuppliesAfterTheft(20f), 0.001f);
        }

        [Test]
        public void SkippedRepairsCanMakeThePlanetUnreachable()
        {
            float m = RerouteModel.CostMultiplier(false, false);
            Assert.IsFalse(RerouteModel.Reachable(Get(RerouteModel.Planet), 45f, m));
        }

        [Test]
        public void UnreachableDestinationScoresZero()
        {
            PlanConfig plan = RerouteModel.PlanFor(Get(RerouteModel.Earth), 45f, true, true);
            Assert.AreEqual(0f, RiskModel.ComputeSuccess(plan), 0.0001f);
            PlanConfig good = RerouteModel.PlanFor(Get(RerouteModel.Planet), 45f, true, true);
            Assert.Greater(RiskModel.ComputeSuccess(good), 0.5f);
        }
    }
}
