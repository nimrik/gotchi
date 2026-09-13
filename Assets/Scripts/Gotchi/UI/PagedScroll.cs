using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // Horizontal pages the user can swipe between (snapping) or jump to via tabs.
    public class PagedScroll : MonoBehaviour, IEndDragHandler, IBeginDragHandler
    {
        private ScrollRect _scroll;
        private RectTransform _content;
        private readonly List<RectTransform> _pages = new List<RectTransform>();
        private Coroutine _snap;
        private bool _dragging;

        public int Current { get; private set; }
        public event Action<int> OnPageChanged;

        public static PagedScroll Create(string name, Transform parent, out RectTransform viewport)
        {
            viewport = UIFactory.CreateRect(name, parent);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = Color.clear;
            var content = UIFactory.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            var paged = viewport.gameObject.AddComponent<PagedScroll>();
            paged._scroll = scroll;
            paged._content = content;
            return paged;
        }

        public RectTransform AddPage(string name)
        {
            var page = UIFactory.CreateRect(name, _content);
            page.anchorMin = new Vector2(0f, 0f);
            page.anchorMax = new Vector2(0f, 1f);
            page.pivot = new Vector2(0f, 0.5f);
            _pages.Add(page);
            Layout();
            return page;
        }

        public float PageWidth => ((RectTransform)transform).rect.width;

        private void OnRectTransformDimensionsChange() => Layout();

        private void Layout()
        {
            float width = PageWidth;
            if (width <= 0f || _content == null) return;
            _content.sizeDelta = new Vector2(width * _pages.Count, 0f);
            for (int i = 0; i < _pages.Count; i++)
            {
                _pages[i].sizeDelta = new Vector2(width, 0f);
                _pages[i].anchoredPosition = new Vector2(i * width, 0f);
            }
            if (!_dragging && _snap == null) _content.anchoredPosition = new Vector2(-Current * width, 0f);
        }

        public void GoTo(int index, bool animate = true)
        {
            index = Mathf.Clamp(index, 0, Mathf.Max(0, _pages.Count - 1));
            bool changed = index != Current;
            Current = index;
            if (_snap != null) StopCoroutine(_snap);
            if (animate && gameObject.activeInHierarchy) _snap = StartCoroutine(SnapTo(-index * PageWidth));
            else _content.anchoredPosition = new Vector2(-index * PageWidth, 0f);
            if (changed) OnPageChanged?.Invoke(index);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            if (_snap != null) { StopCoroutine(_snap); _snap = null; }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            float width = PageWidth;
            int nearest = Mathf.RoundToInt(-_content.anchoredPosition.x / width);
            float velocity = eventData.delta.x;
            if (velocity < -12f) nearest = Mathf.Max(nearest, Current + 1);
            else if (velocity > 12f) nearest = Mathf.Min(nearest, Current - 1);
            GoTo(nearest);
        }

        private IEnumerator SnapTo(float targetX)
        {
            float start = _content.anchoredPosition.x;
            float elapsed = 0f;
            const float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _content.anchoredPosition = new Vector2(Mathf.Lerp(start, targetX, SimpleTween.EaseOutCubic(Mathf.Clamp01(elapsed / duration))), 0f);
                yield return null;
            }
            _content.anchoredPosition = new Vector2(targetX, 0f);
            _snap = null;
        }
    }
}
