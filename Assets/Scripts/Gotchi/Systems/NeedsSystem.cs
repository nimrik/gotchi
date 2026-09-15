using System;
using System.Collections.Generic;
using Gotchi.Data;

namespace Gotchi.Systems
{
    public class NeedsSystem
    {
        public const float Max = 100f;
        // The pet never dies (see 02-game-design.md): a long absence stops hurting after this cap.
        public const float MaxOfflineCatchupHours = 36f;

        private static readonly Dictionary<NeedType, float> DecayPerHour = new Dictionary<NeedType, float>
        {
            { NeedType.Hunger, 9f },
            { NeedType.Hygiene, 4f },
            { NeedType.Energy, 6f },
            { NeedType.Happiness, 7f },
        };

        private readonly PetSaveData _data;
        private readonly AutomationSystem _automation;

        public event Action<NeedType, float> OnNeedChanged;

        public NeedsSystem(PetSaveData data, AutomationSystem automation)
        {
            _data = data;
            _automation = automation;
        }

        public static float BaseDecayPerHour(NeedType type) => DecayPerHour[type];

        public float Get(NeedType type) => _data.GetNeed(type);

        public void Set(NeedType type, float value)
        {
            float clamped = Math.Max(0f, Math.Min(Max, value));
            if (clamped == _data.GetNeed(type)) return;
            _data.SetNeed(type, clamped);
            OnNeedChanged?.Invoke(type, clamped);
        }

        public void Add(NeedType type, float delta) => Set(type, Get(type) + delta);

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            float hours = deltaSeconds / 3600f;
            foreach (var pair in DecayPerHour)
            {
                float rate = pair.Value * _automation.GetDecayMultiplier(pair.Key);
                Add(pair.Key, -rate * hours);
            }
        }

        public void ApplyOfflineElapsed(TimeSpan elapsed)
        {
            if (elapsed <= TimeSpan.Zero) return;
            double hours = Math.Min(elapsed.TotalHours, MaxOfflineCatchupHours);
            Tick((float)(hours * 3600d));
        }

        public float Average
        {
            get
            {
                float sum = 0f;
                int count = 0;
                foreach (NeedType type in Enum.GetValues(typeof(NeedType)))
                {
                    sum += Get(type);
                    count++;
                }
                return count == 0 ? 0f : sum / count;
            }
        }

        public NeedType LowestNeed
        {
            get
            {
                NeedType lowest = NeedType.Hunger;
                float lowestValue = float.MaxValue;
                foreach (NeedType type in Enum.GetValues(typeof(NeedType)))
                {
                    float value = Get(type);
                    if (value >= lowestValue) continue;
                    lowestValue = value;
                    lowest = type;
                }
                return lowest;
            }
        }

        public NeedType HighestNeed
        {
            get
            {
                NeedType highest = NeedType.Hunger;
                float highestValue = float.MinValue;
                foreach (NeedType type in Enum.GetValues(typeof(NeedType)))
                {
                    float value = Get(type);
                    if (value <= highestValue) continue;
                    highestValue = value;
                    highest = type;
                }
                return highest;
            }
        }
    }

    // What a round of any mini-game does to the needs: playing is fun (happiness up) but costs food, a wash
    // and energy, a little more at higher tiers. Balanced so one round never pushes a healthy pet under the
    // SkillGate threshold on its own; three rounds in a row will, which sends the player back to caring.
    public static class MiniGameNeeds
    {
        public const float HungerCost = 6f;
        public const float HygieneCost = 5f;
        public const float EnergyCost = 8f;
        public const float HappinessGain = 12f;

        public static float Scale(int tier) => 1f + 0.15f * Math.Max(0, tier - 1);
        public static float Hunger(int tier) => (float)Math.Round(HungerCost * Scale(tier));
        public static float Hygiene(int tier) => (float)Math.Round(HygieneCost * Scale(tier));
        public static float Energy(int tier) => (float)Math.Round(EnergyCost * Scale(tier));
        public static float Happiness(int tier) => (float)Math.Round(HappinessGain * Scale(tier));

        public static void Apply(NeedsSystem needs, int tier)
        {
            needs.Add(NeedType.Hunger, -Hunger(tier));
            needs.Add(NeedType.Hygiene, -Hygiene(tier));
            needs.Add(NeedType.Energy, -Energy(tier));
            needs.Add(NeedType.Happiness, Happiness(tier));
        }

        public static string Describe(int tier) =>
            $"Play +{Happiness(tier):0}   Food −{Hunger(tier):0}   Wash −{Hygiene(tier):0}   Energy −{Energy(tier):0}";
    }
}
