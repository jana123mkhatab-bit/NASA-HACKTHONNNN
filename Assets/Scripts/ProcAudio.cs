using UnityEngine;

/// <summary>Synthesised sound effects so the project needs no audio assets.</summary>
public static class ProcAudio
{
    const int SR = 44100;
    public static AudioClip Engine()
    {
        int n = SR * 3; var d = new float[n]; float b = 0, lp = 0; var rng = new System.Random(5);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR; float w = (float)(rng.NextDouble() * 2 - 1);
            b = (b + 0.02f * w) / 1.02f; lp += (b * 3.2f - lp) * 0.08f;
            float tone = Mathf.Sin(2 * Mathf.PI * 52f * t) * 0.35f + Mathf.Sin(2 * Mathf.PI * 104.3f * t) * 0.18f + Mathf.Sin(2 * Mathf.PI * 26f * t) * 0.3f;
            d[i] = (tone + lp * 1.2f) * 0.45f;
        }
        for (int i = 0; i < 2000; i++) { float f = i / 2000f; d[i] *= f; d[n - 1 - i] = Mathf.Lerp(d[n - 1 - i], d[i], 1f - f); }  // loop seam
        var c = AudioClip.Create("engine", n, 1, SR, false); c.SetData(d, 0); return c;
    }
    public static AudioClip Wind()
    {
        int n = SR * 4; var d = new float[n]; float b = 0, lp = 0; var rng = new System.Random(9);
        for (int i = 0; i < n; i++)
        {
            float w = (float)(rng.NextDouble() * 2 - 1); b = (b + 0.02f * w) / 1.02f; lp += (b * 9f - lp) * 0.02f;
            d[i] = lp * (0.7f + 0.3f * Mathf.Sin(i / (float)SR * 0.9f));
        }
        for (int i = 0; i < 3000; i++) { float f = i / 3000f; d[i] *= f; d[n - 1 - i] = Mathf.Lerp(d[n - 1 - i], d[i], 1f - f); }
        var c = AudioClip.Create("wind", n, 1, SR, false); c.SetData(d, 0); return c;
    }
    public static AudioClip Blip(float f0, float f1, float len, float vol = 0.3f)
    {
        int n = (int)(SR * len); var d = new float[n];
        for (int i = 0; i < n; i++) { float t = i / (float)n; float f = Mathf.Lerp(f0, f1, t); d[i] = Mathf.Sin(2 * Mathf.PI * f * i / SR) * (1f - t) * (1f - t) * vol; }
        var c = AudioClip.Create("blip", n, 1, SR, false); c.SetData(d, 0); return c;
    }
    public static AudioClip Thud()
    {
        int n = SR / 2; var d = new float[n]; var rng = new System.Random(3); float lp = 0;
        for (int i = 0; i < n; i++) { float t = i / (float)n; float w = (float)(rng.NextDouble() * 2 - 1); lp += (w - lp) * 0.04f; d[i] = (Mathf.Sin(2 * Mathf.PI * 55f * i / SR) * 0.7f + lp) * Mathf.Pow(1f - t, 3f) * 0.8f; }
        var c = AudioClip.Create("thud", n, 1, SR, false); c.SetData(d, 0); return c;
    }
}
