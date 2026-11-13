using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>
    /// The Risk Gauge: a reusable Success Probability widget. The gauge never hides the number.
    ///
    /// Usage (Mission Planner, Design Mode, anywhere):
    ///     RiskGaugeWidget gauge = RiskGaugeWidget.Create(someCanvasTransform);
    ///     UIFactory.SetTopLeft(gauge.Rect, 40, 40, 520, 90);
    ///     gauge.SetPlan(plan);      // computes with RiskModel and shows the briefing line
    ///     gauge.SetUnknown();       // shows "??%" (used in the prologue)
    /// </summary>
    public class RiskGaugeWidget : MonoBehaviour
    {
        public RectTransform Rect { get { return (RectTransform)transform; } }

        Image _fill;
        Text _value;
        Text _briefing;
        float _target;
        float _shown;
        bool _unknown;

        public static RiskGaugeWidget Create(Transform parent)
        {
            RectTransform root = UIFactory.CreateRect("RiskGauge", parent);
            RiskGaugeWidget gauge = root.gameObject.AddComponent<RiskGaugeWidget>();
            gauge.Build();
            return gauge;
        }

        void Build()
        {
            Image back = UIFactory.CreatePanel("Back", transform, UIFactory.PanelColor);
            UIFactory.SetStretch(back.rectTransform, 0f, 0f, 0f, 0f);

            Text label = UIFactory.CreateText("Label", transform, "SUCCESS PROBABILITY", 18, UIFactory.Muted, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(label.rectTransform, 14f, 6f, 330f, 24f);

            _value = UIFactory.CreateText("Value", transform, "0%", 40, UIFactory.TextColor, TextAnchor.UpperRight);
            UIFactory.SetTopRight(_value.rectTransform, 14f, 2f, 150f, 46f);

            Image barBack;
            _fill = UIFactory.CreateBar("Bar", transform, UIFactory.Warn, out barBack);
            UIFactory.SetTopLeft(barBack.rectTransform, 14f, 50f, 412f, 16f);

            _briefing = UIFactory.CreateText("Briefing", transform, "", 18, UIFactory.TextColor, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(_briefing.rectTransform, 14f, 72f, 412f, 50f);

            UIFactory.SetTopLeft(Rect, 0f, 0f, 440f, 124f);
            SetValue(0f);
        }

        public void SetValue(float value01)
        {
            _unknown = false;
            _target = Mathf.Clamp01(value01);
        }

        public void SetUnknown()
        {
            _unknown = true;
            _target = 0f;
            _shown = 0f;
            _value.text = "??%";
            _value.color = UIFactory.Muted;
            UIFactory.SetFill(_fill, 0f);
            _briefing.text = "The pilot never saw this number.";
        }

        public void SetBriefing(string text)
        {
            _briefing.text = text;
        }

        /// <summary>Computes the probability with RiskModel, updates the gauge and the briefing line.</summary>
        public void SetPlan(PlanConfig plan)
        {
            SetValue(RiskModel.ComputeSuccess(plan));
            SetBriefing(RiskModel.RiskBriefing(plan));
        }

        void Update()
        {
            if (_unknown) return;
            _shown = Mathf.MoveTowards(_shown, _target, Time.unscaledDeltaTime * 0.8f);
            UIFactory.SetFill(_fill, _shown);
            _value.text = Mathf.RoundToInt(_shown * 100f) + "%";
            Color c = _shown < 0.35f ? UIFactory.Bad : (_shown < 0.65f ? UIFactory.Warn : UIFactory.Good);
            _fill.color = c;
            _value.color = c;
        }
    }
}
