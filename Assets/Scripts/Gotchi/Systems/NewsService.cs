using System;
using System.Collections.Generic;
using Gotchi.Data;

namespace Gotchi.Systems
{
    public interface INewsService
    {
        List<NewsItem> All();
    }

    // Placeholder feed until the Supabase `news` table is wired.
    public class MockNewsService : INewsService
    {
        public List<NewsItem> All() => new List<NewsItem>
        {
            new NewsItem { Id = "n4", Category = NewsCategory.Event, Title = "Battle weekend", DateUtc = new DateTime(2026, 9, 14),
                Body = "PvP battles pay double XP until Sunday. Pick your moves wisely!" },
            new NewsItem { Id = "n3", Category = NewsCategory.Update, Title = "New look", DateUtc = new DateTime(2026, 9, 13),
                Body = "Menus, buttons and the care dock now wear their crisp new boxes. Tell us what you think." },
            new NewsItem { Id = "n2", Category = NewsCategory.BugFix, Title = "Bouncy bubble fixed", DateUtc = new DateTime(2026, 9, 12),
                Body = "Tapping your friend many times no longer sends the mood bubble across the room." },
            new NewsItem { Id = "n1", Category = NewsCategory.Announcement, Title = "Welcome to Gotchi", DateUtc = new DateTime(2026, 9, 10),
                Body = "Thanks for adopting a friend. Feed, clean, play and rest to keep them happy, and train a skill each day." },
        };
    }

    // Read state lives in the save so the badge stays accurate across launches.
    public class NewsCenter
    {
        private readonly INewsService _service;
        private readonly PetSaveData _data;
        private List<NewsItem> _cache;

        public NewsCenter(INewsService service, PetSaveData data)
        {
            _service = service;
            _data = data;
        }

        public List<NewsItem> Items => _cache ?? (_cache = _service.All());
        public bool IsRead(NewsItem item) => _data.readNewsIds.Contains(item.Id);
        public int UnreadCount { get { int n = 0; foreach (var item in Items) if (!IsRead(item)) n++; return n; } }

        public void MarkAllRead()
        {
            foreach (var item in Items) if (!_data.readNewsIds.Contains(item.Id)) _data.readNewsIds.Add(item.Id);
        }
    }
}
