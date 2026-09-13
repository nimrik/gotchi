using System;
using System.Collections;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // A care button wrapped in a radial meter of its need, with a cooldown badge and a low-need tooltip.
    public class NeedRingView
    {
        private const float LowTooltipThreshold = 10f;
        private const float TooltipSeconds = 5f;
        private const float AnimateThreshold = 2f;

        public readonly NeedType Need;
        public readonly Button Button;
        private readonly Image _ring;
        private readonly Image _badge;
        private readonly Text _badgeText;
        private readonly Image _tooltip;
        private readonly Text _tooltipText;
        private readonly Color _color;
        private readonly CanvasGroup _iconGroup;
        private readonly MonoBehaviour _host;
        private float _shown = 100f;
        private int _lastLowPercent = -1;
        private Coroutine _tooltipRoutine;

        public NeedRingView(Transform parent, CareAction action, Color color, float x, Action onClick, MonoBehaviour host)
        {
            Need = CareActionService.NeedFor(action);
            _color = color;
            _host = host;

            var cell = UIFactory.CreateRect(action + "Cell", parent);
            UIFactory.Place(cell, new Vector2(x, 0f), new Vector2(x, 1f), new Vector2(-80f, 0f), new Vector2(80f, 0f));

            var track = UIFactory.CreatePanel("Track", cell, new Color(0f, 0f, 0f, 0.06f));
            track.sprite = UIFactory.RingSprite;
            UIFactory.Place(track.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-78f, -170f), new Vector2(78f, -14f));
            track.raycastTarget = false;

            _ring = UIFactory.CreatePanel("Ring", cell, color);
            _ring.sprite = UIFactory.RingSprite;
            _ring.type = Image.Type.Filled;
            _ring.fillMethod = Image.FillMethod.Radial360;
            _ring.fillOrigin = (int)Image.Origin360.Top;
            _ring.fillClockwise = true;
            _ring.fillAmount = 1f;
            UIFactory.Place(_ring.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-78f, -170f), new Vector2(78f, -14f));
            _ring.raycastTarget = false;

            Button = UIFactory.CreateIconButton("Button", cell, Color.white, IconFor(action), 116f, onClick, host);
            _iconGroup = Button.GetComponentInChildren<CanvasGroup>();
            UIFactory.Place((RectTransform)Button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-58f, -150f), new Vector2(58f, -34f));

            _badge = UIFactory.CreatePill("Cooldown", Button.transform, UIFactory.Ink);
            UIFactory.Place(_badge.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-64f, -8f), new Vector2(8f, 34f));
            _badge.raycastTarget = false;
            _badgeText = UIFactory.CreateText("Text", _badge.transform, "", 20, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(_badgeText.rectTransform);
            _badge.gameObject.SetActive(false);

            var caption = UIFactory.CreateText("Caption", cell, action.ToString(), 18, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 8f), new Vector2(10f, 44f));

            _tooltip = UIFactory.CreatePill("Tooltip", cell, UIFactory.Ink);
            UIFactory.Place(_tooltip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-54f, -4f), new Vector2(54f, 40f));
            _tooltip.raycastTarget = false;
            _tooltipText = UIFactory.CreateText("Text", _tooltip.transform, "", 22, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(_tooltipText.rectTransform);
            _tooltip.gameObject.SetActive(false);
        }

        private static UIFactory.IconKind IconFor(CareAction action)
        {
            switch (action)
            {
                case CareAction.Feed: return UIFactory.IconKind.Cookie;
                case CareAction.Clean: return UIFactory.IconKind.Bubbles;
                case CareAction.Rest: return UIFactory.IconKind.Moon;
                default: return UIFactory.IconKind.Ball;
            }
        }

        public void SetValue(float value, bool animate)
        {
            float normalized = Mathf.Clamp01(value / NeedsSystem.Max);
            bool bigJump = Mathf.Abs(value - _shown) >= AnimateThreshold;
            _shown = value;
            if (animate && bigJump) _host.StartCoroutine(SimpleTween.FillTo(_ring, normalized));
            else _ring.fillAmount = normalized;
            _ring.color = value < 30f ? Color.Lerp(_color, UIFactory.PinkDark, 0.5f) : _color;

            if (value < LowTooltipThreshold)
            {
                int percent = Mathf.RoundToInt(value);
                if (percent != _lastLowPercent)
                {
                    _lastLowPercent = percent;
                    ShowTooltip(percent + "%");
                }
            }
            else _lastLowPercent = -1;
        }

        public void SetCooldown(float remaining)
        {
            bool ready = remaining <= 0f;
            Button.interactable = ready;
            if (_iconGroup != null) _iconGroup.alpha = ready ? 1f : 0.45f;
            _badge.gameObject.SetActive(!ready);
            if (!ready) _badgeText.text = Mathf.CeilToInt(remaining) + "s";
        }

        private void ShowTooltip(string text)
        {
            if (_tooltipRoutine != null) _host.StopCoroutine(_tooltipRoutine);
            _tooltipRoutine = _host.StartCoroutine(TooltipRoutine(text));
        }

        private IEnumerator TooltipRoutine(string text)
        {
            _tooltipText.text = text;
            _tooltip.gameObject.SetActive(true);
            yield return SimpleTween.PopIn(_tooltip.transform, 0.2f);
            yield return new WaitForSeconds(TooltipSeconds);
            _tooltip.gameObject.SetActive(false);
            _tooltipRoutine = null;
        }
    }
}
