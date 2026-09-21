using System;
using System.Collections.Generic;

namespace Gotchi.Data
{
    [Serializable]
    public struct BranchXp
    {
        public SkillBranch branch;
        public int xp;
    }

    [Serializable]
    public struct ItemCount
    {
        public string id;
        public int count;
    }

    [Serializable]
    public struct QuestState
    {
        public string id;
        public int progress;
        public bool done;
    }

    // Everything the Battle Club remembers (Systems/BattleSystem.cs owns the rules; 13-pvp-design.md explains them).
    [Serializable]
    public class BattleSave
    {
        public string style = "";                                  // "" until the player picks Claw, Fluff or Trick
        public int hpRank, attackRank, defenseRank, speedRank;     // training ranks bought with coins
        public List<string> ownedMoves = new List<string>();
        public List<string> loadout = new List<string>();          // up to four move ids
        public List<ItemCount> items = new List<ItemCount>();      // consumables carried into battle
        public List<string> ownedCharms = new List<string>();
        public string equippedCharm = "";
        public int rating, bestRating;
        public int wins, losses, winStreak, bestWinStreak;
        public List<string> claimedLeagues = new List<string>();   // promotion rewards already paid out
        public string dailyKey = "";                               // local day (yyyy-MM-dd) the daily block belongs to
        public int dailyWins;
        public bool dailyChestClaimed;
        public List<QuestState> quests = new List<QuestState>();
        public List<string> ownedArenas = new List<string>();
        public string arenaId = "";
        public bool starterKitGranted;
        public bool vitalsStarted;                                 // false = hp and mp have never been set: start full
        public float hp, mp;                                       // health and mana as the last fight (and the rest since) left them
        public long vitalsUtcTicks;                                // when hp and mp were last brought up to date (they refill with time)
        public string tradesKey = "";                              // local day the trade board belongs to
        public List<string> tradesDone = new List<string>();       // offers already taken that day
    }

    // The camp (Systems/CampSystem.cs): what feeding and grooming have bought, counted in battles. The four needs
    // that used to live in the save (hunger, hygiene, energy, happiness) are gone; an old save's values are ignored.
    [Serializable]
    public class CampSave
    {
        public int fedBattles;       // battles the ATTACK buff still lasts
        public int groomedBattles;   // battles the DEFENSE buff still lasts
    }

    // The Wild (campaign): which areas are cleared, what was picked up today, and the expedition in progress.
    // Systems/CampaignSystem.cs owns the rules.
    [Serializable]
    public class CampaignSave
    {
        public List<string> clearedAreas = new List<string>();
        public int wildWins;
        public string findsKey = "";                               // local day the finds list belongs to
        public List<string> foundToday = new List<string>();       // "areaId:step" picked up that day
        public bool expeditionActive;
        public string areaId = "";
        public int step;                                           // next step to search, 0-based (health and mana live in BattleSave)
        public int runs;                                           // expeditions started (seeds the wild cats)
    }

    // Serialized with Unity's JsonUtility, which does not support Dictionary — hence the lists.
    [Serializable]
    public class PetSaveData
    {
        public int saveVersion = 1;
        public string petId;
        public string petName;
        public SpeciesType species;
        public List<BranchXp> skillXp = new List<BranchXp>();
        public int evolutionStage;
        public SkillBranch lockedEvolutionBranch;
        public bool evolutionBranchLocked;
        public int softCurrency;
        public int premiumCurrency;
        public List<string> ownedCosmeticIds = new List<string>();
        public List<string> unlockedAutomationIds = new List<string>();
        public int loginStreakDays;
        public long lastLoginUtcTicks;
        public long lastSavedUtcTicks;
        public string displayName = "";
        public string accountEmail = "";
        public string sessionToken = "";
        public string ageBand = "";
        public int preferredPlayHour = 15;
        public List<string> redeemedPromoCodes = new List<string>();
        public int levelXp;
        public long lastCuddleUtcTicks;
        public string equippedCosmeticId = "";
        public string rugId = "rug_pink";
        public List<string> ownedRoomIds = new List<string>();
        public string backgroundId = "bg_cozy";
        public List<string> ownedBackgroundIds = new List<string>();
        public List<string> readNewsIds = new List<string>();
        public long rewardBoostUntilUtcTicks;
        public long cooldownBoostUntilUtcTicks;
        public int streakShields;
        public bool starterBundleOwned;
        public bool referralRewardClaimed;
        public BattleSave battle = new BattleSave();
        public CampaignSave campaign = new CampaignSave();
        public CampSave camp = new CampSave();

        public string ReferralCode => (petId ?? "GOTCHI").Substring(0, 6).ToUpperInvariant();

        public static PetSaveData CreateNew(SpeciesType species, string petName)
        {
            var data = new PetSaveData
            {
                petId = Guid.NewGuid().ToString("N"),
                petName = petName,
                species = species,
                softCurrency = 100,
                premiumCurrency = 0,
                lastSavedUtcTicks = DateTime.UtcNow.Ticks,
                lastLoginUtcTicks = DateTime.UtcNow.Ticks,
                loginStreakDays = 1,
            };
            foreach (SkillBranch branch in Enum.GetValues(typeof(SkillBranch)))
                data.skillXp.Add(new BranchXp { branch = branch, xp = 0 });
            return data;
        }

        public int GetXp(SkillBranch branch)
        {
            for (int i = 0; i < skillXp.Count; i++)
                if (skillXp[i].branch == branch) return skillXp[i].xp;
            return 0;
        }

        public void SetXp(SkillBranch branch, int xp)
        {
            for (int i = 0; i < skillXp.Count; i++)
            {
                if (skillXp[i].branch != branch) continue;
                skillXp[i] = new BranchXp { branch = branch, xp = xp };
                return;
            }
            skillXp.Add(new BranchXp { branch = branch, xp = xp });
        }
    }
}
