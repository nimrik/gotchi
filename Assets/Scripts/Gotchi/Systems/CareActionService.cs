using System;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;

namespace Gotchi.Systems
{
    public enum CareAction { Feed, Clean, Rest, Play }

    public class CareActionService
    {
        public const float CooldownSeconds = 20f;
        public const float RescueThreshold = 20f;
        public const float CuddleCooldownSeconds = 45f;
        public const float CuddleThreshold = 70f;
        private DateTime _lastCuddleUtc = DateTime.MinValue;

        private readonly NeedsSystem _needs;
        private readonly EmotionSystem _emotions;
        private readonly GameClock _clock;
        private readonly Dictionary<CareAction, DateTime> _lastPerformedUtc = new Dictionary<CareAction, DateTime>();

        public event Action<CareAction> OnActionPerformed;
        public Func<float> CooldownScale = () => 1f;

        public CareActionService(NeedsSystem needs, EmotionSystem emotions, GameClock clock)
        {
            _needs = needs;
            _emotions = emotions;
            _clock = clock;
        }

        public static NeedType NeedFor(CareAction action)
        {
            switch (action)
            {
                case CareAction.Feed: return NeedType.Hunger;
                case CareAction.Clean: return NeedType.Hygiene;
                case CareAction.Rest: return NeedType.Energy;
                default: return NeedType.Happiness;
            }
        }

        public static float RestoreAmount(CareAction action) => action == CareAction.Play ? 25f : 30f;

        public float RemainingCooldown(CareAction action)
        {
            if (!_lastPerformedUtc.TryGetValue(action, out DateTime last)) return 0f;
            double remaining = CooldownSeconds * CooldownScale() - (_clock.UtcNow - last).TotalSeconds;
            return remaining <= 0d ? 0f : (float)remaining;
        }

        public bool CanPerform(CareAction action) => RemainingCooldown(action) <= 0f;

        public bool TryPerform(CareAction action)
        {
            if (!CanPerform(action)) return false;

            NeedType need = NeedFor(action);
            bool wasRescue = _needs.Get(need) < RescueThreshold;
            _needs.Add(need, RestoreAmount(action));
            _lastPerformedUtc[action] = _clock.UtcNow;

            _emotions.TriggerEvent(wasRescue ? EmotionType.Gratitude : ReactionFor(action));
            OnActionPerformed?.Invoke(action);
            return true;
        }

        public bool CanCuddle => AllNeedsAbove(CuddleThreshold) && CuddleCooldownRemaining <= 0f;

        public float CuddleCooldownRemaining
        {
            get
            {
                double remaining = CuddleCooldownSeconds - (_clock.UtcNow - _lastCuddleUtc).TotalSeconds;
                return remaining <= 0d ? 0f : (float)remaining;
            }
        }

        public bool TryCuddle()
        {
            if (!CanCuddle) return false;
            _lastCuddleUtc = _clock.UtcNow;
            _needs.Add(NeedType.Happiness, 5f);
            _emotions.TriggerEvent(EmotionType.Affection, 10f);
            OnCuddled?.Invoke();
            return true;
        }

        public event Action OnCuddled;

        private bool AllNeedsAbove(float value)
        {
            foreach (NeedType need in Enum.GetValues(typeof(NeedType)))
                if (_needs.Get(need) < value) return false;
            return true;
        }

        private static EmotionType ReactionFor(CareAction action)
        {
            switch (action)
            {
                case CareAction.Feed: return EmotionType.Satisfaction;
                case CareAction.Clean: return EmotionType.Gladness;
                case CareAction.Rest: return EmotionType.Relief;
                default: return EmotionType.Joy;
            }
        }
    }
}
