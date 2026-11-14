using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>End card comparing the success probability you planned with how the run actually went.</summary>
    public class EndCardUI : MonoBehaviour
    {
        Canvas _canvas;
        RiskGaugeWidget _planned, _actual;
        Text _verdict, _datasets;

        public void Build()
        {
            _canvas = ScreenKit.Overlay(transform, "EndCard", 60, out var p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                "THE LAST KNOWN SIGNAL", "MISSION RECORD");
            p.offsetMin = new Vector2(-560, -470); p.offsetMax = new Vector2(560, 470);
            _planned = RiskGaugeWidget.Create(p, new Vector2(150, -84), new Vector2(300, 320));
            _planned.showBackground = false; _planned.caption = "WHAT YOU PLANNED"; _planned.Build();
            _actual = RiskGaugeWidget.Create(p, new Vector2(590, -84), new Vector2(300, 320));
            _actual.showBackground = false; _actual.caption = "WHAT HAPPENED"; _actual.Build();
            _verdict = UIFactory.Label("Verdict", p, "", 26, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 112), new Vector2(-40, 190));
            _verdict.fontSize = 22;
            _datasets = UIFactory.Label("Datasets", p, "", 16, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 196), new Vector2(-40, 420));
            _datasets.resizeTextForBestFit = true; _datasets.resizeTextMinSize = 11; _datasets.resizeTextMaxSize = 16;
            UIFactory.Label("Note", p, "\"What happened\" is a game-balance score from hull, power, supplies and retries.", 15, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 34), new Vector2(-500, 96), Palette.TextDim).Mono();
            UIFactory.Button("Design", p, "ENTER MISSION DESIGN MODE", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-470, 34), new Vector2(-40, 96), OnDesign);
        }

        public void Show(float planned, float actual, int retries)
        {
            _planned.SnapTo(0f); _actual.SnapTo(0f);
            _planned.SetValue(planned); _actual.SetValue(actual);
            _verdict.text = actual >= planned
                ? "You beat the odds. Good planning plus good flying. Retries: " + retries
                : "The plan said " + (planned * 100f).ToString("0") + "%, the flight delivered " + (actual * 100f).ToString("0") + "%. Retries: " + retries;
            _datasets.text = "<color=#7FE0FF><b>NASA DATA THAT SHAPED THIS PLAYTHROUGH</b></color>  <color=#8899AA>(snapshot 2026-11-13, not live)</color>\n" + NasaData.PlaythroughList();
            Sfx.Play(Sfx.Id.Success);
            _canvas.enabled = true;
        }

        void OnDesign()
        {
            _canvas.enabled = false;
            MissionManager.Instance.GoTo(MissionIds.DesignMode);
        }
    }
}
