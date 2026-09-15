using System;
using Gotchi.Core;
using Gotchi.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // Notification center: announcements, updates, bug fixes and events from the team.
    public class NewsPanelView
    {
        private readonly GameContext _ctx;
        private readonly RectTransform _list;

        public readonly GameObject Root;

        public NewsPanelView(Transform parent, GameContext ctx, Action onClose, MonoBehaviour host)
        {
            _ctx = ctx;
            var root = UIFactory.CreateRect("NewsRoot", parent);
            Root = root.gameObject;
            var scrim = UIFactory.CreatePanel("Scrim", root, UIFactory.Scrim);
            UIFactory.Fill(scrim.rectTransform);
            scrim.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());

            var card = UIFactory.CreateCard("Panel", root, UIFactory.PanelBlue);
            UIFactory.Fill((RectTransform)card.transform.parent, 28f, 28f, 100f, 120f);
            var header = UIFactory.CreateText("Header", card.transform, "News", 44, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -84f), new Vector2(0f, -24f));

            _list = UIFactory.CreateScrollColumn("List", card.transform, UIFactory.Spacing.List, new RectOffset(24, 24, 4, 4));
            UIFactory.Place((RectTransform)_list.parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 110f), new Vector2(0f, -100f));

            var back = UIFactory.CreateButton("Back", card.transform, "Back", UIFactory.Card, onClose, 30, host);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 96f));
            UIFactory.FitToLabel(back);
        }

        private static (string label, Color color) Tag(NewsCategory category)
        {
            switch (category)
            {
                case NewsCategory.Update: return ("UPDATE", UIFactory.Sky);
                case NewsCategory.BugFix: return ("BUG FIX", UIFactory.Mint);
                case NewsCategory.Event: return ("EVENT", UIFactory.Butter);
                default: return ("NEWS", UIFactory.Lavender);
            }
        }

        public void Refresh()
        {
            for (int i = _list.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_list.GetChild(i).gameObject);
            foreach (var item in _ctx.News.Items)
            {
                bool unread = !_ctx.News.IsRead(item);
                var row = UIFactory.CreateCard("News" + item.Id, _list, UIFactory.Card, 0.8f);
                UIFactory.SetPreferredHeight(row.transform.parent.gameObject, 176f);
                var (label, color) = Tag(item.Category);
                var tag = UIFactory.CreatePill("Tag", row.transform, color);
                UIFactory.Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -58f), new Vector2(160f, -18f));
                tag.raycastTarget = false;
                var tagText = UIFactory.CreatePixelText("Text", tag.transform, label, 16, UIFactory.MenuInk, TextAnchor.MiddleCenter);
                UIFactory.Fill(tagText.rectTransform);
                var date = UIFactory.CreateText("Date", row.transform, item.DateUtc.ToString("d MMM"), 18, UIFactory.Muted, TextAnchor.MiddleRight);
                UIFactory.Place(date.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-160f, -58f), new Vector2(-20f, -18f));
                var title = UIFactory.CreatePixelText("Title", row.transform, item.Title, 24, UIFactory.MenuInk, TextAnchor.MiddleLeft);
                UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -100f), new Vector2(-20f, -64f));
                var body = UIFactory.CreateText("Body", row.transform, item.Body, 19, UIFactory.Ink, TextAnchor.UpperLeft);
                UIFactory.Place(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 12f), new Vector2(-20f, -104f));
                if (unread)
                {
                    var dot = UIFactory.CreateCircle("Unread", row.transform, UIFactory.PinkDark, 16f);
                    UIFactory.Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -84f), new Vector2(-18f, -68f));
                    dot.raycastTarget = false;
                }
            }
        }
    }
}
