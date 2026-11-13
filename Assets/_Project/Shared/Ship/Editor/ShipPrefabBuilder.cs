using System.IO;
using LastSignal.Ship;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Ship.EditorTools
{
    /// <summary>
    /// Menu: Last Signal > Ship - Build Ship prefab.
    /// Saves Assets/_Project/Shared/Ship/Ship.prefab from ShipFactory (greybox), so M3 and M4 use the same ship.
    /// The "with Aurora model" variant also places the team's ship_aurora model under Visual and hides the greybox body.
    /// </summary>
    public static class ShipPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Shared/Ship/Ship.prefab";

        static readonly string[] ModelCandidates =
        {
            "Assets/_Project/Art/Models/Ship/ship_aurora.obj",
            "Assets/Resources/Models/Custom/ship_aurora.obj"
        };

        [MenuItem("Last Signal/Ship - Build Ship prefab")]
        public static void BuildGreybox()
        {
            Build(false);
        }

        [MenuItem("Last Signal/Ship - Build Ship prefab (with Aurora model)")]
        public static void BuildWithModel()
        {
            Build(true);
        }

        /// <summary>Builds the prefab only if it does not exist yet. Returns the prefab asset.</summary>
        public static GameObject EnsurePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing;
            return Build(false);
        }

        public static GameObject Build(bool withModel)
        {
            GameObject root = ShipFactory.Build("Ship");
            if (withModel)
            {
                GameObject model = null;
                for (int i = 0; i < ModelCandidates.Length && model == null; i++)
                {
                    model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelCandidates[i]);
                }
                if (model != null)
                {
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    inst.name = "Model";
                    inst.transform.SetParent(root.transform.Find("Visual"), false);
                    ShipFactory.HideGreyboxBody(root);
                }
                else
                {
                    Debug.LogWarning("Last Signal: ship_aurora model not found, the prefab uses the greybox body.");
                }
            }

            string dir = Path.GetDirectoryName(PrefabPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir)) Directory.CreateDirectory(dir);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("Last Signal: saved " + PrefabPath + ". See Shared/Ship/SHIP_INTERFACE.md for the API.");
            Selection.activeObject = prefab;
            return prefab;
        }
    }
}
