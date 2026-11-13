using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// Reusable Success Probability gauge. Drop the RiskGauge prefab on any canvas, or call
    /// RiskGaugeWidget.Create(parent). Call SetValue(0..1) or SetPlan(PlanConfig).
    /// Subsystem reliabilities are GAME-BALANCE values, not NASA data.
    /// </summary>
    public class RiskGaugeWidget : MonoBehaviour
    {
        public Image fill;
        public Text valueText;
        public Text captionText;
        [Range(0, 1)] public float value;

        public static RiskGaugeWidget Create(Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            var rt = UIFactory.Box("RiskGauge", parent, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, size, UIFactory.Panel);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            var g = rt.gameObject.AddComponent<RiskGaugeWidget>();
            g.Build();
            return g;
        }

        void Awake() { if (fill == null) Build(); }

        public void Build()
        {
            var root = (RectTransform)transform;
            var old = new System.Collections.Generic.List<GameObject>();
            foreach (Transform c in root) old.Add(c.gameObject);
            foreach (var o in old) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
            captionText = UIFactory.Label("Caption", root, "SUCCESS PROBABILITY (game balance)", 20, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -34), new Vector2(-12, -6), new Color(0.7f, 0.85f, 1f));
            valueText = UIFactory.Label("Value", root, "0.0 %", 40, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(12, 8), new Vector2(-12, -34));
            fill = UIFactory.Bar("Bar", root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(12, 10), new Vector2(-12, 30), UIFactory.Bad);
            SetValue(value);
        }

        public void SetPlan(PlanConfig plan) => SetValue(RiskModel.ComputeSuccess(plan));

        public void SetValue(float v)
        {
            value = Mathf.Clamp01(v);
            if (fill == null) return;
            fill.fillAmount = value;
            fill.color = value >= 0.70f ? UIFactory.Good : value >= 0.35f ? UIFactory.Warn : UIFactory.Bad;
            if (valueText != null) valueText.text = (value * 100f).ToString("0.0") + " %";
        }
    }
}
