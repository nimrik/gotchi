using System;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // The pet's story, read like an RPG cutscene: portrait above, text box below, tap to continue.
    public class StoryPanelView
    {
        private static readonly EmotionType[] ChapterMoods =
        {
            EmotionType.Nervousness, EmotionType.Gratitude, EmotionType.Curiosity, EmotionType.Joy, EmotionType.Pride,
            EmotionType.Wonder, EmotionType.Affection, EmotionType.Excitement, EmotionType.Inspiration, EmotionType.Warmth,
            EmotionType.Amazement, EmotionType.Love,
        };

        private readonly GameContext _ctx;
        private readonly MonoBehaviour _host;
        private readonly Action _onClose;
        private readonly Text _header;
        private readonly Text _chapterLabel;
        private readonly DialogBoxView _dialog;
        private readonly RectTransform _stage;
        private PetPortraitView _pet;

        public readonly GameObject Root;

        public StoryPanelView(Transform parent, GameContext ctx, Action onClose, MonoBehaviour host)
        {
            _ctx = ctx;
            _host = host;
            _onClose = onClose;
            var root = UIFactory.CreateRect("StoryRoot", parent);
            Root = root.gameObject;
            var scrim = UIFactory.CreatePanel("Scrim", root, UIFactory.Scrim);
            UIFactory.Fill(scrim.rectTransform);
            scrim.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());

            var card = UIFactory.CreateCard("Panel", root, UIFactory.PanelBlue);
            UIFactory.Fill((RectTransform)card.transform.parent, 28f, 28f, 100f, 120f);
            _header = UIFactory.CreateText("Header", card.transform, "", 44, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(_header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -84f), new Vector2(0f, -24f));
            _chapterLabel = UIFactory.CreatePixelText("Chapter", card.transform, "", 26, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            UIFactory.Place(_chapterLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -130f), new Vector2(0f, -90f));

            var scene = UIFactory.CreateFrame("Scene", card.transform, UIFactory.Hex("DFF3E4"));
            scene.raycastTarget = false;
            UIFactory.Place(scene.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 420f), new Vector2(-24f, -140f));
            var grass = UIFactory.CreateCircle("Grass", scene.transform, UIFactory.Hex("BFE7C8"), 520f);
            UIFactory.Place(grass.rectTransform, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(-260f, -90f), new Vector2(260f, 90f));
            grass.raycastTarget = false;
            _stage = UIFactory.CreateRect("Stage", scene.transform);
            UIFactory.Place(_stage, new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), Vector2.zero, Vector2.zero);

            _dialog = new DialogBoxView(card.transform, host, 30);
            UIFactory.Place(_dialog.Root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 120f), new Vector2(-24f, 400f));

            var back = UIFactory.CreateButton("Back", card.transform, "Back", UIFactory.Card, onClose, 30, host);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 96f));
            UIFactory.FitToLabel(back);
        }

        public void Refresh()
        {
            _header.text = $"{_ctx.Data.petName}'s story";
            if (_pet == null)
            {
                var anchor = UIFactory.CreateRect("Pet", _stage);
                _pet = new PetPortraitView(anchor, _ctx.Data.species, _host, 300f);
            }
            Play(1);
        }

        private void Play(int chapter)
        {
            int level = _ctx.Level.Level;
            if (chapter > StoryBook.ChapterCount)
            {
                _chapterLabel.text = "THE END... FOR NOW";
                _dialog.Say($"{_ctx.Data.petName} looks up at you. There will be more to tell soon.", _onClose);
                return;
            }
            if (chapter > level)
            {
                _chapterLabel.text = $"CHAPTER {chapter} - LOCKED";
                _pet.SetFace(EmotionType.Curiosity, true);
                _dialog.Say($"Reach level {chapter} to read the next chapter.", _onClose);
                return;
            }
            _chapterLabel.text = $"CHAPTER {chapter} OF {StoryBook.ChapterCount}";
            _pet.SetFace(ChapterMoods[(chapter - 1) % ChapterMoods.Length], true);
            _dialog.Say(StoryBook.Chapter(chapter, _ctx.Data.petName), () => Play(chapter + 1));
        }
    }
}
