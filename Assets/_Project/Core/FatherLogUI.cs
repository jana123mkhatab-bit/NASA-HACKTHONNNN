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
            _canvas = ScreenKit.Overlay(transform, "FatherLog", 35, out var panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                "FATHER'S LOG", "PRIVATE");
            panel.offsetMin = new Vector2(-460, -380); panel.offsetMax = new Vector2(460, 380);
            var bar = panel.Find("AccentBar");
            if (bar != null) bar.GetComponent<Image>().color = Palette.Foil;
            _body = UIFactory.Label("Body", panel, "", 25, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 110), new Vector2(-40, -80), Palette.Text);
            _body.lineSpacing = 1.12f;
            UIFactory.Button("Close", panel, "CLOSE  [L]", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-240, 34), new Vector2(-40, 90), Toggle, ButtonStyle.Ghost);
        }

        void Update()
        {
            if (_canvas != null && Inp.LogKeyDown()) Toggle();
        }

        public void Toggle()
        {
            if (_canvas == null) return;
            var m = MissionManager.Instance;
            if (!_canvas.enabled && m != null && m.Current == MissionIds.Boot) return; // not on the title screen
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
                    sb.Append("<color=#C9A24A><b>").Append(e.heading.ToUpperInvariant()).AppendLine("</b></color>");
                    sb.AppendLine(e.text).AppendLine();
                }
            }
            if (sb.Length == 0) sb.Append("(no entries yet)");
            _body.text = sb.ToString();
        }
    }
}
