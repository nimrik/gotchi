using System;
using System.Collections.Generic;
using Gotchi.Data;
using UnityEngine;

namespace Gotchi.MiniGames
{
    public struct MiniGameResult
    {
        public SkillBranch Branch;
        public int Score;
        public bool Won;
        public int XpReward;
        public int CoinReward;
        public string Summary;
        public int Tier;
        public float RewardMultiplier;
        public List<string> Lines;   // extra result lines (rating, streak, quests, promotion, area cleared)
        public bool NoReplay;        // the results card offers Continue instead of another round (fights in the Wild)
    }

    // Progressive difficulty: grows with the branch's evolution stage and the character level; rewards scale with it.
    public struct MiniGameDifficulty
    {
        public int Tier;
        public float Intensity;
        public float RewardMultiplier;

        public static MiniGameDifficulty For(int branchStage, int characterLevel)
        {
            int tier = Mathf.Clamp(1 + branchStage + characterLevel / 4, 1, 9);
            return new MiniGameDifficulty
            {
                Tier = tier,
                Intensity = (tier - 1) / 8f,
                RewardMultiplier = 1f + 0.25f * (tier - 1),
            };
        }

        public static MiniGameDifficulty Default => For(0, 1);
    }

    public interface IMiniGame
    {
        SkillBranch Branch { get; }
        event Action<MiniGameResult> OnCompleted;
        void Begin(RectTransform playArea, MiniGameDifficulty difficulty);
        void Abort();
    }

    // Rewards are fixed functions of performance — no random drops.
    public static class MiniGameRewards
    {
        public static MiniGameResult Build(SkillBranch branch, int score, bool won, string summary, MiniGameDifficulty difficulty)
        {
            return new MiniGameResult
            {
                Branch = branch,
                Score = score,
                Won = won,
                XpReward = Mathf.RoundToInt(Mathf.Clamp(score / 4, 5, 90) * difficulty.RewardMultiplier),
                CoinReward = Mathf.RoundToInt((won ? 25 : 10) * difficulty.RewardMultiplier),
                Summary = summary,
                Tier = difficulty.Tier,
                RewardMultiplier = difficulty.RewardMultiplier,
            };
        }
    }

    public class MiniGameInfo
    {
        public SkillBranch Branch;
        public string DisplayName;
        public string Tagline;
        public bool Implemented;
        public string UnavailableReason;
        public Func<GameObject, IMiniGame> Attach;
    }

    public static class MiniGameRegistry
    {
        // One game: the battle. It is staged for the Battle Club (ranked) and for the Wild (campaign); the other
        // mini-games (Bubble Dash, Word Pals, Curious Minds, Bloom Sort, Trail Memory, Meadow Hunt) were removed on
        // 2026-09-21 when the game was refocused on battles.
        public static readonly MiniGameInfo[] All =
        {
            new MiniGameInfo
            {
                Branch = SkillBranch.PvP, DisplayName = "Battle", Tagline = "Turn-based battles. Train, pick your moves, climb the leagues, explore the Wild.",
                Implemented = true, Attach = go => go.AddComponent<BattleMiniGame>(),
            },
        };

        public static MiniGameInfo For(SkillBranch branch)
        {
            foreach (var info in All)
                if (info.Branch == branch) return info;
            return null;
        }
    }
}
