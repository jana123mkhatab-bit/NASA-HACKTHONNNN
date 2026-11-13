using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// Reusable Success Probability gauge: a 270 degree arc that animates to its value and changes
    /// colour at the 35% and 70% lines. Drop the RiskGauge prefab on any canvas, or call
    /// RiskGaugeWidget.Create(parent, pos, size). Call SetValue(0..1), SetPlan(PlanConfig) or SetUnknown().
    /// A wide rectangle gives the compact layout (arc left, number right); a square gives the dial.
    /// Subsystem reliabilities are GAME-BALANCE values, not NASA data.
    /// </summary>
    public class RiskGaugeWidget : MonoBehaviour
    {
        public Image fill;
        public Text valueText;
        public Text captionText;
        [Range(0, 1)] public float value;
        public string caption = "SUCCESS PROBABILITY";
        public bool showBackground = true;

        const float Arc = 0.75f; // 270 degrees
        float _shown;
        bool _unknown;
        Image _glow;

        public static RiskGaugeWidget Create(Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            var rt = UIFactory.Rect("RiskGauge", parent, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, size);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            return rt.gameObject.AddComponent<RiskGaugeWidget>(); // Awake builds it
        }

        void Awake() { if (fill == null) Build(); }

        public void Build()
        {
            var root = (RectTransform)transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var o = root.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
            }

            Vector2 size = root.rect.size;
            if (size.x < 1f || size.y < 1f) size = root.sizeDelta;
            bool dial = size.x < size.y * 1.3f;

            if (showBackground)
            {
                var bg = UIFactory.Round("Bg", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Palette.A(Palette.PanelBgSolid, 0.90f), 10);
                bg.raycastTarget = false;
                UIFactory.Stroke("Edge", root, Palette.PanelEdge, 10);
            }

            float d = dial ? Mathf.Min(size.x, size.y * 0.86f) - 16f : size.y - 14f;
            RectTransform ring;
            if (dial)
            {
                ring = UIFactory.Rect("Ring", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-d / 2, -8 - d), new Vector2(d / 2, -8));
            }
            else
            {
                ring = UIFactory.Rect("Ring", root, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, -d / 2), new Vector2(8 + d, d / 2));
            }

            _glow = Arc270("Glow", ring, new Vector2(-6, -6), new Vector2(6, 6), 0.20f);
            _glow.color = Palette.A(Palette.Bad, 0.18f);
            var track = Arc270("Track", ring, Vector2.zero, Vector2.zero, 0.16f);
            track.color = new Color(1, 1, 1, 0.10f);
            track.fillAmount = Arc;
            fill = Arc270("Fill", ring, Vector2.zero, Vector2.zero, 0.16f);

            if (dial)
            {
                valueText = UIFactory.Label("Value", ring, "0%", Mathf.RoundToInt(d * 0.24f), TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(0, d * 0.02f), new Vector2(0, d * 0.02f)).Bold();
                captionText = UIFactory.Label("Caption", root, caption, Mathf.Clamp(Mathf.RoundToInt(size.x * 0.062f), 14, 22), TextAnchor.LowerCenter,
                    new Vector2(0, 0), new Vector2(1, 0), new Vector2(4, 6), new Vector2(-4, 40), Palette.TextDim).Mono();
            }
            else
            {
                float left = 8 + d + 12;
                captionText = UIFactory.Label("Caption", root, caption, Mathf.Clamp(Mathf.RoundToInt(size.y * 0.22f), 12, 18), TextAnchor.UpperLeft,
                    new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(left, 0), new Vector2(-6, -7), Palette.TextDim).Mono();
                captionText.horizontalOverflow = HorizontalWrapMode.Overflow;
                valueText = UIFactory.Label("Value", root, "0%", Mathf.RoundToInt(size.y * 0.48f), TextAnchor.LowerLeft,
                    new Vector2(0, 0), new Vector2(1, 0.62f), new Vector2(left, 3), new Vector2(-6, 0)).Bold();
            }

            _shown = value;
            Paint();
        }

        static Image Arc270(string name, RectTransform parent, Vector2 offMin, Vector2 offMax, float thickness)
        {
            var rt = UIFactory.Rect(name, parent, Vector2.zero, Vector2.one, offMin, offMax);
            rt.localEulerAngles = new Vector3(0, 0, -45f); // start the sweep at 7:30 so the gap sits at the bottom
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISkin.Ring(thickness);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Bottom;
            img.fillClockwise = true;
            img.raycastTarget = false;
            return img;
        }

        public void SetCaption(string text)
        {
            caption = text;
            if (captionText != null) captionText.text = text;
        }

        public void SetPlan(PlanConfig plan) => SetValue(RiskModel.ComputeSuccess(plan));

        public void SetValue(float v)
        {
            _unknown = false;
            value = Mathf.Clamp01(v);
            if (!Application.isPlaying) { _shown = value; Paint(); }
        }

        /// <summary>Jump straight to a value with no animation.</summary>
        public void SnapTo(float v) { SetValue(v); _shown = value; Paint(); }

        /// <summary>The number the pilot never saw: the gauge reads "??%".</summary>
        public void SetUnknown() { _unknown = true; Paint(); }

        void Update()
        {
            if (_unknown) { Paint(); return; }
            if (Mathf.Approximately(_shown, value)) return;
            float step = Mathf.Max(0.25f, Mathf.Abs(value - _shown) * 3.5f) * Time.unscaledDeltaTime;
            _shown = Mathf.MoveTowards(_shown, value, step);
            Paint();
        }

        void Paint()
        {
            if (fill == null) return;
            if (_unknown)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.2f);
                fill.fillAmount = Arc * (0.08f + 0.05f * pulse);
                fill.color = Palette.A(Palette.TextDim, 0.5f + 0.4f * pulse);
                if (_glow != null) { _glow.fillAmount = Arc; _glow.color = Palette.A(Palette.TextDim, 0.05f + 0.06f * pulse); }
                if (valueText != null) { valueText.text = "??%"; valueText.color = Palette.Text; }
                return;
            }
            Color c = Palette.Risk(_shown);
            fill.fillAmount = Arc * _shown;
            fill.color = c;
            if (_glow != null) { _glow.fillAmount = Arc * _shown; _glow.color = Palette.A(c, 0.22f); }
            if (valueText != null)
            {
                valueText.text = (_shown * 100f).ToString("0.0") + "%";
                valueText.color = c;
            }
        }
    }
}
