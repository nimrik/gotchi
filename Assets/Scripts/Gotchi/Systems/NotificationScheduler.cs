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

        public void CancelAll() => _channel.CancelAll();   // the player switched reminders off in Settings

        // One gentle reminder: the cat is rested and ready again. Nothing decays while the player is away, so there
        // is nothing to nag about; this only says that health and mana are back. Skipped when they already are.
        public void ScheduleReadyReminder(double secondsUntilRested, string petName)
        {
            _channel.CancelAll();
            if (secondsUntilRested < 60d) return;
            DateTime fireAt = ClampOutsideQuietHours(_clock.LocalNow.AddSeconds(secondsUntilRested));
            _channel.Schedule("rested", $"{petName} is rested", $"{petName} has its health and mana back, and is ready for the Battle Club whenever you are.", fireAt);
        }
    }
}
