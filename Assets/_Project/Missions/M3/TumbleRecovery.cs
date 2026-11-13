using System;
using LastSignal.Core;
using LastSignal.Ship;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.M3
{
    /// <summary>
    /// Loss of control after the ram. The stabilizer and main engine are offline; the player must cancel the spin
    /// by hand with the RCS keys. Bars show the spin on each axis and which key pushes against it.
    /// Fails if the time runs out or the spin gets much worse; the Director then fails the mission and
    /// Retry restarts at the impact checkpoint.
    /// </summary>
    public class TumbleRecovery : MonoBehaviour
    {
        [Header("GAME-BALANCE values")]
        public float timeLimit = 25f;
        public float successSpin = 0.25f;
        public float holdSeconds = 1.5f;
        public float failSpin = 5f;
        public float rcsAuthority = 0.8f;
        const float BarRange = 2.5f;

        public bool IsActive { get; private set; }

        ShipController _ship;
        Action _onSuccess;
        Action<string> _onFail;
        float _t;
        float _hold;

        Canvas _canvas;
        Text _title;
        Text _timer;
        Text _total;
        readonly Image[] _fills = new Image[3];
        readonly Text[] _hints = new Text[3];
        static readonly string[] AxisNames = { "PITCH", "YAW", "ROLL" };

        void Awake()
        {
            _canvas = UIFactory.CreateCanvas("Tumble", 33, transform);
            Transform c = _canvas.transform;
            _title = M3Ui.Label("Title", c, M3Lines.TumbleTitle, 34, UIFactory.Bad, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1200f, 50f));
            M3Ui.Label("Help", c, M3Lines.TumbleHelp, 22, UIFactory.TextColor, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 205f), new Vector2(1200f, 34f));
            _timer = M3Ui.Label("Timer", c, "", 24, UIFactory.Warn, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(600f, 32f));

            Image panel = M3Ui.Box("Panel", c, UIFactory.PanelColor, new Vector2(0.5f, 0f), new Vector2(0f, 300f), new Vector2(860f, 190f));
            for (int i = 0; i < 3; i++)
            {
                float y = -18f - i * 44f;
                M3Ui.Label(AxisNames[i], panel.transform, AxisNames[i], 20, UIFactory.Muted, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(16f, y), new Vector2(80f, 30f));
                Image back = M3Ui.Box("Back", panel.transform, UIFactory.BarBack, new Vector2(0f, 1f), new Vector2(100f, y - 6f), new Vector2(320f, 18f));
                M3Ui.Box("Zero", back.transform, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 24f));
                _fills[i] = UIFactory.CreatePanel("Fill", back.transform, UIFactory.Warn);
                _fills[i].raycastTarget = false;
                _hints[i] = M3Ui.Label("Hint", panel.transform, "", 20, UIFactory.TextColor, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(440f, y), new Vector2(410f, 30f));
            }
            _total = M3Ui.Label("Total", panel.transform, "", 20, UIFactory.TextColor, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(16f, -150f), new Vector2(820f, 30f));
            _canvas.gameObject.SetActive(false);
        }

        public void Begin(ShipController ship, Action onSuccess, Action<string> onFail)
        {
            _ship = ship;
            _onSuccess = onSuccess;
            _onFail = onFail;
            _t = 0f;
            _hold = 0f;
            IsActive = true;

            ship.ControlEnabled = true;
            ship.StabilizerOnline = false;
            ship.FlightAssist = false;
            ship.MainEngineOnline = false;
            ship.RcsAuthority = rcsAuthority;
            if (ChaseCamera.Instance != null) ChaseCamera.Instance.SetStable(true);
            _canvas.gameObject.SetActive(true);
        }

        void Update()
        {
            if (!IsActive || _ship == null) return;
            _t += Time.deltaTime;
            Vector3 spin = _ship.LocalSpin;   // x pitch (+ nose up), y yaw (+ right), z roll (+ right)
            float mag = spin.magnitude;

            SetAxis(0, spin.x, "nose rising", "nose dropping", "DOWN arrow", "UP arrow");
            SetAxis(1, spin.y, "turning right", "turning left", "A", "D");
            SetAxis(2, spin.z, "rolling right", "rolling left", "Q", "E");

            float left = Mathf.Max(0f, timeLimit - _t);
            _timer.text = left.ToString("0") + " s left";
            _title.color = Mathf.Repeat(Time.time, 0.8f) < 0.4f ? UIFactory.Bad : UIFactory.Warn;
            _total.text = "SPIN " + mag.ToString("0.00") + " rad/s   (target below " + successSpin.ToString("0.00") + ")"
                + (_hold > 0f ? "   HOLD STEADY " + Mathf.Max(0f, holdSeconds - _hold).ToString("0.0") + " s" : "");
            _total.color = mag < successSpin ? UIFactory.Good : UIFactory.TextColor;

            if (mag < successSpin) _hold += Time.deltaTime;
            else _hold = 0f;

            if (_hold >= holdSeconds) { Finish(true, null); return; }
            if (_t >= timeLimit || mag >= failSpin) Finish(false, M3Lines.FailTumble);
        }

        void SetAxis(int i, float rate, string posWord, string negWord, string counterPos, string counterNeg)
        {
            float v = Mathf.Clamp(rate / BarRange, -1f, 1f);
            RectTransform rt = _fills[i].rectTransform;
            rt.anchorMin = new Vector2(v >= 0f ? 0.5f : 0.5f + v * 0.5f, 0f);
            rt.anchorMax = new Vector2(v >= 0f ? 0.5f + v * 0.5f : 0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            bool steady = Mathf.Abs(rate) < successSpin * 0.6f;
            _fills[i].color = steady ? UIFactory.Good : (Mathf.Abs(rate) > 1.5f ? UIFactory.Bad : UIFactory.Warn);
            _hints[i].text = steady ? "steady" : (rate > 0f ? posWord + "  ->  press " + counterPos : negWord + "  ->  press " + counterNeg);
            _hints[i].color = steady ? UIFactory.Good : UIFactory.TextColor;
        }

        void Finish(bool success, string reason)
        {
            IsActive = false;
            _canvas.gameObject.SetActive(false);
            if (success)
            {
                _ship.StabilizerOnline = true;
                _ship.FlightAssist = true;
                _ship.MainEngineOnline = true;
                _ship.RcsAuthority = 1f;
                if (ChaseCamera.Instance != null) ChaseCamera.Instance.SetStable(false);
                if (_onSuccess != null) _onSuccess();
            }
            else
            {
                _ship.ControlEnabled = false;
                if (_onFail != null) _onFail(reason);
            }
        }
    }
}
