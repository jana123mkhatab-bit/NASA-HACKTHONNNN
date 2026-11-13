using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    // Several small screens share this file's helper; each MonoBehaviour is also in its own file below.
    static class ScreenKit
    {
        public static Canvas Overlay(Transform parent, string name, int order, out RectTransform panel, Vector2 aMin, Vector2 aMax)
        {
            var c = UIFactory.CreateCanvas(name, order);
            c.transform.SetParent(parent, false);
            var dim = UIFactory.Box("Dim", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.6f));
            panel = UIFactory.Box("Panel", dim, aMin, aMax, Vector2.zero, Vector2.zero, new Color(0.05f, 0.08f, 0.13f, 0.97f));
            c.enabled = false;
            return c;
        }
    }
}
