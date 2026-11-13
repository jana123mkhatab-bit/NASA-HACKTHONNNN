using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Camera + global post-processing volume built in code (bloom, ACES, vignette, colour grade, aberration).</summary>
public static class PostFX
{
    public static Volume vol; static Bloom bloom; static Vignette vig; static ChromaticAberration ca; static ColorAdjustments col; static Tonemapping tm; static FilmGrain grain;

    public static void Setup(Camera cam)
    {
        cam.allowHDR = true; cam.allowMSAA = true; cam.nearClipPlane = 0.1f; cam.farClipPlane = 220000f;
        var cd = cam.GetUniversalAdditionalCameraData();
        cd.renderPostProcessing = true; cd.renderShadows = true; cd.stopNaN = true; cd.dithering = true;
        cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; cd.antialiasingQuality = AntialiasingQuality.High;

        var go = new GameObject("PostFX"); vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10;
        var p = ScriptableObject.CreateInstance<VolumeProfile>(); vol.profile = p;
        bloom = p.Add<Bloom>(true); bloom.threshold.Override(1.05f); bloom.intensity.Override(0.85f); bloom.scatter.Override(0.72f); bloom.highQualityFiltering.Override(true);
        tm = p.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
        vig = p.Add<Vignette>(true); vig.intensity.Override(0.26f); vig.smoothness.Override(0.5f); vig.color.Override(Color.black);
        ca = p.Add<ChromaticAberration>(true); ca.intensity.Override(0f);
        col = p.Add<ColorAdjustments>(true); col.postExposure.Override(0.15f); col.contrast.Override(10f); col.saturation.Override(14f);
        grain = p.Add<FilmGrain>(true); grain.type.Override(FilmGrainLookup.Thin1); grain.intensity.Override(0.08f);
    }

    /// <summary>boost 0..1 (aberration), heat 0..1 (orange vignette), damage 0..1 (red vignette)</summary>
    public static void Dynamic(float boost, float heat, float damage, float bloomScale)
    {
        if (vig == null) return;
        ca.intensity.Override(Mathf.Clamp01(boost) * 0.45f);
        float h = Mathf.Max(heat, damage);
        vig.intensity.Override(Mathf.Lerp(0.26f, 0.62f, h));
        vig.color.Override(Color.Lerp(Color.black, damage > heat ? new Color(0.7f, 0.02f, 0.02f) : new Color(1f, 0.38f, 0.05f), h));
        bloom.intensity.Override(0.85f * bloomScale + heat * 0.8f);
    }

    public static void Quality(int level)   // 0 low, 1 medium, 2 high
    {
        if (bloom == null) return;
        bloom.highQualityFiltering.Override(level >= 1);
        grain.intensity.Override(level >= 2 ? 0.08f : 0f);
        QualitySettings.SetQualityLevel(Mathf.Clamp(level, 0, QualitySettings.names.Length - 1), true);
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.shadowDistance = level >= 2 ? 220f : 120f;
    }
}
