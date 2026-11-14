using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LastSignal.M3
{
    public enum ChallengeKind { SolarStorm, Asteroid, Blackout, Power }

    /// <summary>One decision. Negative d* values are costs. All costs are GAME-BALANCE values, not NASA data.</summary>
    public class ChallengeOption
    {
        public string label, costNote, debrief;
        public float dHull, dPower, dFuel, dSupplies;
        /// <summary>Subsystem (SubsystemNames) whose reliability drops, and by how much (0 = none). Feeds RiskModel.</summary>
        public string subsystem;
        public float reliabilityHit;
        /// <summary>Fallback that can always be chosen, even with empty tanks, so the player can never soft-lock.</summary>
        public bool free;
    }

    /// <summary>A NASA-backed event: where the data comes from, why NASA cares, and the choices.</summary>
    public class ChallengeScenario
    {
        public ChallengeKind kind;
        public string title, dataset, dataFile, sourceUrl, retrieved, why, intro;
        public int dataRow;
        public readonly List<KeyValuePair<string, string>> facts = new List<KeyValuePair<string, string>>();
        public readonly List<ChallengeOption> options = new List<ChallengeOption>();
    }

    public struct ChallengeResult
    {
        public string summary;
        public float successBefore, successAfter;
    }

    /// <summary>
    /// Pure logic for the four flight-sector challenges (no scene needed, so it is unit-tested).
    /// Real values come from the CSVs in Data/Resources/LastSignal; a seeded System.Random picks which CSV row is used
    /// and in which order the four sites appear, so runs differ but tests are deterministic.
    /// </summary>
    public static class ChallengeModel
    {
        public const string DonkiCsv = "LastSignal/space_weather_donki_2026-10-01_to_2026-11-10";
        public const string NeoCsv = "LastSignal/neo_close_approaches_2026-11-13";
        public const string HorizonsCsv = "LastSignal/horizons_positions_2026-11-13";
        public const string FluxCsv = "LastSignal/solar_flux_by_distance";
        public const float SolarConstantWm2 = 1361f;   // NASA Earth fact sheet "Solar irradiance" (see solar_flux_by_distance.csv)
        public const string Tag = " [game balance]";

        static readonly ChallengeKind[] AllKinds = { ChallengeKind.SolarStorm, ChallengeKind.Asteroid, ChallengeKind.Blackout, ChallengeKind.Power };

        /// <summary>The four kinds in a seeded random order (Fisher-Yates). Same seed, same order.</summary>
        public static List<ChallengeKind> ShuffledOrder(int seed)
        {
            List<ChallengeKind> list = new List<ChallengeKind>(AllKinds);
            System.Random rng = new System.Random(seed);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                ChallengeKind t = list[i]; list[i] = list[j]; list[j] = t;
            }
            return list;
        }

        /// <summary>Builds the four scenarios in seeded order from the NASA CSVs.</summary>
        public static List<ChallengeScenario> PlanRun(int seed, CsvTable donki, CsvTable neo, CsvTable horizons, CsvTable flux)
        {
            List<ChallengeScenario> run = new List<ChallengeScenario>();
            foreach (ChallengeKind k in ShuffledOrder(seed))
            {
                CsvTable t = k == ChallengeKind.SolarStorm ? donki : k == ChallengeKind.Asteroid ? neo : k == ChallengeKind.Blackout ? horizons : flux;
                run.Add(Build(k, t, seed));
            }
            return run;
        }

        public static List<ChallengeScenario> LoadRun(int seed)
        {
            return PlanRun(seed, CsvTable.Load(DonkiCsv), CsvTable.Load(NeoCsv), CsvTable.Load(HorizonsCsv), CsvTable.Load(FluxCsv));
        }

        /// <summary>Which CSV row a kind uses for a seed. Rows that cannot be used (no light-time) are skipped.</summary>
        public static int PickRow(ChallengeKind kind, CsvTable t, int seed)
        {
            List<int> ok = new List<int>();
            for (int i = 0; i < t.rows.Count; i++)
            {
                if (kind == ChallengeKind.Blackout && Num(t, i, "light_time_from_earth_s") <= 0d) continue;
                if (kind == ChallengeKind.Power && Num(t, i, "flux_w_m2_derived") <= 0d) continue;
                ok.Add(i);
            }
            if (ok.Count == 0) return -1;
            System.Random rng = new System.Random(unchecked(seed * 31 + (int)kind * 7919 + 17));
            return ok[rng.Next(ok.Count)];
        }

        public static ChallengeScenario Build(ChallengeKind kind, CsvTable t, int seed)
        {
            int row = PickRow(kind, t, seed);
            ChallengeScenario s = new ChallengeScenario();
            s.kind = kind;
            s.dataRow = row;
            if (row < 0)
            {
                s.title = kind + " (CSV missing)"; AddFallback(s);
                NasaData.ReportFallback(kind.ToString() + " challenge CSV", "no usable CSV row, placeholder card shown");
                return s;
            }
            switch (kind)
            {
                case ChallengeKind.SolarStorm: BuildStorm(s, t, row); break;
                case ChallengeKind.Asteroid: BuildAsteroid(s, t, row); break;
                case ChallengeKind.Blackout: BuildBlackout(s, t, row); break;
                default: BuildPower(s, t, row); break;
            }
            return s;
        }

        // ---------------------------------------------------------------- A: solar storm (DONKI)

        static void BuildStorm(ChallengeScenario s, CsvTable t, int r)
        {
            string type = Str(t, r, "kind"), cls = Str(t, r, "class_or_type");
            bool flare = type == "solar_flare";
            double speed = Num(t, r, "speed");
            float sev = StormSeverity(flare, cls, speed);
            s.title = flare ? "SOLAR STORM: flare warning" : "SOLAR STORM: CME warning";
            s.dataset = "NASA DONKI (Space Weather Database Of Notifications, Knowledge, Information)";
            s.dataFile = "space_weather_donki_2026-10-01_to_2026-11-10.csv";
            s.sourceUrl = Str(t, r, "source_url");
            s.retrieved = Str(t, r, "retrieved");
            s.why = Str(t, r, "why_nasa_cares");
            s.facts.Add(F("Event", Str(t, r, "event_id")));
            s.facts.Add(F("Type", flare ? "Solar flare, " + cls : "Coronal mass ejection, " + cls));
            s.facts.Add(F("Time (UTC)", Str(t, r, "time_utc")));
            if (!flare) s.facts.Add(F("Speed", speed.ToString("0", CultureInfo.InvariantCulture) + " " + Str(t, r, "speed_unit")));
            s.intro = "Space-weather control reports a " + (flare ? "flare" : "CME") + " heading for your sector. Radiation will reach the ship soon.";
            float hull = Mathf.Round(14f * sev);
            s.options.Add(new ChallengeOption
            {
                label = "Shield: storm shelter", costNote = "Power -" + Mathf.Round(15f * sev).ToString("0") + Tag,
                dPower = -Mathf.Round(15f * sev),
                debrief = "Shields up. Power spent, hull untouched. Real crews shelter the same way when DONKI warns of a storm."
            });
            s.options.Add(new ChallengeOption
            {
                label = "Divert: leave the storm path", costNote = "Fuel -" + Mathf.Round(10f * sev).ToString("0") + Tag,
                dFuel = -Mathf.Round(10f * sev),
                debrief = "You burned fuel to leave the path. Spacecraft operators also reposition or pause work when a CME is forecast."
            });
            s.options.Add(new ChallengeOption
            {
                label = "Press on: take the radiation",
                costNote = "Hull -" + hull.ToString("0") + ", Life support reliability -" + Mathf.RoundToInt(0.06f * 100f) + " points" + Tag,
                dHull = -hull, subsystem = SubsystemNames.LifeSupport, reliabilityHit = 0.06f, free = true,
                debrief = "You saved power and fuel but the radiation damaged the hull and stressed life support, which lowers the mission's success chance."
            });
            s.options.ForEach(o => o.costNote += "  (severity " + sev.ToString("0.00") + " from the CSV value)");
        }

        /// <summary>[game balance] 0.5..1.5: bigger flare class number or faster CME means a harsher event.</summary>
        public static float StormSeverity(bool flare, string classOrType, double speedKmS)
        {
            if (!flare) return Mathf.Clamp((float)(speedKmS / 1000d), 0.5f, 1.5f);
            double mag = 0d;
            if (!string.IsNullOrEmpty(classOrType))
            {
                int k = classOrType.LastIndexOf(' ');
                string tail = classOrType.Substring(k + 1);
                if (tail.Length > 1) double.TryParse(tail.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out mag);
            }
            return Mathf.Clamp(0.5f + (float)mag / 10f, 0.5f, 1.5f);
        }

        // ---------------------------------------------------------------- B: asteroid flyby (JPL CAD)

        static void BuildAsteroid(ChallengeScenario s, CsvTable t, int r)
        {
            double vrel = Num(t, r, "v_rel_km_s");
            float sev = Mathf.Clamp((float)(vrel / 10d), 0.5f, 1.5f);
            s.title = "ASTEROID FLYBY: " + Str(t, r, "designation");
            s.dataset = "JPL Small-Body Database Close-Approach Data (CAD API)";
            s.dataFile = "neo_close_approaches_2026-11-13.csv";
            s.sourceUrl = Str(t, r, "source_url");
            s.retrieved = Str(t, r, "retrieved");
            s.why = Str(t, r, "why_nasa_cares");
            s.facts.Add(F("Object", Str(t, r, "designation")));
            s.facts.Add(F("Close approach (TDB)", Str(t, r, "close_approach_tdb")));
            s.facts.Add(F("Miss distance from Earth", Num(t, r, "nominal_dist_km_derived").ToString("#,0", CultureInfo.InvariantCulture) + " km (derived: au x 149,597,870.7)"));
            s.facts.Add(F("Relative speed", vrel.ToString("0.0", CultureInfo.InvariantCulture) + " km/s"));
            s.facts.Add(F("Absolute magnitude H", Str(t, r, "abs_magnitude_h")));
            s.intro = "A real near-Earth object is crossing your path. In this game it passes close to the ship.";
            s.options.Add(new ChallengeOption
            {
                label = "Burn fuel: dodge it", costNote = "Fuel -" + Mathf.Round(12f * sev).ToString("0") + Tag,
                dFuel = -Mathf.Round(12f * sev),
                debrief = "A clean dodge for a fuel cost. Planetary defence works the same idea: a small push early beats a big one late."
            });
            s.options.Add(new ChallengeOption
            {
                label = "Shield: brace for grazing hits", costNote = "Power -" + Mathf.Round(10f * sev).ToString("0") + ", Hull -3" + Tag,
                dPower = -Mathf.Round(10f * sev), dHull = -3f,
                debrief = "The shield soaked up the small fragments. You paid in power and a little hull."
            });
            float hull = Mathf.Round(18f * sev);
            s.options.Add(new ChallengeOption
            {
                label = "Accept the risk: hold course",
                costNote = "Hull -" + hull.ToString("0") + ", Navigation reliability -4 points" + Tag,
                dHull = -hull, subsystem = SubsystemNames.Navigation, reliabilityHit = 0.04f, free = true,
                debrief = "The object clipped you. Resources saved, but the hull took damage and the star trackers need recalibrating."
            });
            s.options.ForEach(o => o.costNote += "  (severity " + sev.ToString("0.00") + " from relative speed)");
        }

        // ---------------------------------------------------------------- C: communications blackout (Horizons light-time, DSN)

        static void BuildBlackout(ChallengeScenario s, CsvTable t, int r)
        {
            string body = Str(t, r, "body");
            double oneWay = Num(t, r, "light_time_from_earth_s");
            double rtt = oneWay * 2d;
            s.title = "COMMS BLACKOUT: " + body + " distance";
            s.dataset = "JPL Horizons light-time (one-way) and Deep Space Network notes";
            s.dataFile = "horizons_positions_2026-11-13.csv";
            s.sourceUrl = "https://ssd.jpl.nasa.gov/horizons/ and https://deepspace.jpl.nasa.gov/dsn/";
            s.retrieved = Str(t, r, "retrieved");
            s.why = "Radio travels at the speed of light, so a far-away ship cannot wait for Earth to answer every question. The DSN dishes keep the link, but the crew must be able to act alone.";
            s.facts.Add(F("If you were at", body));
            s.facts.Add(F("One-way light time from Earth", oneWay.ToString("0.0", CultureInfo.InvariantCulture) + " s"));
            s.facts.Add(F("Round trip (ask and answer)", rtt.ToString("0.0", CultureInfo.InvariantCulture) + " s = " + (rtt / 60d).ToString("0.0", CultureInfo.InvariantCulture) + " min"));
            s.intro = "A fault appears and the link to ground is dark. Earth's answer would take one round trip.";
            float wait = Mathf.Clamp(2f + (float)rtt / 120f, 2f, 20f);
            s.options.Add(new ChallengeOption
            {
                label = "Wait for ground to reply", costNote = "Supplies -" + Mathf.Round(wait).ToString("0") + " (time spent waiting)" + Tag,
                dSupplies = -Mathf.Round(wait),
                debrief = "You waited one round trip (" + (rtt / 60d).ToString("0.0", CultureInfo.InvariantCulture) + " min in the data). Safe, but crew time and supplies were used."
            });
            s.options.Add(new ChallengeOption
            {
                label = "Safe mode, then wait", costNote = "Power -6" + Tag,
                dPower = -6f,
                debrief = "Safe mode kept the ship stable while the message crossed space. Real craft use it the same way."
            });
            s.options.Add(new ChallengeOption
            {
                label = "Act on your own (autonomy)", costNote = "Hull -4, Navigation reliability -5 points" + Tag,
                dHull = -4f, subsystem = SubsystemNames.Navigation, reliabilityHit = 0.05f, free = true,
                debrief = "You fixed it without confirmation. It was fast, but an unchecked call cost some hull and trust in the nav solution."
            });
        }

        // ---------------------------------------------------------------- D: power / distance from the Sun (derived flux)

        static void BuildPower(ChallengeScenario s, CsvTable t, int r)
        {
            string body = Str(t, r, "body");
            double flux = Num(t, r, "flux_w_m2_derived");
            float ratio = Mathf.Clamp01((float)(flux / SolarConstantWm2));
            float drain = Mathf.Clamp(30f * (1f - ratio), 4f, 30f);   // [game balance]
            s.title = "POWER SHORTFALL: sunlight at " + body;
            s.dataset = "NASA planetary fact sheets, solar irradiance (flux derived by inverse-square law)";
            s.dataFile = "solar_flux_by_distance.csv";
            s.sourceUrl = Str(t, r, "fact_sheet_url");
            s.retrieved = Str(t, r, "retrieved");
            s.why = "Sunlight gets weaker with the square of distance, so solar-powered spacecraft must choose what to run as they travel away from the Sun.";
            s.facts.Add(F("Location", body + ", " + Str(t, r, "dist_from_sun_au") + " AU from the Sun"));
            s.facts.Add(F("Sunlight (derived, not measured)", flux.ToString("0.0", CultureInfo.InvariantCulture) + " W/m2 vs 1361 W/m2 at 1 AU"));
            s.facts.Add(F("Share of Earth-distance sunlight", (ratio * 100f).ToString("0", CultureInfo.InvariantCulture) + " %"));
            s.intro = "Your panels give less than the ship needs. Choose which subsystems stay powered.";
            s.options.Add(new ChallengeOption
            {
                label = "Everything on", costNote = "Power -" + Mathf.Round(drain * 1.5f).ToString("0") + Tag,
                dPower = -Mathf.Round(drain * 1.5f),
                debrief = "Full systems, drained batteries. Fine when the Sun is bright, risky when it is not."
            });
            s.options.Add(new ChallengeOption
            {
                label = "Life support + comms only", costNote = "Power -" + Mathf.Round(drain * 0.5f).ToString("0") + ", Navigation reliability -3 points" + Tag,
                dPower = -Mathf.Round(drain * 0.5f), subsystem = SubsystemNames.Navigation, reliabilityHit = 0.03f,
                debrief = "Navigation went dark to save power. You kept the crew alive and the link up, but you are flying a little blind."
            });
            s.options.Add(new ChallengeOption
            {
                label = "Survival mode: life support only", costNote = "No power spent, Comms reliability -7 points" + Tag,
                subsystem = SubsystemNames.Comms, reliabilityHit = 0.07f, free = true,
                debrief = "Only life support stayed on. No battery spent, but the dark comms link hurts the mission's odds."
            });
        }

        static void AddFallback(ChallengeScenario s)
        {
            s.dataset = "missing CSV";
            s.why = "NASA data file is missing; this is a placeholder so the game never soft-locks.";
            s.sourceUrl = "https://www.nasa.gov/";
            s.options.Add(new ChallengeOption { label = "Continue", costNote = "No cost", free = true, debrief = "Nothing happened." });
        }

        // ---------------------------------------------------------------- cost application

        public static bool CanAfford(ChallengeOption o, PlayerState s)
        {
            if (o.free) return true;
            return s.power + o.dPower >= 0f && s.fuel + o.dFuel >= 0f && s.supplies + o.dSupplies >= 0f;
        }

        /// <summary>At least one option is always selectable, so no challenge can soft-lock.</summary>
        public static bool IsPassable(ChallengeScenario sc, PlayerState s)
        {
            for (int i = 0; i < sc.options.Count; i++) if (CanAfford(sc.options[i], s)) return true;
            return false;
        }

        /// <summary>
        /// Applies the cost to PlayerState (hull never drops below 1 here so a challenge cannot end the run by itself)
        /// and scales plannedSuccess by RiskModel: ratio = success(plan with the subsystem hit) / success(plan).
        /// </summary>
        public static ChallengeResult Apply(ChallengeOption o, PlayerState s)
        {
            ChallengeResult res = new ChallengeResult();
            res.successBefore = s.plannedSuccess;
            s.SetHull(Mathf.Max(1f, s.hull + o.dHull));
            s.SetPower(s.power + o.dPower);
            s.SetFuel(s.fuel + o.dFuel);
            s.SetSupplies(s.supplies + o.dSupplies);
            float ratio = RiskRatio(o.subsystem, o.reliabilityHit);
            s.plannedSuccess = Mathf.Clamp01(s.plannedSuccess * ratio);
            res.successAfter = s.plannedSuccess;
            s.NotifyChanged();
            res.summary = Summary(o, res);
            return res;
        }

        /// <summary>Ratio of RiskModel.ComputeSuccess after / before lowering one subsystem's reliability on the starter plan.</summary>
        public static float RiskRatio(string subsystem, float hit)
        {
            if (hit <= 0f || string.IsNullOrEmpty(subsystem)) return 1f;
            PlanConfig before = PlanConfig.SonsOriginalPlan();
            PlanConfig after = PlanConfig.SonsOriginalPlan();
            for (int i = 0; i < after.subsystems.Count; i++)
            {
                Subsystem ss = after.subsystems[i];
                if (ss.name == subsystem) ss.reliability = Mathf.Max(0.05f, ss.reliability - hit);
            }
            float b = RiskModel.ComputeSuccess(before);
            return b <= 0f ? 1f : Mathf.Clamp01(RiskModel.ComputeSuccess(after) / b);
        }

        static string Summary(ChallengeOption o, ChallengeResult r)
        {
            List<string> parts = new List<string>();
            if (o.dHull != 0f) parts.Add("Hull " + o.dHull.ToString("+0;-0"));
            if (o.dPower != 0f) parts.Add("Power " + o.dPower.ToString("+0;-0"));
            if (o.dFuel != 0f) parts.Add("Fuel " + o.dFuel.ToString("+0;-0"));
            if (o.dSupplies != 0f) parts.Add("Supplies " + o.dSupplies.ToString("+0;-0"));
            if (parts.Count == 0) parts.Add("No resource cost");
            string s = string.Join(", ", parts.ToArray());
            if (r.successAfter < r.successBefore - 1e-6f)
                s += "\nMission success chance (RiskModel): " + (r.successBefore * 100f).ToString("0.0") + "% -> " + (r.successAfter * 100f).ToString("0.0") + "%";
            return s + "  " + Tag.Trim();
        }

        // ---------------------------------------------------------------- helpers

        static KeyValuePair<string, string> F(string k, string v) { return new KeyValuePair<string, string>(k, v); }

        static string Str(CsvTable t, int row, string col)
        {
            int c = t.Column(col);
            if (c < 0 || row < 0 || row >= t.rows.Count || c >= t.rows[row].Count) return "";
            return t.rows[row][c].Trim();
        }

        static double Num(CsvTable t, int row, string col)
        {
            double v;
            return double.TryParse(Str(t, row, col), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0d;
        }
    }
}
