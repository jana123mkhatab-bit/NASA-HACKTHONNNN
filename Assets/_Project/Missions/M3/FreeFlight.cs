using System;
using System.Collections.Generic;
using System.Text;
using LastSignal.Ship;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.M3
{
    /// <summary>
    /// Free 3D flight in the M3 sector (no rails): real inertia with a brake, soft sector bounds that turn the ship
    /// back, a speed / distance nav panel, a top-down radar map pointing at every objective, four NASA data sites
    /// (challenge_sites.csv) and a rescue / retry choice if fuel or power runs out, so the player can never soft-lock.
    /// M3Director switches it on for the open-flight phase.
    ///
    /// NOTE: radii, speeds, drains and rescue costs below are GAME-BALANCE values, not NASA data.
    /// </summary>
    public class FreeFlight : MonoBehaviour
    {
        [Header("Sector (GAME-BALANCE values)")]
        public float softRadius = 30000f;
        public float hardRadius = 36000f;
        public float boundsStrength = 60f;
        public float cruiseSpeed = 400f;
        public float boostPowerPerSecond = 2f;
        public float arrivalRadius = 300f;
        public float radarRange = 36000f;

        [Header("Rescue tug (GAME-BALANCE values)")]
        public float rescueFuel = 40f;
        public float rescuePower = 30f;
        public float rescueSupplyCost = 8f;

        public bool Active { get; private set; }
        public bool Stranded { get; private set; }
        public List<ChallengeSite> Sites { get { return _sites; } }

        /// <summary>Raised when the ship reaches a NASA site (hook for the site challenges).</summary>
        public event Action<ChallengeSite> SiteReached;

        class Blip
        {
            public Transform target;
            public Vector3 direction;   // used when target is null (the Sun)
            public string label;
            public Color color;
            public RectTransform rect;
            public Image dot;
            public Text text;
        }

        ShipController _ship;
        M3Hud _hud;
        M3World _world;
        Transform _relay;
        Action<string> _retry;
        List<ChallengeSite> _sites = new List<ChallengeSite>();

        float _savedDamping;
        float _savedMaxSpeed;
        float _savedPowerDrain;

        GameObject _root;
        Text _nav;
        RectTransform _radar;
        readonly List<Blip> _blips = new List<Blip>();
        GameObject _card;
        Text _cardText;
        float _cardHideAt;
        GameObject _strandedPanel;
        Text _strandedTitle;
        string _strandedReason;
        float _navRefresh;

        // ---------------------------------------------------------------- setup

        public void Setup(ShipController ship, M3Hud hud, M3World world, Transform relay, Action<string> retry)
        {
            _ship = ship;
            _hud = hud;
            _world = world;
            _relay = relay;
            _retry = retry;
            _sites = ChallengeSite.LoadAll();
            BuildBeacons();
            BuildUi();
            _root.SetActive(false);
        }

        Vector3 AnchorPosition(string anchor)
        {
            if (anchor == "earth" && _world.Earth != null) return _world.Earth.localPosition;
            if (anchor == "moon" && _world.Moon != null) return _world.Moon.localPosition;
            return Vector3.zero;   // dock = sector origin
        }

        void BuildBeacons()
        {
            Color c = UIFactory.Accent;
            for (int i = 0; i < _sites.Count; i++)
            {
                ChallengeSite s = _sites[i];
                Transform b = new GameObject("Site_" + s.id).transform;
                b.SetParent(_world.transform, false);
                b.localPosition = AnchorPosition(s.anchor) + s.offset;
                GreyboxKit.Part(PrimitiveType.Sphere, "Core", b, Vector3.zero, Vector3.one * 14f, c, true);
                GreyboxKit.Part(PrimitiveType.Cylinder, "Ring", b, Vector3.zero, new Vector3(70f, 0.6f, 70f), new Vector3(90f, 0f, 0f), c * 0.8f, true);
                GreyboxKit.Part(PrimitiveType.Cylinder, "Mast", b, new Vector3(0f, 40f, 0f), new Vector3(1.5f, 40f, 1.5f), c * 0.6f, true);
                b.gameObject.AddComponent<SlowSpin>();
                s.beacon = b;
            }
        }

        void BuildUi()
        {
            UIFactory.EnsureEventSystem();
            Canvas canvas = UIFactory.CreateCanvas("FreeFlightHud", 35, transform);
            _root = canvas.gameObject;

            // Nav panel, top-left under Core's HUD strip.
            Image nav = M3Ui.Box("Nav", canvas.transform, UIFactory.PanelColor, new Vector2(0f, 1f), new Vector2(24f, -272f), new Vector2(640f, 200f));
            _nav = M3Ui.Label("NavText", nav.transform, "", 19, UIFactory.TextColor, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(612f, 184f));

            // Radar map, bottom-left above the controls hint. Top-down, rotated with the ship's heading.
            Image radarBg = M3Ui.Box("Radar", canvas.transform, new Color(0.02f, 0.05f, 0.09f, 0.82f), new Vector2(0f, 0f), new Vector2(24f, 96f), new Vector2(260f, 260f));
            _radar = radarBg.rectTransform;
            RectTransform frame = M3Ui.Bracket("RadarFrame", _radar, UIFactory.Accent * 0.7f, 2f);
            UIFactory.SetStretch(frame, 0f, 0f, 0f, 0f);
            Text ship = M3Ui.Label("Ship", _radar, "^", 22, UIFactory.Good, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            ship.raycastTarget = false;
            M3Ui.Label("RadarTitle", _radar, "SECTOR MAP  (nose = up)", 14, UIFactory.Muted, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(250f, 20f));

            AddBlip(_world.Earth, "EARTH", new Color(0.4f, 0.7f, 1f));
            AddBlip(_world.Moon, "MOON", new Color(0.8f, 0.8f, 0.82f));
            AddBlip(_world.Dock, "DOCK", UIFactory.Muted);
            if (_relay != null) AddBlip(_relay, "RELAY", UIFactory.Warn);
            for (int i = 0; i < _sites.Count; i++) AddBlip(_sites[i].beacon, "NASA " + (i + 1), UIFactory.Accent);
            Blip sun = AddBlip(null, "SUN", new Color(1f, 0.85f, 0.4f));
            sun.direction = _world.SunDirection;

            // Site card, right side.
            Image card = M3Ui.Box("SiteCard", canvas.transform, UIFactory.PanelColor, new Vector2(1f, 0.5f), new Vector2(-24f, 40f), new Vector2(620f, 430f));
            _card = card.gameObject;
            _cardText = M3Ui.Label("CardText", card.transform, "", 18, UIFactory.TextColor, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(588f, 406f));
            _card.SetActive(false);

            // Stranded panel, centre.
            Image st = M3Ui.Box("Stranded", canvas.transform, new Color(0.08f, 0.02f, 0.02f, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(760f, 280f));
            _strandedPanel = st.gameObject;
            _strandedTitle = M3Ui.Label("Title", st.transform, "", 30, UIFactory.Bad, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(720f, 44f));
            M3Ui.Label("Body", st.transform,
                "The ship is drifting. Choose:\nRescue tug tows you back to the dock and refuels you (costs " + rescueSupplyCost.ToString("0")
                + " supplies) [game balance].\nRetry restarts the mission.", 20, UIFactory.TextColor, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(720f, 110f));
            Button rescue = UIFactory.CreateButton("Rescue", st.transform, "Call rescue tug  [R]", Rescue);
            UIFactory.SetCenter((RectTransform)rescue.transform, -180f, -90f, 320f, 56f);
            Button retry = UIFactory.CreateButton("Retry", st.transform, "Retry mission  [T]", Retry);
            UIFactory.SetCenter((RectTransform)retry.transform, 180f, -90f, 320f, 56f);
            _strandedPanel.SetActive(false);
        }

        Blip AddBlip(Transform target, string label, Color color)
        {
            Blip b = new Blip();
            b.target = target;
            b.label = label;
            b.color = color;
            Image dot = M3Ui.Box("Blip_" + label, _radar, color, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            b.rect = dot.rectTransform;
            b.dot = dot;
            b.rect.pivot = new Vector2(0.5f, 0.5f);
            b.text = M3Ui.Label("Label", b.rect, label, 13, color, TextAnchor.LowerCenter, new Vector2(0.5f, 1f), new Vector2(0f, 2f), new Vector2(110f, 18f));
            b.text.rectTransform.pivot = new Vector2(0.5f, 0f);
            _blips.Add(b);
            return b;
        }

        // ---------------------------------------------------------------- on / off

        public void SetActive(bool on)
        {
            if (_ship == null || on == Active) return;
            Active = on;
            _root.SetActive(on);
            if (on)
            {
                _savedDamping = _ship.assistLinearDamping;
                _savedMaxSpeed = _ship.maxSpeed;
                _savedPowerDrain = _ship.powerPerSecondBoost;
                _ship.assistLinearDamping = 0f;   // real inertia: the ship coasts until you brake
                _ship.maxSpeed = cruiseSpeed;
                _ship.powerPerSecondBoost = boostPowerPerSecond;
                for (int i = 0; i < _sites.Count; i++)
                {
                    if (!_sites[i].visited) _hud.AddMarker(_sites[i].beacon, "NASA SITE " + (i + 1));
                }
                _hud.AddMarker(_world.Moon, "MOON");
                _hud.AddMarker(_world.Earth, "EARTH");
                _hud.Comm("NAV", "Free flight. The ship keeps moving until you brake (B). Visit the NASA data sites, then reach Relay R-7.", 8f);
            }
            else
            {
                _ship.assistLinearDamping = _savedDamping;
                _ship.maxSpeed = _savedMaxSpeed;
                _ship.powerPerSecondBoost = _savedPowerDrain;
                for (int i = 0; i < _sites.Count; i++) _hud.RemoveMarker(_sites[i].beacon);
                _hud.RemoveMarker(_world.Moon);
                _hud.RemoveMarker(_world.Earth);
                if (Stranded) { Stranded = false; _ship.ControlEnabled = true; }
                _strandedPanel.SetActive(false);
                _card.SetActive(false);
            }
        }

        // ---------------------------------------------------------------- simulation

        Vector3 SectorCenter { get { return FloatingOrigin.ToWorld(Vector3.zero); } }

        void FixedUpdate()
        {
            if (!Active || _ship == null || _ship.Body == null || _ship.Body.isKinematic) return;
            Vector3 pos = _ship.transform.position;
            Vector3 center = SectorCenter;
            Vector3 a = FlightModel.BoundsAcceleration(pos, center, softRadius, hardRadius, boundsStrength);
            if (a == Vector3.zero) return;
            _ship.Body.AddForce(a, ForceMode.Acceleration);
            // Gently swing the nose back toward the sector too, so the player sees where "inside" is.
            float f = FlightModel.BoundsFactor((pos - center).magnitude, softRadius, hardRadius);
            Quaternion want = Quaternion.LookRotation(center - pos, _ship.transform.up);
            _ship.Body.MoveRotation(Quaternion.RotateTowards(_ship.Body.rotation, want, 12f * f * Time.fixedDeltaTime));
        }

        void Update()
        {
            if (!Active || _ship == null) return;
            PlayerState s = PlayerState.Current;

            if (!Stranded && FlightModel.IsStranded(s.fuel, s.power)) EnterStranded(s.fuel <= 0f ? "FUEL" : "POWER");
            if (Stranded)
            {
                if (ShipInput.RescuePressed()) Rescue();
                else if (ShipInput.RetryPressed()) Retry();
            }

            for (int i = 0; i < _sites.Count; i++)
            {
                ChallengeSite site = _sites[i];
                if (site.visited || site.beacon == null) continue;
                if ((site.beacon.position - _ship.transform.position).sqrMagnitude <= arrivalRadius * arrivalRadius) Reach(site, i);
            }

            if (_card.activeSelf && (Time.time >= _cardHideAt || ShipInput.ConfirmPressed())) _card.SetActive(false);

            UpdateRadar();
            _navRefresh -= Time.deltaTime;
            if (_navRefresh <= 0f)
            {
                _navRefresh = 0.1f;
                UpdateNav();
            }
        }

        Transform NextObjective(out string name)
        {
            Transform best = null;
            name = "";
            float bestD = float.PositiveInfinity;
            Vector3 p = _ship.transform.position;
            for (int i = 0; i < _sites.Count; i++)
            {
                if (_sites[i].visited || _sites[i].beacon == null) continue;
                float d = (_sites[i].beacon.position - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = _sites[i].beacon; name = "NASA SITE " + (i + 1) + ": " + _sites[i].name; }
            }
            if (best == null && _relay != null) { best = _relay; name = "Relay R-7"; }
            return best;
        }

        void UpdateNav()
        {
            Vector3 p = _ship.transform.position;
            Vector3 v = _ship.Velocity;
            float cap = _ship.maxSpeed;
            StringBuilder sb = new StringBuilder(512);
            sb.Append("SPEED  ").Append(v.magnitude.ToString("0")).Append(" m/s   cap ").Append(cap.ToString("0"))
              .Append(" / boost ").Append((cap * _ship.boostMultiplier).ToString("0")).Append(" [game balance]\n");

            float dEarth = _world.Earth != null ? Vector3.Distance(p, _world.Earth.position) : 0f;
            float dMoon = _world.Moon != null ? Vector3.Distance(p, _world.Moon.position) : 0f;
            sb.Append("EARTH ").Append(M3Ui.Distance(dEarth)).Append(" = ").Append(KmAtScale(dEarth))
              .Append("   MOON ").Append(M3Ui.Distance(dMoon)).Append(" = ").Append(KmAtScale(dMoon)).Append('\n');

            string name;
            Transform next = NextObjective(out name);
            if (next != null)
            {
                Vector3 to = next.position - p;
                float d = to.magnitude;
                float closing = d > 0.01f ? Vector3.Dot(v, to / d) : 0f;
                float eta = FlightModel.Eta(d, closing);
                Vector2 steer = FlightModel.SteerAngles(_ship.transform.InverseTransformDirection(to));
                sb.Append("NEXT  ").Append(name).Append('\n');
                sb.Append("      ").Append(M3Ui.Distance(d)).Append("   ETA ").Append(float.IsInfinity(eta) ? "--" : eta.ToString("0") + " s")
                  .Append("   turn ").Append(steer.x >= 0f ? "right " : "left ").Append(Mathf.Abs(steer.x).ToString("0")).Append("°, ")
                  .Append(steer.y >= 0f ? "up " : "down ").Append(Mathf.Abs(steer.y).ToString("0")).Append("°\n");
            }
            int visited = 0;
            for (int i = 0; i < _sites.Count; i++) if (_sites[i].visited) visited++;
            sb.Append("NASA SITES ").Append(visited).Append(" / ").Append(_sites.Count)
              .Append("    Map scale 1 m = ").Append(SectorLayout.KmPerUnit.ToString("0")).Append(" km [game balance]\n");

            float r = (p - SectorCenter).magnitude;
            if (r > softRadius) sb.Append("<color=#FFB347>SECTOR EDGE: nav computer is turning you back</color>");
            else sb.Append("<color=#8899AA>Positions: JPL Horizons snapshot 2026-11-13 (not live)</color>");
            _nav.text = sb.ToString();
        }

        static string KmAtScale(float units)
        {
            double km = units * (double)SectorLayout.KmPerUnit;
            return km >= 1000d ? (km / 1000d).ToString("#,0") + " thousand km" : km.ToString("0") + " km";
        }

        void UpdateRadar()
        {
            Transform t = _ship.transform;
            Quaternion yawOnly = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
            Quaternion inv = Quaternion.Inverse(yawOnly);
            float half = _radar.rect.width * 0.5f - 10f;
            for (int i = 0; i < _blips.Count; i++)
            {
                Blip b = _blips[i];
                Vector3 rel;
                if (b.target != null) rel = b.target.position - t.position;
                else rel = b.direction * radarRange * 10f;   // the Sun: direction only, always on the rim
                Vector3 local = inv * rel;
                Vector2 pt = new Vector2(local.x, local.z) / radarRange * half;
                if (pt.magnitude > half) pt = pt.normalized * half;
                b.rect.anchoredPosition = pt;
                string elev = Mathf.Abs(local.y) > 500f ? (local.y > 0f ? " ^" : " v") : "";
                if (b.text.text.Length != b.label.Length + elev.Length) b.text.text = b.label + elev;
                bool done = false;
                for (int k = 0; k < _sites.Count; k++) if (_sites[k].beacon == b.target && _sites[k].visited) done = true;
                b.dot.color = done ? b.color * 0.45f : b.color;
            }
        }

        // ---------------------------------------------------------------- sites

        void Reach(ChallengeSite site, int index)
        {
            site.visited = true;
            _hud.RemoveMarker(site.beacon);
            ShowCard(site);
            _hud.Comm("NAV", "NASA site " + (index + 1) + " logged: " + site.name, 4f);
            if (SiteReached != null) SiteReached(site);
        }

        public static string CardText(ChallengeSite site)
        {
            StringBuilder sb = new StringBuilder(640);
            sb.Append("<b><size=24>").Append(site.name).Append("</size></b>\n");
            sb.Append(NasaData.Tag(site.dataFile + ".csv")).Append("\n\n");
            for (int i = 0; i < site.values.Count; i++)
            {
                string v = site.values[i].Value;
                sb.Append(site.values[i].Key).Append(":  <b>").Append(string.IsNullOrEmpty(v) ? "missing in CSV" : v).Append("</b>\n");
            }
            sb.Append("\n<b>Why NASA cares:</b> ").Append(site.why).Append("\n\n");
            sb.Append("<color=#8899AA>Source: ").Append(site.sourceUrl).Append("\n").Append(site.balanceNote).Append("</color>\n");
            sb.Append("<color=#8899AA>Enter to close</color>");
            return sb.ToString();
        }

        void ShowCard(ChallengeSite site)
        {
            _cardText.text = CardText(site);
            var fact = new StringBuilder();
            for (int i = 0; i < site.values.Count && i < 3; i++)
            {
                if (string.IsNullOrEmpty(site.values[i].Value)) { NasaData.ReportFallback(site.dataFile + ".csv", site.name + ": a value is missing in the CSV"); continue; }
                if (fact.Length > 0) fact.Append("; ");
                fact.Append(site.values[i].Key).Append(": ").Append(site.values[i].Value);
            }
            NasaData.Mark(site.dataFile + ".csv", MissionIds.M3, fact.ToString());
            _card.SetActive(true);
            _cardHideAt = Time.time + 18f;
        }

        // ---------------------------------------------------------------- stranded: rescue or retry

        void EnterStranded(string what)
        {
            Stranded = true;
            _strandedReason = what == "FUEL" ? M3Lines.FailFuel : "Power ran out in open space.";
            _ship.ControlEnabled = false;   // no input: the ship drifts on its momentum
            _strandedTitle.text = what + " EMPTY: DRIFTING";
            _strandedPanel.SetActive(true);
            _card.SetActive(false);
        }

        public void Rescue()
        {
            if (!Stranded) return;
            PlayerState s = PlayerState.Current;
            s.SetFuel(Mathf.Max(s.fuel, rescueFuel));
            s.SetPower(Mathf.Max(s.power, rescuePower));
            s.SetSupplies(s.supplies - rescueSupplyCost);
            s.NotifyChanged();
            Vector3 dock = FloatingOrigin.ToWorld(new Vector3(0f, 0f, 60f));
            _ship.Teleport(dock, Quaternion.identity, Vector3.zero, Vector3.zero);
            if (ChaseCamera.Instance != null) ChaseCamera.Instance.SnapNow();
            _ship.ControlEnabled = true;
            Stranded = false;
            _strandedPanel.SetActive(false);
            _hud.Comm("TUG", "Rescue tug towed you back to the dock and refuelled you. Cost: " + rescueSupplyCost.ToString("0") + " supplies [game balance].", 6f);
        }

        public void Retry()
        {
            if (!Stranded) return;
            _strandedPanel.SetActive(false);
            if (_retry != null) _retry(_strandedReason);
        }
    }
}
