using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// The art bible as code. Every colour in the game (materials, lights, fog, UI, text) reads from here.
    /// Style: grounded NASA-facility sci-fi. Graphite test range, worn white ceramic hardware,
    /// hazard-amber markings, and one cyan "signal" colour for everything the player reads or touches.
    /// Mood: dusk on the range. The last orange band on the horizon, cool sky fill, warm work lamps.
    /// </summary>
    public static class Palette
    {
        // ---- world
        public static readonly Color Floor = Hex("#2B3038");      // range concrete
        public static readonly Color Wall = Hex("#1E232B");       // hull plate, hangar skin
        public static readonly Color Ceramic = Hex("#D7DBE0");    // hardware primary: worn white ceramic
        public static readonly Color Steel = Hex("#5B6675");      // hardware secondary: slate steel
        public static readonly Color SteelDark = Hex("#2E343D");
        public static readonly Color Rust = Hex("#C4572E");       // props: flight-recorder orange
        public static readonly Color Foil = Hex("#C9A24A");       // props: insulation foil
        public static readonly Color Scorch = Hex("#1A1614");     // burnt fragment
        public static readonly Color Sand = Hex("#3A332B");       // desert floor at dusk
        public static readonly Color Shadow = Hex("#10141B");     // void blue, never pure black

        // ---- light story
        public static readonly Color KeyCool = Hex("#BFD2E6");
        public static readonly Color LampWarm = Hex("#FFD9A8");
        public static readonly Color Horizon = Hex("#FF8A4C");
        public static readonly Color SkyTop = Hex("#16233A");
        public static readonly Color Fog = Hex("#27324A");

        // ---- signal colours (shared by UI and emissive hardware)
        public static readonly Color Accent = Hex("#3FD0FF");
        public static readonly Color Good = Hex("#4DE38A");
        public static readonly Color Warn = Hex("#FFB938");
        public static readonly Color Bad = Hex("#FF5A4D");

        // ---- UI
        public static readonly Color PanelBg = new Color(0.035f, 0.055f, 0.085f, 0.93f);
        public static readonly Color PanelBgSolid = Hex("#0A1018");
        public static readonly Color PanelEdge = new Color(0.25f, 0.82f, 1f, 0.32f);
        public static readonly Color Inset = new Color(1f, 1f, 1f, 0.055f);
        public static readonly Color Text = Hex("#E6F1FA");
        public static readonly Color TextDim = Hex("#8FA6BA");
        public static readonly Color Ink = Hex("#06090E");
        public static readonly Color ButtonBg = Hex("#12506E");
        public static readonly Color ButtonGhost = new Color(1f, 1f, 1f, 0.07f);

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        public static Color A(Color c, float alpha) { c.a = alpha; return c; }

        /// <summary>Gauge colour for a success probability 0..1 (red, amber, green at the 70% preflight line).</summary>
        public static Color Risk(float p) => p >= 0.70f ? Good : p >= 0.35f ? Warn : Bad;
    }
}
