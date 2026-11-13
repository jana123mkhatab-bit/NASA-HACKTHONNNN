using System;
using UnityEngine;

/// <summary>Scale + physics constants. 1 unit = 63.71 km. One simulated day = DayLen real seconds (x720 time compression).
/// GM values are derived so orbital periods are self-consistent with the compressed clock.</summary>
public static class SC
{
    public const double KmPerUnit = 63.71;
    public const double EarthR = 100, MoonR = 27.3, SunR = 1500;
    public const double EarthOrbitR = 40000, MoonOrbitR = 6030;
    public const double DayLen = 120.0;            // real seconds per simulated day at 1x
    public const double YearDays = 365.256, MonthDays = 27.3217;
    public static readonly double GM_E, GM_M, GM_S;
    public static double TimeScale { get { return 86400.0 / DayLen; } }

    static SC()
    {
        double wm = 2 * Math.PI / (MonthDays * DayLen);
        GM_E = wm * wm * MoonOrbitR * MoonOrbitR * MoonOrbitR;
        GM_M = GM_E * 0.0123;
        double we = 2 * Math.PI / (YearDays * DayLen);
        GM_S = we * we * EarthOrbitR * EarthOrbitR * EarthOrbitR;
    }

    public static double KmS(double unitsPerSec) { return unitsPerSec * KmPerUnit / TimeScale; }
    public static double Km(double units) { return units * KmPerUnit; }
}

/// <summary>Double-precision vector for solar-system-scale positions.</summary>
public struct V3d
{
    public double x, y, z;
    public V3d(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
    public V3d(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public static V3d zero { get { return new V3d(0, 0, 0); } }
    public static V3d operator +(V3d a, V3d b) { return new V3d(a.x + b.x, a.y + b.y, a.z + b.z); }
    public static V3d operator -(V3d a, V3d b) { return new V3d(a.x - b.x, a.y - b.y, a.z - b.z); }
    public static V3d operator *(V3d a, double s) { return new V3d(a.x * s, a.y * s, a.z * s); }
    public static V3d operator *(double s, V3d a) { return new V3d(a.x * s, a.y * s, a.z * s); }
    public static V3d operator /(V3d a, double s) { return new V3d(a.x / s, a.y / s, a.z / s); }
    public static V3d operator -(V3d a) { return new V3d(-a.x, -a.y, -a.z); }
    public double mag { get { return Math.Sqrt(x * x + y * y + z * z); } }
    public double sqr { get { return x * x + y * y + z * z; } }
    public V3d norm { get { double m = mag; return m > 1e-12 ? this / m : zero; } }
    public static double Dot(V3d a, V3d b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
    public static V3d Cross(V3d a, V3d b) { return new V3d(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
    public Vector3 f { get { return new Vector3((float)x, (float)y, (float)z); } }
    public override string ToString() { return "(" + x.ToString("F1") + "," + y.ToString("F1") + "," + z.ToString("F1") + ")"; }
}

public enum GameMode { Menu, Space, Surface, Interior, Dead }

public static class U
{
    public static float Remap(float v, float a, float b, float c, float d) { return Mathf.Lerp(c, d, Mathf.InverseLerp(a, b, v)); }
    public static Vector3 Damp(Vector3 cur, Vector3 tgt, float rate, float dt) { return Vector3.Lerp(cur, tgt, 1f - Mathf.Exp(-rate * dt)); }
    public static float Damp(float cur, float tgt, float rate, float dt) { return Mathf.Lerp(cur, tgt, 1f - Mathf.Exp(-rate * dt)); }
    public static Quaternion Damp(Quaternion cur, Quaternion tgt, float rate, float dt) { return Quaternion.Slerp(cur, tgt, 1f - Mathf.Exp(-rate * dt)); }
    public static string Fmt(double v, string unit)
    {
        double a = Math.Abs(v);
        if (a >= 1e6) return (v / 1e6).ToString("F2") + " M" + unit;
        if (a >= 1e3) return (v / 1e3).ToString("F1") + " k" + unit;
        return v.ToString("F1") + " " + unit;
    }
}
