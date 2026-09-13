using System;
using System.Collections.Generic;
using Gotchi.Data;

namespace Gotchi.Systems
{
    public enum LeaderboardKind { Level, Skill }

    public class LeaderboardEntry
    {
        public string PlayerId;
        public string DisplayName;
        public string PetName;
        public SpeciesType Species;
        public int Value;
        public int Rank;
        public bool IsLocal;
    }

    public class PlayerProfile
    {
        public string PlayerId;
        public string DisplayName;
        public string PetName;
        public SpeciesType Species;
        public int Level;
        public int EvolutionStage;
        public SkillBranch EvolutionBranch;
        public int[] BranchXp = new int[7];
        public int StreakDays;
    }

    public interface ILeaderboardService
    {
        List<LeaderboardEntry> Top(LeaderboardKind kind, SkillBranch branch, int count);
        PlayerProfile Profile(string playerId);
    }

    // Stable fake players around the real local player; the same interface will be backed by Supabase.
    public class MockLeaderboardService : ILeaderboardService
    {
        private static readonly string[] Names =
        {
            "Pixel Pam", "Bubble Ben", "Cozy Kit", "Mochi Max", "Sunny Sol", "Luna Lee", "Pebble Po", "Dot Dana",
            "Fern Finn", "Nova Nia", "Clover Cal", "Marsh Mia", "Toffee Tom", "Sprout Sam", "Ember Eli", "Willow Wren",
            "Kiwi Kai", "Basil Bo", "Poppy Pat", "Cocoa Cam", "Tulip Tia", "Maple Moe", "Peach Pip", "Juniper Jo",
        };

        private readonly List<PlayerProfile> _players = new List<PlayerProfile>();
        private readonly Func<PlayerProfile> _local;

        public MockLeaderboardService(Func<PlayerProfile> local)
        {
            _local = local;
            var random = new System.Random(20260913);
            var species = (SpeciesType[])Enum.GetValues(typeof(SpeciesType));
            for (int i = 0; i < Names.Length; i++)
            {
                var profile = new PlayerProfile
                {
                    PlayerId = "mock-" + i,
                    DisplayName = Names[i],
                    PetName = Names[i].Split(' ')[0],
                    Species = species[i % species.Length],
                    Level = 1 + random.Next(0, 11),
                    StreakDays = 1 + random.Next(0, 30),
                };
                for (int b = 0; b < 7; b++) profile.BranchXp[b] = random.Next(0, 4) == 0 ? 0 : random.Next(20, 1400);
                int best = 0;
                for (int b = 1; b < 7; b++) if (profile.BranchXp[b] > profile.BranchXp[best]) best = b;
                profile.EvolutionBranch = (SkillBranch)best;
                profile.EvolutionStage = Math.Min(SkillTreeSystem.MaxStage, profile.BranchXp[best] / SkillTreeSystem.XpPerStage);
                _players.Add(profile);
            }
        }

        public List<LeaderboardEntry> Top(LeaderboardKind kind, SkillBranch branch, int count)
        {
            var all = new List<PlayerProfile>(_players) { _local() };
            var entries = new List<LeaderboardEntry>();
            foreach (var p in all)
            {
                int value = kind == LeaderboardKind.Level ? p.Level : p.BranchXp[(int)branch];
                entries.Add(new LeaderboardEntry { PlayerId = p.PlayerId, DisplayName = p.DisplayName, PetName = p.PetName, Species = p.Species, Value = value, IsLocal = p.PlayerId == "local" });
            }
            entries.Sort((a, b) => b.Value != a.Value ? b.Value.CompareTo(a.Value) : string.CompareOrdinal(a.DisplayName, b.DisplayName));
            for (int i = 0; i < entries.Count; i++) entries[i].Rank = i + 1;
            if (entries.Count > count) entries.RemoveRange(count, entries.Count - count);
            return entries;
        }

        public PlayerProfile Profile(string playerId)
        {
            if (playerId == "local") return _local();
            return _players.Find(p => p.PlayerId == playerId);
        }
    }
}
