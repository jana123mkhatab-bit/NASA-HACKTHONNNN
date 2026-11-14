using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Debrief card: opens at mission end, names the real NASA concept and data source.</summary>
    public class DebriefCardUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _title, _concept, _source, _body, _planned, _nasa;

        public bool IsOpen => _canvas != null && _canvas.enabled;

        public void Build()
        {
            _canvas = ScreenKit.Overlay(transform, "Debrief", 40, out var p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                "MISSION DEBRIEF", "WHAT NASA REALLY DOES");
            p.offsetMin = new Vector2(-560, -480); p.offsetMax = new Vector2(560, 480);

            _title = UIFactory.Label("Title", p, "", 50, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-40, -78)).Bold();

            var c = UIFactory.Round("ConceptBox", p, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -256), new Vector2(-40, -166), Palette.Inset, 8);
            c.raycastTarget = false;
            UIFactory.Label("ConceptL", c.rectTransform, "THE REAL CONCEPT", 15, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -32), new Vector2(-18, -10), Palette.Warn).Mono();
            _concept = UIFactory.Label("Concept", c.rectTransform, "", 28, TextAnchor.LowerLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 10), new Vector2(-18, -30)).Bold();

            var s = UIFactory.Round("SourceBox", p, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -350), new Vector2(-40, -268), Palette.Inset, 8);
            s.raycastTarget = false;
            UIFactory.Label("SourceL", s.rectTransform, "NASA SOURCE", 15, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -32), new Vector2(-18, -10), Palette.Good).Mono();
            _source = UIFactory.Label("Source", s.rectTransform, "", 24, TextAnchor.LowerLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 10), new Vector2(-18, -30));

            _body = UIFactory.Label("Body", p, "", 25, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 366), new Vector2(-40, -372));
            _body.lineSpacing = 1.1f;
            var nb = UIFactory.Round("NasaBox", p, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 112), new Vector2(-40, 358), Palette.Inset, 8);
            nb.raycastTarget = false;
            _nasa = UIFactory.Label("Nasa", nb.rectTransform, "", 16, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(14, 6), new Vector2(-14, -6));
            _nasa.resizeTextForBestFit = true; _nasa.resizeTextMinSize = 11; _nasa.resizeTextMaxSize = 16;
            _planned = UIFactory.Label("Planned", p, "", 17, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 34), new Vector2(-340, 96), Palette.TextDim).Mono();
            UIFactory.Button("Continue", p, "CONTINUE", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, 34), new Vector2(-40, 96), OnContinue);
        }

        public void Show(DebriefCard card, string missionId = null)
        {
            _nasa.text = NasaData.DebriefBlock(missionId);
            _title.text = card.title;
            _concept.text = card.concept;
            _source.text = card.nasaSource;
            _body.text = card.body;
            var m = MissionManager.Instance;
            _planned.text = m != null
                ? "Planned success going in: " + (m.PlannedSuccess * 100f).ToString("0.0") + "%   ·   Retries so far: " + m.Retries
                : "";
            Sfx.Play(Sfx.Id.Success);
            _canvas.enabled = true;
        }

        void OnContinue()
        {
            _canvas.enabled = false;
            MissionManager.Instance.ContinueAfterDebrief();
        }
    }
}
