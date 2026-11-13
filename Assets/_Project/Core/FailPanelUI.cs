using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>Shown when a mission fails: the reason, and a Retry button that restores the last checkpoint.</summary>
    public class FailPanelUI : MonoBehaviour
    {
        GameObject _root;
        Text _reason;
        Action _onRetry;

        public static FailPanelUI Create(Transform parent)
        {
            GameObject go = new GameObject("FailPanelUI");
            go.transform.SetParent(parent, false);
            FailPanelUI ui = go.AddComponent<FailPanelUI>();
            ui.Build();
            return ui;
        }

        void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("FailPanel", 85, transform);
            _root = canvas.gameObject;
            Image dim = UIFactory.CreatePanel("Dim", canvas.transform, UIFactory.Dim);
            UIFactory.SetStretch(dim.rectTransform, 0f, 0f, 0f, 0f);

            Image panel = UIFactory.CreatePanel("Panel", canvas.transform, UIFactory.PanelColor);
            UIFactory.SetCenter(panel.rectTransform, 0f, 0f, 900f, 420f);

            Text title = UIFactory.CreateText("Title", panel.transform, "MISSION FAILED", 40, UIFactory.Bad, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(title.rectTransform, 40f, 30f, 820f, 56f);

            _reason = UIFactory.CreateText("Reason", panel.transform, "", 28, UIFactory.TextColor, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(_reason.rectTransform, 40f, 110f, 820f, 190f);

            Button retry = UIFactory.CreateButton("Retry", panel.transform, "Retry from checkpoint", OnRetry);
            UIFactory.SetTopRight(retry.GetComponent<RectTransform>(), 40f, 330f, 360f, 56f);

            _root.SetActive(false);
        }

        public void Show(string reason, Action onRetry)
        {
            _onRetry = onRetry;
            _reason.text = string.IsNullOrEmpty(reason) ? "The mission did not succeed." : reason;
            _root.SetActive(true);
        }

        public void Hide() { _root.SetActive(false); }

        void OnRetry()
        {
            Hide();
            Action cb = _onRetry;
            _onRetry = null;
            if (cb != null) cb();
        }
    }
}
