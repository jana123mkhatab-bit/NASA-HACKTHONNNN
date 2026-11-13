using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// Always-on HUD: one slim strip along the top with the mission name, supplies / hull / power bars,
    /// the father's log button and the planned-success gauge. Hidden on the title screen.
    /// </summary>
    public class HudShell : MonoBehaviour
    {
        public static HudShell Instance { get; private set; }
        public RiskGaugeWidget Gauge => _gauge;

        Image _supplies, _hull, _power;
        Text _supT, _hullT, _powT, _mission, _kicker;
        RiskGaugeWidget _gauge;
        Canvas _canvas;
        bool _visible = true;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Build()
        {
            _canvas = UIFactory.CreateCanvas("HUD", 10);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;

            var bar = UIFactory.Box("TopBar", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -UIFactory.TopBar), Vector2.zero,
                Palette.A(Palette.PanelBgSolid, 0.92f));
            bar.GetComponent<Image>().raycastTarget = false;
            var rule = UIFactory.Box("Rule", bar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -2), new Vector2(0, 0), Palette.A(Palette.Accent, 0.45f));
            rule.GetComponent<Image>().raycastTarget = false;
            var glow = UIFactory.Rect("Glow", bar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -26), new Vector2(0, -2));
            var gi = glow.gameObject.AddComponent<Image>();
            gi.sprite = UISkin.FadeDown(); gi.color = Palette.A(Palette.Accent, 0.10f); gi.raycastTarget = false;

            // Left: mission identity.
            var mark = UIFactory.Round("Mark", bar, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, -9), new Vector2(42, 9), Palette.Accent, 4);
            mark.rectTransform.localEulerAngles = new Vector3(0, 0, 45f);
            mark.raycastTarget = false;
            _kicker = UIFactory.Label("Kicker", bar, "", 15, TextAnchor.LowerLeft,
                new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(60, 1), new Vector2(700, -8), Palette.Accent).Mono();
            _mission = UIFactory.Label("Mission", bar, "", 27, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(0, 0.5f), new Vector2(60, 4), new Vector2(760, 5)).Bold();
            _mission.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Right: gauge, then log button, then the three resource bars (laid out right to left).
            _gauge = RiskGaugeWidget.Create(bar, Vector2.zero, new Vector2(214, 56));
            var grt = (RectTransform)_gauge.transform;
            grt.anchorMin = grt.anchorMax = new Vector2(1, 0.5f);
            grt.pivot = new Vector2(1, 0.5f);
            grt.anchoredPosition = new Vector2(-20, 0);
            _gauge.showBackground = false;
            _gauge.caption = "PLANNED SUCCESS";
            _gauge.Build();

            UIFactory.Button("Log", bar, "LOG  [L]", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-360, -19), new Vector2(-252, 19),
                () =>
                {
                    var log = transform.parent != null ? transform.parent.GetComponentInChildren<FatherLogUI>() : null;
                    if (log != null) log.Toggle();
                }, ButtonStyle.Ghost);

            _power = Stat(bar, "POWER", 0, Palette.Warn, out _powT);
            _hull = Stat(bar, "HULL", 1, Palette.Good, out _hullT);
            _supplies = Stat(bar, "SUPPLIES", 2, Palette.Accent, out _supT);
            Refresh();
        }

        static Image Stat(RectTransform parent, string label, int indexFromRight, Color color, out Text valueText)
        {
            float right = -384 - indexFromRight * 196;
            float left = right - 180;
            UIFactory.Label(label + "_L", parent, label, 14, TextAnchor.LowerLeft,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(left, 4), new Vector2(right - 46, 24), Palette.TextDim).Mono();
            valueText = UIFactory.Label(label + "_V", parent, "100", 20, TextAnchor.LowerRight,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(right - 60, 2), new Vector2(right, 26)).Bold();
            return UIFactory.Bar(label + "_B", parent, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(left, -16), new Vector2(right, -3), color);
        }

        void Update()
        {
            if (_canvas != null && MissionManager.Instance != null) Refresh();
        }

        public void Refresh()
        {
            var m = MissionManager.Instance;
            if (m == null || m.state == null || _supplies == null) return;
            // The title screen owns the whole frame.
            _canvas.enabled = _visible && m.Current != MissionIds.Boot;
            Set(_supplies, _supT, m.state.supplies);
            Set(_hull, _hullT, m.state.hull);
            Set(_power, _powT, m.state.power);
            _kicker.text = KickerFor(m.Current);
            _mission.text = MissionIds.TitleFor(m.Current).ToUpperInvariant();
            _gauge.SetValue(m.PlannedSuccess);
        }

        static string KickerFor(string id)
        {
            switch (id)
            {
                case MissionIds.M1: return "MISSION 1 OF 4  ·  GROUND TEST RANGE";
                case MissionIds.M2: return "MISSION 2 OF 4  ·  PREFLIGHT";
                case MissionIds.M3: return "MISSION 3 OF 4  ·  DEEP SPACE";
                case MissionIds.M4: return "MISSION 4 OF 4  ·  SURFACE";
                case MissionIds.Final: return "FINALE";
                case MissionIds.DesignMode: return "FREE PLAY";
                default: return "";
            }
        }

        static void Set(Image bar, Text t, float v)
        {
            bar.fillAmount = Mathf.Clamp01(v / 100f);
            t.text = Mathf.RoundToInt(v).ToString();
        }

        public void SetVisible(bool on) { _visible = on; if (_canvas != null) _canvas.enabled = on; }

        public void ShowGauge(bool show)
        {
            if (_gauge != null) _gauge.gameObject.SetActive(show);
        }
    }
}
