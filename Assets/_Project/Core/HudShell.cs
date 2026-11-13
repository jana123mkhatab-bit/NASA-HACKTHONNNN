using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Always-on HUD: supplies, hull and power bars, mission label, and a planned-success gauge.</summary>
    public class HudShell : MonoBehaviour
    {
        Image _supplies, _hull, _power;
        Text _supT, _hullT, _powT, _mission;
        RiskGaugeWidget _gauge;
        Canvas _canvas;

        public void Build()
        {
            _canvas = UIFactory.CreateCanvas("HUD", 10);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            var panel = UIFactory.Box("Panel", root, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(460, 190), UIFactory.Panel);
            panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = new Vector2(20, -20);

            _supplies = Row(panel, "SUPPLIES", 0, UIFactory.Accent, out _supT);
            _hull = Row(panel, "HULL", 1, UIFactory.Good, out _hullT);
            _power = Row(panel, "POWER", 2, UIFactory.Warn, out _powT);
            _mission = UIFactory.Label("Mission", panel, "", 22, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(14, 8), new Vector2(-14, 40), new Color(0.7f, 0.85f, 1f));

            _gauge = RiskGaugeWidget.Create(root, new Vector2(20, -225), new Vector2(460, 110));
            Refresh();
        }

        static Image Row(RectTransform parent, string label, int index, Color color, out Text valueText)
        {
            float top = -14 - index * 40;
            UIFactory.Label(label + "_L", parent, label, 20, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, top - 30), new Vector2(150, top));
            var bar = UIFactory.Bar(label + "_B", parent, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(160, top - 28), new Vector2(-80, top - 4), color);
            valueText = UIFactory.Label(label + "_V", parent, "100", 20, TextAnchor.MiddleRight,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-70, top - 30), new Vector2(-14, top));
            return bar;
        }

        void Update()
        {
            if (_canvas != null && MissionManager.Instance != null) Refresh();
        }

        public void Refresh()
        {
            var m = MissionManager.Instance;
            if (m == null || m.state == null || _supplies == null) return;
            Set(_supplies, _supT, m.state.supplies);
            Set(_hull, _hullT, m.state.hull);
            Set(_power, _powT, m.state.power);
            _mission.text = "MISSION " + m.Current + "  ·  " + MissionIds.TitleFor(m.Current);
            _gauge.SetValue(m.PlannedSuccess);
        }

        static void Set(Image bar, Text t, float v)
        {
            bar.fillAmount = Mathf.Clamp01(v / 100f);
            t.text = Mathf.RoundToInt(v).ToString();
        }

        public void SetVisible(bool on) { if (_canvas != null) _canvas.enabled = on; }
    }
}
