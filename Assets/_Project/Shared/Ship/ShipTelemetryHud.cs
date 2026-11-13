using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Ship
{
    /// <summary>
    /// Bottom-right telemetry: mass, speed, thrust and the live "a = F / m" line, plus a controls hint bottom-left.
    /// Teaches why the ship speeds up when it gets lighter. Usable in M3 and M4: ShipTelemetryHud.Create(ship).
    /// </summary>
    public class ShipTelemetryHud : MonoBehaviour
    {
        public ShipController ship;
        public bool showControls = true;

        Text _main;
        Text _formula;
        Text _controls;
        GameObject _root;

        public const string ControlsText =
            "W/S thrust   A/D yaw   Up/Down pitch   Q/E roll\nSpace/Ctrl up/down   X stabilize   Shift boost   F fire";

        public static ShipTelemetryHud Create(ShipController ship)
        {
            GameObject go = new GameObject("ShipTelemetryHud");
            ShipTelemetryHud hud = go.AddComponent<ShipTelemetryHud>();
            hud.ship = ship;
            return hud;
        }

        void Awake()
        {
            Canvas canvas = UIFactory.CreateCanvas("Telemetry", 40, transform);
            _root = canvas.gameObject;

            Image panel = UIFactory.CreatePanel("Panel", canvas.transform, UIFactory.PanelColor);
            panel.raycastTarget = false;
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1f, 0f);
            prt.anchoredPosition = new Vector2(-24f, 24f);
            prt.sizeDelta = new Vector2(520f, 132f);

            _main = UIFactory.CreateText("Main", panel.transform, "", 22, UIFactory.TextColor, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(_main.rectTransform, 16f, 10f, 490f, 30f);
            _formula = UIFactory.CreateText("Formula", panel.transform, "", 24, UIFactory.Accent, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(_formula.rectTransform, 16f, 46f, 490f, 34f);
            Text note = UIFactory.CreateText("Note", panel.transform, "Thrust and mass are game-balance values.", 16, UIFactory.Muted, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(note.rectTransform, 16f, 96f, 490f, 24f);

            _controls = UIFactory.CreateText("Controls", canvas.transform, ControlsText, 18, UIFactory.Muted, TextAnchor.LowerLeft);
            RectTransform crt = _controls.rectTransform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0f, 0f);
            crt.anchoredPosition = new Vector2(24f, 24f);
            crt.sizeDelta = new Vector2(760f, 60f);
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }

        void Update()
        {
            if (ship == null) ship = ShipController.Active;
            if (ship == null) return;
            _controls.gameObject.SetActive(showControls);

            float mass = ship.CurrentMass;
            float shownThrust = ship.CurrentThrust > 1f ? ship.CurrentThrust : ship.maxThrust * ship.ThrustMultiplier;
            _main.text = "MASS " + (mass / 1000f).ToString("0.0") + " t     SPEED " + ship.Speed.ToString("0") + " m/s     THRUST "
                + (ship.CurrentThrust / 1000f).ToString("0") + " kN";
            _formula.text = ShipMassModel.Explain(shownThrust, mass) + (ship.CurrentThrust > 1f ? "" : "  (at full)");
        }
    }
}
