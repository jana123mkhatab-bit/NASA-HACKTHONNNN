using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace LastSignal
{
    /// <summary>Tiny CSV reader for the NASA data tables in Data/Resources/LastSignal. Quoted fields supported.</summary>
    public class DataTable
    {
        public readonly List<Dictionary<string, string>> Rows = new List<Dictionary<string, string>>();

        public static DataTable Load(string name)
        {
            var t = new DataTable();
            var asset = Resources.Load<TextAsset>("LastSignal/" + name);
            if (asset == null) { Debug.LogError("[DataTable] missing CSV: " + name); return t; }
            var lines = asset.text.Replace("\r", "").Split('\n');
            string[] head = null;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var f = Split(line);
                if (head == null) { head = f; continue; }
                var d = new Dictionary<string, string>();
                for (int i = 0; i < head.Length && i < f.Length; i++) d[head[i]] = f[i];
                t.Rows.Add(d);
            }
            return t;
        }

        public Dictionary<string, string> Find(string col, string value) =>
            Rows.Find(r => r.TryGetValue(col, out var v) && v == value);

        public static float F(Dictionary<string, string> row, string col) =>
            row != null && row.TryGetValue(col, out var v) &&
            float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : 0f;

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
