using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Fade to black between missions, with a mission title card on the way back in.</summary>
    public class ScreenFader : MonoBehaviour
    {
        Canvas _canvas;
        CanvasGroup _black, _card;
        Text _kicker, _title;

        public void Build()
        {
            _canvas = UIFactory.CreateCanvas("Fader", 90);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            var black = UIFactory.Box("Black", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Palette.Ink);
            _black = UIFactory.Group(black);
            var card = UIFactory.Rect("Card", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _card = UIFactory.Group(card);
            _kicker = UIFactory.Label("Kicker", card, "", 22, TextAnchor.LowerCenter,
                new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 44), new Vector2(0, 80), Palette.Accent).Mono();
            _title = UIFactory.Label("Title", card, "", 76, TextAnchor.MiddleCenter,
                new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(40, -50), new Vector2(-40, 46)).Bold();
            var rule = UIFactory.Box("Rule", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120, -62), new Vector2(120, -59), Palette.Warn);
            rule.GetComponent<Image>().raycastTarget = false;
            _black.alpha = 0f; _card.alpha = 0f;
            _black.blocksRaycasts = false; _card.blocksRaycasts = false;
            _canvas.enabled = false;
        }

        public IEnumerator FadeOut(float seconds = 0.35f)
        {
            _canvas.enabled = true;
            _black.blocksRaycasts = true;
            yield return Tween(_black, 1f, seconds);
        }

        /// <summary>Fade back in. With a title, hold a title card over black first.</summary>
        public IEnumerator FadeIn(string kicker = null, string title = null, float seconds = 0.5f)
        {
            if (!string.IsNullOrEmpty(title))
            {
                _kicker.text = kicker ?? "";
                _title.text = title;
                Sfx.Play(Sfx.Id.Whoosh, 0.6f);
                yield return Tween(_card, 1f, 0.3f);
                yield return Wait(1.3f);
                yield return Tween(_card, 0f, 0.3f);
            }
            yield return Tween(_black, 0f, seconds);
            _black.blocksRaycasts = false;
            _canvas.enabled = false;
        }

        static IEnumerator Tween(CanvasGroup g, float to, float seconds)
        {
            float from = g.alpha, t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds);
                g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            g.alpha = to;
        }

        static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }
    }
}
