using System;
using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.Ship;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.M3
{
    /// <summary>
    /// Damage triage: limited power, choose what to repair. Click a row or press 1-5, then Confirm (or Enter).
    /// The scanner is required. Comms is the trap: with the relay gone, nobody can hear you.
    /// </summary>
    public class TriageMinigame : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        List<RepairOption> _options;
        int _budget;
        Action<TriageResult> _onDone;

        Canvas _canvas;
        Text _header;
        Image _usedFill;
        Text _usedText;
        Text _message;
        Button _confirm;
        readonly List<Text> _rowLabels = new List<Text>();
        readonly List<Image> _rowImages = new List<Image>();
        float _messageUntil;

        public void Open(int powerBudget, Action<TriageResult> onDone)
        {
            _budget = Mathf.Max(0, powerBudget);
            _onDone = onDone;
            _options = TriageModel.DefaultOptions();
            BuildUi();
            IsOpen = true;
            Refresh();
        }

        void BuildUi()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            _rowLabels.Clear();
            _rowImages.Clear();

            _canvas = UIFactory.CreateCanvas("Triage", 45, transform);
            Image dim = UIFactory.CreatePanel("Dim", _canvas.transform, new Color(0f, 0f, 0f, 0.45f));
            UIFactory.SetStretch(dim.rectTransform, 0f, 0f, 0f, 0f);

            Image panel = M3Ui.Box("Panel", _canvas.transform, UIFactory.PanelColor, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1000f, 620f));
            panel.raycastTarget = true;
            Transform p = panel.transform;
            M3Ui.Label("Title", p, "DAMAGE TRIAGE", 36, UIFactory.TextColor, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(900f, 48f));
            _header = M3Ui.Label("Header", p, "", 22, UIFactory.Muted, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(30f, -78f), new Vector2(420f, 30f));

            Image back;
            _usedFill = UIFactory.CreateBar("Used", p, UIFactory.Accent, out back);
            M3Ui.Place(back.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -84f), new Vector2(300f, 18f));
            _usedText = M3Ui.Label("UsedText", p, "", 22, UIFactory.TextColor, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-340f, -78f), new Vector2(200f, 30f));

            for (int i = 0; i < _options.Count; i++)
            {
                int index = i;
                Button row = UIFactory.CreateButton("Row" + i, p, "", delegate { Toggle(index); });
                M3Ui.Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -130f - i * 70f), new Vector2(940f, 60f));
                Text label = row.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.fontSize = 22;
                label.rectTransform.offsetMin = new Vector2(20f, 4f);
                _rowLabels.Add(label);
                _rowImages.Add(row.GetComponent<Image>());
            }

            _message = M3Ui.Label("Message", p, "", 20, UIFactory.Warn, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(30f, 70f), new Vector2(560f, 60f));
            M3Ui.Label("Note", p, "Power costs are GAME-BALANCE values.", 16, UIFactory.Muted, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(30f, 18f), new Vector2(500f, 24f));
            _confirm = UIFactory.CreateButton("Confirm", p, "CONFIRM REPAIRS", Confirm);
            M3Ui.Place(_confirm.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(340f, 64f));
        }

        void Update()
        {
            if (!IsOpen) return;
            int n = ShipInput.NumberPressed();
            if (n >= 1 && n <= _options.Count) Toggle(n - 1);
            if (ShipInput.ConfirmPressed()) Confirm();
            if (_message != null && Time.time > _messageUntil) ShowStatus();
        }

        void Toggle(int index)
        {
            if (!TriageModel.Toggle(_options, index, _budget))
            {
                Say("Not enough power for " + _options[index].label + ". Deselect something first.");
            }
            Refresh();
        }

        void Confirm()
        {
            if (!IsOpen) return;
            string reason;
            if (!TriageModel.CanConfirm(_options, _budget, out reason))
            {
                Say(reason);
                return;
            }
            IsOpen = false;
            TriageResult result = TriageModel.Result(_options);
            Destroy(_canvas.gameObject);
            _canvas = null;
            if (_onDone != null) _onDone(result);
        }

        void Refresh()
        {
            int used = TriageModel.Used(_options);
            _header.text = "Power available: " + _budget;
            _usedText.text = "Used " + used + " / " + _budget;
            UIFactory.SetFill(_usedFill, _budget > 0 ? (float)used / _budget : 0f);
            for (int i = 0; i < _options.Count; i++)
            {
                RepairOption o = _options[i];
                _rowLabels[i].text = (o.selected ? "[X]  " : "[  ]  ") + (i + 1) + "  " + o.label.PadRight(18) + "  " + o.powerCost.ToString().PadLeft(2)
                    + " power     " + o.effect + (o.required ? "  (required)" : "");
                _rowImages[i].color = o.selected ? new Color(0.14f, 0.42f, 0.42f, 1f) : new Color(0.10f, 0.14f, 0.20f, 1f);
            }
            string reason;
            _confirm.interactable = TriageModel.CanConfirm(_options, _budget, out reason);
            ShowStatus();
        }

        void ShowStatus()
        {
            string reason;
            if (TriageModel.CanConfirm(_options, _budget, out reason))
            {
                _message.text = "Ready. Unrepaired systems will cost you later.";
                _message.color = UIFactory.Good;
            }
            else
            {
                _message.text = reason;
                _message.color = UIFactory.Warn;
            }
        }

        void Say(string text)
        {
            _message.text = text;
            _message.color = UIFactory.Bad;
            _messageUntil = Time.time + 2.5f;
        }
    }
}
