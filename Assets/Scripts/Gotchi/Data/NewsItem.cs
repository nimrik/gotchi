using System;

namespace Gotchi.Data
{
    public enum NewsCategory { Announcement, Update, BugFix, Event }

    // A message from the team shown in the notification center (mocked now, Supabase later).
    public class NewsItem
    {
        public string Id;
        public NewsCategory Category;
        public string Title;
        public string Body;
        public DateTime DateUtc;
    }
}
