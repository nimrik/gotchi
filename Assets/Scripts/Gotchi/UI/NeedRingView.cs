using System;
using System.Collections;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // One option in the care block (Sapphire battle-menu style: four options in one box, 2×2). Each option
    // is icon + plain name + the need's percentage; the cell is the button, ▶ shows while held; on cooldown
    // the icon dims and the name reads the seconds left.
    public class NeedRingView
    {
        private const float LowTooltipThreshold = 10f;
        private const float TooltipSeconds = 5f;
        private const float AnimateThreshold = 2f;
        private static readonly Color BarYellow = UIFactory.Hex("E0A020");
        private static readonly Color BarRed = UIFactory.Hex("F06070");

        public readonly NeedType Need;
        public readonly Button Button;
        private readonly string _name;
        private readonly Text _caption;
        private readonly Text _value;
        private readonly Image _tooltip;
        private readonly Text _tooltipText;
        private readonly CanvasGroup _iconGroup;
        private readonly MonoBehaviour _host;
        private float _shown = 100f;
        private int _lastLowPercent = -1;
        private Coroutine _tooltipRoutine;

        // `index` 0..3 → column index % 2, row index / 2 inside `parent` (the shared box).
        public NeedRingView(Transform parent, CareAction action, Color color, int index, Action onClick, MonoBehaviour host)
        {
            Need = CareActionService.NeedFor(action);
            _host = host;
            _name = action.ToString().ToUpperInvariant();

            int col = index % 2, row = index / 2;
            var cell = UIFactory.CreatePanel(action + "Cell", parent, Color.clear);
            UIFactory.Place(cell.rectTransform, new Vector2(col * 0.5f, 0.5f - row * 0.5f), new Vector2(col * 0.5f + 0.5f, 1f - row * 0.5f),
                new Vector2(col == 0 ? 14f : 6f, row == 1 ? 12f : 4f), new Vector2(col == 1 ? -14f : -6f, row == 0 ? -12f : -4f));
            Button = UIFactory.MakePressable(cell, onClick);
            UIFactory.ApplyTransition(Button, false);

            var cursor = UIFactory.CreateCursor(cell.transform, 18f, UIFactory.MenuInk);
            UIFactory.Place(cursor.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, -9f), new Vector2(22f, 9f));
            cursor.enabled = false;
            var press = cell.gameObject.AddComponent<PressFeedback>();
            press.OnPressedChanged = down => { if (cursor != null) cursor.enabled = down; };

            var icon = UIFactory.CreateIcon(IconFor(action), cell.transform, 48f, Color.white);
            UIFactory.Place(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, -24f), new Vector2(78f, 24f));
            _iconGroup = icon.gameObject.AddComponent<CanvasGroup>();

            _caption = UIFactory.CreatePixelText("Caption", cell.transform, _name, 30, UIFactory.MenuInk, TextAnchor.MiddleLeft, false);
            _caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(92f, 0f), new Vector2(-110f, 0f));

            _value = UIFactory.CreatePixelText("Value", cell.transform, "100%", 30, UIFactory.MenuInk, TextAnchor.MiddleRight, false);
            _value.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_value.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-130f, 0f), new Vector2(-18f, 0f));

            _tooltip = UIFactory.CreatePill("Tooltip", cell.transform, UIFactory.Ink);
            UIFactory.Place(_tooltip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-56f, -24f), new Vector2(56f, 24f));
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
                case CareAction.Clean: return UIFactory.IconKind.Shower;
                case CareAction.Rest: return UIFactory.IconKind.Moon;
                default: return UIFactory.IconKind.Ball;
            }
        }

        public void SetValue(float value, bool animate)
        {
            bool bigJump = Mathf.Abs(value - _shown) >= AnimateThreshold;
            int from = Mathf.RoundToInt(_shown), to = Mathf.RoundToInt(value);
            _shown = value;
            if (animate && bigJump) _host.StartCoroutine(SimpleTween.CountUp(_value, from, to, 0.35f, "%"));
            else _value.text = to + "%";
            _value.color = value > 50f ? UIFactory.MenuInk : value > 20f ? BarYellow : BarRed;
            if (animate && bigJump) _host.StartCoroutine(SimpleTween.PunchScale(_value.rectTransform, 0.12f, 0.25f));

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
            if (_iconGroup != null) _iconGroup.alpha = ready ? 1f : 0.4f;
            _caption.text = ready ? _name : Mathf.CeilToInt(remaining) + "S";
            _caption.color = ready ? UIFactory.MenuInk : UIFactory.Muted;
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
