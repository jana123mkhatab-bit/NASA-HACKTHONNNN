using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.M4
{
    /// <summary>Adds the M4 mission to the M4_Planet scene every time it loads (no scene edits, no merge conflicts).</summary>
    static class M4Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (s.name != MissionIds.SceneFor(MissionIds.M4)) return;
            if (Object.FindFirstObjectByType<M4Mission>() != null) return;
            new GameObject("M4 Mission").AddComponent<M4Mission>();
        }
    }

    /// <summary>
    /// Mission 4 "Someone's Been Living Here": scan -> pick site -> land -> collector -> cable -> son's ship -> log
    /// -> reveal -> survey packet -> transmit -> RaiseComplete("M4"). Also hosts the Final (Missions/Final/FinalCallHome).
    /// </summary>
    public class M4Mission : MonoBehaviour
    {
        public static M4Mission Instance { get; private set; }

        public M4World World { get; private set; }
        public M4UI UI { get; private set; }
        public M4CameraRig Rig { get; private set; }
        public M4PostFx PostFx { get; private set; }
        public LandingController Lander { get; private set; }
        public SurfaceWalker Father { get; private set; }
        public SurfaceWalker Son { get; private set; }
        public ExoplanetRecord Planet { get; private set; }
        public float Gravity { get; private set; }
        public LandingSite Site { get; private set; }

        string _source;
        int _siteIndex;
        float _startBattery;
        bool _walking, _waterRead, _logRead, _finalStarted;
        M4Interactable _collectorUse, _hatchUse;

        static PlayerState State => MissionManager.Instance != null ? MissionManager.Instance.state : null;

        void Awake() { Instance = this; }

        void OnEnable() { MissionEvents.OnMissionStart += OnMissionStart; }
        void OnDisable() { MissionEvents.OnMissionStart -= OnMissionStart; }
        void OnDestroy() { if (Instance == this) Instance = null; SurfaceWalker.ClearInteractables(); }

        void Start()
        {
            var stub = FindFirstObjectByType<StubMission>();
            if (stub != null) Destroy(stub.gameObject);

            Planet = ExoplanetCsv.Load(out _source);
            World = gameObject.AddComponent<M4World>();
            World.Build(Planet);
            Rig = M4CameraRig.Attach();
            UI = M4UI.Create(transform);
            PostFx = M4PostFx.Create(transform);
            // Game-scaled gravity: proportional to the CSV-based estimate so a heavier world is harder to land on.
            Gravity = Mathf.Clamp(2.4f * Planet.GravityEstimateG, 1.6f, 5f);

            var mm = MissionManager.Instance;
            if (mm != null && mm.Current == MissionIds.Final) StartFinal();
            else if (M4Checkpoint.ShouldResume()) BeginApproach(M4Checkpoint.siteIndex, true);
            else EnterOrbit();
        }

        void OnMissionStart(string id)
        {
            if (id == MissionIds.Final && World != null) StartFinal();
            // After a Retry, Core raises Start("M4") and then plays the M4 intro ("pick your landing site"), which would
            // replace the checkpoint lines. Play them again one frame later so the player hears the right thing.
            else if (id == MissionIds.M4 && _pendingPip != null) StartCoroutine(PipNextFrame(_pendingPip));
        }

        string[] _pendingPip;

        IEnumerator PipNextFrame(string[] lines)
        {
            yield return null;
            if (_pendingPip != lines) yield break;
            _pendingPip = null;
            Pip(lines);
        }

        static void Pip(params string[] lines)
        {
            var mm = MissionManager.Instance;
            if (mm != null && mm.pipUI != null) mm.pipUI.PlayLines(lines);
        }

        /// <summary>Hides Pip's box (it would sit over the letterbox subtitles). An empty PlayLines closes it at once.</summary>
        public static void SilencePip()
        {
            var mm = MissionManager.Instance;
            if (mm != null && mm.pipUI != null) mm.pipUI.PlayLines(new string[0]);
        }

        static void Hud(bool on)
        {
            var mm = MissionManager.Instance;
            if (mm != null && mm.hud != null) mm.hud.SetVisible(on);
        }

        // ================================================================== orbit + site picker

        void EnterOrbit()
        {
            M4Hooks.Phase("Orbit");
            World.ShowSurface(false);
            PostFx.SetSurface(false);
            Rig.Orbit();
            UI.ShowScanner(Planet, _source, OpenPicker);
            UI.SetObjective("Read the scan, then choose a landing site");
        }

        void OpenPicker()
        {
            M4Hooks.Phase("SitePicker");
            UI.HideScanner();
            UI.SetObjective(null);
            UI.ShowPicker(World.Sites, i => BeginApproach(i, false));
        }

        // ================================================================== descent

        void BeginApproach(int index, bool resumed)
        {
            _siteIndex = Mathf.Clamp(index, 0, World.Sites.Length - 1);
            Site = World.Sites[_siteIndex];
            UI.HidePicker(); UI.HideScanner();
            World.ShowSurface(true);
            PostFx.SetSurface(true);
            World.ShowEllipse(Site, true);

            if (Lander == null)
            {
                Lander = LandingController.Create(World.Root);
                Lander.Finished += OnLandingFinished;
            }

            // Approach from the south-west so the low sun rakes across the terrain in the chase view.
            Vector2 approachDir = new Vector2(0.55f, 0.83f).normalized;
            Vector2 startXZ = Site.xz - approachDir * 70f;
            float siteGround = M4World.Height(Site.xz.x, Site.xz.y);
            float y = Mathf.Max(siteGround + 150f, M4World.Height(startXZ.x, startXZ.y) + 70f);
            // 2.4 m/s of drift toward the site: a hands-off 25-35 s descent ends within about 15 m of the target,
            // so the valley's 26 m ellipse is forgiving while the 10-11 m ellipses need RCS corrections.
            var startVel = new Vector3(approachDir.x, 0f, approachDir.y) * 2.4f + Vector3.down * 9f;

            var st = State;
            float fuel = Mathf.Max(st != null ? st.fuel : 100f, 55f);                 // floor so a rough M3 is still landable
            _startBattery = Mathf.Clamp(st != null ? st.power : 100f, 40f, 65f);       // descent battery (game balance)
            if (st != null) st.fuel = fuel;

            M4Checkpoint.SaveApproach(_siteIndex);
            MissionManager.Instance?.SaveCheckpoint();

            Lander.Begin(Site, Gravity, fuel, _startBattery, new Vector3(startXZ.x, y, startXZ.y), startVel);
            Rig.Chase(Lander.transform, Mathf.Atan2(approachDir.x, approachDir.y) * Mathf.Rad2Deg);
            UI.ShowLanding(true);
            UI.SetObjective($"Land inside the ellipse at site {Site.id} {Site.name}");
            M4Hooks.Phase("Descent");
            if (resumed)
            {
                _pendingPip = new[] { $"Back at the approach checkpoint. Site {Site.id}, same limits.", $"Keep the sink rate under {Site.SafeVerticalSpeed:0.0} metres per second." };
                Pip(_pendingPip);
            }
            else Pip("Descent started. Hold SPACE for the main engine, WASD to translate.", $"Touch down inside the gold ellipse, sink rate under {Site.SafeVerticalSpeed:0.0} m/s. Watch the battery.");
        }

        void OnLandingFinished(bool ok, string reason)
        {
            UI.ShowLanding(false);
            M4Hooks.Landed(ok, reason);
            if (!ok)
            {
                Rig.Shake(1.1f, 1.2f);
                StartCoroutine(FailAfter(reason));
                return;
            }
            Rig.Shake(0.25f, 0.6f);
            var st = State;
            if (st != null)
            {
                st.fuel = Lander.Fuel;
                st.power = Mathf.Clamp(st.power - (_startBattery - Lander.Power), 0f, 100f);
            }
            M4Checkpoint.Clear();
            MissionManager.Instance?.SaveCheckpoint();
            World.ShowEllipse(Site, false);
            Pip("Contact light. We're down. " + reason, Site.id == "A" ? "That beacon is close. Someone is powering it." : "Long walk to the beacon. Someone is powering it.");
            StartCoroutine(AfterLanding());
        }

        IEnumerator FailAfter(string reason)
        {
            yield return new WaitForSeconds(1.6f);
            MissionEvents.RaiseFail(MissionIds.M4, reason);
        }

        IEnumerator AfterLanding()
        {
            M4Hooks.Phase("Landed");
            yield return new WaitForSeconds(2.2f);
            SpawnFather(Lander.transform.position);
            BeginWalk();
        }

        // ================================================================== on foot

        void SpawnFather(Vector3 near)
        {
            Vector3 toCollector = World.Collector.position - near; toCollector.y = 0;
            Vector2 xz = new Vector2(near.x, near.z) + new Vector2(toCollector.x, toCollector.z).normalized * 5f;
            float yaw = Mathf.Atan2(toCollector.x, toCollector.z) * Mathf.Rad2Deg;
            if (Father == null) Father = SurfaceWalker.Spawn("Father", false, M4World.Ground(xz, 0.1f), yaw, World.Root);
            else Father.Teleport(M4World.Ground(xz, 0.1f), yaw);
            Father.gravity = Gravity;
            Rig.Follow(Father.transform, yaw);
        }

        void BeginWalk()
        {
            M4Hooks.Phase("Walk");
            _walking = true;
            EnsureInteractables();
            UI.SetObjective("Walk to the beacon", World.Collector.position, Father.transform);
        }

        void EnsureInteractables()
        {
            if (_collectorUse == null)
            {
                _collectorUse = World.Collector.gameObject.AddComponent<M4Interactable>();
                _collectorUse.prompt = "Read the water collector";
                _collectorUse.radius = 6.5f;
                _collectorUse.onUse = ReadCollector;
                SurfaceWalker.Register(_collectorUse);
            }
            if (_hatchUse == null)
            {
                _hatchUse = World.SonsHatch.gameObject.AddComponent<M4Interactable>();
                _hatchUse.prompt = "Open the ship's hatch";
                _hatchUse.radius = 5f;
                _hatchUse.onUse = OpenHatch;
                _hatchUse.available = false;
                SurfaceWalker.Register(_hatchUse);
            }
        }

        void ReadCollector()
        {
            _collectorUse.available = false;
            var ci = CultureInfo.InvariantCulture;
            string temp = float.IsNaN(Planet.eqTempK) ? "" :
                $"\n<color=#8FA7BF>Equilibrium temperature {Planet.eqTempK.ToString("0", ci)} K ({(Planet.eqTempK - 273.15f).ToString("0", ci)} C): " +
                (Planet.eqTempK < 273f ? "below freezing, so the condenser is heated to keep the water liquid.</color>" : "warm enough for liquid water.</color>");
            UI.ShowInfo("WATER COLLECTOR",
                "Reservoir                312 L\n" +
                "Purity                     97.4 %  (drinkable after filtering)\n" +
                "Output                    2.1 L per day\n" +
                "Last condensation   6 hours ago\n" + temp +
                "\n\nA hand-welded patch on the tank. Fresh tape on the mast.\nA powered cable runs off toward the north-west.\n\n<b>Someone has been living here.</b>",
                () =>
                {
                    _waterRead = true;
                    _hatchUse.available = true;
                    UI.SetObjective("Follow the cable", World.SonsShip.position, Father.transform);
                    Pip("Water readings saved to the survey.", "The cable is live. Follow the light along it.");
                });
        }

        void OpenHatch()
        {
            _hatchUse.available = false;
            UI.SetObjective(null);
            // Stage the father on the clear spot in front of the hatch while the log panel covers the screen,
            // so the reveal never depends on where the player happened to stand.
            Father.Teleport(World.HatchStandPoint(), World.HatchStandYaw());
            UI.ShowSonLog(() => { _logRead = true; PlayReveal(); });
        }

        void Update()
        {
            if (Lander != null && Lander.Active) UI.UpdateLanding(Lander);

            if (_walking && Father != null)
            {
                bool free = !UI.ModalOpen;
                Father.inputEnabled = free;
                var near = free ? Father.Nearby : null;
                UI.SetPrompt(near != null ? near.prompt : null);
                if (near != null && M4Input.InteractDown()) near.onUse?.Invoke();
            }
            else if (UI != null) UI.SetPrompt(null);

            DebugSkips();
        }

        // ================================================================== reveal

        void PlayReveal()
        {
            M4Hooks.Phase("Reveal");
            _walking = false;
            Father.inputEnabled = false;
            if (Son == null)
            {
                var hide = World.SonHidingSpot.position;
                Vector3 toFather = Father.transform.position - hide; toFather.y = 0f;
                float yaw = Mathf.Atan2(toFather.x, toFather.z) * Mathf.Rad2Deg;
                Son = SurfaceWalker.Spawn("Son", true, M4World.Ground(new Vector2(hide.x, hide.z), 0.1f), yaw, World.Root);
                Son.inputEnabled = false;
                Son.gravity = Gravity;
            }
            if (!M4Hooks.TryExternalReveal(AfterReveal)) StartCoroutine(RevealFallback());
        }

        IEnumerator RevealFallback()
        {
            Hud(false);
            SilencePip();
            UI.SetObjective(null);
            yield return StartCoroutine(UI.Letterbox(true));
            PostFx.Cinematic(true, 9f);

            var f = Father.transform;
            Vector3 toShip = World.SonsShip.position - f.position; toShip.y = 0; toShip.Normalize();
            Vector3 shipSide = Vector3.Cross(Vector3.up, toShip);
            // The son comes around the nose; every later shot is framed on the father-son line, not the hull.
            Vector3 toSon = Son.transform.position - f.position; toSon.y = 0; toSon.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, toSon);

            // Shot 1: low, behind the father, the ship looming. Silence.
            Rig.Shot(f.position - toShip * 4f + shipSide * 1.2f + Vector3.up * 1.1f, f.position - toShip * 3f + shipSide * 0.8f + Vector3.up * 1.0f,
                World.SonsShip, Vector3.up * 1.5f, 4f, 38f);
            UI.Subtitle(null, "Footsteps on the gravel, from around the nose of the ship.");
            yield return new WaitForSeconds(2f);

            // The son steps out and stops a few metres in front of the father.
            var mark = new GameObject("Reveal Mark").transform;
            mark.SetParent(World.Root, false);
            mark.position = M4World.Ground(new Vector2(f.position.x, f.position.z) + new Vector2(toSon.x, toSon.z) * 3.0f, 0.05f);
            mark.rotation = Quaternion.LookRotation(-toSon);
            Son.followOffset = Vector3.zero;
            Son.followTarget = mark;
            yield return new WaitForSeconds(3.5f);

            // Shot 2: over the father's shoulder onto the son, slow push-in, shallow focus.
            Son.followTarget = null;
            Vector3 miss = Son.transform.position - mark.position; miss.y = 0f;
            if (miss.magnitude > 0.8f) Son.Teleport(mark.position, mark.eulerAngles.y); // never frame the line on a stuck walker
            Son.transform.rotation = Quaternion.LookRotation(-toSon);
            Father.transform.rotation = Quaternion.LookRotation(toSon);
            Vector3 ots = f.position - toSon * 1.6f + side * 0.7f + Vector3.up * 1.65f;
            Rig.Shot(ots, ots + toSon * 0.8f, Son.transform, Vector3.up * 1.55f, 16f, 30f, 1.0f);
            PostFx.Focus(3.4f);
            string[,] lines =
            {
                { "SON", "Dad." },
                { "FATHER", "...You're alive." },
                { "SON", "The ship that rammed you. The one that took half your supplies." },
                { "SON", "That was me." },
                { "SON", "I left the heading in your scanner. I needed you to find this place." },
                { "FATHER", "Four thousand, three hundred and eighty-one tries." },
                { "SON", "And one more. Let's make this one count." },
            };
            for (int i = 0; i < lines.GetLength(0); i++)
            {
                UI.Subtitle(lines[i, 0], lines[i, 1]);
                yield return new WaitForSeconds(lines[i, 1].Length < 12 ? 2.0f : 3.4f);
            }

            // Shot 3: wide two-shot, the ship and the far beacon.
            Vector3 mid = (Father.transform.position + Son.transform.position) * 0.5f;
            // Camera on the side away from the hull (the rig has no collision, so it must never dolly into the ship).
            Vector3 away = Vector3.Dot(side, mid - World.SonsShip.position) >= 0f ? side : -side;
            Rig.Shot(mid + away * 9f - toSon * 3f + Vector3.up * 2.5f, mid + away * 11f - toSon * 5f + Vector3.up * 3.5f, Son.transform, Vector3.up * 0.8f, 4.5f, 45f, 1.6f);
            PostFx.Focus(10f);
            UI.Subtitle(null, "");
            yield return new WaitForSeconds(3.5f);

            PostFx.Cinematic(false);
            yield return StartCoroutine(UI.Letterbox(false));
            Hud(true);
            Destroy(mark.gameObject);
            AfterReveal();
        }

        void AfterReveal()
        {
            if (Son != null) { Son.followOffset = new Vector3(1.6f, 0f, -0.4f); Son.followTarget = Father.transform; }
            Rig.Follow(Father.transform, Rig.Yaw);
            OpenSurvey();
        }

        // ================================================================== survey + transmit

        void OpenSurvey()
        {
            M4Hooks.Phase("Survey");
            Father.inputEnabled = false;
            var ci = CultureInfo.InvariantCulture;
            Vector3 lp = Lander != null ? Lander.transform.position : M4World.Ground(World.Sites[0].xz);
            UI.ShowSurvey(v => { if (Lander != null && Lander.Dish != null) Lander.Dish.localRotation = Quaternion.Euler(0f, v * 70f, 0f); }, OnTransmitted);
            UI.SetSurveyItem(0, true, "Landing coordinates   " + Coordinates(lp));
            UI.SetSurveyItem(1, _waterRead, _waterRead ? "Water readings   312 L, 97.4 % purity, 2.1 L/day" : "Water readings   (read the collector)");
            var log = MissionManager.Instance != null ? MissionManager.Instance.fatherLog : null;
            int n = log != null ? log.entries.Count : 0;
            UI.SetSurveyItem(2, true, $"Father's flight log   ({n} entries)");
            UI.SetSurveyItem(3, _logRead, $"Son's flight log   ({M4UI.SonAttempts.ToString("#,0", ci)} transmission attempts)");
            Pip("Everything Earth needs is in one packet.", "Align the high-gain antenna, then transmit.");
        }

        /// <summary>Planetographic coordinates from the landing position and the CSV planet radius (fictional base point).</summary>
        string Coordinates(Vector3 p)
        {
            float r = (float.IsNaN(Planet.radiusEarth) ? 1f : Planet.radiusEarth) * 6371000f;
            float lat = -12.4471f + p.z / r * Mathf.Rad2Deg;
            float lon = 141.2093f + p.x / (r * Mathf.Cos(lat * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
            var ci = CultureInfo.InvariantCulture;
            return $"{Mathf.Abs(lat).ToString("0.0000", ci)}° {(lat < 0 ? "S" : "N")}  {Mathf.Abs(lon).ToString("0.0000", ci)}° {(lon < 0 ? "W" : "E")}";
        }

        void OnTransmitted()
        {
            UI.HideSurvey();
            var st = State;
            if (st != null) st.signalPathUp = true;
            M4Hooks.Transmitted();
            M4Hooks.Phase("Transmitted");
            Pip("Packet away. Earth has it.");
            StartCoroutine(CompleteSoon());
        }

        IEnumerator CompleteSoon()
        {
            yield return new WaitForSeconds(2.5f);
            M4Checkpoint.Clear();
            MissionEvents.RaiseComplete(MissionIds.M4);
        }

        // ================================================================== final hand-off

        /// <summary>Puts the world in its post-M4 state (if needed) and starts the Final's call home.</summary>
        public void StartFinal()
        {
            if (_finalStarted) return;
            _finalStarted = true;
            _walking = false;
            UI.HideAll();
            World.ShowSurface(true);
            PostFx.SetSurface(true);
            if (Lander == null)
            {
                Lander = LandingController.Create(World.Root);
                Lander.Park(M4World.Ground(World.Sites[0].xz), 30f);
            }
            if (Father == null)
            {
                // Same collider-free spot used for the reveal, facing away from the hatch.
                float yaw = World.HatchStandYaw() + 180f;
                Father = SurfaceWalker.Spawn("Father", false, World.HatchStandPoint(), yaw, World.Root);
                Father.gravity = Gravity;
                Rig.Follow(Father.transform, yaw);
            }
            if (Son == null)
            {
                // In front of the father (away from the ship): sideways would put him in the crates or under the wing.
                var p = Father.transform.position + Father.transform.forward * 1.8f;
                Son = SurfaceWalker.Spawn("Son", true, M4World.Ground(new Vector2(p.x, p.z), 0.05f), Father.transform.eulerAngles.y, World.Root);
                Son.gravity = Gravity;
            }
            Son.inputEnabled = false;
            Son.followTarget = Father.transform;
            var final = gameObject.GetComponent<LastSignal.Finale.FinalCallHome>();
            if (final == null) final = gameObject.AddComponent<LastSignal.Finale.FinalCallHome>();
            final.Begin(this);
        }

        // ================================================================== editor-only debug skips (F5..F8)

        void DebugSkips()
        {
            int k = M4Input.DebugSkip();
            if (k == 0) return;
            switch (k)
            {
                case 5: // jump to the approach at the valley
                    if (Lander == null || !Lander.Active) BeginApproach(0, false);
                    break;
                case 6: // land now
                    if (Father == null)
                    {
                        if (Site == null) { Site = World.Sites[0]; World.ShowSurface(true); PostFx.SetSurface(true); }
                        if (Lander == null) { Lander = LandingController.Create(World.Root); Lander.Finished += OnLandingFinished; }
                        Lander.Park(M4World.Ground(Site.xz), 30f);
                        UI.HideAll(); UI.ShowLanding(false);
                        SpawnFather(Lander.transform.position);
                        BeginWalk();
                    }
                    break;
                case 7: // straight to the reveal
                    if (Son != null || _finalStarted) break;
                    if (Father == null) { DebugSkipTo6(); }
                    _waterRead = true; _logRead = true;
                    Father.Teleport(World.HatchStandPoint(), World.HatchStandYaw());
                    UI.HideAll();
                    PlayReveal();
                    break;
                case 8:
                    StartFinal();
                    break;
            }
        }

        void DebugSkipTo6()
        {
            Site = World.Sites[0];
            World.ShowSurface(true); PostFx.SetSurface(true);
            if (Lander == null) { Lander = LandingController.Create(World.Root); Lander.Finished += OnLandingFinished; }
            Lander.Park(M4World.Ground(Site.xz), 30f);
            SpawnFather(Lander.transform.position);
            EnsureInteractables();
        }
    }
}
