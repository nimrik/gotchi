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
        private readonly DialogBoxView _dialog;
        private readonly Text _title;
        private readonly Text _coins;
        private readonly Text _gems;
        private readonly RectTransform _wallet;
        private readonly PagedScroll _pages;
        private readonly List<ShopCategory> _categories = new List<ShopCategory>();
        private TabBarView _tabBar;
        private readonly Dictionary<ShopCategory, RectTransform> _grids = new Dictionary<ShopCategory, RectTransform>();

        public readonly GameObject Root;

        public ShopPanelView(Transform parent, GameContext ctx, Action<string> toast, Action onClose, MonoBehaviour host, DialogBoxView dialog, bool allowRealMoney = true)
        {
            _ctx = ctx;
            _dialog = dialog;
            _toast = toast;
            _host = host;

            var root = UIFactory.CreateRect("ShopRoot", parent);
            Root = root.gameObject;
            var scrim = UIFactory.CreatePanel("Scrim", root, UIFactory.Scrim);
            UIFactory.Fill(scrim.rectTransform);
            scrim.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());

            var card = UIFactory.CreateCard("Panel", root, UIFactory.PanelBlue);
            UIFactory.Fill((RectTransform)card.transform.parent, 28f, 28f, 100f, 120f);
            _title = UIFactory.CreatePixelText("Header", card.transform, "SHOP", 52, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            UIFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -84f), new Vector2(0f, -24f));
            // Wallet: the same box as the home header, so the numbers read the same everywhere.
            var wallet = UIFactory.CreateWalletBox("Wallet", card.transform, out _coins, out _gems);
            _wallet = wallet.rectTransform;
            UIFactory.Place(_wallet, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-310f, -176f), new Vector2(310f, -92f));

            if (allowRealMoney) _categories.Add(ShopCategory.Hearts);
            _categories.AddRange(new[] { ShopCategory.Bonuses, ShopCategory.Style, ShopCategory.Backgrounds, ShopCategory.Room, ShopCategory.Helpers });

            var tabs = new TabBarView.Tab[_categories.Count];
            for (int i = 0; i < tabs.Length; i++)
            {
                var (icon, label) = TabInfo(_categories[i]);
                tabs[i] = new TabBarView.Tab(label, icon);
            }
            _tabBar = TabBarView.Arrows("Tabs", card.transform, tabs, host);
            UIFactory.Place(_tabBar.Root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -260f), new Vector2(-24f, -188f));
            _tabBar.OnSelected += index => _pages.GoTo(index);

            _pages = PagedScroll.Create("Pages", card.transform, out RectTransform viewport);
            UIFactory.Place(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 110f), new Vector2(0f, -276f));
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
            _pages.OnPageChanged += index => { _tabBar.Select(index, false); HighlightTab(); _host.StartCoroutine(PopCards(_categories[index])); };

            var back = UIFactory.CreateButton("Back", card.transform, "Back", UIFactory.Card, onClose, 30, host);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 96f));
            UIFactory.FitToLabel(back);

            _pages.GoTo(_categories.IndexOf(ShopCategory.Bonuses), false);
            _tabBar.Select(_pages.Current, false);
            Refresh();
        }

        // Jumps straight to a category (the header wallet boxes open the shop on Hearts / Bonuses).
        public void Show(ShopCategory category)
        {
            int index = _categories.IndexOf(category);
            if (index < 0) index = _categories.IndexOf(ShopCategory.Bonuses);
            _pages.GoTo(index, false);
            _tabBar.Select(index, false);
            HighlightTab();
        }

        private static (UIFactory.IconKind icon, string label) TabInfo(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Hearts: return (UIFactory.IconKind.Heart, "Get Hearts");
                case ShopCategory.Bonuses: return (UIFactory.IconKind.Sparkle, "Bonuses");
                case ShopCategory.Style: return (UIFactory.IconKind.Bag, "Style");
                case ShopCategory.Backgrounds: return (UIFactory.IconKind.Moon, "Backgrounds");
                case ShopCategory.Room: return (UIFactory.IconKind.Leaf, "Room");
                default: return (UIFactory.IconKind.Cookie, "Helpers");
            }
        }

        private void HighlightTab() { }

        public void Refresh()
        {
            _coins.text = _ctx.Wallet.Get(CurrencyType.Soft) + " COINS";
            _gems.text = _ctx.Wallet.Get(CurrencyType.Premium) + " HEARTS";
            float walletWidth = UIFactory.FitWalletBox(_coins, _gems, _coins.text, _gems.text);   // box hugs its content, stays centred
            _wallet.offsetMin = new Vector2(-walletWidth * 0.5f, _wallet.offsetMin.y);
            _wallet.offsetMax = new Vector2(walletWidth * 0.5f, _wallet.offsetMax.y);
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
                    var cardRect = Card(grid, def.DisplayName, $"{def.Description} Needs {def.RequiredXp} battle XP.", TileColor(category),
                        def.SoftCost.ToString(), UIFactory.IconKind.Coin, unlocked ? "Active" : "Unlock", UIFactory.Mint, !unlocked && can, () =>
                        {
                            bool ok = _ctx.Automation.TryUnlock(captured, _ctx.Skills, _ctx.Wallet);
                            _toast(ok ? $"{captured.DisplayName} is on!" : "Not yet — check the requirements.");
                            Refresh();
                        });
                    IconPreview(cardRect, def.Action == CampAction.Feed ? UIFactory.IconKind.Cookie : def.Action == CampAction.Groom ? UIFactory.IconKind.Bubbles : def.Action == CampAction.Rest ? UIFactory.IconKind.Moon : UIFactory.IconKind.Sparkle);
                }
                return;
            }

            if (category == ShopCategory.Backgrounds)
            {
                // The free default set always comes first.
                var cozy = RoomScenes.Find(ShopService.DefaultBackgroundId);
                bool inUse = _ctx.Shop.CurrentBackground == cozy.Id;
                var cozyCard = Card(grid, cozy.Name, "The default room — yours from day one.", TileColor(category),
                    "FREE", null, inUse ? "In use" : "Use", UIFactory.Mint, !inUse, () => { _ctx.Shop.SetBackground(cozy.Id); Refresh(); });
                ScenePreview(cozyCard, cozy);
            }

            foreach (var item in ShopCatalog.Items)
            {
                if (item.Category != category) continue;
                ShopItem captured = item;
                bool owned = _ctx.Shop.Owns(item);
                bool equippable = _ctx.Shop.CanEquip(item);
                bool equipped = _ctx.Shop.IsEquipped(item);
                string price = item.RealMoney ? "App Store" : $"{item.Cost} {(item.CostCurrency == CurrencyType.Soft ? "coins" : "hearts")}";
                string action; Color color; bool interactable; Action onClick;
                if (owned && equippable)
                {
                    bool isRoom = item.Kind == ShopItemKind.RoomDecor || item.Kind == ShopItemKind.Background;
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
                    string question = captured.RealMoney ? $"Buy {captured.DisplayName} through the App Store?" : $"Buy {captured.DisplayName} for {price}?";
                    onClick = () => _dialog.Ask(question, new[] { "Yes", "No" }, choice =>
                    {
                        if (choice != 0) return;
                        _ctx.Shop.Buy(captured, result => { _toast(result.Success ? $"Got {captured.DisplayName}!" : result.Message); Refresh(); });
                    });
                }
                string status = "";
                if (item.Kind == ShopItemKind.RewardBoost && _ctx.Boosts.RewardBoostActive) status = $"\\nActive · {(int)_ctx.Boosts.RewardBoostRemaining.TotalMinutes + 1} min left";
                if (item.Kind == ShopItemKind.CooldownBoost && _ctx.Boosts.CooldownBoostActive) status = $"\\nActive · {(int)_ctx.Boosts.CooldownBoostRemaining.TotalMinutes + 1} min left";
                if (item.Kind == ShopItemKind.StreakShield && _ctx.Boosts.StreakShields > 0) status = $"\\n{_ctx.Boosts.StreakShields} ready";
                var cardRect = Card(grid, item.DisplayName, item.Description + status, TileColor(category),
                    item.RealMoney ? "APP STORE" : $"{item.Cost} {(item.CostCurrency == CurrencyType.Soft ? "COINS" : "HEARTS")}", item.RealMoney ? (UIFactory.IconKind?)null : item.CostCurrency == CurrencyType.Soft ? UIFactory.IconKind.Coin : UIFactory.IconKind.Heart,
                    action, color, interactable, onClick);

                if (item.Kind == ShopItemKind.Cosmetic)
                {
                    var anchor = UIFactory.CreateRect("Preview", cardRect);
                    UIFactory.Place(anchor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(0f, -92f));
                    var preview = new PetPortraitView(anchor, _ctx.Data.species, _host, 112f);
                    preview.SetFace(EmotionType.Joy, false);
                    preview.SetAccessory(item.Id);
                }
                else if (item.Kind == ShopItemKind.Background)
                {
                    ScenePreview(cardRect, RoomScenes.Find(item.Id));
                }
                else if (item.Kind == ShopItemKind.RoomDecor && item.Id.StartsWith("rug_"))
                {
                    Color rug = item.Id == "rug_mint" ? UIFactory.Hex("BFE9D0") : item.Id == "rug_sky" ? UIFactory.Hex("BFE0FF") : UIFactory.Hex("FFC4D6");
                    var swatch = UIFactory.CreateCircle("Swatch", cardRect, rug, 150f);
                    UIFactory.Place(swatch.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-75f, -150f), new Vector2(75f, -30f));
                    swatch.rectTransform.localScale = new Vector3(1f, 0.5f, 1f);
                    swatch.raycastTarget = false;
                }
                else
                {
                    IconPreview(cardRect, item.Kind == ShopItemKind.PremiumCurrencyPack ? UIFactory.IconKind.Heart
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

        private static void ScenePreview(RectTransform card, RoomScenes.Scene scene)
        {
            var preview = RoomScenes.Preview(card, scene, 200f, 116f);
            UIFactory.Place(preview, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-100f, -150f), new Vector2(100f, -34f));
        }

        private static void IconPreview(RectTransform card, UIFactory.IconKind kind)
        {
            var disc = UIFactory.CreateCircle("Disc", card, Color.white, 110f);
            UIFactory.Place(disc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-55f, -145f), new Vector2(55f, -35f));
            disc.raycastTarget = false;
            var icon = UIFactory.CreateIcon(kind, disc.transform, 60f, Color.white);
            icon.anchoredPosition = Vector2.zero;
        }

        private static Color TileColor(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Hearts: return UIFactory.Hex("FFE8F0");
                case ShopCategory.Bonuses: return UIFactory.Hex("FFF1D2");
                case ShopCategory.Style: return UIFactory.Hex("FFE3EC");
                case ShopCategory.Backgrounds: return UIFactory.Hex("E3F2FF");
                case ShopCategory.Room: return UIFactory.Hex("DFF5E6");
                default: return UIFactory.Hex("E0EFFF");
            }
        }

        // Item card: pastel art tile on top, name, description, then price chip and action button on one row.
        private RectTransform Card(Transform grid, string title, string subtitle, Color tile, string priceText, UIFactory.IconKind? priceIcon, string action, Color color, bool interactable, Action onClick)
        {
            var card = UIFactory.CreateCard(title, grid, UIFactory.Card, 0.8f);
            var rect = card.rectTransform;

            var art = UIFactory.CreatePanel("Tile", rect, tile);
            art.sprite = UIFactory.ThinFrameSprite;
            art.type = Image.Type.Sliced;
            art.raycastTarget = false;
            UIFactory.Place(art.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -172f), new Vector2(-12f, -12f));

            var name = UIFactory.CreatePixelText("Name", rect, title, 24, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -216f), new Vector2(-12f, -178f));
            var sub = UIFactory.CreateText("Sub", rect, subtitle, 18, UIFactory.Muted, TextAnchor.UpperCenter);
            UIFactory.Place(sub.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 82f), new Vector2(-16f, -220f));

            // Action button fitted to its word on the right; the price chip takes the rest of the row.
            var button = UIFactory.CreateButton("Action", rect, action, color, onClick, 22, _host);
            UIFactory.Place((RectTransform)button.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-152f, 14f), new Vector2(-12f, 66f));
            UIFactory.FitToLabelRight(button, 12f, 120f);
            button.interactable = interactable;
            float buttonWidth = ((RectTransform)button.transform).rect.width;

            var chip = UIFactory.CreatePanel("Price", rect, Color.white);
            chip.sprite = UIFactory.ThinFrameSprite;
            chip.type = Image.Type.Sliced;
            chip.raycastTarget = false;
            UIFactory.Place(chip.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 14f), new Vector2(-12f - buttonWidth - 10f, 66f));
            float textLeft = 12f;
            if (priceIcon.HasValue)
            {
                var icon = UIFactory.CreateIcon(priceIcon.Value, chip.transform, 30f, Color.white);
                UIFactory.Place(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -15f), new Vector2(40f, 15f));
                textLeft = 46f;
            }
            var priceLabel = UIFactory.CreatePixelText("Text", chip.transform, priceText, priceIcon.HasValue ? 20 : 18, UIFactory.MenuInk, priceIcon.HasValue ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            priceLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Fill(priceLabel.rectTransform, textLeft, 8f, 0f, 0f);

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
