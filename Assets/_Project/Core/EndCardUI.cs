using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>End card: planned success vs retries for each mission, then a button into Design Mode.</summary>
    public class EndCardUI : MonoBehaviour
    {
        GameObject _root;
        Text _summary;
        Action _onContinue;

        public static EndCardUI Create(Transform parent)
        {
            GameObject go = new GameObject("EndCardUI");
            go.transform.SetParent(parent, false);
            EndCardUI ui = go.AddComponent<EndCardUI>();
            ui.Build();
            return ui;
        }

        void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("EndCard", 90, transform);
            _root = canvas.gameObject;
            Image dim = UIFactory.CreatePanel("Dim", canvas.transform, UIFactory.Dim);
            UIFactory.SetStretch(dim.rectTransform, 0f, 0f, 0f, 0f);

            Image panel = UIFactory.CreatePanel("Panel", canvas.transform, UIFactory.PanelColor);
            UIFactory.SetCenter(panel.rectTransform, 0f, 0f, 1200f, 760f);

            Text title = UIFactory.CreateText("Title", panel.transform, "THE LAST KNOWN SIGNAL", 44, UIFactory.Accent, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(title.rectTransform, 40f, 30f, 1120f, 60f);

            _summary = UIFactory.CreateText("Summary", panel.transform, "", 28, UIFactory.TextColor, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(_summary.rectTransform, 40f, 110f, 1120f, 480f);

            Button go = UIFactory.CreateButton("DesignMode", panel.transform, "Open Design Mode", OnContinue);
            UIFactory.SetTopRight(go.GetComponent<RectTransform>(), 40f, 660f, 300f, 56f);

            _root.SetActive(false);
        }

        public void Show(IList<MissionRecord> records, Action onContinue)
        {
            _onContinue = onContinue;
            StringBuilder sb = new StringBuilder();
            sb.Append("Planned success vs. retries\n\n");
            for (int i = 0; i < records.Count; i++)
            {
                MissionRecord r = records[i];
                sb.Append(MissionIds.Title(r.missionId)).Append(":  ");
                sb.Append(r.hasPlan ? "planned " + Mathf.RoundToInt(r.plannedSuccess * 100f) + "%" : "no plan");
                sb.Append("  |  retries " + r.retries).Append("\n");
            }
            _summary.text = sb.ToString();
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
