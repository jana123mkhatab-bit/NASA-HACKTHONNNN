using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>Standard subsystem names used by the Mission Planner and the Risk Model.</summary>
    public static class SubsystemNames
    {
        public const string LifeSupport = "Life support";
        public const string BackupComms = "Backup comms";
        public const string Navigation = "Navigation";
        public const string Power = "Power";
        public const string Propulsion = "Propulsion";
    }

    /// <summary>
    /// One ship subsystem in a mission plan.
    /// NOTE: reliability and massCost are GAME-BALANCE values chosen for gameplay. They are not NASA figures.
    /// </summary>
    [Serializable]
    public class Subsystem
    {
        public string name;
        [Range(0f, 1f)] public float reliability = 0.9f;
        [Min(0)] public int redundancy = 1;
        /// <summary>Mass of ONE unit. Total mass = massCost x redundancy.</summary>
        public float massCost = 10f;

        public Subsystem() { }

        public Subsystem(string name, float reliability, int redundancy, float massCost)
        {
            this.name = name;
            this.reliability = reliability;
            this.redundancy = redundancy;
            this.massCost = massCost;
        }

        public Subsystem Clone()
        {
            return new Subsystem(name, reliability, redundancy, massCost);
        }
    }

    /// <summary>A mission plan: a list of subsystems and a mass budget (part of the contract).</summary>
    [Serializable]
    public class PlanConfig
    {
        public List<Subsystem> subsystems = new List<Subsystem>();
        public float massBudget = 100f;

        public float TotalMass
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < subsystems.Count; i++) total += subsystems[i].massCost * subsystems[i].redundancy;
                return total;
            }
        }

        public bool WithinBudget { get { return TotalMass <= massBudget + 0.0001f; } }

        public Subsystem Find(string subsystemName)
        {
            for (int i = 0; i < subsystems.Count; i++)
            {
                if (subsystems[i].name == subsystemName) return subsystems[i];
            }
            return null;
        }

        public PlanConfig Clone()
        {
            PlanConfig copy = new PlanConfig();
            copy.massBudget = massBudget;
            for (int i = 0; i < subsystems.Count; i++) copy.subsystems.Add(subsystems[i].Clone());
            return copy;
        }

        /// <summary>The son's original plan: untested equipment, every subsystem single-string. Scores about 10%.</summary>
        public static PlanConfig SonOriginal()
        {
            PlanConfig plan = new PlanConfig();
            plan.massBudget = 100f;
            plan.subsystems.Add(new Subsystem(SubsystemNames.LifeSupport, 0.63f, 1, 12f));
            plan.subsystems.Add(new Subsystem(SubsystemNames.BackupComms, 0.63f, 1, 8f));
            plan.subsystems.Add(new Subsystem(SubsystemNames.Navigation, 0.63f, 1, 6f));
            plan.subsystems.Add(new Subsystem(SubsystemNames.Power, 0.63f, 1, 14f));
            plan.subsystems.Add(new Subsystem(SubsystemNames.Propulsion, 0.63f, 1, 20f));
            return plan;
        }

        /// <summary>The father's plan: tested equipment, backup comms and backup life support. Scores about 71%.</summary>
        public static PlanConfig FatherDefault()
        {
            PlanConfig plan = new PlanConfig();
            plan.massBudget = 100f;
            plan.subsystems.Add(new Subsystem(SubsystemNames.LifeSupport, 0.90f, 2, 12f));
            plan.subsystems.Add(new Subsystem(SubsystemNames.BackupComms, 0.90f, 2, 8f));
            plan.subsystems.Add(new Subsystem(SubsystemNames.Navigation, 0.90f, 1, 6f));
            plan.subsystems.Add(new Subsystem(SubsystemNames.Power, 0.90f, 1, 14f));
            plan.subsystems.Add(new Subsystem(SubsystemNames.Propulsion, 0.90f, 1, 20f));
            return plan;
        }
    }
}
