using System;
using Gotchi.MiniGames;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // One action of the camp block (four in one box, 2x2): REST, FOCUS, FEED, GROOM. It replaced the need cell on
    // 2026-09-21. Icon + pixel name, under the name what the action does ("HP +40%", "ATK +10% · 3 BATTLES"), and
    // on the right where it stands: READY, the seconds of cooldown left ("45S"), FULL when the bar has nothing to
    // gain, or the battles a buff still lasts ("2 LEFT", in green). The whole cell is the button and shows ▶ while
    // held; while a press would do nothing (cooldown, full bar, full buff) the icon is dimmed.
    public class CampCellView
    {
        public readonly CampAction Action;
        public readonly Button Button;
        private readonly Text _caption, _effect, _value;
        private readonly CanvasGroup _iconGroup;

        // `index` 0..3 → column index % 2, row index / 2 inside `parent` (the shared box).
        public CampCellView(Transform parent, CampActionDef def, int index, Action onClick)
        {
            Action = def.Action;

            int col = index % 2, row = index / 2;
            var cell = UIFactory.CreatePanel(def.Name + "Cell", parent, Color.clear);
            UIFactory.Place(cell.rectTransform, new Vector2(col * 0.5f, 0.5f - row * 0.5f), new Vector2(col * 0.5f + 0.5f, 1f - row * 0.5f),
                new Vector2(col == 0 ? 14f : 6f, row == 1 ? 12f : 4f), new Vector2(col == 1 ? -14f : -6f, row == 0 ? -12f : -4f));
            Button = UIFactory.MakePressable(cell, onClick);
            UIFactory.ApplyTransition(Button, false);

            var cursor = UIFactory.CreateCursor(cell.transform, 18f, UIFactory.MenuInk);
            UIFactory.Place(cursor.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, -9f), new Vector2(22f, 9f));
            cursor.enabled = false;
            cell.gameObject.AddComponent<PressFeedback>().OnPressedChanged = down => { if (cursor != null) cursor.enabled = down; };

            var icon = UIFactory.CreateIcon(IconFor(def.Action), cell.transform, 48f, Color.white);
            UIFactory.Place(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, -24f), new Vector2(78f, 24f));
            _iconGroup = icon.gameObject.AddComponent<CanvasGroup>();

            _caption = UIFactory.CreatePixelText("Caption", cell.transform, def.Name, 30, UIFactory.MenuInk, TextAnchor.MiddleLeft, false);
            _caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(92f, 22f), new Vector2(-110f, 0f));
            _effect = UIFactory.CreatePixelText("Effect", cell.transform, def.Effect, 20, EffectInk, TextAnchor.MiddleLeft, false);
            _effect.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_effect.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(94f, 2f), new Vector2(-110f, 28f));

            _value = UIFactory.CreatePixelText("Value", cell.transform, "", 26, UIFactory.MenuInk, TextAnchor.MiddleRight, false);
            _value.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_value.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-130f, 0f), new Vector2(-18f, 0f));
        }

        // The small line under the name: darker than Muted, which is too faint at this size.
        private static readonly Color EffectInk = Color.Lerp(UIFactory.Muted, UIFactory.MenuInk, 0.5f);

        private static UIFactory.IconKind IconFor(CampAction action)
        {
            switch (action)
            {
                case CampAction.Rest: return UIFactory.IconKind.Moon;
                case CampAction.Focus: return UIFactory.IconKind.Sparkle;
                case CampAction.Feed: return UIFactory.IconKind.Cookie;
                default: return UIFactory.IconKind.Bubbles;
            }
        }

        // Brings the cell up to date. `effect` is the line under the name; `battlesLeft` > 0 means a buff is working
        // (the line and the count turn green); `useful` is false when a press would be wasted (bar full, buff full).
        public void Refresh(float cooldown, string effect, int battlesLeft, bool useful)
        {
            bool ready = cooldown <= 0f && useful;
            Button.interactable = ready;
            _iconGroup.alpha = ready ? 1f : 0.4f;
            _caption.color = ready ? UIFactory.MenuInk : EffectInk;
            _effect.text = effect;
            _effect.color = battlesLeft > 0 ? PanelRows.Good : EffectInk;
            if (battlesLeft > 0) { _value.text = battlesLeft + " LEFT"; _value.color = PanelRows.Good; }
            else if (cooldown > 0f) { _value.text = Mathf.CeilToInt(cooldown) + "S"; _value.color = EffectInk; }
            else if (!useful) { _value.text = "FULL"; _value.color = EffectInk; }
            else { _value.text = "READY"; _value.color = PanelRows.Good; }
        }
    }
}
