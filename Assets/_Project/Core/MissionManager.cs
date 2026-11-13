using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Core
{
    public enum MissionState { Idle, Running, Debrief, Failed, Finished }

    /// <summary>What the End card shows for one mission.</summary>
    public class MissionRecord
    {
        public string missionId;
        public bool hasPlan;
        public float plannedSuccess;
        public int retries;
        public bool completed;
    }

    /// <summary>
    /// Owns the game flow: Boot -> M1 -> M2 -> M3 -> M4 -> Final -> DesignMode.
    /// Created automatically (GameBootstrap) so any scene can be played on its own.
    /// Missions talk to it only through MissionEvents and MissionManager.Checkpoint(...).
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public string CurrentMissionId { get; private set; }
        public MissionState State { get; private set; }
        public string LastCheckpointName { get; private set; }
        public IList<MissionRecord> Records { get { return _records; } }

        readonly List<MissionRecord> _records = new List<MissionRecord>();
        string _startSnapshot;
        string _checkpointSnapshot;
        string _pendingRestore;
        bool _isRetry;

        HudShell _hud;
        FatherLogUI _log;
        DebriefCardUI _debrief;
        FailPanelUI _fail;
        EndCardUI _end;
        TitleScreenUI _title;

        public static MissionManager EnsureExists()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("MissionManager");
            DontDestroyOnLoad(go);
            return go.AddComponent<MissionManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _hud = new GameObject("HudShell").AddComponent<HudShell>();
            _hud.transform.SetParent(transform, false);
            new GameObject("PipDialogueUI").AddComponent<PipDialogueUI>().transform.SetParent(transform, false);
            _log = new GameObject("FatherLogUI").AddComponent<FatherLogUI>();
            _log.transform.SetParent(transform, false);
            _debrief = DebriefCardUI.Create(transform);
            _fail = FailPanelUI.Create(transform);
            _end = EndCardUI.Create(transform);
            _title = TitleScreenUI.Create(transform, NewGame, JumpTo);

            MissionEvents.MissionCompleted += OnComplete;
            MissionEvents.MissionFailed += OnFail;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void Start()
        {
            // Play-from-any-scene: the scene that is already open becomes the current mission.
            BeginMission(MissionIds.IdForScene(SceneManager.GetActiveScene().name));
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            MissionEvents.MissionCompleted -= OnComplete;
            MissionEvents.MissionFailed -= OnFail;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        // ---------------------------------------------------------------- public API

        /// <summary>Resets progress and starts at Mission 1.</summary>
        public void NewGame()
        {
            PlayerState.Current.ResetToDefaults();
            FatherLog.Clear();
            _records.Clear();
            for (int i = 0; i < MissionIds.Order.Length - 1; i++)
            {
                _records.Add(new MissionRecord { missionId = MissionIds.Order[i] });
            }
            StartMission(MissionIds.M1);
        }

        /// <summary>Loads a mission scene (and keeps current player state).</summary>
        public void StartMission(string id)
        {
            string scene = MissionIds.SceneFor(id);
            if (scene == null) { Debug.LogError("LastSignal: unknown mission id " + id); return; }
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("LastSignal: scene '" + scene + "' is not in Build Settings. Run the menu Last Signal > 1 - Create folders, scenes and core assets.");
                Pip.Say("I can't find the scene " + scene + ". Ask the team to run the setup menu.");
                return;
            }
            HideAllPanels();
            SceneManager.LoadScene(scene);
        }

        void JumpTo(string id) { StartMission(id); }

        /// <summary>Saves a checkpoint of the player state. Retry restores it.</summary>
        public void SetCheckpoint(string checkpointName)
        {
            LastCheckpointName = checkpointName;
            _checkpointSnapshot = PlayerState.Current.ToSnapshot();
        }

        /// <summary>Static shortcut: MissionManager.Checkpoint("after_launch").</summary>
        public static void Checkpoint(string checkpointName)
        {
            if (Instance != null) Instance.SetCheckpoint(checkpointName);
        }

        /// <summary>Restores the last checkpoint (or the mission start) and reloads the scene.</summary>
        public void Retry()
        {
            if (CurrentMissionId == null) return;
            MissionRecord r = GetRecord(CurrentMissionId);
            r.retries++;
            _pendingRestore = _checkpointSnapshot != null ? _checkpointSnapshot : _startSnapshot;
            _isRetry = true;
            StartMission(CurrentMissionId);
        }

        /// <summary>Called by the Mission Planner so the End card can show planned success.</summary>
        public void RecordPlan(string id, PlanConfig plan)
        {
            MissionRecord r = GetRecord(id);
            r.hasPlan = true;
            r.plannedSuccess = RiskModel.ComputeSuccess(plan);
        }

        public MissionRecord GetRecord(string id)
        {
            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].missionId == id) return _records[i];
            }
            MissionRecord r = new MissionRecord { missionId = id };
            _records.Add(r);
            return r;
        }

        // ---------------------------------------------------------------- flow

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UIFactory.EnsureEventSystem();
            BeginMission(MissionIds.IdForScene(scene.name));
        }

        void BeginMission(string id)
        {
            if (id == null) return;
            if (State == MissionState.Running && CurrentMissionId == id && !_isRetry) return; // already started

            CurrentMissionId = id;
            State = MissionState.Running;

            if (_isRetry && _pendingRestore != null)
            {
                PlayerState.Current.RestoreSnapshot(_pendingRestore);
            }
            else
            {
                _startSnapshot = PlayerState.Current.ToSnapshot();
                _checkpointSnapshot = null;
                LastCheckpointName = null;
            }
            _pendingRestore = null;
            _isRetry = false;

            bool isBoot = id == MissionIds.Boot;
            _title.SetVisible(isBoot);
            _hud.SetVisible(!isBoot);
            _hud.SetTitle(MissionIds.Title(id));
            _hud.ShowGauge(id == MissionIds.DesignMode);
            _log.SetVisible(!isBoot);

            if (isBoot) Pip.SayKey("welcome"); else Pip.Hint(id);
            MissionEvents.OnMissionStart(id);
        }

        void OnComplete(string id)
        {
            if (State != MissionState.Running || id != CurrentMissionId) return;
            GetRecord(id).completed = true;
            FatherLog.AddForMission(id);

            if (id == MissionIds.Final)
            {
                State = MissionState.Debrief;
                _end.Show(_records, delegate { StartMission(MissionIds.DesignMode); });
                return;
            }
            if (id == MissionIds.DesignMode)
            {
                State = MissionState.Finished;
                StartMission(MissionIds.Boot);
                return;
            }

            State = MissionState.Debrief;
            string next = MissionIds.Next(id);
            _debrief.Show(DebriefCard.For(id), delegate { StartMission(next); });
        }

        void OnFail(string id, string reason)
        {
            if (State != MissionState.Running || id != CurrentMissionId) return;
            State = MissionState.Failed;
            Pip.Hint(id);
            _fail.Show(reason, Retry);
        }

        void HideAllPanels()
        {
            _debrief.Hide();
            _fail.Hide();
            _end.Hide();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }
    }
}
