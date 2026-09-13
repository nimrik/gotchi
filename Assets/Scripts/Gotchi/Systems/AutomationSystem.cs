using System;
using System.Collections.Generic;
using Gotchi.Data;
using Gotchi.Economy;

namespace Gotchi.Systems
{
    public class AutomationDefinition
    {
        public string Id;
        public string DisplayName;
        public NeedType Need;
        public float DecayMultiplier;
        public int SoftCost;
        public SkillBranch RequiredBranch;
        public int RequiredXp;
    }

    // Phase 2: unlockable helpers that slow need decay. They reduce friction without
    // removing the care loop — the pet still needs manual attention, just less often.
    public class AutomationSystem
    {
        public static readonly AutomationDefinition[] Catalog =
        {
            new AutomationDefinition { Id = "auto_feeder",  DisplayName = "Snack Dispenser", Need = NeedType.Hunger,    DecayMultiplier = 0.6f, SoftCost = 150, RequiredBranch = SkillBranch.Science, RequiredXp = 50 },
            new AutomationDefinition { Id = "auto_bath",    DisplayName = "Bubble Bath",     Need = NeedType.Hygiene,   DecayMultiplier = 0.6f, SoftCost = 150, RequiredBranch = SkillBranch.Nature, RequiredXp = 50 },
            new AutomationDefinition { Id = "auto_bed",     DisplayName = "Cozy Nest",       Need = NeedType.Energy,    DecayMultiplier = 0.6f, SoftCost = 150, RequiredBranch = SkillBranch.Hunter,  RequiredXp = 50 },
            new AutomationDefinition { Id = "auto_toybox",  DisplayName = "Toy Box",         Need = NeedType.Happiness, DecayMultiplier = 0.6f, SoftCost = 150, RequiredBranch = SkillBranch.Sport,   RequiredXp = 50 },
        };

        private const float MinMultiplier = 0.25f;

        private readonly PetSaveData _data;

        public event Action<AutomationDefinition> OnUnlocked;

        public AutomationSystem(PetSaveData data)
        {
            _data = data;
        }

        public bool IsUnlocked(string id) => _data.unlockedAutomationIds.Contains(id);

        public bool CanUnlock(AutomationDefinition def, SkillTreeSystem skills, CurrencyWallet wallet)
        {
            if (IsUnlocked(def.Id)) return false;
            if (skills.GetXp(def.RequiredBranch) < def.RequiredXp) return false;
            return wallet.Get(CurrencyType.Soft) >= def.SoftCost;
        }

        public bool TryUnlock(AutomationDefinition def, SkillTreeSystem skills, CurrencyWallet wallet)
        {
            if (!CanUnlock(def, skills, wallet)) return false;
            if (!wallet.TrySpend(CurrencyType.Soft, def.SoftCost)) return false;
            _data.unlockedAutomationIds.Add(def.Id);
            OnUnlocked?.Invoke(def);
            return true;
        }

        public float GetDecayMultiplier(NeedType need)
        {
            float multiplier = 1f;
            foreach (var def in Catalog)
                if (def.Need == need && IsUnlocked(def.Id))
                    multiplier *= def.DecayMultiplier;
            return Math.Max(MinMultiplier, multiplier);
        }

        public IEnumerable<AutomationDefinition> Locked()
        {
            foreach (var def in Catalog)
                if (!IsUnlocked(def.Id)) yield return def;
        }
    }
}
