#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Tools > Last Horizon > Setup Project: creates and assigns a URP asset, registers the custom shaders, sets linear colour space.
/// Runs automatically once on first open.</summary>
[InitializeOnLoad]
public static class SpaceProjectSetup
{
    const string Key = "LastHorizon.SetupDone.v1";
    static SpaceProjectSetup() { EditorApplication.delayCall += () => { if (!EditorPrefs.GetBool(Key + Application.dataPath, false) && GraphicsSettings.currentRenderPipeline == null) { Run(false); } }; }

    [MenuItem("Tools/Last Horizon/Setup Project")]
    public static void RunMenu() { Run(true); }

    static void Run(bool verbose)
    {
        try
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Directory.CreateDirectory("Assets/Settings");
            const string rpPath = "Assets/Settings/LastHorizonURP.asset", rdPath = "Assets/Settings/LastHorizonRenderer.asset";
            var rpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);
            if (rpAsset == null)
            {
                var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rd, rdPath);
                rpAsset = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(rpAsset, rpPath);
            }
            rpAsset.supportsHDR = true; rpAsset.msaaSampleCount = 4; rpAsset.renderScale = 1f; rpAsset.shadowDistance = 220f;
            rpAsset.supportsCameraDepthTexture = true; rpAsset.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(rpAsset);
            GraphicsSettings.defaultRenderPipeline = rpAsset;
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = rpAsset; }

            // make sure the shaders that are only found by name at runtime end up in builds
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(gs); var arr = so.FindProperty("m_AlwaysIncludedShaders");
            var need = new List<string> { "Space/Sky", "Space/Planet", "Space/Clouds", "Space/Glow", "Space/SunSurface", "Space/Terrain", "Skybox/Procedural", "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit" };
            foreach (var n in need)
            {
                var sh = Shader.Find(n); if (sh == null) continue;
                bool has = false; for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true;
                if (!has) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; }
            }
            so.ApplyModifiedProperties();

            // create the (optional) scene: everything is bootstrapped from code, the scene only needs a camera
            Directory.CreateDirectory("Assets/Scenes");
            var scenePath = "Assets/Scenes/Main.unity";
            if (!File.Exists(scenePath))
            {
                var sc = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
                var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc, scenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != scenePath) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            PlayerSettings.productName = "The Last Horizon"; PlayerSettings.companyName = "Space Apps Team";
            AssetDatabase.SaveAssets(); EditorPrefs.SetBool(Key + Application.dataPath, true);
            Debug.Log("[Last Horizon] Project set up: URP asset assigned, linear colour space, shaders registered. Press Play!");
            if (verbose) EditorUtility.DisplayDialog("The Last Horizon", "Project set up.\n\nPress Play in the Main scene.", "OK");
        }
        catch (System.Exception e) { Debug.LogError("[Last Horizon] Setup failed: " + e); }
    }
}

/// <summary>Import settings for the generated OBJ models and 4K textures.</summary>
public class SpaceAssetPostprocessor : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.Contains("/Resources/Models/")) return;
        var mi = (ModelImporter)assetImporter;
        mi.globalScale = 1f; mi.useFileScale = false; mi.importNormals = ModelImporterNormals.Import;
        mi.indexFormat = ModelImporterIndexFormat.UInt32; mi.isReadable = true; mi.meshCompression = ModelImporterMeshCompression.Off;
        mi.importCameras = false; mi.importLights = false; mi.materialImportMode = ModelImporterMaterialImportMode.None;
        mi.animationType = ModelImporterAnimationType.None; mi.importBlendShapes = false; mi.weldVertices = false;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/Textures/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.maxTextureSize = 8192; ti.mipmapEnabled = true; ti.anisoLevel = 16; ti.filterMode = FilterMode.Trilinear; ti.wrapMode = TextureWrapMode.Repeat;
        ti.streamingMipmaps = false; ti.sRGBTexture = true;
        bool big = assetPath.Contains("earth_daymap") || assetPath.Contains("moon");
        ti.isReadable = big;                         // PlanetGeo samples these on the CPU
        ti.textureCompression = big ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
    }
}
#endif
