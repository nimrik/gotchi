using System;
using Gotchi.Data;
using Gotchi.Systems;

namespace Gotchi.Economy
{
    public class ShopService
    {
        private readonly PetSaveData _data;
        private readonly CurrencyWallet _wallet;
        private readonly NeedsSystem _needs;
        private readonly BoostSystem _boosts;
        private readonly IPurchaseService _purchases;

        public event Action<ShopItem> OnItemGranted;
        public event Action OnEquippedChanged;

        public ShopService(PetSaveData data, CurrencyWallet wallet, NeedsSystem needs, BoostSystem boosts, IPurchaseService purchases)
        {
            _data = data;
            _wallet = wallet;
            _needs = needs;
            _boosts = boosts;
            _purchases = purchases;
        }

        public bool Owns(ShopItem item)
        {
            switch (item.Kind)
            {
                case ShopItemKind.Cosmetic: return _data.ownedCosmeticIds.Contains(item.Id);
                case ShopItemKind.RoomDecor: return _data.ownedRoomIds.Contains(item.Id);
                case ShopItemKind.Background: return OwnsBackground(item.Id);
                case ShopItemKind.StarterBundle: return _data.starterBundleOwned;
                default: return false;
            }
        }

        public bool IsEquipped(ShopItem item)
        {
            if (item.Kind == ShopItemKind.Cosmetic) return _data.equippedCosmeticId == item.Id;
            if (item.Kind == ShopItemKind.RoomDecor) return item.Id.StartsWith("rug_") ? _data.rugId == item.Id : Owns(item);
            if (item.Kind == ShopItemKind.Background) return CurrentBackground == item.Id;
            return false;
        }

        public bool CanEquip(ShopItem item) => Owns(item) && (item.Kind == ShopItemKind.Cosmetic || item.Kind == ShopItemKind.Background || (item.Kind == ShopItemKind.RoomDecor && item.Id.StartsWith("rug_")));

        public void ToggleEquip(ShopItem item)
        {
            if (!CanEquip(item)) return;
            if (item.Kind == ShopItemKind.Cosmetic) _data.equippedCosmeticId = _data.equippedCosmeticId == item.Id ? "" : item.Id;
            else if (item.Kind == ShopItemKind.Background) { SetBackground(item.Id); return; }
            else _data.rugId = item.Id;
            OnEquippedChanged?.Invoke();
        }

        // Backgrounds: the default set is free for everyone; bought sets are remembered in the save.
        public const string DefaultBackgroundId = "bg_cozy";
        public string CurrentBackground => string.IsNullOrEmpty(_data.backgroundId) ? DefaultBackgroundId : _data.backgroundId;
        public bool OwnsBackground(string id) => id == DefaultBackgroundId || _data.ownedBackgroundIds.Contains(id);

        public void SetBackground(string id)
        {
            if (!OwnsBackground(id) || CurrentBackground == id) return;
            _data.backgroundId = id;
            OnEquippedChanged?.Invoke();
        }

        public bool CanAfford(ShopItem item) => item.RealMoney ? _purchases.IsAvailable : _wallet.Get(item.CostCurrency) >= item.Cost;

        public void Buy(ShopItem item, Action<PurchaseResult> onComplete)
        {
            if (Owns(item))
            {
                onComplete?.Invoke(PurchaseResult.Fail("Already owned."));
                return;
            }

            if (item.RealMoney)
            {
                _purchases.Purchase(item, result =>
                {
                    if (result.Success) Grant(item);
                    onComplete?.Invoke(result);
                });
                return;
            }

            if (!_wallet.TrySpend(item.CostCurrency, item.Cost))
            {
                onComplete?.Invoke(PurchaseResult.Fail("Not enough " + (item.CostCurrency == CurrencyType.Soft ? "coins" : "hearts") + "."));
                return;
            }

            Grant(item);
            onComplete?.Invoke(PurchaseResult.Ok());
        }

        private void Grant(ShopItem item)
        {
            switch (item.Kind)
            {
                case ShopItemKind.PremiumCurrencyPack:
                    _wallet.Add(CurrencyType.Premium, item.Amount);
                    break;
                case ShopItemKind.StarterBundle:
                    _data.starterBundleOwned = true;
                    _wallet.Add(CurrencyType.Premium, item.Amount);
                    if (!_data.ownedCosmeticIds.Contains("hat_beanie")) _data.ownedCosmeticIds.Add("hat_beanie");
                    if (!_data.unlockedAutomationIds.Contains("auto_feeder")) _data.unlockedAutomationIds.Add("auto_feeder");
                    break;
                case ShopItemKind.SoftCurrencyPack:
                    _wallet.Add(CurrencyType.Soft, item.Amount);
                    break;
                case ShopItemKind.NeedRefill:
                    foreach (NeedType need in Enum.GetValues(typeof(NeedType)))
                        _needs.Set(need, item.Amount);
                    break;
                case ShopItemKind.RewardBoost:
                    _boosts.ActivateRewardBoost(item.Hours);
                    break;
                case ShopItemKind.CooldownBoost:
                    _boosts.ActivateCooldownBoost(item.Hours);
                    break;
                case ShopItemKind.StreakShield:
                    _boosts.AddStreakShield();
                    break;
                case ShopItemKind.Cosmetic:
                    if (!_data.ownedCosmeticIds.Contains(item.Id)) _data.ownedCosmeticIds.Add(item.Id);
                    _data.equippedCosmeticId = item.Id;
                    OnEquippedChanged?.Invoke();
                    break;
                case ShopItemKind.RoomDecor:
                    if (!_data.ownedRoomIds.Contains(item.Id)) _data.ownedRoomIds.Add(item.Id);
                    if (item.Id.StartsWith("rug_")) _data.rugId = item.Id;
                    OnEquippedChanged?.Invoke();
                    break;
                case ShopItemKind.Background:
                    if (!_data.ownedBackgroundIds.Contains(item.Id)) _data.ownedBackgroundIds.Add(item.Id);
                    _data.backgroundId = item.Id;
                    OnEquippedChanged?.Invoke();
                    break;
            }
            OnItemGranted?.Invoke(item);
        }
    }
}
