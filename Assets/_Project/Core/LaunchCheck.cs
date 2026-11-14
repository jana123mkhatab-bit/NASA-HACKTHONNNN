using System.Collections.Generic;

namespace LastSignal
{
    /// <summary>
    /// Pure resource and launch maths for the Planner and Design Mode. Reuses PlanConfig.TotalMass and RiskModel untouched.
    /// Vehicle payloads are NASA values read from parts_library.csv (SLS fact page). Power numbers are GAME-BALANCE values.
    /// </summary>
    public static class LaunchCheck
    {
        // ---- game balance (tagged on screen)
        public const float WattsPerTonneDraw = 120f;   // every non-Power unit draws this per tonne of hardware
        public const float WattsPerTonneSupply = 600f; // every Power unit supplies this per tonne of hardware

        public class Vehicle
        {
            public string id, name;
            public float payloadTonnes;
            public string reach; // "the Moon" or "deep space"
        }

        public struct Result
        {
            public bool ok;
            public string reason;
        }

        /// <summary>Ids in parts_library.csv that can lift a payload to a destination id (moon, mars, jupiter, trappist1e).</summary>
        public static string[] VehicleIdsFor(string destinationId) =>
            destinationId == "moon"
                ? new[] { "sls_b1_payload_moon" }
                : new[] { "sls_b1b_payload", "sls_b2_payload" };

        public static List<Vehicle> VehiclesFor(string destinationId, DataTable parts)
        {
            var list = new List<Vehicle>();
            if (parts == null) return list;
            foreach (var id in VehicleIdsFor(destinationId))
            {
                var row = parts.Find("id", id);
                if (row == null) continue;
                float kg = DataTable.F(row, "kg_or_n_derived");
                if (kg <= 0f) continue;
                list.Add(new Vehicle { id = id, name = row["part"].Replace(" payload to the Moon", "").Replace(" payload to deep space", ""),
                    payloadTonnes = kg / 1000f, reach = id.EndsWith("_moon") ? "the Moon" : "deep space" });
            }
            return list;
        }

        public static float PowerUsed(PlanConfig plan)
        {
            float w = 0f;
            if (plan == null) return w;
            foreach (var s in plan.subsystems)
                if (s != null && s.name != SubsystemNames.Power) w += s.massCost * System.Math.Max(1, s.redundancy) * WattsPerTonneDraw;
            return w;
        }

        public static float PowerAvailable(PlanConfig plan)
        {
            float w = 0f;
            if (plan == null) return w;
            foreach (var s in plan.subsystems)
                if (s != null && s.name == SubsystemNames.Power) w += s.massCost * System.Math.Max(1, s.redundancy) * WattsPerTonneSupply;
            return w;
        }

        /// <summary>Blocks launch when the plan's total mass exceeds the vehicle's payload to the destination.</summary>
        public static Result Check(PlanConfig plan, Vehicle v, string destinationName)
        {
            if (plan == null) return new Result { ok = false, reason = "No plan applied yet." };
            if (v == null) return new Result { ok = false, reason = "No launch vehicle data for " + destinationName + "." };
            float mass = plan.TotalMass();
            if (mass > v.payloadTonnes + 1e-4f)
                return new Result { ok = false, reason = "LAUNCH BLOCKED: ship mass " + mass.ToString("0.0") + " t is over the " + v.name +
                    " payload of " + v.payloadTonnes.ToString("0.0") + " t to " + v.reach + " (over by " + (mass - v.payloadTonnes).ToString("0.0") + " t). Remove hardware or pick a bigger vehicle." };
            return new Result { ok = true, reason = v.name + " can lift " + mass.ToString("0.0") + " t of " + v.payloadTonnes.ToString("0.0") + " t to " + v.reach + "." };
        }
    }
}
