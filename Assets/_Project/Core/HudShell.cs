using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>
    /// The HUD shell: supplies, hull, power and fuel bars, the mission title, and a Risk Gauge slot.
    /// It is created by the MissionManager and shown in every mission scene.
    ///
    ///     HudShell.Instance.ShowGauge(true);
    ///     HudShell.Instance.Gauge.SetPlan(plan);
    /// </summary>
    public class HudShell : MonoBehaviour
    {
        public static HudShell Instance { get; private set; }

        public RiskGaugeWidget Gauge { get; private set; }

        GameObject _root;
        Text _title;
        readonly Image[] _fills = new Image[4];
        readonly Text[] _values = new Text[4];
        readonly string[] _names = { "SUPPLIES", "HULL", "POWER", "FUEL" };

        void Awake()
        {
            Instance = this;
            Build();
            SetVisible(false);
            ShowGauge(false);
        }

        void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("HUD", 50, transform);
            _root = canvas.gameObject;

            Image strip = UIFactory.CreatePanel("TopStrip", canvas.transform, new Color(0.03f, 0.05f, 0.09f, 0.80f));
            UIFactory.SetTopStrip(strip.rectTransform, 96f);
            strip.raycastTarget = false;

            for (int i = 0; i < 4; i++)
            {
                float x = 24f + i * 250f;

                Text label = UIFactory.CreateText(_names[i] + "_Label", canvas.transform, _names[i], 18, UIFactory.Muted, TextAnchor.UpperLeft);
                UIFactory.SetTopLeft(label.rectTransform, x, 10f, 150f, 24f);

                _values[i] = UIFactory.CreateText(_names[i] + "_Value", canvas.transform, "100", 18, UIFactory.TextColor, TextAnchor.UpperRight);
                UIFactory.SetTopLeft(_values[i].rectTransform, x + 130f, 10f, 90f, 24f);

                Image barBack;
                _fills[i] = UIFactory.CreateBar(_names[i], canvas.transform, UIFactory.Accent, out barBack);
                UIFactory.SetTopLeft(barBack.rectTransform, x, 40f, 220f, 18f);
            }

            _title = UIFactory.CreateText("MissionTitle", canvas.transform, "", 26, UIFactory.TextColor, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(_title.rectTransform, 24f, 64f, 960f, 32f);

            Gauge = RiskGaugeWidget.Create(canvas.transform);
            UIFactory.SetTopRight(Gauge.Rect, 24f, 104f, 440f, 124f);

            if (Application.isEditor || Debug.isDebugBuild)
            {
                Button dev = UIFactory.CreateButton("DevComplete", canvas.transform, "DEV: complete mission", OnDevComplete);
                UIFactory.SetTopRight(dev.GetComponent<RectTransform>(), 24f, 8f, 300f, 44f);
            }
        }

        void OnDevComplete()
        {
            MissionManager manager = MissionManager.Instance;
            if (manager != null && manager.CurrentMissionId != null) MissionEvents.OnMissionComplete(manager.CurrentMissionId);
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }

        public void SetTitle(string text)
        {
            if (_title != null) _title.text = text;
        }

        /// <summary>Shows or hides the Risk Gauge slot. The Mission Planner and Design Mode turn it on.</summary>
        public void ShowGauge(bool show)
        {
            if (Gauge != null) Gauge.gameObject.SetActive(show);
        }

        void Update()
        {
            PlayerState s = PlayerState.Current;
            Apply(0, s.supplies);
            Apply(1, s.hull);
            Apply(2, s.power);
            Apply(3, s.fuel);
        }

        void Apply(int index, float value)
        {
            UIFactory.SetFill(_fills[index], value / 100f);
            _fills[index].color = value < 25f ? UIFactory.Bad : (value < 50f ? UIFactory.Warn : UIFactory.Accent);
            _values[index].text = Mathf.RoundToInt(value).ToString();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }
    }
}
