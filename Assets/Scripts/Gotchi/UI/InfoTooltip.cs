using System.Collections;
using Gotchi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // One shared info box. Tap a meter (the XP bar, the stage bar, a branch bar) and the full numbers for that
    // block pop up above it; tap anywhere (the same meter included), open another one, or wait, and it goes away.
    // Ink box with white text (12-ui-guide.md, "Tooltip"). It never catches taps, so nothing under it is blocked.
    public static class InfoTooltip
    {
        private const float Seconds = 6f;
        private const float MaxWidth = 900f;
        private const float Margin = 24f;
        private const float PadX = 28f, PadY = 20f, TitleHeight = 36f, Gap = 8f;

        private static RectTransform _box;
        private static Text _title, _body;
        private static RectTransform _anchor;
        private static Coroutine _routine;
        private static MonoBehaviour _host;
        private static RectTransform _dismissedAnchor;   // closed by the press that is about to click this same meter
        private static float _dismissedAt;

        public static bool IsOpenFor(RectTransform anchor) => _box != null && _box.gameObject.activeSelf && _anchor == anchor;

        public static void Toggle(RectTransform anchor, string title, string body, MonoBehaviour host)
        {
            // A press anywhere closes the box; when that press lands on the box's own meter, its click must not reopen it.
            bool justDismissed = _dismissedAnchor == anchor && Time.unscaledTime - _dismissedAt < 0.6f;
            _dismissedAnchor = null;
            if (justDismissed) return;
            if (IsOpenFor(anchor)) Hide();
            else Show(anchor, title, body, host);
        }

        public static void Show(RectTransform anchor, string title, string body, MonoBehaviour host)
        {
            if (anchor == null || host == null) return;
            var canvas = anchor.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var root = (RectTransform)canvas.rootCanvas.transform;
            if (_box == null || _box.parent != root) Build(root);

            StopRoutine();
            _anchor = anchor;
            _host = host;
            _title.text = title;
            _body.text = body;

            // Width from the longest line, then the body wraps inside it and gives the height.
            float width = Mathf.Min(MaxWidth, Mathf.Max(_title.preferredWidth, _body.preferredWidth) + PadX * 2f);
            width = Mathf.Min(width, root.rect.width - Margin * 2f);
            _box.sizeDelta = new Vector2(width, 100f);
            float height = PadY + TitleHeight + Gap + _body.preferredHeight + PadY;
            _box.sizeDelta = new Vector2(width, height);

            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector3 bottomLeft = root.InverseTransformPoint(corners[0]), topRight = root.InverseTransformPoint(corners[2]);
            float halfRootW = root.rect.width * 0.5f, halfRootH = root.rect.height * 0.5f;
            float x = Mathf.Clamp((bottomLeft.x + topRight.x) * 0.5f, -halfRootW + Margin + width * 0.5f, halfRootW - Margin - width * 0.5f);
            float y = topRight.y + 14f + height * 0.5f;                                   // above the meter…
            if (y + height * 0.5f > halfRootH - Margin) y = bottomLeft.y - 14f - height * 0.5f;   // …or below when there is no room
            _box.anchoredPosition = new Vector2(x, y);

            _box.SetAsLastSibling();
            _box.gameObject.SetActive(true);
            _routine = host.StartCoroutine(Routine());
        }

        public static void Hide()
        {
            StopRoutine();
            if (_box != null) _box.gameObject.SetActive(false);
            _anchor = null;
        }

        private static void StopRoutine()
        {
            if (_routine != null && _host != null) _host.StopCoroutine(_routine);
            _routine = null;
        }

        private static IEnumerator Routine()
        {
            yield return SimpleTween.PopIn(_box, 0.18f);
            // Stays for a few seconds, and leaves at once when its meter goes away (the panel behind it was closed).
            for (float t = 0f; t < Seconds && _anchor != null && _anchor.gameObject.activeInHierarchy; t += Time.unscaledDeltaTime)
            {
                if (Input.GetMouseButtonDown(0)) { _dismissedAnchor = _anchor; _dismissedAt = Time.unscaledTime; break; }   // touches count as the left button
                yield return null;
            }
            if (_box != null) _box.gameObject.SetActive(false);
            _anchor = null;
            _routine = null;
        }

        private static void Build(RectTransform root)
        {
            var box = UIFactory.CreatePill("InfoTooltip", root, UIFactory.Ink);
            box.raycastTarget = false;
            _box = box.rectTransform;
            _box.anchorMin = _box.anchorMax = _box.pivot = new Vector2(0.5f, 0.5f);

            _title = UIFactory.CreatePixelText("Title", _box, "", 28, Color.white, TextAnchor.UpperLeft, false);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _title.raycastTarget = false;
            UIFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(PadX, -PadY - TitleHeight), new Vector2(-PadX, -PadY));

            _body = UIFactory.CreateText("Body", _box, "", 23, new Color(1f, 1f, 1f, 0.92f), TextAnchor.UpperLeft);
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;
            _body.lineSpacing = 1.15f;
            _body.raycastTarget = false;
            UIFactory.Place(_body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(PadX, PadY), new Vector2(-PadX, -PadY - TitleHeight - Gap));

            _box.gameObject.SetActive(false);
        }
    }
}
