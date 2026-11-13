using System;
using System.Collections.Generic;
using System.Reflection;
using LastSignal.Core;
using LastSignal.Ship;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.M3
{
    /// <summary>
    /// After the stranger leaves: replan with half the supplies so only the planet is reachable.
    ///
    /// If person 2's Planner prefab is assigned and has a component with
    ///     public void OpenForReroute(float supplies, string[] destinationNames, float[] supplyCosts, System.Action&lt;int&gt; onChosen)
    /// it is used (called by reflection, so neither assembly depends on the other).
    /// Otherwise a built-in panel is shown, driving Core's Success Probability gauge with RerouteModel.PlanFor.
    /// </summary>
    public class ReroutePlanner : MonoBehaviour
    {
        public const string ExternalMethod = "OpenForReroute";

        public bool IsOpen { get; private set; }

        List<Destination> _destinations;
        float _supplies;
        bool _engine;
        bool _lifeSupport;
        float _multiplier;
        int _selected;
        Action<Destination, PlanConfig, bool> _onChosen;
        GameObject _external;

        Canvas _canvas;
        readonly List<Image> _rowImages = new List<Image>();
        readonly List<Text> _rowLabels = new List<Text>();
        Text _detail;
        Text _confirmLabel;

        public void Open(GameObject plannerPrefab, float supplies, bool engineRepaired, bool lifeSupportRepaired,
            Action<Destination, PlanConfig, bool> onChosen)
        {
            _destinations = RerouteModel.Default();
            _supplies = supplies;
            _engine = engineRepaired;
            _lifeSupport = lifeSupportRepaired;
            _multiplier = RerouteModel.CostMultiplier(engineRepaired, lifeSupportRepaired);
            _onChosen = onChosen;
            IsOpen = true;

            if (plannerPrefab != null && TryExternal(plannerPrefab)) return;
            BuildFallback();
            Select(0);
        }

        bool TryExternal(GameObject prefab)
        {
            GameObject inst = Instantiate(prefab);
            string[] names = new string[_destinations.Count];
            float[] costs = new float[_destinations.Count];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = _destinations[i].name;
                costs[i] = RerouteModel.EffectiveCost(_destinations[i], _multiplier);
            }
            Type[] sig = { typeof(float), typeof(string[]), typeof(float[]), typeof(Action<int>) };
            MonoBehaviour[] behaviours = inst.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null) continue;
                MethodInfo m = behaviours[i].GetType().GetMethod(ExternalMethod, BindingFlags.Public | BindingFlags.Instance, null, sig, null);
                if (m == null) continue;
                _external = inst;
                Action<int> cb = Choose;
                m.Invoke(behaviours[i], new object[] { _supplies, names, costs, cb });
                return true;
            }
            Debug.LogWarning("M3: the Planner prefab has no " + ExternalMethod + "(float, string[], float[], Action<int>). Using the built-in reroute panel.");
            Destroy(inst);
            return false;
        }

        void BuildFallback()
        {
            _canvas = UIFactory.CreateCanvas("Reroute", 45, transform);
            Image panel = M3Ui.Box("Panel", _canvas.transform, UIFactory.PanelColor, new Vector2(0.5f, 0.5f), new Vector2(-150f, -20f), new Vector2(980f, 520f));
            panel.raycastTarget = true;
            Transform p = panel.transform;
            M3Ui.Label("Title", p, "REROUTE   |   SUPPLIES " + Mathf.FloorToInt(_supplies) + " / 100", 32, UIFactory.TextColor, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(940f, 44f));
            string repairs = (_engine ? "" : "Engine unrepaired (x1.4)  ") + (_lifeSupport ? "" : "Life support unrepaired (x1.25)");
            M3Ui.Label("Repairs", p, repairs.Length > 0 ? repairs : "All key systems repaired.", 18, _multiplier > 1f ? UIFactory.Warn : UIFactory.Muted,
                TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(940f, 26f));

            for (int i = 0; i < _destinations.Count; i++)
            {
                int index = i;
                Button row = UIFactory.CreateButton("Dest" + i, p, "", delegate { Select(index); });
                M3Ui.Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -110f - i * 76f), new Vector2(920f, 64f));
                Text label = row.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.fontSize = 24;
                label.rectTransform.offsetMin = new Vector2(20f, 4f);
                _rowLabels.Add(label);
                _rowImages.Add(row.GetComponent<Image>());
            }

            _detail = M3Ui.Label("Detail", p, "", 20, UIFactory.TextColor, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(30f, 96f), new Vector2(560f, 80f));
            M3Ui.Label("Note", p, M3Lines.PlannerNote, 16, UIFactory.Muted, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(30f, 18f), new Vector2(560f, 50f));
            Button confirm = UIFactory.CreateButton("SetCourse", p, "SET COURSE", delegate { Choose(_selected); });
            M3Ui.Place(confirm.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(340f, 64f));
            _confirmLabel = confirm.GetComponentInChildren<Text>();

            if (HudShell.Instance != null) HudShell.Instance.ShowGauge(true);
        }

        void Select(int index)
        {
            _selected = Mathf.Clamp(index, 0, _destinations.Count - 1);
            for (int i = 0; i < _destinations.Count; i++)
            {
                Destination d = _destinations[i];
                float cost = RerouteModel.EffectiveCost(d, _multiplier);
                bool ok = RerouteModel.Reachable(d, _supplies, _multiplier);
                _rowLabels[i].text = (i == _selected ? "> " : "   ") + (i + 1) + "  " + d.name + "      needs " + Mathf.CeilToInt(cost)
                    + "      " + (ok ? "REACHABLE" : "UNREACHABLE");
                _rowLabels[i].color = ok ? Color.white : new Color(1f, 0.7f, 0.7f);
                _rowImages[i].color = i == _selected ? new Color(0.14f, 0.42f, 0.42f, 1f) : new Color(0.10f, 0.14f, 0.20f, 1f);
            }

            Destination sel = _destinations[_selected];
            bool reachable = RerouteModel.Reachable(sel, _supplies, _multiplier);
            PlanConfig plan = RerouteModel.PlanFor(sel, _supplies, _engine, _lifeSupport);
            if (HudShell.Instance != null) HudShell.Instance.Gauge.SetPlan(plan);
            float need = RerouteModel.EffectiveCost(sel, _multiplier);
            _detail.text = reachable
                ? sel.name + ": you arrive with about " + Mathf.FloorToInt(_supplies - need) + " supplies to spare."
                : sel.name + ": you would run out about " + Mathf.CeilToInt(need - _supplies) + " supplies short.";
            _detail.color = reachable ? UIFactory.Good : UIFactory.Bad;
            _confirmLabel.text = reachable ? "SET COURSE" : "SET COURSE ANYWAY";
        }

        void Update()
        {
            if (!IsOpen || _canvas == null) return;
            int n = ShipInput.NumberPressed();
            if (n >= 1 && n <= _destinations.Count) Select(n - 1);
            if (ShipInput.ConfirmPressed()) Choose(_selected);
        }

        void Choose(int index)
        {
            if (!IsOpen) return;
            if (index < 0 || index >= _destinations.Count) return;
            IsOpen = false;
            Destination d = _destinations[index];
            PlanConfig plan = RerouteModel.PlanFor(d, _supplies, _engine, _lifeSupport);
            bool reachable = RerouteModel.Reachable(d, _supplies, _multiplier);
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (_external != null) Destroy(_external);
            if (HudShell.Instance != null) HudShell.Instance.ShowGauge(false);
            if (_onChosen != null) _onChosen(d, plan, reachable);
        }

        /// <summary>Supplies needed for a destination with the current repairs (for messages).</summary>
        public float CostOf(Destination d)
        {
            return RerouteModel.EffectiveCost(d, _multiplier);
        }
    }
}
