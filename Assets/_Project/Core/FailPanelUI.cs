using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Shown when a mission fails: the reason, Pip's hint, and a retry button.</summary>
    public class FailPanelUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _reason, _hint;

        public bool IsOpen => _canvas != null && _canvas.enabled;

        public void Build()
        {
            _canvas = ScreenKit.Overlay(transform, "FailPanel", 50, out var p, new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.72f));
            UIFactory.Label("Header", p, "MISSION FAILED", 44, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -90), new Vector2(-40, -30), UIFactory.Bad);
            _reason = UIFactory.Label("Reason", p, "", 28, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -170), new Vector2(-40, -100));
            _hint = UIFactory.Label("Hint", p, "", 28, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 110), new Vector2(-40, -180), UIFactory.Accent);
            UIFactory.Button("Retry", p, "Retry from checkpoint", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-380, 30), new Vector2(-40, 100), OnRetry);
        }

        public void Show(string id, string reason, string hint)
        {
            _reason.text = id + ": " + reason;
            _hint.text = "Pip: " + hint;
            _canvas.enabled = true;
        }

        void OnRetry()
        {
            _canvas.enabled = false;
            MissionManager.Instance.RetryFromCheckpoint();
        }
    }
}
