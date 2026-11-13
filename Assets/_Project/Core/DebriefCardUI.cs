using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>Full-screen Debrief card shown after each mission: concept, NASA source, body, Continue button.</summary>
    public class DebriefCardUI : MonoBehaviour
    {
        GameObject _root;
        Text _title, _concept, _source, _body;
        Action _onContinue;

        public static DebriefCardUI Create(Transform parent)
        {
            GameObject go = new GameObject("DebriefCardUI");
            go.transform.SetParent(parent, false);
            DebriefCardUI ui = go.AddComponent<DebriefCardUI>();
            ui.Build();
            return ui;
        }

        void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("Debrief", 80, transform);
            _root = canvas.gameObject;
            Image dim = UIFactory.CreatePanel("Dim", canvas.transform, UIFactory.Dim);
            UIFactory.SetStretch(dim.rectTransform, 0f, 0f, 0f, 0f);

            Image panel = UIFactory.CreatePanel("Panel", canvas.transform, UIFactory.PanelColor);
            UIFactory.SetCenter(panel.rectTransform, 0f, 0f, 1200f, 780f);

            _title = UIFactory.CreateText("Title", panel.transform, "", 40, UIFactory.Accent, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(_title.rectTransform, 40f, 30f, 1120f, 56f);

            _concept = UIFactory.CreateText("Concept", panel.transform, "", 30, UIFactory.TextColor, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(_concept.rectTransform, 40f, 110f, 1120f, 140f);

            _body = UIFactory.CreateText("Body", panel.transform, "", 26, UIFactory.Muted, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(_body.rectTransform, 40f, 270f, 1120f, 300f);

            _source = UIFactory.CreateText("Source", panel.transform, "", 22, UIFactory.Warn, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(_source.rectTransform, 40f, 600f, 1120f, 70f);

            Button next = UIFactory.CreateButton("Continue", panel.transform, "Continue", OnContinue);
            UIFactory.SetTopRight(next.GetComponent<RectTransform>(), 40f, 690f, 240f, 56f);

            _root.SetActive(false);
        }

        public void Show(DebriefCard card, Action onContinue)
        {
            _onContinue = onContinue;
            _title.text = card.title;
            _concept.text = "What this teaches: " + card.concept;
            _body.text = card.body;
            _source.text = "NASA source: " + card.nasaSource;
            _root.SetActive(true);
        }

        public void Hide() { _root.SetActive(false); }

        void OnContinue()
        {
            Hide();
            Action cb = _onContinue;
            _onContinue = null;
            if (cb != null) cb();
        }
    }
}
