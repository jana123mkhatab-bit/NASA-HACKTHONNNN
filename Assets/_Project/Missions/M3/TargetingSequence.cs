using System;
using LastSignal.Core;
using LastSignal.Ship;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.M3
{
    /// <summary>
    /// Relay strike: keep the ship's nose on the relay until the lock bar fills, then press F.
    /// A beam fires and the relay breaks apart. Firing costs a little power.
    /// </summary>
    public class TargetingSequence : MonoBehaviour
    {
        [Header("GAME-BALANCE values")]
        public float lockAngle = 7f;
        public float lockRange = 450f;
        public float lockSeconds = 2f;
        public float firePowerCost = 5f;

        public bool IsActive { get; private set; }

        ShipController _ship;
        RelaySatellite _relay;
        Action _onFired;
        float _lock;
        float _noLockMsg;

        Canvas _canvas;
        RectTransform _canvasRect;
        Text _reticle;
        RectTransform _bracket;
        Image _lockFill;
        Text _status;

        void Awake()
        {
            _canvas = UIFactory.CreateCanvas("Targeting", 32, transform);
            _canvasRect = (RectTransform)_canvas.transform;
            _reticle = M3Ui.Label("Reticle", _canvas.transform, "( + )", 34, UIFactory.Accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 50f));
            _bracket = M3Ui.Bracket("RelayBracket", _canvas.transform, UIFactory.Warn, 3f);
            _bracket.sizeDelta = new Vector2(90f, 90f);

            Image back;
            _lockFill = UIFactory.CreateBar("Lock", _canvas.transform, UIFactory.Warn, out back);
            M3Ui.Place(back.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(420f, 18f));
            M3Ui.Label("LockLabel", _canvas.transform, "LOCK", 18, UIFactory.Muted, TextAnchor.MiddleRight,
                new Vector2(0.5f, 0.5f), new Vector2(-260f, -170f), new Vector2(80f, 24f));
            _status = M3Ui.Label("Status", _canvas.transform, "", 26, UIFactory.TextColor, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(900f, 40f));
            _canvas.gameObject.SetActive(false);
        }

        public void Begin(ShipController ship, RelaySatellite relay, Action onFired)
        {
            _ship = ship;
            _relay = relay;
            _onFired = onFired;
            _lock = 0f;
            IsActive = true;
            _canvas.gameObject.SetActive(true);
        }

        public void Cancel()
        {
            IsActive = false;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!IsActive || _ship == null || _relay == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 toRelay = _relay.transform.position - _ship.transform.position;
            float dist = toRelay.magnitude;
            float angle = Vector3.Angle(_ship.transform.forward, toRelay);
            bool aligned = angle <= lockAngle && dist <= lockRange;

            if (aligned) _lock = Mathf.Min(1f, _lock + Time.deltaTime / lockSeconds);
            else _lock = Mathf.Max(0f, _lock - Time.deltaTime / (lockSeconds * 0.7f));
            bool locked = _lock >= 1f;

            // Reticle where the nose points, bracket on the relay.
            bool on;
            _reticle.rectTransform.anchoredPosition = M3Ui.WorldToCanvas(cam, _canvasRect, _ship.transform.position + _ship.transform.forward * 300f, 60f, out on);
            _bracket.anchorMin = _bracket.anchorMax = _bracket.pivot = new Vector2(0.5f, 0.5f);
            _bracket.anchoredPosition = M3Ui.WorldToCanvas(cam, _canvasRect, _relay.transform.position, 60f, out on);
            Color bc = locked ? UIFactory.Bad : (aligned ? UIFactory.Good : UIFactory.Warn);
            foreach (Image e in _bracket.GetComponentsInChildren<Image>()) e.color = bc;
            _reticle.color = aligned ? UIFactory.Good : UIFactory.Accent;

            UIFactory.SetFill(_lockFill, _lock);
            _lockFill.color = locked ? UIFactory.Bad : UIFactory.Warn;

            if (Time.time < _noLockMsg) _status.text = "NO LOCK. Keep the nose on the relay.";
            else if (locked) _status.text = "LOCKED. Press F to fire.";
            else if (dist > lockRange) _status.text = "Too far: get within " + lockRange.ToString("0") + " m (" + dist.ToString("0") + " m)";
            else _status.text = aligned ? "Hold steady..." : "Turn the nose onto the relay (" + angle.ToString("0") + "° off)";

            if (ShipInput.FirePressed())
            {
                if (locked) Fire();
                else _noLockMsg = Time.time + 1.5f;
            }
        }

        void Fire()
        {
            IsActive = false;
            _canvas.gameObject.SetActive(false);
            PlayerState s = PlayerState.Current;
            s.SetPower(s.power - firePowerCost);

            LineRenderer beam = GreyboxKit.Line("Beam", null, new Color(1f, 0.45f, 0.3f), 0.8f, 2);
            beam.SetPosition(0, _ship.transform.position + _ship.transform.forward * 7f);
            beam.SetPosition(1, _relay.transform.position);
            Destroy(beam.gameObject, 0.35f);

            if (ChaseCamera.Instance != null) ChaseCamera.Instance.Shake(0.3f, 0.4f);
            _relay.Explode(true);
            if (_onFired != null) _onFired();
        }
    }
}
