using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Pip's dialogue box: lines from a PipLineSet, typewriter text, Space/click to advance.</summary>
    public class PipDialogueUI : MonoBehaviour
    {
        public float charsPerSecond = 48f;
        Canvas _canvas;
        Text _text, _hint;
        RectTransform _panel, _face, _eyeL, _eyeR;
        Image _antenna;
        Coroutine _routine;
        bool _skip, _typing;

        public bool IsPlaying { get; private set; }

        public void Build()
        {
            _canvas = UIFactory.CreateCanvas("PipDialogue", 26);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            _panel = UIFactory.Window("Panel", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-460, 40), new Vector2(460, 168));
            var btn = _panel.gameObject.AddComponent<Button>(); // click anywhere on the box to advance
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => _skip = true);

            BuildFace(_panel);
            UIFactory.Label("PipName", _panel, "PIP", 15, TextAnchor.LowerCenter,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 8), new Vector2(118, 28), Palette.Accent).Mono();
            _text = UIFactory.Label("Line", _panel, "", 27, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(138, 22), new Vector2(-28, -12));
            _hint = UIFactory.Label("Hint", _panel, "SPACE / CLICK", 13, TextAnchor.LowerRight,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(138, 8), new Vector2(-18, 26), Palette.A(Palette.TextDim, 0.8f)).Mono();
            _canvas.enabled = false;
        }

        // Pip is a small nervous toy robot: a ceramic head, a dark visor, two cyan eyes and one antenna.
        void BuildFace(RectTransform parent)
        {
            _face = UIFactory.Rect("PipFace", parent, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(26, -34), new Vector2(110, 50));
            var stalk = UIFactory.Box("Stalk", _face, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-2, -4), new Vector2(2, 12), Palette.Steel);
            stalk.GetComponent<Image>().raycastTarget = false;
            _antenna = UIFactory.Round("Tip", _face, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-6, 10), new Vector2(6, 22), Palette.Warn, 6);
            _antenna.raycastTarget = false;
            var head = UIFactory.Round("Head", _face, Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, -6), Palette.Ceramic, 18);
            head.raycastTarget = false;
            var visor = UIFactory.Round("Visor", _face, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(10, -16), new Vector2(-10, 18), Palette.Ink, 12);
            visor.raycastTarget = false;
            _eyeL = Eye(visor.rectTransform, -15);
            _eyeR = Eye(visor.rectTransform, 15);
            var mouth = UIFactory.Round("Mouth", _face, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-10, 9), new Vector2(10, 13), Palette.Steel, 2);
            mouth.raycastTarget = false;
        }

        static RectTransform Eye(RectTransform visor, float x)
        {
            var e = UIFactory.Round("Eye", visor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x - 7, -7), new Vector2(x + 7, 7), Palette.Accent, 7);
            e.raycastTarget = false;
            return e.rectTransform;
        }

        /// <summary>Lift the box above a mission's bottom console (pixels from the bottom edge).</summary>
        public void SetBottom(float y)
        {
            if (_panel == null) return;
            _panel.offsetMin = new Vector2(-460, y);
            _panel.offsetMax = new Vector2(460, y + 128);
        }

        void Update()
        {
            if (_canvas == null || !_canvas.enabled || _face == null) return;
            float t = Time.unscaledTime;
            // Blink every few seconds; bob and flash the antenna while talking.
            float blink = (t % 3.7f) < 0.12f ? 0.12f : 1f;
            _eyeL.localScale = _eyeR.localScale = new Vector3(1f, blink, 1f);
            float bob = _typing ? Mathf.Sin(t * 16f) * 2.2f : Mathf.Sin(t * 2f) * 1.2f;
            _face.anchoredPosition = new Vector2(_face.anchoredPosition.x, 8f + bob);
            _antenna.color = _typing && Mathf.Sin(t * 24f) > 0f ? Palette.Text : Palette.Warn;
            _hint.enabled = !_typing;
        }

        public void Play(string missionId)
        {
            var m = MissionManager.Instance;
            var e = m != null && m.pipLines != null ? m.pipLines.Find(missionId) : null;
            string[] lines = e != null && e.intro != null && e.intro.Length > 0
                ? e.intro
                : new[] { "I am Pip. Mission " + missionId + " is starting." };
            PlayLines(lines);
        }

        public void PlayLines(string[] lines)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Run(lines));
        }

        public void Say(string line) => PlayLines(new[] { line });

        IEnumerator Run(string[] lines)
        {
            IsPlaying = true;
            _canvas.enabled = true;
            foreach (var line in lines)
            {
                _text.text = "";
                _skip = false;
                _typing = true;
                float t = 0f;
                int last = 0;
                while (_text.text.Length < line.Length)
                {
                    t += Time.unscaledDeltaTime * charsPerSecond;
                    int n = Mathf.Min(line.Length, Mathf.FloorToInt(t));
                    if (n != last)
                    {
                        if (n / 3 != last / 3) Sfx.Play(Sfx.Id.Type, 1f, 0.9f + 0.2f * Mathf.PerlinNoise(n * 0.37f, 0f));
                        last = n;
                    }
                    _text.text = line.Substring(0, n);
                    if (_skip || Inp.AdvanceDown()) { _text.text = line; _skip = false; break; }
                    yield return null;
                }
                _typing = false;
                // wait for advance (or auto-advance after a pause that scales with the line length)
                float wait = 0f, hold = Mathf.Clamp(2.5f + line.Length * 0.035f, 3f, 7f);
                _skip = false;
                yield return null;
                while (!_skip && !Inp.AdvanceDown() && wait < hold) { wait += Time.unscaledDeltaTime; yield return null; }
            }
            _canvas.enabled = false;
            IsPlaying = false;
        }

        public string CurrentText => _text != null ? _text.text : "";
    }
}
