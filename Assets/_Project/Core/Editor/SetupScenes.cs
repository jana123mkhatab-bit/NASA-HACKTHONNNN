using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LastSignal.EditorTools
{
    /// <summary>Builds the five mission scenes with the 3D model library, lighting and post-processing.</summary>
    public static class SetupScenes
    {
        const string SceneDir = "Assets/_Project/Scenes/";
        const string Models = "Assets/_Project/Art/Models/";

        public static readonly string[] SceneNames = { "M1_Ground", "M2_Preflight", "M3_DeepSpace", "M4_Planet", "DesignMode" };

        public static void Run()
        {
            Directory.CreateDirectory(SceneDir);
            Build("M1_Ground", BuildGround);
            Build("M2_Preflight", BuildPreflight);
            Build("M3_DeepSpace", BuildDeepSpace);
            Build("M4_Planet", BuildPlanet);
            Build("DesignMode", BuildDesignMode);

            var list = new List<EditorBuildSettingsScene>();
            foreach (var n in SceneNames) list.Add(new EditorBuildSettingsScene(SceneDir + n + ".unity", true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("LastSignal/Setup/Build M1 and M2 scenes")]
        public static void RunMissions12()
        {
            Directory.CreateDirectory(SceneDir);
            SetupContent.Run();
            SetupVisuals.SetupPipeline();
            SetupVisuals.BuildMaterials();
            LoadMaterial("concrete_light");
            LoadMaterial("__grass");
            LoadMaterial("__apron");
            LoadMaterial("__sky_ground");
            Build("M1_Ground", BuildGround);
            Build("M2_Preflight", BuildPreflight);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastSignal] Rebuilt M1_Ground and M2_Preflight with the imported 3D model library.");
        }

        static void LoadMaterial(string name)
        {
            if (SetupVisuals.Materials.ContainsKey(name)) return;
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/" + name + ".mat");
            if (material != null) SetupVisuals.Materials[name] = material;
        }

        static void Build(string name, System.Action<Scene> fill)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            fill(scene);
            var stub = new GameObject("StubMission (delete when the real mission exists)");
            stub.AddComponent<StubMission>();
            AddVolume();
            EditorSceneManager.SaveScene(scene, SceneDir + name + ".unity");
        }

        // ---------------------------------------------------------------- shared helpers

        public static GameObject Model(string relPath, string name, Vector3 pos, float scale = 1f, Vector3? euler = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + relPath);
            if (asset == null) { Debug.LogError("Model missing: " + relPath); return new GameObject(name + " (missing)"); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            if (euler.HasValue) go.transform.eulerAngles = euler.Value;
            Apply(go);
            return go;
        }

        /// <summary>Swap imported default materials for the tuned PBR set, matched by the .mtl material name.</summary>
        static void Apply(GameObject go)
        {
            var mats = SetupVisuals.Materials;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var cur = r.sharedMaterials;
                var next = new Material[cur.Length];
                for (int i = 0; i < cur.Length; i++)
                {
                    string n = cur[i] != null ? cur[i].name : "";
                    if (!mats.TryGetValue(n, out var m)) m = mats.ContainsKey("concrete_light") ? mats["concrete_light"] : cur[i];
                    next[i] = m;
                }
                r.sharedMaterials = next;
                bool fx = go.name.Contains("Atmos") || go.name.Contains("Cloud") || go.name.Contains("Corona") || go.name.Contains("Sky");
                if (fx)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
            }
        }

        /// <summary>Dusk sky, gradient ambient and palette fog for the ground missions.</summary>
        public static void DuskAtmosphere(float fogDensity)
        {
            RenderSettings.skybox = SetupVisuals.Materials["st_sky_dusk"];
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex("#4A5F88");
            RenderSettings.ambientEquatorColor = Palette.Hex("#4A3E40");
            RenderSettings.ambientGroundColor = Palette.Hex("#17130F");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = Palette.Fog;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.6f;
            DynamicGI.UpdateEnvironment();
        }

        /// <summary>Gameplay camera for the staged mission sets (a CameraRig drives it).</summary>
        public static Camera AddStageCamera(float fov)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 1500f; cam.fieldOfView = fov;
            cam.allowHDR = true;
            cam.clearFlags = CameraClearFlags.Skybox;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            return cam;
        }

        public static Light AddSun(Vector3 sunPos, Vector3 target, float intensity, Color color)
        {
            var go = new GameObject("Sun Light");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = color; l.intensity = intensity;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.95f;
            go.transform.position = sunPos;
            go.transform.rotation = Quaternion.LookRotation((target - sunPos).normalized);
            RenderSettings.sun = l;
            return l;
        }

        static Camera AddCamera(bool space, Transform target, float radius, float height, float speed, float startAngle = 0f)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 3000f; cam.fieldOfView = 50f;
            cam.allowHDR = true;
            cam.clearFlags = space ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            cam.backgroundColor = Color.black;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            var orbit = go.AddComponent<CameraOrbit>();
            orbit.target = target; orbit.radius = radius; orbit.height = height;
            orbit.degreesPerSecond = speed; orbit.startAngle = startAngle;
            return cam;
        }

        static void AddVolume()
        {
            var go = new GameObject("Global Volume");
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SetupVisuals.VolumePath);
        }

        static void SpaceAmbient()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.025f, 0.03f, 0.055f);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
        }

        static void GroundAtmosphere(Light sun, float fogDensity)
        {
            RenderSettings.skybox = SetupVisuals.Materials["__sky_ground"];
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = new Color(0.62f, 0.72f, 0.85f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            DynamicGI.UpdateEnvironment();
        }

        static void Ground(float size, string mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
            g.name = "Ground";
            g.transform.position = new Vector3(0, -0.06f, 0);
            g.transform.localScale = Vector3.one * (size / 10f);
            g.GetComponent<Renderer>().sharedMaterial = SetupVisuals.Materials[mat];
            Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        // Celestial rig: Earth (r=10) + clouds + atmosphere, Moon (r=3), Sun (r=20) + corona, star sphere (r=500).
        static Transform AddEarth(Vector3 pos, float scale = 1f)
        {
            var root = new GameObject("Earth System").transform;
            root.position = pos;
            var e = Model("Celestial/earth/earth.obj", "Earth", pos, scale); e.transform.SetParent(root, true);
            var spin = e.AddComponent<SlowSpin>(); spin.degreesPerSecond = new Vector3(0, 1.5f, 0);
            var c = Model("Celestial/earth/earth_clouds.obj", "Earth Clouds", pos, scale); c.transform.SetParent(root, true);
            var cs = c.AddComponent<SlowSpin>(); cs.degreesPerSecond = new Vector3(0, 2.0f, 0);
            var a = Model("Celestial/earth/earth_atmosphere.obj", "Earth Atmosphere", pos, scale); a.transform.SetParent(root, true);
            return root;
        }

        static Transform AddMoon(Vector3 pos, float scale = 1f)
        {
            var m = Model("Celestial/moon/moon.obj", "Moon", pos, scale);
            m.AddComponent<SlowSpin>().degreesPerSecond = new Vector3(0, 0.6f, 0);
            return m.transform;
        }

        static Transform AddSunBody(Vector3 pos)
        {
            var root = new GameObject("Sun System").transform;
            root.position = pos;
            var s = Model("Celestial/sun/sun.obj", "Sun", pos); s.transform.SetParent(root, true);
            s.AddComponent<SlowSpin>().degreesPerSecond = new Vector3(0, 0.8f, 0);
            var c = Model("Celestial/sun/sun_corona.obj", "Sun Corona", pos); c.transform.SetParent(root, true);
            c.AddComponent<SlowSpin>().degreesPerSecond = new Vector3(0, -1.2f, 0);
            foreach (var r in s.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
            return root;
        }

        static void AddStars()
        {
            var sky = Model("Celestial/space/space_skybox.obj", "Star Sky", Vector3.zero, 2f);
            foreach (var r in sky.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
        }

        // ---------------------------------------------------------------- the scenes

        static void BuildGround(Scene s)
        {
            StageKit.BuildShapes();
            StageM1.Build();
        }

        static void BuildPreflight(Scene s)
        {
            StageKit.BuildShapes();
            StageM2.Build();
        }

        static void BuildDeepSpace(Scene s)
        {
            SpaceAmbient();
            AddStars();
            var earth = AddEarth(Vector3.zero);
            AddMoon(new Vector3(48, 6, -22));
            AddSunBody(new Vector3(-420, 70, 260));
            AddSun(new Vector3(-420, 70, 260), Vector3.zero, 2.4f, new Color(1f, 0.96f, 0.9f));
            AddCamera(true, earth, 36f, 6f, 2.5f, 130f);
        }

        static void BuildPlanet(Scene s)
        {
            SpaceAmbient();
            RenderSettings.ambientLight = new Color(0.04f, 0.045f, 0.06f);
            AddStars();
            var planet = AddMoon(Vector3.zero, 7f);   // stand-in planet: the 3 m Moon scaled to a 21 m world
            AddEarth(new Vector3(-150, 60, -220), 1.5f);
            AddSunBody(new Vector3(420, 90, 240));
            AddSun(new Vector3(420, 90, 240), Vector3.zero, 2.6f, new Color(1f, 0.97f, 0.92f));
            AddCamera(true, planet, 28f, 3f, 1.2f, 40f);
        }

        static void BuildDesignMode(Scene s)
        {
            SpaceAmbient();
            AddStars();
            var earth = AddEarth(Vector3.zero);
            AddMoon(new Vector3(60, 4, -30));
            AddSunBody(new Vector3(-450, 90, 280));
            AddSun(new Vector3(-450, 90, 280), Vector3.zero, 2.4f, new Color(1f, 0.96f, 0.9f));
            AddCamera(true, earth, 70f, 18f, 1.5f, 200f);
        }
    }
}
