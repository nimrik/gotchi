using System;
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
        public static readonly MiniGameInfo[] All =
        {
            new MiniGameInfo
            {
                Branch = SkillBranch.Sport, DisplayName = "Bubble Dash", Tagline = "Tap the bubbles before they pop.",
                Implemented = true, Attach = go => go.AddComponent<TapReflexMiniGame>(),
            },
            new MiniGameInfo
            {
                Branch = SkillBranch.Social, DisplayName = "Word Pals", Tagline = "Language quiz.",
                Implemented = true, Attach = go => { var q = go.AddComponent<QuizMiniGame>(); q.Configure(SkillBranch.Social, QuizBank.Language); return q; },
            },
            new MiniGameInfo
            {
                Branch = SkillBranch.Science, DisplayName = "Curious Minds", Tagline = "General knowledge quiz.",
                Implemented = true, Attach = go => { var q = go.AddComponent<QuizMiniGame>(); q.Configure(SkillBranch.Science, QuizBank.Science); return q; },
            },
            new MiniGameInfo
            {
                Branch = SkillBranch.Hunter, DisplayName = "Meadow Hunt", Tagline = "Catch the critters across three waves.",
                Implemented = true, Attach = go => go.AddComponent<HuntMiniGame>(),
            },
            new MiniGameInfo
            {
                Branch = SkillBranch.Nature, DisplayName = "Bloom Sort", Tagline = "Plant each seed in its pot before it wilts.",
                Implemented = true, Attach = go => go.AddComponent<BloomMiniGame>(),
            },
            new MiniGameInfo
            {
                Branch = SkillBranch.PvP, DisplayName = "Battle", Tagline = "Turn-based battle. Pick your moves!",
                Implemented = true, Attach = go => go.AddComponent<BattleMiniGame>(),
            },
            new MiniGameInfo
            {
                Branch = SkillBranch.ExplorerAdventure, DisplayName = "Trail Memory", Tagline = "Watch the route, then repeat it.",
                Implemented = true, Attach = go => go.AddComponent<TrailMiniGame>(),
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
