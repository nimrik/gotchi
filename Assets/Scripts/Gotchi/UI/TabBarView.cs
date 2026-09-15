using System;
using System.Collections.Generic;
using Gotchi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // Screen / tab switchers in the Sapphire box language. Four looks, one API:
    //   Segmented – one box split into segments, the chosen one white, the rest grey.
    //   Chips     – separate boxes with icon + label, wrapping into rows; chosen one white.
    //   Underline – plain labels with a thick bar under the active one.
    //   Arrows    – ◀ TITLE ▶ pager with page dots (Pokémon Bag pockets); pairs with swipeable pages.
    public class TabBarView
    {
        public struct Tab
        {
            public string Label;
            public UIFactory.IconKind? Icon;
            public Tab(string label, UIFactory.IconKind? icon = null) { Label = label; Icon = icon; }
        }

        public readonly RectTransform Root;
        public int Current { get; private set; } = -1;
        public event Action<int> OnSelected;

        private readonly int _count;
        private Action<int> _apply;

        private TabBarView(RectTransform root, int count)
        {
            Root = root;
            _count = count;
        }

        public void Select(int index, bool notify = true)
        {
            index = (index % _count + _count) % _count;
            Current = index;
            _apply?.Invoke(index);
            if (notify) OnSelected?.Invoke(index);
        }

        public static TabBarView Segmented(string name, Transform parent, Tab[] tabs, MonoBehaviour host)
        {
            var root = UIFactory.CreateRect(name, parent);
            var view = new TabBarView(root, tabs.Length);
            UIFactory.AddHorizontalLayout(root.gameObject, 8f, new RectOffset(0, 0, 0, 0), true);
            var segments = new Image[tabs.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                segments[i] = UIFactory.CreateFrame(tabs[i].Label, root, UIFactory.MenuGrey);
                var label = UIFactory.CreatePixelText("Label", segments[i].transform, tabs[i].Label.ToUpperInvariant(), 22, UIFactory.MenuInk, TextAnchor.MiddleCenter);
                UIFactory.Fill(label.rectTransform);
                var button = UIFactory.MakePressable(segments[i], () => view.Select(index));
                UIFactory.ApplyTransition(button, false);
            }
            view._apply = idx => { for (int j = 0; j < segments.Length; j++) segments[j].color = j == idx ? UIFactory.Card : UIFactory.MenuGrey; };
            return view;
        }

        public static TabBarView Chips(string name, Transform parent, Tab[] tabs, MonoBehaviour host, int columns = 0, float rowHeight = 72f, float chipWidth = 0f)
        {
            if (columns <= 0) columns = tabs.Length;
            var root = UIFactory.CreateRect(name, parent);
            UIFactory.AddVerticalLayout(root.gameObject, 8f, new RectOffset(0, 0, 0, 0));
            var view = new TabBarView(root, tabs.Length);
            var chips = new Image[tabs.Length];
            RectTransform row = null;
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                if (i % columns == 0)
                {
                    row = UIFactory.CreateRect("Row", root);
                    UIFactory.SetPreferredHeight(row.gameObject, rowHeight);
                    var rowLayout = UIFactory.AddHorizontalLayout(row.gameObject, 8f, new RectOffset(0, 0, 0, 0), chipWidth <= 0f);
                    rowLayout.childAlignment = TextAnchor.MiddleCenter;
                }
                chips[i] = UIFactory.CreateFrame(tabs[i].Label, row, UIFactory.MenuGrey);
                if (chipWidth > 0f) UIFactory.SetWidth(chips[i].gameObject, chipWidth, 0f);
                float textLeft = 16f;
                if (tabs[i].Icon.HasValue)
                {
                    var disc = UIFactory.CreateCircle("Disc", chips[i].transform, Color.white, 44f);
                    UIFactory.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -22f), new Vector2(58f, 22f));
                    disc.raycastTarget = false;
                    var icon = UIFactory.CreateIcon(tabs[i].Icon.Value, disc.transform, 30f, Color.white);
                    icon.anchoredPosition = Vector2.zero;
                    textLeft = 66f;
                }
                var label = UIFactory.CreatePixelText("Label", chips[i].transform, tabs[i].Label.ToUpperInvariant(), 18, UIFactory.MenuInk, TextAnchor.MiddleLeft);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                UIFactory.Fill(label.rectTransform, textLeft, 10f, 0f, 0f);
                var button = UIFactory.MakePressable(chips[i], () => view.Select(index));
                UIFactory.ApplyTransition(button, false);
            }
            // Without a fixed width, pad the last row so chips keep the same width.
            int remainder = tabs.Length % columns;
            if (chipWidth <= 0f && remainder != 0 && row != null)
                for (int i = remainder; i < columns; i++) UIFactory.CreateRect("Spacer", row);
            view._apply = idx => { for (int j = 0; j < chips.Length; j++) chips[j].color = j == idx ? UIFactory.Card : UIFactory.MenuGrey; };
            return view;
        }

        public static TabBarView Underline(string name, Transform parent, Tab[] tabs, MonoBehaviour host)
        {
            var root = UIFactory.CreateRect(name, parent);
            UIFactory.AddHorizontalLayout(root.gameObject, 0f, new RectOffset(0, 0, 0, 0), true);
            var view = new TabBarView(root, tabs.Length);
            var labels = new Text[tabs.Length];
            var lines = new GameObject[tabs.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                var cell = UIFactory.CreatePanel(tabs[i].Label, root, Color.clear);
                labels[i] = UIFactory.CreatePixelText("Label", cell.transform, tabs[i].Label.ToUpperInvariant(), 22, UIFactory.Muted, TextAnchor.MiddleCenter);
                UIFactory.Fill(labels[i].rectTransform, 0f, 0f, 0f, 10f);
                var line = UIFactory.CreatePanel("Line", cell.transform, UIFactory.FrameDark);
                UIFactory.Place(line.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 0f), new Vector2(-10f, 6f));
                line.raycastTarget = false;
                lines[i] = line.gameObject;
                var button = UIFactory.MakePressable(cell, () => view.Select(index));
                UIFactory.ApplyTransition(button, false);
            }
            view._apply = idx =>
            {
                for (int j = 0; j < labels.Length; j++)
                {
                    labels[j].color = j == idx ? UIFactory.MenuInk : UIFactory.Muted;
                    lines[j].SetActive(j == idx);
                }
                if (host != null && host.gameObject.activeInHierarchy) host.StartCoroutine(SimpleTween.PopIn(lines[idx].transform, 0.15f));
            };
            return view;
        }

        public static TabBarView Arrows(string name, Transform parent, Tab[] tabs, MonoBehaviour host)
        {
            var root = UIFactory.CreateRect(name, parent);
            var view = new TabBarView(root, tabs.Length);
            const float buttonSize = 72f;

            var prev = UIFactory.CreateFrame("Prev", root, UIFactory.Card);
            UIFactory.Place(prev.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -buttonSize), new Vector2(buttonSize, 0f));
            var prevCursor = UIFactory.CreateCursor(prev.transform, 26f, UIFactory.MenuInk);
            prevCursor.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            UIFactory.ApplyTransition(UIFactory.MakePressable(prev, () => view.Select(view.Current - 1)), false);

            var next = UIFactory.CreateFrame("Next", root, UIFactory.Card);
            UIFactory.Place(next.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-buttonSize, -buttonSize), new Vector2(0f, 0f));
            UIFactory.CreateCursor(next.transform, 26f, UIFactory.MenuInk);
            UIFactory.ApplyTransition(UIFactory.MakePressable(next, () => view.Select(view.Current + 1)), false);

            var title = UIFactory.CreateFrame("Title", root, UIFactory.Card);
            title.raycastTarget = false;
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(buttonSize + 12f, -buttonSize), new Vector2(-buttonSize - 12f, 0f));
            var disc = UIFactory.CreateCircle("Disc", title.transform, UIFactory.Hex("FBF6F9"), 48f);
            UIFactory.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, -24f), new Vector2(64f, 24f));
            disc.raycastTarget = false;
            var label = UIFactory.CreatePixelText("Label", title.transform, "", 26, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            UIFactory.Fill(label.rectTransform, 72f, 110f, 0f, 0f);
            var counter = UIFactory.CreatePixelText("Counter", title.transform, "", 20, UIFactory.Muted, TextAnchor.MiddleRight);
            UIFactory.Place(counter.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-110f, 0f), new Vector2(-20f, 0f));

            RectTransform icon = null;
            view._apply = idx =>
            {
                label.text = tabs[idx].Label.ToUpperInvariant();
                if (icon != null) UnityEngine.Object.Destroy(icon.gameObject);
                icon = null;
                disc.gameObject.SetActive(tabs[idx].Icon.HasValue);
                if (tabs[idx].Icon.HasValue)
                {
                    icon = UIFactory.CreateIcon(tabs[idx].Icon.Value, disc.transform, 32f, UIFactory.Hex("FBF6F9"));
                    icon.anchoredPosition = Vector2.zero;
                }
                counter.text = $"{idx + 1}/{tabs.Length}";
                if (host != null && host.gameObject.activeInHierarchy) host.StartCoroutine(SimpleTween.PunchScale(title.transform, 0.04f, 0.18f));
            };
            return view;
        }
    }
}
