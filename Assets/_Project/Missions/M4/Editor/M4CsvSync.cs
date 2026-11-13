using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.M4.EditorTools
{
    /// <summary>
    /// Keeps M4's runtime copy of the exoplanet CSV in sync with person 6's file in Assets/_Project/Data.
    /// Any Data/*exoplanet*.csv (exoplanets.csv preferred) is copied to Missions/M4/Resources/LastSignal/M4/exoplanets.csv
    /// so it loads in builds via Resources (no network calls). Runs on import, or from the menu.
    /// </summary>
    public class M4CsvSync : AssetPostprocessor
    {
        const string DataDir = "Assets/_Project/Data";
        const string TargetDir = "Assets/_Project/Missions/M4/Resources/LastSignal/M4";
        const string Target = TargetDir + "/exoplanets.csv";

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var p in imported)
                if (IsSource(p)) { EditorApplication.delayCall += () => Sync(false); return; }
        }

        static bool IsSource(string p) =>
            p.StartsWith(DataDir + "/", StringComparison.OrdinalIgnoreCase) &&
            p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) &&
            Path.GetFileName(p).IndexOf("exoplanet", StringComparison.OrdinalIgnoreCase) >= 0;

        [MenuItem("LastSignal/M4/Sync exoplanet CSV from Data")]
        static void SyncMenu() => Sync(true);

        static void Sync(bool verbose)
        {
            if (!Directory.Exists(DataDir)) { if (verbose) Debug.LogWarning("[M4] " + DataDir + " not found."); return; }
            string src = null;
            foreach (var f in Directory.GetFiles(DataDir, "*.csv", SearchOption.AllDirectories))
            {
                var path = f.Replace('\\', '/');
                if (!IsSource(path)) continue;
                if (src == null || Path.GetFileName(path).Equals("exoplanets.csv", StringComparison.OrdinalIgnoreCase)) src = path;
            }
            if (src == null) { if (verbose) Debug.LogWarning("[M4] No Data/*exoplanet*.csv yet. M4 uses its fallback CSV."); return; }
            Directory.CreateDirectory(TargetDir);
            if (File.Exists(Target) && File.ReadAllText(Target) == File.ReadAllText(src)) { if (verbose) Debug.Log("[M4] Exoplanet CSV already in sync."); return; }
            File.Copy(src, Target, true);
            AssetDatabase.ImportAsset(Target);
            Debug.Log("[M4] Synced " + src + " -> " + Target);
        }
    }
}
