using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class RiskModelTests
    {
        static PlanConfig Plan(float r, params int[] redundancy)
        {
            var p = new PlanConfig();
            for (int i = 0; i < redundancy.Length; i++) p.subsystems.Add(new Subsystem("S" + i, r, redundancy[i], 1f));
            return p;
        }

        [Test]
        public void FiveSubsystems_R063_N1_IsAboutTenPercent()
        {
            Assert.AreEqual(0.099f, RiskModel.ComputeSuccess(Plan(0.63f, 1, 1, 1, 1, 1)), 0.002f);
        }

        [Test]
        public void FiveSubsystems_R090_TwoWithN2_IsAbout0715()
        {
            Assert.AreEqual(0.715f, RiskModel.ComputeSuccess(Plan(0.90f, 1, 1, 1, 2, 2)), 0.002f);
        }

        [Test]
        public void SonsOriginalPlan_ScoresAboutTenPercent()
        {
            Assert.AreEqual(0.10f, RiskModel.ComputeSuccess(PlanConfig.SonsOriginalPlan()), 0.01f);
        }

        [Test]
        public void EmptyOrNullPlan_Is_Zero_Or_One_Safely()
        {
            Assert.AreEqual(0f, RiskModel.ComputeSuccess(null));
            Assert.AreEqual(1f, RiskModel.ComputeSuccess(new PlanConfig()), 1e-6f);
        }

        [Test]
        public void MoreRedundancy_NeverLowersSuccess()
        {
            float a = RiskModel.ComputeSuccess(Plan(0.8f, 1, 1, 1));
            float b = RiskModel.ComputeSuccess(Plan(0.8f, 2, 1, 1));
            Assert.GreaterOrEqual(b, a);
        }

        [Test]
        public void ReliabilityIsClamped()
        {
            Assert.AreEqual(1f, RiskModel.ComputeSuccess(Plan(5f, 1)), 1e-6f);
            Assert.AreEqual(0f, RiskModel.ComputeSuccess(Plan(-1f, 1)), 1e-6f);
        }
    }
}
