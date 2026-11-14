using System.Collections;
using System.Text;
using LastSignal.Ship;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LastSignal.M3
{
    /// <summary>Pure helpers for the flight visuals (unit-tested, no scene needed).</summary>
    public static class FxMath
    {
        public const float SpeedOfLightKmS = 299792.458f;   // physical constant

        /// <summary>One-way light time for a distance in sector units (1 unit = SectorLayout.KmPerUnit km, game compression).</summary>
        public static float LightTimeSeconds(float sectorUnits)
        {
            return sectorUnits * SectorLayout.KmPerUnit / SpeedOfLightKmS;
        }

        /// <summary>0 = full sunlight, 1 = full shadow, for a ship at p, Sun direction sunDir (unit), a sphere at c with radius r.</summary>
        public static float ShadowFactor(Vector3 p, Vector3 sunDir, Vector3 c, float r)
        {
            Vector3 v = c - p;
            float t = Vector3.Dot(v, sunDir);
            if (t <= 0f) return 0f;                       // the body is on the Sun side: it cannot shade us
            float d = Mathf.Sqrt(Mathf.Max(0f, v.sqrMagnitude - t * t));
            return Mathf.Clamp01((r * 1.1f - d) / (r * 0.2f));   // soft edge: penumbra between 0.9 r and 1.1 r
        }
    }

    /// <summary>
    /// Visual layer for the free-flight sector (M3): textured Earth/Moon/Sun using the existing shaders, solar flare burst and
    /// particle storm, asteroid rocks with trails and a close-pass warning, a comms pulse with a visible delay, eclipse dimming,
    /// thruster plume, speed lines, and bloom/vignette nudges on the project's Global Volume.
    /// It only reads game state; it never changes PlayerState or any NASA data. Cost: about 12 draw calls, no per-frame allocations
    /// except a short HUD string a few times per second. Start the player with "-lsFx off" to hide all of it (used for before shots).
    /// </summary>
    public class M3Fx : MonoBehaviour
    {
        // [game balance] display values
        const float PulseSlowdown = 4f;       // comms pulse is drawn 4x slower than real light time so it can be seen
        const float StormSeconds = 22f;       // longest a storm stays on screen
        const float FlareSeconds = 4f;
        const string Res = "M3Fx/";

        M3World _world;
        ShipController _ship;
        ChallengeSiteSet _sites;
        FreeFlight _free;
        bool _on;

        // lighting + volume
        Light _sunLight;
        float _sunBase, _ambBase;
        Color _ambColor;
        float _shadow;
        Volume _volume;
        Bloom _bloom;
        Vignette _vig;
        float _bloomBase, _vigBase;

        // sun / earth
        Transform _clouds;
        Material _corona;
        float _coronaBase = 1.6f;
        Vector3 _sunScale;
        float _flareT = -1f;
        ParticleSystem _flarePs;

        // storm
        ParticleSystem _stormPs;
        float _stormLeft;

        // thruster / speed lines
        ParticleSystem _plume, _lines;
        Transform _glow;
        float _forceThrust = -1f;

        // comms
        LineRenderer _beam;
        Transform _pulse;
        GreyboxTint _pulseTint;
        Vector3 _emitPos;
        float _clock, _lastPhase;
        bool _blackout;
        float _blackoutClear = -1f;

        // hud
        RectTransform _commsBox, _warnBox;
        Text _commsText, _warnText;
        string _warn = "";
        float _hudTimer;
        bool _forceHud;
        readonly StringBuilder _sb = new StringBuilder(160);

        public static bool Off
        {
            get
            {
                string[] a = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-lsFx" && a[i + 1] == "off") return true;
                return false;
            }
        }

        public static M3Fx Create(Transform parent, M3World world, ShipController ship, ChallengeSiteSet sites, FreeFlight free)
        {
            GameObject go = new GameObject("M3Fx");
            go.transform.SetParent(parent, false);
            M3Fx fx = go.AddComponent<M3Fx>();
            fx._world = world; fx._ship = ship; fx._sites = sites; fx._free = free;
            fx._on = !Off;
            if (fx._on) fx.Build();
            string dir = CaptureDir();
            if (dir != null) fx.StartCoroutine(fx.CaptureRoutine(dir));
            return fx;
        }

        // ---------------------------------------------------------------- build

        void Build()
        {
            RestyleBodies();
            FindLightAndVolume();
            BuildThruster();
            BuildSpeedLines();
            BuildStorm();
            BuildComms();
            BuildHud();
            if (_sites != null)
            {
                for (int i = 0; i < _sites.Sites.Count; i++)
                {
                    ChallengeSiteComponent s = _sites.Sites[i];
                    if (s.scenario == null) continue;
                    s.Entered += OnSiteEntered;
                    s.Resolution += OnSiteResolved;
                    if (s.scenario.kind == ChallengeKind.Asteroid) AsteroidPass.Create(this, s, _ship);
                }
            }
        }

        static Material Load(string name)
        {
            Material m = Resources.Load<Material>(Res + name);
            return m != null ? new Material(m) : null;   // instance: the shared asset is never edited
        }

        void RestyleBodies()
        {
            // Earth: day/night + ocean glint shader, a cloud shell and an atmosphere rim (3 draw calls).
            Material earth = Load("earth_surface");
            if (earth != null && _world.Earth != null)
            {
                Skin(_world.Earth, earth);
                Transform cl = Shell("Clouds", _world.Earth, 1.012f, Load("earth_clouds"));
                _clouds = cl;
                Shell("Atmosphere", _world.Earth, 1.06f, Load("earth_atmosphere"));
            }
            Material moon = Load("moon_surface");
            if (moon != null && _world.Moon != null) Skin(_world.Moon, moon);
            Material sun = Load("sun_surface");
            if (sun != null && _world.Sun != null)
            {
                Skin(_world.Sun, sun);
                _corona = Load("sun_corona");
                Shell("Corona", _world.Sun, 1.35f, _corona);
                _sunScale = _world.Sun.localScale;
            }
        }

        static void Skin(Transform t, Material m)
        {
            GreyboxTint tint = t.GetComponent<GreyboxTint>();
            if (tint != null) Destroy(tint);
            Renderer r = t.GetComponent<Renderer>();
            if (r != null) { r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
        }

        static Transform Shell(string name, Transform body, float scale, Material m)
        {
            if (m == null) return null;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
            go.transform.SetParent(body, false);
            go.transform.localScale = Vector3.one * scale;
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        void FindLightAndVolume()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
                if (lights[i].type == LightType.Directional) { _sunLight = lights[i]; break; }
            if (_sunLight != null) _sunBase = _sunLight.intensity;
            _ambColor = RenderSettings.ambientLight;

            _volume = FindFirstObjectByType<Volume>();
            if (_volume != null && _volume.sharedProfile != null)
            {
                VolumeProfile p = _volume.profile;   // runtime copy: the saved asset is not edited
                if (p.TryGet(out _bloom)) _bloomBase = _bloom.intensity.value;
                if (p.TryGet(out _vig)) _vigBase = _vig.intensity.value;
            }
        }

        // ---------------------------------------------------------------- particles

        static ParticleSystem NewPs(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = GreyboxKit.LineMaterial();   // one shared unlit material for every effect
            pr.shadowCastingMode = ShadowCastingMode.Off;
            pr.receiveShadows = false;
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            return ps;
        }

        static Gradient Fade(Color a, Color b)
        {
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                      new[] { new GradientAlphaKey(a.a, 0f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        void BuildThruster()
        {
            // Rear of the ship, found from its renderers so it fits whatever model ShipFactory built.
            Bounds b = new Bounds(_ship.transform.position, Vector3.zero);
            Renderer[] rs = _ship.GetComponentsInChildren<Renderer>();
            bool any = false;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] is ParticleSystemRenderer || rs[i] is TrailRenderer || rs[i] is LineRenderer) continue;
                if (!any) { b = rs[i].bounds; any = true; } else b.Encapsulate(rs[i].bounds);
            }
            Vector3 f = _ship.transform.forward;
            float half = Mathf.Abs(f.x) * b.extents.x + Mathf.Abs(f.y) * b.extents.y + Mathf.Abs(f.z) * b.extents.z;
            if (!any || half < 0.5f) half = 3f;
            Vector3 rear = _ship.transform.InverseTransformPoint(b.center - f * half);

            _plume = NewPs("Plume", _ship.transform);
            _plume.transform.localPosition = rear;
            _plume.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // cone emits along +Z, so turn it to the back
            var main = _plume.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.3f;
            main.startSpeed = 14f;
            main.startSize = 0.5f;
            main.maxParticles = 160;
            var shape = _plume.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 5f;
            shape.radius = 0.3f;
            var col = _plume.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(new Color(1f, 0.8f, 0.45f, 0.9f), new Color(1f, 0.3f, 0.1f, 0f));
            var size = _plume.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var em = _plume.emission;
            em.rateOverTime = 0f;
            _plume.Play();

            _glow = GreyboxKit.Part(PrimitiveType.Sphere, "PlumeGlow", _ship.transform, rear, Vector3.zero, new Color(1f, 0.6f, 0.25f), true).transform;
        }

        void BuildSpeedLines()
        {
            _lines = NewPs("SpeedLines", _ship.transform);
            var main = _lines.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.7f;
            main.startSpeed = 0f;
            main.startSize = 0.18f;
            main.startColor = new Color(0.8f, 0.92f, 1f, 0.55f);
            main.maxParticles = 220;
            var shape = _lines.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(70f, 45f, 140f);
            var vol = _lines.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.Local;
            vol.x = 0f; vol.y = 0f; vol.z = 0f;
            var em = _lines.emission;
            em.rateOverTime = 0f;
            ParticleSystemRenderer pr = _lines.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.lengthScale = 4f;
            pr.velocityScale = 0.03f;
            _lines.Play();
        }

        void BuildStorm()
        {
            _stormPs = NewPs("SolarStorm", transform);
            var main = _stormPs.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.6f;
            main.startSpeed = 170f;
            main.startSize = 0.35f;
            main.startColor = new Color(1f, 0.85f, 0.45f, 0.8f);
            main.maxParticles = 360;
            var shape = _stormPs.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(150f, 150f, 4f);
            var em = _stormPs.emission;
            em.rateOverTime = 0f;
            _stormPs.Play();
            ParticleSystemRenderer pr = _stormPs.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.lengthScale = 10f;
            pr.velocityScale = 0.02f;

            // One-shot flare burst from the Sun's disc (world space, so it is not scaled by the Sun object).
            _flarePs = NewPs("FlareBurst", transform);
            var fm = _flarePs.main;
            fm.loop = false;
            fm.simulationSpace = ParticleSystemSimulationSpace.World;
            fm.startLifetime = 3.2f;
            fm.startSpeed = 420f;
            fm.startSize = 900f;
            fm.startColor = new Color(1f, 0.6f, 0.2f, 0.8f);
            fm.maxParticles = 80;
            var fs = _flarePs.shape;
            fs.shapeType = ParticleSystemShapeType.Sphere;
            fs.radius = 700f;
            var fe = _flarePs.emission;
            fe.rateOverTime = 0f;
            var fc = _flarePs.colorOverLifetime;
            fc.enabled = true;
            fc.color = Fade(new Color(1f, 0.75f, 0.3f, 0.6f), new Color(1f, 0.3f, 0.1f, 0f));
        }

        // ---------------------------------------------------------------- comms

        void BuildComms()
        {
            _beam = GreyboxKit.Line("CommsBeam", transform, new Color(0.25f, 0.82f, 1f, 0.45f), 0.6f, 2);
            _beam.enabled = false;
            _pulse = GreyboxKit.Part(PrimitiveType.Sphere, "CommsPulse", transform, Vector3.zero, Vector3.one * 4f, new Color(0.25f, 0.82f, 1f), true).transform;
            _pulseTint = _pulse.GetComponent<GreyboxTint>();
            _pulse.gameObject.SetActive(false);
            _emitPos = _ship.transform.position;
        }

        // ---------------------------------------------------------------- hud

        void BuildHud()
        {
            Canvas c = UIFactory.CreateCanvas("FxHud", 32, transform);
            // Top centre, under the mission HUD; dark panels so text stays readable on bright planets.
            _commsBox = UIFactory.Box("Comms", c.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-620f, -150f), new Vector2(-20f, -90f), Palette.PanelBg);
            _commsText = UIFactory.CreateText("Text", _commsBox, "", 16, Palette.Text, TextAnchor.MiddleCenter);
            UIFactory.SetStretch(_commsText.rectTransform, 10f, 4f, 10f, 4f);
            _commsText.Mono();
            _warnBox = UIFactory.Box("Warning", c.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-660f, -214f), new Vector2(-20f, -158f), new Color(0.25f, 0.04f, 0.03f, 0.93f));
            _warnText = UIFactory.CreateText("Text", _warnBox, "", 20, Palette.Bad, TextAnchor.MiddleCenter);
            UIFactory.SetStretch(_warnText.rectTransform, 10f, 4f, 10f, 4f);
            _warnText.Bold();
            _commsBox.gameObject.SetActive(false);
            _warnBox.gameObject.SetActive(false);
        }

        public void SetWarning(string s) { _warn = s ?? ""; }

        // ---------------------------------------------------------------- events

        void OnSiteEntered(ChallengeSiteComponent s)
        {
            if (s.scenario.kind == ChallengeKind.SolarStorm) StartStorm();
            else if (s.scenario.kind == ChallengeKind.Blackout) { _blackout = true; _blackoutClear = -1f; }
        }

        void OnSiteResolved(ChallengeSiteComponent s, int option)
        {
            if (s.scenario.kind == ChallengeKind.SolarStorm) _stormLeft = Mathf.Min(_stormLeft, 3f);
            else if (s.scenario.kind == ChallengeKind.Blackout) _blackoutClear = Time.time + 2.5f;
        }

        public void StartStorm()
        {
            _stormLeft = StormSeconds;
            _flareT = 0f;
            if (_flarePs != null && _world.Sun != null)
            {
                _flarePs.transform.position = _world.Sun.position;
                _flarePs.Emit(80);
            }
        }

        // ---------------------------------------------------------------- per frame

        void Update()
        {
            if (!_on || _ship == null) return;
            float dt = Time.deltaTime;
            if (_world.Earth != null) _world.Earth.Rotate(0f, 0.4f * dt, 0f, Space.Self);
            if (_clouds != null) _clouds.Rotate(0f, 0.25f * dt, 0f, Space.Self);
            UpdateThruster();
            UpdateSpeedLines();
            UpdateEclipse(dt);
            UpdateStorm(dt);
            UpdateComms(dt);
            UpdateHud(dt);
        }

        void UpdateThruster()
        {
            float f = _forceThrust >= 0f ? _forceThrust : (_ship.maxThrust > 0f ? Mathf.Clamp01(_ship.CurrentThrust / _ship.maxThrust) : 0f);
            var em = _plume.emission;
            em.rateOverTime = f * 110f;
            _glow.localScale = Vector3.one * (f * 1.6f);
        }

        void UpdateSpeedLines()
        {
            Rigidbody rb = _ship.Body;
            Vector3 v = rb != null ? rb.linearVelocity : Vector3.zero;
            float speed = v.magnitude;
            var em = _lines.emission;
            em.rateOverTime = Mathf.InverseLerp(15f, 160f, speed) * 130f;
            Vector3 local = _ship.transform.InverseTransformDirection(v);
            var vol = _lines.velocityOverLifetime;
            vol.x = -local.x; vol.y = -local.y; vol.z = -local.z;
            var main = _lines.main;
            main.startSize = 0.12f + Mathf.InverseLerp(15f, 400f, speed) * 0.2f;
        }

        void UpdateEclipse(float dt)
        {
            Vector3 p = _ship.transform.position;
            float k = 0f;
            if (_world.Earth != null) k = Mathf.Max(k, FxMath.ShadowFactor(p, _world.SunDirection, _world.Earth.position, _world.Earth.lossyScale.x * 0.5f));
            if (_world.Moon != null) k = Mathf.Max(k, FxMath.ShadowFactor(p, _world.SunDirection, _world.Moon.position, _world.Moon.lossyScale.x * 0.5f));
            _shadow = Mathf.MoveTowards(_shadow, k, dt * 0.8f);
            if (_sunLight != null) _sunLight.intensity = Mathf.Lerp(_sunBase, _sunBase * 0.04f, _shadow);
            RenderSettings.ambientLight = Color.Lerp(_ambColor, _ambColor * 0.35f, _shadow);
        }

        void UpdateStorm(float dt)
        {
            bool storm = _stormLeft > 0f;
            if (storm)
            {
                _stormLeft -= dt;
                Vector3 s = _world.SunDirection;
                _stormPs.transform.position = _ship.transform.position + s * 160f;
                _stormPs.transform.rotation = Quaternion.LookRotation(-s);
                var em = _stormPs.emission;
                em.rateOverTime = _stormLeft > 3f ? 200f : 60f;
            }
            else if (_stormPs.emission.rateOverTime.constant > 0f)
            {
                var em = _stormPs.emission;
                em.rateOverTime = 0f;
            }

            float bump = 0f;
            if (_flareT >= 0f)
            {
                _flareT += dt;
                float k = Mathf.Clamp01(_flareT / FlareSeconds);
                bump = Mathf.Sin(k * Mathf.PI);
                if (k >= 1f) _flareT = -1f;
            }
            if (_world.Sun != null && _sunScale != Vector3.zero) _world.Sun.localScale = _sunScale * (1f + 0.25f * bump);
            if (_corona != null && _corona.HasProperty("_Intensity")) _corona.SetFloat("_Intensity", _coronaBase + 3f * bump);
            float stormGlow = storm ? 0.12f : 0f;
            if (_bloom != null) _bloom.intensity.Override(_bloomBase + 1.6f * bump + 0.15f * _shadow);
            if (_vig != null) _vig.intensity.Override(_vigBase + 0.12f * bump + stormGlow + 0.1f * _shadow);
        }

        void UpdateComms(float dt)
        {
            bool show = _forceHud || (_free != null && _free.Active);
            _beam.enabled = show;
            _pulse.gameObject.SetActive(false);
            if (!show || _world.Earth == null) return;

            if (_blackout && _blackoutClear > 0f && Time.time > _blackoutClear) _blackout = false;

            Vector3 shipP = _ship.transform.position;
            Vector3 earthP = _world.Earth.position;
            float er = _world.Earth.lossyScale.x * 0.5f;
            Vector3 dir = (earthP - shipP).normalized;
            Vector3 end = earthP - dir * er;
            float dist = Vector3.Distance(shipP, end);
            float shown = Mathf.Max(0.4f, FxMath.LightTimeSeconds(dist) * PulseSlowdown);

            float w0 = 0.4f, w1 = 0.4f + dist * 0.00012f;
            _beam.startWidth = w0; _beam.endWidth = w1;
            _beam.SetPosition(0, shipP + dir * 6f);
            _beam.SetPosition(1, end);
            Color bc = _blackout ? (Mathf.Repeat(Time.time * 7f, 1f) < 0.5f ? new Color(1f, 0.35f, 0.3f, 0.5f) : new Color(1f, 0.35f, 0.3f, 0.06f))
                                 : new Color(0.25f, 0.82f, 1f, 0.22f);
            _beam.startColor = bc;
            _beam.endColor = new Color(bc.r, bc.g, bc.b, bc.a * 0.4f);

            if (_blackout) return;   // no pulse while the link is down

            _clock += dt;
            float cycle = shown * 2f + 1.2f;
            float phase = Mathf.Repeat(_clock, cycle);
            if (phase < _lastPhase) _emitPos = shipP;   // new transmission starts from where the ship is now
            _lastPhase = phase;

            Camera cam = Camera.main;
            float camD = cam != null ? Vector3.Distance(cam.transform.position, _pulse.position) : 100f;
            if (phase < shown) Pulse(Vector3.Lerp(_emitPos, end, phase / shown), new Color(0.25f, 0.82f, 1f), camD);
            else if (phase < shown * 2f) Pulse(Vector3.Lerp(end, shipP, (phase - shown) / shown), Palette.Good, camD);
        }

        void Pulse(Vector3 pos, Color c, float camDist)
        {
            _pulse.gameObject.SetActive(true);
            _pulse.position = pos;
            float d = Camera.main != null ? Vector3.Distance(Camera.main.transform.position, pos) : 100f;
            _pulse.localScale = Vector3.one * Mathf.Max(3f, d * 0.012f);
            if (_pulseTint != null) _pulseTint.SetColor(c);
        }

        void UpdateHud(float dt)
        {
            _hudTimer -= dt;
            if (_hudTimer > 0f) return;
            _hudTimer = 0.2f;

            bool show = _forceHud || (_free != null && _free.Active);
            _commsBox.gameObject.SetActive(show && _world.Earth != null);
            if (show && _world.Earth != null)
            {
                float er = _world.Earth.lossyScale.x * 0.5f;
                float dist = Mathf.Max(0f, Vector3.Distance(_ship.transform.position, _world.Earth.position) - er);
                float lt = FxMath.LightTimeSeconds(dist);
                _sb.Length = 0;
                if (_blackout)
                {
                    _sb.Append("<color=#FF5A4D>COMMS  SIGNAL LOST  (blackout)</color>");
                }
                else
                {
                    _sb.Append("COMMS  one-way light time ").Append(lt.ToString("0.00")).Append(" s   round trip ").Append((lt * 2f).ToString("0.00")).Append(" s\n");
                    _sb.Append("<color=#8FA6BA>Earth ").Append((dist * SectorLayout.KmPerUnit).ToString("#,0")).Append(" km [game scale]  pulse ")
                       .Append(PulseSlowdown.ToString("0")).Append("x slower [game balance]");
                    _sb.Append("</color>");
                    if (_shadow > 0.25f) _sb.Append("\n<color=#FFB938>ECLIPSE: sunlight ").Append(Mathf.RoundToInt((1f - _shadow) * 100f)).Append("%</color>");
                }
                _commsText.supportRichText = true;
                _commsText.text = _sb.ToString();
            }

            bool warn = _warn.Length > 0;
            _warnBox.gameObject.SetActive(warn);
            if (warn) _warnText.text = _warn;
        }

        void OnDestroy()
        {
            if (_sunLight != null) _sunLight.intensity = _sunBase;
            RenderSettings.ambientLight = _ambColor;
        }

        // ---------------------------------------------------------------- screenshot hook (before/after)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CaptureJump()
        {
            // Capture runs only: jump straight into the flight scene (normal play is unchanged).
            if (CaptureDir() != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "M3_DeepSpace")
                UnityEngine.SceneManagement.SceneManager.LoadScene("M3_DeepSpace");
        }

        static string CaptureDir()
        {
            string[] a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-lsCapture") return a[i + 1];
            return null;
        }

        /// <summary>
        /// Used only when the game is started with "-lsCapture folder": teleports the ship to each showcase view, saves a PNG
        /// of the full screen (HUD included) and quits. Run once normally and once with "-lsFx off" for the before/after pair.
        /// </summary>
        IEnumerator CaptureRoutine(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            string tag = _on ? "after" : "before";
            yield return new WaitForSeconds(4f);
            if (_free != null) _free.enabled = false;   // no bounds force while we hop around
            _forceHud = true;
            Vector3 sun = _world.SunDirection;

            yield return Shot(dir, tag, "01_start", null, null);
            // Positions are read at call time: the floating origin shifts the sector after every teleport.
            float er = _world.Earth.lossyScale.x * 0.5f, mr = _world.Moon.lossyScale.x * 0.5f;
            yield return Shot(dir, tag, "02_earth", _world.Earth.position + sun * er * 3.2f + Vector3.up * er * 0.6f, _world.Earth.position);
            yield return Shot(dir, tag, "03_moon", _world.Moon.position + sun * mr * 5f + Vector3.up * mr, _world.Moon.position);
            yield return Shot(dir, tag, "04_sun", _ship.transform.position, _ship.transform.position + sun * 1000f);
            Vector3 side = Vector3.Cross(sun, Vector3.up).normalized;
            yield return Shot(dir, tag, "05_eclipse", _world.Earth.position - sun * er * 2.2f + side * er * 0.5f, _world.Earth.position, 2.5f);

            if (_on) StartStorm();
            yield return Shot(dir, tag, "06_storm", _ship.transform.position, _ship.transform.position + sun * 1000f, 1.8f);

            if (_sites != null)
                for (int i = 0; i < _sites.Sites.Count; i++)
                {
                    ChallengeSiteComponent s = _sites.Sites[i];
                    if (s.scenario == null || s.scenario.kind != ChallengeKind.Asteroid) continue;
                    AsteroidPass ap = s.GetComponent<AsteroidPass>();
                    if (ap != null) ap.Preview();
                    Vector3 sp = s.transform.position;
                    yield return Shot(dir, tag, "07_asteroid", sp + new Vector3(0f, 60f, -520f), sp + new Vector3(0f, 40f, 0f), 2.0f);
                }

            if (_on) { _blackout = false; }
            yield return Shot(dir, tag, "08_comms", _ship.transform.position, _world.Earth.position, 2.2f);
            if (_on) _blackout = true;
            yield return Shot(dir, tag, "09_blackout", _ship.transform.position, _world.Earth.position, 0.4f);
            _blackout = false;

            _forceThrust = 1f;
            Vector3 fwd = Vector3.forward;
            Vector3 pos = _ship.transform.position;
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                _ship.Body.linearVelocity = _ship.transform.forward * 170f;
                yield return null;
            }
            yield return Shot(dir, tag, "10_thrust_speed", null, null, 0.1f, true);
            _ship.Body.linearVelocity = Vector3.zero;
            _forceThrust = -1f;
            Debug.Log("[M3Fx] capture done: " + dir);
            Application.Quit();
        }

        IEnumerator Shot(string dir, string tag, string name, Vector3? from, Vector3? lookAt, float wait = 1.4f, bool keepMoving = false)
        {
            if (from.HasValue && _ship != null)
            {
                _ship.Body.position = from.Value;
                _ship.transform.position = from.Value;
                if (lookAt.HasValue) _ship.transform.rotation = Quaternion.LookRotation((lookAt.Value - from.Value).normalized, Vector3.up);
                _ship.Body.rotation = _ship.transform.rotation;
                _ship.Body.linearVelocity = Vector3.zero;
                _ship.Body.angularVelocity = Vector3.zero;
            }
            float t = 0f;
            while (t < wait)
            {
                if (keepMoving) _ship.Body.linearVelocity = _ship.transform.forward * 170f;
                else if (_ship != null) _ship.Body.linearVelocity = Vector3.zero;
                t += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForEndOfFrame();
            string file = System.IO.Path.Combine(dir, "M3_" + name + "_" + tag + ".png");
            ScreenCapture.CaptureScreenshot(file);
            yield return null;
            yield return null;
            Debug.Log("[M3Fx] shot " + file);
        }
    }

    /// <summary>
    /// Asteroid for one JPL close-approach site: three lumpy rocks on a straight pass with trails, and a CLOSE PASS warning with
    /// the real object designation. Rock sizes, speed and paths are game balance; the object name comes from the CSV via the scenario.
    /// </summary>
    public class AsteroidPass : MonoBehaviour
    {
        const float Length = 2500f;      // [game balance] half length of the flight line, metres
        const float Speed = 220f;        // [game balance] m/s
        const float WakeDist = 3500f;
        const float WarnDist = 700f;

        M3Fx _fx;
        ChallengeSiteComponent _site;
        ShipController _ship;
        readonly Transform[] _rocks = new Transform[3];
        readonly TrailRenderer[] _trails = new TrailRenderer[3];
        static readonly Vector3[] Spread = { Vector3.zero, new Vector3(70f, -35f, -140f), new Vector3(-90f, 50f, -230f) };
        static readonly float[] Size = { 46f, 24f, 15f };
        Vector3 _dir, _lateral;
        float _phase = -1f;
        Vector3 _lastMain;
        float _txt;
        string _name;
        static Mesh _mesh;

        public static void Create(M3Fx fx, ChallengeSiteComponent site, ShipController ship)
        {
            AsteroidPass a = site.gameObject.AddComponent<AsteroidPass>();
            a._fx = fx; a._site = site; a._ship = ship;
            a.Build();
        }

        void Build()
        {
            float ang = _site.scenario.dataRow * 1.3f + 0.7f;   // deterministic from the CSV row
            _dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang) * 0.12f, Mathf.Sin(ang)).normalized;
            _lateral = Vector3.Cross(_dir, Vector3.up).normalized * 150f + Vector3.up * 40f;
            _name = _site.scenario.facts.Count > 0 ? _site.scenario.facts[0].Value : "NEO";
            Transform root = _site.transform.parent;
            for (int i = 0; i < _rocks.Length; i++)
            {
                GameObject g = GreyboxKit.Part(PrimitiveType.Sphere, "Rock" + i, root, Vector3.zero, Vector3.one * Size[i], new Color(0.38f, 0.34f, 0.30f));
                g.GetComponent<MeshFilter>().sharedMesh = RockMesh();
                _rocks[i] = g.transform;
                TrailRenderer tr = g.AddComponent<TrailRenderer>();
                tr.sharedMaterial = GreyboxKit.LineMaterial();
                tr.time = 2.5f;
                tr.startWidth = Size[i] * 0.3f;
                tr.endWidth = 0f;
                tr.shadowCastingMode = ShadowCastingMode.Off;
                Gradient gr = new Gradient();
                gr.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.7f, 0.4f), 0f), new GradientColorKey(new Color(0.45f, 0.5f, 0.6f), 1f) },
                           new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
                tr.colorGradient = gr;
                _trails[i] = tr;
                g.SetActive(false);
            }
        }

        static Mesh RockMesh()
        {
            if (_mesh != null) return _mesh;
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh src = tmp.GetComponent<MeshFilter>().sharedMesh;
            Mesh m = Instantiate(src);
            Destroy(tmp);
            Vector3[] v = m.vertices;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 d = v[i].normalized;
                float n = Mathf.PerlinNoise(d.x * 2.1f + 5f, d.y * 2.1f + 5f) * 0.6f + Mathf.PerlinNoise(d.z * 3.3f + 9f, d.x * 3.3f + 9f) * 0.4f;
                v[i] = new Vector3(d.x * 1.25f, d.y * 0.9f, d.z) * (0.5f * (0.62f + 0.75f * n));   // same position, same bump: no cracks at the UV seam
            }
            m.vertices = v;
            m.RecalculateNormals();
            m.RecalculateBounds();
            m.name = "AsteroidRock (generated)";
            _mesh = m;
            return m;
        }

        /// <summary>Capture helper: park the rocks near the site so a screenshot shows them.</summary>
        public void Preview() { Wake(); _phase = 0.5f; }

        void Wake()
        {
            for (int i = 0; i < _rocks.Length; i++) { _rocks[i].gameObject.SetActive(true); _trails[i].Clear(); }
            _phase = 0f;
            _lastMain = _rocks[0].position;
        }

        void Update()
        {
            if (_ship == null) return;
            if (_phase < 0f)
            {
                if ((_ship.transform.position - _site.transform.position).sqrMagnitude < WakeDist * WakeDist) Wake();
                else return;
            }
            _phase += Time.deltaTime * Speed / (2f * Length);
            if (_phase >= 1f)
            {
                if (_site.Resolved) { Hide(); return; }   // done: the player has decided, the rocks leave
                Wake();
            }

            Vector3 centre = _site.transform.localPosition;
            for (int i = 0; i < _rocks.Length; i++)
            {
                _rocks[i].localPosition = centre + _lateral + Spread[i].x * Vector3.right + Spread[i].y * Vector3.up
                    + _dir * (-Length + _phase * 2f * Length + Spread[i].z);
                _rocks[i].Rotate(6f * (i + 1) * Time.deltaTime, 9f * Time.deltaTime, 3f * Time.deltaTime, Space.Self);
            }
            // The floating origin moves the whole sector at once; clear trails on that jump so no streak is drawn across it.
            Vector3 main = _rocks[0].position;
            if ((main - _lastMain).sqrMagnitude > 150f * 150f) for (int i = 0; i < _trails.Length; i++) _trails[i].Clear();
            _lastMain = main;

            _txt -= Time.deltaTime;
            if (_txt > 0f) return;
            _txt = 0.15f;
            Vector3 rel = main - _ship.transform.position;
            float d = rel.magnitude;
            if (d < WarnDist && !_site.Resolved)
            {
                Vector3 rockVel = _rocks[0].parent.TransformDirection(_dir) * Speed;
                float closing = -Vector3.Dot(rel / Mathf.Max(0.01f, d), rockVel - _ship.Body.linearVelocity);
                bool blink = Mathf.Repeat(Time.time * 4f, 1f) < 0.65f;
                _fx.SetWarning(blink ? "CLOSE PASS  " + _name + "   " + d.ToString("0") + " m   closing " + Mathf.Max(0f, closing).ToString("0") + " m/s [game scale]" : "CLOSE PASS  " + _name);
            }
            else _fx.SetWarning("");
        }

        void Hide()
        {
            for (int i = 0; i < _rocks.Length; i++) _rocks[i].gameObject.SetActive(false);
            _fx.SetWarning("");
            enabled = false;
        }
    }
}
