using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastSignal.EditorTools
{
    /// <summary>URP asset with max-quality settings, post-processing profile, SSAO, and PBR materials.</summary>
    public static class SetupVisuals
    {
        const string Settings = "Assets/_Project/Core/Settings";
        const string MatDir = "Assets/_Project/Art/Materials";
        const string Tex = "Assets/_Project/Art/Textures/";
        public const string VolumePath = Settings + "/GlobalVolumeProfile.asset";

        public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        // ---------------------------------------------------------------- URP

        public static void SetupPipeline()
        {
            Directory.CreateDirectory(Settings);
            string rendPath = Settings + "/LastSignalRenderer.asset";
            string urpPath = Settings + "/LastSignalURP.asset";

            var rend = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendPath);
            if (rend == null)
            {
                rend = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rend, rendPath);
                TryAddSsao(rend);
            }

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(rend);
                AssetDatabase.CreateAsset(urp, urpPath);
            }
            urp.supportsHDR = true;
            urp.msaaSampleCount = 4;
            urp.shadowDistance = 400f;
            urp.shadowCascadeCount = 4;
            urp.mainLightShadowmapResolution = 4096;
            urp.colorGradingMode = ColorGradingMode.HighDynamicRange;
            urp.colorGradingLutSize = 32;
            EditorUtility.SetDirty(urp);

            GraphicsSettings.defaultRenderPipeline = urp;
            int cur = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            }
            QualitySettings.SetQualityLevel(Mathf.Clamp(cur, 0, QualitySettings.names.Length - 1), false);

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.productName = "The Last Known Signal";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;

            SetupVolumeProfile();
            AssetDatabase.SaveAssets();
        }

        static void TryAddSsao(UniversalRendererData rend)
        {
            try
            {
                var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "SSAO";
                AssetDatabase.AddObjectToAsset(ssao, rend);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out _, out long id);
                var so = new SerializedObject(rend);
                var feats = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                feats.arraySize++;
                feats.GetArrayElementAtIndex(feats.arraySize - 1).objectReferenceValue = ssao;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
                so.ApplyModifiedProperties();
            }
            catch (Exception e) { Debug.LogWarning("SSAO feature not added: " + e.Message); }
        }

        static void SetupVolumeProfile()
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(p, VolumePath);
            }
            var bloom = Get<Bloom>(p);
            bloom.threshold.Override(0.95f); bloom.intensity.Override(0.9f); bloom.scatter.Override(0.72f);
            bloom.highQualityFiltering.Override(true);
            var tm = Get<Tonemapping>(p); tm.mode.Override(TonemappingMode.ACES);
            var ca = Get<ColorAdjustments>(p);
            ca.postExposure.Override(0.15f); ca.contrast.Override(14f); ca.saturation.Override(10f);
            var vg = Get<Vignette>(p); vg.intensity.Override(0.28f); vg.smoothness.Override(0.45f);
            var ab = Get<ChromaticAberration>(p); ab.intensity.Override(0.06f);
            var wb = Get<WhiteBalance>(p); wb.temperature.Override(4f);
            EditorUtility.SetDirty(p);
        }

        static T Get<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (!p.TryGet(out T c)) c = p.Add<T>(true);
            c.active = true;
            return c;
        }

        // ---------------------------------------------------------------- materials

        class Mtl { public Color kd = Color.gray; public Color ks = Color.black; public float ns = 10, d = 1; }

        public static void BuildMaterials()
        {
            Directory.CreateDirectory(MatDir);
            Materials.Clear();

            // 1) Generic PBR materials from every .mtl in the model library (buildings, props).
            foreach (var mtlPath in Directory.GetFiles("Assets/_Project/Art/Models", "*.mtl", SearchOption.AllDirectories))
                foreach (var kv in ParseMtl(mtlPath))
                    Materials[kv.Key] = MakeFromMtl(kv.Key, kv.Value);

            // 2) Hand-tuned celestial materials with NASA textures and custom shaders.
            Materials["earth_surface"] = Save("earth_surface", Custom("LastSignal/Earth", m =>
            {
                m.SetTexture("_DayMap", T("earth_daymap.jpg"));
                m.SetTexture("_NightMap", T("earth_nightmap.jpg"));
            }));
            Materials["earth_clouds"] = Save("earth_clouds", Custom("LastSignal/Clouds", m =>
            {
                m.SetTexture("_CloudMap", T("earth_clouds.jpg"));
                m.renderQueue = 3001;
            }));
            Materials["earth_atmosphere"] = Save("earth_atmosphere", Custom("LastSignal/Atmosphere", m =>
            {
                m.SetColor("_Color", new Color(0.25f, 0.55f, 1f, 1f));
                m.SetFloat("_Power", 3.2f); m.SetFloat("_Intensity", 1.8f);
                m.renderQueue = 3002;
            }));
            Materials["sun_surface"] = Save("sun_surface", Unlit(T("sun.jpg"), new Color(2.4f, 1.7f, 0.9f, 1f)));
            Materials["sun_corona"] = Save("sun_corona", Custom("LastSignal/Atmosphere", m =>
            {
                m.SetColor("_Color", new Color(1f, 0.55f, 0.15f, 1f));
                m.SetFloat("_Power", 1.6f); m.SetFloat("_Intensity", 4f);
                m.renderQueue = 3003;
            }));
            var moon = LitMat("moon_surface", Color.white, 0.04f, 0f);
            moon.SetTexture("_BaseMap", T("moon.jpg"));
            Materials["moon_surface"] = Save("moon_surface", moon);
            var sky = Unlit(T("stars_milkyway.jpg"), new Color(1.1f, 1.1f, 1.2f, 1f));
            sky.SetFloat("_Cull", 0f); // render the inside of the sphere
            Materials["skybox_material"] = Save("skybox_material", sky);

            // Ground helpers
            Materials["__grass"] = Save("__grass", LitMat("__grass", new Color(0.18f, 0.32f, 0.13f), 0.1f, 0f));
            Materials["__apron"] = Save("__apron", LitMat("__apron", new Color(0.55f, 0.55f, 0.52f), 0.15f, 0f));

            // Procedural sky for ground scenes.
            var proc = new Material(Shader.Find("Skybox/Procedural"));
            proc.SetFloat("_SunSize", 0.06f); proc.SetFloat("_AtmosphereThickness", 1.1f);
            proc.SetColor("_SkyTint", new Color(0.5f, 0.6f, 0.8f));
            Materials["__sky_ground"] = Save("__sky_ground", proc);

            AssetDatabase.SaveAssets();
        }

        static Texture2D T(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + file);

        static Dictionary<string, Mtl> ParseMtl(string path)
        {
            var d = new Dictionary<string, Mtl>();
            Mtl cur = null;
            foreach (var raw in File.ReadAllLines(path))
            {
                var t = raw.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (t.Length < 2) continue;
                switch (t[0])
                {
                    case "newmtl": cur = new Mtl(); d[t[1]] = cur; break;
                    case "Kd": if (cur != null && t.Length >= 4) cur.kd = new Color(F(t[1]), F(t[2]), F(t[3])); break;
                    case "Ks": if (cur != null && t.Length >= 4) cur.ks = new Color(F(t[1]), F(t[2]), F(t[3])); break;
                    case "Ns": if (cur != null) cur.ns = F(t[1]); break;
                    case "d": if (cur != null) cur.d = F(t[1]); break;
                }
            }
            return d;
        }

        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        static Material MakeFromMtl(string name, Mtl m)
        {
            bool metal = name.Contains("metal") || name.Contains("steel") || name.Contains("lamp");
            bool glass = name.StartsWith("glass");
            float smooth = glass ? 0.93f : Mathf.Clamp01(m.ns / 110f + (metal ? 0.25f : 0f));
            var mat = LitMat(name, m.kd, smooth, metal ? 0.8f : 0f);
            if (glass || m.d < 0.99f)
            {
                var c = m.kd; c.a = glass ? 0.45f : m.d;
                mat.SetColor("_BaseColor", c);
                MakeTransparent(mat);
            }
            if (name.Contains("antenna_red"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1.5f, 0.1f, 0.1f));
            }
            return Save(name, mat);
        }

        static Material LitMat(string name, Color c, float smooth, float metallic)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metallic);
            return m;
        }

        static Material Unlit(Texture tex, Color tint)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", tint);
            return m;
        }

        static Material Custom(string shader, Action<Material> setup)
        {
            var s = Shader.Find(shader);
            if (s == null) throw new Exception("Shader not found (compile error?): " + shader);
            var m = new Material(s);
            setup(m);
            return m;
        }

        static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
        }

        static Material Save(string name, Material m)
        {
            string path = MatDir + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(m, existing);
                UnityEngine.Object.DestroyImmediate(m);
                return existing;
            }
            m.name = name;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
