using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.M3
{
    /// <summary>A place the father could fly to after the stranger leaves.</summary>
    public class Destination
    {
        public string id;
        public string name;
        /// <summary>Supplies needed with a fully repaired ship. GAME-BALANCE value.</summary>
        public float supplyCost;
        public bool isPlanet;

        public Destination(string id, string name, float supplyCost, bool isPlanet)
        {
            this.id = id;
            this.name = name;
            this.supplyCost = supplyCost;
            this.isPlanet = isPlanet;
        }
    }

    /// <summary>
    /// Reachability after the stranger takes half the supplies. The numbers are tuned so only the planet is reachable.
    /// NOTE: supply costs, multipliers and reliabilities are GAME-BALANCE values, not NASA data.
    /// </summary>
    public static class RerouteModel
    {
        public const string Earth = "earth";
        public const string Outpost = "outpost";
        public const string Planet = "planet";
        public const string SuppliesMargin = "Supplies margin";

        /// <summary>The stranger always leaves at least this much (so the planet stays reachable). GAME-BALANCE value.</summary>
        public const float MinSuppliesLeft = 36f;

        public static List<Destination> Default()
        {
            List<Destination> list = new List<Destination>();
            list.Add(new Destination(Earth, "Earth (home)", 85f, false));
            list.Add(new Destination(Outpost, "Outpost Vesta", 60f, false));
            list.Add(new Destination(Planet, "Uncharted planet", 30f, true));
            return list;
        }

        /// <summary>Supplies left after the stranger takes half (never below MinSuppliesLeft unless you had less).</summary>
        public static float SuppliesAfterTheft(float supplies)
        {
            return Mathf.Max(supplies * 0.5f, Mathf.Min(supplies, MinSuppliesLeft));
        }

        /// <summary>Unrepaired engine or life support makes every trip cost more supplies.</summary>
        public static float CostMultiplier(bool engineRepaired, bool lifeSupportRepaired)
        {
            float m = 1f;
            if (!engineRepaired) m *= 1.4f;
            if (!lifeSupportRepaired) m *= 1.25f;
            return m;
        }

        public static float EffectiveCost(Destination d, float multiplier)
        {
            return d.supplyCost * multiplier;
        }

        public static bool Reachable(Destination d, float supplies, float multiplier)
        {
            return supplies + 0.0001f >= EffectiveCost(d, multiplier);
        }

        /// <summary>Plan for the Risk Gauge: repaired systems are reliable, the supplies margin is 0 if you cannot make it.</summary>
        public static PlanConfig PlanFor(Destination d, float supplies, bool engineRepaired, bool lifeSupportRepaired)
        {
            float cost = EffectiveCost(d, CostMultiplier(engineRepaired, lifeSupportRepaired));
            float marginR;
            if (supplies < cost) marginR = 0f;
            else marginR = Mathf.Lerp(0.6f, 0.99f, Mathf.Clamp01((supplies - cost) / Mathf.Max(1f, cost * 0.5f)));

            PlanConfig plan = new PlanConfig();
            plan.massBudget = 100f;
            plan.subsystems.Add(new Subsystem(SubsystemNames.Propulsion, engineRepaired ? 0.95f : 0.75f, 1, 0f));
            plan.subsystems.Add(new Subsystem(SubsystemNames.LifeSupport, lifeSupportRepaired ? 0.95f : 0.8f, 1, 0f));
            plan.subsystems.Add(new Subsystem(SubsystemNames.Navigation, 0.97f, 1, 0f));
            plan.subsystems.Add(new Subsystem(SuppliesMargin, marginR, 1, 0f));
            return plan;
        }
    }
}
