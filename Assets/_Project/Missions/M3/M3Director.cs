using System;
using System.Collections;
using LastSignal.Ship;
using UnityEngine;

namespace LastSignal.M3
{
    /// <summary>
    /// Runs Mission 3 "The Ship That Came Out of Nowhere" as a phase state machine:
    /// Launch -> FlyToRelay -> Targeting -> RelayDown -> Warning -> Intercept -> (RAM, checkpoint) -> Tumble
    /// -> Docking (half the supplies taken, checkpoint) -> Triage -> Scan -> Reroute -> Departing -> Complete.
    ///
    /// Fail paths: losing control after the ram (Retry restarts at the impact), choosing an unreachable
    /// destination (Retry restarts at triage), hull failure, running out of fuel before the relay.
    /// The whole scene is built from code, so the scene file only needs this one component.
    /// </summary>
    public class M3Director : MonoBehaviour
    {
        public enum Phase { Setup, Launch, FlyToRelay, Targeting, RelayDown, Warning, Intercept, Tumble, Docking, Triage, Scan, Reroute, Departing, Done, Failed }

        [Header("Assets (set by the menu Last Signal > M3 - Set up M3_DeepSpace scene)")]
        public GameObject shipPrefab;
        [Tooltip("Person 6's Horizons CSV (Assets/_Project/Data/M3_SignalPath.csv). Falls back to Resources/M3_SignalPath, then the sample file.")]
        public TextAsset signalPathCsv;
        [Tooltip("Optional: person 2's Planner prefab with OpenForReroute(float, string[], float[], Action<int>). Empty = built-in panel.")]
        public GameObject plannerPrefab;

        [Header("Layout (metres, compressed for play)")]
        public Vector3 relayPosition = new Vector3(0f, 25f, 700f);
        public float targetingRange = 400f;
        public float warningDelay = 4f;
        public float warningDuration = 5f;

        [Header("Ram (GAME-BALANCE values)")]
        public float ramHullDamage = 30f;
        public float ramPowerDamage = 50f;
        public float minPowerAfterRam = 25f;
        public float ramPush = 14f;
        [Tooltip("Spin given by the ram, rad/s in ship axes.")]
        public Vector3 ramSpinLocal = new Vector3(1.1f, 1.6f, -0.9f);

        public Phase Current { get; private set; }
        public SignalPathData SignalData { get; private set; }

        public event Action<Phase> PhaseChanged;

        ShipController _ship;
        M3World _world;
        RelaySatellite _relay;
        StrangerCraft _stranger;
        M3Hud _hud;
        SignalPathHud _signal;
        ShipTelemetryHud _telemetry;
        TargetingSequence _targeting;
        TumbleRecovery _tumble;
        TriageMinigame _triage;
        ScannerSweep _scanner;
        ReroutePlanner _reroute;
        TriageResult _triageResult;
        bool _ended;

        // ---------------------------------------------------------------- setup

        void Start()
        {
            Current = Phase.Setup;
            SignalData = LoadSignalData();

            _world = M3World.Build(transform);
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            _world.SetupCamera(cam);

            _ship = SpawnShip();
            ChaseCamera.Attach(cam, _ship.transform);
            _telemetry = ShipTelemetryHud.Create(_ship);
            _telemetry.transform.SetParent(transform, false);
            _hud = M3Hud.Create(transform);
            _signal = SignalPathHud.Create(transform, SignalData);
            _relay = RelaySatellite.Build(relayPosition, transform);
            _stranger = StrangerCraft.Build(transform);

            _targeting = gameObject.AddComponent<TargetingSequence>();
            _tumble = gameObject.AddComponent<TumbleRecovery>();
            _triage = gameObject.AddComponent<TriageMinigame>();
            _scanner = gameObject.AddComponent<ScannerSweep>();
            _reroute = gameObject.AddComponent<ReroutePlanner>();

            _ship.HullDepleted += OnHullDepleted;
            _ship.FuelEmpty += OnFuelEmpty;

            string checkpoint = ActiveCheckpoint();
            if (checkpoint == M3Checkpoint.Impact && M3Checkpoint.hasImpact) StartFromImpact();
            else if (checkpoint == M3Checkpoint.Triage && M3Checkpoint.hasTriage) StartFromTriage();
            else StartFromBeginning();
        }

        void OnDestroy()
        {
            if (_ship != null)
            {
                _ship.HullDepleted -= OnHullDepleted;
                _ship.FuelEmpty -= OnFuelEmpty;
            }
        }

        SignalPathData LoadSignalData()
        {
            TextAsset csv = signalPathCsv;
            if (csv == null) csv = Resources.Load<TextAsset>("M3_SignalPath");
            if (csv == null) csv = Resources.Load<TextAsset>("M3_SignalPath_Sample");
            if (csv == null) return SignalPathData.Parse("", "none");
            return SignalPathData.Parse(csv.text, csv.name + ".csv");
        }

        ShipController SpawnShip()
        {
            GameObject go = shipPrefab != null ? Instantiate(shipPrefab) : ShipFactory.Build();
            go.name = "Ship";
            ShipController ship = go.GetComponent<ShipController>();
            if (ship == null) ship = go.AddComponent<ShipController>();
            if (go.GetComponent<LaunchSequence>() == null) go.AddComponent<LaunchSequence>();
            return ship;
        }

        static string ActiveCheckpoint()
        {
            MissionManager mm = MissionManager.Instance;
            if (mm == null || mm.CurrentMissionId != MissionIds.M3) return null;
            return mm.LastCheckpointName;
        }

        void SetPhase(Phase p)
        {
            Current = p;
            if (PhaseChanged != null) PhaseChanged(p);
        }

        // ---------------------------------------------------------------- entry points

        void StartFromBeginning()
        {
            PlayerState.Current.SetSignalPath(true);
            _ship.Teleport(Vector3.zero, Quaternion.identity, Vector3.zero, Vector3.zero);
            if (ChaseCamera.Instance != null) ChaseCamera.Instance.SnapNow();

            SetPhase(Phase.Launch);
            _hud.SetObjective("Departure");
            string delay = SignalData.IsValid ? SignalPathData.FormatDelay(SignalData.ShipToEarthSeconds) : "hours";
            _hud.Comm(M3Lines.Control, M3Lines.ControlOrder(delay), 10f);
            LaunchSequence launch = _ship.GetComponent<LaunchSequence>();
            launch.burnDirection = LaunchSequence.BurnDirection.Forward;
            launch.Begin(OnLaunched);
        }

        void StartFromImpact()
        {
            _relay.Explode(false);
            _ship.Teleport(M3Checkpoint.impactPosition, M3Checkpoint.impactRotation, M3Checkpoint.impactVelocity, M3Checkpoint.impactSpin);
            if (ChaseCamera.Instance != null) ChaseCamera.Instance.SnapNow();
            _stranger.HoldAt(_ship, M3Checkpoint.strangerOffset);
            _hud.AddMarker(_stranger.transform, "UNKNOWN");
            _hud.Comm(M3Lines.Ship, M3Lines.RetryImpact, 4f);
            StartTumble();
        }

        void StartFromTriage()
        {
            _relay.Explode(false);
            _ship.Teleport(M3Checkpoint.triagePosition, M3Checkpoint.triageRotation, Vector3.zero, Vector3.zero);
            if (ChaseCamera.Instance != null) ChaseCamera.Instance.SnapNow();
            HideSupplyPod();
            _hud.Comm(M3Lines.Ship, "Restarted at damage triage.", 3f);
            StartTriage();
        }

        // ---------------------------------------------------------------- flight and relay

        void OnLaunched()
        {
            if (_ended) return;
            Pip.Say(M3Lines.PipLaunch);
            SetPhase(Phase.FlyToRelay);
            _hud.AddMarker(_relay.transform, "RELAY R-7");
        }

        void Update()
        {
            if (_ended || _ship == null) return;
            switch (Current)
            {
                case Phase.FlyToRelay:
                {
                    float d = Vector3.Distance(_ship.transform.position, _relay.transform.position);
                    _hud.SetObjective(M3Lines.ObjFlyToRelay + ": " + M3Ui.Distance(d));
                    if (d <= targetingRange) BeginTargeting();
                    break;
                }
                case Phase.Intercept:
                    _hud.SetObjective(M3Lines.ObjIntercept + ": " + M3Ui.Distance(_stranger.DistanceToTarget));
                    break;
            }
        }

        void BeginTargeting()
        {
            SetPhase(Phase.Targeting);
            _hud.SetObjective(M3Lines.ObjTarget);
            Pip.Say(M3Lines.PipTargeting);
            _targeting.Begin(_ship, _relay, OnRelayDestroyed);
        }

        void OnRelayDestroyed()
        {
            if (_ended) return;
            _hud.RemoveMarker(_relay.transform);
            PlayerState.Current.SetSignalPath(false);   // the Signal Path HUD breaks the chain
            _hud.Flash(0.3f);
            Pip.Say(M3Lines.PipRelayDown);
            FatherLog.Add(M3Lines.LogRelayDown);
            SetPhase(Phase.RelayDown);
            _hud.SetObjective("Relay R-7 destroyed");
            StartCoroutine(After(warningDelay, BeginWarning));
        }

        // ---------------------------------------------------------------- the stranger

        void BeginWarning()
        {
            SetPhase(Phase.Warning);
            Transform t = _ship.transform;
            Vector3 pos = t.position - t.right * 380f + t.up * 70f + t.forward * 260f;
            _stranger.Appear(pos, _ship);
            _hud.AddMarker(_stranger.transform, "UNKNOWN");
            _hud.SetObjective("Unknown craft detected");
            _hud.Comm(M3Lines.Unknown, M3Lines.UnknownWarning, warningDuration + 1f);
            StartCoroutine(After(warningDuration, BeginIntercept));
        }

        void BeginIntercept()
        {
            SetPhase(Phase.Intercept);
            Pip.Say(M3Lines.PipIntercept);
            _stranger.BeginIntercept(OnRam);
        }

        void OnRam()
        {
            if (_ended) return;
            Transform t = _ship.transform;
            Vector3 dir = (t.position - _stranger.transform.position).normalized;
            Vector3 push = dir * ramPush + t.up * 2f;
            Vector3 spinWorld = t.TransformDirection(ramSpinLocal);

            PlayerState s = PlayerState.Current;
            float hullDamage = Mathf.Min(ramHullDamage, Mathf.Max(0f, s.hull - 10f));   // the ram itself never destroys the ship
            Vector3 velocityAfter = _ship.Velocity + push;
            _ship.ApplyImpact(push, spinWorld - _ship.Body.angularVelocity, hullDamage);
            s.SetPower(Mathf.Max(s.power - ramPowerDamage, Mathf.Min(s.power, minPowerAfterRam)));

            _hud.Flash(1f);
            if (ChaseCamera.Instance != null) ChaseCamera.Instance.Shake(1.2f, 0.8f);
            GreyboxKit.Flash((t.position + _stranger.transform.position) * 0.5f, new Color(1f, 0.7f, 0.4f), 12f, 0.5f);

            Vector3 strangerOffset = -dir * 40f + Vector3.up * 10f;
            _stranger.HoldAt(_ship, strangerOffset);

            // Checkpoint after the damage, so Retry restarts in the tumble with the same state.
            M3Checkpoint.SaveImpact(t.position, t.rotation, velocityAfter, spinWorld, strangerOffset);
            MissionManager.Checkpoint(M3Checkpoint.Impact);
            StartTumble();
        }

        void StartTumble()
        {
            SetPhase(Phase.Tumble);
            _hud.SetObjective("Regain control");
            Pip.Say(M3Lines.PipTumble);
            _tumble.Begin(_ship, OnStabilized, Fail);
        }

        void OnStabilized()
        {
            if (_ended) return;
            SetPhase(Phase.Docking);
            _ship.ControlEnabled = false;
            _ship.Hold(true);
            _hud.SetObjective("Hold still. The unknown craft is docking");
            _hud.Comm(M3Lines.Unknown, M3Lines.UnknownDocking, 5f);
            _stranger.Dock(delegate { StartCoroutine(TakeSupplies()); });
        }

        IEnumerator TakeSupplies()
        {
            _hud.Comm(M3Lines.Ship, M3Lines.ShipSupplyBay, 5f);
            PlayerState s = PlayerState.Current;
            float from = s.supplies;
            float to = RerouteModel.SuppliesAfterTheft(from);
            float t = 0f;
            while (t < 4f)
            {
                t += Time.deltaTime;
                s.SetSupplies(Mathf.Lerp(from, to, t / 4f));
                _hud.SetObjective("Supplies being taken: " + Mathf.FloorToInt(s.supplies));
                yield return null;
            }
            s.SetSupplies(to);
            HideSupplyPod();
            Pip.Say(M3Lines.PipLeaving);

            bool gone = false;
            _stranger.Leave(M3World.PlanetPosition - _ship.transform.position, delegate { gone = true; });
            _hud.SetObjective("The unknown craft is leaving");
            while (!gone) yield return null;
            _hud.RemoveMarker(_stranger.transform);

            M3Checkpoint.SaveTriage(_ship.transform.position, _ship.transform.rotation);
            MissionManager.Checkpoint(M3Checkpoint.Triage);
            StartTriage();
        }

        void HideSupplyPod()
        {
            Transform pod = _ship.transform.Find("Visual/GreyboxBody/SupplyPodR");
            if (pod != null) pod.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- triage, scan, reroute

        void StartTriage()
        {
            SetPhase(Phase.Triage);
            _ship.Hold(true);
            _ship.ControlEnabled = false;
            _hud.SetObjective(M3Lines.ObjTriage);
            _triage.Open(Mathf.FloorToInt(PlayerState.Current.power), OnTriageDone);
        }

        void OnTriageDone(TriageResult r)
        {
            if (_ended) return;
            _triageResult = r;
            PlayerState s = PlayerState.Current;
            s.SetPower(s.power - r.powerUsed);
            _ship.ThrustMultiplier = r.engine ? 1f : 0.5f;
            _ship.SuppliesDrainMultiplier = r.lifeSupport ? 1f : 2f;
            if (r.hull) s.SetHull(s.hull + 15f);
            if (r.comms) _hud.Comm(M3Lines.Ship, M3Lines.CommsUseless, 7f);

            SetPhase(Phase.Scan);
            _hud.SetObjective(M3Lines.ObjScan);
            _scanner.Run(_ship.transform, OnScanDone);
        }

        void OnScanDone()
        {
            if (_ended) return;
            _world.RevealPlanet();
            PlayerState s = PlayerState.Current;
            s.heading = (M3World.PlanetPosition - _ship.transform.position).normalized;
            s.NotifyChanged();
            _hud.AddMarker(_world.Planet, "UNCHARTED PLANET");
            Pip.Say(M3Lines.PipPlanet);
            FatherLog.Add(M3Lines.LogPlanet);
            _hud.SetObjective("Planet found");
            StartCoroutine(After(2.5f, StartReroute));
        }

        void StartReroute()
        {
            SetPhase(Phase.Reroute);
            _hud.SetObjective(M3Lines.ObjReroute);
            _reroute.Open(plannerPrefab, PlayerState.Current.supplies, _triageResult.engine, _triageResult.lifeSupport, OnRouteChosen);
        }

        void OnRouteChosen(Destination d, PlanConfig plan, bool reachable)
        {
            if (_ended) return;
            PlayerState s = PlayerState.Current;
            s.SetPlannedSuccess(RiskModel.ComputeSuccess(plan));
            if (MissionManager.Instance != null) MissionManager.Instance.RecordPlan(MissionIds.M3, plan);

            Vector3 target = d.isPlanet ? M3World.PlanetPosition
                : (d.id == RerouteModel.Earth ? M3World.EarthPosition : new Vector3(-25000f, 3000f, 9000f));
            float shortBy = _reroute.CostOf(d) - s.supplies;
            SetPhase(Phase.Departing);
            _hud.SetObjective(M3Lines.ObjDepart + ": " + d.name);
            StartCoroutine(Depart(target, reachable, d.name, shortBy));
        }

        IEnumerator Depart(Vector3 target, bool reachable, string destinationName, float shortBy)
        {
            _ship.Hold(false);
            _ship.ControlEnabled = false;
            _ship.DrainSupplies = false;
            _ship.InputOverride = delegate { return AutopilotTowards(target); };
            float t = 0f;
            while (t < 7f)
            {
                t += Time.deltaTime;
                if (t > 4f) _hud.FadeTo(1f);
                yield return null;
            }
            _ship.InputOverride = null;
            if (reachable) Complete();
            else Fail(M3Lines.FailSupplies(destinationName, shortBy));
        }

        ShipInputFrame AutopilotTowards(Vector3 target)
        {
            Transform t = _ship.transform;
            Vector3 local = t.InverseTransformDirection((target - t.position).normalized);
            ShipInputFrame f = new ShipInputFrame();
            f.yaw = Mathf.Clamp(local.x * 3f, -1f, 1f);
            f.pitch = Mathf.Clamp(local.y * 3f, -1f, 1f);
            if (local.z < 0f && Mathf.Abs(local.x) < 0.2f) f.yaw = 1f;   // target straight behind: start turning
            f.thrust = local.z > 0.95f ? 1f : 0f;
            return f;
        }

        // ---------------------------------------------------------------- end states

        void OnHullDepleted()
        {
            Fail(M3Lines.FailHull);
        }

        void OnFuelEmpty()
        {
            if (Current == Phase.Launch || Current == Phase.FlyToRelay || Current == Phase.Targeting) Fail(M3Lines.FailFuel);
        }

        void Complete()
        {
            if (_ended) return;
            _ended = true;
            SetPhase(Phase.Done);
            MissionEvents.RaiseComplete(MissionIds.M3);
        }

        void Fail(string reason)
        {
            if (_ended) return;
            _ended = true;
            SetPhase(Phase.Failed);
            _targeting.Cancel();
            _ship.InputOverride = null;
            _ship.ControlEnabled = false;
            MissionEvents.RaiseFail(MissionIds.M3, reason);
        }

        IEnumerator After(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            if (!_ended && action != null) action();
        }
    }
}
