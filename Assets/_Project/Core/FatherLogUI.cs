using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>
    /// "Father's log" panel. A Log button opens a list of entries added through FatherLog.Add(...)
    /// and automatically when missions complete.
    /// </summary>
    public class FatherLogUI : MonoBehaviour
    {
        public static FatherLogUI Instance { get; private set; }

        GameObject _root;
        GameObject _button;
        GameObject _panel;
        Text _body;

        void Awake()
        {
            Instance = this;
            Canvas canvas = UIFactory.CreateCanvas("FatherLog", 55, transform);
            _root = canvas.gameObject;

            Button open = UIFactory.CreateButton("LogButton", canvas.transform, "Log", Toggle);
            UIFactory.SetTopLeft(open.GetComponent<RectTransform>(), 24f, 110f, 140f, 48f);
            _button = open.gameObject;

            _panel = UIFactory.CreatePanel("Panel", canvas.transform, UIFactory.PanelColor).gameObject;
            UIFactory.SetCenter(_panel.GetComponent<RectTransform>(), 0f, 0f, 1000f, 640f);

            Text title = UIFactory.CreateText("Title", _panel.transform, "FATHER'S LOG", 28, UIFactory.Accent, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(title.rectTransform, 30f, 22f, 600f, 40f);

            _body = UIFactory.CreateText("Body", _panel.transform, "", 28, UIFactory.TextColor, TextAnchor.UpperLeft);
            UIFactory.SetStretch(_body.rectTransform, 30f, 76f, 30f, 90f);

            Button close = UIFactory.CreateButton("Close", _panel.transform, "Close", Toggle);
            UIFactory.SetTopRight(close.GetComponent<RectTransform>(), 30f, 20f, 160f, 48f);

            _panel.SetActive(false);
            FatherLog.Changed += Refresh;
            Refresh();
            SetVisible(false);
        }

        void OnDestroy()
        {
            FatherLog.Changed -= Refresh;
            if (Instance == this) Instance = null;
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }

        void Toggle()
        {
            _panel.SetActive(!_panel.activeSelf);
        }

        void Refresh()
        {
            if (_body == null) return;
            StringBuilder sb = new StringBuilder();
            if (FatherLog.Entries.Count == 0) sb.Append("No entries yet.");
            for (int i = 0; i < FatherLog.Entries.Count; i++)
            {
                sb.Append("- ").Append(FatherLog.Entries[i]).Append("\n\n");
            }
            _body.text = sb.ToString();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }
    }
}
