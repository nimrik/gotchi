using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // Every UI element is built in code: rounded cards, soft shadows, pastel theme, procedural sprites.
    public static class UIFactory
    {
        public static readonly Color Cream = Hex("FFF6EC");
        public static readonly Color Peach = Hex("FFE1D0");
        public static readonly Color Card = Color.white;
        public static readonly Color Ink = Hex("4A3F55");
        public static readonly Color Muted = Hex("9A8FA6");
        public static readonly Color Pink = Hex("FF9EBB");
        public static readonly Color PinkDark = Hex("E86F96");
        public static readonly Color Primary = Hex("F27FA5");   // CTA buttons; white label
        public static readonly Color Mint = Hex("A8E6CF");
        public static readonly Color Sky = Hex("A7D8FF");
        public static readonly Color Lavender = Hex("CDBBFF");
        public static readonly Color Butter = Hex("FFD98E");
        public static readonly Color Coral = Hex("FFB59E");
        public static readonly Color Shadow = new Color(0.45f, 0.3f, 0.35f, 0.14f);
        public static readonly Color Scrim = new Color(0.29f, 0.25f, 0.33f, 0.45f);

        // Spacing scale (canvas units). See 12-ui-guide.md.
        // Corner radii (canvas units). Pills are always half their height (PillRadius component).
        public static class Radius
        {
            public const float Card = 32f;
            public const float Row = 24f;
            public const float Input = 24f;
            public const float Base = 40f; // radius baked into RoundedSprite at multiplier 1
        }

        // Button heights by type.
        public static class ButtonHeight
        {
            public const float Large = 84f;   // primary actions, Back
            public const float Medium = 64f;  // row actions (Play, Buy, Train)
            public const float Small = 52f;   // chips
        }

        public static class Spacing
        {
            public const float Section = 24f;   // between blocks on a page
            public const float List = 16f;      // between rows in a list
            public const float Pad = 24f;       // inner padding of cards and pages
            public const float Gutter = 24f;    // page side margins
        }

        // Legacy names kept for the mini-games.
        public static Color Paper => Cream;
        public static Color Panel => Card;
        public static Color Accent => Pink;

        private static Font _heading;
        private static Font _body;
        private static Sprite _circle;
        private static Sprite _rounded;
        private static Sprite _gradient;
        private static Sprite _ring;

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color c) ? c : Color.magenta;
        }

        public const float FontScale = 1.0f;

        public static Font HeadingFont => _heading ?? (_heading = Resources.Load<Font>("Fonts/FredokaOne-Regular") ?? BodyFont);
        public static Font BodyFont => _body ?? (_body = Resources.Load<Font>("Fonts/VarelaRound-Regular") ?? Resources.Load<Font>("Fonts/PixelifySans") ?? LoadBuiltinFont("LegacyRuntime.ttf") ?? LoadBuiltinFont("Arial.ttf"));

        private static Font LoadBuiltinFont(string name)
        {
            try { return Resources.GetBuiltinResource<Font>(name); }
            catch (Exception) { return null; }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (_circle != null) return _circle;
                const int size = 256;
                var pixels = new Color[size * size];
                float radius = size / 2f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - radius, dy = y + 0.5f - radius;
                        float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                _circle = MakeSprite(size, size, pixels, Vector4.zero);
                return _circle;
            }
        }

        // 9-sliced rounded rectangle; corner radius in canvas units = 40 / pixelsPerUnitMultiplier.
        public static Sprite RoundedSprite
        {
            get
            {
                if (_rounded != null) return _rounded;
                const int size = 96;
                const float radius = 40f;
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float px = x + 0.5f, py = y + 0.5f;
                        float cx = Mathf.Clamp(px, radius, size - radius);
                        float cy = Mathf.Clamp(py, radius, size - radius);
                        float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                        pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f));
                    }
                _rounded = MakeSprite(size, size, pixels, new Vector4(radius, radius, radius, radius));
                return _rounded;
            }
        }

        // Hard-edged ring for radial meters (Image.Type.Filled, Radial360).
        public static Sprite RingSprite
        {
            get
            {
                if (_ring != null) return _ring;
                const int size = 256;
                const float outer = 128f, inner = 100f;
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - outer, dy = y + 0.5f - outer;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                _ring = MakeSprite(size, size, pixels, Vector4.zero);
                return _ring;
            }
        }

        public static Sprite GradientSprite
        {
            get
            {
                if (_gradient != null) return _gradient;
                const int h = 128;
                var pixels = new Color[4 * h];
                for (int y = 0; y < h; y++)
                {
                    float a = Mathf.SmoothStep(0f, 1f, y / (float)(h - 1));
                    for (int x = 0; x < 4; x++) pixels[y * 4 + x] = new Color(1f, 1f, 1f, a);
                }
                _gradient = MakeSprite(4, h, pixels, Vector4.zero);
                return _gradient;
            }
        }

        private static Sprite MakeSprite(int w, int h, Color[] pixels, Vector4 border)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Fill(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        // Fits `rect` to the device safe area (notch / home indicator) inside its parent.
        public static RectTransform ApplySafeArea(RectTransform rect)
        {
            Rect safe = Screen.safeArea;
            var min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            var max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        // cornerScale is kept for old call sites: >= 1 means a card (radius 32), < 1 a row (radius 24).
        public static Image CreateRounded(string name, Transform parent, Color color, float cornerScale = 1f) =>
            CreateRoundedRadius(name, parent, color, cornerScale >= 1f ? Radius.Card : Radius.Row);

        public static Image CreateRoundedRadius(string name, Transform parent, Color color, float radius)
        {
            var image = CreatePanel(name, parent, color);
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = Radius.Base / Mathf.Max(1f, radius);
            return image;
        }

        // Pill: corner radius always half the element's height, updated on resize.
        public static Image MakePill(Image image)
        {
            if (image.GetComponent<PillRadius>() == null) image.gameObject.AddComponent<PillRadius>();
            return image;
        }

        public static Image CreatePill(string name, Transform parent, Color color) => MakePill(CreateRoundedRadius(name, parent, color, Radius.Base));

        // Rounded card with a soft drop shadow behind it. cornerScale >= 1: card radius; < 1: row radius.
        public static Image CreateCard(string name, Transform parent, Color color, float cornerScale = 1f)
        {
            bool isCard = cornerScale >= 1f;
            float radius = isCard ? Radius.Card : Radius.Row;
            var holder = CreateRect(name, parent);
            if (isCard)
            {
                var shadow = CreateRoundedRadius("Shadow", holder, Shadow, radius);
                Fill(shadow.rectTransform, -4f, -4f, -2f, -10f);
                shadow.raycastTarget = false;
            }
            var card = CreateRoundedRadius("Card", holder, color, radius);
            Fill(card.rectTransform);
            return card;
        }

        public static Image CreateCircle(string name, Transform parent, Color color, float diameter)
        {
            var image = CreatePanel(name, parent, color);
            image.sprite = CircleSprite;
            image.rectTransform.sizeDelta = new Vector2(diameter, diameter);
            return image;
        }

        // A bubble: pastel circle with a glossy highlight.
        public static Image CreateBubble(string name, Transform parent, Color color, float diameter)
        {
            var bubble = CreateCircle(name, parent, color, diameter);
            var rim = CreateCircle("Rim", bubble.transform, new Color(1f, 1f, 1f, 0.35f), diameter * 0.9f);
            rim.raycastTarget = false;
            var highlight = CreateCircle("Highlight", bubble.transform, new Color(1f, 1f, 1f, 0.75f), diameter * 0.28f);
            highlight.rectTransform.anchoredPosition = new Vector2(-diameter * 0.2f, diameter * 0.22f);
            highlight.raycastTarget = false;
            return bubble;
        }

        public static Image CreateGradient(string name, Transform parent, Color color)
        {
            var image = CreatePanel(name, parent, color);
            image.sprite = GradientSprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(string name, Transform parent, string content, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool heading = false)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = heading ? HeadingFont : BodyFont;
            text.text = content;
            text.fontSize = Mathf.RoundToInt(size * FontScale);
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label, Color color, Action onClick, int fontSize = 34, MonoBehaviour tweenHost = null, Color? textColor = null)
        {
            var background = CreatePill(name, parent, color);
            if (color == Pink) { color = Primary; background.color = Primary; }
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ApplyTransition(button);

            var text = CreateText("Label", background.transform, label, fontSize, textColor ?? LabelColorFor(color), TextAnchor.MiddleCenter, true);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Fill(text.rectTransform, 8f, 8f, 4f, 4f);

            background.gameObject.AddComponent<PressFeedback>();
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }

        // Pill-shaped meter; the fill is a rounded image whose anchorMax.x is the normalized value.
        public static Image CreatePillBar(string name, Transform parent, Color fillColor, out RectTransform fill)
        {
            var track = CreatePill(name, parent, new Color(0f, 0f, 0f, 0.06f));
            var fillImage = CreatePill("Fill", track.transform, fillColor);
            fill = fillImage.rectTransform;
            Place(fill, Vector2.zero, new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            fillImage.raycastTarget = false;
            return track;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go, float spacing, RectOffset padding)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        // expandChildren=false keeps LayoutElement flexible weights meaningful (force-expand overrides them).
        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go, float spacing, RectOffset padding, bool expandChildren = false)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = expandChildren;
            layout.childForceExpandHeight = true;
            return layout;
        }

        public static LayoutElement SetPreferredHeight(GameObject go, float height)
        {
            var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 0f;
            return element;
        }

        public static LayoutElement SetWidth(GameObject go, float preferredWidth, float flexibleWidth)
        {
            var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.minWidth = preferredWidth > 0f ? preferredWidth : 0f;
            element.flexibleWidth = flexibleWidth;
            return element;
        }

        public enum IconKind { Cookie, Bubbles, Moon, Ball, Sparkle, Bag, Heart, Coin, Gem, Chat, Shield, Paw, Flask, Leaf, Compass, Gear, Trophy }

        // Procedural icons built from circles and rounded rects; `background` is what sits behind the
        // icon (needed for the masked crescent / ring tricks).
        public static RectTransform CreateIcon(IconKind kind, Transform parent, float size, Color background)
        {
            var root = CreateRect(kind + "Icon", parent);
            root.sizeDelta = new Vector2(size, size);
            float s = size;
            switch (kind)
            {
                case IconKind.Cookie:
                    Dot(root, Hex("E9B97A"), s * 0.9f, 0f, 0f);
                    Dot(root, Hex("7A4B2A"), s * 0.16f, -s * 0.18f, s * 0.12f);
                    Dot(root, Hex("7A4B2A"), s * 0.14f, s * 0.16f, s * 0.18f);
                    Dot(root, Hex("7A4B2A"), s * 0.15f, s * 0.08f, -s * 0.18f);
                    Dot(root, Hex("7A4B2A"), s * 0.11f, -s * 0.22f, -s * 0.14f);
                    break;
                case IconKind.Bubbles:
                    Bubble(root, Hex("7CBDF5"), s * 0.6f, -s * 0.16f, -s * 0.1f);
                    Bubble(root, Hex("7CBDF5"), s * 0.42f, s * 0.22f, s * 0.14f);
                    Bubble(root, Hex("7CBDF5"), s * 0.26f, s * 0.2f, -s * 0.26f);
                    break;
                case IconKind.Moon:
                {
                    var moon = Dot(root, Hex("F5B942"), s * 0.8f, -s * 0.05f, 0f);
                    Dot(moon.transform, background, s * 0.7f, s * 0.26f, s * 0.14f);
                    Dot(root, Color.white, s * 0.12f, s * 0.3f, -s * 0.22f);
                    break;
                }
                case IconKind.Ball:
                {
                    var ball = Dot(root, PinkDark, s * 0.86f, 0f, 0f);
                    var band = CreateRounded("Band", ball.transform, new Color(1f, 1f, 1f, 0.7f), 0.5f);
                    band.rectTransform.sizeDelta = new Vector2(s * 0.86f, s * 0.2f);
                    band.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                    band.raycastTarget = false;
                    Dot(root, new Color(1f, 1f, 1f, 0.8f), s * 0.2f, -s * 0.22f, s * 0.24f);
                    break;
                }
                case IconKind.Sparkle:
                {
                    var v = CreateRounded("V", root, Hex("F2B84B"), 0.35f);
                    v.rectTransform.sizeDelta = new Vector2(s * 0.26f, s * 0.95f);
                    v.raycastTarget = false;
                    var h = CreateRounded("H", root, Hex("F2B84B"), 0.35f);
                    h.rectTransform.sizeDelta = new Vector2(s * 0.95f, s * 0.26f);
                    h.raycastTarget = false;
                    Dot(root, Color.white, s * 0.22f, 0f, 0f);
                    break;
                }
                case IconKind.Bag:
                {
                    var handle = Dot(root, Hex("E8907A"), s * 0.46f, 0f, s * 0.2f);
                    Dot(handle.transform, background, s * 0.3f, 0f, 0f);
                    var body = CreateRounded("Body", root, Coral, 0.7f);
                    body.rectTransform.sizeDelta = new Vector2(s * 0.74f, s * 0.6f);
                    body.rectTransform.anchoredPosition = new Vector2(0f, -s * 0.14f);
                    body.raycastTarget = false;
                    Dot(root, new Color(1f, 1f, 1f, 0.6f), s * 0.14f, -s * 0.18f, -s * 0.02f);
                    break;
                }
                case IconKind.Heart:
                {
                    var square = CreateRounded("Square", root, Pink, 0.45f);
                    square.rectTransform.sizeDelta = new Vector2(s * 0.56f, s * 0.56f);
                    square.rectTransform.anchoredPosition = new Vector2(0f, -s * 0.1f);
                    square.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    square.raycastTarget = false;
                    Dot(root, Pink, s * 0.46f, -s * 0.2f, s * 0.14f);
                    Dot(root, Pink, s * 0.46f, s * 0.2f, s * 0.14f);
                    Dot(root, new Color(1f, 1f, 1f, 0.7f), s * 0.12f, -s * 0.26f, s * 0.22f);
                    break;
                }
                case IconKind.Coin:
                    Dot(root, Hex("F2C14E"), s, 0f, 0f);
                    Dot(root, Hex("FFE08A"), s * 0.7f, 0f, 0f);
                    Dot(root, new Color(1f, 1f, 1f, 0.7f), s * 0.16f, -s * 0.22f, s * 0.22f);
                    break;
                case IconKind.Gem:
                {
                    var gem = CreateRounded("Gem", root, Hex("B39DFF"), 0.45f);
                    gem.rectTransform.sizeDelta = new Vector2(s * 0.68f, s * 0.68f);
                    gem.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    gem.raycastTarget = false;
                    Dot(root, new Color(1f, 1f, 1f, 0.75f), s * 0.18f, -s * 0.16f, s * 0.16f);
                    break;
                }
                case IconKind.Chat:
                {
                    var body = CreateRounded("Body", root, Sky, 0.7f);
                    body.rectTransform.sizeDelta = new Vector2(s * 0.82f, s * 0.6f);
                    body.rectTransform.anchoredPosition = new Vector2(0f, s * 0.08f);
                    body.raycastTarget = false;
                    Dot(root, Sky, s * 0.24f, -s * 0.24f, -s * 0.3f);
                    Dot(root, Ink, s * 0.1f, -s * 0.2f, s * 0.08f);
                    Dot(root, Ink, s * 0.1f, 0f, s * 0.08f);
                    Dot(root, Ink, s * 0.1f, s * 0.2f, s * 0.08f);
                    break;
                }
                case IconKind.Shield:
                {
                    var outer = CreateRounded("Outer", root, Lavender, 0.8f);
                    outer.rectTransform.sizeDelta = new Vector2(s * 0.72f, s * 0.78f);
                    outer.rectTransform.anchoredPosition = new Vector2(0f, s * 0.06f);
                    outer.raycastTarget = false;
                    Dot(root, Lavender, s * 0.5f, 0f, -s * 0.26f);
                    var inner = CreateRounded("Inner", root, new Color(1f, 1f, 1f, 0.55f), 0.6f);
                    inner.rectTransform.sizeDelta = new Vector2(s * 0.34f, s * 0.42f);
                    inner.rectTransform.anchoredPosition = new Vector2(0f, s * 0.02f);
                    inner.raycastTarget = false;
                    break;
                }
                case IconKind.Paw:
                    Dot(root, Coral, s * 0.5f, 0f, -s * 0.16f);
                    Dot(root, Coral, s * 0.22f, -s * 0.3f, s * 0.16f);
                    Dot(root, Coral, s * 0.22f, 0f, s * 0.3f);
                    Dot(root, Coral, s * 0.22f, s * 0.3f, s * 0.16f);
                    break;
                case IconKind.Flask:
                {
                    var neck = CreateRounded("Neck", root, Mint, 0.4f);
                    neck.rectTransform.sizeDelta = new Vector2(s * 0.26f, s * 0.42f);
                    neck.rectTransform.anchoredPosition = new Vector2(0f, s * 0.24f);
                    neck.raycastTarget = false;
                    Dot(root, Mint, s * 0.62f, 0f, -s * 0.14f);
                    Dot(root, new Color(1f, 1f, 1f, 0.8f), s * 0.14f, -s * 0.12f, -s * 0.06f);
                    Dot(root, new Color(1f, 1f, 1f, 0.8f), s * 0.1f, s * 0.1f, -s * 0.22f);
                    break;
                }
                case IconKind.Leaf:
                {
                    var stem = CreateRounded("Stem", root, Hex("7FCF9A"), 0.4f);
                    stem.rectTransform.sizeDelta = new Vector2(s * 0.1f, s * 0.6f);
                    stem.rectTransform.anchoredPosition = new Vector2(0f, -s * 0.16f);
                    stem.raycastTarget = false;
                    foreach (float side in new[] { -1f, 1f })
                    {
                        var leaf = Dot(root, Hex("9ED9B5"), s * 0.42f, side * s * 0.2f, s * 0.1f);
                        leaf.rectTransform.localScale = new Vector3(0.7f, 1.1f, 1f);
                        leaf.rectTransform.localRotation = Quaternion.Euler(0f, 0f, side * 35f);
                    }
                    break;
                }
                case IconKind.Trophy:
                {
                    var cup = CreateRounded("Cup", root, Butter, 0.8f);
                    cup.rectTransform.sizeDelta = new Vector2(s * 0.56f, s * 0.5f);
                    cup.rectTransform.anchoredPosition = new Vector2(0f, s * 0.14f);
                    cup.raycastTarget = false;
                    Dot(root, Butter, s * 0.24f, -s * 0.36f, s * 0.14f);
                    Dot(root, background, s * 0.12f, -s * 0.36f, s * 0.14f);
                    Dot(root, Butter, s * 0.24f, s * 0.36f, s * 0.14f);
                    Dot(root, background, s * 0.12f, s * 0.36f, s * 0.14f);
                    var stem = CreateRounded("Stem", root, Hex("E0A93E"), 0.4f);
                    stem.rectTransform.sizeDelta = new Vector2(s * 0.14f, s * 0.22f);
                    stem.rectTransform.anchoredPosition = new Vector2(0f, -s * 0.2f);
                    stem.raycastTarget = false;
                    var foot = CreateRounded("Foot", root, Hex("E0A93E"), 0.5f);
                    foot.rectTransform.sizeDelta = new Vector2(s * 0.44f, s * 0.12f);
                    foot.rectTransform.anchoredPosition = new Vector2(0f, -s * 0.36f);
                    foot.raycastTarget = false;
                    Dot(root, new Color(1f, 1f, 1f, 0.7f), s * 0.12f, -s * 0.14f, s * 0.24f);
                    break;
                }
                case IconKind.Gear:
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var tooth = CreateRounded("Tooth", root, Muted, 0.4f);
                        tooth.rectTransform.sizeDelta = new Vector2(s * 0.26f, s * 0.9f);
                        tooth.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
                        tooth.raycastTarget = false;
                    }
                    Dot(root, Muted, s * 0.62f, 0f, 0f);
                    Dot(root, background, s * 0.26f, 0f, 0f);
                    break;
                }
                case IconKind.Compass:
                {
                    Dot(root, Butter, s * 0.9f, 0f, 0f);
                    Dot(root, Color.white, s * 0.66f, 0f, 0f);
                    Dot(root, Butter, s * 0.54f, 0f, 0f);
                    var needle = CreateRounded("Needle", root, Coral, 0.4f);
                    needle.rectTransform.sizeDelta = new Vector2(s * 0.14f, s * 0.6f);
                    needle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 40f);
                    needle.raycastTarget = false;
                    Dot(root, Ink, s * 0.12f, 0f, 0f);
                    break;
                }
            }
            return root;
        }

        public static IconKind BranchIcon(Gotchi.Data.SkillBranch branch)
        {
            switch (branch)
            {
                case Gotchi.Data.SkillBranch.Sport: return IconKind.Ball;
                case Gotchi.Data.SkillBranch.Social: return IconKind.Chat;
                case Gotchi.Data.SkillBranch.Warrior: return IconKind.Shield;
                case Gotchi.Data.SkillBranch.Hunter: return IconKind.Paw;
                case Gotchi.Data.SkillBranch.Science: return IconKind.Flask;
                case Gotchi.Data.SkillBranch.Nature: return IconKind.Leaf;
                default: return IconKind.Compass;
            }
        }

        public static string BranchShortName(Gotchi.Data.SkillBranch branch) =>
            branch == Gotchi.Data.SkillBranch.ExplorerAdventure ? "Explorer" : branch.ToString();

        private static Image Dot(Transform parent, Color color, float diameter, float x, float y)
        {
            var dot = CreateCircle("Dot", parent, color, diameter);
            dot.rectTransform.anchoredPosition = new Vector2(x, y);
            dot.raycastTarget = false;
            return dot;
        }

        private static void Bubble(Transform parent, Color color, float diameter, float x, float y)
        {
            var bubble = CreateBubble("Bubble", parent, color, diameter);
            bubble.rectTransform.anchoredPosition = new Vector2(x, y);
            bubble.raycastTarget = false;
        }

        // Round icon button (dock / header). The icon is centred and non-interactive.
        public static Button CreateIconButton(string name, Transform parent, Color color, IconKind icon, float diameter, Action onClick, MonoBehaviour tweenHost = null, bool innerDisc = false)
        {
            var background = CreateCircle(name, parent, color, diameter);
            if (innerDisc)
            {
                var disc = CreateCircle("Disc", background.transform, Color.white, diameter * 0.72f);
                disc.raycastTarget = false;
            }
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ApplyTransition(button, false);
            var iconRect = CreateIcon(icon, background.transform, diameter * (innerDisc ? 0.46f : 0.55f), innerDisc ? Color.white : color);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.gameObject.AddComponent<CanvasGroup>();
            background.gameObject.AddComponent<PressFeedback>();
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }

        public static InputField CreateInputField(string name, Transform parent, string placeholder, InputField.ContentType contentType)
        {
            var box = CreateRoundedRadius(name, parent, Hex("F6F0F7"), Radius.Input);
            var text = CreateText("Text", box.transform, "", 28, Ink, TextAnchor.MiddleLeft);
            Fill(text.rectTransform, 24f, 24f, 8f, 8f);
            text.supportRichText = false;
            var hint = CreateText("Placeholder", box.transform, placeholder, 28, Muted, TextAnchor.MiddleLeft);
            Fill(hint.rectTransform, 24f, 24f, 8f, 8f);
            var field = box.gameObject.AddComponent<InputField>();
            field.targetGraphic = box;
            field.textComponent = text;
            field.placeholder = hint;
            field.contentType = contentType;
            field.caretWidth = 3;
            return field;
        }

        public static Slider CreateSlider(string name, Transform parent, float value, Action<float> onChange)
        {
            var track = CreatePill(name, parent, new Color(0f, 0f, 0f, 0.08f));
            var fillArea = CreateRect("FillArea", track.transform);
            Fill(fillArea, 10f, 10f, 0f, 0f);
            var fill = CreatePill("Fill", fillArea, Pink);
            Fill(fill.rectTransform);
            fill.raycastTarget = false;
            var handleArea = CreateRect("HandleArea", track.transform);
            Fill(handleArea, 14f, 14f, 0f, 0f);
            var handle = CreateCircle("Handle", handleArea, Color.white, 36f);
            var slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return slider;
        }

        // Vertical scrolling list; returns the content rect to add rows to.
        public static RectTransform CreateScrollColumn(string name, Transform parent, float spacing, RectOffset padding)
        {
            var viewport = CreateRect(name, parent);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = Color.clear;
            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            AddVerticalLayout(content.gameObject, spacing, padding);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        // Turns any graphic into a tappable element with the standard press animation.
        public static Button MakePressable(Graphic target, Action onClick)
        {
            var button = target.gameObject.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
            ApplyTransition(button);
            if (target.gameObject.GetComponent<PressFeedback>() == null) target.gameObject.AddComponent<PressFeedback>();
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }

        // Press feedback is the scale animation; no hover/press tint (icons would sink into grey). Disabled still dims.
        public static void ApplyTransition(Selectable selectable, bool dimWhenDisabled = true)
        {
            var colors = selectable.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = dimWhenDisabled ? new Color(0.82f, 0.82f, 0.82f, 0.7f) : Color.white;
            selectable.colors = colors;
        }

        // White on strong backgrounds, ink on light pastels.
        public static Color LabelColorFor(Color background)
        {
            float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            return luminance < 0.76f ? Color.white : Ink;
        }

        public static string PrettyName(string identifier)
        {
            var sb = new System.Text.StringBuilder(identifier.Length + 4);
            for (int i = 0; i < identifier.Length; i++)
            {
                char c = identifier[i];
                if (i > 0 && char.IsUpper(c)) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static void SetButtonLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }
    }
}
