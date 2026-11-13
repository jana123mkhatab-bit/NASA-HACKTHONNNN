using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// Mission 2, "Ten Percent". The father opens his son's original flight assessment (about 10%; it never
    /// reached the pilot's briefing), stores it, then fixes the plan in the Mission Planner. Every system
    /// check runs against the plan he applied: an untested single unit fails qualification, and the
    /// emergency radio cut only keeps the link if the plan carries a backup radio. Preflight clears at 70%.
    /// Reliabilities and masses are game-balance values (see PlannerPanel).
    /// </summary>
    public class M2PreflightMission : MonoBehaviour
    {
        enum Result { Pending, Pass, Fail }
        static readonly string[] CheckNames = { "Life support", "Comms", "Navigation", "Emergency radio cut" };
        static readonly string[] CheckLamps = { "LifeSupport", "Comms", "Navigation", null };

        readonly Result[] _checks = new Result[4];
        bool _sonRecord, _fatherRecord, _busy, _shown;
        PlanConfig _applied;
        PlannerPanel _planner;
        CameraRig _rig;
        Transform _systems, _radioLive;

        Canvas _canvas;
        RectTransform _root, _left, _right, _console;
        readonly Text[] _chips = new Text[4];
        readonly Button[] _runBtns = new Button[4];
        readonly Text[] _objChips = new Text[3];
        readonly Text[] _objText = new Text[3];
        Text _detail, _recSon, _recFather;
        Button _sonBtn;

        void Start()
        {
            var stub = FindFirstObjectByType<StubMission>();
            if (stub != null) Destroy(stub.gameObject);
            var cam = Camera.main;
            _rig = cam != null ? cam.GetComponent<CameraRig>() : null;
            var ship = GameObject.Find("M2 Stage/Ship");
            if (ship != null)
            {
                _systems = ship.transform.Find("Systems");
                _radioLive = _systems != null ? _systems.Find("Radio/Live") : null;
            }
            else Debug.LogWarning("[M2] 'M2 Stage' not found. Run LastSignal > Setup > Build M1 and M2 scenes.");
            BuildUI();
            Refresh();
        }

        // =============================================================== UI

        void BuildUI()
        {
            _canvas = UIFactory.CreateCanvas("M2 Preflight", 20);
            _canvas.transform.SetParent(transform, false);
            _root = (RectTransform)_canvas.transform;
            float top = -(UIFactory.TopBar + UIFactory.Margin);

            _left = UIFactory.Window("Checks", _root, new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(24, 24), new Vector2(24 + UIFactory.ColumnW, top), "PREFLIGHT CHECKS", "RUN");
            UIFactory.Label("Intro", _left, "Every check runs against the plan on file. Change the plan and the checks reset.", 17,
                TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -122), new Vector2(-20, -68), Palette.TextDim);
            for (int i = 0; i < 4; i++)
            {
                int k = i;
                float y = -132 - i * 96;
                var card = UIFactory.Round("Check" + i, _left, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, y - 84), new Vector2(-20, y), Palette.Inset, 8);
                card.raycastTarget = false;
                UIFactory.Label("Name", card.rectTransform, CheckNames[i], 22, TextAnchor.UpperLeft, Vector2.zero, Vector2.one,
                    new Vector2(16, 0), new Vector2(-150, -12)).Bold();
                _chips[i] = UIFactory.Chip("Chip", card.rectTransform, "PENDING", Palette.TextDim, new Vector2(0, 0), new Vector2(0, 0),
                    new Vector2(16, 12), new Vector2(126, 40));
                _runBtns[i] = UIFactory.Button("Run", card.rectTransform, i == 3 ? "CUT RADIO" : "RUN CHECK", new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                    new Vector2(-148, -24), new Vector2(-14, 24), () => StartCoroutine(RunCheck(k)), i == 3 ? ButtonStyle.Danger : ButtonStyle.Primary);
            }
            var box = UIFactory.Round("DetailBox", _left, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 22), new Vector2(-20, -530), Palette.Inset, 8);
            box.raycastTarget = false;
            UIFactory.Label("DetailL", box.rectTransform, "CHECK LOG", 14, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(16, -30), new Vector2(-16, -8), Palette.Accent).Mono();
            _detail = UIFactory.Label("Detail", box.rectTransform, "No checks run yet.", 19, TextAnchor.UpperLeft, Vector2.zero, Vector2.one,
                new Vector2(16, 12), new Vector2(-16, -36));

            _right = UIFactory.Window("Records", _root, new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-24 - UIFactory.ColumnW, 24), new Vector2(-24, top), "FLIGHT RECORDS", "PLAN");
            _recSon = Record(_right, -72, "RECORD 1  ·  SURVEY FLIGHT 041, ORIGINAL ASSESSMENT");
            _recFather = Record(_right, -312, "RECORD 2  ·  THE FATHER'S REVISED PLAN");
            _sonBtn = UIFactory.Button("OpenSon", _right, "OPEN THE SON'S ASSESSMENT", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(20, 96), new Vector2(-20, 160), OpenSonsAssessment);
            UIFactory.Button("Planner", _right, "OPEN MISSION PLANNER", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(20, 22), new Vector2(-20, 86), OpenPlanner, ButtonStyle.Ghost);

            _console = UIFactory.Window("Console", _root, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-460, 24), new Vector2(460, 24 + UIFactory.ConsoleH));
            string[] obj =
            {
                "Open and store the son's original assessment",
                "Fix the plan: at least 70% inside the mass budget",
                "Pass all system checks and the emergency radio cut",
            };
            for (int i = 0; i < 3; i++)
            {
                float y = -22 - i * 50;
                _objChips[i] = UIFactory.Chip("Chip" + i, _console, "", Palette.TextDim, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(20, y - 34), new Vector2(110, y));
                _objText[i] = UIFactory.Label("Obj" + i, _console, obj[i], 19, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(122, y - 34), new Vector2(600, y));
            }
            UIFactory.Button("Preflight", _console, "RUN PREFLIGHT", new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-290, -96), new Vector2(-20, -20), RunPreflight, ButtonStyle.Confirm);
            UIFactory.Label("Line", _console, "Launch needs 70% or better.", 15, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-290, -130), new Vector2(-20, -104), Palette.TextDim).Mono();
        }

        static Text Record(RectTransform win, float y, string title)
        {
            var card = UIFactory.Round("Rec", win, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, y - 226), new Vector2(-20, y), Palette.Inset, 8);
            card.raycastTarget = false;
            UIFactory.Label("T", card.rectTransform, title, 13, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(16, -30), new Vector2(-16, -10), Palette.Accent).Mono();
            return UIFactory.Label("Body", card.rectTransform, "", 19, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(16, 12), new Vector2(-16, -36));
        }

        // =============================================================== flow

        void Update()
        {
            var mm = MissionManager.Instance;
            if (!_shown && (mm == null || mm.Current != MissionIds.Boot))
            {
                _shown = true;
                if (mm != null && mm.pipUI != null) mm.pipUI.SetBottom(24 + UIFactory.ConsoleH + 16);
            }
        }

        PlannerPanel Planner()
        {
            if (_planner != null) return _planner;
            var prefab = Resources.Load<GameObject>("LastSignal/PlannerPanel");
            var go = prefab != null ? Instantiate(prefab) : new GameObject("Planner");
            go.transform.SetParent(transform, false);
            _planner = go.GetComponent<PlannerPanel>();
            if (_planner == null) _planner = go.AddComponent<PlannerPanel>();
            _planner.Build();
            _planner.PlanApplied += OnPlanApplied;
            return _planner;
        }

        void OpenPlanner()
        {
            if (_busy) return;
            var p = Planner();
            if (_applied != null) p.LoadPlan(_applied);
            p.SetVisible(true);
        }

        void OpenSonsAssessment()
        {
            if (_busy) return;
            var p = Planner();
            p.LoadSonsPlan();
            p.SetVisible(true);
            if (!_sonRecord)
            {
                _sonRecord = true;
                Sfx.Play(Sfx.Id.Beep);
                Say("Five untested subsystems, one of each. Sixty-three in a hundred, five times in a row. One flight in ten comes home.");
            }
            Refresh();
        }

        void OnPlanApplied(PlanConfig plan)
        {
            _applied = plan;
            for (int i = 0; i < _checks.Length; i++) _checks[i] = Result.Pending;
            float p = RiskModel.ComputeSuccess(plan);
            if (p >= 0.70f)
            {
                _fatherRecord = true;
                Say("Seventy percent or better. Now prove it: run every check against this plan.");
            }
            else Say("That plan is under the seventy percent line. Preflight will not clear it.");
            _detail.text = "New plan on file: " + (p * 100f).ToString("0.0") + "%. All checks reset to pending.";
            if (_rig != null) _rig.GoTo("ship");
            Refresh();
        }

        Subsystem Sub(string name) => _applied?.subsystems.Find(s => s != null && s.name == name);

        IEnumerator RunCheck(int i)
        {
            if (_busy) yield break;
            if (_applied == null)
            {
                Sfx.Play(Sfx.Id.Deny);
                _detail.text = "There is no plan on file. Open the Mission Planner and apply one first.";
                Say("We cannot test a ship that has no plan. Open the planner first.");
                yield break;
            }
            _busy = true;
            SetButtons(false);
            UIFactory.SetChip(_chips[i], "TESTING", Palette.Accent);
            if (_rig != null) _rig.GoTo(i == 3 ? "comms" : "ship", 1f);

            if (i < 3)
            {
                var s = Sub(CheckNames[i]);
                float works = s == null ? 0f : 1f - Mathf.Pow(1f - s.reliability, s.redundancy);
                for (int k = 0; k < 4; k++) { Sfx.Play(Sfx.Id.Beep, 0.6f, 0.9f + k * 0.08f); yield return new WaitForSeconds(0.3f); }
                bool pass = works >= 0.895f;
                _checks[i] = pass ? Result.Pass : Result.Fail;
                _detail.text = CheckNames[i].ToUpperInvariant() + ": " + (pass
                    ? "PASS. Works " + (works * 100f).ToString("0.0") + "% of the time on this plan."
                    : "FAIL. Works only " + (works * 100f).ToString("0.0") + "% of the time. Use tested equipment or add a backup unit, then apply the plan again.");
                Sfx.Play(pass ? Sfx.Id.Confirm : Sfx.Id.Fail);
            }
            else
            {
                // The emergency test: cut the long-range radio and see whether the capsule keeps talking.
                var comms = Sub("Comms");
                bool backup = comms != null && comms.redundancy >= 2;
                _detail.text = "Cutting the long-range radio...";
                Sfx.Play(Sfx.Id.Deny);
                if (_radioLive != null) _radioLive.gameObject.SetActive(false);
                if (_rig != null) _rig.Shake(0.08f, 0.5f);
                yield return new WaitForSeconds(1.6f);
                if (backup && _radioLive != null) _radioLive.gameObject.SetActive(true);
                _checks[3] = backup ? Result.Pass : Result.Fail;
                float supplies = MissionManager.Instance != null ? MissionManager.Instance.state.supplies : 100f;
                _detail.text = backup
                    ? "PASS. The backup radio picked up the link in 1.6 s. Sealed supplies retained: " + supplies.ToString("0") + "%."
                    : "FAIL. No backup radio: the capsule went silent. Sealed supplies retained, but nobody can hear it.";
                Sfx.Play(backup ? Sfx.Id.Confirm : Sfx.Id.Fail);
                if (!backup) Say("Silence. That is what the son's ship sounded like. Give the comms a second unit.");
                if (_radioLive != null && !backup) StartCoroutine(RestoreRadioLater());
            }
            _busy = false;
            SetButtons(true);
            Refresh();
        }

        IEnumerator RestoreRadioLater()
        {
            yield return new WaitForSeconds(2.5f);
            if (_radioLive != null) _radioLive.gameObject.SetActive(true);
        }

        void RunPreflight()
        {
            if (_busy) return;
            string problem = null;
            float p = _applied != null ? RiskModel.ComputeSuccess(_applied) : 0f;
            if (!_sonRecord) problem = "The son's original assessment is not in the records yet.";
            else if (_applied == null || p < 0.70f) problem = "The plan on file is " + (p * 100f).ToString("0.0") + "%. Preflight needs 70%.";
            else
                for (int i = 0; i < _checks.Length; i++)
                    if (_checks[i] != Result.Pass)
                    {
                        problem = CheckNames[i] + (_checks[i] == Result.Fail ? " failed its check. Go back to that system, fix the plan and retest." : " has not been checked yet.");
                        break;
                    }

            if (problem != null)
            {
                Sfx.Play(Sfx.Id.Fail);
                _detail.text = "PREFLIGHT HOLD: " + problem;
                Say(problem);
                return;
            }
            _fatherRecord = true;
            Sfx.Play(Sfx.Id.Success);
            if (_planner != null) _planner.SetVisible(false);
            MissionEvents.RaiseComplete(MissionIds.M2);
        }

        void SetButtons(bool on) { foreach (var b in _runBtns) if (b != null) b.interactable = on; }

        void Say(string line)
        {
            var pip = MissionManager.Instance != null ? MissionManager.Instance.pipUI : null;
            if (pip != null) pip.Say(line);
        }

        void Refresh()
        {
            if (_detail == null) return;
            bool allPass = true;
            for (int i = 0; i < 4; i++)
            {
                var r = _checks[i];
                UIFactory.SetChip(_chips[i], r == Result.Pass ? "PASS" : r == Result.Fail ? "FAIL" : "PENDING",
                    r == Result.Pass ? Palette.Good : r == Result.Fail ? Palette.Bad : Palette.TextDim);
                if (r != Result.Pass) allPass = false;
                if (CheckLamps[i] != null && _systems != null)
                {
                    var lamp = _systems.Find(CheckLamps[i]);
                    if (lamp != null)
                    {
                        lamp.Find("Pending").gameObject.SetActive(r != Result.Pass);
                        lamp.Find("Pass").gameObject.SetActive(r == Result.Pass);
                    }
                }
            }

            float son = RiskModel.ComputeSuccess(PlanConfig.SonsOriginalPlan());
            _recSon.text = _sonRecord
                ? "Success probability: <color=#FF5A4D><b>" + (son * 100f).ToString("0.0") + "%</b></color>\n" +
                  "Five untested subsystems, no redundancy.\n<color=#FFB938>Status: the number was lost between the engineering " +
                  "assessment and the pilot's briefing. A reporting gap. He never saw it.</color>"
                : "<color=#8FA6BA>Sealed. Open it to read what the engineers knew.</color>";
            _sonBtn.gameObject.SetActive(!_sonRecord);

            float mine = _applied != null ? RiskModel.ComputeSuccess(_applied) : 0f;
            _recFather.text = _applied == null
                ? "<color=#8FA6BA>No plan applied yet.</color>"
                : "Success probability: <color=" + (mine >= 0.7f ? "#4DE38A" : "#FFB938") + "><b>" + (mine * 100f).ToString("0.0") + "%</b></color>\n" +
                  "Mass " + _applied.TotalMass().ToString("0.0") + " t.  Comms units: " + (Sub("Comms")?.redundancy ?? 0) +
                  ".\nThis number goes on the crew's briefing. Both records are saved.";

            SetObjective(0, _sonRecord);
            SetObjective(1, _applied != null && mine >= 0.70f);
            SetObjective(2, allPass);
        }

        void SetObjective(int i, bool done)
        {
            UIFactory.SetChip(_objChips[i], done ? "DONE" : "OPEN", done ? Palette.Good : Palette.TextDim);
            _objText[i].color = done ? Palette.TextDim : Palette.Text;
        }
    }
}
