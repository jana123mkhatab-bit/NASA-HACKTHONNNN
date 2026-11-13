using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Debrief card: opens at mission end, names the real NASA concept and data source.</summary>
    public class DebriefCardUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _title, _concept, _source, _body;

        public bool IsOpen => _canvas != null && _canvas.enabled;

        public void Build()
        {
            _canvas = ScreenKit.Overlay(transform, "Debrief", 40, out var p, new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.85f));
            UIFactory.Label("Header", p, "MISSION DEBRIEF", 24, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -60), new Vector2(-40, -20), UIFactory.Accent);
            _title = UIFactory.Label("Title", p, "", 44, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -140), new Vector2(-40, -60));
            _concept = UIFactory.Label("Concept", p, "", 30, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -230), new Vector2(-40, -150), UIFactory.Warn);
            _source = UIFactory.Label("Source", p, "", 26, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -300), new Vector2(-40, -235), UIFactory.Good);
            _body = UIFactory.Label("Body", p, "", 28, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 110), new Vector2(-40, -310));
            UIFactory.Button("Continue", p, "Continue", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, 30), new Vector2(-40, 100), OnContinue);
        }

        public void Show(DebriefCard card)
        {
            _title.text = card.title;
            _concept.text = "NASA concept: " + card.concept;
            _source.text = "Data source: " + card.nasaSource;
            _body.text = card.body;
            _canvas.enabled = true;
        }

        void OnContinue()
        {
            _canvas.enabled = false;
            MissionManager.Instance.ContinueAfterDebrief();
        }
    }
}
