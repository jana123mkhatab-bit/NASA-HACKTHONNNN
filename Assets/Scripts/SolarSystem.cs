using System;
using UnityEngine;

public class Body
{
    public string name; public double radius, gm; public Body parent;
    public double orbitR, periodDays, ecc, inclDeg, phase;      // Keplerian orbit
    public double spinDays, tiltDeg, spinPhaseDeg;
    public bool tidallyLocked;
    public V3d pos, vel;                                         // vel = units per REAL second at 1x warp
    public Quaternion rot = Quaternion.identity;
    public GameObject root, model;
    public PlanetGeo geo;
    public Material[] sunDirMats = new Material[0];
    public double soi;                                           // sphere of influence (reference frame radius)

    public V3d PosAt(double days)
    {
        if (orbitR <= 0) return parent != null ? parent.PosAt(days) : V3d.zero;
        double M = phase + 2 * Math.PI * days / periodDays, E = M;
        for (int i = 0; i < 7; i++) E = M + ecc * Math.Sin(E);
        double x = orbitR * (Math.Cos(E) - ecc), z = orbitR * Math.Sqrt(1 - ecc * ecc) * Math.Sin(E);
        double ci = Math.Cos(inclDeg * Math.PI / 180), si = Math.Sin(inclDeg * Math.PI / 180);
        var p = new V3d(x, z * si, z * ci);
        return (parent != null ? parent.PosAt(days) : V3d.zero) + p;
    }

    public Quaternion RotAt(double days, V3d sunPos, V3d earthPos)
    {
        if (tidallyLocked && geo != null)
        {
            Vector3 toEarth = (earthPos - PosAt(days)).norm.f;
            var qWorld = Quaternion.LookRotation(toEarth, Vector3.up);
            var qLocal = Quaternion.LookRotation(geo.d0, Vector3.up);
            return qWorld * Quaternion.Inverse(qLocal);
        }
        double deg = 360.0 * days / spinDays + spinPhaseDeg;
        var tilt = Quaternion.AngleAxis((float)tiltDeg, Vector3.forward);
        return tilt * Quaternion.AngleAxis((float)(-(deg % 360.0)), Vector3.up);    // negative = west-to-east spin in Unity's left-handed space
    }
}

/// <summary>Sun, Earth (surface + clouds + atmosphere), Moon, star dome. Positions in double precision, rendered around a floating origin.</summary>
public class SolarSystem : MonoBehaviour
{
    public Body sun, earth, moon; public Body[] all;
    public double simDays;
    public static readonly double[] WarpSteps = { 1, 5, 20, 100, 500, 2500 };
    public int warpIndex = 0;
    public double Warp { get { return WarpSteps[warpIndex]; } }
    public Light sunLight;
    public Transform skyDome;
    Material skyMat;
    Transform cloudT;
    public static readonly DateTime Epoch = new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);
    public DateTime Date { get { return Epoch.AddDays(simDays); } }

    public void Init()
    {
        Texture2D day = Mats.Tex("earth_daymap"), night = Mats.Tex("earth_nightmap"), clouds = Mats.Tex("earth_clouds"),
                  moonT = Mats.Tex("moon"), sunT = Mats.Tex("sun"), stars = Mats.Tex("stars_milkyway");

        sun = new Body { name = "Sun", radius = SC.SunR, gm = SC.GM_S, spinDays = 25.4, tiltDeg = 7.25 };
        earth = new Body { name = "Earth", radius = SC.EarthR, gm = SC.GM_E, parent = sun, orbitR = SC.EarthOrbitR, periodDays = SC.YearDays, ecc = 0.0167, phase = 1.1,
                           spinDays = 0.99727, tiltDeg = 23.44, soi = 12000 };
        moon = new Body { name = "Moon", radius = SC.MoonR, gm = SC.GM_M, parent = earth, orbitR = SC.MoonOrbitR, periodDays = SC.MonthDays, ecc = 0.0549, inclDeg = 5.14, phase = 2.2,
                          tidallyLocked = true, soi = 1100 };
        all = new[] { sun, earth, moon };

        // ---------------- SUN
        sun.root = new GameObject("Sun");
        sun.model = Mats.Model("Celestial/sun", sun.root.transform, false);
        sun.model.transform.localScale = Vector3.one * (float)(SC.SunR / 20.0);
        var sunMat = new Material(Mats.Sh("Space/SunSurface")); sunMat.SetTexture("_MainTex", sunT); sunMat.SetColor("_Color", new Color(2.7f, 1.75f, 0.8f));
        foreach (var r in sun.model.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = sunMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
        var corona = Mats.Model("Celestial/sun_corona", sun.root.transform, false);
        corona.transform.localScale = Vector3.one * (float)(SC.SunR / 20.0) * 1.03f;
        var cMat = Mats.Glow(new Color(1.0f, 0.55f, 0.18f), 2.0f, 2.6f, true);
        foreach (var r in corona.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = cMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        var halo = Mats.Model("Celestial/sun_corona", sun.root.transform, false);
        halo.transform.localScale = Vector3.one * (float)(SC.SunR / 20.0) * 1.7f;
        var hMat = Mats.Glow(new Color(1.0f, 0.45f, 0.1f), 4.0f, 1.2f, true);
        foreach (var r in halo.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = hMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }

        // ---------------- EARTH
        earth.root = new GameObject("Earth");
        earth.model = Mats.Model("Celestial/earth", earth.root.transform, false);
        earth.geo = new PlanetGeo(Mats.FirstMesh(earth.model), day);
        float es = (float)(SC.EarthR / 10.0);
        var eMat = new Material(Mats.Sh("Space/Planet"));
        eMat.SetTexture("_MainTex", day); eMat.SetTexture("_NightTex", night);
        eMat.SetFloat("_Haze", 1.0f); eMat.SetFloat("_Detail", 0.35f); eMat.SetFloat("_DetailScale", 900); eMat.SetFloat("_DetailFar", 420);
        foreach (var r in earth.model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = eMat;
        var cl = Mats.Model("Celestial/earth_clouds", earth.root.transform, false); cloudT = cl.transform;
        var clMat = new Material(Mats.Sh("Space/Clouds")); clMat.SetTexture("_MainTex", clouds);
        foreach (var r in cl.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = clMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        var at = Mats.Model("Celestial/earth_atmosphere", earth.root.transform, false);
        float atmoS = es * (float)((SC.EarthR * 1.06) / (SC.EarthR * 1.015));
        var aMat = Mats.Glow(new Color(0.28f, 0.55f, 1.0f), 2.6f, 2.4f, true, 0f, 1f);
        foreach (var r in at.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = aMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        float sx = earth.geo.mirrored ? -1f : 1f;
        earth.model.transform.localScale = new Vector3(sx * es, es, es);
        cl.transform.localScale = new Vector3(sx * es, es, es);
        at.transform.localScale = new Vector3(sx * atmoS, atmoS, atmoS);
        earth.sunDirMats = new[] { eMat, clMat, aMat };

        // ---------------- MOON
        moon.root = new GameObject("Moon");
        moon.model = Mats.Model("Celestial/moon", moon.root.transform, false);
        moon.geo = new PlanetGeo(Mats.FirstMesh(moon.model), moonT);
        var mMat = new Material(Mats.Sh("Space/Planet"));
        mMat.SetTexture("_MainTex", moonT); mMat.SetFloat("_Spec", 0f); mMat.SetFloat("_Haze", 0f); mMat.SetFloat("_Detail", 0.55f);
        mMat.SetFloat("_DetailScale", 500); mMat.SetFloat("_DetailFar", 250); mMat.SetFloat("_Ambient", 0.006f); mMat.SetFloat("_NightStrength", 0f);
        foreach (var r in moon.model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mMat;
        float ms = (float)(SC.MoonR / 2.85);
        moon.model.transform.localScale = new Vector3((moon.geo.mirrored ? -1f : 1f) * ms, ms, ms);
        moon.sunDirMats = new[] { mMat };

        // ---------------- sun light + star dome
        var lg = new GameObject("SunLight"); sunLight = lg.AddComponent<Light>();
        sunLight.type = LightType.Directional; sunLight.color = new Color(1f, 0.96f, 0.9f); sunLight.intensity = 2.2f;
        sunLight.shadows = LightShadows.Soft; sunLight.shadowStrength = 0.95f;
        RenderSettings.sun = sunLight;

        var dome = Mats.Model("Celestial/space_skybox", null, false);
        dome.name = "StarDome"; skyDome = dome.transform; skyDome.localScale = Vector3.one * 300f;   // radius 500*300 = 150,000
        skyMat = new Material(Mats.Sh("Space/Sky")); skyMat.SetTexture("_MainTex", stars);
        foreach (var r in dome.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = skyMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }

        // initial spin phase: subsolar point over the western Atlantic so the spaceport starts in daylight
        simDays = 0; UpdatePositions(0);
        double best = 1e9, bestPhase = 0;
        for (int p = 0; p < 360; p += 2)
        {
            earth.spinPhaseDeg = p; earth.rot = earth.RotAt(0, sun.pos, earth.pos);
            var toSun = Quaternion.Inverse(earth.rot) * (sun.pos - earth.pos).norm.f;
            double la, lo; earth.geo.LatLon(toSun, out la, out lo);
            double dlon = Math.Abs(((lo + 70) % 360 + 540) % 360 - 180);
            if (dlon < best) { best = dlon; bestPhase = p; }
        }
        earth.spinPhaseDeg = bestPhase;
        UpdatePositions(0);
    }

    /// <summary>Advance simulated time and recompute body state. Returns nothing; use Body.pos afterwards.</summary>
    public void Tick(double realDt, double timeFactor)
    {
        double dDays = realDt * timeFactor / SC.DayLen;
        simDays += dDays;
        UpdatePositions(dDays);
        if (cloudT != null) cloudT.localRotation *= Quaternion.Euler(0, (float)(-0.35 * realDt * timeFactor * 0.1), 0);
    }

    void UpdatePositions(double dDays)
    {
        foreach (var b in all)
        {
            b.pos = b.PosAt(simDays);
            double h = 1e-5;
            b.vel = (b.PosAt(simDays + h) - b.pos) / (h * SC.DayLen);
        }
        foreach (var b in all) b.rot = b.RotAt(simDays, sun.pos, earth.pos);
    }

    /// <summary>Position ship-relative to the floating origin.</summary>
    public void Place(V3d origin)
    {
        foreach (var b in all)
        {
            b.root.transform.position = (b.pos - origin).f;
            b.root.transform.rotation = b.rot;
            var toSun = (sun.pos - b.pos).norm; var v = new Vector4((float)toSun.x, (float)toSun.y, (float)toSun.z, 0);
            foreach (var m in b.sunDirMats) if (m != null) m.SetVector("_SunDir", v);
        }
        var ts = (origin - sun.pos).norm;
        sunLight.transform.rotation = Quaternion.LookRotation(ts.f);
    }

    public void SetVisible(bool v) { foreach (var b in all) b.root.SetActive(v); }
    public void SetStarDim(float d) { skyMat.SetFloat("_Dim", d); }
    public Transform Dome { get { return skyDome; } }

    public void ChangeWarp(int d)
    {
        warpIndex = Mathf.Clamp(warpIndex + d, 0, WarpSteps.Length - 1);
    }

    /// <summary>Strongest gravitational acceleration at p (all bodies).</summary>
    public V3d Gravity(V3d p)
    {
        V3d a = V3d.zero;
        foreach (var b in all)
        {
            V3d d = b.pos - p; double r2 = d.sqr; double r = Math.Sqrt(r2);
            double rr = Math.Max(r, b.radius * 0.5);
            a = a + d * (b.gm / (rr * rr * rr));
        }
        return a;
    }

    public Body ReferenceBody(V3d p)
    {
        if ((p - moon.pos).mag < moon.soi) return moon;
        if ((p - earth.pos).mag < earth.soi) return earth;
        return sun;
    }

    public Body Nearest(V3d p, out double altitude)
    {
        Body best = sun; double bd = 1e18;
        foreach (var b in all) { double a = (p - b.pos).mag - b.radius; if (a < bd) { bd = a; best = b; } }
        altitude = bd; return best;
    }
}
