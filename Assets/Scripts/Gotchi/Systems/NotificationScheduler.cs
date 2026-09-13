using System;
using Gotchi.Core;
using Gotchi.Data;
using UnityEngine;

namespace Gotchi.Systems
{
    public interface INotificationChannel
    {
        void Schedule(string id, string title, string body, DateTime fireAtLocal);
        void CancelAll();
    }

    // MVP channel: logs instead of delivering. Swap for Unity's Mobile Notifications package
    // (com.unity.mobile.notifications) to send real iOS local notifications.
    public class LogNotificationChannel : INotificationChannel
    {
        public void Schedule(string id, string title, string body, DateTime fireAtLocal) =>
            Debug.Log($"[Notification:{id}] {fireAtLocal:g} — {title}: {body}");

        public void CancelAll() => Debug.Log("[Notification] cancel all");
    }

    public class NotificationScheduler
    {
        public const float ReminderThreshold = 30f;

        public TimeSpan QuietStart = new TimeSpan(21, 0, 0);
        public TimeSpan QuietEnd = new TimeSpan(9, 0, 0);

        private readonly INotificationChannel _channel;
        private readonly GameClock _clock;

        public NotificationScheduler(INotificationChannel channel, GameClock clock)
        {
            _channel = channel;
            _clock = clock;
        }

        // Quiet hours wrap midnight (21:00 → 09:00). Anything inside the window is pushed to QuietEnd.
        public DateTime ClampOutsideQuietHours(DateTime local)
        {
            TimeSpan time = local.TimeOfDay;
            bool wraps = QuietStart > QuietEnd;
            bool inQuiet = wraps
                ? (time >= QuietStart || time < QuietEnd)
                : (time >= QuietStart && time < QuietEnd);
            if (!inQuiet) return local;

            DateTime day = local.Date;
            if (wraps && time >= QuietStart) day = day.AddDays(1);
            return day + QuietEnd;
        }

        public void ScheduleCareReminders(NeedsSystem needs, AutomationSystem automation, string petName)
        {
            _channel.CancelAll();
            DateTime now = _clock.LocalNow;

            foreach (NeedType need in Enum.GetValues(typeof(NeedType)))
            {
                float value = needs.Get(need);
                if (value <= ReminderThreshold) continue;

                float ratePerHour = NeedsSystem.BaseDecayPerHour(need) * automation.GetDecayMultiplier(need);
                if (ratePerHour <= 0f) continue;

                double hoursUntilLow = (value - ReminderThreshold) / ratePerHour;
                DateTime fireAt = ClampOutsideQuietHours(now.AddHours(hoursUntilLow));
                _channel.Schedule($"care_{need}", $"{petName} says hi", BodyFor(need, petName), fireAt);
            }
        }

        // Deliberately gentle copy — no "your pet is suffering" framing (02-game-design.md).
        private static string BodyFor(NeedType need, string petName)
        {
            switch (need)
            {
                case NeedType.Hunger: return $"{petName} could go for a snack whenever you're free.";
                case NeedType.Hygiene: return $"{petName} is thinking about a bubble bath.";
                case NeedType.Energy: return $"{petName} is getting a little sleepy.";
                default: return $"{petName} would love to play when you have a minute.";
            }
        }
    }
}
