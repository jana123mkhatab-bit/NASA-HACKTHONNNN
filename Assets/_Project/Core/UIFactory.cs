using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>
    /// Builds simple uGUI elements from code so the core UI works without any prefab or scene setup.
    /// Reference resolution is 1920x1080. Positions are in those units.
    /// Other team members can reuse these helpers for quick greybox UI.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color PanelColor = new Color(0.04f, 0.06f, 0.11f, 0.90f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.65f);
        public static readonly Color Accent = new Color(0.30f, 0.85f, 0.75f, 1f);
        public static readonly Color TextColor = new Color(0.92f, 0.95f, 1f, 1f);
        public static readonly Color Muted = new Color(0.62f, 0.70f, 0.82f, 1f);
        public static readonly Color Good = new Color(0.30f, 0.80f, 0.45f, 1f);
        public static readonly Color Warn = new Color(0.95f, 0.70f, 0.20f, 1f);
        public static readonly Color Bad = new Color(0.90f, 0.30f, 0.30f, 1f);
        public static readonly Color BarBack = new Color(1f, 1f, 1f, 0.12f);

        static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (System.Exception) { }
                }
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (System.Exception) { }
                }
                if (_font == null)
                {
                    _font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                }
                return _font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        // ---------- placement helpers (pixels in the 1920x1080 reference space) ----------

        /// <summary>Place from the top-left corner: x to the right, y downward.</summary>
        public static void SetTopLeft(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }

        /// <summary>Place from the top-right corner: x to the left, y downward.</summary>
        public static void SetTopRight(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }

        /// <summary>Place relative to the screen centre.</summary>
        public static void SetCenter(RectTransform rt, float centerX, float centerY, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centerX, centerY);
            rt.sizeDelta = new Vector2(width, height);
        }

        /// <summary>Stretch across the whole parent with an inner margin on every side.</summary>
        public static void SetStretch(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Full-width strip along the top edge.</summary>
        public static void SetTopStrip(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -height);
            rt.offsetMax = new Vector2(0f, 0f);
        }

        /// <summary>Strip along the bottom edge, inset from the sides and lifted from the bottom.</summary>
        public static void SetBottomStrip(RectTransform rt, float height, float sideMargin, float bottomMargin)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(sideMargin, bottomMargin);
            rt.offsetMax = new Vector2(-sideMargin, bottomMargin + height);
        }

        // ---------- element builders ----------

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rt = CreateRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text CreateText(string name, Transform parent, string content, int size, Color color, TextAnchor align)
        {
            RectTransform rt = CreateRect(name, parent);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button CreateButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            Image img = CreatePanel(name, parent, new Color(0.14f, 0.42f, 0.42f, 1f));
            Button button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.14f, 0.42f, 0.42f, 1f);
            colors.highlightedColor = new Color(0.20f, 0.58f, 0.56f, 1f);
            colors.pressedColor = new Color(0.10f, 0.30f, 0.30f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            Text text = CreateText(name + "_Label", img.transform, label, 28, Color.white, TextAnchor.MiddleCenter);
            SetStretch(text.rectTransform, 6f, 6f, 6f, 6f);
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>Creates a horizontal bar (background + fill). Use SetFill to change its value from 0 to 1.</summary>
        public static Image CreateBar(string name, Transform parent, Color fillColor, out Image background)
        {
            background = CreatePanel(name + "_Back", parent, BarBack);
            Image fill = CreatePanel(name + "_Fill", background.transform, fillColor);
            RectTransform rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return fill;
        }

        public static void SetFill(Image fill, float value01)
        {
            RectTransform rt = fill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Makes sure the scene has an EventSystem so buttons work, using the right input module.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            System.Type moduleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null)
            {
                Component module = go.AddComponent(moduleType);
                System.Reflection.MethodInfo assign = moduleType.GetMethod("AssignDefaultActions");
                if (assign != null) assign.Invoke(module, null);
                return;
            }
#endif
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
