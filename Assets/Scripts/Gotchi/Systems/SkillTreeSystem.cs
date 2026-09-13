using System;
using Gotchi.Data;

namespace Gotchi.Systems
{
    // Single-branch-locked evolution (02-game-design.md): once the dominant branch crosses
    // LockInXp the creature commits to that path and only that branch advances its form.
    public class SkillTreeSystem
    {
        public const int XpPerStage = 250;
        public const int MaxStage = 5;
        public const int LockInXp = 500;

        private readonly PetSaveData _data;

        public event Action<SkillBranch, int> OnXpChanged;
        public event Action<int> OnEvolutionStageChanged;
        public event Action<SkillBranch> OnBranchLocked;

        public SkillTreeSystem(PetSaveData data)
        {
            _data = data;
        }

        public int GetXp(SkillBranch branch) => _data.GetXp(branch);

        public bool IsLocked => _data.evolutionBranchLocked;
        public SkillBranch LockedBranch => _data.lockedEvolutionBranch;
        public int EvolutionStage => _data.evolutionStage;

        public SkillBranch EvolutionBranch => IsLocked ? LockedBranch : DominantBranch;

        public SkillBranch DominantBranch
        {
            get
            {
                SkillBranch best = SkillBranch.Sport;
                int bestXp = int.MinValue;
                foreach (SkillBranch branch in Enum.GetValues(typeof(SkillBranch)))
                {
                    int xp = GetXp(branch);
                    if (xp <= bestXp) continue;
                    bestXp = xp;
                    best = branch;
                }
                return best;
            }
        }

        public void AddXp(SkillBranch branch, int amount)
        {
            if (amount <= 0) return;
            int total = GetXp(branch) + amount;
            _data.SetXp(branch, total);
            OnXpChanged?.Invoke(branch, total);
            RecalculateEvolution();
        }

        public int XpIntoCurrentStage(SkillBranch branch) => GetXp(branch) % XpPerStage;

        private void RecalculateEvolution()
        {
            if (!IsLocked)
            {
                SkillBranch dominant = DominantBranch;
                if (GetXp(dominant) >= LockInXp)
                {
                    _data.evolutionBranchLocked = true;
                    _data.lockedEvolutionBranch = dominant;
                    OnBranchLocked?.Invoke(dominant);
                }
            }

            int stage = Math.Min(MaxStage, GetXp(EvolutionBranch) / XpPerStage);
            if (stage == _data.evolutionStage) return;
            _data.evolutionStage = stage;
            OnEvolutionStageChanged?.Invoke(stage);
        }
    }
}
