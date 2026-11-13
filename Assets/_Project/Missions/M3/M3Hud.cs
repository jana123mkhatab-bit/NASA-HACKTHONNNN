using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.M3
{
    /// <summary>
    /// M3 overlay: objective line (top centre), comms box (bottom centre), world markers with distance,
    /// a red damage flash and a fade to black. Sits under Core's HUD strip.
    /// </summary>
    public class M3Hud : MonoBehaviour
    {
        class Marker
        {
            public Transform target;
            public string label;
            public RectTransform rect;
            public Text text;
        }

        Canvas _canvas;
        RectTransform _canvasRect;
        Text _objective;
        Image _commBox;
        Text _commSpeaker;
        Text _commText;
        float _commHideAt;
        Image _flash;
        float _flashAlpha;
        Image _fade;
        float _fadeTarget;
        readonly List<Marker> _markers = new List<Marker>();

        public static M3Hud Create(Transform parent)
        {
            GameObject go = new GameObject("M3Hud");
            go.transform.SetParent(parent, false);
            return go.AddComponent<M3Hud>();
        }

        void Awake()
        {
            _canvas = UIFactory.CreateCanvas("M3Overlay", 30, transform);
            _canvasRect = (RectTransform)_canvas.transform;

            _objective = M3Ui.Label("Objective", _canvas.transform, "", 26, UIFactory.TextColor, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(60f, -112f), new Vector2(640f, 70f));

            _commBox = M3Ui.Box("Comms", _canvas.transform, UIFactory.PanelColor, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(980f, 120f));
            _commSpeaker = M3Ui.Label("Speaker", _commBox.transform, "", 18, UIFactory.Warn, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(18f, -10f), new Vector2(940f, 26f));
            _commText = M3Ui.Label("Text", _commBox.transform, "", 24, UIFactory.TextColor, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(18f, -38f), new Vector2(940f, 76f));
            _commBox.gameObject.SetActive(false);

            _flash = UIFactory.CreatePanel("DamageFlash", _canvas.transform, new Color(1f, 0.1f, 0.05f, 0f));
            _flash.raycastTarget = false;
            UIFactory.SetStretch(_flash.rectTransform, 0f, 0f, 0f, 0f);

            _fade = UIFactory.CreatePanel("Fade", _canvas.transform, new Color(0f, 0f, 0f, 0f));
            _fade.raycastTarget = false;
            UIFactory.SetStretch(_fade.rectTransform, 0f, 0f, 0f, 0f);
        }

        public void SetObjective(string text)
        {
            _objective.text = text;
        }

        /// <summary>Shows a radio message. seconds &lt;= 0 keeps it until the next message.</summary>
        public void Comm(string speaker, string text, float seconds)
        {
            _commSpeaker.text = speaker;
            _commText.text = text;
            _commBox.gameObject.SetActive(true);
            _commHideAt = seconds > 0f ? Time.time + seconds : float.PositiveInfinity;
        }

        public void HideComm()
        {
            _commBox.gameObject.SetActive(false);
        }

        public void Flash(float strength)
        {
            _flashAlpha = Mathf.Max(_flashAlpha, Mathf.Clamp01(strength));
        }

        public void FadeTo(float alpha)
        {
            _fadeTarget = Mathf.Clamp01(alpha);
        }

        public void AddMarker(Transform target, string label)
        {
            RemoveMarker(target);
            Marker m = new Marker();
            m.target = target;
            m.label = label;
            m.rect = M3Ui.Bracket("Marker_" + label, _canvas.transform, UIFactory.Accent, 3f);
            m.rect.sizeDelta = new Vector2(46f, 46f);
            m.text = UIFactory.CreateText("Label", m.rect, "", 20, UIFactory.Accent, TextAnchor.UpperCenter);
            m.text.rectTransform.anchorMin = m.text.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            m.text.rectTransform.pivot = new Vector2(0.5f, 1f);
            m.text.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            m.text.rectTransform.sizeDelta = new Vector2(360f, 30f);
            _markers.Add(m);
        }

        public void RemoveMarker(Transform target)
        {
            for (int i = _markers.Count - 1; i >= 0; i--)
            {
                if (_markers[i].target == target)
                {
                    if (_markers[i].rect != null) Destroy(_markers[i].rect.gameObject);
                    _markers.RemoveAt(i);
                }
            }
        }

        public void ClearMarkers()
        {
            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i].rect != null) Destroy(_markers[i].rect.gameObject);
            }
            _markers.Clear();
        }

        void LateUpdate()
        {
            if (_commBox.gameObject.activeSelf && Time.time >= _commHideAt) _commBox.gameObject.SetActive(false);

            _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, Time.deltaTime * 1.5f);
            _flash.color = new Color(1f, 0.1f, 0.05f, _flashAlpha * 0.55f);

            Color fc = _fade.color;
            fc.a = Mathf.MoveTowards(fc.a, _fadeTarget, Time.deltaTime * 0.6f);
            _fade.color = fc;

            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 camPos = cam.transform.position;
            for (int i = 0; i < _markers.Count; i++)
            {
                Marker m = _markers[i];
                if (m.target == null || !m.target.gameObject.activeInHierarchy)
                {
                    m.rect.gameObject.SetActive(false);
                    continue;
                }
                m.rect.gameObject.SetActive(true);
                bool onScreen;
                Vector2 p = M3Ui.WorldToCanvas(cam, _canvasRect, m.target.position, 40f, out onScreen);
                m.rect.anchorMin = m.rect.anchorMax = new Vector2(0.5f, 0.5f);
                m.rect.pivot = new Vector2(0.5f, 0.5f);
                m.rect.anchoredPosition = p;
                float d = Vector3.Distance(camPos, m.target.position);
                m.text.text = m.label + "  " + M3Ui.Distance(d) + (onScreen ? "" : "  (off screen)");
            }
        }
    }
}
