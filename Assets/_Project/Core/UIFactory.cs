using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace LastSignal
{
    /// <summary>Tiny code-only uGUI builder so every screen works without hand-made prefabs.</summary>
    public static class UIFactory
    {
        public static readonly Color Panel = new Color(0.04f, 0.06f, 0.10f, 0.88f);
        public static readonly Color Accent = new Color(0.30f, 0.80f, 1.00f, 1f);
        public static readonly Color Good = new Color(0.30f, 0.90f, 0.50f, 1f);
        public static readonly Color Warn = new Color(1.00f, 0.75f, 0.20f, 1f);
        public static readonly Color Bad = new Color(1.00f, 0.30f, 0.30f, 1f);
        public static readonly Color TextCol = new Color(0.90f, 0.95f, 1f, 1f);

        static Font _font;
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        public static RectTransform Box(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color color)
        {
            var rt = Rect(name, parent, aMin, aMax, offMin, offMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        public static Text Label(string name, Transform parent, string text, int size, TextAnchor anchor,
            Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color? color = null)
        {
            var rt = Rect(name, parent, aMin, aMax, offMin, offMax);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.alignment = anchor;
            t.color = color ?? TextCol;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(string name, Transform parent, string text,
            Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, UnityEngine.Events.UnityAction onClick)
        {
            var rt = Box(name, parent, aMin, aMax, offMin, offMax, new Color(0.12f, 0.35f, 0.55f, 1f));
            var b = rt.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(0.6f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.5f, 0.7f, 0.9f, 1f);
            b.colors = colors;
            if (onClick != null) b.onClick.AddListener(onClick);
            Label("Text", rt, text, 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return b;
        }

        /// <summary>A horizontal fill bar. Returns the fill Image (use fillAmount 0..1).</summary>
        public static Image Bar(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color fill)
        {
            var back = Box(name, parent, aMin, aMax, offMin, offMax, new Color(1, 1, 1, 0.12f));
            var f = Box("Fill", back, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2), fill);
            var img = f.GetComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            // Filled images need a sprite to honour fillAmount; use a 1x1 white sprite.
            img.sprite = WhiteSprite();
            return img;
        }

        static Sprite _white;
        public static Sprite WhiteSprite()
        {
            if (_white != null) return _white;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px); tex.Apply();
            _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            return _white;
        }
    }

    /// <summary>Input wrapper that works with the old and the new input system.</summary>
    public static class Inp
    {
        public static bool LogKeyDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.L);
#endif
        }

        public static bool AdvanceDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
        }
    }
}
