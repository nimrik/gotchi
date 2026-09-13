using System;
using System.Collections.Generic;

namespace Gotchi.Data
{
    [Serializable]
    public struct NeedValue
    {
        public NeedType type;
        public float value;
    }

    [Serializable]
    public struct BranchXp
    {
        public SkillBranch branch;
        public int xp;
    }

    // Serialized with Unity's JsonUtility, which does not support Dictionary — hence the lists.
    [Serializable]
    public class PetSaveData
    {
        public int saveVersion = 1;
        public string petId;
        public string petName;
        public SpeciesType species;
        public List<NeedValue> needs = new List<NeedValue>();
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
        public long rewardBoostUntilUtcTicks;
        public long cooldownBoostUntilUtcTicks;
        public int streakShields;
        public bool starterBundleOwned;
        public bool referralRewardClaimed;

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
            foreach (NeedType need in Enum.GetValues(typeof(NeedType)))
                data.needs.Add(new NeedValue { type = need, value = 80f });
            foreach (SkillBranch branch in Enum.GetValues(typeof(SkillBranch)))
                data.skillXp.Add(new BranchXp { branch = branch, xp = 0 });
            return data;
        }

        public float GetNeed(NeedType type)
        {
            for (int i = 0; i < needs.Count; i++)
                if (needs[i].type == type) return needs[i].value;
            return 0f;
        }

        public void SetNeed(NeedType type, float value)
        {
            for (int i = 0; i < needs.Count; i++)
            {
                if (needs[i].type != type) continue;
                needs[i] = new NeedValue { type = type, value = value };
                return;
            }
            needs.Add(new NeedValue { type = type, value = value });
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
