using Gotchi.Data;

namespace Gotchi.Economy
{
    public enum ShopCategory { Gems, Boosts, Style, Room, Helpers }

    public enum ShopItemKind
    {
        PremiumCurrencyPack, StarterBundle, SoftCurrencyPack, NeedRefill,
        RewardBoost, CooldownBoost, StreakShield, Cosmetic, RoomDecor,
    }

    public class ShopItem
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public ShopCategory Category;
        public ShopItemKind Kind;
        // Real-money items are bought through IPurchaseService; everything else is paid from the wallet.
        public bool RealMoney;
        public CurrencyType CostCurrency;
        public int Cost;
        public int Amount;
        public float Hours;
        public string StoreProductId;
    }

    // Every item is a fixed, visible purchase — no randomized boxes or gacha. See 05-monetization-compliance.md.
    public static class ShopCatalog
    {
        public static readonly ShopItem[] Items =
        {
            // Real money → gems
            new ShopItem { Id = "gems_small",  Category = ShopCategory.Gems, Kind = ShopItemKind.PremiumCurrencyPack, DisplayName = "Gem Pouch",  Description = "50 gems.",  RealMoney = true, Amount = 50,  StoreProductId = "com.gotchi.gems.small" },
            new ShopItem { Id = "gems_medium", Category = ShopCategory.Gems, Kind = ShopItemKind.PremiumCurrencyPack, DisplayName = "Gem Jar",    Description = "150 gems — best for a few boosts.", RealMoney = true, Amount = 150, StoreProductId = "com.gotchi.gems.medium" },
            new ShopItem { Id = "gems_large",  Category = ShopCategory.Gems, Kind = ShopItemKind.PremiumCurrencyPack, DisplayName = "Gem Chest",  Description = "400 gems — the outfit collector's pick.", RealMoney = true, Amount = 400, StoreProductId = "com.gotchi.gems.large" },
            new ShopItem { Id = "starter_bundle", Category = ShopCategory.Gems, Kind = ShopItemKind.StarterBundle, DisplayName = "Starter Bundle", Description = "120 gems, the Cozy Beanie and a Snack Dispenser. One time only.", RealMoney = true, Amount = 120, StoreProductId = "com.gotchi.starter" },

            // Boosts & care (gems / coins)
            new ShopItem { Id = "coins_pack",   Category = ShopCategory.Boosts, Kind = ShopItemKind.SoftCurrencyPack, DisplayName = "Coin Bag",     Description = "500 coins for helpers and decor.", CostCurrency = CurrencyType.Premium, Cost = 20, Amount = 500 },
            new ShopItem { Id = "treat_box",    Category = ShopCategory.Boosts, Kind = ShopItemKind.NeedRefill,       DisplayName = "Treat Box",    Description = "Fills every need right now.", CostCurrency = CurrencyType.Soft, Cost = 40, Amount = 100 },
            new ShopItem { Id = "zoomies",      Category = ShopCategory.Boosts, Kind = ShopItemKind.CooldownBoost,    DisplayName = "Zoomies",      Description = "No care cooldowns for 1 hour.", CostCurrency = CurrencyType.Premium, Cost = 15, Hours = 1f },
            new ShopItem { Id = "lucky_hour",   Category = ShopCategory.Boosts, Kind = ShopItemKind.RewardBoost,      DisplayName = "Lucky Hour",   Description = "Double mini-game XP and coins for 1 hour.", CostCurrency = CurrencyType.Premium, Cost = 20, Hours = 1f },
            new ShopItem { Id = "streak_shield",Category = ShopCategory.Boosts, Kind = ShopItemKind.StreakShield,     DisplayName = "Streak Shield", Description = "Keeps your login streak if you miss a day.", CostCurrency = CurrencyType.Premium, Cost = 10 },

            // Style (worn by the pet)
            new ShopItem { Id = "bow_cherry",  Category = ShopCategory.Style, Kind = ShopItemKind.Cosmetic, DisplayName = "Cherry Bow",  Description = "A little bow by the ear.", CostCurrency = CurrencyType.Soft, Cost = 120 },
            new ShopItem { Id = "scarf_star",  Category = ShopCategory.Style, Kind = ShopItemKind.Cosmetic, DisplayName = "Star Scarf",  Description = "Warm and a bit dramatic.", CostCurrency = CurrencyType.Soft, Cost = 250 },
            new ShopItem { Id = "hat_beanie",  Category = ShopCategory.Style, Kind = ShopItemKind.Cosmetic, DisplayName = "Cozy Beanie", Description = "Pom-pom included.", CostCurrency = CurrencyType.Premium, Cost = 30 },
            new ShopItem { Id = "crown_tiny",  Category = ShopCategory.Style, Kind = ShopItemKind.Cosmetic, DisplayName = "Tiny Crown",  Description = "For the ruler of the rug.", CostCurrency = CurrencyType.Premium, Cost = 80 },

            // Room
            new ShopItem { Id = "rug_mint",     Category = ShopCategory.Room, Kind = ShopItemKind.RoomDecor, DisplayName = "Mint Rug",     Description = "A fresh green rug.", CostCurrency = CurrencyType.Soft, Cost = 150 },
            new ShopItem { Id = "rug_sky",      Category = ShopCategory.Room, Kind = ShopItemKind.RoomDecor, DisplayName = "Sky Rug",      Description = "A calm blue rug.", CostCurrency = CurrencyType.Soft, Cost = 150 },
            new ShopItem { Id = "fairy_lights", Category = ShopCategory.Room, Kind = ShopItemKind.RoomDecor, DisplayName = "Fairy Lights", Description = "Twinkles along the ceiling.", CostCurrency = CurrencyType.Soft, Cost = 200 },
        };

        public static ShopItem Find(string id)
        {
            foreach (var item in Items) if (item.Id == id) return item;
            return null;
        }
    }
}
