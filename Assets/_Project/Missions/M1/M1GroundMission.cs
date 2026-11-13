using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// Mission 1, "What Fell to Earth". PLAN: build a test frame on the rocket sled inside a mass budget
    /// (a = F / m, live). RUN: test fire down the rail; overweight, under-powered or unclamped frames fail
    /// and return to the bench with a Pip hint. Then inspect the recovered fragment for its serial number
    /// and pull the capsule's last recorded heading from the test archive. The recovery report is empty.
    /// All part masses and thrusts are game-balance values (see M1Parts).
    /// </summary>
    public class M1GroundMission : MonoBehaviour
    {
        enum Mode { Bench, Running, Fragment, Archive }

        // ---- state
        Mode _mode = Mode.Bench;
        int _engine = -1;
        readonly bool[] _payload = new bool[M1Parts.Payloads.Length];
        readonly HashSet<string> _secured = new HashSet<string>();
        readonly bool[] _clue = new bool[3];
        bool _validRun, _headingPulled, _shown;
        int _attempts;

        // ---- scene
        CameraRig _rig;
        Transform _sled, _modules, _fragment;
        Vector3 _sledHome;

        // ---- UI
        Canvas _canvas;
        RectTransform _root, _left, _right, _console, _fragWin, _archWin;
        readonly List<Image> _engineCards = new List<Image>();
        readonly List<Image> _payloadChecks = new List<Image>();
        Text _massValue, _thrustValue, _accelValue, _formula, _margin, _clampText, _status, _run;
        Image _massFill, _accelFill;
        Text[] _objChips = new Text[3];
        Text[] _objText = new Text[3];
        Button _fireBtn, _secureBtn;
        Text _fragText, _archText;
        Button _archQuery, _archComplete;
        readonly RectTransform[] _markers = new RectTransform[3];
        Transform[] _hot = new Transform[3];

        static readonly string[] ClueNames = { "Scorched outer skin", "Separation bolt", "Data plate" };
        static readonly string[] ClueText =
        {
            "Heat scoring on the convex side only. This panel was on the OUTSIDE of the vehicle.",
            "The pyro bolt fired cleanly. The section separated on ascent, as designed.",
            "Stamped data plate: <b>LK-2590-041</b>. Survey flight 041. His capsule.",
        };

        void Start()
        {
            var stub = FindFirstObjectByType<StubMission>();
            if (stub != null) Destroy(stub.gameObject);

            var cam = Camera.main;
            _rig = cam != null ? cam.GetComponent<CameraRig>() : null;
            var stage = GameObject.Find("M1 Stage");
            if (stage != null)
            {
                _sled = stage.transform.Find("Sled");
                _modules = _sled != null ? _sled.Find("Modules") : null;
                _fragment = stage.transform.Find("FragmentBench/Fragment");
            }
            else Debug.LogWarning("[M1] 'M1 Stage' not found. Run LastSignal > Setup > Build M1 and M2 scenes.");
            if (_sled != null) _sledHome = _sled.position;
            if (_fragment != null)
            {
                _hot[0] = _fragment.Find("Hot_Edge");
                _hot[1] = _fragment.Find("Hot_Bolt");
                _hot[2] = _fragment.Find("Hot_Bracket");
            }

            BuildUI();
            SelectEngine(1); // start on the balanced engine with only the required payload
            for (int i = 0; i < _payload.Length; i++) _payload[i] = M1Parts.Payloads[i].required;
            ApplyModules();
            Refresh();
        }

        // =============================================================== UI construction

        void BuildUI()
        {
            _canvas = UIFactory.CreateCanvas("M1 Ground Test", 20);
            _canvas.transform.SetParent(transform, false);
            _root = (RectTransform)_canvas.transform;
            float top = -(UIFactory.TopBar + UIFactory.Margin);

            // ---- left: the bench (PLAN)
            _left = UIFactory.Window("Bench", _root, new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(24, 24), new Vector2(24 + UIFactory.ColumnW, top), "TEST FRAME BENCH", "PLAN");
            Section(_left, "ENGINE", -72);
            for (int i = 0; i < M1Parts.Engines.Length; i++)
            {
                int k = i;
                var p = M1Parts.Engines[i];
                float y = -98 - i * 70;
                var b = UIFactory.Button("Engine_" + p.id, _left, "", new Vector2(0, 1), new Vector2(1, 1),
                    new Vector2(20, y - 62), new Vector2(-20, y), () => { SelectEngine(k); Sfx.Play(Sfx.Id.Snap); }, ButtonStyle.Ghost);
                _engineCards.Add((Image)b.targetGraphic);
                var rt = (RectTransform)b.transform;
                UIFactory.Label("Name", rt, p.name, 22, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-10, -8)).Bold();
                UIFactory.Label("Note", rt, p.note, 15, TextAnchor.LowerLeft, Vector2.zero, Vector2.one, new Vector2(16, 8), new Vector2(-10, 0), Palette.TextDim).Mono();
                UIFactory.Label("Spec", rt, (p.thrustN / 1000f).ToString("0.0") + " kN\n" + p.massKg.ToString("0") + " kg", 17,
                    TextAnchor.MiddleRight, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-16, 0), Palette.Text).Mono();
            }

            Section(_left, "PAYLOAD", -322);
            for (int i = 0; i < M1Parts.Payloads.Length; i++)
            {
                int k = i;
                var p = M1Parts.Payloads[i];
                float y = -348 - i * 60;
                var b = UIFactory.Button("Payload_" + p.id, _left, "", new Vector2(0, 1), new Vector2(1, 1),
                    new Vector2(20, y - 52), new Vector2(-20, y), () => TogglePayload(k), ButtonStyle.Ghost);
                var rt = (RectTransform)b.transform;
                var box = UIFactory.Round("Check", rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, -10), new Vector2(34, 10), Palette.Accent, 4);
                box.raycastTarget = false;
                _payloadChecks.Add(box);
                UIFactory.Label("Name", rt, p.name + (p.required ? "  <color=#FFB938>*</color>" : ""), 20, TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(48, 0), new Vector2(-80, -6)).Bold();
                UIFactory.Label("Note", rt, p.note, 14, TextAnchor.LowerLeft, Vector2.zero, Vector2.one, new Vector2(48, 6), new Vector2(-80, 0), Palette.TextDim).Mono();
                UIFactory.Label("Mass", rt, p.massKg.ToString("0") + " kg", 17, TextAnchor.MiddleRight,
                    Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-14, 0)).Mono();
            }
            UIFactory.Label("Req", _left, "<color=#FFB938>*</color> required for this test", 15, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -680), new Vector2(-20, -656), Palette.TextDim).Mono();

            _clampText = UIFactory.Label("Clamps", _left, "", 18, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(22, 96), new Vector2(-20, 126), Palette.TextDim).Mono();
            _secureBtn = UIFactory.Button("Secure", _left, "LOCK ALL CLAMPS", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(20, 22), new Vector2(-20, 86), SecureAll);
            UIFactory.Label("Balance", _left, "Masses and thrusts: game-balance values, not NASA data.", 13, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(22, 130), new Vector2(-20, 152), Palette.A(Palette.TextDim, 0.8f)).Mono();

            // ---- right: live telemetry
            _right = UIFactory.Window("Telemetry", _root, new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-24 - UIFactory.ColumnW, 24), new Vector2(-24, top), "TELEMETRY", "a = F / m");
            Section(_right, "TOTAL MASS", -72);
            _massValue = Big(_right, -96, 40);
            _massFill = UIFactory.Bar("MassBar", _right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -168), new Vector2(-20, -152), Palette.Accent);
            _margin = UIFactory.Label("Margin", _right, "", 16, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(22, -198), new Vector2(-20, -174), Palette.TextDim).Mono();

            Section(_right, "THRUST", -222);
            _thrustValue = Big(_right, -246, 40);

            Section(_right, "ACCELERATION", -322);
            _accelValue = Big(_right, -346, 64);
            _accelFill = UIFactory.Bar("AccelBar", _right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -448), new Vector2(-20, -432), Palette.Good);
            // Target line at 16 of a 24 m/s2 scale.
            var line = UIFactory.Box("Target", _accelFill.transform.parent, new Vector2(16f / 24f, 0), new Vector2(16f / 24f, 1),
                new Vector2(-1.5f, -6), new Vector2(1.5f, 6), Palette.Warn);
            line.GetComponent<Image>().raycastTarget = false;
            UIFactory.Label("TargetL", _right, "target " + M1Parts.TargetAccel.ToString("0") + " m/s²", 15, TextAnchor.UpperRight,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -476), new Vector2(-20, -452), Palette.Warn).Mono();

            var fbox = UIFactory.Round("FormulaBox", _right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -590), new Vector2(-20, -494), Palette.Inset, 8);
            fbox.raycastTarget = false;
            UIFactory.Label("FormulaL", fbox.rectTransform, "NEWTON'S SECOND LAW", 14, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -28), new Vector2(-16, -8), Palette.Accent).Mono();
            _formula = UIFactory.Label("Formula", fbox.rectTransform, "", 22, TextAnchor.LowerLeft,
                Vector2.zero, Vector2.one, new Vector2(16, 10), new Vector2(-16, -30)).Mono();

            Section(_right, "LIVE RUN", -620);
            _run = UIFactory.Label("Run", _right, "Standing by on the rail.", 20, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -760), new Vector2(-20, -644)).Mono();

            // ---- bottom centre: objectives and actions (RUN)
            _console = UIFactory.Window("Console", _root, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-460, 24), new Vector2(460, 24 + UIFactory.ConsoleH));
            string[] obj =
            {
                "Pass a valid test fire  (16 m/s², max 270 kg, clamps locked)",
                "Identify the recovered fragment  (3 clues)",
                "Pull the capsule's last heading from the archive",
            };
            for (int i = 0; i < 3; i++)
            {
                float y = -22 - i * 50;
                _objChips[i] = UIFactory.Chip("Chip" + i, _console, "", Palette.TextDim, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(20, y - 34), new Vector2(110, y));
                _objText[i] = UIFactory.Label("Obj" + i, _console, obj[i], 19, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(122, y - 34), new Vector2(600, y));
            }
            _fireBtn = UIFactory.Button("Fire", _console, "TEST FIRE", new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-290, -82), new Vector2(-20, -20), () => StartCoroutine(TestFire()), ButtonStyle.Danger);
            UIFactory.Button("Fragment", _console, "INSPECT FRAGMENT", new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-290, -130), new Vector2(-20, -90), () => SetMode(Mode.Fragment), ButtonStyle.Ghost);
            UIFactory.Button("Archive", _console, "OPEN ARCHIVE", new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-290, -176), new Vector2(-20, -136), () => SetMode(Mode.Archive), ButtonStyle.Ghost);
            _status = UIFactory.Label("Status", _console, "", 15, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(22, 8), new Vector2(-310, 30), Palette.TextDim).Mono();

            // ---- fragment window
            _fragWin = UIFactory.Window("FragmentWin", _root, new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-24 - UIFactory.ColumnW, 24), new Vector2(-24, top), "RECOVERED FRAGMENT", "INSPECT");
            UIFactory.Label("Intro", _fragWin, "Only a fragment of the launch vehicle came home. Click the three markers on the part.", 20,
                TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -150), new Vector2(-20, -70), Palette.TextDim);
            _fragText = UIFactory.Label("Clues", _fragWin, "", 21, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(22, 110), new Vector2(-20, -160));
            _fragText.lineSpacing = 1.1f;
            UIFactory.Button("Back", _fragWin, "BACK TO THE BENCH", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(20, 22), new Vector2(-20, 86), () => SetMode(Mode.Bench), ButtonStyle.Ghost);
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var m = UIFactory.Rect("Marker" + i, _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-26, -26), new Vector2(26, 26));
                var ring = m.gameObject.AddComponent<Image>();
                ring.sprite = UISkin.Ring(0.2f);
                ring.color = Palette.Accent;
                var b = m.gameObject.AddComponent<Button>();
                b.targetGraphic = ring;
                b.onClick.AddListener(() => Inspect(k));
                m.gameObject.AddComponent<UIButtonFx>();
                var dot = UIFactory.Round("Dot", m, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6, -6), new Vector2(6, 6), Palette.Accent, 6);
                dot.raycastTarget = false;
                UIFactory.Label("N", m, (i + 1).ToString(), 15, TextAnchor.MiddleCenter, new Vector2(1, 1), new Vector2(1, 1),
                    new Vector2(-4, -4), new Vector2(18, 18), Palette.Text).Mono();
                _markers[i] = m;
            }

            // ---- archive window
            _archWin = UIFactory.Window("ArchiveWin", _root, new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-24 - UIFactory.ColumnW, 24), new Vector2(-24, top), "GROUND-TEST ARCHIVE", "RECORDS");
            _archText = UIFactory.Label("Records", _archWin, "", 19, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(22, 250), new Vector2(-20, -72));
            _archText.font = UIFactory.FontMono;
            _archText.lineSpacing = 1.15f;
            _archQuery = UIFactory.Button("Query", _archWin, "QUERY SERIAL LK-2590-041", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(20, 168), new Vector2(-20, 232), PullHeading);
            _archComplete = UIFactory.Button("Complete", _archWin, "FILE FINDINGS · END MISSION", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(20, 94), new Vector2(-20, 158), Complete, ButtonStyle.Confirm);
            UIFactory.Button("Back", _archWin, "BACK TO THE BENCH", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(20, 22), new Vector2(-20, 84), () => SetMode(Mode.Bench), ButtonStyle.Ghost);

            SetMode(Mode.Bench, false);
        }

        static void Section(RectTransform win, string text, float y)
        {
            UIFactory.Label("S_" + text, win, text, 14, TextAnchor.LowerLeft, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(22, y - 20), new Vector2(-20, y), Palette.TextDim).Mono();
        }

        static Text Big(RectTransform win, float y, int size)
        {
            var t = UIFactory.Label("Big", win, "", size, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(20, y - size - 8), new Vector2(-20, y));
            return t.Bold();
        }

        // =============================================================== bench logic

        float Mass()
        {
            float m = M1Parts.FrameMassKg;
            if (_engine >= 0) m += M1Parts.Engines[_engine].massKg;
            for (int i = 0; i < _payload.Length; i++) if (_payload[i]) m += M1Parts.Payloads[i].massKg;
            return m;
        }

        float Thrust() => _engine >= 0 ? M1Parts.Engines[_engine].thrustN : 0f;

        IEnumerable<string> Installed()
        {
            if (_engine >= 0) yield return M1Parts.Engines[_engine].id;
            for (int i = 0; i < _payload.Length; i++) if (_payload[i]) yield return M1Parts.Payloads[i].id;
        }

        string FirstUnsecured()
        {
            foreach (var id in Installed()) if (!_secured.Contains(id)) return id;
            return null;
        }

        void SelectEngine(int i)
        {
            if (_mode != Mode.Bench) return;
            _engine = i;
            ApplyModules();
            Refresh();
        }

        void TogglePayload(int i)
        {
            if (_mode != Mode.Bench) return;
            _payload[i] = !_payload[i];
            Sfx.Play(_payload[i] ? Sfx.Id.Snap : Sfx.Id.Whoosh, _payload[i] ? 1f : 0.5f);
            ApplyModules();
            Refresh();
        }

        void SecureAll()
        {
            if (_mode != Mode.Bench) return;
            foreach (var id in Installed()) _secured.Add(id);
            Sfx.Play(Sfx.Id.Lock);
            ApplyModules();
            Refresh();
        }

        void ApplyModules()
        {
            if (_modules == null) return;
            var on = new HashSet<string>(Installed());
            foreach (Transform m in _modules)
            {
                string id = m.name.Replace("Mod_", "");
                bool active = on.Contains(id);
                if (m.gameObject.activeSelf != active) m.gameObject.SetActive(active);
                if (!active) _secured.Remove(id); // pulling a part off unlocks its clamp
                bool locked = _secured.Contains(id);
                var red = m.Find("LedRed"); var green = m.Find("LedGreen");
                if (red != null) red.gameObject.SetActive(!locked);
                if (green != null) green.gameObject.SetActive(locked);
            }
        }

        void Refresh()
        {
            if (_massValue == null) return;
            float m = Mass(), f = Thrust(), a = f > 0f ? f / m : 0f;
            bool over = m > M1Parts.MassBudgetKg;

            for (int i = 0; i < _engineCards.Count; i++)
            {
                bool sel = i == _engine;
                _engineCards[i].color = sel ? Palette.A(Palette.Accent, 0.22f) : Palette.ButtonGhost;
                var edge = _engineCards[i].transform.Find("Edge");
                if (edge != null) edge.GetComponent<Image>().color = sel ? Palette.Accent : Palette.A(Palette.Text, 0.22f);
            }
            for (int i = 0; i < _payloadChecks.Count; i++)
                _payloadChecks[i].color = _payload[i] ? Palette.Accent : Palette.A(Palette.Text, 0.14f);

            _massValue.text = m.ToString("0") + " <size=24><color=#8FA6BA>/ " + M1Parts.MassBudgetKg.ToString("0") + " kg</color></size>";
            _massValue.color = over ? Palette.Bad : Palette.Text;
            _massFill.fillAmount = Mathf.Clamp01(m / M1Parts.MassBudgetKg);
            _massFill.color = over ? Palette.Bad : Palette.Accent;
            _margin.text = over
                ? "OVER BUDGET by " + (m - M1Parts.MassBudgetKg).ToString("0") + " kg: the rail brake cannot stop it"
                : "Margin " + (M1Parts.MassBudgetKg - m).ToString("0") + " kg  ·  frame alone " + M1Parts.FrameMassKg.ToString("0") + " kg";

            _thrustValue.text = (f / 1000f).ToString("0.0") + " <size=24><color=#8FA6BA>kN</color></size>";
            bool fast = a >= M1Parts.TargetAccel;
            _accelValue.text = a.ToString("0.0") + " <size=30><color=#8FA6BA>m/s²</color></size>";
            _accelValue.color = fast ? Palette.Good : Palette.Warn;
            _accelFill.fillAmount = Mathf.Clamp01(a / 24f);
            _accelFill.color = fast ? Palette.Good : Palette.Warn;
            _formula.text = "a = " + f.ToString("0") + " N ÷ " + m.ToString("0") + " kg = " + a.ToString("0.0");

            int total = 0, locked = 0;
            foreach (var id in Installed()) { total++; if (_secured.Contains(id)) locked++; }
            _clampText.text = "Clamps locked: " + locked + " / " + total;
            _clampText.color = locked == total ? Palette.Good : Palette.Warn;

            bool fragDone = _clue[0] && _clue[1] && _clue[2];
            SetObjective(0, _validRun);
            SetObjective(1, fragDone);
            SetObjective(2, _headingPulled);
            _status.text = _attempts == 0 ? "Bunker: range is clear." : "Test fires so far: " + _attempts;
        }

        void SetObjective(int i, bool done)
        {
            UIFactory.SetChip(_objChips[i], done ? "DONE" : "OPEN", done ? Palette.Good : Palette.TextDim);
            _objText[i].color = done ? Palette.TextDim : Palette.Text;
        }

        // =============================================================== modes

        void SetMode(Mode mode, bool sound = true)
        {
            if (_mode == Mode.Running) return;
            _mode = mode;
            if (sound) Sfx.Play(Sfx.Id.Whoosh, 0.4f);
            bool bench = mode == Mode.Bench;
            _left.gameObject.SetActive(bench);
            _right.gameObject.SetActive(bench);
            _console.gameObject.SetActive(bench);
            _fragWin.gameObject.SetActive(mode == Mode.Fragment);
            _archWin.gameObject.SetActive(mode == Mode.Archive);
            if (_rig != null && _shown) _rig.GoTo(mode == Mode.Fragment ? "fragment" : mode == Mode.Archive ? "archive" : "bench");
            if (mode == Mode.Fragment) RefreshFragment();
            if (mode == Mode.Archive) RefreshArchive();
            Refresh();
        }

        void Update()
        {
            // Stay hidden behind the title screen; arrive on the bench when the run begins.
            var mm = MissionManager.Instance;
            bool live = mm == null || mm.Current != MissionIds.Boot;
            if (_canvas != null) _canvas.enabled = live;
            if (live && !_shown)
            {
                _shown = true;
                if (_rig != null) _rig.GoTo("bench", 2.2f);
                if (mm != null && mm.pipUI != null) mm.pipUI.SetBottom(24 + UIFactory.ConsoleH + 16);
            }
            for (int i = 0; i < 3; i++) PlaceMarker(i);
        }

        void PlaceMarker(int i)
        {
            var m = _markers[i];
            if (m == null) return;
            var cam = Camera.main;
            bool show = _mode == Mode.Fragment && _canvas.enabled && cam != null && _hot[i] != null;
            Vector3 sp = show ? cam.WorldToScreenPoint(_hot[i].position) : Vector3.zero;
            show &= sp.z > 0f;
            if (m.gameObject.activeSelf != show) m.gameObject.SetActive(show);
            if (!show) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, sp, null, out var local);
            m.anchoredPosition = local;
            float pulse = _clue[i] ? 1f : 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 5f + i);
            m.localScale = new Vector3(pulse, pulse, 1f);
            m.GetComponent<Image>().color = _clue[i] ? Palette.Good : Palette.Accent;
        }

        // =============================================================== fragment

        void Inspect(int i)
        {
            if (_clue[i]) return;
            _clue[i] = true;
            Sfx.Play(Sfx.Id.Beep);
            RefreshFragment();
            Refresh();
            var pip = MissionManager.Instance != null ? MissionManager.Instance.pipUI : null;
            if (_clue[0] && _clue[1] && _clue[2] && pip != null)
                pip.Say("External section, separated on ascent. Serial LK-2590-041. The archive will know where the capsule went.");
        }

        void RefreshFragment()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                sb.Append(_clue[i] ? "<color=#4DE38A>" : "<color=#8FA6BA>").Append(i + 1).Append("  ").Append(ClueNames[i].ToUpperInvariant()).AppendLine("</color>");
                sb.AppendLine(_clue[i] ? ClueText[i] : "Not inspected yet.").AppendLine();
            }
            if (_clue[0] && _clue[1] && _clue[2])
                sb.Append("<color=#FFB938><b>Finding:</b> an external section that separated on ascent. The capsule flew on without it.</color>");
            _fragText.text = sb.ToString();
        }

        // =============================================================== archive

        void RefreshArchive()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#8FA6BA>RANGE 7 · SLED RUNS</color>");
            sb.AppendLine(_validRun
                ? "RUN 0" + (411 + _attempts) + "   <color=#4DE38A>VALID</color>   archive unlocked"
                : "RUN ----   <color=#FF5A4D>NO VALID RUN</color>   archive locked");
            sb.AppendLine();
            sb.AppendLine("<color=#8FA6BA>CAPSULE RECORDS</color>");
            if (!_headingPulled)
            {
                sb.AppendLine(!_validRun ? "Locked until a valid test fire is logged."
                    : !(_clue[2]) ? "Enter a serial number. Find it on the fragment."
                    : "Ready to query LK-2590-041.");
            }
            else
            {
                sb.AppendLine("SERIAL     LK-2590-041");
                sb.AppendLine("FLIGHT     SURVEY 041");
                sb.AppendLine("LAST FIX   T+00:41:07");
                sb.AppendLine("HEADING    <color=#3FD0FF>RA 07h 41m  DEC +24° 11'</color>");
                sb.AppendLine("           (outbound, past the orbit of Mars)");
                sb.AppendLine();
                sb.AppendLine("<color=#8FA6BA>RECOVERY REPORT</color>");
                sb.AppendLine("<color=#FF5A4D>EMPTY · 0 PAGES · NO SEARCH FILED</color>");
            }
            _archText.text = sb.ToString();
            _archQuery.interactable = _validRun && _clue[2] && !_headingPulled;
            _archQuery.gameObject.SetActive(!_headingPulled);
            _archComplete.gameObject.SetActive(_headingPulled);
        }

        void PullHeading()
        {
            if (!(_validRun && _clue[2])) return;
            _headingPulled = true;
            if (MissionManager.Instance != null) MissionManager.Instance.state.heading = new Vector3(.42f, .18f, .89f);
            Sfx.Play(Sfx.Id.Confirm);
            RefreshArchive();
            Refresh();
            var pip = MissionManager.Instance != null ? MissionManager.Instance.pipUI : null;
            if (pip != null) pip.Say("The heading is real. The recovery report is empty. Nobody went looking for him.");
        }

        void Complete()
        {
            if (!_headingPulled) return;
            MissionEvents.RaiseComplete(MissionIds.M1);
        }

        // =============================================================== test fire

        IEnumerator TestFire()
        {
            if (_mode != Mode.Bench) yield break;
            var pip = MissionManager.Instance != null ? MissionManager.Instance.pipUI : null;
            if (_engine < 0) { Sfx.Play(Sfx.Id.Deny); yield break; }

            float m = Mass(), f = Thrust(), a = f / m;
            string loose = FirstUnsecured();
            bool over = m > M1Parts.MassBudgetKg;
            bool slow = a < M1Parts.TargetAccel;
            bool missing = false;
            for (int i = 0; i < _payload.Length; i++) if (M1Parts.Payloads[i].required && !_payload[i]) missing = true;

            _mode = Mode.Running;
            _attempts++;
            _left.gameObject.SetActive(false);
            _console.gameObject.SetActive(false);
            if (_rig != null) { _rig.GoTo("run", 0.9f); }
            for (int c = 3; c > 0; c--)
            {
                _run.text = "T-" + c + "   range clear";
                Sfx.Play(Sfx.Id.Beep, 1f, c == 1 ? 1.25f : 1f);
                yield return new WaitForSeconds(0.55f);
            }

            Transform eng = _modules != null ? _modules.Find("Mod_" + M1Parts.Engines[_engine].id) : null;
            Transform plume = eng != null ? eng.Find("Plume") : null;
            if (plume != null) plume.gameObject.SetActive(true);
            var loop = Sfx.Loop(Sfx.Id.Engine, 0.7f);
            Sfx.Play(Sfx.Id.Whoosh);
            if (_rig != null) { _rig.Follow(_sled); _rig.Shake(0.05f, 2.6f); }

            // Simulate a = F / m on the rail. Distance is scaled so every run stays on the track.
            const float burn = 2.6f, scale = 0.3f;
            Transform dropped = null; Vector3 dropFrom = Vector3.zero; Quaternion dropRot = Quaternion.identity;
            float t = 0f, x = 0f, v = 0f;
            string failure = null;
            while (t < burn)
            {
                t += Time.deltaTime;
                v = a * t;
                x = 0.5f * a * t * t;
                if (_sled != null) _sled.position = _sledHome + Vector3.right * (x * scale);
                if (plume != null) plume.localScale = Vector3.one * (0.9f + 0.2f * Mathf.PerlinNoise(Time.time * 30f, 0f));
                _run.text = "T+" + t.ToString("0.00") + " s\nv  " + v.ToString("0.0") + " m/s\nx  " + x.ToString("0.0") + " m\na  " + a.ToString("0.0") + " m/s²";

                if (loose != null && dropped == null && t > 0.7f && _modules != null)
                {
                    dropped = _modules.Find("Mod_" + loose);
                    if (dropped != null)
                    {
                        dropFrom = dropped.localPosition; dropRot = dropped.localRotation;
                        dropped.SetParent(_sled.parent, true);
                        Sfx.Play(Sfx.Id.Snap, 1f, 0.7f);
                        if (_rig != null) _rig.Shake(0.18f, 0.6f);
                    }
                    failure = "A module broke loose: the " + NameOf(loose) + " was not clamped.";
                    break;
                }
                yield return null;
            }

            if (plume != null) plume.gameObject.SetActive(false);
            Sfx.StopLoop(loop);

            if (dropped != null) StartCoroutine(Tumble(dropped));
            // Coast and brake.
            float brake = 0f;
            while (brake < 0.9f && failure == null)
            {
                brake += Time.deltaTime;
                float vb = Mathf.Lerp(v, 0f, brake / 0.9f);
                x += vb * Time.deltaTime;
                if (_sled != null) _sled.position = _sledHome + Vector3.right * (x * scale);
                yield return null;
            }
            if (_rig != null) _rig.Follow(null);

            if (failure == null)
            {
                if (missing) failure = "No flight recorder or telemetry on board: the run happened, but nothing was recorded.";
                else if (over) failure = "Brake overrun: " + m.ToString("0") + " kg is more than the " + M1Parts.MassBudgetKg.ToString("0") + " kg the rail brake is rated for.";
                else if (slow) failure = "Too slow: " + a.ToString("0.0") + " m/s² is below the " + M1Parts.TargetAccel.ToString("0") + " m/s² the abort profile needs.";
            }

            if (failure == null)
            {
                _validRun = true;
                _run.text = "<color=#4DE38A>VALID RUN</color>\npeak v " + v.ToString("0.0") + " m/s\nlogged to the archive";
                Sfx.Play(Sfx.Id.Success);
                if (pip != null) pip.Say("Valid run. Same thrust, less mass, more acceleration. The archive is open now.");
            }
            else
            {
                _run.text = "<color=#FF5A4D>RUN FAILED</color>\n" + failure;
                Sfx.Play(Sfx.Id.Fail);
                if (pip != null) pip.Say(HintFor(loose != null, over, slow, missing));
            }

            yield return new WaitForSeconds(1.4f);
            // Recover the sled to the bench.
            if (_rig != null) _rig.GoTo("bench", 1.2f);
            Vector3 from = _sled != null ? _sled.position : Vector3.zero;
            float r = 0f;
            while (r < 1f && _sled != null)
            {
                r += Time.deltaTime / 1.2f;
                _sled.position = Vector3.Lerp(from, _sledHome, Mathf.SmoothStep(0f, 1f, r));
                yield return null;
            }
            if (dropped != null)
            {
                dropped.SetParent(_modules, false);
                dropped.localPosition = dropFrom; dropped.localRotation = dropRot;
            }
            _mode = Mode.Bench;
            SetMode(Mode.Bench, false);
        }

        IEnumerator Tumble(Transform part)
        {
            Vector3 start = part.position;
            Vector3 vel = new Vector3(-2.5f, 2.2f, -1.2f);
            Vector3 spin = new Vector3(260f, 90f, 340f);
            float t = 0f;
            while (t < 1.1f && part != null && part.parent != _modules)
            {
                t += Time.deltaTime;
                vel += Vector3.down * 9.81f * Time.deltaTime;
                Vector3 p = part.position + vel * Time.deltaTime;
                if (p.y < 0.05f) { p.y = 0.05f; vel = Vector3.zero; spin = Vector3.zero; }
                part.position = p;
                part.Rotate(spin * Time.deltaTime, Space.World);
                yield return null;
            }
        }

        static string NameOf(string id)
        {
            foreach (var p in M1Parts.Engines) if (p.id == id) return p.name;
            foreach (var p in M1Parts.Payloads) if (p.id == id) return p.name.ToLowerInvariant();
            return id;
        }

        static string HintFor(bool loose, bool over, bool slow, bool missing)
        {
            if (loose) return "Something was not clamped. Every red light on the frame is a part that will leave without us.";
            if (missing) return "We need the flight recorder and the telemetry pack on board, or the run never happened.";
            if (over) return "Too heavy for the brake. Take off what this test does not need.";
            if (slow) return "Mass fights thrust. Less mass or more thrust, and watch the acceleration number.";
            return "Check the numbers before the next test.";
        }
    }
}
