using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>
    /// Pip's dialogue box: typewriter text at the bottom of the screen.
    /// Listens to Pip.Spoke, so any script can call Pip.Say("...") or Pip.Hint("M1").
    /// </summary>
    public class PipDialogueUI : MonoBehaviour
    {
        public static PipDialogueUI Instance { get; private set; }

        public float charactersPerSecond = 40f;

        GameObject _panel;
        Text _text;
        Coroutine _running;

        void Awake()
        {
            Instance = this;
            Canvas canvas = UIFactory.CreateCanvas("PipDialogue", 60, transform);
            _panel = UIFactory.CreatePanel("Panel", canvas.transform, UIFactory.PanelColor).gameObject;
            UIFactory.SetBottomStrip(_panel.GetComponent<RectTransform>(), 150f, 260f, 40f);

            Text name = UIFactory.CreateText("Name", _panel.transform, "PIP", 20, UIFactory.Accent, TextAnchor.UpperLeft);
            UIFactory.SetTopLeft(name.rectTransform, 24f, 12f, 200f, 26f);

            _text = UIFactory.CreateText("Line", _panel.transform, "", 30, UIFactory.TextColor, TextAnchor.UpperLeft);
            UIFactory.SetStretch(_text.rectTransform, 24f, 44f, 24f, 12f);

            _panel.SetActive(false);
            Pip.Spoke += OnSpoke;
        }

        void OnDestroy()
        {
            Pip.Spoke -= OnSpoke;
            if (Instance == this) Instance = null;
        }

        void OnSpoke(string line)
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Type(line));
        }

        IEnumerator Type(string line)
        {
            _panel.SetActive(true);
            _text.text = "";
            float delay = 1f / Mathf.Max(1f, charactersPerSecond);
            for (int i = 1; i <= line.Length; i++)
            {
                _text.text = line.Substring(0, i);
                yield return new WaitForSecondsRealtime(delay);
            }
            yield return new WaitForSecondsRealtime(2.5f + line.Length * 0.03f);
            _panel.SetActive(false);
            _running = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }
    }
}
