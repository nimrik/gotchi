using System;
using Gotchi.Core;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    public class StoryPanelView
    {
        private readonly GameContext _ctx;
        private readonly RectTransform _content;
        private readonly Text _header;

        public readonly GameObject Root;

        public StoryPanelView(Transform parent, GameContext ctx, Action onClose, MonoBehaviour host)
        {
            _ctx = ctx;
            var root = UIFactory.CreateRect("StoryRoot", parent);
            Root = root.gameObject;
            var scrim = UIFactory.CreatePanel("Scrim", root, UIFactory.Scrim);
            UIFactory.Fill(scrim.rectTransform);
            scrim.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());

            var card = UIFactory.CreateCard("Panel", root, UIFactory.Cream);
            UIFactory.Fill((RectTransform)card.transform.parent, 28f, 28f, 100f, 120f);
            _header = UIFactory.CreateText("Header", card.transform, "", 44, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(_header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -84f), new Vector2(0f, -24f));
            _content = UIFactory.CreateScrollColumn("Scroll", card.transform, UIFactory.Spacing.List, new RectOffset(24, 24, 8, 8));
            UIFactory.Place((RectTransform)_content.parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 110f), new Vector2(0f, -96f));
            var back = UIFactory.CreateButton("Back", card.transform, "Back", UIFactory.Card, onClose, 30, host);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 96f));
        }

        public void Refresh()
        {
            int level = _ctx.Level.Level;
            _header.text = $"{_ctx.Data.petName}'s story · Lv {level}";
            for (int i = _content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);
            for (int chapter = 1; chapter <= StoryBook.ChapterCount; chapter++)
            {
                bool unlocked = chapter <= level;
                var row = UIFactory.CreateCard("Chapter" + chapter, _content, unlocked ? UIFactory.Card : UIFactory.Hex("F1ECF2"), 0.8f);
                UIFactory.SetPreferredHeight(row.transform.parent.gameObject, 124f);
                var title = UIFactory.CreateText("Title", row.transform, $"Chapter {chapter}" + (unlocked ? "" : $" · reach level {chapter}"), 24, unlocked ? UIFactory.PinkDark : UIFactory.Muted, TextAnchor.MiddleLeft, true);
                UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -44f), new Vector2(-24f, -10f));
                var body = UIFactory.CreateText("Body", row.transform, unlocked ? StoryBook.Chapter(chapter, _ctx.Data.petName) : "…", 22, UIFactory.Ink, TextAnchor.UpperLeft);
                UIFactory.Place(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 8f), new Vector2(-24f, -48f));
            }
        }
    }
}
