using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.MiniGames
{
    // Solo PvE: critters drift across the meadow in waves; tap them before they escape.
    public class HuntMiniGame : MonoBehaviour, IMiniGame
    {
        private MiniGameDifficulty _difficulty;
        private int Waves => 3 + (_difficulty.Tier - 1) / 2;
        private float CritterSize => Mathf.Lerp(130f, 100f, _difficulty.Intensity);
        private float SpawnGap => Mathf.Lerp(0.55f, 0.32f, _difficulty.Intensity);

        private class Critter
        {
            public GameObject Go;
            public RectTransform Rect;
            public Vector2 Velocity;
        }

        private RectTransform _playArea;
        private Text _hud;
        private Text _banner;
        private readonly List<Critter> _critters = new List<Critter>();
        private int _wave;
        private int _pendingSpawns;
        private int _caught;
        private int _escaped;
        private bool _running;
        private MiniGameStage _stage;

        public SkillBranch Branch => SkillBranch.Hunter;
        public event Action<MiniGameResult> OnCompleted;

        public void Begin(RectTransform playArea, MiniGameDifficulty difficulty)
        {
            _playArea = playArea;
            _difficulty = difficulty;
            Canvas.ForceUpdateCanvases();

            _hud = UIFactory.CreateText("HUD", playArea, "", 32, UIFactory.Ink, TextAnchor.UpperCenter, true);
            var hudRect = _hud.rectTransform;
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.anchoredPosition = new Vector2(0f, -10f);
            hudRect.sizeDelta = new Vector2(0f, 72f);

            _banner = UIFactory.CreateText("Banner", playArea, "", 64, UIFactory.PinkDark, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(_banner.rectTransform);

            _stage = new MiniGameStage(playArea, new Vector2(0.5f, 0.1f), 170f, this);
            _wave = 0;
            _caught = 0;
            _escaped = 0;
            _running = true;
            StartCoroutine(RunWaves());
        }

        public void Abort()
        {
            _running = false;
            StopAllCoroutines();
            ClearCritters();
        }

        private IEnumerator RunWaves()
        {
            for (_wave = 1; _wave <= Waves && _running; _wave++)
            {
                _banner.text = $"Wave {_wave}";
                _banner.gameObject.SetActive(true);
                yield return SimpleTween.PopIn(_banner.transform, 0.3f);
                yield return new WaitForSeconds(0.6f);
                _banner.gameObject.SetActive(false);

                _pendingSpawns = 3 + _wave + (_difficulty.Tier - 1) / 2;
                while (_pendingSpawns > 0 && _running)
                {
                    SpawnCritter();
                    _pendingSpawns--;
                    yield return new WaitForSeconds(SpawnGap);
                }
                while (_critters.Count > 0 && _running) yield return null;
                yield return new WaitForSeconds(0.4f);
            }
            if (_running) Finish();
        }

        private void Update()
        {
            if (!_running) return;
            Rect area = _playArea.rect;
            float margin = CritterSize;
            for (int i = _critters.Count - 1; i >= 0; i--)
            {
                var critter = _critters[i];
                if (critter.Rect == null) { _critters.RemoveAt(i); continue; }
                critter.Rect.anchoredPosition += critter.Velocity * Time.deltaTime;
                Vector2 p = critter.Rect.anchoredPosition;
                bool outside = p.x < area.xMin - margin || p.x > area.xMax + margin || p.y < area.yMin - margin || p.y > area.yMax + margin;
                if (!outside) continue;
                _critters.RemoveAt(i);
                _escaped++;
                _stage.React(EmotionType.Worry);
                Destroy(critter.Go);
            }
            RefreshHud();
        }

        private void SpawnCritter()
        {
            Rect area = _playArea.rect;
            float speed = Mathf.Lerp(220f, 420f, (_wave - 1) / (float)Mathf.Max(1, Waves - 1)) * (1f + 0.6f * _difficulty.Intensity);
            bool fromLeft = UnityEngine.Random.value < 0.5f;
            float y = UnityEngine.Random.Range(area.yMin + CritterSize + 200f, area.yMax - CritterSize - 70f);
            float x = fromLeft ? area.xMin - CritterSize / 2f : area.xMax + CritterSize / 2f;
            float drift = UnityEngine.Random.Range(-60f, 60f);

            var image = UIFactory.CreateBubble("Critter", _playArea, UIFactory.Mint, CritterSize);
            foreach (float side in new[] { -1f, 1f })
            {
                var eye = UIFactory.CreateCircle("Eye", image.transform, UIFactory.Ink, CritterSize * 0.13f);
                eye.rectTransform.anchoredPosition = new Vector2(side * CritterSize * 0.18f, CritterSize * 0.06f);
                eye.raycastTarget = false;
                var cheek = UIFactory.CreateCircle("Cheek", image.transform, new Color(1f, 0.55f, 0.68f, 0.6f), CritterSize * 0.14f);
                cheek.rectTransform.anchoredPosition = new Vector2(side * CritterSize * 0.3f, -CritterSize * 0.08f);
                cheek.raycastTarget = false;
            }
            image.rectTransform.anchoredPosition = new Vector2(x, y);
            var critter = new Critter { Go = image.gameObject, Rect = image.rectTransform, Velocity = new Vector2(fromLeft ? speed : -speed, drift) };
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => OnCaught(critter, image));
            _critters.Add(critter);
        }

        private void OnCaught(Critter critter, Image image)
        {
            if (!_running || !_critters.Remove(critter)) return;
            _caught++;
            _stage.LungeTo(critter.Rect.anchoredPosition.x);
            if (_caught % 3 == 0) _stage.React(EmotionType.Excitement);
            StartCoroutine(SimpleTween.Flash(image, Color.white, 0.1f));
            StartCoroutine(SimpleTween.ShrinkAndDestroy(critter.Go, 0.12f));
            StartCoroutine(SimpleTween.PunchScale(_hud.transform, 0.1f, 0.15f));
        }

        private void RefreshHud()
        {
            if (_hud != null) _hud.text = $"Wave {Mathf.Clamp(_wave, 1, Waves)}/{Waves}   Caught {_caught}   Escaped {_escaped}";
        }

        private void Finish()
        {
            _running = false;
            ClearCritters();
            int total = _caught + _escaped;
            bool won = total > 0 && _caught >= Mathf.CeilToInt(total * 0.6f);
            _stage.React(won ? EmotionType.Pride : EmotionType.Sorrow);
            OnCompleted?.Invoke(MiniGameRewards.Build(Branch, _caught * 15, won, won ? "Sharp eyes!" : "The critters got away this time.", _difficulty));
        }

        private void ClearCritters()
        {
            foreach (var critter in _critters)
                if (critter.Go != null) Destroy(critter.Go);
            _critters.Clear();
        }
    }
}
