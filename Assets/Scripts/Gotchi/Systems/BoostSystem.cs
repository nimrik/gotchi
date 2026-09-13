using System;
using Gotchi.Core;
using Gotchi.Data;

namespace Gotchi.Systems
{
    // Timed boosts bought with gems, plus streak shields. Stored as UTC expiry ticks in the save.
    public class BoostSystem
    {
        private readonly PetSaveData _data;
        private readonly GameClock _clock;

        public event Action OnChanged;

        public BoostSystem(PetSaveData data, GameClock clock)
        {
            _data = data;
            _clock = clock;
        }

        public bool RewardBoostActive => _data.rewardBoostUntilUtcTicks > _clock.UtcNow.Ticks;
        public bool CooldownBoostActive => _data.cooldownBoostUntilUtcTicks > _clock.UtcNow.Ticks;
        public float RewardMultiplier => RewardBoostActive ? 2f : 1f;
        public float CooldownScale => CooldownBoostActive ? 0f : 1f;
        public int StreakShields => _data.streakShields;

        public TimeSpan RewardBoostRemaining => Remaining(_data.rewardBoostUntilUtcTicks);
        public TimeSpan CooldownBoostRemaining => Remaining(_data.cooldownBoostUntilUtcTicks);

        private TimeSpan Remaining(long untilTicks)
        {
            long now = _clock.UtcNow.Ticks;
            return untilTicks > now ? TimeSpan.FromTicks(untilTicks - now) : TimeSpan.Zero;
        }

        public void ActivateRewardBoost(float hours)
        {
            _data.rewardBoostUntilUtcTicks = Extend(_data.rewardBoostUntilUtcTicks, hours);
            OnChanged?.Invoke();
        }

        public void ActivateCooldownBoost(float hours)
        {
            _data.cooldownBoostUntilUtcTicks = Extend(_data.cooldownBoostUntilUtcTicks, hours);
            OnChanged?.Invoke();
        }

        private long Extend(long untilTicks, float hours)
        {
            long now = _clock.UtcNow.Ticks;
            return Math.Max(untilTicks, now) + TimeSpan.FromHours(hours).Ticks;
        }

        public void AddStreakShield()
        {
            _data.streakShields++;
            OnChanged?.Invoke();
        }

        public bool ConsumeStreakShield()
        {
            if (_data.streakShields <= 0) return false;
            _data.streakShields--;
            OnChanged?.Invoke();
            return true;
        }
    }
}
