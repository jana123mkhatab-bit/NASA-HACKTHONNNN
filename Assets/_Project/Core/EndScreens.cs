using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Shared helper for the modal screens (debrief, fail, end card, log): a dim layer plus a styled window.</summary>
    static class ScreenKit
    {
        public static Canvas Overlay(Transform parent, string name, int order, out RectTransform panel, Vector2 aMin, Vector2 aMax,
            string title = null, string kicker = null)
        {
            var c = UIFactory.CreateCanvas(name, order);
            c.transform.SetParent(parent, false);
            var dim = UIFactory.Box("Dim", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Palette.A(Palette.Ink, 0.72f));
            panel = UIFactory.Window("Panel", dim, aMin, aMax, Vector2.zero, Vector2.zero, title, kicker);
            c.enabled = false;
            return c;
        }
    }
}
