using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.M3.Tests
{
    /// <summary>Trigger radius, cost application (PlayerState + RiskModel) and seeded selection of the four challenge sites.</summary>
    public class ChallengeSiteTests
    {
        static PlayerState NewState(float v = 100f, float planned = 0.5f)
        {
            PlayerState s = ScriptableObject.CreateInstance<PlayerState>();
            s.supplies = s.hull = s.power = s.fuel = v;
            s.plannedSuccess = planned;
            return s;
        }

        static ChallengeScenario Scenario(ChallengeKind k, int seed) { return ChallengeModel.LoadRun(seed).First(x => x.kind == k); }

        // ------------------------------------------------------------ trigger

        [Test]
        public void Trigger_FiresOnceInsideRadius_NotOutside()
        {
            GameObject go = new GameObject("site");
            try
            {
                go.transform.position = new Vector3(100f, 0f, 0f);
                ChallengeSiteComponent c = go.AddComponent<ChallengeSiteComponent>();
                c.radius = 50f;
                c.scenario = Scenario(ChallengeKind.Asteroid, 1);
                int entered = 0;
                c.Entered += _ => entered++;

                Assert.IsFalse(c.Tick(new Vector3(0f, 0f, 0f)), "outside the radius");
                Assert.IsFalse(c.Triggered);
                Assert.IsTrue(c.Tick(new Vector3(60f, 0f, 0f)), "inside the radius");
                Assert.IsFalse(c.Tick(new Vector3(100f, 0f, 0f)), "must not trigger twice");
                Assert.AreEqual(1, entered);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void InRadius_BoundaryIsInclusive()
        {
            Assert.IsTrue(ChallengeSiteComponent.InRadius(new Vector3(50f, 0f, 0f), Vector3.zero, 50f));
            Assert.IsFalse(ChallengeSiteComponent.InRadius(new Vector3(50.1f, 0f, 0f), Vector3.zero, 50f));
        }

        // ------------------------------------------------------------ cost application

        [Test]
        public void Apply_ChangesPlayerStateByTheOptionCosts()
        {
            ChallengeScenario sc = Scenario(ChallengeKind.SolarStorm, 3);
            PlayerState st = NewState();
            ChallengeOption shield = sc.options[0];
            ChallengeModel.Apply(shield, st);
            Assert.AreEqual(100f + shield.dPower, st.power, 1e-4f);
            Assert.Less(st.power, 100f, "shield must cost power");
            Assert.AreEqual(100f, st.hull, 1e-4f);
            Assert.AreEqual(0.5f, st.plannedSuccess, 1e-6f, "shielding has no RiskModel penalty");
        }

        [Test]
        public void Apply_RiskyOption_LowersPlannedSuccessThroughRiskModel()
        {
            ChallengeScenario sc = Scenario(ChallengeKind.Asteroid, 3);
            ChallengeOption risky = sc.options.First(o => o.reliabilityHit > 0f);
            PlayerState st = NewState(100f, 0.4f);
            float ratio = ChallengeModel.RiskRatio(risky.subsystem, risky.reliabilityHit);
            Assert.Less(ratio, 1f);
            ChallengeResult r = ChallengeModel.Apply(risky, st);
            Assert.AreEqual(0.4f * ratio, st.plannedSuccess, 1e-5f);
            Assert.Less(r.successAfter, r.successBefore);
            Assert.AreEqual(100f + risky.dHull, st.hull, 1e-4f);

            // The ratio equals RiskModel's own before/after on the starter plan.
            PlanConfig a = PlanConfig.SonsOriginalPlan(), b = PlanConfig.SonsOriginalPlan();
            b.subsystems.First(x => x.name == risky.subsystem).reliability -= risky.reliabilityHit;
            Assert.AreEqual(RiskModel.ComputeSuccess(b) / RiskModel.ComputeSuccess(a), ratio, 1e-5f);
        }

        [Test]
        public void Apply_NeverKillsHull_OrGoesNegative()
        {
            foreach (ChallengeKind k in System.Enum.GetValues(typeof(ChallengeKind)))
                foreach (ChallengeOption o in Scenario(k, 5).options)
                {
                    PlayerState st = NewState(2f, 0.3f);
                    ChallengeModel.Apply(o, st);
                    Assert.GreaterOrEqual(st.hull, 1f, k + " / " + o.label);
                    Assert.GreaterOrEqual(st.power, 0f);
                    Assert.GreaterOrEqual(st.fuel, 0f);
                    Assert.GreaterOrEqual(st.supplies, 0f);
                }
        }

        [Test]
        public void Choose_AppliesOnlyOnce()
        {
            GameObject go = new GameObject("site");
            try
            {
                ChallengeSiteComponent c = go.AddComponent<ChallengeSiteComponent>();
                c.scenario = Scenario(ChallengeKind.Blackout, 2);
                PlayerState st = NewState();
                Assert.IsNotNull(c.Choose(2, st).summary);
                float hull = st.hull;
                Assert.IsNull(c.Choose(2, st).summary, "second choice must be ignored");
                Assert.AreEqual(hull, st.hull);
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ------------------------------------------------------------ passable, never soft-locks

        [Test]
        public void EveryChallenge_IsPassable_EvenWithEmptyTanks()
        {
            for (int seed = 0; seed < 200; seed++)
                foreach (ChallengeScenario sc in ChallengeModel.LoadRun(seed))
                {
                    PlayerState empty = NewState(0f, 0.2f);
                    empty.hull = 1f;
                    Assert.IsTrue(ChallengeModel.IsPassable(sc, empty), sc.kind + " seed " + seed);
                    ChallengeOption free = sc.options.First(o => ChallengeModel.CanAfford(o, empty));
                    ChallengeModel.Apply(free, empty);
                    Assert.GreaterOrEqual(empty.hull, 1f);
                }
        }

        // ------------------------------------------------------------ NASA backing

        [Test]
        public void EveryScenario_HasDatasetSourceWhyRealValuesAndBalanceTags()
        {
            foreach (ChallengeScenario sc in ChallengeModel.LoadRun(11))
            {
                Assert.GreaterOrEqual(sc.dataRow, 0, sc.kind + " CSV row missing");
                Assert.IsFalse(string.IsNullOrEmpty(sc.dataset), sc.kind.ToString());
                StringAssert.Contains("https://", sc.sourceUrl, sc.kind.ToString());
                Assert.IsFalse(string.IsNullOrEmpty(sc.why), sc.kind.ToString());
                Assert.IsFalse(string.IsNullOrEmpty(sc.retrieved), sc.kind.ToString());
                Assert.GreaterOrEqual(sc.facts.Count, 3, sc.kind.ToString());
                foreach (var f in sc.facts) Assert.IsFalse(string.IsNullOrWhiteSpace(f.Value), sc.kind + ": " + f.Key);
                Assert.AreEqual(3, sc.options.Count, sc.kind.ToString());
                foreach (ChallengeOption o in sc.options)
                {
                    StringAssert.Contains("[game balance]", o.costNote, sc.kind + " / " + o.label);
                    Assert.IsFalse(string.IsNullOrEmpty(o.debrief));
                }
                string card = ChallengePanel.CardText(sc);
                StringAssert.Contains(sc.sourceUrl, card);
                StringAssert.Contains("Why NASA cares", card);
            }
        }

        // ------------------------------------------------------------ seeded selection

        [Test]
        public void SameSeed_SameOrderAndSameCsvRows()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var a = ChallengeModel.LoadRun(seed);
                var b = ChallengeModel.LoadRun(seed);
                CollectionAssert.AreEqual(a.Select(x => x.kind).ToList(), b.Select(x => x.kind).ToList());
                CollectionAssert.AreEqual(a.Select(x => x.dataRow).ToList(), b.Select(x => x.dataRow).ToList());
                CollectionAssert.AreEqual(a.Select(x => x.title).ToList(), b.Select(x => x.title).ToList());
            }
        }

        [Test]
        public void Order_IsAPermutationOfFourKinds_AndVariesAcrossSeeds()
        {
            HashSet<string> orders = new HashSet<string>();
            for (int seed = 0; seed < 60; seed++)
            {
                List<ChallengeKind> o = ChallengeModel.ShuffledOrder(seed);
                Assert.AreEqual(4, o.Distinct().Count());
                orders.Add(string.Join(",", o.Select(x => x.ToString()).ToArray()));
            }
            Assert.GreaterOrEqual(orders.Count, 6, "different seeds must give different orders");
        }

        [Test]
        public void CsvRowChoice_VariesAcrossSeeds_AndStaysInRange()
        {
            CsvTable donki = CsvTable.Load(ChallengeModel.DonkiCsv);
            HashSet<int> rows = new HashSet<int>();
            for (int seed = 0; seed < 60; seed++)
            {
                int r = ChallengeModel.PickRow(ChallengeKind.SolarStorm, donki, seed);
                Assert.That(r, Is.InRange(0, donki.rows.Count - 1));
                rows.Add(r);
            }
            Assert.GreaterOrEqual(rows.Count, 3, "different seeds must use different DONKI events");

            CsvTable hz = CsvTable.Load(ChallengeModel.HorizonsCsv);
            for (int seed = 0; seed < 60; seed++)
                Assert.AreNotEqual("Earth", hz.rows[ChallengeModel.PickRow(ChallengeKind.Blackout, hz, seed)][0], "Earth has zero light-time and must not be picked");
        }

        [Test]
        public void StormSeverity_ComesFromTheCsvValue()
        {
            Assert.Greater(ChallengeModel.StormSeverity(true, "class M6.7", 0), ChallengeModel.StormSeverity(true, "class M3.5", 0));
            Assert.Greater(ChallengeModel.StormSeverity(false, "type O", 1048), ChallengeModel.StormSeverity(false, "type C", 973));
        }

        [Test]
        public void SiteSlots_AreInsideTheSector()
        {
            FreeFlight f = new GameObject("ff").AddComponent<FreeFlight>();
            try { foreach (Vector3 s in ChallengeSiteSet.Slots) Assert.Less(s.magnitude, f.softRadius); }
            finally { Object.DestroyImmediate(f.gameObject); }
        }
    }
}
