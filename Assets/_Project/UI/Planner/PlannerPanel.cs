using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// The Mission Planner: the heart of the challenge answer. For each subsystem the player picks the
    /// equipment grade (untested r = 0.63 or tested r = 0.90) and the number of redundant units n, inside
    /// a mass budget. The gauge shows RiskModel.ComputeSuccess live: P = product of [1 - (1 - r)^n].
    /// Reusable: missions call Build(), LoadSonsPlan(), SetVisible(true) and listen to PlanApplied.
    /// ALL RELIABILITIES AND MASSES ARE GAME-BALANCE VALUES, NOT NASA DATA.
    /// </summary>
    public class PlannerPanel : MonoBehaviour
    {
        // ---- game balance (labelled as such on screen)
        public const float UntestedR = 0.63f, TestedR = 0.90f, UntestedMassFactor = 0.7f;
        public const int MaxUnits = 3;
        public float massBudgetTonnes = 9.5f;

        class Def { public string name, role; public float testedMass; }
        static readonly Def[] Defs =
        {
            new Def { name = "Life support", role = "air, water, heat for the crew", testedMass = 1.4f },
            new Def { name = "Comms", role = "radio · 2+ units = backup", testedMass = 0.8f },
            new Def { name = "Navigation", role = "star trackers and guidance", testedMass = 0.9f },
            new Def { name = "Power", role = "reactor control, batteries", testedMass = 1.6f },
            new Def { name = "Propulsion", role = "engine and turbopumps", testedMass = 2.0f },
        };

        class Row
        {
            public bool tested;
            public int units = 1;
            public Image gradeLo, gradeHi;
            public Text unitsText, works, mass, name;
        }

        public PlanConfig CurrentPlan { get; private set; }
        /// <summary>The last plan the player applied (null until Apply is pressed).</summary>
        public PlanConfig AppliedPlan { get; private set; }
        public float Probability => RiskModel.ComputeSuccess(CurrentPlan);
        public bool OverBudget => CurrentPlan != null && CurrentPlan.TotalMass() > massBudgetTonnes + 1e-4f;
        public bool IsVisible => _canvas != null && _canvas.enabled;
        public event Action<PlanConfig> PlanApplied;

        Canvas _canvas;
        readonly List<Row> _rows = new List<Row>();
        RiskGaugeWidget _gauge;
        Text _massText, _formula, _briefing;
        Image _massFill;
        Button _apply;
        bool _built;

        public void Build()
        {
            if (_built) return;
            _built = true;
            _canvas = UIFactory.CreateCanvas("MissionPlanner", 30);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            UIFactory.Box("Dim", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Palette.A(Palette.Ink, 0.6f));
            var win = UIFactory.Window("Planner", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-690, -420), new Vector2(690, 380), "MISSION PLANNER", "GAME-BALANCE VALUES, NOT NASA DATA");
            var kicker = win.Find("Kicker") as RectTransform;
            if (kicker != null) kicker.offsetMax = new Vector2(-80, kicker.offsetMax.y);

            Header(win, "SUBSYSTEM", 24, 220);
            Header(win, "EQUIPMENT  (r per unit)", 236, 500);
            Header(win, "UNITS  n", 512, 650);
            Header(win, "WORKS", 662, 772);
            Header(win, "MASS", 784, 870);
            for (int i = 0; i < Defs.Length; i++) CreateRow(win, i);

            var note = UIFactory.Round("Rocket", win, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(870, 150), Palette.Inset, 8);
            note.raycastTarget = false;
            UIFactory.Label("RocketL", note.rectTransform, "WHY MASS MATTERS  ·  THE ROCKET EQUATION", 14, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -30), new Vector2(-16, -8), Palette.Accent).Mono();
            UIFactory.Label("RocketT", note.rectTransform,
                "m0 = mf · e^(dv / ve). Every tonne of spare hardware must be pushed by extra propellant, so redundancy is never free. " +
                "Untested parts are lighter but fail more often.", 19, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 10), new Vector2(-16, -34), Palette.TextDim);

            // Right column: gauge, mass, formula, briefing, actions.
            _gauge = RiskGaugeWidget.Create(win, new Vector2(970, -66), new Vector2(300, 300));
            _gauge.showBackground = false;
            _gauge.caption = "SUCCESS PROBABILITY";
            _gauge.Build();
            _massText = UIFactory.Label("Mass", win, "", 19, TextAnchor.LowerLeft, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(900, -410), new Vector2(1356, -380)).Mono();
            _massFill = UIFactory.Bar("MassBar", win, new Vector2(0, 1), new Vector2(0, 1), new Vector2(900, -432), new Vector2(1356, -416), Palette.Accent);
            var f = UIFactory.Round("FormulaBox", win, new Vector2(0, 1), new Vector2(0, 1), new Vector2(900, -548), new Vector2(1356, -446), Palette.Inset, 8);
            f.raycastTarget = false;
            UIFactory.Label("FormulaL", f.rectTransform, "P = PRODUCT OF [ 1 - (1 - r)^n ]", 14, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -28), new Vector2(-14, -8), Palette.Accent).Mono();
            _formula = UIFactory.Label("Formula", f.rectTransform, "", 17, TextAnchor.LowerLeft, Vector2.zero, Vector2.one,
                new Vector2(14, 8), new Vector2(-14, -30)).Mono();
            _briefing = UIFactory.Label("RiskBriefing", win, "", 19, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(900, -660), new Vector2(1356, -560));

            UIFactory.Button("LoadSonsPlan", win, "LOAD SON'S PLAN", new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(900, 24), new Vector2(1120, 84), () => { LoadSonsPlan(); Sfx.Play(Sfx.Id.Whoosh, 0.5f); }, ButtonStyle.Ghost);
            _apply = UIFactory.Button("Apply", win, "APPLY PLAN", new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(1132, 24), new Vector2(1356, 84), Apply);
            UIFactory.Button("Close", win, "X", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-62, -50), new Vector2(-18, -14),
                () => SetVisible(false), ButtonStyle.Ghost);

            LoadSonsPlan();
            _gauge.SnapTo(Probability);
        }

        static void Header(RectTransform win, string text, float x0, float x1)
        {
            UIFactory.Label("H_" + text, win, text, 14, TextAnchor.LowerLeft, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(x0, -88), new Vector2(x1, -66), Palette.TextDim).Mono();
        }

        void CreateRow(RectTransform win, int index)
        {
            var d = Defs[index];
            var row = new Row();
            float top = -96f - index * 92f, bot = top - 82f;
            var bg = UIFactory.Round("Row_" + d.name, win, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, bot), new Vector2(874, top), Palette.Inset, 8);
            bg.raycastTarget = false;
            var r = bg.rectTransform;
            row.name = UIFactory.Label("Name", r, d.name, 23, TextAnchor.UpperLeft, Vector2.zero, new Vector2(0, 1), new Vector2(14, 0), new Vector2(210, -12)).Bold();
            UIFactory.Label("Role", r, d.role, 13, TextAnchor.LowerLeft, Vector2.zero, new Vector2(0, 1), new Vector2(14, 10), new Vector2(214, 0), Palette.TextDim).Mono();

            row.gradeLo = (Image)UIFactory.Button("Untested", r, "UNTESTED  63%", new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(216, -22), new Vector2(346, 22), () => SetGrade(row, false), ButtonStyle.Ghost).targetGraphic;
            row.gradeHi = (Image)UIFactory.Button("Tested", r, "TESTED  90%", new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(352, -22), new Vector2(482, 22), () => SetGrade(row, true), ButtonStyle.Ghost).targetGraphic;
            UIFactory.Button("Minus", r, "-", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(494, -22), new Vector2(534, 22),
                () => SetUnits(row, row.units - 1), ButtonStyle.Ghost);
            row.unitsText = UIFactory.Label("Units", r, "1", 26, TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(536, -22), new Vector2(586, 22)).Bold();
            UIFactory.Button("Plus", r, "+", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(588, -22), new Vector2(628, 22),
                () => SetUnits(row, row.units + 1), ButtonStyle.Ghost);
            row.works = UIFactory.Label("Works", r, "", 24, TextAnchor.MiddleRight, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(636, -20), new Vector2(748, 20)).Bold();
            row.mass = UIFactory.Label("Mass", r, "", 17, TextAnchor.MiddleRight, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(754, -20), new Vector2(840, 20)).Mono();
            _rows.Add(row);
        }

        void SetGrade(Row row, bool tested)
        {
            if (row.tested == tested) return;
            row.tested = tested;
            Sfx.Play(Sfx.Id.Snap, 0.6f);
            Refresh();
        }

        void SetUnits(Row row, int n)
        {
            n = Mathf.Clamp(n, 1, MaxUnits);
            if (n == row.units) { Sfx.Play(Sfx.Id.Deny); return; }
            row.units = n;
            Sfx.Play(Sfx.Id.Click);
            Refresh();
        }

        /// <summary>The son's original configuration: five single-string, untested subsystems (about 10%).</summary>
        public void LoadSonsPlan()
        {
            foreach (var r in _rows) { r.tested = false; r.units = 1; }
            Refresh();
        }

        /// <summary>Put a plan into the editor, matching subsystems by name.</summary>
        public void LoadPlan(PlanConfig plan)
        {
            if (plan == null) return;
            for (int i = 0; i < _rows.Count; i++)
            {
                var s = plan.subsystems.Find(x => x != null && x.name == Defs[i].name);
                if (s == null) continue;
                _rows[i].tested = s.reliability >= (UntestedR + TestedR) * 0.5f;
                _rows[i].units = Mathf.Clamp(s.redundancy, 1, MaxUnits);
            }
            Refresh();
        }

        void Apply()
        {
            if (OverBudget) { Sfx.Play(Sfx.Id.Deny); return; }
            AppliedPlan = Clone(CurrentPlan);
            MissionManager.Instance?.SetPlannedSuccess(Probability);
            Sfx.Play(Sfx.Id.Confirm);
            PlanApplied?.Invoke(Clone(CurrentPlan));
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (_canvas == null) return;
            if (visible && !_canvas.enabled) Sfx.Play(Sfx.Id.Whoosh, 0.5f);
            _canvas.enabled = visible;
        }

        void Update()
        {
            if (IsVisible && Inp.EscapeDown()) SetVisible(false);
        }

        void Refresh()
        {
            CurrentPlan = new PlanConfig();
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i]; var d = Defs[i];
                float r = row.tested ? TestedR : UntestedR;
                float unitMass = d.testedMass * (row.tested ? 1f : UntestedMassFactor);
                CurrentPlan.subsystems.Add(new Subsystem(d.name, r, row.units, unitMass));
            }
            if (_massText == null) return;

            Subsystem weakest = null; float weakestP = 2f;
            var factors = new StringBuilder();
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i]; var s = CurrentPlan.subsystems[i];
                float p = 1f - Mathf.Pow(1f - s.reliability, s.redundancy);
                if (p < weakestP) { weakestP = p; weakest = s; }
                Paint(row.gradeLo, !row.tested);
                Paint(row.gradeHi, row.tested);
                row.unitsText.text = row.units.ToString();
                row.unitsText.color = row.units > 1 ? Palette.Accent : Palette.Text;
                row.works.text = (p * 100f).ToString("0.0") + "%";
                row.works.color = Palette.Risk(p * p * p); // shade by how much this one link drags the chain
                row.mass.text = (s.massCost * s.redundancy).ToString("0.0") + " t";
                if (i > 0) factors.Append(" × ");
                factors.Append(p.ToString("0.000"));
            }

            float mass = CurrentPlan.TotalMass();
            bool over = OverBudget;
            float prob = Probability;
            _massText.text = "PLAN MASS  " + mass.ToString("0.0") + " / " + massBudgetTonnes.ToString("0.0") + " t" + (over ? "   <color=#FF5A4D>OVER BUDGET</color>" : "");
            _massFill.fillAmount = Mathf.Clamp01(mass / massBudgetTonnes);
            _massFill.color = over ? Palette.Bad : Palette.Accent;
            _formula.text = factors + "\n= " + (prob * 100f).ToString("0.0") + "%";
            _gauge.SetValue(prob);
            _apply.interactable = !over;

            string weak = weakest == null ? "" : "<b>" + weakest.name + "</b> works " + (weakestP * 100f).ToString("0.0") + "% of the time (" +
                weakest.redundancy + (weakest.redundancy == 1 ? " unit, " : " units, ") + (weakest.reliability > 0.8f ? "tested" : "untested") + ").";
            if (over)
                _briefing.text = "<color=#FF5A4D>Over the mass budget by " + (mass - massBudgetTonnes).ToString("0.0") + " t.</color> The ship cannot carry this. Remove a unit somewhere.";
            else if (prob >= 0.70f)
                _briefing.text = "<color=#4DE38A>Clears the 70% preflight line.</color> Weakest link: " + weak;
            else
                _briefing.text = "<color=#FFB938>Risk briefing:</color> weakest link is " + weak + " Test the equipment or add a backup unit there first.";
        }

        static void Paint(Image img, bool on)
        {
            img.color = on ? Palette.A(Palette.Accent, 0.26f) : Palette.ButtonGhost;
            var edge = img.transform.Find("Edge");
            if (edge != null) edge.GetComponent<Image>().color = on ? Palette.Accent : Palette.A(Palette.Text, 0.2f);
            var t = img.GetComponentInChildren<Text>();
            if (t != null) { t.color = on ? Palette.Text : Palette.TextDim; t.fontSize = 15; }
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
