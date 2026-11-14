using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// "Data and Sources" screen opened from the title. Lists every dataset with its source_url, retrieved date and what
    /// in the game uses it, whether the file is in this build, and any fallback a mission reported. States that the data is a snapshot.
    /// </summary>
    public class DataSourcesScreen : MonoBehaviour
    {
        Canvas _canvas;
        ScrollRect _scroll;
        Text _text;

        public void Build()
        {
            _canvas = ScreenKit.Overlay(transform, "DataSources", 80, out var p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                "DATA & SOURCES", "NASA OPEN DATA");
            p.offsetMin = new Vector2(-600, -420); p.offsetMax = new Vector2(600, 420);

            UIFactory.Label("Snapshot", p, "<b>This is a SNAPSHOT, not live data.</b> " + NasaData.SnapshotLine + "\n" +
                "Reliabilities, part masses, mass budgets, supplies and mission days are [game balance] values, not NASA data.",
                20, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-40, -66), Palette.Warn);

            var view = UIFactory.Box("View", p, Vector2.zero, Vector2.one, new Vector2(40, 100), new Vector2(-70, -158), Palette.A(Palette.Ink, 0.45f));
            view.gameObject.AddComponent<RectMask2D>();
            var contentRt = UIFactory.Rect("Content", view, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            _text = contentRt.gameObject.AddComponent<Text>();
            _text.font = UIFactory.Font; _text.fontSize = 19; _text.color = Palette.Text; _text.supportRichText = true;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap; _text.verticalOverflow = VerticalWrapMode.Overflow; _text.raycastTarget = false;
            var fit = contentRt.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll = view.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = view; _scroll.content = contentRt;
            _scroll.horizontal = false; _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped; _scroll.scrollSensitivity = 40f;
            view.GetComponent<Image>().raycastTarget = true;   // the scroll view needs something to hit

            UIFactory.Button("Up", p, "UP", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-64, -300), new Vector2(-40, -250), () => Nudge(0.15f), ButtonStyle.Ghost);
            UIFactory.Button("Down", p, "DN", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-64, -360), new Vector2(-40, -310), () => Nudge(-0.15f), ButtonStyle.Ghost);
            UIFactory.Button("Close", p, "BACK", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, 30), new Vector2(-40, 84), Hide);
            UIFactory.Label("Hint", p, "Mouse wheel or UP / DN to scroll.   Open each source_url to check a number yourself.", 16, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 30), new Vector2(-320, 84), Palette.TextDim).Mono();
        }

        void Nudge(float d)
        {
            if (_scroll != null) _scroll.verticalNormalizedPosition = Mathf.Clamp01(_scroll.verticalNormalizedPosition + d);
        }

        public void Show()
        {
            _text.text = Describe();
            _canvas.enabled = true;
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 1f;
        }

        public void Hide() { _canvas.enabled = false; }

        void Update()
        {
            if (_canvas != null && _canvas.enabled && Inp.EscapeDown()) Hide();
        }

        public static string Describe()
        {
            var sb = new StringBuilder();
            foreach (NasaSource s in NasaData.All)
            {
                bool inBuild = Resources.Load<TextAsset>("LastSignal/" + s.file.Replace(".csv", "")) != null;
                sb.Append("<color=#7FE0FF><b>").Append(s.title).Append("</b></color>   <color=#8899AA>[").Append(s.IsDerived ? "NASA data, DERIVED by us" : "NASA data").Append("]</color>\n");
                sb.Append("file: ").Append(s.file).Append("   in this build: ").Append(inBuild ? "<color=#4DE38A>yes</color>" : "<color=#FF5A4D>NO, MISSING</color>").Append('\n');
                sb.Append("source_url: ").Append(s.sourceUrl).Append('\n');
                sb.Append("retrieved: ").Append(s.retrieved).Append("   (snapshot, not live)\n");
                sb.Append("used by: ").Append(s.usedBy).Append('\n');
                sb.Append("why NASA cares: ").Append(s.why).Append('\n');
                foreach (var f in NasaData.Fallbacks)
                    if (f.file == s.file) sb.Append("<color=#FFB938><b>FALLBACK USED this session:</b> ").Append(f.reason).Append("</color>\n");
                sb.Append('\n');
            }
            if (NasaData.Fallbacks.Count == 0) sb.Append("<color=#4DE38A>No mission has reported fallback data this session.</color>\n");
            return sb.ToString();
        }
    }
}
