using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.M4
{
    /// <summary>
    /// Builds the planet surface at runtime: valley terrain with a mountain ring, a sloped ridge and a crater,
    /// boulders, the son's water collector, his crashed ship and camp, the power cable between them,
    /// the father's lander, sun/ambient/fog/reflection lighting and ambient dust.
    /// Everything is parented under one root so the orbit view can hide it.
    /// </summary>
    public class M4World : MonoBehaviour
    {
        public const float Size = 800f;
        const int Res = 241;                         // vertices per side (3.3 m cells)
        public const float PlayRadius = 360f;

        // Layout (metres). The valley axis runs from the collector (origin) to the son's ship.
        public static readonly Vector2 CollectorXZ = new Vector2(0f, 0f);
        public static readonly Vector2 ShipXZ = new Vector2(-55f, 100f);
        static readonly Vector2 ValleyDir = new Vector2(-0.482f, 0.876f);
        static readonly Vector2 RidgeXZ = new Vector2(230f, 170f);
        static readonly Vector2 CraterXZ = new Vector2(-210f, -170f);
        const float CraterR = 70f;

        public Transform Root { get; private set; }
        public Transform Collector { get; private set; }
        public Transform SonsShip { get; private set; }
        public Transform SonsHatch { get; private set; }
        public Transform SonHidingSpot { get; private set; }
        public Light SunLight { get; private set; }
        public Vector3[] CablePath { get; private set; }
        public LandingSite[] Sites { get; private set; }
        public ExoplanetRecord Planet { get; private set; }

        GameObject _moonPlaceholder, _earthPlaceholder;
        LineRenderer _ellipse;
        ReflectionProbe _probe;
        ParticleSystem _motes;

        // ------------------------------------------------------------------ height field

        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        public static float ValleyMask(float x, float z)
        {
            float along = x * ValleyDir.x + z * ValleyDir.y;
            float perp = Mathf.Abs(x * ValleyDir.y - z * ValleyDir.x);
            return Mathf.Exp(-(perp * perp) / (2f * 55f * 55f)) * (1f - Smooth(180f, 300f, Mathf.Abs(along)));
        }

        public static float CraterMask(float x, float z) => 1f - Smooth(0.45f, 0.9f, Vector2.Distance(new Vector2(x, z), CraterXZ) / CraterR);

        /// <summary>Analytic terrain height. Deterministic, so gameplay can query it without raycasts.</summary>
        public static float Height(float x, float z)
        {
            float h = (Noise.Fbm(x * 0.0035f + 11f, z * 0.0035f + 7f, 5, 0, 101) - 0.5f) * 34f;
            h += (Noise.Fbm(x * 0.03f, z * 0.03f, 3, 0, 202) - 0.5f) * 2.2f;

            float r = Mathf.Sqrt(x * x + z * z);
            float ring = Smooth(285f, 420f, r);
            h += ring * (20f + Noise.Ridged(x * 0.006f, z * 0.006f, 5, 303) * 110f);

            // valley floor
            float along = x * ValleyDir.x + z * ValleyDir.y;
            float floorH = -8f + (Noise.Fbm(x * 0.02f, z * 0.02f, 3, 0, 404) - 0.5f) * 1.2f + along * 0.008f;
            h = Mathf.Lerp(h, floorH, ValleyMask(x, z) * 0.93f);

            // ridge shoulder near site B
            Vector2 rd = new Vector2(0.8f, -0.6f);
            Vector2 p = new Vector2(x, z) - RidgeXZ;
            float perpR = p.x * rd.y - p.y * rd.x - 45f;
            float alongR = Mathf.Abs(Vector2.Dot(p, rd));
            h += 26f * Mathf.Exp(-(perpR * perpR) / (2f * 85f * 85f)) * (1f - Smooth(140f, 260f, alongR));

            // crater near site C
            float dc = Vector2.Distance(new Vector2(x, z), CraterXZ) / CraterR;
            if (dc < 1f) h -= (1f - dc * dc) * 16f;
            h += 9f * Mathf.Exp(-((dc - 1f) * (dc - 1f)) / (2f * 0.12f * 0.12f));
            float floorMask = 1f - Smooth(0.35f, 0.7f, dc);                       // flat, icy crater floor
            h = Mathf.Lerp(h, -14f + (Noise.Fbm(x * 0.05f, z * 0.05f, 3, 0, 909) - 0.5f) * 1.5f, floorMask * 0.9f);
            return h;
        }

        public static Vector3 Ground(Vector2 xz, float lift = 0f) => new Vector3(xz.x, Height(xz.x, xz.y) + lift, xz.y);

        public static Vector3 GroundNormal(float x, float z)
        {
            const float e = 1.0f;
            float hl = Height(x - e, z), hr = Height(x + e, z), hd = Height(x, z - e), hu = Height(x, z + e);
            return new Vector3(hl - hr, 2f * e, hd - hu).normalized;
        }

        // ------------------------------------------------------------------ build

        public void Build(ExoplanetRecord planet)
        {
            Planet = planet;
            Root = new GameObject("M4 Surface").transform;
            Root.SetParent(transform, false);

            Sites = LandingSite.Defaults();
            foreach (var s in Sites) s.beaconDistance = Vector2.Distance(s.xz, CollectorXZ);

            var grid = BuildTerrain();
            ScatterRocks();
            BuildCollector();
            BuildSonsShip();
            BuildCable();
            BuildSiteFlags();
            _moonPlaceholder = GameObject.Find("Moon");
            _earthPlaceholder = GameObject.Find("Earth System");
            if (_earthPlaceholder != null) _earthPlaceholder.SetActive(false); // Earth is not in this sky
            SetupLighting(planet);
            Root.gameObject.SetActive(false);
        }

        /// <summary>Switch between the orbit view (placeholder planet from space) and the surface.</summary>
        public void ShowSurface(bool on)
        {
            Root.gameObject.SetActive(on);
            if (_moonPlaceholder != null) _moonPlaceholder.SetActive(!on);
            RenderSettings.fog = on;
            if (_motes != null) _motes.gameObject.SetActive(on);
            if (on) StartCoroutine(RefreshProbeSoon());
        }

        IEnumerator RefreshProbeSoon()
        {
            yield return null;
            yield return null;
            if (_probe != null) _probe.RenderProbe();
        }

        float[] BuildTerrain()
        {
            var heights = new float[Res * Res];
            var verts = new Vector3[Res * Res];
            var uvs = new Vector2[Res * Res];
            float step = Size / (Res - 1);
            for (int j = 0; j < Res; j++)
                for (int i = 0; i < Res; i++)
                {
                    float x = -Size / 2f + i * step, z = -Size / 2f + j * step;
                    float h = Height(x, z);
                    heights[j * Res + i] = h;
                    verts[j * Res + i] = new Vector3(x, h, z);
                    uvs[j * Res + i] = new Vector2(i / (float)(Res - 1), j / (float)(Res - 1));
                }
            var tris = new int[(Res - 1) * (Res - 1) * 6];
            int t = 0;
            for (int j = 0; j < Res - 1; j++)
                for (int i = 0; i < Res - 1; i++)
                {
                    int a = j * Res + i, b = a + 1, c = a + Res, d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            var mesh = new Mesh { name = "M4_Terrain", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();

            var go = new GameObject("Terrain");
            go.transform.SetParent(Root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = M4Materials.Terrain(TerrainAlbedo(heights, step), TerrainNormal());
            mr.shadowCastingMode = ShadowCastingMode.On;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return heights;
        }

        /// <summary>Macro colour: ochre dust, dark basalt on steep slopes, damp silt and pale mineral streaks in the valley, frost in the crater.</summary>
        static Texture2D TerrainAlbedo(float[] heights, float step)
        {
            const int n = 512;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "M4_TerrainAlb", wrapMode = TextureWrapMode.Clamp, anisoLevel = 8 };
            var px = new Color[n * n];
            Color dust = new Color(0.56f, 0.39f, 0.27f), rust = new Color(0.43f, 0.26f, 0.18f), basalt = new Color(0.19f, 0.16f, 0.15f);
            Color silt = new Color(0.30f, 0.24f, 0.21f), salt = new Color(0.78f, 0.72f, 0.63f), frost = new Color(0.66f, 0.72f, 0.80f);
            Color summit = new Color(0.70f, 0.56f, 0.44f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)(n - 1), v = y / (float)(n - 1);
                    float gx = u * (Res - 1), gz = v * (Res - 1);
                    int i = Mathf.Min(Res - 2, (int)gx), j = Mathf.Min(Res - 2, (int)gz);
                    float h = heights[j * Res + i];
                    float sx = (heights[j * Res + i + 1] - h) / step, sz = (heights[(j + 1) * Res + i] - h) / step;
                    float slope = Mathf.Sqrt(sx * sx + sz * sz);
                    float wx = -Size / 2f + u * Size, wz = -Size / 2f + v * Size;

                    float n1 = Noise.Fbm(wx * 0.018f, wz * 0.018f, 4, 0, 501);
                    float n2 = Noise.Fbm(wx * 0.07f, wz * 0.07f, 3, 0, 503);
                    Color c = Color.Lerp(dust, rust, Smooth(0.35f, 0.7f, n1));
                    c = Color.Lerp(c, summit, Smooth(25f, 70f, h) * 0.6f);
                    c = Color.Lerp(c, basalt, Smooth(0.35f, 0.85f, slope + (n2 - 0.5f) * 0.3f));
                    float vm = ValleyMask(wx, wz);
                    c = Color.Lerp(c, silt, vm * 0.55f);
                    float streak = Smooth(0.58f, 0.68f, Noise.Fbm(wx * 0.05f + 3f, wz * 0.012f, 3, 0, 507));
                    c = Color.Lerp(c, salt, vm * streak * 0.55f);
                    float cm = CraterMask(wx, wz);
                    c = Color.Lerp(c, frost, cm * Smooth(0.5f, 0.62f, n2) * 0.8f);
                    c *= 0.9f + n2 * 0.2f;
                    c.a = 1f;
                    px[y * n + x] = c;
                }
            tex.SetPixels(px); tex.Apply(true);
            return tex;
        }

        /// <summary>Metre-scale relief (erosion channels and pebbles) on top of the mesh shape.</summary>
        static Texture2D TerrainNormal()
        {
            const int n = 512;
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float wx = x / (float)n * Size, wz = y / (float)n * Size;
                    float channels = 1f - Mathf.Abs(Noise.Fbm(wx * 0.04f, wz * 0.04f, 4, 0, 601) * 2f - 1f);
                    h[y * n + x] = channels * 0.6f + Noise.Fbm(wx * 0.3f, wz * 0.3f, 3, 0, 607) * 0.4f;
                }
            return M4Materials.HeightToNormal(h, n, 3.5f, "M4_TerrainNrm", false);
        }

        // ------------------------------------------------------------------ rocks

        static Mesh[] _rockMeshes;

        static Mesh RockMesh(int seed)
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var src = tmp.GetComponent<MeshFilter>().sharedMesh;
            var m = Instantiate(src);
            Destroy(tmp);
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = v[i];
                float d = Noise.Fbm(p.x * 2.2f + seed, p.y * 2.2f + p.z * 1.7f, 4, 0, seed) - 0.5f;
                float flat = p.y < -0.15f ? 0.55f : 1f; // flat bottom so rocks sit on the ground
                v[i] = new Vector3(p.x * (1f + d * 0.9f), p.y * (0.75f + d * 0.6f) * flat, p.z * (1f + d * 0.9f));
            }
            m.vertices = v;
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            m.name = "M4_Rock" + seed;
            return m;
        }

        void ScatterRocks()
        {
            if (_rockMeshes == null) _rockMeshes = new[] { RockMesh(1), RockMesh(2), RockMesh(3), RockMesh(4) };
            var parent = new GameObject("Rocks").transform;
            parent.SetParent(Root, false);
            var mat = M4Materials.Rock();
            var rnd = new System.Random(2590);
            int placed = 0;
            for (int k = 0; k < 2600 && placed < 520; k++)
            {
                float x = (float)(rnd.NextDouble() * 2 - 1) * 340f, z = (float)(rnd.NextDouble() * 2 - 1) * 340f;
                var xz = new Vector2(x, z);
                float cm = CraterMask(x, z);
                float density = 0.18f + cm * 0.9f + Smooth(0.2f, 0.6f, 1f - GroundNormal(x, z).y * 1.0f + 0.25f) * 0.3f;
                if (rnd.NextDouble() > density) continue;
                // keep the landing ellipse, the camp and the cable path readable
                if (Vector2.Distance(xz, Sites[0].xz) < 30f || Vector2.Distance(xz, CollectorXZ) < 14f || Vector2.Distance(xz, ShipXZ) < 20f) continue;
                if (DistanceToSegment(xz, CollectorXZ, ShipXZ) < 6f) continue;
                if (Vector2.Distance(xz, Sites[1].xz) < 8f) continue;

                bool boulder = rnd.NextDouble() < 0.12 + cm * 0.35;
                float s = boulder ? 1.6f + (float)rnd.NextDouble() * 2.8f : 0.25f + (float)rnd.NextDouble() * 0.9f;
                var go = new GameObject(boulder ? "Boulder" : "Rock");
                go.transform.SetParent(parent, false);
                go.transform.position = Ground(xz, -0.12f * s);
                go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f) *
                                        Quaternion.FromToRotation(Vector3.up, GroundNormal(x, z));
                go.transform.localScale = new Vector3(s, s * (0.6f + (float)rnd.NextDouble() * 0.5f), s * (0.8f + (float)rnd.NextDouble() * 0.4f));
                go.AddComponent<MeshFilter>().sharedMesh = _rockMeshes[rnd.Next(_rockMeshes.Length)];
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = s < 0.5f ? ShadowCastingMode.Off : ShadowCastingMode.On;
                if (boulder) { var sc = go.AddComponent<SphereCollider>(); sc.radius = 0.45f; go.tag = "Untagged"; go.name = "Boulder"; }
                placed++;
            }
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // ------------------------------------------------------------------ primitives helper

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 lpos, Vector3 lscale,
            Vector3 leuler, Material mat, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lpos;
            go.transform.localScale = lscale;
            go.transform.localEulerAngles = leuler;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Destroy(go.GetComponent<Collider>());
            return go;
        }

        static Light PointLight(string name, Transform parent, Vector3 lpos, Color c, float intensity, float range, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lpos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        static TextMesh Label3D(string text, Transform parent, Vector3 lpos, Vector3 leuler, float size, Color c)
        {
            var go = new GameObject("Label " + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lpos;
            go.transform.localEulerAngles = leuler;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text; tm.characterSize = size; tm.fontSize = 64; tm.color = c;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.font = UIFactory.Font; // a code-added TextMesh has no font or material until assigned
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && tm.font != null) mr.sharedMaterial = tm.font.material;
            return tm;
        }

        // ------------------------------------------------------------------ the water collector

        void BuildCollector()
        {
            var root = new GameObject("Water Collector").transform;
            root.SetParent(Root, false);
            root.position = Ground(CollectorXZ, -0.05f);
            root.rotation = Quaternion.Euler(0f, 20f, 0f);
            Collector = root;

            var white = M4Materials.Hull("CollectorHull", new Color(0.86f, 0.86f, 0.84f), 0.6f, 0.5f);
            var dark = M4Materials.Hull("DarkMetal", new Color(0.18f, 0.19f, 0.21f), 0.9f, 0.45f);
            var fin = M4Materials.Pbr("CondenserFin", new Color(0.55f, 0.6f, 0.66f), 1f, 0.82f);
            var glow = M4Materials.Emissive("CyanGlow", new Color(0.1f, 0.3f, 0.35f), new Color(0.25f, 0.85f, 1f), 4f);
            var screen = M4Materials.Emissive("Screen", Color.black, new Color(0.3f, 0.9f, 1f), 1.6f);

            Prim(PrimitiveType.Cylinder, "Pad", root, new Vector3(0, 0.1f, 0), new Vector3(7f, 0.15f, 7f), Vector3.zero, dark);
            Prim(PrimitiveType.Cylinder, "Tank", root, new Vector3(0, 1.9f, 0), new Vector3(2.8f, 1.7f, 2.8f), Vector3.zero, white, true);
            Prim(PrimitiveType.Cylinder, "Band", root, new Vector3(0, 2.7f, 0), new Vector3(2.86f, 0.07f, 2.86f), Vector3.zero, glow);
            Prim(PrimitiveType.Cylinder, "Mast", root, new Vector3(0, 6.5f, 0), new Vector3(0.35f, 3.2f, 0.35f), Vector3.zero, dark);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f;
                var f = Prim(PrimitiveType.Cube, "Fin " + i, root, Quaternion.Euler(0, a, 0) * new Vector3(0, 6.2f, 0.85f),
                    new Vector3(0.05f, 3.4f, 1.4f), new Vector3(0, a, 0), fin);
                f.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            }
            Prim(PrimitiveType.Sphere, "Dish", root, new Vector3(0, 10.1f, 0), new Vector3(2.6f, 0.35f, 2.6f), Vector3.zero, white);
            var beacon = Prim(PrimitiveType.Sphere, "Beacon", root, new Vector3(0, 10.5f, 0), Vector3.one * 0.35f, Vector3.zero, glow);
            var bl = PointLight("Beacon Light", root, new Vector3(0, 10.6f, 0), new Color(0.35f, 0.85f, 1f), 6f, 22f);
            var blink = beacon.AddComponent<M4Blink>(); blink.target = bl; blink.period = 2.2f; blink.minIntensity = 0.6f;
            PointLight("Tank Glow", root, new Vector3(0, 2.7f, 2.2f), new Color(0.3f, 0.8f, 1f), 1.5f, 7f);

            // pipes to the ground and a read-out screen
            for (int i = 0; i < 3; i++)
            {
                var a = Quaternion.Euler(0, i * 120f + 15f, 0);
                Prim(PrimitiveType.Cylinder, "Pipe " + i, root, a * new Vector3(0, 0.6f, 2.1f), new Vector3(0.18f, 0.6f, 0.18f), Vector3.zero, dark);
            }
            Prim(PrimitiveType.Cube, "Screen Frame", root, new Vector3(0, 1.6f, 1.48f), new Vector3(1.1f, 0.7f, 0.08f), Vector3.zero, dark);
            Prim(PrimitiveType.Cube, "Screen", root, new Vector3(0, 1.6f, 1.53f), new Vector3(0.95f, 0.55f, 0.02f), Vector3.zero, screen);
            Label3D("H2O 312 L", root, new Vector3(0, 1.62f, 1.56f), new Vector3(0, 180, 0), 0.025f, new Color(0.6f, 1f, 1f));
            // hand repairs: a patch plate and tape bands (someone maintains this)
            Prim(PrimitiveType.Cube, "Patch", root, new Vector3(-1.2f, 1.2f, 0.9f), new Vector3(0.7f, 0.5f, 0.05f), new Vector3(0, -50, 8), dark);
            Prim(PrimitiveType.Cylinder, "Tape", root, new Vector3(0, 4.2f, 0), new Vector3(0.42f, 0.06f, 0.42f), Vector3.zero,
                M4Materials.Pbr("Tape", new Color(0.75f, 0.45f, 0.12f), 0f, 0.35f));
        }

        // ------------------------------------------------------------------ the son's ship and camp

        void BuildSonsShip()
        {
            var root = new GameObject("Son's Ship LK-2590").transform;
            root.SetParent(Root, false);
            root.position = Ground(ShipXZ, 0.2f);
            root.rotation = Quaternion.Euler(0f, 145f, 0f) * Quaternion.Euler(0f, 0f, 5f);
            SonsShip = root;

            var hull = M4Materials.Hull("SonHull", new Color(0.62f, 0.64f, 0.66f), 0.8f, 0.45f);
            var scorch = M4Materials.Pbr("Scorch", new Color(0.05f, 0.045f, 0.04f), 0.3f, 0.15f);
            var dark = M4Materials.Hull("DarkMetal", new Color(0.18f, 0.19f, 0.21f), 0.9f, 0.45f);
            var canopy = M4Materials.Glass("Canopy", new Color(0.05f, 0.08f, 0.1f, 0.75f), 0.3f);
            var orange = M4Materials.Pbr("SafetyOrange", new Color(0.85f, 0.32f, 0.06f), 0.1f, 0.4f);
            var panel = M4Materials.Pbr("SolarPanel", new Color(0.04f, 0.06f, 0.14f), 0.7f, 0.9f);
            var tarp = M4Materials.Pbr("Tarp", new Color(0.55f, 0.42f, 0.25f), 0f, 0.15f);
            var crate = M4Materials.Hull("Crate", new Color(0.82f, 0.82f, 0.78f), 0.2f, 0.35f);
            var red = M4Materials.Emissive("RedLamp", new Color(0.3f, 0f, 0f), new Color(1f, 0.15f, 0.08f), 6f);

            var body = Prim(PrimitiveType.Capsule, "Fuselage", root, new Vector3(0, 1.8f, 0), new Vector3(3.2f, 6f, 3.2f), new Vector3(90, 0, 0), hull, true);
            Prim(PrimitiveType.Sphere, "Canopy", root, new Vector3(0, 2.9f, 3.6f), new Vector3(2.0f, 1.3f, 3.0f), Vector3.zero, canopy);
            Prim(PrimitiveType.Cylinder, "Stripe", root, new Vector3(0, 1.8f, 1.2f), new Vector3(3.25f, 0.18f, 3.25f), new Vector3(90, 0, 0), orange);
            // wings: left intact with solar film, right snapped and dug into the ground
            Prim(PrimitiveType.Cube, "Wing L", root, new Vector3(-4.6f, 1.6f, -0.8f), new Vector3(6.5f, 0.22f, 3.4f), new Vector3(0, 0, 6), hull, true);
            Prim(PrimitiveType.Cube, "Solar L", root, new Vector3(-4.6f, 1.74f, -0.8f), new Vector3(6f, 0.03f, 3f), new Vector3(0, 0, 6), panel);
            Prim(PrimitiveType.Cube, "Wing R stub", root, new Vector3(2.6f, 1.5f, -0.8f), new Vector3(2f, 0.22f, 3.4f), new Vector3(0, 0, -4), hull, true);
            Prim(PrimitiveType.Cube, "Wing R broken", root, new Vector3(5.6f, 0.4f, -1.4f), new Vector3(4.4f, 0.22f, 3.2f), new Vector3(8, 18, -28), scorch, true);
            for (int i = 0; i < 2; i++)
                Prim(PrimitiveType.Cylinder, "Engine " + i, root, new Vector3(i == 0 ? -1f : 1f, 1.6f, -5.6f), new Vector3(1.2f, 1.1f, 1.2f), new Vector3(90, 0, 0), scorch);
            Prim(PrimitiveType.Sphere, "Burn mark", root, new Vector3(0.9f, 2.6f, -2.5f), new Vector3(2.2f, 1.2f, 2.4f), Vector3.zero, scorch);
            for (int i = 0; i < 3; i++)
                Prim(PrimitiveType.Cylinder, "Leg " + i, root, new Vector3(i == 0 ? 0 : (i == 1 ? -1.4f : 1.4f), 0.45f, i == 0 ? 3.5f : -3f),
                    new Vector3(0.25f, 0.6f, 0.25f), Vector3.zero, dark);
            Label3D("LK-2590", root, new Vector3(1.65f, 2.1f, 1.8f), new Vector3(0, -90, 0), 0.06f, new Color(0.15f, 0.15f, 0.15f));

            // hatch (interaction point) on the left side, with a ramp
            SonsHatch = new GameObject("Hatch").transform;
            SonsHatch.SetParent(root, false);
            SonsHatch.localPosition = new Vector3(-2.2f, 0.2f, 1.2f);
            Prim(PrimitiveType.Cube, "Ramp", root, new Vector3(-2.4f, 0.55f, 1.2f), new Vector3(1.6f, 0.08f, 1.4f), new Vector3(0, 0, -28), dark);
            PointLight("Hatch Light", root, new Vector3(-1.9f, 1.6f, 1.2f), new Color(1f, 0.7f, 0.4f), 2.5f, 6f);

            // improvised antenna mast: 4,381 attempts were sent from here
            var mast = new GameObject("Antenna Mast").transform;
            mast.SetParent(root, false);
            mast.localPosition = new Vector3(-6.5f, 0, -5.5f);
            Prim(PrimitiveType.Cylinder, "Pole", mast, new Vector3(0, 3.5f, 0), new Vector3(0.12f, 3.5f, 0.12f), Vector3.zero, dark);
            Prim(PrimitiveType.Sphere, "Dish", mast, new Vector3(0.3f, 6.4f, 0), new Vector3(1.6f, 0.25f, 1.6f), new Vector3(0, 0, 60), hull);
            var lamp = Prim(PrimitiveType.Sphere, "Lamp", mast, new Vector3(0, 7.1f, 0), Vector3.one * 0.2f, Vector3.zero, red);
            var ll = PointLight("Mast Light", mast, new Vector3(0, 7.2f, 0), new Color(1f, 0.2f, 0.1f), 2f, 10f);
            var b = lamp.AddComponent<M4Blink>(); b.target = ll; b.period = 1.1f; b.minIntensity = 0f; b.sharp = true;

            // camp under a tarp: crates taken from the father's ship, water bottles, a warm lamp
            var camp = new GameObject("Camp").transform;
            camp.SetParent(root, false);
            camp.localPosition = new Vector3(-4.5f, 0f, 2.8f);
            Prim(PrimitiveType.Cube, "Tarp", camp, new Vector3(0, 1.7f, 0.6f), new Vector3(4.2f, 0.04f, 3.4f), new Vector3(18, 0, 4), tarp);
            for (int i = 0; i < 2; i++) Prim(PrimitiveType.Cylinder, "Tarp pole " + i, camp, new Vector3(i == 0 ? -1.9f : 1.9f, 0.8f, 2f), new Vector3(0.06f, 0.8f, 0.06f), Vector3.zero, dark);
            for (int i = 0; i < 5; i++)
            {
                var c = Prim(PrimitiveType.Cube, "Crate " + i, camp, new Vector3(-1.3f + (i % 3) * 1.05f, 0.45f + (i / 3) * 0.9f, -0.2f + (i % 2) * 0.15f),
                    new Vector3(0.95f, 0.85f, 0.95f), new Vector3(0, i * 7f, 0), crate, true);
                if (i < 3) Label3D("NASA · SUPPLY", c.transform, new Vector3(0, 0, -0.51f), Vector3.zero, 0.012f, new Color(0.1f, 0.15f, 0.4f));
            }
            for (int i = 0; i < 6; i++)
                Prim(PrimitiveType.Cylinder, "Bottle " + i, camp, new Vector3(1.4f + (i % 3) * 0.25f, 0.2f, 0.8f + (i / 3) * 0.25f),
                    new Vector3(0.18f, 0.2f, 0.18f), Vector3.zero, M4Materials.Glass("WaterBottle", new Color(0.4f, 0.7f, 0.9f, 0.55f)));
            PointLight("Camp Lamp", camp, new Vector3(0.2f, 1.2f, 0.6f), new Color(1f, 0.62f, 0.32f), 3f, 9f, true);
            // The son steps out from around the nose on the hatch side. From here to the reveal mark in front of
            // HatchStandPoint the straight path clears the fuselage, the left wing and the camp crates (all have colliders).
            SonHidingSpot = new GameObject("Son Hiding Spot").transform;
            SonHidingSpot.SetParent(root, false);
            SonHidingSpot.localPosition = new Vector3(-1.0f, 0f, 8.5f);
        }

        /// <summary>
        /// Clear standing spot in front of the hatch, outside every collider: the wing ends at local z 0.9,
        /// the crates start at z 2.1. Used for the son's-log and reveal staging.
        /// </summary>
        public Vector3 HatchStandPoint()
        {
            var p = SonsShip.TransformPoint(new Vector3(-4.6f, 0f, 1.6f));
            return Ground(new Vector2(p.x, p.z), 0.05f);
        }

        /// <summary>Yaw (degrees) that faces the hatch from HatchStandPoint.</summary>
        public float HatchStandYaw()
        {
            Vector3 d = SonsHatch.position - HatchStandPoint(); d.y = 0f;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        // ------------------------------------------------------------------ cable, footprints, flags

        void BuildCable()
        {
            var a = CollectorXZ + new Vector2(1.6f, 1.2f);
            var b = ShipXZ + new Vector2(2.5f, -3f);
            var ctrl = (a + b) * 0.5f + new Vector2(22f, 6f);
            var pts = new List<Vector3>();
            const int n = 140;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                Vector2 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * ctrl + t * t * b;
                pts.Add(Ground(p, 0.08f));
            }
            CablePath = pts.ToArray();

            var go = new GameObject("Power Cable");
            go.transform.SetParent(Root, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = CablePath.Length;
            lr.SetPositions(CablePath);
            lr.widthMultiplier = 0.09f;
            lr.numCapVertices = 2;
            lr.sharedMaterial = M4Materials.Emissive("Cable", new Color(0.05f, 0.06f, 0.07f), new Color(0.2f, 0.75f, 0.9f), 0.9f);
            lr.shadowCastingMode = ShadowCastingMode.On;

            var dark = M4Materials.Hull("DarkMetal", new Color(0.18f, 0.19f, 0.21f), 0.9f, 0.45f);
            var amber = M4Materials.Emissive("AmberLamp", new Color(0.3f, 0.2f, 0f), new Color(1f, 0.6f, 0.15f), 5f);
            int post = 0;
            for (int i = 8; i < CablePath.Length - 4; i += 12, post++)
            {
                var p = new GameObject("Cable Post " + post).transform;
                p.SetParent(Root, false);
                p.position = CablePath[i];
                Prim(PrimitiveType.Cylinder, "Post", p, new Vector3(0, 0.6f, 0), new Vector3(0.07f, 0.6f, 0.07f), Vector3.zero, dark);
                var lamp = Prim(PrimitiveType.Sphere, "Lamp", p, new Vector3(0, 1.25f, 0), Vector3.one * 0.12f, Vector3.zero, amber);
                var bl = lamp.AddComponent<M4Blink>(); bl.period = 1.6f; bl.phase = post * 0.18f; bl.minIntensity = 0.15f;
            }

            // a light pulse running along the cable towards the ship: "follow me"
            var pulse = new GameObject("Cable Pulse");
            pulse.transform.SetParent(Root, false);
            Prim(PrimitiveType.Sphere, "Spark", pulse.transform, Vector3.zero, Vector3.one * 0.22f, Vector3.zero,
                M4Materials.Emissive("PulseGlow", Color.black, new Color(0.4f, 0.95f, 1f), 8f));
            PointLight("Pulse Light", pulse.transform, new Vector3(0, 0.4f, 0), new Color(0.4f, 0.9f, 1f), 3f, 6f);
            pulse.AddComponent<M4PathFollower>().Init(CablePath, 9f);

            // boot prints beside the cable: someone walks this path
            var print = M4Materials.Pbr("BootPrint", new Color(0.12f, 0.09f, 0.08f), 0f, 0.05f);
            var prints = new GameObject("Boot Prints").transform;
            prints.SetParent(Root, false);
            for (int i = 4; i < CablePath.Length - 4; i += 2)
            {
                Vector3 dir = (CablePath[i + 1] - CablePath[i - 1]).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, dir) * ((i / 2) % 2 == 0 ? 0.25f : -0.25f) + Vector3.Cross(Vector3.up, dir) * 1.1f;
                Vector3 pos = CablePath[i] + side;
                pos.y = Height(pos.x, pos.z) + 0.01f;
                var fp = Prim(PrimitiveType.Cube, "Print", prints, Vector3.zero, new Vector3(0.14f, 0.012f, 0.3f), Vector3.zero, print);
                fp.transform.position = pos;
                fp.transform.rotation = Quaternion.LookRotation(dir, GroundNormal(pos.x, pos.z));
                fp.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        void BuildSiteFlags()
        {
            var flagMat = M4Materials.Emissive("SiteFlag", new Color(0.3f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.4f), 2.5f);
            foreach (var s in Sites)
            {
                var p = new GameObject("Site " + s.id + " marker").transform;
                p.SetParent(Root, false);
                p.position = Ground(s.xz);
                Prim(PrimitiveType.Cylinder, "Beacon", p, new Vector3(0, 0.5f, 0), new Vector3(0.25f, 0.5f, 0.25f), Vector3.zero, flagMat);
            }
            var go = new GameObject("Landing Ellipse");
            go.transform.SetParent(Root, false);
            _ellipse = go.AddComponent<LineRenderer>();
            _ellipse.useWorldSpace = true;
            _ellipse.loop = true;
            _ellipse.widthMultiplier = 0.35f;
            _ellipse.sharedMaterial = M4Materials.Emissive("Ellipse", Color.black, new Color(1f, 0.8f, 0.3f), 3f);
            _ellipse.shadowCastingMode = ShadowCastingMode.Off;
            go.SetActive(false);
        }

        public void ShowEllipse(LandingSite s, bool on)
        {
            if (_ellipse == null) return;
            _ellipse.gameObject.SetActive(on);
            if (!on) return;
            const int n = 72;
            _ellipse.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                var xz = s.xz + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s.EllipseRadius;
                _ellipse.SetPosition(i, Ground(xz, 0.25f));
            }
        }

        // ------------------------------------------------------------------ lighting and atmosphere

        void SetupLighting(ExoplanetRecord planet)
        {
            var sunGo = GameObject.Find("Sun Light");
            SunLight = sunGo != null ? sunGo.GetComponent<Light>() : null;
            if (SunLight == null)
            {
                SunLight = new GameObject("Sun Light").AddComponent<Light>();
                SunLight.type = LightType.Directional;
            }
            // Low K-dwarf sun: long raking shadows across the valley.
            Vector3 toSun = new Vector3(0.74f, 0.30f, 0.60f).normalized;
            SunLight.transform.rotation = Quaternion.LookRotation(-toSun);
            float kelvin = float.IsNaN(planet.starTempK) ? 4400f : planet.starTempK;
            SunLight.color = Mathf.CorrelatedColorTemperatureToRGB(Mathf.Clamp(kelvin, 2500f, 9000f));
            float flux = float.IsNaN(planet.insolation) ? 1f : planet.insolation;
            SunLight.intensity = Mathf.Lerp(1.4f, 3.2f, Mathf.Clamp01(Mathf.Sqrt(flux)));   // dimmer star, dimmer light (artistic mapping)
            SunLight.shadows = LightShadows.Soft;
            SunLight.shadowStrength = 0.92f;
            SunLight.shadowBias = 0.04f;
            SunLight.shadowNormalBias = 0.35f;
            RenderSettings.sun = SunLight;

            var sunBody = GameObject.Find("Sun System");
            if (sunBody != null)
            {
                sunBody.transform.position = toSun * 850f;
                sunBody.transform.localScale = Vector3.one * 0.7f;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.10f, 0.11f, 0.17f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.11f, 0.08f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.035f, 0.03f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0011f;
            RenderSettings.fogColor = new Color(0.30f, 0.21f, 0.16f);
            RenderSettings.fog = false; // turned on with the surface

            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = new Color(0.012f, 0.012f, 0.025f);

            var probeGo = new GameObject("Valley Reflection Probe");
            probeGo.transform.SetParent(Root, false);
            probeGo.transform.position = Ground(new Vector2(-20f, 30f), 6f);
            _probe = probeGo.AddComponent<ReflectionProbe>();
            _probe.mode = ReflectionProbeMode.Realtime;
            _probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            _probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            _probe.size = new Vector3(700f, 250f, 700f);
            _probe.resolution = 256;
            _probe.hdr = true;
            _probe.farClipPlane = 1200f;

            // ambient floating dust around the camera
            if (cam != null)
            {
                var old = cam.transform.Find("M4 Ambient Dust");
                _motes = old != null ? old.GetComponent<ParticleSystem>() : M4Fx.AmbientDust(cam.transform);
                _motes.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ characters and lander

        /// <summary>Greybox astronaut, pivot at the feet, about 1.85 m. son = darker worn suit with orange patches.</summary>
        public static Transform BuildAstronaut(string name, bool son)
        {
            var root = new GameObject(name).transform;
            var suit = son ? M4Materials.Pbr("SonSuit", new Color(0.55f, 0.52f, 0.48f), 0f, 0.22f) : M4Materials.SuitFabric();
            var dark = M4Materials.Hull("DarkMetal", new Color(0.18f, 0.19f, 0.21f), 0.9f, 0.45f);
            var visor = son ? M4Materials.Pbr("VisorBlue", new Color(0.05f, 0.12f, 0.2f), 1f, 0.96f)
                            : M4Materials.Pbr("VisorGold", new Color(0.85f, 0.62f, 0.25f), 1f, 0.95f);
            var accent = son ? M4Materials.Pbr("SafetyOrange", new Color(0.85f, 0.32f, 0.06f), 0.1f, 0.4f)
                             : M4Materials.Pbr("NasaBlue", new Color(0.08f, 0.22f, 0.6f), 0.1f, 0.5f);
            var chest = M4Materials.Emissive(son ? "ChestAmber" : "ChestCyan", Color.black, son ? new Color(1f, 0.55f, 0.2f) : new Color(0.3f, 0.9f, 1f), 3f);

            Prim(PrimitiveType.Capsule, "Torso", root, new Vector3(0, 1.15f, 0), new Vector3(0.62f, 0.42f, 0.45f), Vector3.zero, suit);
            Prim(PrimitiveType.Capsule, "Leg L", root, new Vector3(-0.15f, 0.45f, 0), new Vector3(0.26f, 0.45f, 0.26f), Vector3.zero, suit);
            Prim(PrimitiveType.Capsule, "Leg R", root, new Vector3(0.15f, 0.45f, 0), new Vector3(0.26f, 0.45f, 0.26f), Vector3.zero, suit);
            Prim(PrimitiveType.Cube, "Boot L", root, new Vector3(-0.15f, 0.07f, 0.05f), new Vector3(0.22f, 0.14f, 0.34f), Vector3.zero, dark);
            Prim(PrimitiveType.Cube, "Boot R", root, new Vector3(0.15f, 0.07f, 0.05f), new Vector3(0.22f, 0.14f, 0.34f), Vector3.zero, dark);
            Prim(PrimitiveType.Capsule, "Arm L", root, new Vector3(-0.42f, 1.15f, 0), new Vector3(0.2f, 0.36f, 0.2f), new Vector3(0, 0, 8), suit);
            Prim(PrimitiveType.Capsule, "Arm R", root, new Vector3(0.42f, 1.15f, 0), new Vector3(0.2f, 0.36f, 0.2f), new Vector3(0, 0, -8), suit);
            Prim(PrimitiveType.Cube, "Backpack", root, new Vector3(0, 1.25f, -0.33f), new Vector3(0.5f, 0.62f, 0.26f), Vector3.zero, M4Materials.Hull("PackHull", new Color(0.85f, 0.85f, 0.83f), 0.3f, 0.4f));
            Prim(PrimitiveType.Cube, "Shoulder band", root, new Vector3(0, 1.42f, 0), new Vector3(0.64f, 0.06f, 0.47f), Vector3.zero, accent);
            Prim(PrimitiveType.Cube, "Chest light", root, new Vector3(0.12f, 1.28f, 0.24f), new Vector3(0.1f, 0.05f, 0.02f), Vector3.zero, chest);
            Prim(PrimitiveType.Sphere, "Helmet", root, new Vector3(0, 1.66f, 0), Vector3.one * 0.42f, Vector3.zero, M4Materials.Hull("PackHull", Color.white, 0.3f, 0.4f));
            Prim(PrimitiveType.Sphere, "Visor", root, new Vector3(0, 1.67f, 0.08f), new Vector3(0.34f, 0.28f, 0.3f), Vector3.zero, visor);
            return root;
        }

        /// <summary>The father's lander. Uses person 3's Resources/LastSignal/Ship prefab when it exists.</summary>
        public static Transform BuildLander(out Transform engineSocket, out Transform dish)
        {
            var root = new GameObject("Father's Lander").transform;
            dish = null;
            var shared = Resources.Load<GameObject>("LastSignal/Ship");
            if (shared != null)
            {
                var v = Instantiate(shared, root, false);
                v.name = "Ship (person 3)";
                engineSocket = v.transform.Find("EngineSocket");
                if (engineSocket == null) { engineSocket = new GameObject("EngineSocket").transform; engineSocket.SetParent(root, false); engineSocket.localPosition = new Vector3(0, 1f, 0); }
                return root;
            }

            var white = M4Materials.Hull("LanderHull", new Color(0.9f, 0.9f, 0.88f), 0.55f, 0.55f);
            var dark = M4Materials.Hull("DarkMetal", new Color(0.18f, 0.19f, 0.21f), 0.9f, 0.45f);
            var gold = M4Materials.Pbr("GoldFoil", new Color(0.92f, 0.68f, 0.28f), 1f, 0.72f, null, M4Materials.NormalFromNoise(128, 6, 6f, 71), 0.8f, new Vector2(3, 3));
            var win = M4Materials.Emissive("Window", Color.black, new Color(1f, 0.8f, 0.55f), 2.5f);
            var navR = M4Materials.Emissive("NavRed", Color.black, new Color(1f, 0.1f, 0.05f), 6f);
            var navG = M4Materials.Emissive("NavGreen", Color.black, new Color(0.1f, 1f, 0.3f), 6f);
            var bell = M4Materials.Pbr("EngineBell", new Color(0.12f, 0.1f, 0.09f), 1f, 0.6f);

            Prim(PrimitiveType.Cylinder, "Descent stage", root, new Vector3(0, 2.6f, 0), new Vector3(3.4f, 0.75f, 3.4f), Vector3.zero, gold);
            Prim(PrimitiveType.Cylinder, "Ascent stage", root, new Vector3(0, 4.2f, 0), new Vector3(2.7f, 0.9f, 2.7f), Vector3.zero, white);
            Prim(PrimitiveType.Sphere, "Cabin dome", root, new Vector3(0, 5.1f, 0), new Vector3(2.6f, 1.3f, 2.6f), Vector3.zero, white);
            Prim(PrimitiveType.Cube, "Window", root, new Vector3(0, 4.6f, 1.32f), new Vector3(0.9f, 0.35f, 0.05f), Vector3.zero, win);
            Prim(PrimitiveType.Cylinder, "Engine bell", root, new Vector3(0, 1.5f, 0), new Vector3(1.1f, 0.5f, 1.1f), Vector3.zero, bell);
            for (int i = 0; i < 4; i++)
            {
                var q = Quaternion.Euler(0, 45f + i * 90f, 0);
                var leg = Prim(PrimitiveType.Cylinder, "Leg " + i, root, q * new Vector3(0, 1.2f, 2.0f), new Vector3(0.14f, 1.35f, 0.14f), Vector3.zero, dark);
                leg.transform.localRotation = q * Quaternion.Euler(28f, 0, 0);
                Prim(PrimitiveType.Cylinder, "Foot " + i, root, q * new Vector3(0, 0.05f, 2.55f), new Vector3(0.6f, 0.05f, 0.6f), Vector3.zero, dark);
                Prim(PrimitiveType.Cube, "RCS " + i, root, q * new Vector3(0, 4.3f, 1.45f), new Vector3(0.3f, 0.3f, 0.3f), Vector3.zero, dark);
            }
            Prim(PrimitiveType.Sphere, "Nav R", root, new Vector3(-1.75f, 2.6f, 0), Vector3.one * 0.12f, Vector3.zero, navR);
            Prim(PrimitiveType.Sphere, "Nav G", root, new Vector3(1.75f, 2.6f, 0), Vector3.one * 0.12f, Vector3.zero, navG);
            var dishRoot = new GameObject("High-gain dish").transform;
            dishRoot.SetParent(root, false);
            dishRoot.localPosition = new Vector3(0.9f, 6.1f, -0.4f);
            Prim(PrimitiveType.Cylinder, "Dish arm", dishRoot, new Vector3(0, 0.3f, 0), new Vector3(0.06f, 0.3f, 0.06f), Vector3.zero, dark);
            Prim(PrimitiveType.Sphere, "Dish", dishRoot, new Vector3(0, 0.65f, 0), new Vector3(1.1f, 0.2f, 1.1f), new Vector3(60, 0, 0), white);
            dish = dishRoot;
            engineSocket = new GameObject("EngineSocket").transform;
            engineSocket.SetParent(root, false);
            engineSocket.localPosition = new Vector3(0, 1.0f, 0);
            return root;
        }
    }
}
