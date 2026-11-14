using System;
using System.Collections.Generic;
using System.Text;
using LastSignal.Ship;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.M3
{
    /// <summary>
    /// Reusable challenge site: fly into its radius and it fires once. The scenario (NASA card, options, debrief)
    /// is plain data from ChallengeModel, so any scene can drop this on an object and give it a scenario.
    /// (Named ...Component because ChallengeSite is already the CSV data class used by FreeFlight.)
    /// </summary>
    public class ChallengeSiteComponent : MonoBehaviour
    {
        public float radius = 300f;   // [game balance]
        public ChallengeScenario scenario;
        public bool Triggered { get; private set; }
        public bool Resolved { get; private set; }

        /// <summary>Raised once when the ship enters the radius.</summary>
        public event Action<ChallengeSiteComponent> Entered;
        /// <summary>Raised once when the player has made a choice (option index).</summary>
        public event Action<ChallengeSiteComponent, int> Resolution;

        public static bool InRadius(Vector3 shipPos, Vector3 sitePos, float r)
        {
            return (shipPos - sitePos).sqrMagnitude <= r * r;
        }

        /// <summary>Checks the ship position. Returns true only on the frame it triggers (never twice).</summary>
        public bool Tick(Vector3 shipPos)
        {
            if (Triggered || scenario == null) return false;
            if (!InRadius(shipPos, transform.position, radius)) return false;
            Triggered = true;
            if (Entered != null) Entered(this);
            return true;
        }

        /// <summary>Applies the chosen option to PlayerState and RiskModel. Safe to call once.</summary>
        public ChallengeResult Choose(int index, PlayerState state)
        {
            ChallengeResult res = new ChallengeResult();
            if (Resolved || scenario == null || index < 0 || index >= scenario.options.Count || state == null) return res;
            ChallengeOption o = scenario.options[index];
            if (!ChallengeModel.CanAfford(o, state)) return res;
            Resolved = true;
            res = ChallengeModel.Apply(o, state);
            if (MissionManager.Instance != null && MissionManager.Instance.state == state) MissionManager.Instance.SetPlannedSuccess(state.plannedSuccess);
            if (Resolution != null) Resolution(this, index);
            return res;
        }
    }

    /// <summary>The four challenge sites in the flight sector, placed in a seeded random order.</summary>
    public class ChallengeSiteSet : MonoBehaviour
    {
        // [game balance] layout slots (metres from the dock), inside the sector's soft radius.
        public static readonly Vector3[] Slots =
        {
            new Vector3(3500f, 300f, 5000f), new Vector3(-4500f, -400f, 7000f),
            new Vector3(6500f, 800f, -2500f), new Vector3(-2500f, 500f, -6000f)
        };

        public int Seed { get; private set; }
        public List<ChallengeSiteComponent> Sites { get { return _sites; } }

        readonly List<ChallengeSiteComponent> _sites = new List<ChallengeSiteComponent>();
        Transform _ship;
        M3Hud _hud;
        ChallengePanel _panel;
        bool _active;

        public void Setup(Transform ship, M3Hud hud, Transform sectorRoot, int seed)
        {
            _ship = ship;
            _hud = hud;
            Seed = seed;
            List<ChallengeScenario> run = ChallengeModel.LoadRun(seed);
            _panel = ChallengePanel.Create(transform);
            for (int i = 0; i < run.Count; i++)
            {
                Transform b = new GameObject("Challenge_" + run[i].kind).transform;
                b.SetParent(sectorRoot, false);
                b.localPosition = Slots[i % Slots.Length];
                Color c = UIFactory.Warn;
                GreyboxKit.Part(PrimitiveType.Sphere, "Core", b, Vector3.zero, Vector3.one * 16f, c, true);
                GreyboxKit.Part(PrimitiveType.Cylinder, "Ring", b, Vector3.zero, new Vector3(80f, 0.6f, 80f), new Vector3(90f, 0f, 0f), c * 0.8f, true);
                b.gameObject.AddComponent<SlowSpin>();
                ChallengeSiteComponent site = b.gameObject.AddComponent<ChallengeSiteComponent>();
                site.scenario = run[i];
                site.Entered += OnEntered;
                _sites.Add(site);
            }
        }

        public void SetActive(bool on)
        {
            if (_hud == null || on == _active) return;
            _active = on;
            for (int i = 0; i < _sites.Count; i++)
            {
                if (on && !_sites[i].Triggered) _hud.AddMarker(_sites[i].transform, "ALERT " + (i + 1));
                else _hud.RemoveMarker(_sites[i].transform);
            }
            if (!on) _panel.Hide();
        }

        /// <summary>Narration clip / subtitle id for a site kind (see narration_script.csv).</summary>
        public static string NarrationId(ChallengeKind k)
        {
            switch (k)
            {
                case ChallengeKind.SolarStorm: return "site_a_solar";
                case ChallengeKind.Asteroid: return "site_b_asteroid";
                case ChallengeKind.Blackout: return "site_c_blackout";
                default: return "site_d_power";
            }
        }

        void OnEntered(ChallengeSiteComponent site)
        {
            _hud.RemoveMarker(site.transform);
            if (site.scenario != null) NarrationPlayer.Play(NarrationId(site.scenario.kind));
            _panel.Show(site);
        }

        void Update()
        {
            if (!_active || _ship == null) return;
            for (int i = 0; i < _sites.Count; i++)
            {
                if (_panel.Busy) return;   // one card at a time
                _sites[i].Tick(_ship.position);
            }
        }
    }

    /// <summary>Three steps: NASA DATA card, the decision, the debrief line.</summary>
    public class ChallengePanel : MonoBehaviour
    {
        enum Step { Hidden, Card, Choose, Debrief }

        Step _step;
        ChallengeSiteComponent _site;
        GameObject _root;
        Text _body;
        Button[] _buttons = new Button[3];
        Text[] _buttonText = new Text[3];
        Button _next;
        Text _nextText;

        public bool Busy { get { return _step != Step.Hidden; } }

        public static ChallengePanel Create(Transform parent)
        {
            ChallengePanel p = new GameObject("ChallengePanel").AddComponent<ChallengePanel>();
            p.transform.SetParent(parent, false);
            p.Build();
            return p;
        }

        void Build()
        {
            UIFactory.EnsureEventSystem();
            Canvas canvas = UIFactory.CreateCanvas("ChallengeHud", 45, transform);
            Image box = M3Ui.Box("Card", canvas.transform, UIFactory.PanelColor, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(900f, 640f));
            _root = box.gameObject;
            _body = M3Ui.Label("Body", box.transform, "", 19, UIFactory.TextColor, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(864f, 450f));
            _body.supportRichText = true;
            for (int i = 0; i < _buttons.Length; i++)
            {
                int idx = i;
                Button b = UIFactory.CreateButton("Option" + i, box.transform, "", () => OnOption(idx));
                UIFactory.SetCenter((RectTransform)b.transform, 0f, -70f - i * 66f, 860f, 58f);
                _buttons[i] = b;
                _buttonText[i] = b.GetComponentInChildren<Text>();
                if (_buttonText[i] != null) _buttonText[i].fontSize = 18;
            }
            _next = UIFactory.CreateButton("Next", box.transform, "Continue  [Enter]", OnNext);
            UIFactory.SetCenter((RectTransform)_next.transform, 0f, -270f, 360f, 56f);
            _nextText = _next.GetComponentInChildren<Text>();
            _root.SetActive(false);
        }

        public void Show(ChallengeSiteComponent site)
        {
            _site = site;
            MarkUse(site.scenario);
            _step = Step.Card;
            _root.SetActive(true);
            Refresh(null);
        }

        static void MarkUse(ChallengeScenario s)
        {
            if (s == null || string.IsNullOrEmpty(s.dataFile)) return;
            var facts = new StringBuilder();
            for (int i = 0; i < s.facts.Count && i < 3; i++) { if (i > 0) facts.Append("; "); facts.Append(s.facts[i].Key).Append(": ").Append(s.facts[i].Value); }
            NasaData.Mark(s.dataFile, MissionIds.M3, facts.ToString());
            if (s.sourceUrl != null && s.sourceUrl.Contains("deepspace.jpl.nasa.gov")) NasaData.Mark("dsn_notes.csv", MissionIds.M3);
        }

        public void Hide()
        {
            _step = Step.Hidden;
            if (_root != null) _root.SetActive(false);
        }

        public static string CardText(ChallengeScenario s)
        {
            StringBuilder sb = new StringBuilder(700);
            sb.Append("<b><size=26>").Append(s.title).Append("</size></b>\n");
            sb.Append("<color=#7FE0FF><b>NASA DATA</b></color>  ").Append(s.dataset).Append('\n');
            sb.Append("<color=#8899AA>").Append(s.dataFile).Append("  (snapshot retrieved ").Append(s.retrieved).Append(", not live)</color>\n\n");
            for (int i = 0; i < s.facts.Count; i++) sb.Append(s.facts[i].Key).Append(":  <b>").Append(s.facts[i].Value).Append("</b>\n");
            sb.Append("\n<b>Why NASA cares:</b> ").Append(s.why).Append("\n\n");
            sb.Append("<color=#8899AA>Source: ").Append(s.sourceUrl).Append("</color>\n\n");
            sb.Append(s.intro);
            return sb.ToString();
        }

        void Refresh(ChallengeResult? result)
        {
            for (int i = 0; i < _buttons.Length; i++) _buttons[i].gameObject.SetActive(false);
            _next.gameObject.SetActive(false);
            ChallengeScenario s = _site.scenario;
            PlayerState st = PlayerState.Current;
            if (_step == Step.Card)
            {
                _body.text = CardText(s);
                _nextText.text = "Decide  [Enter]";
                _next.gameObject.SetActive(true);
                UIFactory.SetCenter((RectTransform)_next.transform, 0f, -270f, 360f, 56f);
            }
            else if (_step == Step.Choose)
            {
                _body.text = "<b><size=24>" + s.title + "</size></b>\n\nChoose (keys 1-3 or click). Costs are shown first. Lighter choices cost resources, riskier ones cost hull and lower the mission's success chance.";
                for (int i = 0; i < s.options.Count && i < _buttons.Length; i++)
                {
                    bool ok = st == null || ChallengeModel.CanAfford(s.options[i], st);
                    _buttons[i].gameObject.SetActive(true);
                    _buttons[i].interactable = ok;
                    _buttonText[i].text = (i + 1) + ".  " + s.options[i].label + "   |   " + s.options[i].costNote + (ok ? "" : "   (not enough resources)");
                }
            }
            else if (_step == Step.Debrief)
            {
                int c = _chosen;
                _body.text = "<b><size=24>" + s.title + "</size></b>\n\nYou chose: <b>" + s.options[c].label + "</b>\n\n" + _lastSummary
                    + "\n\n<b>Debrief:</b> " + s.options[c].debrief + "\n\n<color=#8899AA>Why NASA cares: " + s.why + "</color>";
                _nextText.text = "Back to flight  [Enter]";
                _next.gameObject.SetActive(true);
                UIFactory.SetCenter((RectTransform)_next.transform, 0f, -270f, 360f, 56f);
            }
        }

        int _chosen;
        string _lastSummary = "";

        void OnOption(int i)
        {
            if (_step != Step.Choose || _site == null) return;
            PlayerState st = PlayerState.Current;
            ChallengeResult r = _site.Choose(i, st);
            if (r.summary == null) return;   // not affordable or already resolved
            _chosen = i;
            _lastSummary = r.summary;
            _step = Step.Debrief;
            Refresh(r);
        }

        void OnNext()
        {
            if (_step == Step.Card) { _step = Step.Choose; Refresh(null); }
            else if (_step == Step.Debrief) Hide();
        }

        void Update()
        {
            if (_step == Step.Hidden) return;
            if (ShipInput.ConfirmPressed()) OnNext();
            else if (_step == Step.Choose)
            {
                int n = ShipInput.NumberPressed();
                if (n >= 1 && n <= 3) OnOption(n - 1);
            }
        }
    }
}
