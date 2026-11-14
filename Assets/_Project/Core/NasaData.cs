using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LastSignal
{
    /// <summary>One row of Data/Resources/LastSignal/data_sources.csv.</summary>
    public class NasaSource
    {
        public string file, title, sourceUrl, retrieved, kind, usedBy, why;
        public bool IsDerived { get { return kind == "DERIVED"; } }
    }

    /// <summary>
    /// The one place that knows where every NASA number comes from. Draws the "NASA DATA" tag used on
    /// destination cards, the M4 planet card, debriefs and the Data and Sources screen, remembers which datasets
    /// shaped this playthrough (for the end card), and records any time a mission had to use fallback data.
    /// All data is a snapshot, never live.
    /// </summary>
    public static class NasaData
    {
        public const string SnapshotLine = "Snapshot taken 2026-11-13 (files retrieved 2026-11-13 and 2026-11-14). Not live data.";
        const string Cyan = "#7FE0FF", Grey = "#8899AA", Amber = "#FFB938";

        public class Use { public string file, mission, fact; }
        public class Fallback { public string file, reason; }

        static List<NasaSource> _all;
        static readonly List<Use> _uses = new List<Use>();
        static readonly List<Fallback> _fallbacks = new List<Fallback>();

        public static List<NasaSource> All
        {
            get { if (_all == null) _all = Load(); return _all; }
        }

        public static List<Use> Uses { get { return _uses; } }
        public static List<Fallback> Fallbacks { get { return _fallbacks; } }

        public static NasaSource Get(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            for (int i = 0; i < All.Count; i++) if (All[i].file == file) return All[i];
            return null;
        }

        /// <summary>"horizons_positions_2026-11-13.csv#Moon" or "exoplanet_trappist1e.csv (sy_dist ...)" -> the file name.</summary>
        public static string FileFromRef(string r)
        {
            if (string.IsNullOrEmpty(r)) return r;
            int i = r.IndexOfAny(new[] { '#', ' ' });
            return i < 0 ? r : r.Substring(0, i);
        }

        // ------------------------------------------------------------ run tracking

        public static void ResetRun() { _uses.Clear(); _fallbacks.Clear(); }

        /// <summary>Call when a dataset really shaped something on screen. The fact is the real value shown (optional).</summary>
        public static void Mark(string file, string mission, string fact = null)
        {
            for (int i = 0; i < _uses.Count; i++)
                if (_uses[i].file == file && _uses[i].mission == mission && _uses[i].fact == fact) return;
            _uses.Add(new Use { file = file, mission = mission, fact = fact });
        }

        /// <summary>Call when a mission could not read its real CSV and used fallback numbers. Logged and shown in Data and Sources.</summary>
        public static void ReportFallback(string file, string reason)
        {
            for (int i = 0; i < _fallbacks.Count; i++) if (_fallbacks[i].file == file && _fallbacks[i].reason == reason) return;
            _fallbacks.Add(new Fallback { file = file, reason = reason });
            Debug.LogWarning("[NasaData] FALLBACK for " + file + ": " + reason);
        }

        public static bool HasFallback(string file)
        {
            for (int i = 0; i < _fallbacks.Count; i++) if (_fallbacks[i].file == file) return true;
            return false;
        }

        // ------------------------------------------------------------ text blocks (rich text)

        /// <summary>Two lines: the NASA DATA tag, then file, retrieved date and "not live".</summary>
        public static string Tag(string file)
        {
            NasaSource s = Get(file);
            if (s == null) return "<color=" + Amber + "><b>NASA DATA</b></color>  " + file + "  (not listed in data_sources.csv)";
            return "<color=" + Cyan + "><b>" + (s.IsDerived ? "NASA DATA (derived)" : "NASA DATA") + "</b></color>  " + s.title + "\n<color=" + Grey + ">" + s.file +
                   "  (snapshot retrieved " + s.retrieved + ", not live)</color>";
        }

        /// <summary>Tag plus "why NASA cares" and the source_url.</summary>
        public static string Card(string file)
        {
            NasaSource s = Get(file);
            if (s == null) return Tag(file);
            return Tag(file) + "\n<b>Why NASA cares:</b> " + s.why + "\n<color=" + Grey + ">Source: " + s.sourceUrl + "</color>";
        }

        static string CompactCard(string file)
        {
            NasaSource s = Get(file);
            if (s == null) return Tag(file);
            return "<color=" + Cyan + "><b>NASA DATA</b></color>  " + s.title + " <color=" + Grey + ">" + s.file + ", retrieved " + s.retrieved + ", not live, " + s.sourceUrl +
                   "</color>\n  <b>Why NASA cares:</b> " + s.why;
        }

        /// <summary>Debrief block: every dataset marked for this mission, with the real values that were shown.</summary>
        public static string DebriefBlock(string mission)
        {
            if (mission == MissionIds.M1) EnsureM1Facts();
            if (mission == MissionIds.Final) EnsureFinalFacts();
            var seen = new List<string>();
            var sb = new StringBuilder();
            for (int i = 0; i < _uses.Count; i++)
            {
                Use u = _uses[i];
                if (u.mission != mission) continue;
                if (!seen.Contains(u.file))
                {
                    seen.Add(u.file);
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(Card(u.file));
                }
                if (!string.IsNullOrEmpty(u.fact)) sb.Append("\n  <b>").Append(u.fact).Append("</b>");
            }
            if (sb.Length == 0)
                return "<color=" + Cyan + "><b>NASA DATA</b></color>  No NASA CSV value drives this mission. The concept comes from the NASA source above; reliabilities and masses are [game balance].";
            if (AnyFallbackFor(seen))
                sb.Append("\n<color=" + Amber + ">Some values in this mission came from FALLBACK data. See Data and Sources.</color>");
            return sb.ToString();
        }

        static bool AnyFallbackFor(List<string> files)
        {
            for (int i = 0; i < files.Count; i++) if (HasFallback(files[i])) return true;
            return false;
        }

        /// <summary>End-card list of the datasets that shaped this playthrough.</summary>
        public static string PlaythroughList()
        {
            var seen = new List<string>();
            var sb = new StringBuilder();
            for (int i = 0; i < _uses.Count; i++)
            {
                if (seen.Contains(_uses[i].file)) continue;
                seen.Add(_uses[i].file);
                NasaSource s = Get(_uses[i].file);
                string missions = MissionsFor(_uses[i].file);
                sb.Append("<color=" + Cyan + ">•</color> ").Append(s != null ? s.title : _uses[i].file).Append("  <color=" + Grey + ">")
                  .Append(_uses[i].file).Append(", retrieved ").Append(s != null ? s.retrieved : "?").Append(", used in ").Append(missions).Append("</color>\n");
            }
            if (sb.Length == 0) sb.Append("No NASA dataset was recorded in this run.\n");
            for (int i = 0; i < _fallbacks.Count; i++)
                sb.Append("<color=" + Amber + ">Fallback used: ").Append(_fallbacks[i].file).Append(" (").Append(_fallbacks[i].reason).Append(")</color>\n");
            return sb.ToString().TrimEnd('\n');
        }

        static string MissionsFor(string file)
        {
            var m = new List<string>();
            for (int i = 0; i < _uses.Count; i++) if (_uses[i].file == file && !m.Contains(_uses[i].mission)) m.Add(_uses[i].mission);
            return string.Join(", ", m.ToArray());
        }

        // ------------------------------------------------------------ real values for debriefs with no live data hook

        static void EnsureM1Facts()
        {
            Dictionary<string, string> thrust = Row("parts_library", "id", "sls_b1_thrust"), mass = Row("parts_library", "id", "sls_b1_liftoff_mass");
            if (thrust == null || mass == null) { ReportFallback("parts_library.csv", "SLS rows missing"); return; }
            double n, kg;
            if (!double.TryParse(thrust["kg_or_n_derived"], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out n)) return;
            if (!double.TryParse(mass["kg_or_n_derived"], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out kg) || kg <= 0) return;
            Mark("parts_library.csv", MissionIds.M1,
                "SLS Block 1: thrust " + n.ToString("N0") + " N / liftoff mass " + kg.ToString("N0") + " kg = " + (n / kg).ToString("0.0") + " m/s2 (derived, a = F / m)");
        }

        static void EnsureFinalFacts()
        {
            Dictionary<string, string> r = Row("dsn_notes", "id", "dsn_complexes");
            if (r == null) { ReportFallback("dsn_notes.csv", "dsn_complexes row missing"); return; }
            Mark("dsn_notes.csv", MissionIds.Final, "Deep Space Network: " + r["number"] + " " + r["unit"] + " (Goldstone, Madrid, Canberra), so contact never drops as Earth turns");
        }

        // ------------------------------------------------------------ CSV

        public static Dictionary<string, string> Row(string resource, string col, string value)
        {
            TextAsset a = Resources.Load<TextAsset>("LastSignal/" + resource);
            if (a == null) return null;
            List<Dictionary<string, string>> rows = Parse(a.text);
            for (int i = 0; i < rows.Count; i++) { string v; if (rows[i].TryGetValue(col, out v) && v == value) return rows[i]; }
            return null;
        }

        static List<NasaSource> Load()
        {
            var list = new List<NasaSource>();
            TextAsset a = Resources.Load<TextAsset>("LastSignal/data_sources");
            if (a == null) { Debug.LogError("[NasaData] data_sources.csv missing from Resources/LastSignal"); return list; }
            foreach (Dictionary<string, string> r in Parse(a.text))
            {
                list.Add(new NasaSource
                {
                    file = G(r, "file"), title = G(r, "title"), sourceUrl = G(r, "source_url"), retrieved = G(r, "retrieved"),
                    kind = G(r, "kind"), usedBy = G(r, "used_by"), why = G(r, "why_nasa_cares")
                });
            }
            return list;
        }

        static string G(Dictionary<string, string> r, string k) { string v; return r.TryGetValue(k, out v) ? v : ""; }

        public static List<Dictionary<string, string>> Parse(string text)
        {
            var rows = new List<Dictionary<string, string>>();
            string[] head = null;
            foreach (string line in text.Replace("\r", "").Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                string[] f = Split(line);
                if (head == null) { head = f; continue; }
                var d = new Dictionary<string, string>();
                for (int i = 0; i < head.Length && i < f.Length; i++) d[head[i].Trim()] = f[i];
                rows.Add(d);
            }
            return rows;
        }

        static string[] Split(string line)
        {
            var res = new List<string>(); var sb = new StringBuilder(); bool q = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (q)
                {
                    if (c == '"') { if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; } else q = false; }
                    else sb.Append(c);
                }
                else if (c == '"') q = true;
                else if (c == ',') { res.Add(sb.ToString()); sb.Length = 0; }
                else sb.Append(c);
            }
            res.Add(sb.ToString());
            return res.ToArray();
        }
    }
}
