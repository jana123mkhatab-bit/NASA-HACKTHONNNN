using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LastSignal.M3
{
    /// <summary>A parsed CSV: header row plus rows looked up by their first column.</summary>
    public class CsvTable
    {
        public readonly List<string> header = new List<string>();
        public readonly List<List<string>> rows = new List<List<string>>();

        public static CsvTable Parse(string text)
        {
            CsvTable t = new CsvTable();
            if (string.IsNullOrEmpty(text)) return t;
            string[] lines = text.Replace("\r", "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Trim().Length == 0 || line.StartsWith("#")) continue;
                List<string> cells = SignalPathData.SplitCsvLine(line);
                if (t.header.Count == 0)
                {
                    for (int c = 0; c < cells.Count; c++) t.header.Add(cells[c].Trim());
                }
                else t.rows.Add(cells);
            }
            return t;
        }

        /// <summary>Loads Resources/&lt;path&gt;.csv. Returns an empty table if missing.</summary>
        public static CsvTable Load(string resourcePath)
        {
            TextAsset a = Resources.Load<TextAsset>(resourcePath);
            if (a == null)
            {
                Debug.LogWarning("[M3] Missing CSV Resources/" + resourcePath);
                return new CsvTable();
            }
            return Parse(a.text);
        }

        public int Column(string name)
        {
            return header.IndexOf(name);
        }

        public List<string> Row(string key)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Count > 0 && rows[i][0].Trim() == key) return rows[i];
            }
            return null;
        }

        public string Get(string key, string column)
        {
            List<string> r = Row(key);
            int c = Column(column);
            if (r == null || c < 0 || c >= r.Count) return null;
            return r[c].Trim();
        }

        public bool TryGetDouble(string key, string column, out double value)
        {
            return double.TryParse(Get(key, column), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }

    /// <summary>
    /// The deep-space sector layout. One rule is used for Earth, the Moon and the distance between them:
    /// 1 sector metre = KmPerUnit km [game balance compression]. Directions and distances come from the
    /// JPL Horizons snapshot (2026-11-13, not live); radii from the NSSDC fact sheets (body_radii.csv).
    /// The Sun is drawn in its real direction but not at its real distance (it would be 6 million metres away).
    /// </summary>
    public static class SectorLayout
    {
        public const float KmPerUnit = 25f;   // [game balance] sector compression
        public const string HorizonsCsv = "LastSignal/horizons_positions_2026-11-13";
        public const string RadiiCsv = "LastSignal/body_radii";
        public const string SitesCsv = "LastSignal/challenge_sites";

        /// <summary>Horizons uses the ecliptic frame (z = ecliptic north); Unity is y-up.</summary>
        public static Vector3 EclipticToUnity(double x, double y, double z)
        {
            return new Vector3((float)x, (float)z, (float)y);
        }

        public static float KmToUnits(double km)
        {
            return (float)(km / KmPerUnit);
        }

        /// <summary>Moon relative to Earth in sector units, from the two heliocentric Horizons rows.</summary>
        public static bool TryMoonOffset(CsvTable horizons, out Vector3 offsetUnits)
        {
            offsetUnits = Vector3.zero;
            double ex, ey, ez, mx, my, mz;
            if (!horizons.TryGetDouble("Earth", "x_km", out ex) || !horizons.TryGetDouble("Earth", "y_km", out ey)
                || !horizons.TryGetDouble("Earth", "z_km", out ez) || !horizons.TryGetDouble("Moon", "x_km", out mx)
                || !horizons.TryGetDouble("Moon", "y_km", out my) || !horizons.TryGetDouble("Moon", "z_km", out mz)) return false;
            offsetUnits = EclipticToUnity(mx - ex, my - ey, mz - ez) / KmPerUnit;
            return true;
        }

        /// <summary>Unit vector from Earth toward the Sun (the Horizons frame is Sun-centred, so it is -Earth).</summary>
        public static bool TrySunDirection(CsvTable horizons, out Vector3 dir, out double earthSunKm)
        {
            dir = Vector3.forward;
            earthSunKm = 0d;
            double ex, ey, ez;
            if (!horizons.TryGetDouble("Earth", "x_km", out ex) || !horizons.TryGetDouble("Earth", "y_km", out ey)
                || !horizons.TryGetDouble("Earth", "z_km", out ez)) return false;
            earthSunKm = System.Math.Sqrt(ex * ex + ey * ey + ez * ez);
            if (earthSunKm <= 0d) return false;
            dir = -EclipticToUnity(ex, ey, ez).normalized;
            return true;
        }

        public static bool TryRadiusKm(CsvTable radii, string body, out double km)
        {
            return radii.TryGetDouble(body, "volumetric_mean_radius_km", out km);
        }

        /// <summary>Angular diameter in degrees of a sphere of the given radius seen from a distance (same units).</summary>
        public static float AngularDiameterDeg(double radius, double distance)
        {
            if (distance <= 0d) return 0f;
            return (float)(2d * System.Math.Atan(radius / distance) * 180d / System.Math.PI);
        }
    }

    /// <summary>One NASA-backed challenge site in the sector (rows of challenge_sites.csv).</summary>
    public class ChallengeSite
    {
        public string id, name, anchor, dataFile, why, sourceUrl, retrieved, balanceNote;
        public Vector3 offset;
        /// <summary>(label, "row.column") pairs pointing into dataFile.</summary>
        public readonly List<KeyValuePair<string, string>> refs = new List<KeyValuePair<string, string>>();
        /// <summary>(label, value) read from the NASA CSV. Value is null if the CSV is missing it.</summary>
        public readonly List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();

        public bool visited;
        public Transform beacon;

        public static List<ChallengeSite> Parse(CsvTable t)
        {
            List<ChallengeSite> list = new List<ChallengeSite>();
            for (int i = 0; i < t.rows.Count; i++)
            {
                string key = t.rows[i][0].Trim();
                ChallengeSite s = new ChallengeSite();
                s.id = key;
                s.name = t.Get(key, "name");
                s.anchor = t.Get(key, "anchor");
                double x, y, z;
                t.TryGetDouble(key, "offset_x", out x);
                t.TryGetDouble(key, "offset_y", out y);
                t.TryGetDouble(key, "offset_z", out z);
                s.offset = new Vector3((float)x, (float)y, (float)z);
                s.dataFile = t.Get(key, "data_file");
                s.why = t.Get(key, "why_nasa_cares");
                s.sourceUrl = t.Get(key, "source_url");
                s.retrieved = t.Get(key, "retrieved");
                s.balanceNote = t.Get(key, "balance_note");
                string vals = t.Get(key, "values") ?? "";
                foreach (string part in vals.Split('|'))
                {
                    int eq = part.LastIndexOf('=');   // labels may contain '=' ("Earth = 1"); refs never do
                    if (eq <= 0) continue;
                    s.refs.Add(new KeyValuePair<string, string>(part.Substring(0, eq).Trim(), part.Substring(eq + 1).Trim()));
                }
                list.Add(s);
            }
            return list;
        }

        /// <summary>Reads "row.column" from a table (the row key may contain spaces; the last '.' splits).</summary>
        public static string Resolve(CsvTable data, string rowDotColumn)
        {
            if (data == null || string.IsNullOrEmpty(rowDotColumn)) return null;
            int dot = rowDotColumn.LastIndexOf('.');
            if (dot <= 0) return null;
            return data.Get(rowDotColumn.Substring(0, dot), rowDotColumn.Substring(dot + 1));
        }

        public void ResolveValues(CsvTable data)
        {
            values.Clear();
            for (int i = 0; i < refs.Count; i++) values.Add(new KeyValuePair<string, string>(refs[i].Key, Resolve(data, refs[i].Value)));
        }

        /// <summary>Loads challenge_sites.csv and fills each site's values from its NASA CSV.</summary>
        public static List<ChallengeSite> LoadAll()
        {
            List<ChallengeSite> sites = Parse(CsvTable.Load(SectorLayout.SitesCsv));
            for (int i = 0; i < sites.Count; i++) sites[i].ResolveValues(CsvTable.Load("LastSignal/" + sites[i].dataFile));
            return sites;
        }
    }
}
