using System;
using System.Collections.Generic;
using System.Globalization;

namespace LastSignal.M3
{
    /// <summary>One link of the signal path, for example ship -> relay.</summary>
    public class SignalLink
    {
        public string id;
        public string from;
        public string to;
        public double distanceKm;
        public double oneWaySeconds;
        public string epoch;
        public string source;
    }

    /// <summary>
    /// Reads the Signal Path CSV (distances and one-way light times from the JPL Horizons System, prepared by person 6).
    /// No values are hard-coded: the HUD shows whatever the CSV says.
    ///
    /// Expected columns (header row, any order, extra columns ignored, lines starting with # are comments):
    ///     link, from, to, epoch_utc, distance_km | distance_au, one_way_light_time_s | one_way_light_time_min, source
    /// Required links: "ship_relay" and "relay_earth". If a light time is missing it is computed as distance / c.
    /// </summary>
    public class SignalPathData
    {
        public const string ShipRelay = "ship_relay";
        public const string RelayEarth = "relay_earth";
        public const double SpeedOfLightKmPerSecond = 299792.458;
        public const double KmPerAU = 149597870.7;

        public readonly List<SignalLink> links = new List<SignalLink>();
        /// <summary>Name of the file the data came from.</summary>
        public string fileName;
        /// <summary>True if the rows say SAMPLE or PLACEHOLDER (not yet real Horizons data).</summary>
        public bool isSample;
        /// <summary>Problems found while parsing (empty if fine).</summary>
        public readonly List<string> errors = new List<string>();

        public bool IsValid { get { return Get(ShipRelay) != null && Get(RelayEarth) != null; } }

        public SignalLink Get(string id)
        {
            for (int i = 0; i < links.Count; i++)
            {
                if (string.Equals(links[i].id, id, StringComparison.OrdinalIgnoreCase)) return links[i];
            }
            return null;
        }

        /// <summary>Ship -> relay -> Earth one-way delay in seconds, or -1 if a link is missing.</summary>
        public double ShipToEarthSeconds
        {
            get
            {
                SignalLink a = Get(ShipRelay);
                SignalLink b = Get(RelayEarth);
                if (a == null || b == null) return -1;
                return a.oneWaySeconds + b.oneWaySeconds;
            }
        }

        public static double LightSecondsForKm(double km)
        {
            return km / SpeedOfLightKmPerSecond;
        }

        /// <summary>"8 h 12 m", "12 m 30 s" or "3.2 s".</summary>
        public static string FormatDelay(double seconds)
        {
            if (seconds < 0) return "?";
            if (seconds < 60) return seconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";
            long total = (long)Math.Round(seconds);
            long h = total / 3600;
            long m = (total % 3600) / 60;
            long s = total % 60;
            if (h > 0) return h + " h " + m.ToString("00") + " m";
            return m + " m " + s.ToString("00") + " s";
        }

        public static SignalPathData Parse(string csv, string fileName = "")
        {
            SignalPathData data = new SignalPathData();
            data.fileName = fileName;
            if (string.IsNullOrEmpty(csv))
            {
                data.errors.Add("The CSV is empty.");
                return data;
            }

            string[] lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            Dictionary<string, int> col = null;
            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                List<string> cells = SplitCsvLine(line);
                if (col == null)
                {
                    col = new Dictionary<string, int>();
                    for (int c = 0; c < cells.Count; c++) col[cells[c].Trim().ToLowerInvariant()] = c;
                    if (Find(col, "link", "segment", "id") < 0)
                    {
                        data.errors.Add("No 'link' column in the header.");
                        return data;
                    }
                    continue;
                }

                SignalLink link = new SignalLink();
                link.id = Cell(cells, Find(col, "link", "segment", "id"));
                if (string.IsNullOrEmpty(link.id)) continue;
                link.from = Cell(cells, Find(col, "from"));
                link.to = Cell(cells, Find(col, "to"));
                link.epoch = Cell(cells, Find(col, "epoch_utc", "epoch", "date", "date_utc"));
                link.source = Cell(cells, Find(col, "source", "note", "notes"));

                double km = Number(cells, Find(col, "distance_km", "range_km", "delta_km"));
                if (double.IsNaN(km))
                {
                    double au = Number(cells, Find(col, "distance_au", "delta_au", "delta", "range_au"));
                    if (!double.IsNaN(au)) km = au * KmPerAU;
                }
                double lt = Number(cells, Find(col, "one_way_light_time_s", "light_time_s", "one_way_lt_s"));
                if (double.IsNaN(lt))
                {
                    double ltMin = Number(cells, Find(col, "one_way_light_time_min", "light_time_min", "1-way_lt", "one_way_lt_min"));
                    if (!double.IsNaN(ltMin)) lt = ltMin * 60.0;
                }

                if (double.IsNaN(lt) && !double.IsNaN(km)) lt = LightSecondsForKm(km);
                if (double.IsNaN(km) && !double.IsNaN(lt)) km = lt * SpeedOfLightKmPerSecond;
                if (double.IsNaN(lt))
                {
                    data.errors.Add("Row '" + link.id + "' has no distance and no light time.");
                    continue;
                }
                link.distanceKm = km;
                link.oneWaySeconds = lt;

                string src = (link.source ?? "").ToUpperInvariant();
                if (src.Contains("SAMPLE") || src.Contains("PLACEHOLDER")) data.isSample = true;
                data.links.Add(link);
            }

            if (data.Get(ShipRelay) == null) data.errors.Add("Missing link 'ship_relay'.");
            if (data.Get(RelayEarth) == null) data.errors.Add("Missing link 'relay_earth'.");
            return data;
        }

        static int Find(Dictionary<string, int> col, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                int idx;
                if (col.TryGetValue(names[i], out idx)) return idx;
            }
            return -1;
        }

        static string Cell(List<string> cells, int index)
        {
            if (index < 0 || index >= cells.Count) return "";
            return cells[index].Trim();
        }

        static double Number(List<string> cells, int index)
        {
            string s = Cell(cells, index);
            if (s.Length == 0) return double.NaN;
            double v;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
            return double.NaN;
        }

        /// <summary>Splits one CSV line, honouring double quotes.</summary>
        public static List<string> SplitCsvLine(string line)
        {
            List<string> cells = new List<string>();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else quoted = !quoted;
                }
                else if (ch == ',' && !quoted)
                {
                    cells.Add(sb.ToString());
                    sb.Length = 0;
                }
                else
                {
                    sb.Append(ch);
                }
            }
            cells.Add(sb.ToString());
            return cells;
        }
    }
}
