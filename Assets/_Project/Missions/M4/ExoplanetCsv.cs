using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace LastSignal.M4
{
    /// <summary>One planet row. NaN means the CSV had no value for that column.</summary>
    [Serializable]
    public class ExoplanetRecord
    {
        public string name = "Unknown planet";
        public string host = "";
        public float radiusEarth = float.NaN;   // pl_rade  [Earth radii]
        public float insolation = float.NaN;    // pl_insol [Earth flux]
        public float eqTempK = float.NaN;       // pl_eqt   [K]
        public float periodDays = float.NaN;    // pl_orbper [days]
        public float distancePc = float.NaN;    // sy_dist  [parsec]
        public float starTempK = float.NaN;     // st_teff  [K]

        /// <summary>
        /// Rough surface gravity in g for a rocky planet, from radius only.
        /// Mass from the Chen &amp; Kipping (2017) terran mass-radius relation: R = 1.008 M^0.279.
        /// This is a MODEL estimate, not a measured value; the UI labels it so.
        /// </summary>
        public float GravityEstimateG
        {
            get
            {
                if (float.IsNaN(radiusEarth) || radiusEarth <= 0f) return 1f;
                float r = Mathf.Min(radiusEarth, 1.6f); // the relation only holds for rocky worlds
                float mass = Mathf.Pow(r / 1.008f, 1f / 0.279f);
                return mass / (r * r);
            }
        }

        public float LightYears => float.IsNaN(distancePc) ? float.NaN : distancePc * 3.26156f;
    }

    /// <summary>
    /// Loads the exoplanet CSV that person 6 puts in Assets/_Project/Data. The editor script M4CsvSync copies any
    /// Data/*exoplanet*.csv into Missions/M4/Resources/LastSignal/M4/exoplanets.csv so it ships in builds.
    /// Column names follow the NASA Exoplanet Archive (pl_name, pl_rade, pl_insol, pl_eqt, pl_orbper, sy_dist, st_teff);
    /// friendly aliases are accepted too. Lines starting with # are comments. No network calls.
    /// </summary>
    public static class ExoplanetCsv
    {
        public const string SyncedResource = "LastSignal/M4/exoplanets";
        public const string FallbackResource = "LastSignal/M4/exoplanets_fallback";
        public const string PreferredPlanet = "Kepler-442 b";
        /// <summary>The real NASA row (data_sources.csv name). The synced exoplanets.csv is a copy of it.</summary>
        public const string RealFile = "exoplanet_trappist1e.csv";
        /// <summary>True if the last Load used the fallback CSV or found nothing. Shown on the planet card and in Data and Sources.</summary>
        public static bool LastWasFallback;

        public static ExoplanetRecord Load(out string sourceLabel)
        {
            var ta = Resources.Load<TextAsset>(SyncedResource);
            LastWasFallback = false;
            sourceLabel = "Data/exoplanets.csv (NASA Exoplanet Archive)";
            if (ta == null)
            {
                ta = Resources.Load<TextAsset>(FallbackResource);
                sourceLabel = "M4 FALLBACK CSV (approximate values, not the NASA row)";
                LastWasFallback = true;
                NasaData.ReportFallback(RealFile, "M4 used exoplanets_fallback.csv because the real exoplanets.csv is missing");
            }
            if (ta == null)
            {
                LastWasFallback = true;
                NasaData.ReportFallback(RealFile, "M4 found no exoplanet CSV at all");
                sourceLabel = "no CSV found";
                Debug.LogWarning("[M4] No exoplanet CSV found. Add Assets/_Project/Data/exoplanets.csv.");
                return new ExoplanetRecord();
            }
            var rows = Parse(ta.text);
            if (rows.Count == 0) { sourceLabel += " (empty)"; return new ExoplanetRecord(); }
            foreach (var r in rows)
                if (string.Equals(r.name.Trim(), PreferredPlanet, StringComparison.OrdinalIgnoreCase)) return r;
            return rows[0];
        }

        public static List<ExoplanetRecord> Parse(string text)
        {
            var list = new List<ExoplanetRecord>();
            if (string.IsNullOrEmpty(text)) return list;
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            Dictionary<string, int> col = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var cells = SplitCsv(line);
                if (col == null)
                {
                    col = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < cells.Count; i++) col[cells[i].Trim()] = i;
                    continue;
                }
                var r = new ExoplanetRecord
                {
                    name = Str(cells, col, "pl_name", "name", "planet") ?? "Unknown planet",
                    host = Str(cells, col, "hostname", "host", "star") ?? "",
                    radiusEarth = Num(cells, col, "pl_rade", "radius_earth", "radius"),
                    insolation = Num(cells, col, "pl_insol", "insolation"),
                    eqTempK = Num(cells, col, "pl_eqt", "eq_temp_k", "equilibrium_temperature"),
                    periodDays = Num(cells, col, "pl_orbper", "orbital_period_days", "period"),
                    distancePc = Num(cells, col, "sy_dist", "distance_pc"),
                    starTempK = Num(cells, col, "st_teff", "star_temp_k"),
                };
                list.Add(r);
            }
            return list;
        }

        static string Str(List<string> cells, Dictionary<string, int> col, params string[] keys)
        {
            foreach (var k in keys)
                if (col.TryGetValue(k, out int i) && i < cells.Count && cells[i].Trim().Length > 0) return cells[i].Trim();
            return null;
        }

        static float Num(List<string> cells, Dictionary<string, int> col, params string[] keys)
        {
            var s = Str(cells, col, keys);
            return s != null && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : float.NaN;
        }

        /// <summary>Splits one CSV line, honouring double-quoted fields.</summary>
        static List<string> SplitCsv(string line)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool q = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (q && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else q = !q;
                }
                else if (c == ',' && !q) { cells.Add(sb.ToString()); sb.Length = 0; }
                else sb.Append(c);
            }
            cells.Add(sb.ToString());
            return cells;
        }

        public static string Fmt(float v, string format, string unit)
            => float.IsNaN(v) ? "n/a" : v.ToString(format, CultureInfo.InvariantCulture) + unit;
    }
}
