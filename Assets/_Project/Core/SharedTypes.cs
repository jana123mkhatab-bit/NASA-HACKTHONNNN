using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal
{
    /// <summary>Mission ids, order and scene names. One scene per mission.</summary>
    public static class MissionIds
    {
        public const string Boot = "Boot", M1 = "M1", M2 = "M2", M3 = "M3", M4 = "M4", Final = "Final", DesignMode = "DesignMode";

        public static readonly string[] Order = { Boot, M1, M2, M3, M4, Final, DesignMode };

        public static string SceneFor(string id)
        {
            switch (id)
            {
                case M1: return "M1_Ground";
                case M2: return "M2_Preflight";
                case M3: return "M3_DeepSpace";
                case M4: return "M4_Planet";
                case Final: return "M4_Planet"; // finale is owned by person 4 and lives in the M4 scene until split
                case DesignMode: return "DesignMode";
                default: return "M1_Ground";
            }
        }

        public static string Next(string id)
        {
            int i = Array.IndexOf(Order, id);
            return i < 0 || i >= Order.Length - 1 ? null : Order[i + 1];
        }

        public static string TitleFor(string id)
        {
            switch (id)
            {
                case M1: return "What Fell to Earth";
                case M2: return "Ten Percent";
                case M3: return "The Ship That Came Out of Nowhere";
                case M4: return "Someone's Been Living Here";
                case Final: return "Call Home";
                case DesignMode: return "Mission Design Mode";
                default: return "The Last Known Signal";
            }
        }
    }

    /// <summary>Static event bus. Missions raise Complete/Fail; MissionManager listens.</summary>
    public static class MissionEvents
    {
        public static event Action<string> OnMissionStart;
        public static event Action<string> OnMissionComplete;
        public static event Action<string, string> OnMissionFail;

        public static void RaiseStart(string id) => OnMissionStart?.Invoke(id);
        public static void RaiseComplete(string id) => OnMissionComplete?.Invoke(id);
        public static void RaiseFail(string id, string reason) => OnMissionFail?.Invoke(id, reason);

        /// <summary>Tests only: drop all subscribers.</summary>
        public static void ClearAll() { OnMissionStart = null; OnMissionComplete = null; OnMissionFail = null; }
    }

    /// <summary>One ship subsystem. Reliability and massCost are GAME-BALANCE values, not NASA data.</summary>
    [Serializable]
    public class Subsystem
    {
        public string name;
        [Range(0f, 1f)] public float reliability = 0.9f; // r (game balance)
        [Min(1)] public int redundancy = 1;              // n
        public float massCost = 1f;                      // game balance units

        public Subsystem() { }
        public Subsystem(string name, float r, int n, float massCost)
        {
            this.name = name; reliability = r; redundancy = n; this.massCost = massCost;
        }
    }

    /// <summary>A mission plan: a list of subsystems.</summary>
    [Serializable]
    public class PlanConfig
    {
        public List<Subsystem> subsystems = new List<Subsystem>();

        public float TotalMass()
        {
            float m = 0f;
            foreach (var s in subsystems) if (s != null) m += s.massCost * Mathf.Max(1, s.redundancy);
            return m;
        }

        /// <summary>Game-balance starter plan: five subsystems at r=0.63, n=1 (about 10%).</summary>
        public static PlanConfig SonsOriginalPlan()
        {
            var p = new PlanConfig();
            foreach (var n in new[] { "Propulsion", "Power", "Life support", "Navigation", "Comms" })
                p.subsystems.Add(new Subsystem(n, 0.63f, 1, 1f));
            return p;
        }
    }

    /// <summary>P = product over subsystems of [1 - (1 - r)^n]. Pure math, no Unity state.</summary>
    public static class RiskModel
    {
        public static float ComputeSuccess(PlanConfig plan)
        {
            if (plan == null || plan.subsystems == null) return 0f;
            double p = 1.0;
            foreach (var s in plan.subsystems)
            {
                if (s == null) continue;
                double r = Math.Min(1.0, Math.Max(0.0, s.reliability));
                int n = Math.Max(1, s.redundancy);
                p *= 1.0 - Math.Pow(1.0 - r, n);
            }
            return (float)p;
        }
    }

}
