using System.Collections.Generic;
using System.Text;
using Gotchi.Data;

namespace Gotchi.Systems
{
    // A branch can only be trained while the needs it draws on are reasonably met.
    public static class SkillGate
    {
        public const float Threshold = 30f;

        private static readonly Dictionary<SkillBranch, NeedType[]> Requirements = new Dictionary<SkillBranch, NeedType[]>
        {
            { SkillBranch.Sport, new[] { NeedType.Hunger, NeedType.Energy } },
            { SkillBranch.Social, new[] { NeedType.Happiness, NeedType.Hygiene } },
            { SkillBranch.Warrior, new[] { NeedType.Energy, NeedType.Happiness } },
            { SkillBranch.Hunter, new[] { NeedType.Hunger, NeedType.Energy } },
            { SkillBranch.Science, new[] { NeedType.Energy } },
            { SkillBranch.Nature, new[] { NeedType.Happiness } },
            { SkillBranch.ExplorerAdventure, new[] { NeedType.Energy, NeedType.Hunger } },
        };

        public static NeedType[] RequiredNeeds(SkillBranch branch) => Requirements[branch];

        public static bool IsAvailable(SkillBranch branch, NeedsSystem needs, out string reason)
        {
            var missing = new List<NeedType>();
            foreach (var need in Requirements[branch])
                if (needs.Get(need) < Threshold) missing.Add(need);
            if (missing.Count == 0) { reason = null; return true; }

            var sb = new StringBuilder("Needs ");
            for (int i = 0; i < missing.Count; i++)
            {
                if (i > 0) sb.Append(i == missing.Count - 1 ? " and " : ", ");
                sb.Append(missing[i]);
            }
            sb.Append(" above ").Append((int)Threshold).Append(" first.");
            reason = sb.ToString();
            return false;
        }
    }
}
