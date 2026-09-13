using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Economy;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    public class ShopPanelView
    {
        private readonly GameContext _ctx;
        private readonly Action<string> _toast;
        private readonly MonoBehaviour _host;
        private readonly Text _title;
        private readonly Text _wallet;
        private readonly PagedScroll _pages;
        private readonly List<ShopCategory> _categories = new List<ShopCategory>();
        private readonly Dictionary<ShopCategory, Image> _tabs = new Dictionary<ShopCategory, Image>();
        private readonly Dictionary<ShopCategory, RectTransform> _grids = new Dictionary<ShopCategory, RectTransform>();

        public readonly GameObject Root;

        public ShopPanelView(Transform parent, GameContext ctx, Action<string> toast, Action onClose, MonoBehaviour host, bool allowRealMoney = true)
        {
            _ctx = ctx;
            _toast = toast;
            _host = host;

            var root = UIFactory.CreateRect("ShopRoot", parent);
            Root = root.gameObject;
            var scrim = UIFactory.CreatePanel("Scrim", root, UIFactory.Scrim);
            UIFactory.Fill(scrim.rectTransform);
            scrim.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());

            var card = UIFactory.CreateCard("Panel", root, UIFactory.Cream);
            UIFactory.Fill((RectTransform)card.transform.parent, 28f, 28f, 100f, 120f);
            _title = UIFactory.CreateText("Header", card.transform, "Shop", 44, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -84f), new Vector2(0f, -24f));
            _wallet = UIFactory.CreateText("Wallet", card.transform, "", 24, UIFactory.Muted, TextAnchor.MiddleCenter);
            UIFactory.Place(_wallet.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -118f), new Vector2(0f, -84f));

            if (allowRealMoney) _categories.Add(ShopCategory.Gems);
            _categories.AddRange(new[] { ShopCategory.Boosts, ShopCategory.Style, ShopCategory.Room, ShopCategory.Helpers });

            var tabs = UIFactory.CreateRect("Tabs", card.transform);
            UIFactory.Place(tabs, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -232f), new Vector2(-24f, -126f));
            UIFactory.AddHorizontalLayout(tabs.gameObject, 8f, new RectOffset(0, 0, 0, 0), true);
            foreach (var category in _categories) AddTab(tabs, category);

            _pages = PagedScroll.Create("Pages", card.transform, out RectTransform viewport);
            UIFactory.Place(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 110f), new Vector2(0f, -244f));
            foreach (var category in _categories)
            {
                var page = _pages.AddPage(category.ToString());
                var grid = UIFactory.CreateRect("Grid", page);
                UIFactory.Fill(grid, 24f, 24f, 8f, 8f);
                var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
                layout.spacing = new Vector2(UIFactory.Spacing.List, UIFactory.Spacing.List);
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 2;
                _grids[category] = grid;
            }
            _pages.OnPageChanged += index => { HighlightTab(); _host.StartCoroutine(PopCards(_categories[index])); };

            var back = UIFactory.CreateButton("Back", card.transform, "Back", UIFactory.Card, onClose, 30, host);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 96f));

            _pages.GoTo(_categories.IndexOf(ShopCategory.Boosts), false);
            Refresh();
        }

        private static (UIFactory.IconKind icon, string label) TabInfo(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Gems: return (UIFactory.IconKind.Gem, "Gems");
                case ShopCategory.Boosts: return (UIFactory.IconKind.Sparkle, "Boosts");
                case ShopCategory.Style: return (UIFactory.IconKind.Bag, "Style");
                case ShopCategory.Room: return (UIFactory.IconKind.Leaf, "Room");
                default: return (UIFactory.IconKind.Cookie, "Helpers");
            }
        }

        private void AddTab(Transform parent, ShopCategory category)
        {
            var (icon, label) = TabInfo(category);
            var cell = UIFactory.CreateRect("Tab" + category, parent);
            var button = UIFactory.CreateIconButton("Button", cell, UIFactory.Card, icon, 72f, () => _pages.GoTo(_categories.IndexOf(category)), _host);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-36f, -72f), new Vector2(36f, 0f));
            var text = UIFactory.CreateText("Label", cell, label, 18, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 28f));
            _tabs[category] = button.image;
        }

        private void HighlightTab()
        {
            var current = _categories[_pages.Current];
            foreach (var pair in _tabs) pair.Value.color = pair.Key == current ? UIFactory.Hex("FFE1EA") : UIFactory.Card;
            var (_, label) = TabInfo(current);
            _title.text = label;
        }

        public void Refresh()
        {
            _wallet.text = $"{_ctx.Wallet.Get(CurrencyType.Soft)} coins · {_ctx.Wallet.Get(CurrencyType.Premium)} gems";
            HighlightTab();
            foreach (var category in _categories) BuildPage(category);
        }

        private void BuildPage(ShopCategory category)
        {
            var grid = _grids[category];
            for (int i = grid.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(grid.GetChild(i).gameObject);
            float width = Mathf.Max(200f, (_pages.PageWidth - 48f - UIFactory.Spacing.List) / 2f);
            grid.GetComponent<GridLayoutGroup>().cellSize = new Vector2(width, 344f);

            if (category == ShopCategory.Helpers)
            {
                foreach (var def in AutomationSystem.Catalog)
                {
                    AutomationDefinition captured = def;
                    bool unlocked = _ctx.Automation.IsUnlocked(def.Id);
                    bool can = _ctx.Automation.CanUnlock(def, _ctx.Skills, _ctx.Wallet);
                    var cardRect = Card(grid, def.DisplayName, $"Slows {def.Need} decay. Needs {def.RequiredXp} {UIFactory.PrettyName(def.RequiredBranch.ToString())} XP.", $"{def.SoftCost} coins",
                        unlocked ? "Active" : "Unlock", UIFactory.Mint, !unlocked && can, () =>
                        {
                            bool ok = _ctx.Automation.TryUnlock(captured, _ctx.Skills, _ctx.Wallet);
                            _toast(ok ? $"{captured.DisplayName} is on!" : "Not yet — check the requirements.");
                            Refresh();
                        });
                    IconPreview(cardRect, def.Need == NeedType.Hunger ? UIFactory.IconKind.Cookie : def.Need == NeedType.Hygiene ? UIFactory.IconKind.Bubbles : def.Need == NeedType.Energy ? UIFactory.IconKind.Moon : UIFactory.IconKind.Ball);
                }
                return;
            }

            foreach (var item in ShopCatalog.Items)
            {
                if (item.Category != category) continue;
                ShopItem captured = item;
                bool owned = _ctx.Shop.Owns(item);
                bool equippable = _ctx.Shop.CanEquip(item);
                bool equipped = _ctx.Shop.IsEquipped(item);
                string price = item.RealMoney ? "App Store" : $"{item.Cost} {(item.CostCurrency == CurrencyType.Soft ? "coins" : "gems")}";
                string action; Color color; bool interactable; Action onClick;
                if (owned && equippable)
                {
                    bool isRoom = item.Kind == ShopItemKind.RoomDecor;
                    action = equipped ? (isRoom ? "In use" : "Wearing") : (isRoom ? "Use" : "Wear");
                    color = UIFactory.Mint; interactable = !equipped;
                    onClick = () => { _ctx.Shop.ToggleEquip(captured); Refresh(); };
                }
                else if (owned)
                {
                    action = "Owned"; color = UIFactory.Hex("EEEAF0"); interactable = false; onClick = () => { };
                }
                else
                {
                    action = "Buy"; color = item.RealMoney ? UIFactory.Lavender : UIFactory.Butter;
                    interactable = _ctx.Shop.CanAfford(item);
                    onClick = () => _ctx.Shop.Buy(captured, result => { _toast(result.Success ? $"Got {captured.DisplayName}!" : result.Message); Refresh(); });
                }
                string status = "";
                if (item.Kind == ShopItemKind.RewardBoost && _ctx.Boosts.RewardBoostActive) status = $"\\nActive · {(int)_ctx.Boosts.RewardBoostRemaining.TotalMinutes + 1} min left";
                if (item.Kind == ShopItemKind.CooldownBoost && _ctx.Boosts.CooldownBoostActive) status = $"\\nActive · {(int)_ctx.Boosts.CooldownBoostRemaining.TotalMinutes + 1} min left";
                if (item.Kind == ShopItemKind.StreakShield && _ctx.Boosts.StreakShields > 0) status = $"\\n{_ctx.Boosts.StreakShields} ready";
                var cardRect = Card(grid, item.DisplayName, item.Description + status, price, action, color, interactable, onClick);

                if (item.Kind == ShopItemKind.Cosmetic)
                {
                    var anchor = UIFactory.CreateRect("Preview", cardRect);
                    UIFactory.Place(anchor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(0f, -96f));
                    var preview = new PetPortraitView(anchor, _ctx.Data.species, _host, 120f);
                    preview.SetEmotion(EmotionType.Joy, false);
                    preview.SetAccessory(item.Id);
                }
                else if (item.Kind == ShopItemKind.RoomDecor && item.Id.StartsWith("rug_"))
                {
                    Color rug = item.Id == "rug_mint" ? UIFactory.Hex("BFE9D0") : item.Id == "rug_sky" ? UIFactory.Hex("BFE0FF") : UIFactory.Hex("FFC4D6");
                    var swatch = UIFactory.CreateCircle("Swatch", cardRect, rug, 150f);
                    UIFactory.Place(swatch.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-75f, -150f), new Vector2(75f, -20f));
                    swatch.rectTransform.localScale = new Vector3(1f, 0.5f, 1f);
                    swatch.raycastTarget = false;
                }
                else
                {
                    IconPreview(cardRect, item.Kind == ShopItemKind.PremiumCurrencyPack ? UIFactory.IconKind.Gem
                        : item.Kind == ShopItemKind.StarterBundle ? UIFactory.IconKind.Sparkle
                        : item.Kind == ShopItemKind.SoftCurrencyPack ? UIFactory.IconKind.Coin
                        : item.Kind == ShopItemKind.NeedRefill ? UIFactory.IconKind.Cookie
                        : item.Kind == ShopItemKind.CooldownBoost ? UIFactory.IconKind.Ball
                        : item.Kind == ShopItemKind.RewardBoost ? UIFactory.IconKind.Sparkle
                        : item.Kind == ShopItemKind.StreakShield ? UIFactory.IconKind.Shield
                        : UIFactory.IconKind.Leaf);
                }
            }
        }

        private static void IconPreview(RectTransform card, UIFactory.IconKind kind)
        {
            var disc = UIFactory.CreateCircle("Disc", card, UIFactory.Hex("FBF6F9"), 120f);
            UIFactory.Place(disc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-60f, -140f), new Vector2(60f, -20f));
            disc.raycastTarget = false;
            var icon = UIFactory.CreateIcon(kind, disc.transform, 64f, UIFactory.Hex("FBF6F9"));
            icon.anchoredPosition = Vector2.zero;
        }

        private RectTransform Card(Transform grid, string title, string subtitle, string price, string action, Color color, bool interactable, Action onClick)
        {
            var card = UIFactory.CreateCard(title, grid, UIFactory.Card, 0.8f);
            var rect = card.rectTransform;
            var name = UIFactory.CreateText("Name", rect, title, 26, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -190f), new Vector2(-12f, -150f));
            var sub = UIFactory.CreateText("Sub", rect, subtitle, 19, UIFactory.Muted, TextAnchor.UpperCenter);
            UIFactory.Place(sub.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 96f), new Vector2(-14f, -192f));
            var priceText = UIFactory.CreateText("Price", rect, price, 20, UIFactory.PinkDark, TextAnchor.MiddleCenter, true);
            UIFactory.Place(priceText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 68f), new Vector2(0f, 96f));
            var button = UIFactory.CreateButton("Action", rect, action, color, onClick, 24, _host);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-90f, 12f), new Vector2(90f, 64f));
            button.interactable = interactable;
            return rect;
        }

        private IEnumerator PopCards(ShopCategory category)
        {
            var grid = _grids[category];
            for (int i = 0; i < grid.childCount; i++)
            {
                var child = grid.GetChild(i);
                _host.StartCoroutine(SimpleTween.PopIn(child, 0.28f));
                yield return new WaitForSeconds(0.04f);
            }
        }
    }
}
