using System.Collections;
using Gotchi.Core;
using Gotchi.Creature;
using Gotchi.Creature3D;
using Gotchi.Data;
using Gotchi.Systems;
using Gotchi.UI;
using UnityEngine;

namespace Gotchi.MiniGames
{
    public static class MiniGameContext
    {
        public static SpeciesType Species = SpeciesType.Cat;
        public static string PetName = "Mochi";
        public static int Level = 1;
        // The Battle Club rules and save. Set by the HUD before a battle; null (lab, tests) means a plain default fight.
        public static BattleSystem Battle;
        // The fight to stage next (a wild cat, an area boss). Taken by BattleMiniGame.Begin; null = a ranked battle.
        public static BattleEncounter Encounter;
    }

    // A creature inside a mini-game: placed on the play area, reacts and lunges.
    public class MiniGameStage
    {
        public readonly RectTransform Anchor;
        public readonly PetPortraitView Pet;
        private readonly MonoBehaviour _host;
        private readonly RectTransform _playArea;
        private Coroutine _lunge;

        public MiniGameStage(RectTransform playArea, Vector2 normalizedAnchor, float size, MonoBehaviour host, SpeciesType? species = null, CatCoat coat = null)
        {
            _host = host;
            _playArea = playArea;
            Anchor = UIFactory.CreateRect("Stage", playArea);
            UIFactory.Place(Anchor, normalizedAnchor, normalizedAnchor, Vector2.zero, Vector2.zero);
            Pet = new PetPortraitView(Anchor, species ?? MiniGameContext.Species, host, size, coat);
            Pet.SetFace(EmotionType.Curiosity, false);
        }

        public void React(EmotionType emotion) => Pet.SetFace(emotion, true);
        public void Play(OneShot clip, float direction = 1f) => Pet.Play(clip, direction);
        public void Face(float direction) => Pet.SetFacing(direction);

        // Dash horizontally toward a point in play-area local space, then settle back.
        public void LungeTo(float localX)
        {
            float half = _playArea.rect.width / 2f;
            float target = Mathf.Clamp(localX - (Anchor.anchorMin.x - 0.5f) * _playArea.rect.width, -half * 0.6f, half * 0.6f) * 0.35f;
            Pet.Play(OneShot.Attack, localX >= 0f ? 1f : -1f);
            StartLunge(new Vector2(target, 0f));
        }

        // Dash along `offset` (canvas units) and back: a battle attack goes up the field toward the rival, or down it.
        public void LungeBy(Vector2 offset)
        {
            Pet.Play(OneShot.Attack, offset.x >= 0f ? 1f : -1f);
            StartLunge(offset);
        }

        private void StartLunge(Vector2 offset)
        {
            if (_lunge != null) { _host.StopCoroutine(_lunge); Anchor.anchoredPosition = Vector2.zero; }
            _lunge = _host.StartCoroutine(Lunge(offset));
        }

        private IEnumerator Lunge(Vector2 offset)
        {
            float elapsed = 0f;
            while (elapsed < 0.14f)
            {
                elapsed += Time.deltaTime;
                Anchor.anchoredPosition = Vector2.Lerp(Vector2.zero, offset, SimpleTween.EaseOutCubic(elapsed / 0.14f));
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                Anchor.anchoredPosition = Vector2.Lerp(offset, Vector2.zero, SimpleTween.EaseOutCubic(elapsed / 0.4f));
                yield return null;
            }
            Anchor.anchoredPosition = Vector2.zero;
            _lunge = null;
        }
    }
}
