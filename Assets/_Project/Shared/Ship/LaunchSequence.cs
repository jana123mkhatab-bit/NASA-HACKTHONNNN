using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Ship
{
    /// <summary>
    /// Countdown, release and an automatic burn, then control goes to the player.
    /// M3 uses Forward (departure from the dock). M4 can use Up (takeoff from the planet).
    ///
    ///     GetComponent&lt;LaunchSequence&gt;().Begin(() => Debug.Log("player has control"));
    /// </summary>
    [RequireComponent(typeof(ShipController))]
    public class LaunchSequence : MonoBehaviour
    {
        public enum BurnDirection { Forward, Up }

        public BurnDirection burnDirection = BurnDirection.Forward;
        public int countdownFrom = 3;
        public float burnDuration = 2.5f;
        [Range(0f, 1.6f)] public float burnThrottle = 1f;
        public bool showCountdownUI = true;

        public bool IsRunning { get; private set; }

        /// <summary>Raised with "3", "2", "1", "IGNITION", "RELEASE" / "LIFTOFF", "" (done).</summary>
        public event Action<string> Status;

        ShipController _ship;
        Canvas _canvas;
        Text _text;

        void Awake()
        {
            _ship = GetComponent<ShipController>();
        }

        public void Begin(Action onDone)
        {
            if (IsRunning) return;
            StartCoroutine(Run(onDone));
        }

        IEnumerator Run(Action onDone)
        {
            IsRunning = true;
            _ship.Hold(true);
            _ship.ControlEnabled = false;

            for (int i = countdownFrom; i >= 1; i--)
            {
                Show(i.ToString(), UIFactory.TextColor);
                if (ChaseCamera.Instance != null) ChaseCamera.Instance.Shake(0.05f * (countdownFrom - i + 1), 0.9f);
                yield return new WaitForSeconds(1f);
            }

            Show("IGNITION", UIFactory.Warn);
            if (ChaseCamera.Instance != null) ChaseCamera.Instance.Shake(0.35f, 1f);
            yield return new WaitForSeconds(0.6f);

            if (burnDirection == BurnDirection.Up && _ship.Mode == ShipMode.Landed) _ship.SetMode(ShipMode.Atmosphere);
            _ship.Hold(false);
            Show(burnDirection == BurnDirection.Up ? "LIFTOFF" : "RELEASE", UIFactory.Good);

            ShipInputFrame burn = new ShipInputFrame();
            if (burnDirection == BurnDirection.Up) burn.lift = Mathf.Clamp01(burnThrottle);
            else burn.thrust = Mathf.Clamp01(burnThrottle);
            burn.boost = burnThrottle > 1f;
            _ship.InputOverride = () => burn;

            float t = 0f;
            while (t < burnDuration)
            {
                t += Time.deltaTime;
                if (ChaseCamera.Instance != null) ChaseCamera.Instance.Shake(0.15f, 0.1f);
                yield return null;
            }

            _ship.InputOverride = null;
            _ship.ControlEnabled = true;
            Show("", UIFactory.TextColor);
            IsRunning = false;
            if (onDone != null) onDone();
        }

        void Show(string text, Color color)
        {
            if (Status != null) Status(text);
            if (!showCountdownUI) return;
            if (_canvas == null)
            {
                _canvas = UIFactory.CreateCanvas("LaunchCountdown", 60, transform);
                _text = UIFactory.CreateText("Countdown", _canvas.transform, "", 120, UIFactory.TextColor, TextAnchor.MiddleCenter);
                UIFactory.SetCenter(_text.rectTransform, 0f, 140f, 1200f, 180f);
                _canvas.transform.SetParent(null, false);   // keep the UI upright even when the ship turns
            }
            _text.text = text;
            _text.color = color;
            _canvas.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
        }
    }
}
