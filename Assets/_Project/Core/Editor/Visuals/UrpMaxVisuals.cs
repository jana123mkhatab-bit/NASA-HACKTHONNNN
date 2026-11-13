using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Menus 0b and 4: URP asset at maximum quality, then a post-processing look in every scene.
    /// Only compiled when the Universal RP package is installed (define LS_URP).
    /// </summary>
    public static class UrpMaxVisuals
    {
        const string SettingsDir = "Assets/_Project/Settings";
        const string PostFxDir = "Assets/_Project/Art/PostFX";
        const string ScenesDir = "Assets/_Project/Core/Scenes";

        // ---------------------------------------------------------------- 0b

        [MenuItem("Last Signal/0b - Configure URP asset for maximum visuals", false, 1)]
        public static void ConfigureUrpAsset()
        {
            EnsureFolder(SettingsDir);

            UniversalRenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null)
            {
                UniversalRendererData rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, SettingsDir + "/LastSignal_URP_Renderer.asset");
                asset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(asset, SettingsDir + "/LastSignal_URP.asset");
                GraphicsSettings.defaultRenderPipeline = asset;
                int current = QualitySettings.GetQualityLevel();
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = asset;
                }
                QualitySettings.SetQualityLevel(current, false);
                Debug.Log("Last Signal: created and assigned a new URP asset in " + SettingsDir);
            }

            SerializedObject so = new SerializedObject(asset);
            List<string> skipped = new List<string>();
            SetBool(so, "m_SupportsHDR", true, skipped);
            SetInt(so, "m_MSAA", 4, skipped);
            SetFloat(so, "m_RenderScale", 1f, skipped);
            SetInt(so, "m_MainLightShadowmapResolution", 4096, skipped);
            SetInt(so, "m_ShadowCascadeCount", 4, skipped);
            SetFloat(so, "m_ShadowDistance", 150f, skipped);
            SetInt(so, "m_SoftShadowQuality", 3, skipped);
            SetInt(so, "m_ColorGradingMode", 1, skipped);   // High Dynamic Range grading
            SetInt(so, "m_HDRColorBufferPrecision", 1, skipped); // 64 bit, less banding in glow
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            if (skipped.Count > 0)
                Debug.LogWarning("Last Signal: these URP settings were not found in this URP version and were skipped (set them by hand on the URP asset): " + string.Join(", ", skipped.ToArray()));
            Debug.Log("Last Signal: URP asset configured (HDR, 4x MSAA, 4096 shadows, soft shadows, HDR grading). Now run menu 1, 2, 3 and 4 if you have not yet.");
        }

        static void SetBool(SerializedObject so, string name, bool v, List<string> skipped)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) { skipped.Add(name); return; }
            p.boolValue = v;
        }

        static void SetInt(SerializedObject so, string name, int v, List<string> skipped)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) { skipped.Add(name); return; }
            p.intValue = v;
        }

        static void SetFloat(SerializedObject so, string name, float v, List<string> skipped)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) { skipped.Add(name); return; }
            p.floatValue = v;
        }

        // ---------------------------------------------------------------- 4

        [MenuItem("Last Signal/4 - Add post-processing look to all scenes", false, 40)]
        public static void AddPostProcessingToScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(PostFxDir);

            VolumeProfile profile = GetOrCreateProfile();
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { ScenesDir });
            int count = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                SetupScene(profile);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                count++;
            }
            Debug.Log("Last Signal: post-processing added to " + count + " scenes. View it in the Game view, not the Scene view.");
        }

        static VolumeProfile GetOrCreateProfile()
        {
            string path = PostFxDir + "/SpaceLook.asset";
            VolumeProfile existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null) return existing;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            Tonemapping tone = profile.Add<Tonemapping>(false);
            tone.mode.Override(TonemappingMode.ACES);
            AssetDatabase.AddObjectToAsset(tone, profile);

            Bloom bloom = profile.Add<Bloom>(false);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.7f);
            bloom.highQualityFiltering.Override(true);
            AssetDatabase.AddObjectToAsset(bloom, profile);

            Vignette vignette = profile.Add<Vignette>(false);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);
            AssetDatabase.AddObjectToAsset(vignette, profile);

            ColorAdjustments color = profile.Add<ColorAdjustments>(false);
            color.contrast.Override(10f);
            color.saturation.Override(10f);
            AssetDatabase.AddObjectToAsset(color, profile);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static void SetupScene(VolumeProfile profile)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.allowHDR = true;
                cam.allowMSAA = true;
                UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
            }

            Volume volume = Object.FindFirstObjectByType<Volume>();
            if (volume == null)
            {
                GameObject go = new GameObject("Global Volume");
                volume = go.AddComponent<Volume>();
            }
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    lights[i].shadows = LightShadows.Soft;
                    lights[i].intensity = 1.3f;
                    lights[i].color = new Color(1f, 0.96f, 0.9f);
                }
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.5f;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
