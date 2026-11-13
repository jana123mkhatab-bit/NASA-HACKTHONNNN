using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Father's log. Press L to open or close. Entries unlock as missions are completed.</summary>
    public class FatherLogUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _body;

        public void Build()
        {
            _canvas = UIFactory.CreateCanvas("FatherLog", 30);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            var panel = UIFactory.Box("Panel", root, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.07f, 0.05f, 0.95f));
            UIFactory.Label("Title", panel, "FATHER'S LOG", 40, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -80), new Vector2(-100, -20), new Color(1f, 0.85f, 0.5f));
            UIFactory.Button("Close", panel, "X", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -70), new Vector2(-20, -20), Toggle);
            _body = UIFactory.Label("Body", panel, "", 28, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(30, 30), new Vector2(-30, -100), new Color(0.95f, 0.9f, 0.8f));
            _canvas.enabled = false;
        }

        void Update()
        {
            if (_canvas != null && Inp.LogKeyDown()) Toggle();
        }

        public void Toggle()
        {
            if (_canvas == null) return;
            _canvas.enabled = !_canvas.enabled;
            if (_canvas.enabled) Refresh();
        }

        public void Refresh()
        {
            var m = MissionManager.Instance;
            var sb = new StringBuilder();
            if (m != null && m.fatherLog != null)
            {
                foreach (var e in m.fatherLog.entries)
                {
                    bool open = string.IsNullOrEmpty(e.unlockedAfter) || m.IsCompleted(e.unlockedAfter);
                    if (!open) continue;
                    sb.AppendLine(e.heading).AppendLine(e.text).AppendLine();
                }
            }
            if (sb.Length == 0) sb.Append("(no entries yet)");
            _body.text = sb.ToString();
        }
    }
}
