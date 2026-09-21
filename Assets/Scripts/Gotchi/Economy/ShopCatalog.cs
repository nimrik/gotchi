using Gotchi.Data;

namespace Gotchi.Economy
{
    public enum ShopCategory { Hearts, Bonuses, Style, Backgrounds, Room, Helpers }

    public enum ShopItemKind
    {
        PremiumCurrencyPack, StarterBundle, SoftCurrencyPack, NeedRefill,
        RewardBoost, CooldownBoost, StreakShield, Cosmetic, RoomDecor, Background,
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
            // Real money → hearts (the premium currency: love your pet gives back). Names say the amount.
            new ShopItem { Id = "gems_small",  Category = ShopCategory.Hearts, Kind = ShopItemKind.PremiumCurrencyPack, DisplayName = "50 Hearts",  Description = "A little bundle of love.",  RealMoney = true, Amount = 50,  StoreProductId = "com.gotchi.gems.small" },
            new ShopItem { Id = "gems_medium", Category = ShopCategory.Hearts, Kind = ShopItemKind.PremiumCurrencyPack, DisplayName = "150 Hearts", Description = "Enough for a few bonuses.", RealMoney = true, Amount = 150, StoreProductId = "com.gotchi.gems.medium" },
            new ShopItem { Id = "gems_large",  Category = ShopCategory.Hearts, Kind = ShopItemKind.PremiumCurrencyPack, DisplayName = "400 Hearts", Description = "A whole heart-shaped box.", RealMoney = true, Amount = 400, StoreProductId = "com.gotchi.gems.large" },
            new ShopItem { Id = "starter_bundle", Category = ShopCategory.Hearts, Kind = ShopItemKind.StarterBundle, DisplayName = "Starter Pack", Description = "120 hearts + Cozy Beanie + Snack Dispenser. One time only.", RealMoney = true, Amount = 120, StoreProductId = "com.gotchi.starter" },

            // Bonuses (hearts / coins). Every name says what you get.
            new ShopItem { Id = "coins_pack",   Category = ShopCategory.Bonuses, Kind = ShopItemKind.SoftCurrencyPack, DisplayName = "500 Coins",      Description = "Trade 20 hearts for 500 coins.", CostCurrency = CurrencyType.Premium, Cost = 20, Amount = 500 },
            new ShopItem { Id = "treat_box",    Category = ShopCategory.Bonuses, Kind = ShopItemKind.NeedRefill,       DisplayName = "Full Recovery",  Description = "Health and mana to full right now.", CostCurrency = CurrencyType.Soft, Cost = 40, Amount = 100 },
            new ShopItem { Id = "zoomies",      Category = ShopCategory.Bonuses, Kind = ShopItemKind.CooldownBoost,    DisplayName = "No Cooldowns",   Description = "Camp actions and treats have no wait for 1 hour.", CostCurrency = CurrencyType.Premium, Cost = 15, Hours = 1f },
            new ShopItem { Id = "lucky_hour",   Category = ShopCategory.Bonuses, Kind = ShopItemKind.RewardBoost,      DisplayName = "Double Rewards", Description = "2× XP and coins from battles for 1 hour.", CostCurrency = CurrencyType.Premium, Cost = 20, Hours = 1f },
            new ShopItem { Id = "streak_shield",Category = ShopCategory.Bonuses, Kind = ShopItemKind.StreakShield,     DisplayName = "Streak Shield",  Description = "Keeps your login streak if you miss a day.", CostCurrency = CurrencyType.Premium, Cost = 10 },

            // Style (worn by the pet)
            new ShopItem { Id = "bow_cherry",  Category = ShopCategory.Style, Kind = ShopItemKind.Cosmetic, DisplayName = "Cherry Bow",  Description = "A little bow by the ear.", CostCurrency = CurrencyType.Soft, Cost = 120 },
            new ShopItem { Id = "scarf_star",  Category = ShopCategory.Style, Kind = ShopItemKind.Cosmetic, DisplayName = "Star Scarf",  Description = "Warm and a bit dramatic.", CostCurrency = CurrencyType.Soft, Cost = 250 },
            new ShopItem { Id = "hat_beanie",  Category = ShopCategory.Style, Kind = ShopItemKind.Cosmetic, DisplayName = "Cozy Beanie", Description = "Pom-pom included.", CostCurrency = CurrencyType.Premium, Cost = 30 },
            new ShopItem { Id = "crown_tiny",  Category = ShopCategory.Style, Kind = ShopItemKind.Cosmetic, DisplayName = "Tiny Crown",  Description = "For the ruler of the rug.", CostCurrency = CurrencyType.Premium, Cost = 80 },

            // Backgrounds (whole scene behind the pet; "bg_cozy" is the free default, see RoomScenes)
            new ShopItem { Id = "bg_meadow", Category = ShopCategory.Backgrounds, Kind = ShopItemKind.Background, DisplayName = "Meadow",       Description = "Rolling hills, flowers and a big sun.", CostCurrency = CurrencyType.Soft, Cost = 300 },
            new ShopItem { Id = "bg_beach",  Category = ShopCategory.Backgrounds, Kind = ShopItemKind.Background, DisplayName = "Beach Day",    Description = "Waves, warm sand and a palm tree.", CostCurrency = CurrencyType.Soft, Cost = 350 },
            new ShopItem { Id = "bg_snow",   Category = ShopCategory.Backgrounds, Kind = ShopItemKind.Background, DisplayName = "Snow Day",     Description = "Soft snow, a pine and a snowman.", CostCurrency = CurrencyType.Soft, Cost = 350 },
            new ShopItem { Id = "bg_night",  Category = ShopCategory.Backgrounds, Kind = ShopItemKind.Background, DisplayName = "Starry Night", Description = "Moonlight and twinkling stars.", CostCurrency = CurrencyType.Premium, Cost = 40 },

            // Room (decor on top of any background)
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
