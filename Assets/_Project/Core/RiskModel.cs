using System;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// The Risk Model behind the Success Probability gauge.
    ///
    /// Each subsystem has a base reliability r and n redundant units.
    /// A subsystem works if at least one of its n units works:   R = 1 - (1 - r)^n
    /// The mission succeeds only if every subsystem works:        P = product of all R
    ///
    /// Reliabilities in this game are GAME-BALANCE values, not NASA figures.
    /// </summary>
    public static class RiskModel
    {
        public static float SubsystemSuccess(float reliability, int redundancy)
        {
            if (redundancy <= 0) return 0f;
            float r = Mathf.Clamp01(reliability);
            return (float)(1.0 - Math.Pow(1.0 - r, redundancy));
        }

        public static float SubsystemSuccess(Subsystem subsystem)
        {
            if (subsystem == null) return 0f;
            return SubsystemSuccess(subsystem.reliability, subsystem.redundancy);
        }

        /// <summary>Success Probability from 0 to 1. An empty plan scores 0.</summary>
        public static float ComputeSuccess(PlanConfig plan)
        {
            if (plan == null || plan.subsystems == null || plan.subsystems.Count == 0) return 0f;
            double p = 1.0;
            for (int i = 0; i < plan.subsystems.Count; i++)
            {
                p *= SubsystemSuccess(plan.subsystems[i]);
            }
            return (float)p;
        }

        /// <summary>The subsystem with the lowest success chance (the first one if several tie), or null.</summary>
        public static Subsystem Weakest(PlanConfig plan)
        {
            if (plan == null || plan.subsystems == null || plan.subsystems.Count == 0) return null;
            Subsystem weakest = null;
            float lowest = float.MaxValue;
            for (int i = 0; i < plan.subsystems.Count; i++)
            {
                float s = SubsystemSuccess(plan.subsystems[i]);
                if (s < lowest)
                {
                    lowest = s;
                    weakest = plan.subsystems[i];
                }
            }
            return weakest;
        }

        /// <summary>One-line "Risk Briefing" shown under the gauge: always tells the player the real number and why.</summary>
        public static string RiskBriefing(PlanConfig plan)
        {
            if (plan == null || plan.subsystems == null || plan.subsystems.Count == 0) return "No plan loaded.";

            int percent = Mathf.RoundToInt(ComputeSuccess(plan) * 100f);
            Subsystem weakest = Weakest(plan);
            string text = "Success chance " + percent + "%.";
            if (weakest != null)
            {
                int weakPercent = Mathf.RoundToInt(SubsystemSuccess(weakest) * 100f);
                text += " Weakest link: " + weakest.name + " (" + weakPercent + "%).";
            }
            if (!plan.WithinBudget)
            {
                text += " Over the mass budget by " + Mathf.CeilToInt(plan.TotalMass - plan.massBudget) + ".";
            }
            return text;
        }

        /// <summary>Rolls one subsystem at a checkpoint. True means it worked. Pass your own System.Random for repeatable runs.</summary>
        public static bool RollSubsystem(Subsystem subsystem, System.Random rng)
        {
            if (rng == null) rng = new System.Random();
            return rng.NextDouble() < SubsystemSuccess(subsystem);
        }
    }
}
