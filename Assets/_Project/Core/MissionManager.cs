using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal
{
    /// <summary>
    /// State machine: Boot -> M1 -> M2 -> M3 -> M4 -> Final -> DesignMode.
    /// Missions only raise MissionEvents. This class is the only one that loads scenes.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public PlayerState state;
        public PipLineSet pipLines;
        public FatherLogBook fatherLog;

        public string Current { get; private set; } = MissionIds.Boot;
        public int Retries { get; private set; }
        public float PlannedSuccess => state != null ? state.plannedSuccess : 0f;

        /// <summary>Game-balance estimate of how the run really went (0..1).</summary>
        public float ActualSuccess
        {
            get
            {
                if (state == null) return 0f;
                float a = state.hull / 100f * 0.4f + state.power / 100f * 0.3f + state.supplies / 100f * 0.3f;
                return Mathf.Clamp01(a - 0.03f * Retries);
            }
        }

        public bool Busy { get; private set; }

        /// <summary>
        /// Set by a mission that resumes mid-mission on Retry (M4 resumes at its approach checkpoint). The next
        /// retry then skips the generic mission intro, which would talk about the start of the mission.
        /// Cleared automatically after one retry.
        /// </summary>
        public bool SkipNextRetryIntro { get; set; }

        PlayerState.Snapshot _checkpoint;
        string _checkpointMission = MissionIds.Boot;
        readonly HashSet<string> _completed = new HashSet<string>();

        public bool IsCompleted(string id) => _completed.Contains(id);

        // UI hooks (set by GameBootstrap)
        public DebriefCardUI debriefUI;
        public FailPanelUI failUI;
        public EndCardUI endUI;
        public PipDialogueUI pipUI;
        public HudShell hud;

        public static MissionManager Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("MissionManager");
            return go.AddComponent<MissionManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (state == null) state = Resources.Load<PlayerState>("LastSignal/PlayerState");
            if (state == null) state = ScriptableObject.CreateInstance<PlayerState>();
            if (pipLines == null) pipLines = Resources.Load<PipLineSet>("LastSignal/PipLines");
            if (fatherLog == null) fatherLog = Resources.Load<FatherLogBook>("LastSignal/FatherLog");
            // Work on a runtime copy so play mode never edits the asset on disk.
            state = Instantiate(state);
            if (state.plannedSuccess <= 0f) state.plannedSuccess = RiskModel.ComputeSuccess(PlanConfig.SonsOriginalPlan());
        }

        void OnEnable()
        {
            MissionEvents.OnMissionComplete += HandleComplete;
            MissionEvents.OnMissionFail += HandleFail;
        }

        void OnDisable()
        {
            MissionEvents.OnMissionComplete -= HandleComplete;
            MissionEvents.OnMissionFail -= HandleFail;
        }

        void Start()
        {
            // A scene opened directly in the Editor adopts its own mission id.
            // M1_Ground is the first scene: it shows the title screen and waits for Begin.
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == MissionIds.SceneFor(MissionIds.M1)) return;
            foreach (var id in MissionIds.Order)
                if (id != MissionIds.Boot && MissionIds.SceneFor(id) == sceneName && Current == MissionIds.Boot)
                {
                    AdoptMission(id);
                    break;
                }
        }

        void AdoptMission(string id)
        {
            Current = id;
            SaveCheckpoint();
            MissionEvents.RaiseStart(id);
            pipUI?.Play(id);
        }

        // ---------- public API ----------

        /// <summary>Start a new run from the title screen.</summary>
        public void BeginRun()
        {
            state.ResetToDefaults();
            state.plannedSuccess = RiskModel.ComputeSuccess(PlanConfig.SonsOriginalPlan());
            Retries = 0;
            _completed.Clear();
            GoTo(MissionIds.M1);
        }

        public void SetPlannedSuccess(float p) { state.plannedSuccess = Mathf.Clamp01(p); hud?.Refresh(); }

        /// <summary>Save the state to reload from if the mission is failed.</summary>
        public void SaveCheckpoint()
        {
            _checkpoint = state.Capture();
            _checkpointMission = Current;
        }

        public void GoTo(string id)
        {
            if (Busy) return;
            StartCoroutine(GoToRoutine(id));
        }

        IEnumerator GoToRoutine(string id)
        {
            Busy = true;
            Current = id;
            SaveCheckpoint();
            string scene = MissionIds.SceneFor(id);
            if (SceneManager.GetActiveScene().name != scene)
            {
                var op = SceneManager.LoadSceneAsync(scene);
                if (op == null) { Debug.LogError($"[MissionManager] Scene '{scene}' is not in Build Settings. Run LastSignal > Setup > Run All."); Busy = false; yield break; }
                while (!op.isDone) yield return null;
            }
            Busy = false;
            MissionEvents.RaiseStart(id);
            hud?.Refresh();
            pipUI?.Play(id);
        }

        /// <summary>Reload the current mission scene from the last checkpoint.</summary>
        public void RetryFromCheckpoint()
        {
            if (Busy) return;
            Retries++;
            state.Restore(_checkpoint);
            Current = _checkpointMission;
            StartCoroutine(RetryRoutine());
        }

        IEnumerator RetryRoutine()
        {
            Busy = true;
            var op = SceneManager.LoadSceneAsync(MissionIds.SceneFor(Current));
            while (op != null && !op.isDone) yield return null;
            Busy = false;
            MissionEvents.RaiseStart(Current);
            hud?.Refresh();
            if (!SkipNextRetryIntro) pipUI?.Play(Current);
            SkipNextRetryIntro = false;
        }

        public void ContinueAfterDebrief()
        {
            string next = MissionIds.Next(Current);
            if (next == null) return;
            GoTo(next);
        }

        // ---------- event handlers ----------

        void HandleComplete(string id)
        {
            // Completion is processed once per run. A second Complete for the same mission (double click, a
            // mission raising it twice) would stack debrief cards or open the end card twice.
            // BeginRun clears the set, so a new run starts clean.
            if (!_completed.Add(id)) return;
            // Any scene can call this directly: adopt the id if we were not tracking it yet.
            if (Current != id) Current = id;
            if (id == MissionIds.Final)
            {
                if (endUI != null) endUI.Show(PlannedSuccess, ActualSuccess, Retries);
                else GoTo(MissionIds.DesignMode);
                return;
            }
            var card = Resources.Load<DebriefCard>("LastSignal/Debrief_" + id);
            if (debriefUI != null) debriefUI.Show(card != null ? card : FallbackCard(id));
            else ContinueAfterDebrief();
        }

        void HandleFail(string id, string reason)
        {
            if (failUI != null && failUI.IsOpen) return; // one fail panel at a time
            if (Current != id) Current = id;
            string hint = pipLines != null ? pipLines.Find(id)?.failHint : null;
            if (failUI != null) failUI.Show(id, reason, string.IsNullOrEmpty(hint) ? "Try again. Check the plan first." : hint);
            else RetryFromCheckpoint();
        }

        static DebriefCard FallbackCard(string id)
        {
            var c = ScriptableObject.CreateInstance<DebriefCard>();
            c.title = MissionIds.TitleFor(id);
            c.concept = "(placeholder concept)";
            c.nasaSource = "(placeholder NASA source)";
            c.body = "Debrief for " + id + " has not been written yet.";
            return c;
        }
    }
}
