using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>All 2D UI: main menu, HUD, radar, system map, pause/settings/controls. Uses IMGUI so it works in any project with zero setup.</summary>
public class Hud : MonoBehaviour
{
    public enum Scr { None, Main, Pause, Settings, Controls }
    public Scr screen = Scr.Main;
    public bool showMap, showHelp = true;
    GameManager gm; GUIStyle st, btn; Texture2D white, btnN, btnH;
    float sc = 1f; readonly Color cy = new Color(0.25f, 0.85f, 1f), warn = new Color(1f, 0.6f, 0.15f), red = new Color(1f, 0.25f, 0.2f), green = new Color(0.35f, 1f, 0.55f);
    float helpTimer = 14f;

    public void Init(GameManager g) { gm = g; }

    void MakeStyles()
    {
        if (st != null) return;
        white = Texture2D.whiteTexture;
        btnN = Solid(new Color(0.05f, 0.12f, 0.2f, 0.85f)); btnH = Solid(new Color(0.1f, 0.35f, 0.5f, 0.95f));
        st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, richText = true, wordWrap = false };
        btn = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        btn.normal.background = btnN; btn.hover.background = btnH; btn.active.background = btnH; btn.normal.textColor = Color.white; btn.hover.textColor = cy; btn.active.textColor = cy;
    }
    static Texture2D Solid(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

    // ------------------------------------------------------------ primitives
    Rect R(float x, float y, float w, float h) { return new Rect(x * sc, y * sc, w * sc, h * sc); }
    void Box(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, white); GUI.color = Color.white; }
    void Text(Rect r, string s, int size, Color c, TextAnchor a = TextAnchor.UpperLeft, bool bold = false)
    {
        st.fontSize = Mathf.RoundToInt(size * sc); st.alignment = a; st.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        st.normal.textColor = new Color(0, 0, 0, 0.7f); GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), s, st);
        st.normal.textColor = c; GUI.Label(r, s, st);
    }
    void Line(Vector2 a, Vector2 b, Color c, float w = 1.5f)
    {
        Vector2 d = b - a; float len = d.magnitude; if (len < 0.5f) return;
        var m = GUI.matrix; GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a);
        GUI.color = c; GUI.DrawTexture(new Rect(a.x, a.y - w * 0.5f, len, w), white); GUI.matrix = m; GUI.color = Color.white;
    }
    void Circle(Vector2 c, float r, Color col, float w = 1.5f, int seg = 48)
    {
        Vector2 prev = c + new Vector2(r, 0);
        for (int i = 1; i <= seg; i++) { float a = i * Mathf.PI * 2 / seg; Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; Line(prev, p, col, w); prev = p; }
    }
    void Disc(Vector2 c, float r, Color col) { Box(new Rect(c.x - r, c.y - r, r * 2, r * 2), col); }
    void Bar(float x, float y, float w, float frac, Color c, string label)
    {
        Text(R(x, y - 15, w, 14), label, 11, new Color(0.7f, 0.85f, 0.95f));
        Box(R(x, y, w, 7), new Color(1, 1, 1, 0.12f)); Box(R(x, y, w * Mathf.Clamp01(frac), 7), c);
    }
    bool Button(float x, float y, float w, float h, string label, int size = 18) { btn.fontSize = Mathf.RoundToInt(size * sc); return GUI.Button(R(x, y, w, h), label, btn); }
    Vector2 Screen2(Vector3 world, out bool front)
    {
        Vector3 s = gm.cam.WorldToScreenPoint(world); front = s.z > 0;
        return new Vector2(s.x, Screen.height - s.y);
    }
    static string Km(double km) { double a = Math.Abs(km); if (a >= 1e6) return (km / 1e6).ToString("F2") + " Mkm"; if (a >= 1000) return (km / 1000).ToString("F1") + " Mm"; return km.ToString("F0") + " km"; }

    // ------------------------------------------------------------ main entry
    void OnGUI()
    {
        if (gm == null) return;
        MakeStyles(); sc = Screen.height / 900f;
        if (gm.mode == GameMode.Menu) { DrawMenu(); DrawFade(); return; }
        if (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout)
        {
            if (Event.current.type == EventType.Repaint)
            {
                DrawOverlays();
                if (gm.mode == GameMode.Space) DrawSpaceHud();
                else if (gm.mode == GameMode.Surface || gm.mode == GameMode.Interior) DrawSurfaceHud();
                else if (gm.mode == GameMode.Dead) DrawDead();
                DrawMessages();
                if (showMap && gm.mode == GameMode.Space) DrawMap();
                if (showHelp && helpTimer > 0 && !gm.paused) DrawQuickHelp();
            }
        }
        if (gm.paused) DrawPause();
        DrawFade();
    }

    void Update() { if (gm != null && gm.mode != GameMode.Menu && helpTimer > 0 && !gm.paused) helpTimer -= Time.deltaTime; if (Inp.Down(K.F1) || Inp.Down(K.H)) { helpTimer = helpTimer > 0 ? 0 : 20f; } }

    void DrawFade() { if (gm.fade > 0.001f) Box(new Rect(0, 0, Screen.width, Screen.height), new Color(gm.fadeColor.r, gm.fadeColor.g, gm.fadeColor.b, gm.fade)); }

    void DrawOverlays()
    {
        var s = gm.ship;
        if (s.damageFlash > 0.01f) Box(new Rect(0, 0, Screen.width, Screen.height), new Color(1, 0.05f, 0.02f, 0.18f * s.damageFlash));
        if (s.heat > 0.05f && gm.mode == GameMode.Space) Box(new Rect(0, 0, Screen.width, Screen.height), new Color(1f, 0.4f, 0.05f, 0.14f * s.heat));
    }

    // ------------------------------------------------------------ MAIN MENU
    void DrawMenu()
    {
        Box(new Rect(0, 0, Screen.width * 0.42f, Screen.height), new Color(0, 0.02f, 0.05f, 0.62f));
        Box(R(60, 150, 4, 118), cy);
        Text(R(80, 140, 700, 40), "SPACE EXPLORER  //  NASA SPACE APPS 2026", 15, cy, TextAnchor.UpperLeft, true);
        Text(R(80, 165, 700, 90), "THE LAST HORIZON", 62, Color.white, TextAnchor.UpperLeft, true);
        Text(R(82, 240, 600, 30), "Real orbital mechanics. Real NASA imagery. One ship.", 17, new Color(0.7f, 0.85f, 0.95f));
        if (screen == Scr.Main)
        {
            if (Button(80, 330, 340, 54, "LAUNCH  -  FREE FLIGHT")) { gm.NewGame(); }
            if (Button(80, 396, 340, 48, "CONTROLS")) screen = Scr.Controls;
            if (Button(80, 456, 340, 48, "SETTINGS")) screen = Scr.Settings;
            if (Button(80, 516, 340, 48, "QUIT")) { Application.Quit(); }
        }
        else if (screen == Scr.Settings) DrawSettings(true);
        else if (screen == Scr.Controls) DrawControls(true);
        Text(R(80, 840, 900, 24), "Tip: after launch press H for the quick help card, M for the system map.", 13, new Color(0.6f, 0.75f, 0.85f));
    }

    void DrawSettings(bool menu)
    {
        float x = 80, y = 320;
        Text(R(x, y - 40, 400, 30), "SETTINGS", 22, cy, TextAnchor.UpperLeft, true);
        Text(R(x, y, 300, 24), "Mouse sensitivity  " + gm.ship.sensitivity.ToString("F2"), 15, Color.white);
        gm.ship.sensitivity = GUI.HorizontalSlider(R(x, y + 28, 340, 20), gm.ship.sensitivity, 0.03f, 0.4f);
        gm.ship.invertY = GUI.Toggle(R(x, y + 56, 300, 24), gm.ship.invertY, "  Invert Y (flight-stick style)");
        Text(R(x, y + 96, 300, 24), "Master volume", 15, Color.white);
        AudioListener.volume = GUI.HorizontalSlider(R(x, y + 124, 340, 20), AudioListener.volume, 0f, 1f);
        Text(R(x, y + 160, 300, 24), "Graphics quality", 15, Color.white);
        for (int i = 0; i < 3; i++) if (Button(x + i * 115, y + 190, 105, 38, new[] { "LOW", "MEDIUM", "HIGH" }[i], 14)) { gm.SetQuality(i); }
        Text(R(x, y + 240, 340, 24), "Planet day length on surface: " + (SC.DayLen / gm.surfaceTimeFactor / 60f).ToString("F0") + " min", 15, Color.white);
        gm.surfaceTimeFactor = GUI.HorizontalSlider(R(x, y + 268, 340, 20), gm.surfaceTimeFactor, 0.005f, 0.4f);
        if (Button(x, y + 310, 160, 42, "BACK")) screen = menu ? Scr.Main : Scr.Pause;
    }

    static readonly string[,] Keys = {
        {"MOUSE", "Pitch / yaw (cursor is captured)"}, {"W / S", "Thrust forward / reverse"}, {"A / D", "Strafe left / right"}, {"SPACE / CTRL", "Thrust up / down"},
        {"Q / E", "Roll"}, {"SHIFT", "Boost (ship) / sprint (astronaut)"}, {"X", "Full stop (flight assist)"}, {"Z", "Toggle flight assist  <->  Newtonian physics"},
        {"C", "Supercruise (far from planets) - W/S set cruise speed"}, {"V", "Camera: chase / cockpit / far  (astronaut: 1st/3rd person)"}, {"RIGHT MOUSE", "Free look"},
        {", and .", "Time warp  - / +"}, {"M", "System map"}, {"G", "Landing gear"}, {"L", "Ship lights"}, {"E", "Interact: exit / board ship, enter Mission Control"},
        {"F", "Astronaut flashlight"}, {"SPACE (air)", "Astronaut jetpack"}, {"ESC", "Pause"}, {"F1", "Toggle help card"}
    };

    void DrawControls(bool menu)
    {
        float x = menu ? 80 : 300, y = menu ? 290 : 160;
        Box(R(x - 20, y - 20, 760, 40 + Keys.GetLength(0) * 25 + 70), new Color(0, 0.03f, 0.07f, menu ? 0.2f : 0.92f));
        Text(R(x, y, 300, 30), "CONTROLS", 22, cy, TextAnchor.UpperLeft, true);
        for (int i = 0; i < Keys.GetLength(0); i++) { Text(R(x, y + 38 + i * 25, 190, 24), Keys[i, 0], 14, cy, TextAnchor.UpperLeft, true); Text(R(x + 200, y + 38 + i * 25, 560, 24), Keys[i, 1], 14, Color.white); }
        if (Button(x, y + 46 + Keys.GetLength(0) * 25, 160, 38, "BACK", 14)) screen = menu ? Scr.Main : Scr.Pause;
    }

    void DrawQuickHelp()
    {
        float a = Mathf.Clamp01(helpTimer / 3f);
        Box(R(24, 700, 360, 176), new Color(0, 0.03f, 0.07f, 0.6f * a));
        string[] l = gm.mode == GameMode.Space ?
            new[] { "W/S/A/D  thrust    SPACE/CTRL  up/down", "MOUSE  steer     Q/E  roll     SHIFT  boost", "Z  assist/newtonian    C  supercruise", "M  map    , .  time warp    V  camera", "Fly down to a planet to land.   H  help" } :
            new[] { "WASD  move    SHIFT  sprint    SPACE  jump / jetpack", "E  board ship / enter doors    F  flashlight", "V  first / third person", "Walk to the glowing arch to enter Mission Control", "Ship: SPACE/CTRL  up/down    G  gear" };
        for (int i = 0; i < l.Length; i++) Text(R(36, 712 + i * 28, 340, 24), l[i], 13, new Color(0.85f, 0.95f, 1f, a));
    }

    // ------------------------------------------------------------ SPACE HUD
    void DrawSpaceHud()
    {
        var s = gm.ship; var ss = gm.ss;
        double alt; Body nb = ss.Nearest(s.pos, out alt);
        Body rf = ss.ReferenceBody(s.pos);
        V3d rel = s.pos - rf.pos, rv = s.vel - rf.vel;
        double speed = rv.mag, radial = V3d.Dot(rel.norm, rv);
        string mode = s.supercruise ? "<color=#ffd24a>SUPERCRUISE</color>" : s.assist ? "<color=#5dff9a>FLIGHT ASSIST</color>" : "<color=#ffa030>NEWTONIAN</color>";

        Box(R(20, 20, 330, 174), new Color(0, 0.04f, 0.09f, 0.55f)); Box(R(20, 20, 3, 174), cy);
        Text(R(34, 26, 310, 22), "AURORA X-1   " + mode, 14, Color.white, TextAnchor.UpperLeft, true);
        Text(R(34, 52, 310, 22), "SPEED     <b>" + SC.KmS(speed).ToString("F2") + "</b> km/s  <size=11>vs " + rf.name + "</size>", 14, cy);
        Text(R(34, 76, 310, 22), "NEAREST   <b>" + nb.name + "</b>   ALT <b>" + Km(SC.Km(alt)) + "</b>", 14, cy);
        Text(R(34, 100, 310, 22), "RADIAL    <b>" + SC.KmS(radial).ToString("F2") + "</b> km/s " + (radial < 0 ? "(closing)" : "(receding)"), 14, cy);
        double pe, ap, per;
        string orb = "ORBIT     --";
        if (!s.assist && !s.supercruise && rf != ss.sun && Orbit(rel, rv, rf.gm, rf.radius, out pe, out ap, out per))
            orb = "ORBIT     Pe <b>" + Km(SC.Km(pe)) + "</b>  Ap <b>" + Km(SC.Km(ap)) + "</b>";
        else if (!s.assist && !s.supercruise) orb = "ORBIT     <color=#ffa030>escape trajectory</color>";
        Text(R(34, 124, 310, 22), orb, 14, cy);
        Text(R(34, 148, 310, 22), "CRUISE    " + (s.supercruise ? (SC.KmS(s.cruiseSpeed) / 1000.0).ToString("F1") + " Mm/s" : "offline") + "     LIGHTS " + (s.lightsOn ? "ON" : "OFF"), 13, new Color(0.7f, 0.85f, 0.95f));
        Text(R(34, 170, 310, 20), "Z assist   C supercruise   X stop", 11, new Color(0.6f, 0.75f, 0.85f));

        // date + warp
        Box(R(1250, 20, 330, 62), new Color(0, 0.04f, 0.09f, 0.55f));
        Text(R(1262, 24, 310, 24), ss.Date.ToString("yyyy-MM-dd  HH:mm") + " UTC", 16, Color.white, TextAnchor.UpperLeft, true);
        Text(R(1262, 50, 310, 24), "TIME WARP  <b>x" + ss.Warp + "</b>   <size=11>( , slower  . faster )</size>", 13, ss.warpIndex > 0 ? warn : cy);

        // bars
        float bx = 800 - 270;
        Bar(bx, 846, 150, s.hull / 100f, s.hull > 35 ? green : red, "HULL");
        Bar(bx + 195, 846, 150, s.shield / 100f, cy, "SHIELD");
        Bar(bx + 390, 846, 150, s.energy / 100f, warn, "BOOST ENERGY");

        // crosshair
        Vector2 c = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Circle(c, 14 * sc, new Color(1, 1, 1, 0.55f), 1.5f, 24); Disc(c, 1.5f * sc, new Color(1, 1, 1, 0.9f));
        // prograde / retrograde markers
        if (speed > 0.5)
        {
            bool fr; Vector2 pg = Screen2(gm.cam.transform.position + (rv.norm.f) * 100f, out fr);
            if (fr) { Circle(pg, 11 * sc, green, 2f, 20); Line(pg + Vector2.up * 11 * sc, pg + Vector2.up * 20 * sc, green); Line(pg + Vector2.left * 11 * sc, pg + Vector2.left * 20 * sc, green); Line(pg + Vector2.right * 11 * sc, pg + Vector2.right * 20 * sc, green); }
            Vector2 rg = Screen2(gm.cam.transform.position - (rv.norm.f) * 100f, out fr);
            if (fr) { Circle(rg, 11 * sc, warn, 2f, 20); Line(rg + new Vector2(-8, -8) * sc, rg + new Vector2(8, 8) * sc, warn); Line(rg + new Vector2(-8, 8) * sc, rg + new Vector2(8, -8) * sc, warn); }
        }
        // body markers
        foreach (var b in ss.all)
        {
            bool fr; Vector2 p = Screen2(b.root.transform.position, out fr); if (!fr) continue;
            double d = (b.pos - s.pos).mag;
            if (p.x < 0 || p.x > Screen.width || p.y < 0 || p.y > Screen.height) continue;
            Color col = b == ss.sun ? new Color(1, 0.8f, 0.3f) : b == ss.earth ? new Color(0.4f, 0.7f, 1f) : new Color(0.85f, 0.85f, 0.85f);
            Circle(p, 7 * sc, col, 1.5f, 16);
            Text(new Rect(p.x + 12 * sc, p.y - 10 * sc, 220 * sc, 40 * sc), b.name.ToUpper() + "\n<size=11>" + Km(SC.Km(d - b.radius)) + "</size>", 13, col);
        }
        // landing guidance
        if (nb != ss.sun && alt < nb.radius * 1.2 + 40)
        {
            double at = nb == ss.earth ? 3.0 : 2.0;
            string g = alt < at + 6 ? "<b>ATMOSPHERIC ENTRY IMMINENT</b>" : "<b>APPROACHING " + nb.name.ToUpper() + "</b>";
            Text(new Rect(0, Screen.height * 0.22f, Screen.width, 30 * sc), g + "\n<size=13>descend below " + Km(SC.Km(at)) + " altitude to land   -   current " + Km(SC.Km(alt)) + "   closing " + SC.KmS(-radial).ToString("F2") + " km/s</size>", 20, warn, TextAnchor.UpperCenter);
        }
        DrawRadar(s, ss);
        if (s.heat > 0.3f) Text(new Rect(0, Screen.height * 0.3f, Screen.width, 40 * sc), "<b>HEAT WARNING</b>", 26, red, TextAnchor.UpperCenter);
    }

    static bool Orbit(V3d r, V3d v, double mu, double R, out double pe, out double ap, out double period)
    {
        pe = ap = period = 0;
        double rm = r.mag, v2 = v.sqr, eps = v2 * 0.5 - mu / rm; if (eps >= 0) return false;
        double a = -mu / (2 * eps); V3d h = V3d.Cross(r, v); double e = Math.Sqrt(Math.Max(0, 1 + 2 * eps * h.sqr / (mu * mu)));
        pe = a * (1 - e) - R; ap = a * (1 + e) - R; period = 2 * Math.PI * Math.Sqrt(a * a * a / mu); return true;
    }

    void DrawRadar(ShipController s, SolarSystem ss)
    {
        Vector2 c = new Vector2(Screen.width - 130 * sc, Screen.height - 130 * sc); float r = 100 * sc;
        Disc(c, r, new Color(0, 0.05f, 0.1f, 0.55f)); Circle(c, r, cy * 0.8f, 1.5f, 48); Circle(c, r * 0.5f, cy * 0.4f, 1f, 36);
        Line(c + Vector2.left * r, c + Vector2.right * r, cy * 0.3f, 1f); Line(c + Vector2.up * r, c + Vector2.down * r, cy * 0.3f, 1f);
        Disc(c, 3 * sc, Color.white);
        foreach (var b in ss.all)
        {
            Vector3 rel = Quaternion.Inverse(s.rot) * (b.pos - s.pos).f; float d = rel.magnitude;
            float rr = Mathf.Clamp01(Mathf.Log10(1f + d / 80f) / 3.2f) * r;
            Vector2 dir = new Vector2(rel.x, rel.z); dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.zero;
            Vector2 p = c + new Vector2(dir.x, -dir.y) * rr * Mathf.Clamp01(new Vector2(rel.x, rel.z).magnitude / Mathf.Max(d, 0.001f) + 0.15f);
            Color col = b == ss.sun ? new Color(1, 0.8f, 0.3f) : b == ss.earth ? new Color(0.4f, 0.7f, 1f) : Color.white;
            Line(p, p + Vector2.down * (-rel.y / Mathf.Max(d, 1f)) * 18 * sc, col * 0.7f, 1.5f); Disc(p, (b == ss.sun ? 6 : 4) * sc, col);
        }
        Text(new Rect(c.x - r, c.y + r + 2, r * 2, 18 * sc), "NAV RADAR", 10, new Color(0.6f, 0.8f, 0.9f), TextAnchor.UpperCenter);
    }

    // ------------------------------------------------------------ SURFACE HUD
    void DrawSurfaceHud()
    {
        var sw = gm.surface; var s = gm.ship;
        Box(R(20, 20, 360, 118), new Color(0, 0.04f, 0.09f, 0.55f)); Box(R(20, 20, 3, 118), cy);
        Text(R(34, 26, 340, 22), (gm.mode == GameMode.Interior ? "MISSION CONTROL" : sw.siteName.ToUpper()), 14, Color.white, TextAnchor.UpperLeft, true);
        string tod = sw.atmosphere ? (sw.dayFactor > 0.85f ? "DAY" : sw.dayFactor > 0.15f ? "TWILIGHT" : "NIGHT") : "LUNAR " + (sw.dayFactor > 0.5f ? "DAY" : "NIGHT");
        Text(R(34, 52, 340, 22), "BODY  <b>" + sw.body.name + "</b>   g <b>" + sw.gravity.ToString("F2") + "</b> m/s2   " + tod, 14, cy);
        if (gm.mode == GameMode.Surface)
        {
            if (gm.pilotInShip)
            {
                Vector3 v = s.rb.linearVelocity;
                Text(R(34, 76, 340, 22), "SPEED <b>" + (v.magnitude * 3.6f).ToString("F0") + "</b> km/h   V-SPEED <b>" + v.y.ToString("F1") + "</b> m/s", 14, cy);
                Text(R(34, 100, 340, 22), "ALT <b>" + Mathf.Min(s.groundDist, 9999f).ToString("F0") + "</b> m   GEAR <b>" + (s.gearDown ? "DOWN" : "UP") + "</b>   " + (s.assist ? "HOVER ASSIST" : "GRAVITY"), 14, cy);
                string st2 = s.landed ? "<color=#5dff9a>LANDED  -  press E to disembark</color>" : s.transform.position.y > 1000 ? "<color=#ffd24a>Climb past 1500 m to return to orbit</color>" : "";
                if (st2 != "") Text(new Rect(0, Screen.height * 0.78f, Screen.width, 30 * sc), st2, 18, Color.white, TextAnchor.UpperCenter, true);
                Bar(Screen.width / sc / 2 - 170, 846, 150, s.hull / 100f, s.hull > 35 ? green : red, "HULL"); Bar(Screen.width / sc / 2 + 20, 846, 150, s.energy / 100f, warn, "BOOST ENERGY");
            }
            else
            {
                Text(R(34, 76, 340, 22), "JETPACK  <b>" + gm.astro.jetFuel.ToString("F0") + "</b>%    " + (gm.astro.flashlightOn ? "FLASHLIGHT ON" : ""), 14, cy);
                Bar(Screen.width / sc / 2 - 75, 846, 150, gm.astro.jetFuel / 100f, warn, "JETPACK");
            }
            Vector2 c = new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (!gm.pilotInShip) Disc(c, 2f * sc, new Color(1, 1, 1, 0.8f)); else Circle(c, 12 * sc, new Color(1, 1, 1, 0.4f), 1.5f, 24);
            // POI markers
            foreach (var p in sw.pois)
            {
                bool fr; Vector2 sp = Screen2(p.pos, out fr); if (!fr) continue;
                float d = Vector3.Distance(gm.cam.transform.position, p.pos); if (d < 12f && p.name != "MISSION CONTROL") continue;
                Disc(sp, 3 * sc, p.color); Line(sp + Vector2.up * 6 * sc, sp + Vector2.up * 16 * sc, p.color, 1.5f);
                Text(new Rect(sp.x - 100 * sc, sp.y - 38 * sc, 200 * sc, 22 * sc), p.name + "  " + d.ToString("F0") + " m", 12, p.color, TextAnchor.UpperCenter);
            }
            // compass
            float hd = gm.cam.transform.eulerAngles.y; string[] cd = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }; 
            Box(R(800 - 150, 20, 300, 24), new Color(0, 0.04f, 0.09f, 0.5f));
            Text(new Rect(Screen.width / 2f - 150 * sc, 22 * sc, 300 * sc, 24 * sc), "HDG " + hd.ToString("F0") + "°  " + cd[Mathf.RoundToInt(hd / 45f) % 8], 14, Color.white, TextAnchor.UpperCenter, true);
            // minimap (north-up not available in scene space; shows pad & POIs relative to you)
            Vector2 mc = new Vector2(Screen.width - 120 * sc, Screen.height - 120 * sc); float mr = 90 * sc;
            Disc(mc, mr, new Color(0, 0.05f, 0.1f, 0.55f)); Circle(mc, mr, cy * 0.8f, 1.5f, 48);
            Vector3 me = gm.PlayerPos; float yaw = gm.cam.transform.eulerAngles.y;
            foreach (var p in sw.pois)
            {
                Vector3 dlt = Quaternion.Euler(0, -yaw, 0) * (p.pos - me); Vector2 q = new Vector2(dlt.x, -dlt.z) / 600f; if (q.magnitude > 1) q = q.normalized;
                Disc(mc + q * mr, 4 * sc, p.color);
            }
            Disc(mc, 4 * sc, Color.white); Line(mc, mc + Vector2.down * 14 * sc, Color.white, 2f);
        }
        // interaction prompt
        if (gm.prompt != "") { Box(new Rect(Screen.width / 2f - 230 * sc, Screen.height * 0.68f, 460 * sc, 40 * sc), new Color(0, 0.05f, 0.1f, 0.75f)); Text(new Rect(Screen.width / 2f - 230 * sc, Screen.height * 0.68f + 8 * sc, 460 * sc, 30 * sc), gm.prompt, 17, Color.white, TextAnchor.UpperCenter, true); }
    }

    void DrawDead()
    {
        Box(new Rect(0, 0, Screen.width, Screen.height), new Color(0.2f, 0, 0, 0.55f));
        Text(new Rect(0, Screen.height * 0.4f, Screen.width, 80 * sc), "SHIP LOST", 60, red, TextAnchor.UpperCenter, true);
        Text(new Rect(0, Screen.height * 0.52f, Screen.width, 30 * sc), "Emergency beacon launched - respawning in Earth orbit...", 18, Color.white, TextAnchor.UpperCenter);
    }

    void DrawMessages()
    {
        float y = 760; int n = 0;
        for (int i = gm.messages.Count - 1; i >= 0 && n < 5; i--, n++) { var m = gm.messages[i]; Text(R(24, y - n * 24, 600, 22), m.text, 14, new Color(0.85f, 0.95f, 1f, Mathf.Clamp01(m.t))); }
        if (gm.ship.noticeT > 0) Text(new Rect(0, Screen.height * 0.16f, Screen.width, 40 * sc), "<b>" + gm.ship.notice + "</b>", 22, warn, TextAnchor.UpperCenter);
    }

    // ------------------------------------------------------------ MAP
    void DrawMap()
    {
        var ss = gm.ss; var s = gm.ship;
        Box(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0.01f, 0.03f, 0.9f));
        Text(new Rect(0, 24 * sc, Screen.width, 36 * sc), "SYSTEM MAP   (M to close)", 24, cy, TextAnchor.UpperCenter, true);
        // --- left: heliocentric plan
        Vector2 c1 = new Vector2(Screen.width * 0.27f, Screen.height * 0.52f); float sc1 = Screen.height * 0.38f / (float)SC.EarthOrbitR;
        Disc(c1, 10 * sc, new Color(1, 0.8f, 0.3f)); Text(new Rect(c1.x + 14 * sc, c1.y - 8 * sc, 100 * sc, 20 * sc), "SUN", 12, new Color(1, 0.8f, 0.3f));
        PlotOrbit(c1, sc1, ss.sun, ss.earth); Vector2 e1 = Plan(c1, sc1, ss.earth.pos); Disc(e1, 5 * sc, new Color(0.4f, 0.7f, 1f)); Text(new Rect(e1.x + 8 * sc, e1.y - 8 * sc, 100 * sc, 20 * sc), "EARTH", 12, new Color(0.4f, 0.7f, 1f));
        Vector2 sp1 = Plan(c1, sc1, s.pos); Disc(sp1, 4 * sc, green);
        Text(new Rect(c1.x - 150 * sc, c1.y + Screen.height * 0.42f - 10 * sc, 300 * sc, 20 * sc), "HELIOCENTRIC VIEW", 13, new Color(0.6f, 0.8f, 0.9f), TextAnchor.UpperCenter);
        // --- right: Earth-Moon
        Vector2 c2 = new Vector2(Screen.width * 0.73f, Screen.height * 0.52f); float sc2 = Screen.height * 0.36f / (float)SC.MoonOrbitR;
        Disc(c2, Mathf.Max(3, (float)SC.EarthR * sc2), new Color(0.3f, 0.55f, 1f)); Text(new Rect(c2.x + 10 * sc, c2.y + 8 * sc, 100 * sc, 20 * sc), "EARTH", 12, new Color(0.4f, 0.7f, 1f));
        PlotOrbit(c2, sc2, ss.earth, ss.moon);
        Vector2 m2 = Plan(c2, sc2, ss.moon.pos - ss.earth.pos); Disc(m2, Mathf.Max(3, (float)SC.MoonR * sc2 * 2), Color.white); Text(new Rect(m2.x + 8 * sc, m2.y - 8 * sc, 100 * sc, 20 * sc), "MOON", 12, Color.white);
        Vector2 sp2 = Plan(c2, sc2, s.pos - ss.earth.pos); Disc(sp2, 4 * sc, green); Text(new Rect(sp2.x + 8 * sc, sp2.y - 8 * sc, 100 * sc, 20 * sc), "YOU", 12, green);
        Text(new Rect(c2.x - 150 * sc, c2.y + Screen.height * 0.42f - 10 * sc, 300 * sc, 20 * sc), "EARTH - MOON  (rotating-frame-free, to scale)", 13, new Color(0.6f, 0.8f, 0.9f), TextAnchor.UpperCenter);
    }
    Vector2 Plan(Vector2 c, float k, V3d p) { return c + new Vector2((float)p.x * k, -(float)p.z * k); }
    void PlotOrbit(Vector2 c, float k, Body parent, Body b)
    {
        Vector2 prev = Vector2.zero; V3d pp = parent.pos;
        for (int i = 0; i <= 90; i++)
        {
            double t = b.periodDays * i / 90.0; V3d p = b.PosAt(ss_time(b) + t) - parent.PosAt(ss_time(b) + t);
            Vector2 q = Plan(c, k, p); if (i > 0) Line(prev, q, new Color(1, 1, 1, 0.22f), 1f); prev = q;
        }
    }
    double ss_time(Body b) { return gm.ss.simDays; }

    // ------------------------------------------------------------ PAUSE
    void DrawPause()
    {
        Box(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0.01f, 0.03f, 0.6f));
        if (screen == Scr.Settings) { DrawSettings(false); return; }
        if (screen == Scr.Controls) { DrawControls(false); return; }
        Text(R(0, 190, 1600, 60), "PAUSED", 48, Color.white, TextAnchor.UpperCenter, true);
        float cx = 800 - 170;
        if (Button(cx, 300, 340, 52, "RESUME")) gm.Resume();
        if (Button(cx, 366, 340, 48, "CONTROLS")) screen = Scr.Controls;
        if (Button(cx, 426, 340, 48, "SETTINGS")) screen = Scr.Settings;
        if (Button(cx, 486, 340, 48, "MAIN MENU")) gm.ReturnToMenu();
        if (Button(cx, 546, 340, 48, "QUIT")) Application.Quit();
    }
}
