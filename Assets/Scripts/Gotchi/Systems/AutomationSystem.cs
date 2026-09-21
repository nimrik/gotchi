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
        public string Description;       // what it does to its camp action
        public CampAction Action;        // the camp action it improves (CampSystem reads this through Def(...).HelperId)
        public int SoftCost;
        public SkillBranch RequiredBranch;
        public int RequiredXp;
    }

    // Helpers: bought once, they make one camp action better for good (a buff that lasts five battles instead of
    // three, a rest that gives back 60% instead of 40%). They used to slow the decay of a need; the needs are gone
    // (2026-09-21), the ids stayed so old saves and the Starter Pack keep what they own. Unlocked with coins plus
    // battle XP, one every 100 XP.
    public class AutomationSystem
    {
        public static readonly AutomationDefinition[] Catalog =
        {
            new AutomationDefinition { Id = "auto_feeder", DisplayName = "Snack Dispenser", Action = CampAction.Feed,  Description = "FEED lasts 5 battles instead of 3.",  SoftCost = 150, RequiredBranch = SkillBranch.PvP, RequiredXp = 100 },
            new AutomationDefinition { Id = "auto_bath",   DisplayName = "Grooming Kit",    Action = CampAction.Groom, Description = "GROOM lasts 5 battles instead of 3.", SoftCost = 150, RequiredBranch = SkillBranch.PvP, RequiredXp = 200 },
            new AutomationDefinition { Id = "auto_bed",    DisplayName = "Cozy Nest",       Action = CampAction.Rest,  Description = "REST gives back 60% health instead of 40%.", SoftCost = 150, RequiredBranch = SkillBranch.PvP, RequiredXp = 300 },
            new AutomationDefinition { Id = "auto_toybox", DisplayName = "Quiet Corner",    Action = CampAction.Focus, Description = "FOCUS gives back 60% mana instead of 40%.",  SoftCost = 150, RequiredBranch = SkillBranch.PvP, RequiredXp = 400 },
        };

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

        public IEnumerable<AutomationDefinition> Locked()
        {
            foreach (var def in Catalog)
                if (!IsUnlocked(def.Id)) yield return def;
        }
    }
}
