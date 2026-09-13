using System.Collections;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.UI;
using UnityEngine;

namespace Gotchi.MiniGames
{
    public static class MiniGameContext
    {
        public static SpeciesType Species = SpeciesType.Cat;
    }

    // The player's own creature inside a mini-game: placed on the play area, reacts and lunges.
    public class MiniGameStage
    {
        public readonly RectTransform Anchor;
        public readonly PetPortraitView Pet;
        private readonly MonoBehaviour _host;
        private readonly RectTransform _playArea;
        private Coroutine _lunge;

        public MiniGameStage(RectTransform playArea, Vector2 normalizedAnchor, float size, MonoBehaviour host, SpeciesType? species = null)
        {
            _host = host;
            _playArea = playArea;
            Anchor = UIFactory.CreateRect("Stage", playArea);
            UIFactory.Place(Anchor, normalizedAnchor, normalizedAnchor, Vector2.zero, Vector2.zero);
            Pet = new PetPortraitView(Anchor, species ?? MiniGameContext.Species, host, size);
            Pet.SetEmotion(EmotionType.Curiosity, false);
        }

        public void React(EmotionType emotion) => Pet.SetEmotion(emotion, true);

        // Dash horizontally toward a point in play-area local space, then settle back.
        public void LungeTo(float localX)
        {
            if (_lunge != null) _host.StopCoroutine(_lunge);
            float half = _playArea.rect.width / 2f;
            float target = Mathf.Clamp(localX - (Anchor.anchorMin.x - 0.5f) * _playArea.rect.width, -half * 0.6f, half * 0.6f) * 0.35f;
            _lunge = _host.StartCoroutine(Lunge(target));
        }

        private IEnumerator Lunge(float targetX)
        {
            Vector2 start = Anchor.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < 0.14f)
            {
                elapsed += Time.deltaTime;
                Anchor.anchoredPosition = new Vector2(Mathf.Lerp(start.x, targetX, SimpleTween.EaseOutCubic(elapsed / 0.14f)), start.y);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                Anchor.anchoredPosition = new Vector2(Mathf.Lerp(targetX, 0f, SimpleTween.EaseOutCubic(elapsed / 0.4f)), start.y);
                yield return null;
            }
            Anchor.anchoredPosition = new Vector2(0f, start.y);
            _lunge = null;
        }
    }
}
