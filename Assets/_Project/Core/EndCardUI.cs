using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>End card comparing the success probability you planned with how the run actually went.</summary>
    public class EndCardUI : MonoBehaviour
    {
        Canvas _canvas;
        Image _planned, _actual;
        Text _plannedT, _actualT, _verdict;

        public void Build()
        {
            _canvas = ScreenKit.Overlay(transform, "EndCard", 60, out var p, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            UIFactory.Label("Header", p, "THE LAST KNOWN SIGNAL: RESULT", 40, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -90), new Vector2(-40, -30), UIFactory.Accent);
            UIFactory.Label("PL", p, "PLANNED", 26, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -190), new Vector2(240, -130));
            _planned = UIFactory.Bar("PlannedBar", p, new Vector2(0, 1), new Vector2(1, 1), new Vector2(250, -180), new Vector2(-190, -140), UIFactory.Warn);
            _plannedT = UIFactory.Label("PV", p, "", 28, TextAnchor.MiddleRight, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-180, -190), new Vector2(-40, -130));
            UIFactory.Label("AL", p, "ACTUAL", 26, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -270), new Vector2(240, -210));
            _actual = UIFactory.Bar("ActualBar", p, new Vector2(0, 1), new Vector2(1, 1), new Vector2(250, -260), new Vector2(-190, -220), UIFactory.Good);
            _actualT = UIFactory.Label("AV", p, "", 28, TextAnchor.MiddleRight, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-180, -270), new Vector2(-40, -210));
            _verdict = UIFactory.Label("Verdict", p, "", 28, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 120), new Vector2(-40, -300));
            UIFactory.Button("Design", p, "Enter Mission Design Mode", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-480, 30), new Vector2(-40, 100), OnDesign);
        }

        public void Show(float planned, float actual, int retries)
        {
            _planned.fillAmount = planned; _actual.fillAmount = actual;
            _plannedT.text = (planned * 100f).ToString("0.0") + " %";
            _actualT.text = (actual * 100f).ToString("0.0") + " %";
            _verdict.text = actual >= planned
                ? "You beat the odds. Good planning plus good flying. Retries: " + retries
                : "The plan said " + (planned * 100f).ToString("0") + " %, the flight delivered " + (actual * 100f).ToString("0") + " %. Retries: " + retries;
            _canvas.enabled = true;
        }

        void OnDesign()
        {
            _canvas.enabled = false;
            MissionManager.Instance.GoTo(MissionIds.DesignMode);
        }
    }
}
