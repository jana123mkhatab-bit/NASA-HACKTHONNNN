using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Core.Tests
{
    public class RiskModelTests
    {
        const float Tol = 0.001f;

        [Test]
        public void SonOriginalPlan_IsAboutTenPercent()
        {
            // 0.63 ^ 5 = 0.0992
            Assert.AreEqual(0.0992f, RiskModel.ComputeSuccess(PlanConfig.SonOriginal()), Tol);
        }

        [Test]
        public void FatherPlan_IsAboutSeventyOnePercent()
        {
            // 0.9^3 * 0.99^2 = 0.7145
            Assert.AreEqual(0.7145f, RiskModel.ComputeSuccess(PlanConfig.FatherDefault()), Tol);
        }

        [Test]
        public void ZeroRedundancy_GivesZero()
        {
            PlanConfig plan = PlanConfig.FatherDefault();
            plan.subsystems[0].redundancy = 0;
            Assert.AreEqual(0f, RiskModel.ComputeSuccess(plan), Tol);
        }

        [Test]
        public void EmptyPlan_GivesZero()
        {
            Assert.AreEqual(0f, RiskModel.ComputeSuccess(new PlanConfig()), Tol);
        }

        [Test]
        public void PerfectReliability_GivesOne()
        {
            PlanConfig plan = PlanConfig.FatherDefault();
            for (int i = 0; i < plan.subsystems.Count; i++) plan.subsystems[i].reliability = 1f;
            Assert.AreEqual(1f, RiskModel.ComputeSuccess(plan), Tol);
        }

        [Test]
        public void MoreRedundancy_NeverLowersSuccess()
        {
            PlanConfig plan = PlanConfig.SonOriginal();
            float previous = RiskModel.ComputeSuccess(plan);
            for (int n = 2; n <= 4; n++)
            {
                plan.subsystems[0].redundancy = n;
                float now = RiskModel.ComputeSuccess(plan);
                Assert.GreaterOrEqual(now, previous);
                previous = now;
            }
        }

        [Test]
        public void MassBudget_BothDefaultPlansFit()
        {
            Assert.IsTrue(PlanConfig.SonOriginal().WithinBudget);
            Assert.IsTrue(PlanConfig.FatherDefault().WithinBudget);
            Assert.AreEqual(60f, PlanConfig.SonOriginal().TotalMass, Tol);
            Assert.AreEqual(80f, PlanConfig.FatherDefault().TotalMass, Tol);
        }

        [Test]
        public void MissionIds_RoundTripAndOrder()
        {
            string[] ids = { MissionIds.M1, MissionIds.M2, MissionIds.M3, MissionIds.M4, MissionIds.Final, MissionIds.DesignMode };
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.AreEqual(ids[i], MissionIds.IdForScene(MissionIds.SceneFor(ids[i])));
            }
            Assert.AreEqual(MissionIds.M1, MissionIds.Next(MissionIds.Boot));
            Assert.AreEqual(MissionIds.M2, MissionIds.Next(MissionIds.M1));
            Assert.AreEqual(MissionIds.DesignMode, MissionIds.Next(MissionIds.Final));
            Assert.IsNull(MissionIds.Next(MissionIds.DesignMode));
        }

        [Test]
        public void PlayerState_SnapshotRoundTrip()
        {
            PlayerState s = ScriptableObject.CreateInstance<PlayerState>();
            s.ResetToDefaults();
            s.SetFuel(42f);
            string json = s.ToSnapshot();
            s.SetFuel(5f);
            s.RestoreSnapshot(json);
            Assert.AreEqual(42f, s.fuel, Tol);
            Object.DestroyImmediate(s);
        }
    }
}
