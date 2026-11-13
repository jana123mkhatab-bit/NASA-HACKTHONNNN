using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Bootstraps the whole game at runtime (no scene setup needed) and runs the state machine:
/// Menu -> Space <-> Surface (-> Interior). Owns time, transitions, camera and audio.</summary>
public class GameManager : MonoBehaviour
{
    public class Msg { public string text; public float t; }

    public static GameManager I;
    public Camera cam; public CameraRig rig; public SolarSystem ss; public ShipController ship; public Astronaut astro;
    public SurfaceWorld surface; public InteriorHall hall; public SpaceDust dust; public Hud hud;
    public GameMode mode = GameMode.Menu;
    public bool paused, pilotInShip = true, busy;
    public float fade = 1f; public Color fadeColor = Color.black;
    public string prompt = "";
    public List<Msg> messages = new List<Msg>();
    public float surfaceTimeFactor = 0.05f;      // simulated time speed on the surface (day = 40 min at 0.05)

    AudioSource engineSrc, windSrc, sfx; AudioClip blip, thud;
    V3d menuOffset; float menuT, landCooldown, lastHull = 100f, deadTimer;
    string setupWarning = "";
    Color savedAmbient; AmbientMode savedAmbientMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindObjectOfType<GameManager>() != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    public Vector3 PlayerPos { get { return pilotInShip ? ship.transform.position : astro.transform.position; } }
    public void Say(string s) { messages.Add(new Msg { text = s, t = 5f }); if (messages.Count > 8) messages.RemoveAt(0); }
    public void SetQuality(int q) { PostFX.Quality(q); }

    // ================================================================================== SETUP
    void Awake()
    {
        I = this; DontDestroyOnLoad(gameObject);
        Application.runInBackground = true; Time.timeScale = 1f; QualitySettings.vSyncCount = 1;
        if (GraphicsSettings.currentRenderPipeline == null)
            setupWarning = "URP is not active. Use the menu  Tools > Last Horizon > Setup Project  (then press Play again).";

        cam = Camera.main;
        if (cam == null) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; cam = cg.AddComponent<Camera>(); }
        foreach (var other in FindObjectsOfType<Camera>()) if (other != cam) other.enabled = false;
        if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        foreach (var l in FindObjectsOfType<AudioListener>()) if (l.gameObject != cam.gameObject) l.enabled = false;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.farClipPlane = 220000f;
        PostFX.Setup(cam); PostFX.Quality(2);
        foreach (var dl in FindObjectsOfType<Light>()) if (dl.type == LightType.Directional) dl.gameObject.SetActive(false);

        ss = new GameObject("SolarSystem").AddComponent<SolarSystem>(); ss.Init();
        ship = new GameObject("Aurora").AddComponent<ShipController>();
        astro = new GameObject("Astronaut").AddComponent<Astronaut>(); astro.gameObject.SetActive(false);
        surface = new GameObject("SurfaceManager").AddComponent<SurfaceWorld>();
        hall = new GameObject("HallManager").AddComponent<InteriorHall>(); hall.Build();
        dust = new GameObject("SpaceDust").AddComponent<SpaceDust>(); dust.Init();
        rig = gameObject.AddComponent<CameraRig>(); rig.Init(cam);
        hud = gameObject.AddComponent<Hud>(); hud.Init(this);

        engineSrc = gameObject.AddComponent<AudioSource>(); engineSrc.clip = ProcAudio.Engine(); engineSrc.loop = true; engineSrc.volume = 0; engineSrc.spatialBlend = 0; engineSrc.Play();
        windSrc = gameObject.AddComponent<AudioSource>(); windSrc.clip = ProcAudio.Wind(); windSrc.loop = true; windSrc.volume = 0; windSrc.spatialBlend = 0; windSrc.Play();
        sfx = gameObject.AddComponent<AudioSource>(); sfx.spatialBlend = 0;
        blip = ProcAudio.Blip(900f, 1500f, 0.12f, 0.25f); thud = ProcAudio.Thud();

        ss.Tick(0, 1);
        ResetToOrbit();
        menuOffset = ship.pos - ss.earth.pos;
        RestoreSpaceRender();
        ship.controlsEnabled = false;
        mode = GameMode.Menu; hud.screen = Hud.Scr.Main;
        StartCoroutine(FadeTo(0f, 1.5f));
    }

    /// <summary>Hover over the lit side of Earth in flight-assist mode.</summary>
    void ResetToOrbit()
    {
        V3d sd = (ss.sun.pos - ss.earth.pos).norm;
        Vector3 d = Quaternion.AngleAxis(38f, Vector3.up) * sd.f;
        double R = ss.earth.radius + 40.0;
        V3d p = ss.earth.pos + new V3d(d) * R;
        Vector3 t = Vector3.Cross(Vector3.up, d).normalized;
        ship.ResetSpace(p, ss.earth.vel, Quaternion.LookRotation(t, d));
        ship.assist = true; ship.visual.gameObject.SetActive(true);
        lastHull = 100f; landCooldown = 2f; rig.Reset(); ss.warpIndex = 0;
    }

    void RestoreSpaceRender()
    {
        RenderSettings.skybox = null; RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.012f, 0.014f, 0.022f);
        var L = ss.sunLight; L.enabled = true; L.intensity = 2.2f; L.color = new Color(1f, 0.96f, 0.9f);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        ss.SetStarDim(1f);
    }

    // ================================================================================== FLOW HELPERS
    public IEnumerator FadeTo(float target, float dur)
    {
        float start = fade, t = 0;
        while (t < dur) { t += Time.unscaledDeltaTime; fade = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t / dur)); yield return null; }
        fade = target;
    }

    void Transition(Action mid, float outT = 0.7f, float inT = 1.0f)
    {
        if (busy) return;
        StartCoroutine(TransitionCo(mid, outT, inT));
    }
    IEnumerator TransitionCo(Action mid, float outT, float inT)
    {
        busy = true; fadeColor = Color.black;
        yield return FadeTo(1f, outT);
        mid();
        yield return null; yield return null;
        yield return FadeTo(0f, inT);
        busy = false;
    }

    void LockCursor(bool l)
    {
        var want = l ? CursorLockMode.Locked : CursorLockMode.None;
        if (Cursor.lockState != want) Cursor.lockState = want;
        if (Cursor.visible == l) Cursor.visible = !l;
    }

    public void NewGame()
    {
        Transition(() =>
        {
            ResetToOrbit(); pilotInShip = true; mode = GameMode.Space; hud.screen = Hud.Scr.None; ship.controlsEnabled = true;
            Say("Aurora X-1 online. Flight assist ON. Earth is below you - fly down to land, or head for the Moon.");
        }, 0.6f, 1.4f);
    }

    public void ReturnToMenu()
    {
        Resume();
        Transition(() =>
        {
            if (surface.body != null) surface.Teardown();
            hall.SetActive(false); astro.gameObject.SetActive(false); pilotInShip = true;
            ss.SetVisible(true); ss.Dome.gameObject.SetActive(true); dust.SetActive(true); RestoreSpaceRender();
            ResetToOrbit(); menuOffset = ship.pos - ss.earth.pos; ship.controlsEnabled = false;
            mode = GameMode.Menu; hud.screen = Hud.Scr.Main;
        });
    }

    public void Pause() { if (paused) return; paused = true; Time.timeScale = 0f; hud.screen = Hud.Scr.Pause; }
    public void Resume() { if (!paused) return; paused = false; Time.timeScale = 1f; hud.screen = Hud.Scr.None; }

    // ================================================================================== UPDATE
    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = messages.Count - 1; i >= 0; i--) { messages[i].t -= Time.unscaledDeltaTime; if (messages[i].t <= 0) messages.RemoveAt(i); }
        LockCursor(!paused && mode != GameMode.Menu && mode != GameMode.Dead);
        prompt = "";

        if (mode == GameMode.Menu) { MenuTick(dt); return; }
        if (Inp.Down(K.Esc))
        {
            if (!paused) Pause();
            else if (hud.screen == Hud.Scr.Pause) Resume();
            else hud.screen = Hud.Scr.Pause;
        }
        if (paused) return;

        switch (mode)
        {
            case GameMode.Space: SpaceTick(dt); break;
            case GameMode.Surface: SurfaceTick(dt); break;
            case GameMode.Interior: InteriorTick(dt); break;
            case GameMode.Dead: DeadTick(dt); break;
        }
        AudioTick(dt);
    }

    // ---------------------------------------------------------------- MENU
    void MenuTick(float dt)
    {
        ss.Tick(dt, 1.0);
        ship.pos = ss.earth.pos + menuOffset; ship.vel = ss.earth.vel;
        ss.Place(ship.pos); ship.transform.position = Vector3.zero; menuT += dt;
        engineSrc.volume = 0; windSrc.volume = 0;
    }

    // ---------------------------------------------------------------- SPACE
    void SpaceTick(float dt)
    {
        ship.controlsEnabled = !busy && ship.alive;
        bool inp = ship.controlsEnabled;
        if (inp)
        {
            if (Inp.Down(K.Z) && !ship.supercruise) { ship.assist = !ship.assist; ship.Notice(ship.assist ? "FLIGHT ASSIST ON" : "NEWTONIAN MODE - gravity affects you", 2f); sfx.PlayOneShot(blip); }
            if (Inp.Down(K.C))
            {
                if (ship.supercruise) { ship.supercruise = false; ship.Notice("SUPERCRUISE OFF", 2f); }
                else
                {
                    double a; Body n = ss.Nearest(ship.pos, out a);
                    if (a < n.radius * 0.6 + 90.0) ship.Notice("MASS LOCKED - move away from " + n.name + " to engage supercruise", 3f);
                    else { ship.supercruise = true; ship.assist = true; ship.cruiseSpeed = 2500; ship.Notice("SUPERCRUISE ENGAGED  (W/S change speed)", 3f); }
                }
                sfx.PlayOneShot(blip);
            }
            if (Inp.Down(K.V)) rig.CycleView();
            if (Inp.Down(K.M)) { hud.showMap = !hud.showMap; sfx.PlayOneShot(blip); }
            if (Inp.Down(K.Period)) { ss.ChangeWarp(1); sfx.PlayOneShot(blip); }
            if (Inp.Down(K.Comma)) { ss.ChangeWarp(-1); sfx.PlayOneShot(blip); }
        }

        // ---- safety: drop out of warp near any body / when hot
        double alt0; Body nb0 = ss.Nearest(ship.pos, out alt0);
        if (ss.warpIndex > 0 && (alt0 < nb0.radius * 0.8 + 60.0 || ship.heat > 0.05f || ship.supercruise || !ship.alive))
        {
            ss.warpIndex = 0; Say("Time warp dropped: " + (ship.supercruise ? "supercruise active" : "proximity to " + nb0.name));
        }

        // ---- physics + orbital mechanics
        double warp = ss.Warp;
        if (warp <= 1.0) { ss.Tick(dt, 1.0); ship.SpaceStep(dt, ss, V3d.zero); }
        else
        {
            ship.controlsEnabled = false;
            double total = dt * warp; int n = Mathf.Clamp(Mathf.CeilToInt((float)(total / 0.25)), 1, 400); double c = total / n;
            for (int i = 0; i < n; i++) { ss.Tick(c, 1.0); ship.SpaceStep((float)c, ss, V3d.zero); }
        }

        // ---- collisions / landing
        double alt; Body nb = ss.Nearest(ship.pos, out alt);
        landCooldown -= dt;
        if (ship.alive && (ship.pos - ss.sun.pos).mag < ss.sun.radius * 0.97) { ship.hull = 0; ship.alive = false; ship.Notice("INCINERATED BY THE SUN", 3f); }
        if (ship.alive && !busy && nb != ss.sun && landCooldown <= 0 && !ship.supercruise)
        {
            double enter = nb == ss.earth ? 3.0 : 2.0;
            if (alt < enter) EnterSurface(nb);
        }
        if (ship.hull < lastHull - 0.5f) sfx.PlayOneShot(thud, 0.8f);
        lastHull = ship.hull;
        if (!ship.alive && mode == GameMode.Space) Die();

        ss.Place(ship.pos); ship.transform.position = Vector3.zero;
    }

    // ---------------------------------------------------------------- SPACE -> SURFACE
    void EnterSurface(Body b)
    {
        Vector3 upLocal = (Quaternion.Inverse(b.rot) * (ship.pos - b.pos).f).normalized;
        V3d rel = ship.pos - b.pos, vrel = ship.vel - b.vel;
        double closing = -V3d.Dot(rel.norm, vrel);
        Vector3 fwdWorld = ship.rot * Vector3.forward, vWorld = vrel.f;
        ss.warpIndex = 0; hud.showMap = false; ship.supercruise = false;
        Transition(() =>
        {
            surface.Build(ss, b, upLocal);
            ss.SetVisible(false); ss.Dome.gameObject.SetActive(false); dust.SetActive(false);
            cam.clearFlags = b == ss.moon ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            RenderSettings.ambientMode = AmbientMode.Trilight; ss.sunLight.enabled = true;
            Quaternion toScene = Quaternion.Inverse(surface.qSite) * Quaternion.Inverse(b.rot);
            Vector3 f = toScene * fwdWorld; f.y = 0;
            float yaw = f.sqrMagnitude > 1e-4f ? Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg : 0f;
            Vector3 v = Vector3.ClampMagnitude(toScene * vWorld * 0.3f, 150f);
            ship.SetSurfaceMode(new Vector3(0, 420f, 0), yaw, v); ship.SetYaw(yaw); ship.assist = true; ship.landed = false;
            ship.transform.position = new Vector3(0, 420f, 0);
            mode = GameMode.Surface; pilotInShip = true; rig.Reset(); rig.view = CameraRig.View.Chase;
            Say("Entering " + b.name + " atmosphere...  " + surface.siteName);
            if (closing > 220.0 && ship.alive) { ship.Damage((float)(closing - 220.0) * 0.5f); ship.Notice("HARD ENTRY!", 2.5f); }
            ship.controlsEnabled = true;
        }, 0.9f, 1.6f);
    }

    // ---------------------------------------------------------------- SURFACE
    void SurfaceTick(float dt)
    {
        ss.Tick(dt, surfaceTimeFactor);
        surface.Tick();
        if (pilotInShip)
        {
            ship.controlsEnabled = !busy && ship.alive;
            ship.SurfaceStep(dt, surface.gravity, surface.atmosphere);
            if (ship.controlsEnabled)
            {
                if (Inp.Down(K.V)) rig.CycleView();
                if (Inp.Down(K.Z)) { ship.assist = !ship.assist; ship.Notice(ship.assist ? "HOVER ASSIST ON" : "HOVER ASSIST OFF - gravity applies", 2f); }
                if (ship.landed) { prompt = "[E]  Disembark"; if (Inp.Down(K.E)) Disembark(); }
                if (ship.transform.position.y > 1500f && !busy) ExitToSpace();
            }
            if (ship.hull < lastHull - 0.5f) sfx.PlayOneShot(thud, 0.8f);
            lastHull = ship.hull;
            if (!ship.alive) Die();
        }
        else
        {
            astro.Tick(dt, !busy, cam.transform);
            Vector3 ap = astro.transform.position, sp = ship.transform.position;
            if (Vector3.Distance(ap, sp) < 14f) { prompt = "[E]  Board Aurora"; if (Inp.Down(K.E) && !busy) Board(); }
            else if (surface.kind == SurfaceKind.Spaceport && surface.doorPos != Vector3.zero && Vector2.Distance(new Vector2(ap.x, ap.z), new Vector2(surface.doorPos.x, surface.doorPos.z)) < 4.5f)
            { prompt = "[E]  Enter Mission Control"; if (Inp.Down(K.E) && !busy) EnterHall(); }
            if (ap.y < -60f) astro.Spawn(sp + ship.transform.right * 9.5f + Vector3.up * 2f, 0, surface.gravity, surface.body == ss.moon);
        }
    }

    void Disembark()
    {
        pilotInShip = false; ship.controlsEnabled = false;
        ship.rb.linearVelocity = Vector3.zero; ship.rb.isKinematic = true;
        astro.gameObject.SetActive(true); astro.SetVisible(true);
        Vector3 p = ship.transform.position + ship.transform.right * 9.5f + Vector3.up * 1.5f;
        astro.Spawn(p, ship.Yaw + 90f, surface.gravity, surface.body == ss.moon); astro.firstPerson = false;
        rig.Reset(); Say("Disembarked. WASD to walk, SHIFT sprint, SPACE jump / jetpack, F flashlight.");
    }

    void Board()
    {
        astro.gameObject.SetActive(false); pilotInShip = true; ship.rb.isKinematic = false; ship.controlsEnabled = true; rig.Reset();
        Say("Back aboard Aurora.");
    }

    // ---------------------------------------------------------------- SURFACE -> SPACE
    void ExitToSpace()
    {
        Body b = surface.body; Vector3 upL = surface.upLocal; Quaternion qs = surface.qSite;
        Vector3 fScene = ship.transform.forward, vScene = ship.rb.linearVelocity;
        Transition(() =>
        {
            surface.Teardown();
            Vector3 upW = b.rot * upL, fW = b.rot * (qs * fScene);
            Vector3 vW = b.rot * (qs * vScene) / 0.3f;
            ship.SetSpaceMode();
            ship.pos = b.pos + new V3d(upW) * (b.radius + (b == ss.earth ? 3.6 : 2.6));
            ship.vel = b.vel + new V3d(Vector3.ClampMagnitude(vW, 150f));
            ship.rot = Quaternion.LookRotation(fW.sqrMagnitude > 0.01f ? fW : upW, upW);
            ship.assist = true; ship.supercruise = false; ship.landed = false;
            ss.SetVisible(true); ss.Dome.gameObject.SetActive(true); dust.SetActive(true); RestoreSpaceRender();
            mode = GameMode.Space; rig.Reset(); landCooldown = 5f; ss.warpIndex = 0;
            Say("Orbit reached. " + b.name + " is below you.");
            ship.controlsEnabled = true;
        }, 0.9f, 1.4f);
    }

    // ---------------------------------------------------------------- INTERIOR
    void EnterHall()
    {
        Transition(() =>
        {
            savedAmbientMode = RenderSettings.ambientMode; savedAmbient = RenderSettings.ambientLight;
            surface.suspend = true; ss.sunLight.enabled = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.28f, 0.3f, 0.34f); RenderSettings.fog = false;
            hall.SetActive(true); astro.Spawn(hall.spawn, 0f, 9.81f, false); astro.firstPerson = false;
            mode = GameMode.Interior; rig.Reset(); Say("Mission Control. The big screen shows Earth in real time.");
        }, 0.5f, 0.8f);
    }

    void InteriorTick(float dt)
    {
        astro.Tick(dt, !busy, cam.transform);
        Vector3 ap = astro.transform.position;
        if (Vector2.Distance(new Vector2(ap.x, ap.z), new Vector2(hall.exitPos.x, hall.exitPos.z)) < 3.8f) { prompt = "[E]  Leave Mission Control"; if (Inp.Down(K.E) && !busy) ExitHall(); }
        if (ap.y < hall.origin.y - 5f) astro.Spawn(hall.spawn, 0f, 9.81f, false);
    }

    void ExitHall()
    {
        Transition(() =>
        {
            hall.SetActive(false); surface.suspend = false; ss.sunLight.enabled = true;
            RenderSettings.ambientMode = savedAmbientMode; RenderSettings.ambientLight = savedAmbient; RenderSettings.fog = surface.atmosphere;
            astro.Spawn(surface.doorPos + new Vector3(0, 0.2f, -5f), 180f, surface.gravity, false);
            mode = GameMode.Surface; rig.Reset();
        }, 0.5f, 0.8f);
    }

    // ---------------------------------------------------------------- DEATH
    void Die()
    {
        mode = GameMode.Dead; deadTimer = 0f; ship.controlsEnabled = false; ship.visual.gameObject.SetActive(false);
        if (!pilotInShip) { astro.gameObject.SetActive(false); pilotInShip = true; }
        sfx.PlayOneShot(thud); engineSrc.volume = 0; windSrc.volume = 0;
        PostFX.Dynamic(0, 0, 0, 1f);
    }

    void DeadTick(float dt)
    {
        deadTimer += dt;
        if (deadTimer > 3.5f && !busy)
        {
            Transition(() =>
            {
                if (surface.body != null) surface.Teardown();
                hall.SetActive(false); surface.suspend = false; astro.gameObject.SetActive(false); pilotInShip = true;
                ss.SetVisible(true); ss.Dome.gameObject.SetActive(true); dust.SetActive(true); RestoreSpaceRender();
                ResetToOrbit(); mode = GameMode.Space; ship.controlsEnabled = true;
                Say("Emergency beacon recovered. A new Aurora has been launched.");
            }, 0.8f, 1.4f);
        }
    }

    // ================================================================================== LATE: CAMERA + FX
    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (mode == GameMode.Menu || mode == GameMode.Space || mode == GameMode.Dead)
        {
            if (mode == GameMode.Menu)
            {
                Vector3 toE = (ss.earth.pos - ship.pos).f.normalized;
                float a = menuT * 0.12f;
                Vector3 side = Vector3.Cross(toE, Vector3.up).normalized;
                Vector3 camPos = -toE * 19f + side * Mathf.Sin(a) * 9f + Vector3.up * (4f + Mathf.Cos(a * 0.7f) * 2f);
                Vector3 look = toE * 90f + Vector3.up * -5f;
                cam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(look - camPos));
                cam.fieldOfView = 52f; cam.nearClipPlane = 0.1f;
            }
            else if (mode == GameMode.Space)
            {
                double a; ss.Nearest(ship.pos, out a);
                Body rf = ss.ReferenceBody(ship.pos);
                float sp = (float)(ship.vel - rf.vel).mag;
                rig.SpaceUpdate(ship, dt, Mathf.Clamp01(sp / 450f));
            }
            else rig.SpaceUpdate(ship, dt, 0f);
            ss.Dome.position = cam.transform.position;
            Body rf2 = ss.ReferenceBody(ship.pos);
            dust.Tick(cam.transform.position, (ship.vel - rf2.vel).f, dt);
            float boost = ship.supercruise ? 1f : Mathf.Clamp01(ship.throttleVis - 1f);
            PostFX.Dynamic(boost, ship.heat, ship.damageFlash, 1f);
        }
        else
        {
            if (mode == GameMode.Surface && pilotInShip) rig.SurfaceShipUpdate(ship, dt);
            else rig.AstroUpdate(astro, dt);
            PostFX.Dynamic(0f, 0f, ship.damageFlash, mode == GameMode.Interior ? 0.7f : 1f);
        }
    }

    // ================================================================================== AUDIO
    void AudioTick(float dt)
    {
        bool flying = (mode == GameMode.Space || (mode == GameMode.Surface && pilotInShip)) && ship.alive;
        float tv = flying ? Mathf.Clamp01(ship.throttleVis) : 0f;
        engineSrc.volume = U.Damp(engineSrc.volume, flying ? 0.08f + tv * 0.45f : 0f, 6f, dt);
        engineSrc.pitch = 0.7f + tv * 0.7f + (ship.supercruise ? 0.5f : 0f);
        float wind = 0f;
        if (mode == GameMode.Surface && surface.atmosphere) wind = pilotInShip ? Mathf.Clamp01(ship.rb.linearVelocity.magnitude / 120f) * 0.6f + 0.05f : 0.08f;
        windSrc.volume = U.Damp(windSrc.volume, wind, 4f, dt);
    }

    // ================================================================================== setup warning overlay
    void OnGUI()
    {
        if (setupWarning == "") return;
        var s = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        s.normal.textColor = new Color(1f, 0.7f, 0.2f);
        GUI.Label(new Rect(0, Screen.height - 80, Screen.width, 60), setupWarning, s);
    }
}
