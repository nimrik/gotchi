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
            new NewsItem { Id = "n5", Category = NewsCategory.Update, Title = "The camp", DateUtc = new DateTime(2026, 9, 21),
                Body = "REST, FOCUS, FEED and GROOM get your cat ready for the next fight. Nothing runs down while you are away." },
            new NewsItem { Id = "n4", Category = NewsCategory.Event, Title = "Battle weekend", DateUtc = new DateTime(2026, 9, 14),
                Body = "Ranked battles pay double XP until Sunday. Pick your moves wisely!" },
            new NewsItem { Id = "n3", Category = NewsCategory.Update, Title = "New look", DateUtc = new DateTime(2026, 9, 13),
                Body = "Menus, buttons and the camp now wear their crisp new boxes. Tell us what you think." },
            new NewsItem { Id = "n2", Category = NewsCategory.BugFix, Title = "Too many pokes", DateUtc = new DateTime(2026, 9, 12),
                Body = "Tapping your cat no longer changes its face at random. Poke it too much and it walks off for a moment." },
            new NewsItem { Id = "n1", Category = NewsCategory.Announcement, Title = "Welcome to Gotchi", DateUtc = new DateTime(2026, 9, 10),
                Body = "Thanks for taking a cat in. Pick a fighting style, train, and climb the leagues of the Battle Club." },
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
