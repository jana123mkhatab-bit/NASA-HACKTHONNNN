using System.Collections.Generic;

namespace LastSignal.M3
{
    /// <summary>One thing the father can repair after the ram.</summary>
    public class RepairOption
    {
        public string id;
        public string label;
        public string effect;
        public int powerCost;
        public bool required;
        public bool selected;

        public RepairOption(string id, string label, int powerCost, bool required, string effect)
        {
            this.id = id;
            this.label = label;
            this.powerCost = powerCost;
            this.required = required;
            this.effect = effect;
        }
    }

    /// <summary>What the player chose in triage.</summary>
    public struct TriageResult
    {
        public bool scanner;
        public bool engine;
        public bool lifeSupport;
        public bool comms;
        public bool hull;
        public int powerUsed;
    }

    /// <summary>
    /// Triage rules: limited power, pick what to repair. The scanner is required to go on.
    /// NOTE: power costs are GAME-BALANCE values, not NASA data.
    /// </summary>
    public static class TriageModel
    {
        public const string Scanner = "scanner";
        public const string Engine = "engine";
        public const string LifeSupport = "life_support";
        public const string Comms = "comms";
        public const string Hull = "hull";

        public static List<RepairOption> DefaultOptions()
        {
            List<RepairOption> list = new List<RepairOption>();
            list.Add(new RepairOption(Scanner, "Scanner", 20, true, "Needed to find anywhere to go."));
            list.Add(new RepairOption(Engine, "Main engine", 15, false, "Without it: half thrust, trips cost more supplies."));
            list.Add(new RepairOption(LifeSupport, "Life support", 10, false, "Without it: supplies drain twice as fast."));
            list.Add(new RepairOption(Comms, "Long-range comms", 25, false, "Talks to the relay... which is gone."));
            list.Add(new RepairOption(Hull, "Hull patch", 10, false, "+15 hull."));
            return list;
        }

        public static int Used(List<RepairOption> options)
        {
            int used = 0;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].selected) used += options[i].powerCost;
            }
            return used;
        }

        /// <summary>Toggles an option. Returns false (and changes nothing) if selecting it would go over the budget.</summary>
        public static bool Toggle(List<RepairOption> options, int index, int budget)
        {
            if (index < 0 || index >= options.Count) return false;
            RepairOption o = options[index];
            if (!o.selected && Used(options) + o.powerCost > budget) return false;
            o.selected = !o.selected;
            return true;
        }

        public static bool CanConfirm(List<RepairOption> options, int budget, out string reason)
        {
            if (Used(options) > budget)
            {
                reason = "Not enough power.";
                return false;
            }
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].required && !options[i].selected)
                {
                    reason = options[i].label + " must be repaired. " + options[i].effect;
                    return false;
                }
            }
            reason = "";
            return true;
        }

        public static TriageResult Result(List<RepairOption> options)
        {
            TriageResult r = new TriageResult();
            for (int i = 0; i < options.Count; i++)
            {
                if (!options[i].selected) continue;
                switch (options[i].id)
                {
                    case Scanner: r.scanner = true; break;
                    case Engine: r.engine = true; break;
                    case LifeSupport: r.lifeSupport = true; break;
                    case Comms: r.comms = true; break;
                    case Hull: r.hull = true; break;
                }
            }
            r.powerUsed = Used(options);
            return r;
        }
    }
}
