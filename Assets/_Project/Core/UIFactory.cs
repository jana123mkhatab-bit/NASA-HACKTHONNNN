using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace LastSignal
{
    public enum ButtonStyle { Primary, Ghost, Danger, Confirm }

    /// <summary>
    /// Code-only uGUI builder so every screen works without hand-made prefabs.
    /// All colours come from <see cref="Palette"/>, all shapes and fonts from <see cref="UISkin"/>.
    /// </summary>
    public static class UIFactory
    {
        // Kept for existing callers: these are the palette's UI colours.
        public static readonly Color Panel = Palette.PanelBg;
        public static readonly Color Accent = Palette.Accent;
        public static readonly Color Good = Palette.Good;
        public static readonly Color Warn = Palette.Warn;
        public static readonly Color Bad = Palette.Bad;
        public static readonly Color TextCol = Palette.Text;
        public static readonly Color PanelColor = Palette.PanelBg;
        public static readonly Color TextColor = Palette.Text;
        public static readonly Color Muted = Palette.TextDim;
        public static readonly Color BarBack = new Color(1f, 1f, 1f, 0.10f);

        public static Font Font => UISkin.Body;
        public static Font FontBold => UISkin.Bold;
        public static Font FontMono => UISkin.Mono;

        // Screen zones shared by every mission layout (reference resolution 1920 x 1080).
        public const float TopBar = 68f;      // HUD strip height
        public const float Margin = 24f;
        public const float ColumnW = 452f;    // left / right mission columns
        public const float ConsoleH = 196f;   // bottom-centre mission console

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

        public static Canvas CreateCanvas(string name, int sortOrder, Transform parent)
        {
            var canvas = CreateCanvas(name, sortOrder);
            if (parent != null) canvas.transform.SetParent(parent, false);
            return canvas;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            var panel = Box(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, color);
            return panel.GetComponent<Image>();
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            return Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        public static Button CreateButton(string name, Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            return Button(name, parent, text, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, onClick);
        }

        public static Image CreateBar(string name, Transform parent, Color fill, out Image back)
        {
            var background = Round(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, BarBack, 4);
            background.raycastTarget = false;
            back = background;
            var fillRect = Box("Fill", background.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f), fill);
            var image = fillRect.GetComponent<Image>();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 1f;
            image.sprite = WhiteSprite();
            image.raycastTarget = false;
            return image;
        }

        public static void SetStretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static void SetFill(Image image, float value)
        {
            if (image != null) image.fillAmount = Mathf.Clamp01(value);
        }

        public static Text CreateText(string name, Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            return Label(name, parent, text, size, anchor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, color);
        }

        public static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static void SetCenter(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
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

        /// <summary>Plain solid rectangle (dims, dividers, fills).</summary>
        public static RectTransform Box(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color color)
        {
            var rt = Rect(name, parent, aMin, aMax, offMin, offMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        /// <summary>Rounded rectangle (chips, insets, cards).</summary>
        public static Image Round(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color color, int radius = 8)
        {
            var rt = Rect(name, parent, aMin, aMax, offMin, offMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISkin.Rounded(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        /// <summary>Rounded outline with no fill.</summary>
        public static Image Stroke(string name, Transform parent, Color color, int radius = 8, float thickness = 1.5f)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISkin.Outline(radius, thickness);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// The standard window: soft shadow, dark glass body, cyan edge and an optional header strip.
        /// Returns the body; put content inside it. With a title, content starts 56 px below the top.
        /// </summary>
        public static RectTransform Window(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax,
            string title = null, string kicker = null)
        {
            var body = Round(name, parent, aMin, aMax, offMin, offMax, Palette.PanelBg, 12);
            var rt = body.rectTransform;

            var shadow = Rect("Shadow", rt, Vector2.zero, Vector2.one, new Vector2(-24, -30), new Vector2(24, 18));
            var si = shadow.gameObject.AddComponent<Image>();
            si.sprite = UISkin.Shadow(12, 24); si.type = Image.Type.Sliced;
            si.color = new Color(0f, 0f, 0f, 0.55f); si.raycastTarget = false;
            // Draw the shadow behind the body without it being clipped: it is a child, so it renders on top
            // of the body's own pixels only outside the rounded rect, where the body is transparent.
            shadow.SetAsFirstSibling();
            var cover = Round("Body", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Palette.PanelBgSolid, 12);
            cover.color = Palette.A(Palette.PanelBgSolid, 0.90f);
            cover.raycastTarget = false;
            body.color = new Color(0, 0, 0, 0.001f); // keeps raycast blocking, visuals come from the cover

            var sheen = Rect("Sheen", rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(2, -120), new Vector2(-2, -2));
            var sh = sheen.gameObject.AddComponent<Image>();
            sh.sprite = UISkin.FadeDown(); sh.color = new Color(0.45f, 0.75f, 1f, 0.07f); sh.raycastTarget = false;

            Stroke("Edge", rt, Palette.PanelEdge, 12);

            if (!string.IsNullOrEmpty(title))
            {
                var bar = Box("AccentBar", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -40), new Vector2(26, -18), Palette.Accent);
                bar.GetComponent<Image>().raycastTarget = false;
                Label("Title", rt, title, 26, TextAnchor.MiddleLeft,
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(38, -46), new Vector2(-20, -12), Palette.Text).Bold();
                if (!string.IsNullOrEmpty(kicker))
                    Label("Kicker", rt, kicker, 17, TextAnchor.MiddleRight,
                        new Vector2(0, 1), new Vector2(1, 1), new Vector2(38, -46), new Vector2(-22, -12), Palette.TextDim).Mono();
                var line = Box("Rule", rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -55), new Vector2(-20, -54), Palette.A(Palette.Accent, 0.22f));
                line.GetComponent<Image>().raycastTarget = false;
            }
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
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }

        public static Text Bold(this Text t) { t.font = FontBold; return t; }
        public static Text Mono(this Text t) { t.font = FontMono; return t; }

        public static Button Button(string name, Transform parent, string text,
            Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, UnityEngine.Events.UnityAction onClick,
            ButtonStyle style = ButtonStyle.Primary)
        {
            Color bg, edge, fg = Palette.Text;
            switch (style)
            {
                case ButtonStyle.Ghost: bg = Palette.ButtonGhost; edge = Palette.A(Palette.Text, 0.22f); break;
                case ButtonStyle.Danger: bg = Palette.A(Palette.Warn, 0.92f); edge = Palette.Warn; fg = Palette.Ink; break;
                case ButtonStyle.Confirm: bg = Palette.A(Palette.Good, 0.90f); edge = Palette.Good; fg = Palette.Ink; break;
                default: bg = Palette.ButtonBg; edge = Palette.A(Palette.Accent, 0.75f); break;
            }
            var img = Round(name, parent, aMin, aMax, offMin, offMax, bg, 8);
            var rt = img.rectTransform;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.normalColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.selectedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
            if (onClick != null) b.onClick.AddListener(onClick);
            Stroke("Edge", rt, edge, 8);
            int size = Mathf.Clamp(Mathf.RoundToInt((offMax.y - offMin.y) * 0.42f), 17, 28);
            if (aMin.y != aMax.y) size = 24;
            var label = Label("Text", rt, text, size, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-6, 0), fg).Bold();
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            rt.gameObject.AddComponent<UIButtonFx>();
            return b;
        }

        /// <summary>Change a button's caption.</summary>
        public static void SetLabel(Button b, string text)
        {
            var t = b != null ? b.GetComponentInChildren<Text>() : null;
            if (t != null) t.text = text;
        }

        /// <summary>A horizontal fill bar. Returns the fill Image (use fillAmount 0..1).</summary>
        public static Image Bar(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color fill)
        {
            var back = Round(name, parent, aMin, aMax, offMin, offMax, new Color(1, 1, 1, 0.10f), 4);
            back.raycastTarget = false;
            var f = Box("Fill", back.rectTransform, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3), fill);
            var img = f.GetComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            img.raycastTarget = false;
            // Filled images need a sprite to honour fillAmount; use a 1x1 white sprite.
            img.sprite = WhiteSprite();
            return img;
        }

        /// <summary>Small rounded status chip with a caption. Returns the caption so the caller can update it.</summary>
        public static Text Chip(string name, Transform parent, string text, Color color,
            Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var img = Round(name, parent, aMin, aMax, offMin, offMax, Palette.A(color, 0.16f), 6);
            img.raycastTarget = false;
            Stroke("Edge", img.rectTransform, Palette.A(color, 0.7f), 6, 1.2f);
            return Label("Text", img.rectTransform, text, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, color).Mono();
        }

        /// <summary>Recolour a chip made with <see cref="Chip"/>.</summary>
        public static void SetChip(Text chip, string text, Color color)
        {
            if (chip == null) return;
            chip.text = text; chip.color = color;
            var bg = chip.transform.parent.GetComponent<Image>();
            if (bg != null) bg.color = Palette.A(color, 0.16f);
            var edge = chip.transform.parent.Find("Edge");
            if (edge != null) edge.GetComponent<Image>().color = Palette.A(color, 0.7f);
        }

        /// <summary>Fade and enable/disable a whole group of widgets.</summary>
        public static CanvasGroup Group(Component c)
        {
            var g = c.GetComponent<CanvasGroup>();
            return g != null ? g : c.gameObject.AddComponent<CanvasGroup>();
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

        public static bool EscapeDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
