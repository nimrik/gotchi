using System;
using Gotchi.Core;
using Gotchi.Data;

namespace Gotchi.Systems
{
    // Ambient emotion is derived from need levels; explicit moments (a won mini-game, a rescue
    // from starvation) temporarily override it. Every category is reachable — see ComputeAmbient
    // for the need-driven ones and the callers of TriggerEvent for the event-driven ones.
    public class EmotionSystem
    {
        public const float CriticalThreshold = 15f;
        public const float LowThreshold = 35f;
        public const float ModerateThreshold = 55f;
        public const float HappyThreshold = 72f;
        public const float BlissThreshold = 85f;

        private readonly NeedsSystem _needs;
        private readonly GameClock _clock;

        private bool _hasOverride;
        private EmotionType _override;
        private DateTime _overrideExpiresUtc;

        public EmotionType Current { get; private set; }
        public EmotionCategory CurrentCategory => EmotionCatalog.GetCategory(Current);

        public event Action<EmotionType> OnEmotionChanged;

        public EmotionSystem(NeedsSystem needs, GameClock clock)
        {
            _needs = needs;
            _clock = clock;
            Current = ComputeAmbient();
        }

        public void TriggerEvent(EmotionType emotion, float durationSeconds = 8f)
        {
            _hasOverride = true;
            _override = emotion;
            _overrideExpiresUtc = _clock.UtcNow.AddSeconds(durationSeconds);
            Apply(emotion);
        }

        public void Refresh()
        {
            if (_hasOverride)
            {
                if (_clock.UtcNow < _overrideExpiresUtc) return;
                _hasOverride = false;
            }
            Apply(ComputeAmbient());
        }

        private void Apply(EmotionType emotion)
        {
            if (emotion == Current) return;
            Current = emotion;
            OnEmotionChanged?.Invoke(emotion);
        }

        private EmotionType ComputeAmbient()
        {
            NeedType lowest = _needs.LowestNeed;
            float lowestValue = _needs.Get(lowest);

            if (lowestValue < CriticalThreshold)
            {
                switch (lowest)
                {
                    case NeedType.Hunger: return EmotionType.Fury;
                    case NeedType.Hygiene: return EmotionType.Revulsion;
                    case NeedType.Energy: return EmotionType.Overwhelmed;
                    default: return EmotionType.Grief;
                }
            }

            if (lowestValue < LowThreshold)
            {
                switch (lowest)
                {
                    case NeedType.Hunger: return EmotionType.Irritation;
                    case NeedType.Hygiene: return EmotionType.Dislike;
                    case NeedType.Energy: return EmotionType.Helplessness;
                    default: return EmotionType.Loneliness;
                }
            }

            if (lowestValue < ModerateThreshold)
            {
                switch (lowest)
                {
                    case NeedType.Hunger: return EmotionType.Worry;
                    case NeedType.Hygiene: return EmotionType.Aversion;
                    case NeedType.Energy: return EmotionType.Nervousness;
                    default: return EmotionType.Sorrow;
                }
            }

            float average = _needs.Average;
            if (average >= BlissThreshold) return EmotionType.Love;

            if (average >= HappyThreshold)
            {
                switch (_needs.HighestNeed)
                {
                    case NeedType.Happiness: return EmotionType.Joy;
                    case NeedType.Hunger: return EmotionType.Satisfaction;
                    case NeedType.Energy: return EmotionType.Gladness;
                    default: return EmotionType.Pride;
                }
            }

            return lowest == NeedType.Happiness ? EmotionType.Curiosity : EmotionType.Warmth;
        }
    }
}
