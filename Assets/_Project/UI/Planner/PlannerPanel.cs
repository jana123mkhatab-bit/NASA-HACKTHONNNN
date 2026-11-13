using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Reusable code-built planner. All reliability and mass values are game balance.</summary>
    public class PlannerPanel : MonoBehaviour
    {
        [Serializable]
        class Row
        {
            public Subsystem subsystem;
            public Slider reliability;
            public Toggle backup;
            public Text reliabilityText;
            public Text massText;
        }

        public float massBudget = 10f;
        public PlanConfig CurrentPlan { get; private set; }
        public event Action<PlanConfig> PlanApplied;

        Canvas _canvas;
        Text _massText;
        Image _massFill;
        Text _briefing;
        RiskGaugeWidget _gauge;
        readonly List<Row> _rows = new List<Row>();
        bool _built;

        public void Build()
        {
            if (_built) return;
            _built = true;
            CurrentPlan = CreateDefaultPlan();
            _canvas = UIFactory.CreateCanvas("MissionPlanner", 30);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            var panel = UIFactory.Box("PlannerPanel", root, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(-650, -440), new Vector2(650, 440), UIFactory.Panel);
            UIFactory.Label("Title", panel, "MISSION PLANNER", 32, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -62), new Vector2(-28, -18), UIFactory.Accent);
            UIFactory.Button("LoadSonsPlan", panel, "Load son's plan", new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-250, -62), new Vector2(-28, -18), LoadSonsPlan);
            _massText = UIFactory.Label("Mass", panel, "", 22, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(.65f, 1), new Vector2(28, -115), new Vector2(-10, -76));
            _massFill = UIFactory.Bar("MassBar", panel, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(28, -155), new Vector2(-28, -125), UIFactory.Accent);

            string[] names = { "Life support", "Backup comms", "Navigation", "Power", "Propulsion" };
            for (int i = 0; i < names.Length; i++) CreateRow(panel, i, names[i]);

            _gauge = RiskGaugeWidget.Create(root, new Vector2(338, -684), new Vector2(560, 118));
            _briefing = UIFactory.Label("RiskBriefing", panel, "", 21, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(28, 110), new Vector2(-28, 164), UIFactory.TextCol);
            UIFactory.Button("Apply", panel, "Apply plan", new Vector2(.5f, 0), new Vector2(1, 0),
                new Vector2(8, 28), new Vector2(-28, 92), Apply);
            Refresh();
        }

        void CreateRow(RectTransform parent, int index, string name)
        {
            float top = -190f - index * 82f;
            var row = new Row { subsystem = new Subsystem(name, .90f, 1, 1f) };
            row.reliability = CreateSlider(parent, "Reliability_" + name, new Vector2(235, top - 48), new Vector2(560, top - 10), .63f, .90f, row);
            row.reliabilityText = UIFactory.Label("ReliabilityText_" + name, parent, "", 20, TextAnchor.MiddleRight,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(570, top - 48), new Vector2(665, top - 10));
            row.backup = CreateToggle(parent, "Backup_" + name, "Backup", new Vector2(700, top - 48), new Vector2(820, top - 10), row);
            row.massText = UIFactory.Label("Mass_" + name, parent, "", 20, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(860, top - 48), new Vector2(1040, top - 10));
            UIFactory.Label("Name_" + name, parent, name, 22, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, top - 48), new Vector2(220, top - 10));
            _rows.Add(row);
        }

        Slider CreateSlider(Transform parent, string name, Vector2 min, Vector2 max, float low, float high, Row row)
        {
            var rt = UIFactory.Box(name, parent, new Vector2(0, 1), new Vector2(0, 1),
                min, max, new Color(1, 1, 1, .12f));
            var slider = rt.gameObject.AddComponent<Slider>();
            var handle = UIFactory.Box("Handle", rt, new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(-8, 0), new Vector2(8, 0), UIFactory.Accent);
            slider.handleRect = handle;
            slider.targetGraphic = rt.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = low;
            slider.maxValue = high;
            slider.value = row.subsystem.reliability;
            slider.onValueChanged.AddListener(value => { row.subsystem.reliability = value; Refresh(); });
            return slider;
        }

        Toggle CreateToggle(Transform parent, string name, string label, Vector2 min, Vector2 max, Row row)
        {
            var rt = UIFactory.Box(name, parent, new Vector2(0, 1), new Vector2(0, 1), min, max,
                new Color(.12f, .35f, .55f, 1f));
            var toggle = rt.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = rt.GetComponent<Image>();
            toggle.isOn = false;
            toggle.onValueChanged.AddListener(value => { row.subsystem.redundancy = value ? 2 : 1; Refresh(); });
            UIFactory.Label("Label", rt, label, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return toggle;
        }

        PlanConfig CreateDefaultPlan()
        {
            var p = new PlanConfig();
            foreach (string name in new[] { "Life support", "Backup comms", "Navigation", "Power", "Propulsion" })
                p.subsystems.Add(new Subsystem(name, .90f, 1, 1f));
            return p;
        }

        public void LoadSonsPlan()
        {
            CurrentPlan = Clone(PlanConfig.SonsOriginalPlan());
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].subsystem.reliability = CurrentPlan.subsystems[i].reliability;
                _rows[i].subsystem.redundancy = 1;
                _rows[i].reliability.value = .63f;
                _rows[i].backup.isOn = false;
            }
            Refresh();
        }

        void Apply()
        {
            float probability = RiskModel.ComputeSuccess(CurrentPlan);
            MissionManager.Instance?.SetPlannedSuccess(probability);
            PlanApplied?.Invoke(Clone(CurrentPlan));
            SetVisible(false);
        }

        public void SetVisible(bool visible) { if (_canvas != null) _canvas.enabled = visible; }

        void Refresh()
        {
            if (_massText == null) return;
            CurrentPlan = new PlanConfig();
            foreach (var row in _rows)
            {
                row.subsystem.reliability = row.reliability.value;
                row.subsystem.redundancy = row.backup.isOn ? 2 : 1;
                CurrentPlan.subsystems.Add(row.subsystem);
                row.reliabilityText.text = (row.subsystem.reliability * 100f).ToString("0") + "%";
                row.massText.text = (row.subsystem.massCost * row.subsystem.redundancy).ToString("0.0") + " kg";
            }
            float mass = CurrentPlan.TotalMass();
            _massText.text = "Plan mass  " + mass.ToString("0.0") + " / " + massBudget.ToString("0.0") + " kg" +
                (mass > massBudget ? "  OVER BUDGET" : "");
            _massFill.fillAmount = Mathf.Clamp01(mass / massBudget);
            _massFill.color = mass > massBudget ? UIFactory.Bad : UIFactory.Accent;
            _gauge.SetPlan(CurrentPlan);
            Subsystem weakest = null;
            foreach (var s in CurrentPlan.subsystems)
                if (weakest == null || s.reliability < weakest.reliability) weakest = s;
            _briefing.text = weakest == null
                ? "Risk briefing: add subsystems before launch."
                : "Risk briefing: weakest subsystem is " + weakest.name + " (" +
                  (weakest.reliability * 100f).ToString("0") + "%, " + weakest.redundancy + " unit" +
                  (weakest.redundancy == 1 ? "" : "s") + ").";
        }

        static PlanConfig Clone(PlanConfig source)
        {
            var copy = new PlanConfig();
            foreach (var s in source.subsystems)
                copy.subsystems.Add(new Subsystem(s.name, s.reliability, s.redundancy, s.massCost));
            return copy;
        }
    }
}
