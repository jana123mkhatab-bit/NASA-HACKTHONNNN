using System.Collections.Generic;
using LastSignal.Core;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// One-click project setup. Menu: Last Signal > 1, 2, 3, Check setup.
    /// Safe to run again: existing assets and scenes are never overwritten.
    /// </summary>
    public static class LastSignalSetup
    {
        const string Root = "Assets/_Project";
        const string ScenesDir = Root + "/Core/Scenes";
        const string ResourcesDir = Root + "/Core/Resources";
        const string PrefabDir = Root + "/Art/Prefabs";
        const string MaterialDir = Root + "/Art/Materials";
        const string TexDir = Root + "/Art/Textures";
        const string CelestialDir = Root + "/Art/Models/Celestial";
        const string MascotDir = Root + "/Art/Characters/Mascot";

        static readonly string[] Folders =
        {
            Root + "/Core", Root + "/Core/Resources", Root + "/Core/Scenes", Root + "/Core/Editor", Root + "/Core/Tests",
            Root + "/Missions", Root + "/Missions/M1", Root + "/Missions/M2", Root + "/Missions/M3",
            Root + "/Missions/M4", Root + "/Missions/Final", Root + "/Shared/Ship", Root + "/UI/Planner", Root + "/Cinematics",
            Root + "/DesignMode", Root + "/Shared", Root + "/UI", Root + "/Data", Root + "/Audio", Root + "/Docs",
            Root + "/Art", Root + "/Art/Models", Root + "/Art/Textures", Root + "/Art/Characters", Root + "/Art/Materials", Root + "/Art/Prefabs"
        };

        // ------------------------------------------------------------------ 1

        [MenuItem("Last Signal/1 - Create folders, scenes and core assets")]
        public static void CreateFoldersScenesAndAssets()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            for (int i = 0; i < Folders.Length; i++) EnsureFolder(Folders[i]);
            PlayerSettings.productName = "The Last Known Signal";

            CreatePlayerStateAsset();
            CreatePipLinesAsset();
            CreateDebriefCards();

            string[] sceneIds = { MissionIds.Boot, MissionIds.M1, MissionIds.M2, MissionIds.M3, MissionIds.M4, MissionIds.Final, MissionIds.DesignMode };
            List<string> paths = new List<string>();
            for (int i = 0; i < sceneIds.Length; i++)
            {
                string id = sceneIds[i];
                string path = ScenesDir + "/" + MissionIds.SceneFor(id) + ".unity";
                paths.Add(path);
                if (File.Exists(path)) continue;
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                if (id != MissionIds.Boot)
                {
                    GameObject stub = new GameObject("StubMission");
                    StubMission sm = stub.AddComponent<StubMission>();
                    sm.missionId = id;
                }
                EditorSceneManager.SaveScene(scene, path);
            }

            MergeBuildSettings(paths);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Last Signal: folders, scenes and core assets are ready. Open Boot and press Play.");
        }

        static void CreatePlayerStateAsset()
        {
            string path = ResourcesDir + "/PlayerState.asset";
            if (File.Exists(path)) return;
            PlayerState s = ScriptableObject.CreateInstance<PlayerState>();
            s.ResetToDefaults();
            AssetDatabase.CreateAsset(s, path);
        }

        static void CreatePipLinesAsset()
        {
            string path = ResourcesDir + "/PipLines.asset";
            if (File.Exists(path)) return;
            PipLines p = ScriptableObject.CreateInstance<PipLines>();
            p.lines = Pip.DefaultLines();
            AssetDatabase.CreateAsset(p, path);
        }

        static void CreateDebriefCards()
        {
            EnsureFolder(ResourcesDir + "/Debrief");
            string[] ids = { MissionIds.M1, MissionIds.M2, MissionIds.M3, MissionIds.M4, MissionIds.Final };
            string[] concepts =
            {
                "TODO (person 6): rocket equation and why most of a rocket is fuel.",
                "TODO (person 6): redundancy and risk, R = 1 - (1 - r)^n.",
                "TODO (person 6): communication delay and the Deep Space Network.",
                "TODO (person 6): habitable zone and choosing a planet.",
                "TODO (person 6): how the whole mission chain multiplies into one success probability."
            };
            for (int i = 0; i < ids.Length; i++)
            {
                string path = ResourcesDir + "/Debrief/" + ids[i] + ".asset";
                if (File.Exists(path)) continue;
                DebriefCard c = ScriptableObject.CreateInstance<DebriefCard>();
                c.missionId = ids[i];
                c.title = "Debrief: " + MissionIds.Title(ids[i]);
                c.concept = concepts[i];
                c.nasaSource = "TODO (person 6): name and link the NASA source.";
                c.body = "TODO (person 6): replace this placeholder with the final debrief text.";
                AssetDatabase.CreateAsset(c, path);
            }
        }

        static void MergeBuildSettings(List<string> ours)
        {
            List<EditorBuildSettingsScene> merged = new List<EditorBuildSettingsScene>();
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < ours.Count; i++)
            {
                merged.Add(new EditorBuildSettingsScene(ours[i], true));
                seen.Add(ours[i]);
            }
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (seen.Contains(existing[i].path)) continue;
                merged.Add(existing[i]);
            }
            EditorBuildSettings.scenes = merged.ToArray();
        }

        // ------------------------------------------------------------------ 2

        [MenuItem("Last Signal/2 - Build space prefabs (Earth, Moon, Sun, skybox)")]
        public static void BuildSpacePrefabs()
        {
            EnsureFolder(PrefabDir);
            EnsureFolder(MaterialDir);

            Material earth = MakeMaterial("Earth", LoadTex("earth_daymap.jpg"), Color.white);
            Material clouds = MakeMaterial("EarthClouds", LoadTex("earth_clouds_alpha.png"), new Color(1f, 1f, 1f, 0.85f));
            MakeTransparent(clouds);
            Material atmosphere = MakeMaterial("EarthAtmosphere", null, new Color(0.35f, 0.6f, 1f, 0.18f));
            MakeTransparent(atmosphere);
            Material moon = MakeMaterial("Moon", LoadTex("moon.jpg"), Color.white);
            Material sun = MakeMaterial("Sun", LoadTex("sun.jpg"), Color.white);
            SetEmission(sun, new Color(1f, 0.85f, 0.5f), 1.5f);
            Material corona = MakeMaterial("SunCorona", null, new Color(1f, 0.7f, 0.2f, 0.25f));
            MakeTransparent(corona);

            MakeModelPrefab("Earth", CelestialDir + "/earth/earth.obj", earth);
            MakeEarthWithLayers(clouds, atmosphere, earth);
            MakeModelPrefab("Moon", CelestialDir + "/moon/moon.obj", moon);
            MakeSunPrefab(sun, corona);

            Material sky = CreateSkyboxMaterial();
            BuildSpacePreviewScene(sky);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Last Signal: space prefabs built in " + PrefabDir + ". Open Core/Scenes/SpacePreview to look at them.");
        }

        static Texture2D LoadTex(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + file);
        }

        static bool UsingSrp()
        {
            return GraphicsSettings.currentRenderPipeline != null;
        }

        static Shader LitShader()
        {
            Shader s = null;
            if (UsingSrp()) s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        static Material MakeMaterial(string name, Texture2D tex, Color color)
        {
            string path = MaterialDir + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(LitShader());
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != LitShader())
            {
                m.shader = LitShader(); // the render pipeline changed since this material was made
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void SetEmission(Material m, Color color, float intensity)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * intensity);
            EditorUtility.SetDirty(m);
        }

        /// <summary>Makes a material see-through. Works for URP Lit and the Built-in Standard shader.</summary>
        static void MakeTransparent(Material m)
        {
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetShaderPassEnabled("ShadowCaster", false);
            }
            else if (m.HasProperty("_Mode"))
            {
                m.SetFloat("_Mode", 3f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
        }

        static GameObject InstantiateModel(string assetPath, Material material, string name)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning("Last Signal: model not found: " + assetPath);
                return null;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = name;
            ApplyMaterial(go, material);
            return go;
        }

        static void ApplyMaterial(GameObject go, Material material)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] mats = new Material[Mathf.Max(1, renderers[i].sharedMaterials.Length)];
                for (int k = 0; k < mats.Length; k++) mats[k] = material;
                renderers[i].sharedMaterials = mats;
            }
        }

        static void SavePrefab(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        static void MakeModelPrefab(string name, string modelPath, Material material)
        {
            GameObject go = InstantiateModel(modelPath, material, name);
            if (go != null) SavePrefab(go, name);
        }

        static void MakeEarthWithLayers(Material clouds, Material atmosphere, Material surface)
        {
            GameObject root = new GameObject("EarthFull");
            GameObject body = InstantiateModel(CelestialDir + "/earth/earth.obj", surface, "Surface");
            if (body == null) { Object.DestroyImmediate(root); return; }
            body.transform.SetParent(root.transform, false);
            GameObject c = InstantiateModel(CelestialDir + "/earth/earth_clouds.obj", clouds, "Clouds");
            if (c != null) c.transform.SetParent(root.transform, false);
            GameObject a = InstantiateModel(CelestialDir + "/earth/earth_atmosphere.obj", atmosphere, "Atmosphere");
            if (a != null) a.transform.SetParent(root.transform, false);
            SavePrefab(root, "EarthFull");
        }

        static void MakeSunPrefab(Material sun, Material corona)
        {
            GameObject root = new GameObject("SunFull");
            GameObject body = InstantiateModel(CelestialDir + "/sun/sun.obj", sun, "Sun");
            if (body == null) { Object.DestroyImmediate(root); return; }
            body.transform.SetParent(root.transform, false);
            GameObject c = InstantiateModel(CelestialDir + "/sun/sun_corona.obj", corona, "Corona");
            if (c != null) c.transform.SetParent(root.transform, false);
            SavePrefab(root, "SunFull");
        }

        static Material CreateSkyboxMaterial()
        {
            string path = MaterialDir + "/SpaceSkybox.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(m, path);
            }
            Texture2D stars = LoadTex("stars_milkyway.jpg");
            if (stars != null) m.SetTexture("_MainTex", stars);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void BuildSpacePreviewScene(Material sky)
        {
            string path = ScenesDir + "/SpacePreview.unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            RenderSettings.skybox = sky;

            PlacePrefab("EarthFull", new Vector3(-6f, 0f, 12f));
            PlacePrefab("Moon", new Vector3(5f, 2f, 14f));
            PlacePrefab("SunFull", new Vector3(30f, 8f, 60f));

            EditorSceneManager.SaveScene(scene, path);
        }

        static void PlacePrefab(string name, Vector3 position)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + name + ".prefab");
            if (prefab == null) return;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = position;
        }

        // ------------------------------------------------------------------ 3

        [MenuItem("Last Signal/3 - Build mascot prefab (Pip)")]
        public static void BuildMascotPrefab()
        {
            EnsureFolder(PrefabDir);
            EnsureFolder(MaterialDir);

            GameObject halo = AssetDatabase.LoadAssetAtPath<GameObject>(MascotDir + "/Mascot_Halo.fbx");
            GameObject backdrop = AssetDatabase.LoadAssetAtPath<GameObject>(MascotDir + "/Mascot_Backdrop.fbx");
            if (halo == null)
            {
                Debug.LogError("Last Signal: Mascot_Halo.fbx not found in " + MascotDir);
                return;
            }

            GameObject root = new GameObject("Mascot");
            GameObject haloInstance = (GameObject)PrefabUtility.InstantiatePrefab(halo);
            haloInstance.name = "Halo";
            haloInstance.transform.SetParent(root.transform, false);
            FixMascotMaterials(haloInstance);

            MascotIdle idle = root.AddComponent<MascotIdle>();
            idle.halo = haloInstance.transform;

            string prefabPath = PrefabDir + "/Mascot.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            if (backdrop != null)
            {
                GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(backdrop);
                b.name = "MascotBackdrop";
                FixMascotMaterials(b);
                PrefabUtility.SaveAsPrefabAsset(b, PrefabDir + "/MascotBackdrop.prefab");
                Object.DestroyImmediate(b);
            }

            BuildMascotPreviewScene();
            AddMascotToBoot();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Last Signal: Mascot prefab built. Open Core/Scenes/MascotPreview, or press Play from Boot.");
        }

        /// <summary>Copies the FBX's materials into project assets and makes glass and beam parts see-through.</summary>
        static void FixMascotMaterials(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] src = renderers[i].sharedMaterials;
                Material[] dst = new Material[src.Length];
                for (int k = 0; k < src.Length; k++)
                {
                    Material original = src[k];
                    if (original == null) { dst[k] = null; continue; }
                    string safe = original.name.Replace(" ", "_").Replace("/", "_");
                    string path = MaterialDir + "/Mascot_" + safe + ".mat";
                    Material copy = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (copy == null)
                    {
                        copy = new Material(LitShader());
                        Color c = Color.white;
                        if (original.HasProperty("_BaseColor")) c = original.GetColor("_BaseColor");
                        else if (original.HasProperty("_Color")) c = original.GetColor("_Color");
                        if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", c);
                        if (copy.HasProperty("_Color")) copy.SetColor("_Color", c);
                        Texture tex = null;
                        if (original.HasProperty("_MainTex")) tex = original.GetTexture("_MainTex");
                        if (tex == null && original.HasProperty("_BaseMap")) tex = original.GetTexture("_BaseMap");
                        if (tex != null)
                        {
                            if (copy.HasProperty("_BaseMap")) copy.SetTexture("_BaseMap", tex);
                            if (copy.HasProperty("_MainTex")) copy.SetTexture("_MainTex", tex);
                        }
                        string lower = original.name.ToLowerInvariant();
                        if (lower.Contains("glass") || lower.Contains("beam"))
                        {
                            Color t = c;
                            t.a = lower.Contains("beam") ? 0.35f : 0.30f;
                            if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", t);
                            if (copy.HasProperty("_Color")) copy.SetColor("_Color", t);
                            MakeTransparent(copy);
                            if (lower.Contains("beam")) SetEmission(copy, c, 1.2f);
                        }
                        AssetDatabase.CreateAsset(copy, path);
                    }
                    dst[k] = copy;
                }
                renderers[i].sharedMaterials = dst;
            }
        }

        static void BuildMascotPreviewScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Mascot.prefab");
            if (prefab != null)
            {
                GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                FitInFrontOfCamera(m, 3f);
            }
            EditorSceneManager.SaveScene(scene, ScenesDir + "/MascotPreview.unity");
        }

        static void AddMascotToBoot()
        {
            string bootPath = ScenesDir + "/Boot.unity";
            if (!File.Exists(bootPath)) return;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Mascot.prefab");
            if (prefab == null) return;
            Scene scene = EditorSceneManager.OpenScene(bootPath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "Mascot") { return; }
            }
            GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            FitInFrontOfCamera(m, 3f);
            m.transform.position += new Vector3(4.5f, -0.5f, 0f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Scales the object to the given height and puts it 8 units in front of the default camera.</summary>
        static void FitInFrontOfCamera(GameObject go, float targetHeight)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                float h = Mathf.Max(0.0001f, b.size.y);
                go.transform.localScale = go.transform.localScale * (targetHeight / h);
            }
            go.transform.position = new Vector3(0f, 1f, 0f);
        }

        // ------------------------------------------------------------------ check

        [MenuItem("Last Signal/Check setup")]
        public static void CheckSetup()
        {
            List<string> problems = new List<string>();
            string[] sceneIds = { MissionIds.Boot, MissionIds.M1, MissionIds.M2, MissionIds.M3, MissionIds.M4, MissionIds.Final, MissionIds.DesignMode };
            for (int i = 0; i < sceneIds.Length; i++)
            {
                string path = ScenesDir + "/" + MissionIds.SceneFor(sceneIds[i]) + ".unity";
                if (!File.Exists(path)) problems.Add("Missing scene: " + path);
                else if (!InBuildSettings(path)) problems.Add("Scene not in Build Settings: " + path);
            }
            if (!File.Exists(ResourcesDir + "/PlayerState.asset")) problems.Add("Missing PlayerState asset (run menu 1).");
            if (!File.Exists(ResourcesDir + "/PipLines.asset")) problems.Add("Missing PipLines asset (run menu 1).");
            if (!File.Exists(PrefabDir + "/EarthFull.prefab")) problems.Add("Space prefabs not built yet (run menu 2).");
            if (!File.Exists(PrefabDir + "/Mascot.prefab")) problems.Add("Mascot prefab not built yet (run menu 3).");

            if (problems.Count == 0) Debug.Log("Last Signal: setup looks complete.");
            else Debug.LogWarning("Last Signal: setup problems:\n- " + string.Join("\n- ", problems.ToArray()));
        }

        static bool InBuildSettings(string path)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == path) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ helpers

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string leaf = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
