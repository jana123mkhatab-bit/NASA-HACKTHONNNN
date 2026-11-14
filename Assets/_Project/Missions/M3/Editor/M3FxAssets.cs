using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.M3.EditorTools
{
    /// <summary>
    /// Copies the existing Earth/Moon/Sun materials (Art/Materials, shaders in Art/Shaders) into Missions/M3/Resources/M3Fx so the
    /// player build includes them and M3Fx can load them at run time. Safe to run again. Batch:
    /// -executeMethod LastSignal.M3.EditorTools.M3FxAssets.Prepare
    /// </summary>
    public static class M3FxAssets
    {
        static readonly string[] Names = { "earth_surface", "earth_clouds", "earth_atmosphere", "moon_surface", "sun_surface", "sun_corona" };
        const string From = "Assets/_Project/Art/Materials/";
        const string To = "Assets/_Project/Missions/M3/Resources/M3Fx/";

        [MenuItem("LastSignal/M3/Prepare Fx materials")]
        public static void Prepare()
        {
            Directory.CreateDirectory(To);
            AssetDatabase.Refresh();
            int copied = 0;
            foreach (string n in Names)
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(To + n + ".mat") != null) continue;
                if (AssetDatabase.CopyAsset(From + n + ".mat", To + n + ".mat")) copied++;
                else Debug.LogError("[M3Fx] could not copy " + From + n + ".mat");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M3Fx] materials ready, copied " + copied);
        }
    }
}
