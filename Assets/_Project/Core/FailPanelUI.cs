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
            _canvas = ScreenKit.Overlay(transform, "FailPanel", 50, out var p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                "MISSION FAILED", "RETURN TO CHECKPOINT");
            p.offsetMin = new Vector2(-470, -230); p.offsetMax = new Vector2(470, 230);
            var bar = p.Find("AccentBar");
            if (bar != null) bar.GetComponent<Image>().color = Palette.Bad;
            _reason = UIFactory.Label("Reason", p, "", 30, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -200), new Vector2(-40, -80)).Bold();
            var box = UIFactory.Round("HintBox", p, new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 116), new Vector2(-40, -212), Palette.Inset, 8);
            box.raycastTarget = false;
            UIFactory.Label("HintL", box.rectTransform, "PIP SAYS", 15, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -32), new Vector2(-18, -10), Palette.Accent).Mono();
            _hint = UIFactory.Label("Hint", box.rectTransform, "", 25, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 10), new Vector2(-18, -36));
            UIFactory.Button("Retry", p, "RETRY FROM CHECKPOINT", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-400, 34), new Vector2(-40, 96), OnRetry);
        }

        public void Show(string id, string reason, string hint)
        {
            _reason.text = reason;
            _hint.text = hint;
            Sfx.Play(Sfx.Id.Fail);
            _canvas.enabled = true;
        }

        void OnRetry()
        {
            _canvas.enabled = false;
            MissionManager.Instance.RetryFromCheckpoint();
        }
    }
}
