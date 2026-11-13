using UnityEngine;
using System.Collections.Generic;

/// <summary>Material factory (URP) + OBJ model loading with material remapping.</summary>
public static class Mats
{
    static Dictionary<string, Material> cache = new Dictionary<string, Material>();

    public static Shader Sh(string n)
    {
        var s = Shader.Find(n);
        if (s == null) Debug.LogError("[Space] Shader not found: " + n);
        return s;
    }

    public static Material Lit(Color c, float metal = 0f, float smooth = 0.4f, Color? emission = null)
    {
        var m = new Material(Sh("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c); m.SetColor("_Color", c);
        m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
        if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); }
        return m;
    }

    public static Material Unlit(Color hdr)
    {
        var m = new Material(Sh("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", hdr); m.SetColor("_Color", hdr);
        return m;
    }

    /// <summary>Space/Glow material. additive=true -> One One, else alpha blend.</summary>
    public static Material Glow(Color hdr, float power, float intensity, bool additive = true, float baseAlpha = 0f, float sunMask = 0f, bool cullFront = false)
    {
        var m = new Material(Sh("Space/Glow"));
        m.SetColor("_Color", hdr); m.SetFloat("_Power", power); m.SetFloat("_Intensity", intensity);
        m.SetFloat("_Base", baseAlpha); m.SetFloat("_SunMask", sunMask);
        m.SetFloat("_Src", additive ? 1f : 5f); m.SetFloat("_Dst", additive ? 1f : 10f);
        m.SetFloat("_Cull", cullFront ? 1f : 2f);
        return m;
    }

    public static Texture2D Tex(string n)
    {
        var t = Resources.Load<Texture2D>("Textures/" + n);
        if (t == null) Debug.LogWarning("[Space] Missing texture " + n);
        return t;
    }

    // ---- named material table used to re-skin imported OBJ meshes (names come from the .mtl files)
    public static Material Named(string name)
    {
        Material m; if (cache.TryGetValue(name, out m) && m != null) return m;
        m = Create(name); cache[name] = m; return m;
    }

    static Material Create(string n)
    {
        Color Hex(float r, float g, float b) { return new Color(r, g, b); }
        switch (n)
        {
            // spaceship
            case "ship_hull": return Lit(Hex(0.82f, 0.86f, 0.92f), 0.85f, 0.78f);
            case "ship_dark": return Lit(Hex(0.07f, 0.08f, 0.1f), 0.9f, 0.6f);
            case "ship_accent": return Lit(Hex(0.1f, 0.28f, 0.65f), 0.7f, 0.65f);
            case "ship_engine": return Lit(Hex(0.2f, 0.21f, 0.25f), 1f, 0.55f);
            case "ship_glow": return Unlit(new Color(0.15f, 2.2f, 3.2f, 1f));
            case "ship_glass": return Glow(new Color(0.35f, 0.75f, 1f, 1f), 2.2f, 1.6f, false, 0.22f);
            // astronaut
            case "suit_white": return Lit(Hex(0.9f, 0.9f, 0.88f), 0f, 0.35f);
            case "suit_dark": return Lit(Hex(0.12f, 0.12f, 0.14f), 0.3f, 0.3f);
            case "suit_pack": return Lit(Hex(0.72f, 0.74f, 0.78f), 0.6f, 0.5f);
            case "suit_boot": return Lit(Hex(0.18f, 0.18f, 0.2f), 0f, 0.2f);
            case "suit_orange": return Lit(Hex(1f, 0.45f, 0.08f), 0f, 0.4f);
            case "suit_blue": return Lit(Hex(0.15f, 0.35f, 0.95f), 0f, 0.4f);
            case "suit_glow": return Unlit(new Color(0.3f, 2.5f, 2.2f, 1f));
            case "suit_visor": return Lit(Hex(0.95f, 0.72f, 0.2f), 1f, 0.97f);
            // city (from buildings.mtl)
            case "glass_blue": return Lit(Hex(0.25f, 0.45f, 0.75f), 0.6f, 0.92f);
            case "glass_tinted": return Lit(Hex(0.12f, 0.2f, 0.22f), 0.6f, 0.9f);
            case "glass_clear": return Lit(Hex(0.45f, 0.58f, 0.66f), 0.6f, 0.92f);
            case "concrete_dark": return Lit(Hex(0.3f, 0.3f, 0.3f), 0f, 0.15f);
            case "concrete_light": return Lit(Hex(0.72f, 0.72f, 0.68f), 0f, 0.2f);
            case "brick": return Lit(Hex(0.55f, 0.27f, 0.2f), 0f, 0.1f);
            case "roof_tile": return Lit(Hex(0.4f, 0.2f, 0.15f), 0f, 0.15f);
            case "siding_white": return Lit(Hex(0.9f, 0.9f, 0.86f), 0f, 0.25f);
            case "roof_dark": return Lit(Hex(0.18f, 0.18f, 0.2f), 0f, 0.2f);
            case "metal_corrugated": return Lit(Hex(0.5f, 0.5f, 0.55f), 0.8f, 0.5f);
            case "steel_frame": return Lit(Hex(0.4f, 0.42f, 0.45f), 0.8f, 0.55f);
            case "asphalt": return Lit(Hex(0.1f, 0.1f, 0.11f), 0f, 0.25f);
            case "grass": return Lit(Hex(0.18f, 0.42f, 0.12f), 0f, 0.05f);
            case "wood_bench": return Lit(Hex(0.45f, 0.3f, 0.15f), 0f, 0.2f);
            case "tree_bark": return Lit(Hex(0.3f, 0.2f, 0.1f), 0f, 0.1f);
            case "tree_leaves": return Lit(Hex(0.12f, 0.45f, 0.1f), 0f, 0.1f);
            case "lamp_metal": return Lit(Hex(0.3f, 0.3f, 0.32f), 0.8f, 0.5f);
            case "sidewalk": return Lit(Hex(0.6f, 0.58f, 0.55f), 0f, 0.15f);
            case "antenna_red": return Lit(Hex(0.85f, 0.1f, 0.1f), 0f, 0.4f);
        }
        return Lit(new Color(0.6f, 0.6f, 0.6f));
    }

    /// <summary>Replace every material on a model by name (imported .mtl names) using Named().</summary>
    public static void Reskin(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var src = r.sharedMaterials; var dst = new Material[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                string nm = src[i] != null ? src[i].name.Replace(" (Instance)", "") : "none";
                dst[i] = Named(nm);
            }
            r.sharedMaterials = dst;
        }
    }

    /// <summary>Load an OBJ from Resources/Models, instantiate it and re-skin it.</summary>
    public static GameObject Model(string path, Transform parent = null, bool skin = true)
    {
        var p = Resources.Load<GameObject>("Models/" + path);
        if (p == null) { Debug.LogError("[Space] Missing model: " + path); return new GameObject("missing_" + path); }
        var go = Object.Instantiate(p, parent);
        go.name = System.IO.Path.GetFileName(path);
        go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        if (skin) Reskin(go);
        return go;
    }

    public static Mesh FirstMesh(GameObject go)
    {
        var mf = go.GetComponentInChildren<MeshFilter>(true);
        return mf != null ? mf.sharedMesh : null;
    }
}

/// <summary>Latitude/longitude frame of a textured sphere, measured from the imported mesh so it is correct whatever the OBJ import handedness.</summary>
public class PlanetGeo
{
    public Vector3 d0, d90;      // directions of (lat0,lon0) and (lat0,lon+90E) in the model's local space
    public bool mirrored;        // true -> the model must be scaled x = -1 to look correct
    public Texture2D readable;

    public PlanetGeo(Mesh m, Texture2D tex)
    {
        readable = tex;
        var v = m.vertices; var uv = m.uv;
        int a = 0, b = 0; float da = 1e9f, db = 1e9f;
        for (int i = 0; i < v.Length; i++)
        {
            float ea = (uv[i].x - 0.5f) * (uv[i].x - 0.5f) + (uv[i].y - 0.5f) * (uv[i].y - 0.5f);
            float eb = (uv[i].x - 0.75f) * (uv[i].x - 0.75f) + (uv[i].y - 0.5f) * (uv[i].y - 0.5f);
            if (ea < da) { da = ea; a = i; }
            if (eb < db) { db = eb; b = i; }
        }
        d0 = v[a].normalized; d90 = v[b].normalized;
        // left-handed world: a correct planet has Cross(east, north) = -up
        mirrored = Vector3.Dot(Vector3.Cross(d90, Vector3.up), d0) > 0f;
        if (mirrored) { d0.x = -d0.x; d90.x = -d90.x; }   // directions in the parent space after the x = -1 fix
    }

    public Vector3 Dir(double latDeg, double lonDeg)
    {
        float la = (float)(latDeg * Mathf.Deg2Rad), lo = (float)(lonDeg * Mathf.Deg2Rad);
        return (Mathf.Cos(la) * (Mathf.Cos(lo) * d0 + Mathf.Sin(lo) * d90) + Mathf.Sin(la) * Vector3.up).normalized;
    }
    public void LatLon(Vector3 dir, out double lat, out double lon)
    {
        dir.Normalize();
        lat = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        lon = Mathf.Atan2(Vector3.Dot(dir, d90), Vector3.Dot(dir, d0)) * Mathf.Rad2Deg;
    }
    public void Frame(Vector3 up, out Vector3 east, out Vector3 north)
    {
        double la, lo; LatLon(up, out la, out lo);
        float lr = (float)(lo * Mathf.Deg2Rad);
        east = (-Mathf.Sin(lr) * d0 + Mathf.Cos(lr) * d90).normalized;
        north = (Vector3.up - up * Vector3.Dot(Vector3.up, up)).normalized;
        if (north.sqrMagnitude < 1e-6f) north = Vector3.forward;
    }
    /// <summary>Colour of the surface texture at this lat/lon.</summary>
    public Color Sample(double lat, double lon)
    {
        if (readable == null) return new Color(0.2f, 0.4f, 0.2f);
        float u = (float)(lon / 360.0 + 0.5), v = (float)(lat / 180.0 + 0.5);
        try { return readable.GetPixelBilinear(u, v); } catch { return new Color(0.2f, 0.4f, 0.2f); }
    }
}
