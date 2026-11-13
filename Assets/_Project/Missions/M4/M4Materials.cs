using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.M4
{
    /// <summary>
    /// Runtime PBR materials for the planet scene, built on URP/Lit (no extra packages).
    /// Terrain and rock maps are generated procedurally (tileable value-noise fBm) so the scene ships with
    /// no texture assets: base colour + normal map + URP detail albedo/normal for close-up grain.
    /// </summary>
    public static class M4Materials
    {
        static Shader _lit, _unlit, _particles;
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        // A shader that a player build stripped makes Shader.Find return null, and new Material(null) throws,
        // which would stop the whole planet from building. Fall back down a chain instead.
        static Shader Lit => _lit != null ? _lit
            : (_lit = FindFirst("Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Universal Render Pipeline/Unlit"));
        static Shader UnlitShader => _unlit != null ? _unlit
            : (_unlit = FindFirst("Universal Render Pipeline/Unlit", "Universal Render Pipeline/Lit"));
        static Shader ParticleShader => _particles != null ? _particles
            : (_particles = FindFirst("Universal Render Pipeline/Particles/Unlit", "Universal Render Pipeline/Unlit", "Universal Render Pipeline/Lit"));

        static Shader FindFirst(params string[] names)
        {
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            Debug.LogError("[M4] None of these shaders are in the build: " + string.Join(", ", names) +
                           ". Add one to Project Settings > Graphics > Always Included Shaders.");
            return Shader.Find("Hidden/InternalErrorShader");
        }

        public static void ClearCache() => Cache.Clear();

        // ------------------------------------------------------------------ generic PBR

        public static Material Pbr(string name, Color baseColor, float metallic, float smoothness,
            Texture2D albedo = null, Texture2D normal = null, float normalScale = 1f, Vector2? tiling = null)
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;
            var m = new Material(Lit) { name = "M4_" + name, enableInstancing = true };
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            if (albedo != null) { m.SetTexture("_BaseMap", albedo); }
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", normalScale);
                m.EnableKeyword("_NORMALMAP");
            }
            if (tiling.HasValue) m.SetTextureScale("_BaseMap", tiling.Value);
            Cache[name] = m;
            return m;
        }

        public static Material Emissive(string name, Color baseColor, Color emission, float intensity)
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;
            var m = Pbr(name, baseColor, 0f, 0.6f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        /// <summary>Transparent glass/visor: dark tint, mirror-smooth, picks up the reflection probe.</summary>
        public static Material Glass(string name, Color tint, float metallic = 0.1f)
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;
            var m = Pbr(name, tint, metallic, 0.97f);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            return m;
        }

        public static Material Unlit(string name, Color c)
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;
            var m = new Material(UnlitShader) { name = "M4_" + name };
            m.SetColor("_BaseColor", c);
            Cache[name] = m;
            return m;
        }

        /// <summary>Additive (fire, glow) or alpha-blended (dust) particle material with a soft round sprite.</summary>
        public static Material Particle(string name, bool additive)
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;
            var m = new Material(ParticleShader) { name = "M4_" + name };
            m.SetTexture("_BaseMap", SoftDot());
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)(additive ? BlendMode.SrcAlpha : BlendMode.SrcAlpha));
            m.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (!additive) m.EnableKeyword("_ALPHAPREMULTIPLY_OFF");
            m.renderQueue = (int)RenderQueue.Transparent;
            Cache[name] = m;
            return m;
        }

        // ------------------------------------------------------------------ the planet's surfaces

        /// <summary>Terrain: macro colour over the whole map (UV 0..1) plus tiled detail grain.</summary>
        public static Material Terrain(Texture2D macroAlbedo, Texture2D macroNormal)
        {
            var m = Pbr("Terrain", Color.white, 0f, 0.18f, macroAlbedo, macroNormal, 1.0f);
            m.SetTexture("_DetailAlbedoMap", DetailAlbedo());
            m.SetTexture("_DetailNormalMap", DetailNormal());
            m.SetFloat("_DetailAlbedoMapScale", 1f);
            m.SetFloat("_DetailNormalMapScale", 0.9f);
            m.SetTextureScale("_DetailAlbedoMap", new Vector2(140f, 140f));
            m.EnableKeyword("_DETAIL_MULX2");
            return m;
        }

        public static Material Rock()
        {
            if (Cache.TryGetValue("Rock", out var c)) return c;
            var alb = NoiseTexture(128, 6, 1.8f, new Color(0.30f, 0.24f, 0.21f), new Color(0.52f, 0.43f, 0.36f), 5);
            var nrm = NormalFromNoise(128, 8, 3.0f, 11);
            return Pbr("Rock", Color.white, 0f, 0.22f, alb, nrm, 1.3f, new Vector2(2, 2));
        }

        /// <summary>Weathered spacecraft hull: brushed light metal with panel lines baked into the normal map.</summary>
        public static Material Hull(string name, Color tint, float metallic = 0.85f, float smooth = 0.55f)
        {
            if (Cache.TryGetValue(name, out var c)) return c;
            return Pbr(name, tint, metallic, smooth, HullAlbedo(), HullNormal(), 0.7f, new Vector2(2, 2));
        }

        public static Material SuitFabric()
        {
            if (Cache.TryGetValue("Suit", out var c)) return c;
            var alb = NoiseTexture(64, 4, 0.08f, new Color(0.82f, 0.82f, 0.80f), new Color(0.93f, 0.93f, 0.91f), 21);
            return Pbr("Suit", Color.white, 0f, 0.28f, alb, NormalFromNoise(64, 16, 0.8f, 23), 0.5f, new Vector2(3, 3));
        }

        // ------------------------------------------------------------------ texture generators

        static Texture2D _dot, _detailAlb, _detailNrm, _hullAlb, _hullNrm;

        public static Texture2D SoftDot()
        {
            if (_dot != null) return _dot;
            const int n = 64;
            _dot = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "M4_SoftDot" };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color(1, 1, 1, a);
                }
            _dot.SetPixels(px); _dot.Apply();
            return _dot;
        }

        static Texture2D DetailAlbedo()
        {
            if (_detailAlb != null) return _detailAlb;
            // Centred on 0.5 grey because URP multiplies detail albedo by 2.
            _detailAlb = NoiseTexture(256, 16, 1f, new Color(0.36f, 0.36f, 0.36f), new Color(0.64f, 0.64f, 0.64f), 31, true);
            return _detailAlb;
        }

        static Texture2D DetailNormal()
        {
            if (_detailNrm != null) return _detailNrm;
            _detailNrm = NormalFromNoise(256, 32, 2.2f, 37);
            return _detailNrm;
        }

        static Texture2D HullAlbedo()
        {
            if (_hullAlb != null) return _hullAlb;
            const int n = 256;
            _hullAlb = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "M4_HullAlb", wrapMode = TextureWrapMode.Repeat };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float grime = Noise.Fbm(u * 4f, v * 4f, 4, 4, 41);                 // large stains
                    float brush = Noise.Fbm(u * 64f, v * 2f, 2, 64, 43) * 0.06f;        // brushed streaks
                    float scratch = Mathf.Pow(Noise.Fbm(u * 32f, v * 32f, 3, 32, 47), 6f) * 0.5f;
                    float panel = (x % 64 < 2 || y % 64 < 2) ? 0.55f : 1f;            // panel seams
                    float g = (0.78f + brush - grime * 0.22f + scratch) * panel;
                    px[y * n + x] = new Color(g, g, g * 0.98f, 1f);
                }
            _hullAlb.SetPixels(px); _hullAlb.Apply(true);
            return _hullAlb;
        }

        static Texture2D HullNormal()
        {
            if (_hullNrm != null) return _hullNrm;
            const int n = 256;
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int px = x % 64, py = y % 64;
                    float seam = (px < 2 || py < 2) ? 0f : 1f;                                       // recessed seams
                    float rivet = (Mathf.Abs(px - 6) < 2 && Mathf.Abs(py - 6) < 2) ? 0.6f : 0f;     // rivets
                    float dent = Noise.Fbm(x / (float)n * 6f, y / (float)n * 6f, 3, 6, 53) * 0.35f; // dents
                    h[y * n + x] = seam + rivet + dent;
                }
            _hullNrm = HeightToNormal(h, n, 2.5f, "M4_HullNrm");
            return _hullNrm;
        }

        /// <summary>Tileable fBm noise mapped between two colours.</summary>
        public static Texture2D NoiseTexture(int n, int period, float contrast, Color a, Color b, int seed, bool linear = false)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true, linear) { wrapMode = TextureWrapMode.Repeat, name = "M4_Noise" + seed };
            t.anisoLevel = 8;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = Noise.Fbm(x / (float)n * period, y / (float)n * period, 5, period, seed);
                    v = Mathf.Clamp01(0.5f + (v - 0.5f) * (1f + contrast));
                    px[y * n + x] = Color.Lerp(a, b, v);
                }
            t.SetPixels(px); t.Apply(true);
            return t;
        }

        public static Texture2D NormalFromNoise(int n, int period, float strength, int seed)
        {
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    h[y * n + x] = Noise.Fbm(x / (float)n * period, y / (float)n * period, 5, period, seed);
            return HeightToNormal(h, n, strength, "M4_Nrm" + seed);
        }

        /// <summary>Converts a tileable height field to a tangent-space normal map (RGB = XYZ, A = 1, works with URP's unpack).</summary>
        public static Texture2D HeightToNormal(float[] h, int n, float strength, string name, bool wrap = true)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { name = name, wrapMode = wrap ? TextureWrapMode.Repeat : TextureWrapMode.Clamp };
            t.anisoLevel = 8;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int xl = wrap ? (x - 1 + n) % n : Mathf.Max(0, x - 1), xr = wrap ? (x + 1) % n : Mathf.Min(n - 1, x + 1);
                    int yd = wrap ? (y - 1 + n) % n : Mathf.Max(0, y - 1), yu = wrap ? (y + 1) % n : Mathf.Min(n - 1, y + 1);
                    float dx = (h[y * n + xr] - h[y * n + xl]) * strength;
                    float dy = (h[yu * n + x] - h[yd * n + x]) * strength;
                    var nv = new Vector3(-dx, -dy, 1f).normalized;
                    px[y * n + x] = new Color(nv.x * 0.5f + 0.5f, nv.y * 0.5f + 0.5f, nv.z * 0.5f + 0.5f, 1f);
                }
            t.SetPixels(px); t.Apply(true);
            return t;
        }
    }

    /// <summary>Hash-based value noise with optional integer period (tileable). Deterministic per seed.</summary>
    public static class Noise
    {
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        static int Wrap(int v, int p) => p <= 0 ? v : ((v % p) + p) % p;

        public static float Value(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(Wrap(x0, period), Wrap(y0, period), seed);
            float b = Hash(Wrap(x0 + 1, period), Wrap(y0, period), seed);
            float c = Hash(Wrap(x0, period), Wrap(y0 + 1, period), seed);
            float d = Hash(Wrap(x0 + 1, period), Wrap(y0 + 1, period), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Fractal sum, 0..1. period = lattice period of the first octave (0 = no tiling).</summary>
        public static float Fbm(float x, float y, int octaves, int period, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Value(x, y, period, seed + i * 17) * amp;
                norm += amp;
                x *= 2f; y *= 2f; if (period > 0) period *= 2;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        /// <summary>Ridged noise (sharp crests) for mountain lines.</summary>
        public static float Ridged(float x, float y, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                float v = 1f - Mathf.Abs(Value(x, y, 0, seed + i * 31) * 2f - 1f);
                sum += v * v * amp; norm += amp;
                x *= 2f; y *= 2f; amp *= 0.5f;
            }
            return sum / norm;
        }
    }
}
