using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Pip's dialogue box: lines from a PipLineSet, typewriter text, Space/click to advance.</summary>
    public class PipDialogueUI : MonoBehaviour
    {
        public float charsPerSecond = 45f;
        Canvas _canvas;
        Text _text;
        Coroutine _routine;
        bool _skip;

        public bool IsPlaying { get; private set; }

        public void Build()
        {
            _canvas = UIFactory.CreateCanvas("PipDialogue", 20);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            var panel = UIFactory.Box("Panel", root, new Vector2(0.15f, 0), new Vector2(0.85f, 0), new Vector2(0, 30), new Vector2(0, 190), UIFactory.Panel);
            var btn = panel.gameObject.AddComponent<Button>(); // click anywhere on the box to advance
            btn.onClick.AddListener(() => _skip = true);
            UIFactory.Box("PipIcon", panel, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, -50), new Vector2(118, 50), UIFactory.Accent);
            UIFactory.Label("PipName", panel, "PIP", 26, TextAnchor.MiddleCenter,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, -50), new Vector2(118, 50), new Color(0.02f, 0.05f, 0.1f));
            _text = UIFactory.Label("Line", panel, "", 30, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(140, 12), new Vector2(-24, -12));
            _canvas.enabled = false;
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

        IEnumerator Run(string[] lines)
        {
            IsPlaying = true;
            _canvas.enabled = true;
            foreach (var line in lines)
            {
                _text.text = "";
                _skip = false;
                float t = 0f;
                while (_text.text.Length < line.Length)
                {
                    t += Time.unscaledDeltaTime * charsPerSecond;
                    int n = Mathf.Min(line.Length, Mathf.FloorToInt(t));
                    _text.text = line.Substring(0, n);
                    if (_skip || Inp.AdvanceDown()) { _text.text = line; _skip = false; break; }
                    yield return null;
                }
                // wait for advance (or auto-advance after a pause)
                float wait = 0f;
                _skip = false;
                yield return null;
                while (!_skip && !Inp.AdvanceDown() && wait < 4f) { wait += Time.unscaledDeltaTime; yield return null; }
            }
            _canvas.enabled = false;
            IsPlaying = false;
        }

        public string CurrentText => _text != null ? _text.text : "";
    }
}
