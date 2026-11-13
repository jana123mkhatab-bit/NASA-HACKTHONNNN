using LastSignal.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.M3
{
    /// <summary>Small layout helpers on top of Core's UIFactory.</summary>
    public static class M3Ui
    {
        /// <summary>Places a rect by an anchor/pivot point (0..1 on each axis) with an offset in reference pixels.</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }

        public static Image Box(string name, Transform parent, Color color, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            Image img = UIFactory.CreatePanel(name, parent, color);
            img.raycastTarget = false;
            Place(img.rectTransform, anchor, offset, size);
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int fontSize, Color color, TextAnchor align,
            Vector2 anchor, Vector2 offset, Vector2 size)
        {
            Text t = UIFactory.CreateText(name, parent, text, fontSize, color, align);
            Place(t.rectTransform, anchor, offset, size);
            return t;
        }

        /// <summary>Four thin edges forming a hollow square (target brackets).</summary>
        public static RectTransform Bracket(string name, Transform parent, Color color, float thickness)
        {
            RectTransform root = UIFactory.CreateRect(name, parent);
            Edge(root, color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -thickness), Vector2.zero);
            Edge(root, color, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, thickness));
            Edge(root, color, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(thickness, 0f));
            Edge(root, color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-thickness, 0f), Vector2.zero);
            return root;
        }

        static void Edge(RectTransform parent, Color c, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            Image e = UIFactory.CreatePanel("Edge", parent, c);
            e.raycastTarget = false;
            e.rectTransform.anchorMin = min;
            e.rectTransform.anchorMax = max;
            e.rectTransform.offsetMin = offMin;
            e.rectTransform.offsetMax = offMax;
        }

        /// <summary>Screen point of a world position in canvas reference pixels (1920x1080 centred), clamped to the edges if off screen.</summary>
        public static Vector2 WorldToCanvas(Camera cam, RectTransform canvasRect, Vector3 world, float margin, out bool onScreen)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0f;
            if (behind) sp = new Vector3(Screen.width - sp.x, Screen.height - sp.y, -sp.z);
            Vector2 size = canvasRect.rect.size;
            Vector2 p = new Vector2(sp.x / Mathf.Max(1, Screen.width) * size.x - size.x * 0.5f, sp.y / Mathf.Max(1, Screen.height) * size.y - size.y * 0.5f);
            Vector2 half = size * 0.5f - new Vector2(margin, margin);
            onScreen = !behind && Mathf.Abs(p.x) <= half.x && Mathf.Abs(p.y) <= half.y;
            if (!onScreen)
            {
                if (behind && p.sqrMagnitude < 1f) p = new Vector2(0f, -1f);
                float k = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(p.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(p.y)));
                if (behind || k < 1f) p *= k;
            }
            return p;
        }

        public static string Distance(float metres)
        {
            if (metres >= 10000f) return (metres / 1000f).ToString("0") + " km";
            if (metres >= 1000f) return (metres / 1000f).ToString("0.0") + " km";
            return metres.ToString("0") + " m";
        }
    }
}
