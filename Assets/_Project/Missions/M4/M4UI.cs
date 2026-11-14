using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.M4
{
    /// <summary>
    /// All M4 screens, code-built with Core's UIFactory: planet scanner, site picker, landing HUD, objective,
    /// interaction prompt, info card, son's transmission log, survey packet + antenna alignment,
    /// letterbox subtitles and a fade. Sorting: 15 (in-world HUD) and 25 (modal panels), 32 (cinematic).
    /// </summary>
    public class M4UI : MonoBehaviour
    {
        static readonly Color Card = new Color(0.07f, 0.11f, 0.17f, 0.96f);
        static readonly Color CardSel = new Color(0.10f, 0.32f, 0.45f, 1f);
        const string Dim = "#8FA7BF";

        Canvas _hud, _modal, _cine;
        RectTransform _scanner, _picker, _landing, _objective, _prompt, _info, _log, _survey;
        Text _scanText, _scanNasa, _pickDetails, _landText, _warn, _objText, _promptText, _infoTitle, _infoBody, _logCount, _logLines;
        Image _fuel, _power, _throttle;
        Button _beginBtn, _infoBtn, _logBtn, _transmitBtn;
        readonly List<Image> _cards = new List<Image>();
        readonly List<Text> _cardTexts = new List<Text>();
        Action<int> _onPick;
        Action _onBegin, _onInfoClose, _onLogDone;
        int _selected = -1;

        // letterbox / fade
        RectTransform _barTop, _barBottom;
        Text _subtitle;
        Image _fade;

        public bool ModalOpen => (_info != null && _info.gameObject.activeSelf) || (_log != null && _log.gameObject.activeSelf)
                                 || (_survey != null && _survey.gameObject.activeSelf) || (_picker != null && _picker.gameObject.activeSelf);

        public static M4UI Create(Transform parent)
        {
            var go = new GameObject("M4 UI");
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<M4UI>();
            ui.Build();
            return ui;
        }

        void Build()
        {
            UIFactory.EnsureEventSystem();
            _hud = UIFactory.CreateCanvas("M4 HUD", 15); _hud.transform.SetParent(transform, false);
            _modal = UIFactory.CreateCanvas("M4 Panels", 25); _modal.transform.SetParent(transform, false);
            _cine = UIFactory.CreateCanvas("M4 Cinematic", 32); _cine.transform.SetParent(transform, false);
            BuildScanner(); BuildPicker(); BuildLanding(); BuildObjective(); BuildPrompt(); BuildInfo(); BuildLog(); BuildSurvey(); BuildCinematic();
            HideAll();
        }

        public void HideAll()
        {
            foreach (var r in new[] { _scanner, _picker, _landing, _objective, _prompt, _info, _log, _survey })
                if (r != null) r.gameObject.SetActive(false);
        }

        static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        // ================================================================== scanner

        void BuildScanner()
        {
            _scanner = UIFactory.Box("Scanner", _hud.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-660, -420), new Vector2(-40, 420), UIFactory.Panel);
            UIFactory.Label("Title", _scanner, "PLANET SCANNER", 30, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -62), new Vector2(-28, -18), UIFactory.Accent);
            _scanText = UIFactory.Label("Body", _scanner, "", 22, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(28, 300), new Vector2(-28, -70));
            _scanNasa = UIFactory.Label("NasaData", _scanner, "", 16, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(28, 104), new Vector2(-28, 296));
            _scanNasa.resizeTextForBestFit = true; _scanNasa.resizeTextMinSize = 11; _scanNasa.resizeTextMaxSize = 16;
            UIFactory.Button("Choose", _scanner, "Choose landing site", new Vector2(0, 0), new Vector2(1, 0), new Vector2(28, 26), new Vector2(-28, 92), () => _onBegin?.Invoke());
        }

        public void ShowScanner(ExoplanetRecord p, string source, Action onChoose)
        {
            _onBegin = onChoose;
            var ci = CultureInfo.InvariantCulture;
            float tC = float.IsNaN(p.eqTempK) ? float.NaN : p.eqTempK - 273.15f;
            var sb = new StringBuilder();
            sb.Append("<b>based on ").Append(p.name).Append("</b>");
            if (!string.IsNullOrEmpty(p.host)) sb.Append($"  <color={Dim}>(orbits {p.host})</color>");
            sb.Append("\n\n");
            Row(sb, "Radius", ExoplanetCsv.Fmt(p.radiusEarth, "0.00", " Earth radii"));
            Row(sb, "Insolation", ExoplanetCsv.Fmt(p.insolation, "0.00", " x Earth"));
            Row(sb, "Equilibrium temp.", ExoplanetCsv.Fmt(p.eqTempK, "0", " K") + (float.IsNaN(tC) ? "" : "  (" + tC.ToString("0", ci) + " C)"));
            Row(sb, "Orbital period", ExoplanetCsv.Fmt(p.periodDays, "0.0", " days"));
            if (!float.IsNaN(p.LightYears)) Row(sb, "Distance", p.LightYears.ToString("#,0", ci) + " light-years");
            sb.Append($"\n<color={Dim}>Surface gravity (model estimate): {p.GravityEstimateG.ToString("0.00", ci)} g\n");
            sb.Append("from radius via the Chen & Kipping 2017 mass-radius relation.\n");
            sb.Append("Source: ").Append(source).Append("</color>\n\n");
            sb.Append("Water signal detected in a valley near a faint repeating beacon.");
            _scanText.text = sb.ToString();
            if (ExoplanetCsv.LastWasFallback)
                _scanNasa.text = "<color=#FFB938><b>FALLBACK DATA, not the verified NASA row.</b> The real file exoplanet_trappist1e.csv could not be read, so these numbers are approximate.</color>";
            else
            {
                NasaData.Mark(ExoplanetCsv.RealFile, MissionIds.M4, p.name + ": radius " + ExoplanetCsv.Fmt(p.radiusEarth, "0.00", " Earth") + ", insolation " +
                    ExoplanetCsv.Fmt(p.insolation, "0.000", " Earth") + ", equilibrium temp. " + ExoplanetCsv.Fmt(p.eqTempK, "0.0", " K"));
                _scanNasa.text = NasaData.Card(ExoplanetCsv.RealFile);
            }
            _scanner.gameObject.SetActive(true);
        }

        static void Row(StringBuilder sb, string k, string v) => sb.Append($"<color={Dim}>{k.PadRight(18, ' ')}</color>  {v}\n");

        public void HideScanner() => _scanner.gameObject.SetActive(false);

        // ================================================================== site picker

        void BuildPicker()
        {
            _picker = UIFactory.Box("SitePicker", _modal.transform, new Vector2(0.07f, 0.13f), new Vector2(0.93f, 0.9f), Vector2.zero, Vector2.zero, UIFactory.Panel);
            UIFactory.Label("Title", _picker, "CHOOSE A LANDING SITE", 34, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(34, -70), new Vector2(-34, -20), UIFactory.Accent);
            UIFactory.Label("Note", _picker, "Orbital scan data. Slope, sunlight and boulders change how hard the landing is. Touchdown limits are game-balance values.", 21,
                TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(34, -108), new Vector2(-34, -72), new Color(0.6f, 0.7f, 0.8f));
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                float x0 = 0.025f + i * 0.325f;
                var b = UIFactory.Button("Site " + i, _picker, "", new Vector2(x0, 0.30f), new Vector2(x0 + 0.30f, 0.84f), Vector2.zero, Vector2.zero, () => Select(idx));
                var img = b.GetComponent<Image>(); img.color = Card;
                var t = b.GetComponentInChildren<Text>();
                t.alignment = TextAnchor.UpperLeft; t.fontSize = 24;
                t.rectTransform.offsetMin = new Vector2(22, 16); t.rectTransform.offsetMax = new Vector2(-22, -18);
                _cards.Add(img); _cardTexts.Add(t);
            }
            _pickDetails = UIFactory.Label("Details", _picker, "Select a site.", 24, TextAnchor.UpperLeft, new Vector2(0.025f, 0.03f), new Vector2(0.7f, 0.28f), Vector2.zero, Vector2.zero);
            _beginBtn = UIFactory.Button("Begin", _picker, "Begin descent", new Vector2(0.72f, 0.05f), new Vector2(0.975f, 0.2f), Vector2.zero, Vector2.zero, () => { if (_selected >= 0) _onPick?.Invoke(_selected); });
            _beginBtn.interactable = false;
        }

        LandingSite[] _sites;

        public void ShowPicker(LandingSite[] sites, Action<int> onPick)
        {
            _sites = sites; _onPick = onPick; _selected = -1; _beginBtn.interactable = false;
            for (int i = 0; i < 3 && i < sites.Length; i++)
            {
                var s = sites[i];
                _cards[i].color = Card;
                _cardTexts[i].text =
                    $"<size=34><b>{s.id}  {s.name}</b></size>\n<color={Dim}>{s.blurb}</color>\n\n" +
                    $"Water signal   {Meter(s.waterSignal)}\n" +
                    $"Slope              {s.slopeDeg:0} deg\n" +
                    $"Solar exposure {Meter(s.solarExposure)}\n" +
                    $"Beacon            {s.beaconDistance:0} m\n" +
                    (s.boulders ? "Boulders         MANY\n" : "Boulders         few\n") +
                    $"\nRisk  <color={Hex(RiskColor(s))}><b>{s.RiskLabel}</b></color>";
            }
            _pickDetails.text = "Select a site to see how it changes the landing.";
            _picker.gameObject.SetActive(true);
        }

        static string Meter(float v)
        {
            int n = Mathf.RoundToInt(Mathf.Clamp01(v) * 5f);
            return "<color=#4DCCFF>" + new string('|', n * 2) + "</color><color=#33475B>" + new string('|', (5 - n) * 2) + "</color>";
        }

        static Color RiskColor(LandingSite s) => s.Risk < 0.3f ? UIFactory.Good : s.Risk < 0.55f ? UIFactory.Warn : UIFactory.Bad;

        void Select(int i)
        {
            _selected = i;
            for (int k = 0; k < _cards.Count; k++) _cards[k].color = k == i ? CardSel : Card;
            var s = _sites[i];
            _pickDetails.text =
                $"<b>Site {s.id} {s.name}</b> changes the landing:\n" +
                $"Safe touchdown  <b>{s.SafeVerticalSpeed:0.0} m/s</b> down, <b>{s.SafeHorizontalSpeed:0.0} m/s</b> sideways   ·   Ellipse <b>{s.EllipseRadius:0} m</b>\n" +
                $"Wind gusts up to <b>{s.WindStrength:0.0} m/s2</b>   ·   Battery drain <b>{s.PowerDrainPerSecond:0.00} %/s</b>" +
                (s.boulders ? "   ·   <color=#FF8080>boulder field</color>" : "") +
                $"\nWalk to the beacon after landing: <b>{s.beaconDistance:0} m</b>";
            _beginBtn.interactable = true;
        }

        public void HidePicker() => _picker.gameObject.SetActive(false);

        // ================================================================== landing HUD

        void BuildLanding()
        {
            _landing = UIFactory.Box("LandingHUD", _hud.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-540, -330), new Vector2(-30, 340), UIFactory.Panel);
            _landText = UIFactory.Label("Readout", _landing, "", 25, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(26, 190), new Vector2(-26, -18));
            UIFactory.Label("FuelL", _landing, "FUEL", 21, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(26, 140), new Vector2(150, 172));
            _fuel = UIFactory.Bar("Fuel", _landing, new Vector2(0, 0), new Vector2(1, 0), new Vector2(160, 144), new Vector2(-26, 168), UIFactory.Accent);
            UIFactory.Label("PowL", _landing, "BATTERY", 21, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(26, 100), new Vector2(150, 132));
            _power = UIFactory.Bar("Power", _landing, new Vector2(0, 0), new Vector2(1, 0), new Vector2(160, 104), new Vector2(-26, 128), UIFactory.Warn);
            UIFactory.Label("ThrL", _landing, "THROTTLE", 21, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(26, 60), new Vector2(150, 92));
            _throttle = UIFactory.Bar("Throttle", _landing, new Vector2(0, 0), new Vector2(1, 0), new Vector2(160, 64), new Vector2(-26, 88), new Color(1f, 0.55f, 0.25f));
            UIFactory.Label("Keys", _landing, "SPACE main engine  ·  WASD translate", 20, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(26, 14), new Vector2(-26, 48), new Color(0.6f, 0.7f, 0.8f));
            _warn = UIFactory.Label("Warning", _hud.transform, "", 34, TextAnchor.MiddleCenter, new Vector2(0.3f, 0.72f), new Vector2(0.7f, 0.79f), Vector2.zero, Vector2.zero, UIFactory.Bad);
        }

        public void ShowLanding(bool on) { _landing.gameObject.SetActive(on); _warn.gameObject.SetActive(on); }

        public void UpdateLanding(LandingController lc)
        {
            var s = lc.Site;
            float vs = lc.Velocity.y, hs = new Vector2(lc.Velocity.x, lc.Velocity.z).magnitude;
            bool vsBad = -vs > s.SafeVerticalSpeed, hsBad = hs > s.SafeHorizontalSpeed;
            string Col(bool bad) => bad ? "#FF6060" : "#66E699";
            var cam = Camera.main != null ? Camera.main.transform : lc.transform;
            string windDir = "";
            if (lc.Wind.sqrMagnitude > 0.0025f)
            {
                var local = cam.InverseTransformDirection(lc.Wind);
                windDir = Mathf.Abs(local.x) > Mathf.Abs(local.z) ? (local.x > 0 ? "pushing right" : "pushing left") : (local.z > 0 ? "pushing away" : "pushing toward you");
            }
            _landText.text =
                $"<color=#4DCCFF><b>DESCENT · SITE {s.id} {s.name}</b></color>\n\n" +
                $"ALTITUDE        <b>{Mathf.Max(0f, lc.Altitude):0.0} m</b>\n" +
                $"V-SPEED          <color={Col(vsBad)}><b>{vs:0.0} m/s</b></color>   <color={Dim}>safe {s.SafeVerticalSpeed:0.0}</color>\n" +
                $"H-SPEED          <color={Col(hsBad)}><b>{hs:0.0} m/s</b></color>   <color={Dim}>safe {s.SafeHorizontalSpeed:0.0}</color>\n" +
                $"TARGET            {lc.DistanceToTarget:0} m  <color={Dim}>(ellipse {s.EllipseRadius:0} m)</color>\n" +
                $"WIND               {lc.Wind.magnitude:0.0} m/s2  <color={Dim}>{windDir}</color>\n" +
                $"BATTERY DRAIN  {lc.PowerDrain:0.00} %/s\n" +
                $"<color={Dim}>gravity {lc.Gravity:0.0} m/s2 (game-scaled)</color>";
            _fuel.fillAmount = lc.Fuel / 100f;
            _power.fillAmount = lc.Power / 100f;
            _throttle.fillAmount = lc.Throttle;

            string w = "";
            if (lc.Power < 15f) w = "BATTERY LOW";
            else if (lc.Fuel <= 0f) w = "FUEL EMPTY";
            else if (lc.Altitude < 40f && vsBad) w = "SINK RATE";
            else if (lc.Altitude < 25f && hsBad) w = "DRIFT";
            _warn.text = w;
            _warn.enabled = w.Length > 0 && Mathf.Repeat(Time.time, 0.8f) < 0.55f;
        }

        // ================================================================== objective + prompt

        void BuildObjective()
        {
            _objective = UIFactory.Box("Objective", _hud.transform, new Vector2(0.3f, 1), new Vector2(0.7f, 1), new Vector2(0, -112), new Vector2(0, -20), new Color(0.04f, 0.06f, 0.10f, 0.7f));
            _objText = UIFactory.Label("Text", _objective, "", 27, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(18, 6), new Vector2(-18, -6));
        }

        public void SetObjective(string text, Vector3? target = null, Transform from = null)
        {
            _objective.gameObject.SetActive(!string.IsNullOrEmpty(text));
            _objBase = text; _objTarget = target; _objFrom = from;
            RefreshObjective();
        }

        string _objBase; Vector3? _objTarget; Transform _objFrom;

        void RefreshObjective()
        {
            if (_objText == null || string.IsNullOrEmpty(_objBase)) return;
            string extra = "";
            if (_objTarget.HasValue && _objFrom != null)
            {
                Vector3 to = _objTarget.Value - _objFrom.position; to.y = 0;
                var cam = Camera.main != null ? Camera.main.transform : _objFrom;
                float ang = Vector3.SignedAngle(Vector3.ProjectOnPlane(cam.forward, Vector3.up), to, Vector3.up);
                string arrow = Mathf.Abs(ang) < 25f ? "ahead" : Mathf.Abs(ang) > 150f ? "behind you" : ang > 0 ? "to the right" : "to the left";
                extra = $"\n<size=22><color={Dim}>{to.magnitude:0} m  ·  {arrow}</color></size>";
            }
            _objText.text = $"<color=#4DCCFF>OBJECTIVE</color>  {_objBase}{extra}";
        }

        void BuildPrompt()
        {
            _prompt = UIFactory.Box("Prompt", _hud.transform, new Vector2(0.36f, 0.27f), new Vector2(0.64f, 0.33f), Vector2.zero, Vector2.zero, new Color(0.04f, 0.06f, 0.10f, 0.75f));
            _promptText = UIFactory.Label("Text", _prompt, "", 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        public void SetPrompt(string text)
        {
            bool on = !string.IsNullOrEmpty(text);
            if (_prompt.gameObject.activeSelf != on) _prompt.gameObject.SetActive(on);
            if (on) _promptText.text = "<color=#FFD24D><b>[E]</b></color>  " + text;
        }

        void Update() => RefreshObjective();

        // ================================================================== info card

        void BuildInfo()
        {
            _info = UIFactory.Box("Info", _modal.transform, new Vector2(0.28f, 0.24f), new Vector2(0.72f, 0.78f), Vector2.zero, Vector2.zero, UIFactory.Panel);
            _infoTitle = UIFactory.Label("Title", _info, "", 32, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -70), new Vector2(-30, -20), UIFactory.Accent);
            _infoBody = UIFactory.Label("Body", _info, "", 25, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(30, 100), new Vector2(-30, -80));
            _infoBtn = UIFactory.Button("Close", _info, "Continue", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, 24), new Vector2(-30, 86), () =>
            {
                _info.gameObject.SetActive(false);
                var cb = _onInfoClose; _onInfoClose = null; cb?.Invoke();
            });
        }

        public void ShowInfo(string title, string body, Action onClose)
        {
            _infoTitle.text = title; _infoBody.text = body; _onInfoClose = onClose;
            _info.gameObject.SetActive(true);
        }

        // ================================================================== son's transmission log

        void BuildLog()
        {
            _log = UIFactory.Box("SonLog", _modal.transform, new Vector2(0.22f, 0.12f), new Vector2(0.78f, 0.9f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.03f, 0.02f, 0.97f));
            UIFactory.Label("Title", _log, "LK-2590  ·  TRANSMISSION LOG", 30, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -66), new Vector2(-30, -20), new Color(0.4f, 1f, 0.55f));
            _logCount = UIFactory.Label("Count", _log, "", 46, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -130), new Vector2(-30, -70), new Color(0.6f, 1f, 0.7f));
            _logLines = UIFactory.Label("Lines", _log, "", 23, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(30, 100), new Vector2(-30, -140), new Color(0.45f, 0.9f, 0.55f));
            _logBtn = UIFactory.Button("Done", _log, "Continue", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, 24), new Vector2(-30, 86), () =>
            {
                _log.gameObject.SetActive(false);
                var cb = _onLogDone; _onLogDone = null; cb?.Invoke();
            });
        }

        public const int SonAttempts = 4381;

        /// <summary>Plays the log: the attempt counter races upward, then the personal entries appear.</summary>
        public void ShowSonLog(Action onDone)
        {
            _onLogDone = onDone;
            _logBtn.gameObject.SetActive(false);
            _log.gameObject.SetActive(true);
            StartCoroutine(SonLogRoutine());
        }

        IEnumerator SonLogRoutine()
        {
            var lines = new List<string>();
            var ci = CultureInfo.InvariantCulture;
            int shown = 0;
            float t = 0f;
            const float dur = 6.5f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Pow(Mathf.Clamp01(t / dur), 2.2f);
                int n = Mathf.Clamp(Mathf.RoundToInt(k * SonAttempts), 1, SonAttempts);
                while (shown < n)
                {
                    shown += Mathf.Max(1, (n - shown) / 6);
                    shown = Mathf.Min(shown, n);
                    int day = 3 + shown * 611 / SonAttempts;
                    lines.Add($"ATTEMPT {shown.ToString("0000", ci)}   DAY {day.ToString("000", ci)}   HAIL NASA ...  NO CARRIER");
                    if (lines.Count > 13) lines.RemoveAt(0);
                }
                _logCount.text = "ATTEMPTS  " + n.ToString("#,0", ci);
                _logLines.text = string.Join("\n", lines);
                yield return null;
            }
            string[] personal =
            {
                "",
                "DAY 019   Crash site holding. Collector built from the wing tanks.",
                "DAY 212   Water is fine. Food is not. Rationing.",
                "DAY 598   The relay overhead went silent. Then an engine flare.",
                "DAY 611   A ship. NASA colours. Dad's build team's design.",
                "DAY 612   I had nothing left. I took what I needed and left the heading here in his scanner. I'm sorry, Dad.",
                "",
                $"ATTEMPT {SonAttempts.ToString("#,0", ci)}   ...  still listening.",
            };
            lines.Clear();
            foreach (var p in personal)
            {
                lines.Add(p);
                _logLines.text = string.Join("\n", lines);
                yield return new WaitForSecondsRealtime(p.Length == 0 ? 0.3f : 1.1f);
            }
            _logBtn.gameObject.SetActive(true);
        }

        // ================================================================== survey packet

        Text[] _items;
        Text _antennaText, _txText;
        RectTransform _needle, _targetZone;
        Image _txBar;
        float _needlePos, _targetPos, _lockTimer;
        bool _aligning, _locked, _sending;
        Action<float> _onAlign;
        Action _onTransmitted;
        bool[] _have = new bool[4];

        void BuildSurvey()
        {
            _survey = UIFactory.Box("Survey", _modal.transform, new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.88f), Vector2.zero, Vector2.zero, UIFactory.Panel);
            UIFactory.Label("Title", _survey, "SURVEY PACKET FOR EARTH", 32, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -68), new Vector2(-30, -20), UIFactory.Accent);
            _items = new Text[4];
            for (int i = 0; i < 4; i++)
                _items[i] = UIFactory.Label("Item" + i, _survey, "", 26, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1),
                    new Vector2(30, -130 - i * 56), new Vector2(-30, -80 - i * 56));

            _antennaText = UIFactory.Label("AntL", _survey, "", 24, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 250), new Vector2(-30, 290));
            var track = UIFactory.Box("Track", _survey, new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 200), new Vector2(-30, 240), new Color(1, 1, 1, 0.08f));
            _targetZone = UIFactory.Box("Target", track, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(-30, 0), new Vector2(30, 0), new Color(0.3f, 0.9f, 0.5f, 0.35f));
            _needle = UIFactory.Box("Needle", track, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(-3, -6), new Vector2(3, 6), UIFactory.Warn);
            UIFactory.Button("Left", _survey, "<", new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 140), new Vector2(110, 190), () => _needlePos -= 0.05f);
            UIFactory.Button("Right", _survey, ">", new Vector2(0, 0), new Vector2(0, 0), new Vector2(120, 140), new Vector2(200, 190), () => _needlePos += 0.05f);
            _txBar = UIFactory.Bar("TxBar", _survey, new Vector2(0, 0), new Vector2(1, 0), new Vector2(220, 150), new Vector2(-330, 180), UIFactory.Good);
            _txText = UIFactory.Label("TxText", _survey, "", 22, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 90), new Vector2(-330, 130), new Color(0.7f, 0.85f, 1f));
            _transmitBtn = UIFactory.Button("Transmit", _survey, "TRANSMIT", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, 26), new Vector2(-30, 110), StartTransmit);
            UIFactory.Label("Keys", _survey, "A / D or < > to align the high-gain antenna on Earth", 20, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 26), new Vector2(-330, 70), new Color(0.6f, 0.7f, 0.8f));
        }

        public void ShowSurvey(Action<float> onAlign, Action onTransmitted)
        {
            _onAlign = onAlign; _onTransmitted = onTransmitted;
            _needlePos = -0.75f; _targetPos = 0.35f; _lockTimer = 0f; _locked = false; _sending = false; _aligning = true;
            _txBar.fillAmount = 0f; _txText.text = "";
            _survey.gameObject.SetActive(true);
            RefreshSurvey();
        }

        public void HideSurvey() { _survey.gameObject.SetActive(false); _aligning = false; }
        public bool SurveyOpen => _survey.gameObject.activeSelf;

        public void SetSurveyItem(int i, bool have, string text)
        {
            _have[i] = have;
            _items[i].text = (have ? "<color=#66E699>[x]</color>  " : "<color=#FF8080>[  ]</color>  ") + text;
            RefreshSurvey();
        }

        bool AllItems => _have[0] && _have[1] && _have[2] && _have[3];

        void RefreshSurvey()
        {
            if (_transmitBtn == null) return;
            _transmitBtn.interactable = AllItems && _locked && !_sending;
            _antennaText.text = _locked ? "ANTENNA  <color=#66E699><b>LOCKED ON EARTH</b></color>"
                                        : $"ANTENNA  <color=#FFC040>searching</color>  <color={Dim}>error {Mathf.Abs(_needlePos - _targetPos) * 40f:0.0} deg</color>";
            if (!AllItems && _txText.text.Length == 0) _txText.text = "<color=#FF8080>Packet incomplete.</color>";
            else if (AllItems && !_sending && _txBar.fillAmount <= 0f) _txText.text = "Packet ready.";
        }

        void LateUpdate()
        {
            if (_survey == null || !_survey.gameObject.activeSelf || !_aligning) return;
            if (!_locked && !_sending)
            {
                _needlePos = Mathf.Clamp(_needlePos + M4Input.Move().x * Time.deltaTime * 0.6f, -1f, 1f);
                bool inZone = Mathf.Abs(_needlePos - _targetPos) < 0.07f;
                _lockTimer = inZone ? _lockTimer + Time.deltaTime : 0f;
                if (_lockTimer > 0.8f) { _locked = true; _needlePos = _targetPos; }
                _onAlign?.Invoke(_needlePos);
            }
            float w = ((RectTransform)_needle.parent).rect.width * 0.5f;
            _needle.anchoredPosition = new Vector2(_needlePos * w, 0);
            _targetZone.anchoredPosition = new Vector2(_targetPos * w, 0);
            RefreshSurvey();
        }

        void StartTransmit()
        {
            if (!AllItems || !_locked || _sending) return;
            StartCoroutine(TransmitRoutine());
        }

        IEnumerator TransmitRoutine()
        {
            _sending = true;
            string[] parts = { "landing coordinates", "water readings", "father's flight log", "son's flight log" };
            float t = 0f; const float dur = 5f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                _txBar.fillAmount = k;
                _txText.text = "Sending " + parts[Mathf.Min(parts.Length - 1, (int)(k * parts.Length))] + " ...  " + (k * 100f).ToString("0") + " %";
                yield return null;
            }
            _txText.text = "<color=#66E699><b>Carrier acknowledged. Survey received by the NASA Deep Space Network.</b></color>";
            yield return new WaitForSeconds(2.2f);
            _aligning = false;
            _onTransmitted?.Invoke();
        }

        // ================================================================== cinematic: letterbox, subtitles, fade

        void BuildCinematic()
        {
            _barTop = UIFactory.Box("BarTop", _cine.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0), Color.black);
            _barBottom = UIFactory.Box("BarBottom", _cine.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 0), Color.black);
            _subtitle = UIFactory.Label("Subtitle", _cine.transform, "", 34, TextAnchor.MiddleCenter, new Vector2(0.1f, 0), new Vector2(0.9f, 0), new Vector2(0, 20), new Vector2(0, 130));
            _fade = UIFactory.Box("Fade", _cine.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0)).GetComponent<Image>();
            _fade.raycastTarget = false;
            _barTop.GetComponent<Image>().raycastTarget = false;
            _barBottom.GetComponent<Image>().raycastTarget = false;
        }

        float _barH;

        public IEnumerator Letterbox(bool on, float dur = 0.8f)
        {
            float from = _barH, to = on ? 140f : 0f, t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                SetBars(Mathf.SmoothStep(from, to, t / dur));
                yield return null;
            }
            SetBars(to);
            if (!on) _subtitle.text = "";
        }

        void SetBars(float h)
        {
            _barH = h;
            _barTop.offsetMin = new Vector2(0, -h); _barTop.offsetMax = Vector2.zero;
            _barBottom.offsetMin = Vector2.zero; _barBottom.offsetMax = new Vector2(0, h);
        }

        public void Subtitle(string speaker, string line)
        {
            NarrationPlayer.PlayMatching(line);
            _subtitle.text = string.IsNullOrEmpty(speaker) ? $"<i>{line}</i>" : $"<color=#FFD24D><b>{speaker}</b></color>   {line}";
        }

        public IEnumerator Fade(float to, float dur)
        {
            float from = _fade.color.a, t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                _fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / dur));
                yield return null;
            }
            _fade.color = new Color(0, 0, 0, to);
        }
    }
}
