using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Title screen shown over the first scene. Begin starts a new run at M1.</summary>
    public class TitleScreenUI : MonoBehaviour
    {
        Canvas _canvas;

        public void Build()
        {
            _canvas = UIFactory.CreateCanvas("Title", 70);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            UIFactory.Box("Shade", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.45f));
            UIFactory.Label("Name", root, "THE LAST KNOWN SIGNAL", 96, TextAnchor.MiddleCenter,
                new Vector2(0, 0.55f), new Vector2(1, 0.8f), Vector2.zero, Vector2.zero);
            UIFactory.Label("Sub", root, "Year 2590. A pilot vanished. His father joins the build team.", 32, TextAnchor.MiddleCenter,
                new Vector2(0, 0.45f), new Vector2(1, 0.55f), Vector2.zero, Vector2.zero, new Color(0.7f, 0.85f, 1f));
            UIFactory.Button("Begin", root, "Begin", new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(-150, -35), new Vector2(150, 35), OnBegin);
            UIFactory.Label("Credit", root, "NASA Space Apps Challenge Cairo 2026  ·  Space Mission Design Game", 22, TextAnchor.LowerCenter,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 20), new Vector2(0, 80), new Color(1, 1, 1, 0.6f));
        }

        void OnBegin()
        {
            _canvas.enabled = false;
            MissionManager.Instance.BeginRun();
        }

        public void Show() { _canvas.enabled = true; }
    }
}
