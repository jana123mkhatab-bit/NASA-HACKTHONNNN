using System;
using System.Collections.Generic;
using UnityEngine;

public enum SurfaceKind { Spaceport, Land, Ocean, Ice, Moon }

public struct Poi { public string name; public Vector3 pos; public Color color; }

/// <summary>Builds the local walkable world for a landing: spaceport city, wilderness, ocean platform or lunar terrain,
/// plus sky, sun/day-night cycle and Earth/Moon in the sky at their true directions.</summary>
public class SurfaceWorld : MonoBehaviour
{
    public static SurfaceWorld I;
    public SurfaceKind kind; public Body body; public double lat, lon;
    public float gravity = 9.81f; public bool atmosphere = true;
    public Quaternion qSite = Quaternion.identity;           // scene vector -> planet-local vector
    public Vector3 upLocal;
    public List<Poi> pois = new List<Poi>();
    public Vector3 doorPos; public float halfSize = 1350f;
    public float dayFactor = 1f;
    public string siteName = "";

    SolarSystem ss; GameObject root, skyRig; Material skyMat;
    Transform skyEarth, skyMoon, skySun; Material skyEarthMat, skyMoonMat;
    float seed;
    public const double PortLat = 28.57, PortLon = -80.65;

    void Awake() { I = this; }

    // ------------------------------------------------------------------ BUILD
    public void Build(SolarSystem solar, Body b, Vector3 siteDirLocal)
    {
        Teardown();
        ss = solar; body = b; upLocal = siteDirLocal.normalized;
        Vector3 east, north; b.geo.Frame(upLocal, out east, out north);
        qSite = Quaternion.LookRotation(north, upLocal);
        b.geo.LatLon(upLocal, out lat, out lon);
        seed = (float)(lat * 3.7 + lon * 1.9);
        root = new GameObject("SurfaceWorld");
        pois.Clear();

        bool isMoon = b == ss.moon;
        gravity = isMoon ? 1.62f : 9.81f; atmosphere = !isMoon;
        if (isMoon) { kind = SurfaceKind.Moon; siteName = "Moon (" + lat.ToString("F1") + "°, " + lon.ToString("F1") + "°)"; }
        else
        {
            Vector3 port = b.geo.Dir(PortLat, PortLon);
            if (Vector3.Angle(port, upLocal) < 18f) { kind = SurfaceKind.Spaceport; siteName = "Cape Spaceport, Florida"; }
            else
            {
                Color c = b.geo.Sample(lat, lon);
                if (Mathf.Abs((float)lat) > 66f || (c.r + c.g + c.b) / 3f > 0.78f) kind = SurfaceKind.Ice;
                else if (c.b > c.r * 1.25f && c.b > c.g * 1.02f) kind = SurfaceKind.Ocean;
                else kind = SurfaceKind.Land;
                siteName = kind + " region (" + lat.ToString("F1") + "°, " + lon.ToString("F1") + "°)";
            }
        }

        SetupSky(isMoon);
        switch (kind)
        {
            case SurfaceKind.Moon: BuildMoon(); break;
            case SurfaceKind.Ocean: BuildOcean(); break;
            case SurfaceKind.Spaceport: BuildSpaceport(); break;
            default: BuildWilderness(kind == SurfaceKind.Ice); break;
        }
        pois.Add(new Poi { name = "LANDING PAD", pos = new Vector3(0, 2, 0), color = new Color(0.3f, 1f, 1f) });
    }

    public void Teardown()
    {
        if (root != null) Destroy(root);
        if (skyRig != null) Destroy(skyRig);
        RenderSettings.skybox = null; RenderSettings.fog = false;
    }

    // ------------------------------------------------------------------ SKY
    void SetupSky(bool moon)
    {
        skyRig = new GameObject("SkyRig");
        if (!moon)
        {
            skyMat = new Material(Mats.Sh("Skybox/Procedural"));
            skyMat.SetFloat("_SunSize", 0.05f); skyMat.SetFloat("_SunSizeConvergence", 4f); skyMat.SetFloat("_AtmosphereThickness", 1.0f);
            skyMat.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.5f)); skyMat.SetColor("_GroundColor", new Color(0.4f, 0.38f, 0.34f)); skyMat.SetFloat("_Exposure", 1.3f);
            RenderSettings.skybox = skyMat;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = 0.00032f;
            skyMoon = MakeSkyBody("Celestial/moon", ss.moon, out skyMoonMat, 2.85f);
        }
        else
        {
            skyEarth = MakeSkyBody("Celestial/earth", ss.earth, out skyEarthMat, 10f);
            skyEarthMat.SetTexture("_NightTex", Mats.Tex("earth_nightmap")); skyEarthMat.SetFloat("_Haze", 1f);
            RenderSettings.fog = false;
            var sg = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(sg.GetComponent<Collider>()); sg.name = "SkySun";
            sg.transform.SetParent(skyRig.transform, false);
            sg.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(new Color(14f, 11f, 7f));
            sg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; skySun = sg.transform;
        }
    }

    Transform MakeSkyBody(string path, Body other, out Material mat, float modelRadius)
    {
        var g = Mats.Model(path, skyRig.transform, false);
        mat = new Material(Mats.Sh("Space/Planet"));
        if (other == ss.earth) { mat.SetTexture("_MainTex", Mats.Tex("earth_daymap")); mat.SetFloat("_DetailFar", 0); }
        else { mat.SetTexture("_MainTex", Mats.Tex("moon")); mat.SetFloat("_Spec", 0); mat.SetFloat("_Haze", 0); mat.SetFloat("_DetailFar", 0); mat.SetFloat("_Ambient", 0.004f); }
        foreach (var r in g.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        return g.transform;
    }

    void UpdateSkyBody(Transform t, Body other, Material mat, float modelRadius, Vector3 sceneSun)
    {
        if (t == null) return;
        Vector3 toOtherWorld = (other.pos - body.pos).norm.f;
        Vector3 dirScene = Quaternion.Inverse(qSite) * (Quaternion.Inverse(body.rot) * toOtherWorld);
        double dist = (other.pos - body.pos).mag;
        float ang = Mathf.Atan((float)(other.radius / dist));
        float D = 9000f, R = D * Mathf.Tan(ang);
        t.position = Camera.main.transform.position + dirScene * D;
        float mir = other.geo != null && other.geo.mirrored ? -1f : 1f;
        float s = R / modelRadius; t.localScale = new Vector3(mir * s, s, s);
        t.rotation = Quaternion.Inverse(qSite) * Quaternion.Inverse(body.rot) * other.rot;
        mat.SetVector("_SunDir", new Vector4(sceneSun.x, sceneSun.y, sceneSun.z, 0));
        mat.SetFloat("_SunIntensity", 1.6f);
    }

    // ------------------------------------------------------------------ PER FRAME
    public Vector3 SceneSunDir()
    {
        Vector3 toSunWorld = (ss.sun.pos - body.pos).norm.f;
        return Quaternion.Inverse(qSite) * (Quaternion.Inverse(body.rot) * toSunWorld);
    }

    public bool suspend;     // true while the player is inside the interior hall
    public void Tick()
    {
        if (ss == null || body == null || Camera.main == null || suspend) return;
        Vector3 sd = SceneSunDir(); float elev = sd.y;
        var L = ss.sunLight;
        L.transform.rotation = Quaternion.LookRotation(-sd);
        skyRig.transform.position = Camera.main.transform.position;
        if (atmosphere)
        {
            dayFactor = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-0.12f, 0.25f, elev));
            L.intensity = Mathf.Lerp(0f, 2.0f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-0.04f, 0.18f, elev)));
            L.color = Color.Lerp(new Color(1f, 0.55f, 0.28f), new Color(1f, 0.96f, 0.9f), Mathf.InverseLerp(0f, 0.35f, elev));
            skyMat.SetFloat("_Exposure", Mathf.Lerp(0.15f, 1.3f, dayFactor));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.01f, 0.015f, 0.04f), new Color(0.38f, 0.5f, 0.75f), dayFactor);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.008f, 0.01f, 0.025f), new Color(0.32f, 0.36f, 0.42f), dayFactor);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.004f, 0.004f, 0.01f), new Color(0.16f, 0.15f, 0.13f), dayFactor);
            RenderSettings.fogColor = Color.Lerp(new Color(0.01f, 0.015f, 0.03f), new Color(0.62f, 0.72f, 0.86f), dayFactor);
            ss.SetStarDim(1f - dayFactor);
            UpdateSkyBody(skyMoon, ss.moon, skyMoonMat, 2.85f, sd);
        }
        else
        {
            dayFactor = elev > 0 ? 1f : 0f;
            L.intensity = elev > -0.01f ? 2.6f : 0f; L.color = new Color(1f, 0.97f, 0.92f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.035f, 0.04f);
            ss.SetStarDim(1f);
            UpdateSkyBody(skyEarth, ss.earth, skyEarthMat, 10f, sd);
            if (skySun != null) { float D = 12000f; skySun.position = Camera.main.transform.position + sd * D; skySun.localScale = Vector3.one * (float)(2 * D * 0.0046f); }
        }
        if (ss.Dome != null) ss.Dome.position = Camera.main.transform.position;
    }

    // ------------------------------------------------------------------ TERRAIN HELPERS
    static float Fbm(float x, float z, int oct, float seed)
    {
        float a = 1, f = 1, s = 0, t = 0;
        for (int i = 0; i < oct; i++) { s += a * Mathf.PerlinNoise(x * f + seed + i * 17.3f, z * f - seed * 0.7f + i * 9.1f); t += a; a *= 0.5f; f *= 2.05f; }
        return s / t;
    }

    Mesh MakeTerrain(int n, float size, Func<float, float, float> H, Func<float, float, float, Color> C)
    {
        var v = new Vector3[(n + 1) * (n + 1)]; var col = new Color[v.Length];
        float cell = size / n;
        for (int j = 0; j <= n; j++)
            for (int i = 0; i <= n; i++)
            {
                float x = -size / 2 + i * cell, z = -size / 2 + j * cell, h = H(x, z);
                int k = j * (n + 1) + i; v[k] = new Vector3(x, h, z); col[k] = C(x, z, h);
            }
        var t = new int[n * n * 6]; int q = 0;
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                int a = j * (n + 1) + i, b = a + 1, c = a + n + 1, d = c + 1;
                t[q++] = a; t[q++] = c; t[q++] = b; t[q++] = b; t[q++] = c; t[q++] = d;
            }
        var m = new Mesh(); m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = v; m.colors = col; m.triangles = t; m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    GameObject AddTerrain(Mesh m, string name = "Terrain")
    {
        var g = new GameObject(name); g.transform.SetParent(root.transform, false);
        g.AddComponent<MeshFilter>().sharedMesh = m;
        var mat = new Material(Mats.Sh("Space/Terrain"));
        g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        g.AddComponent<MeshCollider>().sharedMesh = m;
        return g;
    }

    static float FlatBlend(float x, float z, float cx, float cz, float r0, float r1)
    {
        float d = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));
        return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(r0, r1, d));
    }

    // ------------------------------------------------------------------ MOON
    void BuildMoon()
    {
        var rng = new System.Random((int)(seed * 1000));
        int nc = 70; var cx = new float[nc]; var cz = new float[nc]; var cr = new float[nc];
        for (int i = 0; i < nc; i++)
        {
            cx[i] = (float)(rng.NextDouble() * 2 - 1) * 1500f; cz[i] = (float)(rng.NextDouble() * 2 - 1) * 1500f;
            cr[i] = 8f + Mathf.Pow((float)rng.NextDouble(), 2.2f) * 150f;
            if (Mathf.Sqrt(cx[i] * cx[i] + cz[i] * cz[i]) < cr[i] + 90f) { cx[i] += 400f; }
        }
        float sd = seed;
        Func<float, float, float> H = (x, z) =>
        {
            float h = (Fbm(x * 0.004f, z * 0.004f, 5, sd) - 0.5f) * 38f + (Fbm(x * 0.03f, z * 0.03f, 3, sd + 9) - 0.5f) * 2.2f;
            for (int i = 0; i < nc; i++)
            {
                float dx = x - cx[i], dz = z - cz[i], d2 = dx * dx + dz * dz, r = cr[i];
                if (d2 > r * r * 4f) continue;
                float d = Mathf.Sqrt(d2) / r, depth = 0.17f * r;
                if (d < 1f) h += -depth * (1f - d * d); 
                float rim = 0.32f * depth * Mathf.Exp(-Mathf.Pow((d - 1.05f) / 0.22f, 2f)); h += rim;
            }
            return h * FlatBlend(x, z, 0, 0, 60, 160);
        };
        Func<float, float, float, Color> C = (x, z, h) =>
        {
            float n = Fbm(x * 0.05f, z * 0.05f, 3, sd + 3);
            float s = 0.78f + n * 0.45f + Mathf.Clamp(h * 0.004f, -0.12f, 0.12f);
            return new Color(0.55f * s, 0.53f * s, 0.5f * s);
        };
        AddTerrain(MakeTerrain(250, 3000f, H, C));
        ScatterRocks(rng, 260, H, new Color(0.45f, 0.43f, 0.41f), 150f);
        BuildPad(false);
        pois.Add(new Poi { name = "EARTH-FACING SITE", pos = new Vector3(0, 40, 0), color = new Color(0.5f, 0.8f, 1f) });
    }

    // ------------------------------------------------------------------ WILDERNESS / ICE
    void BuildWilderness(bool ice)
    {
        float sd = seed;
        Color c0 = body.geo.Sample(lat, lon);
        bool desert = c0.r > c0.g * 1.08f;
        Color baseC = ice ? new Color(0.88f, 0.92f, 0.96f) : desert ? Color.Lerp(c0, new Color(0.7f, 0.55f, 0.35f), 0.5f) : Color.Lerp(c0, new Color(0.16f, 0.36f, 0.1f), 0.6f);
        Func<float, float, float> H = (x, z) => ((Fbm(x * 0.0025f, z * 0.0025f, 5, sd) - 0.5f) * 110f + (Fbm(x * 0.02f, z * 0.02f, 3, sd + 4) - 0.5f) * 4f) * FlatBlend(x, z, 0, 0, 70, 220);
        Func<float, float, float, Color> C = (x, z, h) =>
        {
            float n = Fbm(x * 0.02f, z * 0.02f, 3, sd + 1);
            Color c = baseC * (0.72f + n * 0.55f);
            if (!ice && h > 36f) c = Color.Lerp(c, new Color(0.42f, 0.4f, 0.38f), Mathf.InverseLerp(36f, 70f, h));
            if (!ice && h > 62f) c = Color.Lerp(c, Color.white, Mathf.InverseLerp(62f, 85f, h));
            c.a = 1; return c;
        };
        AddTerrain(MakeTerrain(250, 3000f, H, C));
        var rng = new System.Random((int)(seed * 1000));
        if (!ice && !desert) ScatterTrees(rng, 420, H);
        ScatterRocks(rng, 90, H, ice ? new Color(0.7f, 0.75f, 0.8f) : new Color(0.4f, 0.38f, 0.35f), 220f);
        BuildPad(true);
    }

    // ------------------------------------------------------------------ OCEAN
    void BuildOcean()
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube); w.name = "Ocean"; w.transform.SetParent(root.transform, false);
        w.transform.localScale = new Vector3(9000, 2, 9000); w.transform.position = new Vector3(0, -1.2f, 0);
        w.GetComponent<Renderer>().sharedMaterial = Mats.Lit(new Color(0.02f, 0.14f, 0.26f), 0f, 0.96f);
        // landing barge
        var p = GameObject.CreatePrimitive(PrimitiveType.Cube); p.name = "Barge"; p.transform.SetParent(root.transform, false);
        p.transform.localScale = new Vector3(110, 3, 70); p.transform.position = new Vector3(0, -1.5f, 0);
        p.GetComponent<Renderer>().sharedMaterial = Mats.Named("concrete_dark");
        BuildPadMarkings(root.transform, 38f, 0.01f);
        RenderSettings.fogDensity = 0.00045f;
        siteName = "Open ocean - emergency barge";
    }

    // ------------------------------------------------------------------ SPACEPORT
    void BuildSpaceport()
    {
        float sd = seed;
        Func<float, float, float> H = (x, z) => ((Fbm(x * 0.003f, z * 0.003f, 4, sd) - 0.5f) * 40f) * FlatBlend(x, z, 0, 330, 560, 1050) - 0.25f;
        Func<float, float, float, Color> C = (x, z, h) => { float n = Fbm(x * 0.03f, z * 0.03f, 3, sd); return new Color(0.2f + n * 0.12f, 0.4f + n * 0.14f, 0.12f + n * 0.06f); };
        AddTerrain(MakeTerrain(250, 3000f, H, C));
        // city district (630 x 630 m) ahead of the pad
        var city = Mats.Model("Buildings/city_district", root.transform);
        city.transform.position = new Vector3(0, 0, 420);
        AddMeshColliders(city);
        pois.Add(new Poi { name = "CITY DISTRICT", pos = new Vector3(0, 30, 420), color = new Color(1f, 0.8f, 0.3f) });
        BuildPad(true);
        var rng = new System.Random(77);
        // hangar, towers, skyline around the pad
        Place("Buildings/warehouse_01", new Vector3(-120, 0, 10), 90f);
        Place("Buildings/warehouse_01", new Vector3(-120, 0, 60), 90f);
        Place("Buildings/tower_antenna", new Vector3(95, 0, -40), 0f);
        Place("Buildings/tower_antenna", new Vector3(110, 0, 35), 30f);
        Place("Buildings/skyscraper_01", new Vector3(170, 0, 90), 20f);
        Place("Buildings/skyscraper_02", new Vector3(-200, 0, 150), 0f);
        // Mission Control (office) + glowing entrance
        var office = Place("Buildings/office_building_01", new Vector3(-70, 0, 125), 0f);
        doorPos = new Vector3(-70, 0, 104.5f);
        BuildDoorArch(doorPos);
        pois.Add(new Poi { name = "MISSION CONTROL", pos = doorPos + Vector3.up * 5f, color = new Color(0.3f, 1f, 0.9f) });
        for (int i = 0; i < 10; i++) Place("Buildings/street_lamp", new Vector3(-30 + i * 6.5f, 0, 70), 0f);
        ScatterTrees(rng, 160, (x, z) => 0f, 140f, 380f);
    }

    GameObject Place(string path, Vector3 p, float yaw)
    {
        var g = Mats.Model(path, root.transform); g.transform.position = p; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        AddMeshColliders(g); return g;
    }

    void AddMeshColliders(GameObject g)
    {
        foreach (var mf in g.GetComponentsInChildren<MeshFilter>()) if (mf.GetComponent<Collider>() == null && mf.sharedMesh != null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
    }

    void BuildDoorArch(Vector3 p)
    {
        var g = new GameObject("MissionDoor"); g.transform.SetParent(root.transform, false); g.transform.position = p;
        Material glow = Mats.Unlit(new Color(0.2f, 2.4f, 2.2f));
        Action<Vector3, Vector3> bar = (c, s) => { var b = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(b.GetComponent<Collider>()); b.transform.SetParent(g.transform, false); b.transform.localPosition = c; b.transform.localScale = s; b.GetComponent<Renderer>().sharedMaterial = glow; };
        bar(new Vector3(-2.2f, 2.2f, 0), new Vector3(0.3f, 4.4f, 0.3f)); bar(new Vector3(2.2f, 2.2f, 0), new Vector3(0.3f, 4.4f, 0.3f)); bar(new Vector3(0, 4.45f, 0), new Vector3(4.7f, 0.3f, 0.3f));
        var f = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(f.GetComponent<Collider>()); f.transform.SetParent(g.transform, false);
        f.transform.localPosition = new Vector3(0, 0.02f, 0); f.transform.localScale = new Vector3(5f, 0.02f, 5f); f.GetComponent<Renderer>().sharedMaterial = glow;
        var l = new GameObject("L"); l.transform.SetParent(g.transform, false); l.transform.localPosition = new Vector3(0, 3, -1);
        var li = l.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(0.3f, 1f, 0.9f); li.range = 18; li.intensity = 6;
        var tm = new GameObject("Sign"); tm.transform.SetParent(g.transform, false); tm.transform.localPosition = new Vector3(0, 5.2f, 0);
        var t = tm.AddComponent<TextMesh>(); t.text = "MISSION CONTROL"; t.characterSize = 0.22f; t.fontSize = 60; t.anchor = TextAnchor.MiddleCenter; t.color = new Color(0.5f, 1f, 0.95f);
        tm.transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    // ------------------------------------------------------------------ PAD + SCATTER
    void BuildPad(bool concrete)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder); p.name = "Pad"; p.transform.SetParent(root.transform, false);
        p.transform.localScale = new Vector3(92f, 0.35f, 92f); p.transform.position = new Vector3(0, -0.34f, 0);
        p.GetComponent<Renderer>().sharedMaterial = kind == SurfaceKind.Moon ? Mats.Lit(new Color(0.3f, 0.3f, 0.32f), 0.2f, 0.3f) : Mats.Named("concrete_dark");
        BuildPadMarkings(root.transform, 40f, 0.02f);
    }

    void BuildPadMarkings(Transform parent, float r, float y)
    {
        var ring = Mats.Unlit(new Color(0.15f, 1.8f, 2.2f)); var white = Mats.Unlit(new Color(1.6f, 1.6f, 1.6f));
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI * 2 / 24;
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(s.GetComponent<Collider>()); s.transform.SetParent(parent, false);
            s.transform.position = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); s.transform.localScale = new Vector3(1.6f, 0.08f, 0.5f);
            s.transform.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90f, 0); s.GetComponent<Renderer>().sharedMaterial = ring;
        }
        // 'H'-style landing mark: three bars
        Action<Vector3, Vector3> bar = (c, sz) => { var b = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(b.GetComponent<Collider>()); b.transform.SetParent(parent, false); b.transform.position = c; b.transform.localScale = sz; b.GetComponent<Renderer>().sharedMaterial = white; };
        bar(new Vector3(-8, y, 0), new Vector3(1.5f, 0.06f, 18f)); bar(new Vector3(8, y, 0), new Vector3(1.5f, 0.06f, 18f)); bar(new Vector3(0, y, 0), new Vector3(16f, 0.06f, 1.5f));
        // beacon beam
        var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(beam.GetComponent<Collider>()); beam.transform.SetParent(parent, false);
        beam.transform.position = new Vector3(0, 250, 0); beam.transform.localScale = new Vector3(3f, 250f, 3f);
        beam.GetComponent<Renderer>().sharedMaterial = Mats.Glow(new Color(0.2f, 0.9f, 1f), 1.2f, 0.9f, true, 0.05f); beam.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void ScatterTrees(System.Random rng, int count, Func<float, float, float> H, float minR = 100f, float maxR = 1300f)
    {
        var prefab = Resources.Load<GameObject>("Models/Buildings/tree_01"); if (prefab == null) return;
        var holder = new GameObject("Trees"); holder.transform.SetParent(root.transform, false);
        for (int i = 0; i < count; i++)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2, r = minR + (float)rng.NextDouble() * (maxR - minR);
            float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r + (kind == SurfaceKind.Spaceport ? 300f : 0f);
            if (kind == SurfaceKind.Spaceport && Mathf.Abs(x) < 340f && z > 70f && z < 760f) continue;
            float y = H(x, z);
            var t = Instantiate(prefab, holder.transform); t.transform.position = new Vector3(x, y - 0.1f, z);
            t.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0); t.transform.localScale = Vector3.one * (1.6f + (float)rng.NextDouble() * 2.2f);
            Mats.Reskin(t);
        }
    }

    void ScatterRocks(System.Random rng, int count, Func<float, float, float> H, Color tint, float minR)
    {
        var holder = new GameObject("Rocks"); holder.transform.SetParent(root.transform, false);
        var mat = Mats.Lit(tint, 0f, 0.1f);
        var meshes = new Mesh[4]; for (int i = 0; i < 4; i++) meshes[i] = RockMesh(rng.Next());
        for (int i = 0; i < count; i++)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2, r = 45f + (float)rng.NextDouble() * 1350f;
            float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r, s = 0.4f + Mathf.Pow((float)rng.NextDouble(), 3f) * 6f;
            var g = new GameObject("Rock"); g.transform.SetParent(holder.transform, false);
            g.transform.position = new Vector3(x, H(x, z) + s * 0.25f, z); g.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 360f, 0);
            g.transform.localScale = new Vector3(s, s * (0.5f + (float)rng.NextDouble() * 0.5f), s * (0.7f + (float)rng.NextDouble() * 0.6f));
            var m = meshes[rng.Next(4)]; g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (s > 1.2f) g.AddComponent<SphereCollider>().radius = 0.5f;
        }
    }

    static Mesh RockMesh(int seed)
    {
        int nl = 9, nm = 14; var rng = new System.Random(seed); float s0 = seed * 0.013f;
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i <= nl; i++)
            for (int j = 0; j < nm; j++)
            {
                float ph = Mathf.PI * i / nl, th = Mathf.PI * 2 * j / nm;
                var d = new Vector3(Mathf.Sin(ph) * Mathf.Cos(th), Mathf.Cos(ph), Mathf.Sin(ph) * Mathf.Sin(th));
                float r = 0.5f * (0.75f + Mathf.PerlinNoise(d.x * 2.1f + s0, d.z * 2.1f + d.y * 1.7f) * 0.5f);
                v.Add(d * r);
            }
        for (int i = 0; i < nl; i++)
            for (int j = 0; j < nm; j++)
            {
                int a = i * nm + j, b = i * nm + (j + 1) % nm, c = (i + 1) * nm + j, d = (i + 1) * nm + (j + 1) % nm;
                t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
            }
        var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }
}
