using System.Collections.Generic;
using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Procedural UI skin: rounded 9-slice panels, outlines, soft shadows, rings and gradients are
    /// drawn into small textures at runtime, so the whole interface is styled without any image files.
    /// Fonts are OFL-licensed (see Art/CREDITS.md) and fall back to Unity's built-in font.
    /// </summary>
    public static class UISkin
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static Font _body, _bold, _mono;

        public static Font Body => _body != null ? _body : (_body = LoadFont("Rajdhani-Medium"));
        public static Font Bold => _bold != null ? _bold : (_bold = LoadFont("Rajdhani-Bold"));
        public static Font Mono => _mono != null ? _mono : (_mono = LoadFont("ShareTechMono-Regular"));

        static Font LoadFont(string name)
        {
            var f = Resources.Load<Font>("LastSignal/Fonts/" + name);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>Filled rounded rectangle, 9-sliced.</summary>
        public static Sprite Rounded(int radius) => Get("r" + radius, () => MakeRounded(radius, 0f, 0f));

        /// <summary>Rounded outline only, 9-sliced.</summary>
        public static Sprite Outline(int radius, float thickness = 1.5f) =>
            Get("o" + radius + "_" + thickness, () => MakeRounded(radius, thickness, 0f));

        /// <summary>Soft drop shadow or glow for a rounded box, 9-sliced. Extends <paramref name="spread"/> px outside.</summary>
        public static Sprite Shadow(int radius, int spread) =>
            Get("s" + radius + "_" + spread, () => MakeRounded(radius, 0f, spread));

        public static Sprite Circle() => Get("circle", () => MakeRing(128, 0f));

        /// <summary>Annulus for radial gauges. Thickness is a fraction of the radius.</summary>
        public static Sprite Ring(float thickness = 0.16f) => Get("ring" + thickness, () => MakeRing(256, thickness));

        /// <summary>White at the top fading to transparent at the bottom.</summary>
        public static Sprite FadeDown() => Get("fadedown", () => MakeGradient(false));

        /// <summary>White on the left fading to transparent on the right.</summary>
        public static Sprite FadeRight() => Get("faderight", () => MakeGradient(true));

        /// <summary>Diagonal hazard stripes, tileable.</summary>
        public static Sprite Hazard() => Get("hazard", MakeHazard);

        static Sprite Get(string key, System.Func<Sprite> make)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            Cache[key] = s;
            return s;
        }

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            return t;
        }

        // Signed distance to a rounded box centred in a size x size square.
        static float RoundedSdf(float x, float y, float half, float radius)
        {
            float qx = Mathf.Abs(x) - (half - radius);
            float qy = Mathf.Abs(y) - (half - radius);
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static Sprite MakeRounded(int radius, float outline, float spread)
        {
            int pad = Mathf.CeilToInt(spread);
            int border = radius + pad + 2;
            int size = border * 2 + 4;
            var tex = NewTex(size, size);
            var px = new Color32[size * size];
            float half = size * 0.5f - pad;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedSdf(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f, half, radius);
                    float a;
                    if (spread > 0f) a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / spread), 2f) * (d > 0f ? 1f : 1f);
                    else if (outline > 0f) a = Mathf.Clamp01(outline * 0.5f + 0.5f - Mathf.Abs(d + outline * 0.5f));
                    else a = Mathf.Clamp01(0.5f - d);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite MakeRing(int size, float thickness)
        {
            var tex = NewTex(size, size);
            var px = new Color32[size * size];
            float r = size * 0.5f - 1f;
            float inner = thickness > 0f ? r * (1f - thickness) : -1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (inner > 0f) a *= Mathf.Clamp01(d - inner + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeGradient(bool horizontal)
        {
            const int n = 64;
            var tex = NewTex(horizontal ? n : 1, horizontal ? 1 : n);
            var px = new Color32[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (n - 1f);
                float a = horizontal ? 1f - t : t; // texture row 0 is the bottom
                px[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeHazard()
        {
            const int n = 32;
            var tex = NewTex(n, n);
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool on = ((x + y) % n) < n / 2;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(on ? 255 : 40));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
