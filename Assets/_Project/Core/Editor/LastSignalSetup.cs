using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>One-click setup: menu LastSignal > Setup, or batch mode via -executeMethod.</summary>
    public static class LastSignalSetup
    {
        [MenuItem("LastSignal/Setup/Run All (content, URP, materials, scenes)")]
        public static void RunAll()
        {
            AssetDatabase.Refresh();
            SetupContent.Run();
            SetupVisuals.SetupPipeline();
            SetupVisuals.BuildMaterials();
            MakePrefabs();
            SetupScenes.Run();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LastSignal] Setup complete: scenes in Assets/_Project/Scenes, all added to Build Settings.");
        }

        [MenuItem("LastSignal/Setup/1 Content (Pip, log, debriefs)")] static void M1() { SetupContent.Run(); }
        [MenuItem("LastSignal/Setup/2 URP + post-processing")] static void M2() { SetupVisuals.SetupPipeline(); }
        [MenuItem("LastSignal/Setup/3 Materials")] static void M3() { SetupVisuals.BuildMaterials(); }
        [MenuItem("LastSignal/Setup/4 Scenes + Build Settings")] static void M4() { SetupVisuals.BuildMaterials(); SetupScenes.Run(); }

        /// <summary>Batch entry: Unity.exe -batchmode -quit -executeMethod LastSignal.EditorTools.LastSignalSetup.RunAllBatch</summary>
        public static void RunAllBatch()
        {
            try { RunAll(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void MakePrefabs()
        {
            Directory.CreateDirectory("Assets/_Project/UI");
            var go = new GameObject("RiskGauge", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(460, 110);
            go.GetComponent<UnityEngine.UI.Image>().color = UIFactory.Panel;
            var g = go.AddComponent<RiskGaugeWidget>();
            g.value = 0.5f;
            g.Build();
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/_Project/UI/RiskGauge.prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
