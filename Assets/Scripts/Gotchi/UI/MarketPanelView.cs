using System;
using System.Collections;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Economy;
using Gotchi.Systems;
using UnityEngine;

namespace Gotchi.UI
{
    // The Market: where battle goods change hands (rules in Systems/BattleSystem.cs). Three pages:
    //   BUY    items, charms and arenas, at list price
    //   SELL   items and bought charms, at half price (promotion rewards are keepsakes)
    //   TRADE  the daily board: three swaps with other keepers, each good once a day, value for value
    // The trade board is a mock of player-to-player trading; BattleSystem.TodayTrades / TryTrade are the seam.
    // Everything here costs coins except one arena, because hearts must never buy anything that wins fights.
    public class MarketPanelView
    {
        public const int BuyPage = 0, SellPage = 1, TradePage = 2;

        private readonly GameContext _ctx;
        private readonly BattleSystem _battle;
        private readonly Action<string> _toast;
        private readonly MonoBehaviour _host;
        private readonly DialogBoxView _dialog;
        private readonly PagedPanel _panel;
        private bool _dirty;

        public GameObject Root => _panel.Root;

        public MarketPanelView(Transform parent, GameContext ctx, Action<string> toast, Action onClose, MonoBehaviour host, DialogBoxView dialog)
        {
            _ctx = ctx;
            _battle = ctx.Battle;
            _toast = toast;
            _host = host;
            _dialog = dialog;
            _panel = new PagedPanel(parent, "MarketRoot", "Market", new[] { "Buy", "Sell", "Trade" },
                new[] { UIFactory.IconKind.Shop, UIFactory.IconKind.Coin, UIFactory.IconKind.Chat }, ctx, onClose, host);
            _battle.OnChanged += MarkDirty;
            _ctx.Wallet.OnBalanceChanged += (_, __) => MarkDirty();
            Refresh();
        }

        private void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            if (Root.activeInHierarchy) _host.StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            if (_dirty) Refresh();
        }

        public void ShowPage(int index) => _panel.ShowPage(index);

        public void Refresh()
        {
            _dirty = false;
            _panel.Rebuild(lists =>
            {
                BuildBuy(lists[BuyPage]);
                BuildSell(lists[SellPage]);
                BuildTrade(lists[TradePage]);
            });
        }

        private int Coins => _ctx.Wallet.Get(CurrencyType.Soft);

        // ---------------------------------------------------------------- BUY

        private void BuildBuy(RectTransform list)
        {
            PanelRows.Note(list, $"Items · the bag holds {BattleSystem.MaxCarry} of each", 44f, true);
            foreach (var item in BattleSystem.Items)
            {
                BattleItemDef captured = item;
                bool can = _battle.CanBuyItem(item, out _);
                var box = PanelRows.Box(list, item.Id, 112f, Color.white);
                PanelRows.Pixel(box, $"{item.Name.ToUpperInvariant()}  x{_battle.Count(item.Id)}", 26, UIFactory.MenuInk, 24f, -16f, PanelRows.TextRight, 36f);
                PanelRows.Body(box, item.Description, 21, UIFactory.Muted, 24f, -58f, PanelRows.TextRight, 50f);
                PanelRows.ActionButton(box, $"Buy · {item.Cost}", UIFactory.Butter, can, () => { if (_battle.TryBuyItem(captured)) _toast($"{captured.Name} in the bag."); }, _host);
            }

            PanelRows.Note(list, "Charms · the cat holds one", 44f, true);
            foreach (var charm in BattleSystem.Charms)
            {
                BattleCharmDef captured = charm;
                bool owned = _battle.OwnsCharm(charm.Id);
                var box = PanelRows.Box(list, charm.Id, 112f, owned || charm.Cost <= 0 ? PanelRows.Locked : Color.white);
                PanelRows.Pixel(box, charm.Name.ToUpperInvariant(), 26, UIFactory.MenuInk, 24f, -16f, PanelRows.TextRight, 36f);
                PanelRows.Body(box, charm.Description, 21, UIFactory.Muted, 24f, -58f, PanelRows.TextRight, 50f);
                if (owned) PanelRows.ActionButton(box, "Owned", PanelRows.Off, false, () => { }, _host);
                else if (charm.Cost <= 0) PanelRows.ActionButton(box, BattleSystem.Leagues[Mathf.Max(0, charm.RewardLeague)].Name, PanelRows.Off, false, () => { }, _host);
                else
                    PanelRows.ActionButton(box, $"Buy · {charm.Cost}", UIFactory.Butter, Coins >= charm.Cost, () =>
                        _dialog.Ask($"Buy the {captured.Name} for {captured.Cost} coins?", new[] { "Yes", "No" }, choice =>
                        {
                            if (choice == 0 && _battle.TryBuyCharm(captured)) _toast($"Got the {captured.Name}!");
                        }), _host);
            }

            PanelRows.Note(list, "Arenas · looks only", 44f, true);
            foreach (var arena in BattleSystem.Arenas)
            {
                ArenaDef captured = arena;
                bool owned = _battle.OwnsArena(arena.Id);
                var box = PanelRows.Box(list, arena.Id, 112f, owned || arena.RewardOnly ? PanelRows.Locked : Color.white);
                BattleClubPanelView.ArenaSwatch(box, arena);
                PanelRows.Pixel(box, arena.Name.ToUpperInvariant(), 26, UIFactory.MenuInk, 116f, -16f, PanelRows.TextRight, 36f);
                PanelRows.Body(box, arena.Description, 21, UIFactory.Muted, 116f, -58f, PanelRows.TextRight, 50f);
                if (owned) PanelRows.ActionButton(box, "Owned", PanelRows.Off, false, () => { }, _host);
                else if (arena.RewardOnly) PanelRows.ActionButton(box, "Crystal", PanelRows.Off, false, () => { }, _host);
                else
                {
                    string unit = arena.Currency == CurrencyType.Soft ? "coins" : "hearts";
                    PanelRows.ActionButton(box, $"Buy · {arena.Cost}", arena.Currency == CurrencyType.Soft ? UIFactory.Butter : UIFactory.Pink, _ctx.Wallet.Get(arena.Currency) >= arena.Cost, () =>
                        _dialog.Ask($"Buy {captured.Name} for {captured.Cost} {unit}?", new[] { "Yes", "No" }, choice =>
                        {
                            if (choice == 0 && _battle.TryBuyArena(captured)) _toast($"{captured.Name} is yours!");
                        }), _host);
                }
            }
        }

        // ---------------------------------------------------------------- SELL

        private void BuildSell(RectTransform list)
        {
            PanelRows.Note(list, "The Market pays half the list price. Charms won by promotion are keepsakes and stay with you.", 76f);
            bool any = false;
            foreach (var item in BattleSystem.Items)
            {
                int count = _battle.Count(item.Id);
                if (count <= 0) continue;
                any = true;
                BattleItemDef captured = item;
                var box = PanelRows.Box(list, item.Id, 100f, Color.white);
                PanelRows.Pixel(box, $"{item.Name.ToUpperInvariant()}  x{count}", 26, UIFactory.MenuInk, 24f, -14f, PanelRows.TextRight, 36f);
                PanelRows.Body(box, $"Bought for {item.Cost}.", 21, UIFactory.Muted, 24f, -54f, PanelRows.TextRight, 30f);
                PanelRows.ActionButton(box, $"Sell · {BattleSystem.SellPrice(item.Cost)}", UIFactory.Mint, true, () => { if (_battle.TrySellItem(captured)) _toast($"Sold a {captured.Name}."); }, _host);
            }
            foreach (var charm in BattleSystem.Charms)
            {
                if (!_battle.OwnsCharm(charm.Id) || charm.Cost <= 0) continue;
                any = true;
                BattleCharmDef captured = charm;
                bool held = _battle.EquippedCharm == charm.Id;
                var box = PanelRows.Box(list, charm.Id, 100f, held ? PanelRows.Held : Color.white);
                PanelRows.Pixel(box, charm.Name.ToUpperInvariant(), 26, UIFactory.MenuInk, 24f, -14f, PanelRows.TextRight, 36f);
                PanelRows.Body(box, held ? "Being held. Take it off in the Bag to sell it." : $"Bought for {charm.Cost}.", 21, UIFactory.Muted, 24f, -54f, PanelRows.TextRight, 30f);
                PanelRows.ActionButton(box, held ? "Held" : $"Sell · {BattleSystem.SellPrice(charm.Cost)}", held ? PanelRows.Off : UIFactory.Mint, !held, () =>
                    _dialog.Ask($"Sell the {captured.Name} for {BattleSystem.SellPrice(captured.Cost)} coins?", new[] { "Yes", "No" }, choice =>
                    {
                        if (choice == 0 && _battle.TrySellCharm(captured)) _toast($"Sold the {captured.Name}.");
                    }), _host);
            }
            if (!any) PanelRows.Note(list, "Nothing to sell yet. Items are found in the Wild and bought on the Buy page.", 80f);
        }

        // ---------------------------------------------------------------- TRADE

        private void BuildTrade(RectTransform list)
        {
            PanelRows.Note(list, "Other keepers post three swaps a day, value for value, which beats selling at half price. Each swap can be taken once.", 96f);
            foreach (var offer in _battle.TodayTrades())
            {
                TradeOffer captured = offer;
                var give = BattleSystem.FindItem(offer.GiveItemId);
                var get = BattleSystem.FindItem(offer.GetItemId);
                bool done = _battle.TradeDone(offer);
                bool can = _battle.CanTrade(offer, out string reason);
                var box = PanelRows.Box(list, offer.Id, 146f, done ? PanelRows.Locked : Color.white);
                PanelRows.Pixel(box, offer.Keeper.ToUpperInvariant(), 26, UIFactory.MenuInk, 24f, -16f, PanelRows.TextRight, 36f);
                PanelRows.Body(box, $"Gives {offer.GetCount}x {get.Name} for your {offer.GiveCount}x {give.Name}.", 22, UIFactory.Ink, 24f, -56f, PanelRows.TextRight, 30f);
                PanelRows.Body(box, done ? "Traded today." : can ? $"You have {_battle.Count(give.Id)}x {give.Name}." : reason, 21, can ? UIFactory.Muted : UIFactory.PinkDark, 24f, -90f, PanelRows.TextRight, 30f);
                PanelRows.ActionButton(box, done ? "Done" : "Trade", done ? PanelRows.Off : UIFactory.Mint, can, () =>
                    _dialog.Ask($"Give {captured.GiveCount}x {give.Name} to {captured.Keeper} for {captured.GetCount}x {get.Name}?", new[] { "Yes", "No" }, choice =>
                    {
                        if (choice == 0 && _battle.TryTrade(captured)) _toast($"Traded with {captured.Keeper}.");
                    }), _host);
            }
            PanelRows.Note(list, "Trading with friends, and offers of your own, arrive with live play.", 60f);
        }
    }
}
