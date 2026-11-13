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
            urp.maxAdditionalLightsCount = 8;
            // Soft shadows and shadows from the range floodlights have no public setters.
            var uso = new SerializedObject(urp);
            SetInt(uso, "m_SoftShadowsSupported", 1);
            SetInt(uso, "m_AdditionalLightShadowsSupported", 1);
            uso.ApplyModifiedPropertiesWithoutUndo();
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

        static void SetInt(SerializedObject so, string prop, int value)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning("[LastSignal] URP setting not found: " + prop); return; }
            if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value != 0; else p.intValue = value;
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
            // Moody sci-fi grade: glow only on emissive accents, deep but readable shadows.
            bloom.threshold.Override(1.05f); bloom.intensity.Override(0.55f); bloom.scatter.Override(0.7f);
            bloom.highQualityFiltering.Override(true);
            var tm = Get<Tonemapping>(p); tm.mode.Override(TonemappingMode.ACES);
            var ca = Get<ColorAdjustments>(p);
            ca.postExposure.Override(0.25f); ca.contrast.Override(12f); ca.saturation.Override(6f);
            var vg = Get<Vignette>(p); vg.intensity.Override(0.3f); vg.smoothness.Override(0.45f);
            var ab = Get<ChromaticAberration>(p); ab.intensity.Override(0.03f);
            var wb = Get<WhiteBalance>(p); wb.temperature.Override(0f);
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

            // Procedural dusk sky for the ground scenes: the sun sits on the horizon behind the set.
            var proc = new Material(Shader.Find("Skybox/Procedural"));
            proc.SetFloat("_SunSize", 0.035f); proc.SetFloat("_SunSizeConvergence", 4f);
            proc.SetFloat("_AtmosphereThickness", 1.45f); proc.SetFloat("_Exposure", 0.85f);
            proc.SetColor("_SkyTint", new Color(0.36f, 0.46f, 0.78f));
            proc.SetColor("_GroundColor", Palette.Shadow);
            Materials["__sky_ground"] = Save("__sky_ground", proc);

            BuildStageMaterials();
            Materials["st_sky_dusk"] = Save("st_sky_dusk", DuskSky());
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Palette-driven dusk sky: a lat-long gradient (orange horizon band, violet, deep navy zenith)
        /// with a few stars, rendered by Unity's built-in Skybox/Panoramic shader.
        /// </summary>
        static Material DuskSky()
        {
            const string path = "Assets/_Project/Art/Generated/tex_sky_dusk.asset";
            Directory.CreateDirectory("Assets/_Project/Art/Generated");
            const int w = 512, h = 256;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool isNew = tex == null;
            if (isNew) tex = new Texture2D(w, h, TextureFormat.RGBAHalf, false);
            var keys = new[]
            {
                (0.00f, Palette.Shadow), (0.47f, Palette.Fog), (0.50f, Palette.Horizon * 1.25f),
                (0.53f, Palette.Hex("#C0614A")), (0.58f, Palette.Hex("#5B3F63")), (0.68f, Palette.Hex("#2A3558")),
                (0.82f, Palette.SkyTop), (1.00f, Palette.Hex("#0A111E")),
            };
            var rng = new System.Random(11);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = y / (h - 1f);
                Color c = keys[keys.Length - 1].Item2;
                for (int k = 0; k < keys.Length - 1; k++)
                    if (v <= keys[k + 1].Item1)
                    {
                        float t = Mathf.InverseLerp(keys[k].Item1, keys[k + 1].Item1, v);
                        c = Color.Lerp(keys[k].Item2, keys[k + 1].Item2, Mathf.SmoothStep(0f, 1f, t));
                        break;
                    }
                for (int x = 0; x < w; x++)
                {
                    // Brighter glow on one side of the sky, where the sun went down.
                    float az = Mathf.Cos((x / (float)w - 0.25f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    float band = Mathf.Exp(-Mathf.Pow((v - 0.5f) / 0.06f, 2f));
                    Color p = c + Palette.Horizon * (band * az * 0.6f);
                    if (v > 0.62f && rng.NextDouble() < 0.0016) p += Color.white * (0.4f + (float)rng.NextDouble() * 0.8f) * Mathf.InverseLerp(0.62f, 0.85f, v);
                    p.a = 1f;
                    px[y * w + x] = p;
                }
            }
            tex.SetPixels(px);
            tex.wrapModeU = TextureWrapMode.Repeat;
            tex.wrapModeV = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(false);
            if (isNew) AssetDatabase.CreateAsset(tex, path); else EditorUtility.SetDirty(tex);
            var m = new Material(Shader.Find("Skybox/Panoramic"));
            m.SetTexture("_MainTex", tex);
            m.SetFloat("_Mapping", 1f);
            m.SetFloat("_ImageType", 0f);
            m.SetFloat("_Exposure", 1f);
            m.SetFloat("_Rotation", 0f);
            return m;
        }

        // ---------------------------------------------------------------- stage materials (palette)

        static void BuildStageMaterials()
        {
            Stage("st_sand", Palette.Sand, 0.12f, 0f);
            var floor = LitMat("st_floor", Color.white, 0.32f, 0f);
            floor.SetTexture("_BaseMap", ConcreteTexture());
            floor.SetTextureScale("_BaseMap", new Vector2(20f, 4f));
            Materials["st_floor"] = Save("st_floor", floor);
            Stage("st_track", Palette.Wall, 0.3f, 0.1f);
            Stage("st_hangar", Palette.Wall, 0.35f, 0.3f);
            Stage("st_mountain", Palette.Shadow, 0f, 0f);
            Stage("st_ceramic", Palette.Ceramic, 0.55f, 0f);
            Stage("st_steel", Palette.Steel, 0.62f, 0.85f);
            Stage("st_steel_dark", Palette.SteelDark, 0.45f, 0.6f);
            Stage("st_rust", Palette.Rust, 0.45f, 0.05f);
            Stage("st_foil", Palette.Foil, 0.78f, 1f);
            Stage("st_hazard", Palette.Warn * 0.85f, 0.3f, 0f);
            Stage("st_scorch", Palette.Scorch, 0.35f, 0.5f);
            Stage("st_nozzle", Palette.Hex("#3A2E2A"), 0.6f, 0.9f);
            Stage("st_glass", Palette.Ink, 0.95f, 0.3f);

            Glow("st_glow_cyan", Palette.Accent, 3.2f);
            Glow("st_glow_amber", Palette.Warn, 3.0f);
            Glow("st_glow_red", Palette.Bad, 3.6f);
            Glow("st_glow_green", Palette.Good, 3.0f);
            Glow("st_glow_warm", Palette.LampWarm, 2.6f);
            Glow("st_screen", Palette.Accent, 0.9f);

            Materials["st_plume_outer"] = Save("st_plume_outer", Additive(new Color(3.2f, 1.25f, 0.3f, 0.55f)));
            Materials["st_plume_core"] = Save("st_plume_core", Additive(new Color(5f, 5.6f, 7f, 0.9f)));
        }

        static void Stage(string name, Color c, float smooth, float metallic)
        {
            c.a = 1f;
            Materials[name] = Save(name, LitMat(name, c, smooth, metallic));
        }

        static void Glow(string name, Color c, float intensity)
        {
            var m = LitMat(name, c * 0.25f, 0.4f, 0f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(c.r * intensity, c.g * intensity, c.b * intensity, 1f));
            // Without this flag URP's material validation strips the emission keyword.
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            Materials[name] = Save(name, m);
        }

        static Material Additive(Color hdr)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", hdr);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            return m;
        }

        /// <summary>Small tiling concrete texture: slab seams and a little noise so the range floor reads at scale.</summary>
        static Texture2D ConcreteTexture()
        {
            const string path = "Assets/_Project/Art/Generated/tex_concrete.asset";
            Directory.CreateDirectory("Assets/_Project/Art/Generated");
            const int n = 128;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool isNew = tex == null;
            if (isNew) tex = new Texture2D(n, n, TextureFormat.RGB24, true);
            var rng = new System.Random(7);
            var px = new Color[n * n];
            Color b = Palette.Floor;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = 1f + ((float)rng.NextDouble() - 0.5f) * 0.14f;
                    v += (Mathf.PerlinNoise(x * 0.06f, y * 0.06f) - 0.5f) * 0.22f;
                    if (x < 2 || y < 2) v *= 0.55f; // slab seam
                    px[y * n + x] = new Color(b.r * v, b.g * v, b.b * v, 1f);
                }
            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 8;
            tex.Apply(true);
            if (isNew) AssetDatabase.CreateAsset(tex, path); else EditorUtility.SetDirty(tex);
            return tex;
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
