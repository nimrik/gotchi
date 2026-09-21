using System;
using Gotchi.Core;
using Gotchi.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // The shell the battle panels share (Battle Club, Market, the Wild): scrim, blue card, pixel title, the wallet
    // box, a ◀ PAGE ▶ pager over swipeable pages that each hold one scrolling list, and Back. With a single page
    // the pager is left out. The owner fills the lists through Rebuild, which also keeps every list's scroll
    // position, so buying or training something never throws the page back to its top.
    public class PagedPanel
    {
        public readonly GameObject Root;
        public readonly RectTransform[] Lists;

        private readonly GameContext _ctx;
        private readonly Text _coins, _hearts;
        private readonly RectTransform _wallet;
        private readonly PagedScroll _pages;
        private readonly TabBarView _tabBar;

        public PagedPanel(Transform parent, string name, string title, string[] pageNames, UIFactory.IconKind[] pageIcons, GameContext ctx, Action onClose, MonoBehaviour host)
        {
            _ctx = ctx;
            var root = UIFactory.CreateRect(name, parent);
            Root = root.gameObject;
            var scrim = UIFactory.CreatePanel("Scrim", root, UIFactory.Scrim);
            UIFactory.Fill(scrim.rectTransform);
            scrim.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());

            var card = UIFactory.CreateCard("Panel", root, UIFactory.PanelBlue);
            UIFactory.Fill((RectTransform)card.transform.parent, 28f, 28f, 100f, 120f);
            var header = UIFactory.CreatePixelText("Header", card.transform, title.ToUpperInvariant(), 52, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            UIFactory.Place(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -84f), new Vector2(0f, -24f));
            var wallet = UIFactory.CreateWalletBox("Wallet", card.transform, out _coins, out _hearts);
            _wallet = wallet.rectTransform;
            UIFactory.Place(_wallet, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-310f, -176f), new Vector2(310f, -92f));

            bool paged = pageNames.Length > 1;
            if (paged)
            {
                var tabs = new TabBarView.Tab[pageNames.Length];
                for (int i = 0; i < tabs.Length; i++) tabs[i] = new TabBarView.Tab(pageNames[i], pageIcons != null && i < pageIcons.Length ? pageIcons[i] : (UIFactory.IconKind?)null);
                _tabBar = TabBarView.Arrows("Tabs", card.transform, tabs, host);
                UIFactory.Place(_tabBar.Root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -260f), new Vector2(-24f, -188f));
                _tabBar.OnSelected += index => _pages.GoTo(index);
            }

            _pages = PagedScroll.Create("Pages", card.transform, out RectTransform viewport);
            UIFactory.Place(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 110f), new Vector2(0f, paged ? -276f : -192f));
            Lists = new RectTransform[pageNames.Length];
            for (int i = 0; i < pageNames.Length; i++)
            {
                var page = _pages.AddPage(pageNames[i]);
                Lists[i] = UIFactory.CreateScrollColumn("List", page, UIFactory.Spacing.List, new RectOffset(24, 24, 8, 16));
                UIFactory.Fill((RectTransform)Lists[i].parent);
            }
            if (paged) _pages.OnPageChanged += index => _tabBar.Select(index, false);

            var back = UIFactory.CreateButton("Back", card.transform, "Back", UIFactory.Card, onClose, 30, host);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 96f));
            UIFactory.FitToLabel(back);

            ShowPage(0);
        }

        public int Page => _pages.Current;

        public void ShowPage(int index)
        {
            _pages.GoTo(index, false);
            _tabBar?.Select(index, false);
        }

        // Empties every list, lets `fill` build them again, and puts each list back where the player had scrolled it.
        public void Rebuild(Action<RectTransform[]> fill)
        {
            _coins.text = _ctx.Wallet.Get(CurrencyType.Soft) + " COINS";
            _hearts.text = _ctx.Wallet.Get(CurrencyType.Premium) + " HEARTS";
            float walletWidth = UIFactory.FitWalletBox(_coins, _hearts, _coins.text, _hearts.text);
            _wallet.offsetMin = new Vector2(-walletWidth * 0.5f, _wallet.offsetMin.y);
            _wallet.offsetMax = new Vector2(walletWidth * 0.5f, _wallet.offsetMax.y);

            var scrolls = new float[Lists.Length];
            for (int i = 0; i < Lists.Length; i++)
            {
                // 1 = top. A list that was empty or fitted its page has no position worth keeping (the ScrollRect reports
                // 0, the bottom, for those), so it starts from the top.
                var viewport = (RectTransform)Lists[i].parent;
                bool scrollable = Lists[i].childCount > 0 && Lists[i].rect.height > viewport.rect.height + 1f;
                scrolls[i] = scrollable ? viewport.GetComponent<ScrollRect>().verticalNormalizedPosition : 1f;
                for (int c = Lists[i].childCount - 1; c >= 0; c--)
                {
                    var old = Lists[i].GetChild(c).gameObject;
                    old.SetActive(false);              // Destroy waits for the end of the frame; this takes the row out of the layout now
                    UnityEngine.Object.Destroy(old);
                }
            }
            fill(Lists);
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < Lists.Length; i++) Lists[i].parent.GetComponent<ScrollRect>().verticalNormalizedPosition = scrolls[i];
        }
    }

    // Building blocks of a list row in those panels (12-ui-guide.md, "Battle Club"): a flat framed box, pixel title,
    // body lines, a style chip, one action button on the right. Texts stop 250 px from the right edge so the
    // widest button ("SWITCH · 150") never covers them.
    public static class PanelRows
    {
        public const float TextRight = -250f;
        public static readonly Color Held = UIFactory.Hex("FFF6DC"), Locked = UIFactory.Hex("F1ECF2"), Off = UIFactory.Hex("EEEAF0"), Good = UIFactory.Hex("2E9E62");

        public static RectTransform Box(RectTransform list, string name, float height, Color color)
        {
            var card = UIFactory.CreateCard(name, list, color, 0.8f);
            card.raycastTarget = false;
            UIFactory.SetPreferredHeight(card.transform.parent.gameObject, height);
            return card.rectTransform;
        }

        public static void Note(RectTransform list, string text, float height, bool heading = false)
        {
            var label = heading
                ? UIFactory.CreatePixelText("Heading", list, text.ToUpperInvariant(), 22, UIFactory.MenuInk, TextAnchor.LowerLeft)
                : UIFactory.CreateText("Note", list, text, 22, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            UIFactory.SetPreferredHeight(label.gameObject, height);
        }

        // `left` from the box's left edge, `top` down from its top (negative), `right` in from the right edge
        // (negative), `height` of the line.
        public static Text Pixel(RectTransform box, string text, int size, Color color, float left, float top, float right, float height)
        {
            var label = UIFactory.CreatePixelText("Label", box, text, size, color, TextAnchor.MiddleLeft);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(left, top - height), new Vector2(right, top));
            return label;
        }

        public static Text Body(RectTransform box, string text, int size, Color color, float left, float top, float right, float height)
        {
            var label = UIFactory.CreateText("Body", box, text, size, color, TextAnchor.UpperLeft);
            UIFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(left, top - height), new Vector2(right, top));
            return label;
        }

        // A chip pinned to the box's top-right: `right` in from the right edge (negative), `top` down from the top.
        public static void Chip(RectTransform box, string text, Color color, float right, float top, float width)
        {
            var chip = UIFactory.CreatePill("Chip", box, color);
            chip.raycastTarget = false;
            UIFactory.Place(chip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(right - width, top - 38f), new Vector2(right, top));
            var label = UIFactory.CreatePixelText("Text", chip.transform, text, 18, UIFactory.MenuInk, TextAnchor.MiddleCenter, false);
            UIFactory.Fill(label.rectTransform);
        }

        // The row's one action, fitted to its word, pinned to the right edge and centred vertically.
        public static Button ActionButton(RectTransform box, string label, Color color, bool interactable, Action onClick, MonoBehaviour host)
        {
            var button = UIFactory.CreateButton("Action", box, label, color, onClick, 22, host);
            UIFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-200f, -30f), new Vector2(-16f, 30f));
            UIFactory.FitToLabelRight(button, 16f, 130f);
            button.interactable = interactable;
            return button;
        }

        // A full-width primary button in a list (FIGHT!, SEARCH, SET OUT).
        public static Button Primary(RectTransform list, string label, Color color, bool interactable, Action onClick, MonoBehaviour host, float width = 440f)
        {
            var holder = UIFactory.CreateRect(label + "Holder", list);
            UIFactory.SetPreferredHeight(holder.gameObject, 100f);
            var button = UIFactory.CreateButton(label, holder, label, color, onClick, 34, host);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-width * 0.5f, -96f), new Vector2(width * 0.5f, -4f));
            button.interactable = interactable;
            return button;
        }
    }
}
