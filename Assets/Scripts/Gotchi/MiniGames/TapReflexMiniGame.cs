using System;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.MiniGames
{
    public class TapReflexMiniGame : MonoBehaviour, IMiniGame
    {
        private const float Duration = 20f;
        private const int MaxCombo = 5;
        private MiniGameDifficulty _difficulty;
        private float TargetLife => Mathf.Lerp(1.2f, 0.7f, _difficulty.Intensity);
        private float TargetSize => Mathf.Lerp(150f, 108f, _difficulty.Intensity);
        private int WinScore => 150 + 60 * (_difficulty.Tier - 1);

        private static readonly Color[] Palette = { UIFactory.Pink, UIFactory.Sky, UIFactory.Mint, UIFactory.Lavender, UIFactory.Butter, UIFactory.Coral };

        private enum Kind { Normal, Golden, Thorn }

        private class Target
        {
            public GameObject Go;
            public Image Image;
            public Color BaseColor;
            public float Life;
            public Kind Kind;
        }

        private MiniGameStage _stage;

        private RectTransform _playArea;
        private Text _hud;
        private readonly List<Target> _targets = new List<Target>();
        private bool _running;
        private float _timeLeft;
        private float _spawnTimer;
        private int _score;
        private int _combo = 1;

        public SkillBranch Branch => SkillBranch.Sport;
        public event Action<MiniGameResult> OnCompleted;

        public void Begin(RectTransform playArea, MiniGameDifficulty difficulty)
        {
            _playArea = playArea;
            _difficulty = difficulty;
            Canvas.ForceUpdateCanvases();
            _hud = UIFactory.CreateText("HUD", playArea, "", 34, UIFactory.Ink, TextAnchor.UpperCenter, true);
            var rect = _hud.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -10f);
            rect.sizeDelta = new Vector2(0f, 72f);

            _stage = new MiniGameStage(playArea, new Vector2(0.5f, 0.11f), 190f, this);
            _timeLeft = Duration;
            _score = 0;
            _combo = 1;
            _running = true;
            RefreshHud();
        }

        public void Abort()
        {
            _running = false;
            ClearTargets();
        }

        private void Update()
        {
            if (!_running) return;
            float dt = Time.deltaTime;
            _timeLeft -= dt;
            _spawnTimer -= dt;

            if (_spawnTimer <= 0f)
            {
                SpawnTarget();
                float progress = 1f - _timeLeft / Duration;
                _spawnTimer = Mathf.Lerp(Mathf.Lerp(0.9f, 0.55f, _difficulty.Intensity), Mathf.Lerp(0.45f, 0.28f, _difficulty.Intensity), progress);
            }

            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                var target = _targets[i];
                target.Life -= dt;
                if (target.Image != null)
                    target.Image.color = Color.Lerp(Color.Lerp(target.BaseColor, UIFactory.PinkDark, 0.6f), target.BaseColor, target.Life / TargetLife);
                if (target.Life > 0f) continue;
                _targets.RemoveAt(i);
                _combo = 1;
                StartCoroutine(SimpleTween.ShrinkAndDestroy(target.Go));
            }

            RefreshHud();
            if (_timeLeft <= 0f) Finish();
        }

        private void SpawnTarget()
        {
            Rect area = _playArea.rect;
            float half = TargetSize / 2f;
            float x = UnityEngine.Random.Range(area.xMin + half, area.xMax - half);
            float y = UnityEngine.Random.Range(area.yMin + half + 230f, area.yMax - half - 70f);

            float roll = UnityEngine.Random.value;
            Kind kind = roll < 0.1f ? Kind.Golden : (_difficulty.Tier >= 2 && roll < 0.24f) ? Kind.Thorn : Kind.Normal;
            Color color = kind == Kind.Golden ? UIFactory.Hex("F5B942") : kind == Kind.Thorn ? UIFactory.Hex("8E8AA3") : Palette[UnityEngine.Random.Range(0, Palette.Length)];
            float size = kind == Kind.Golden ? TargetSize * 1.15f : TargetSize;
            var image = UIFactory.CreateBubble("Bubble", _playArea, color, size);
            image.rectTransform.anchoredPosition = new Vector2(x, y);
            if (kind == Kind.Thorn)
            {
                foreach (float angle in new[] { 45f, -45f })
                {
                    var spike = UIFactory.CreateRoundedRadius("Spike", image.transform, UIFactory.Ink, 6f);
                    spike.rectTransform.sizeDelta = new Vector2(10f, size * 0.5f);
                    spike.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                    spike.raycastTarget = false;
                }
            }
            var target = new Target { Go = image.gameObject, Image = image, BaseColor = color, Life = kind == Kind.Golden ? TargetLife * 0.8f : TargetLife, Kind = kind };
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => OnTapped(target));
            _targets.Add(target);
            StartCoroutine(SimpleTween.PopIn(image.transform));
        }

        private void OnTapped(Target target)
        {
            if (!_running || !_targets.Remove(target)) return;
            _stage.LungeTo(target.Go.GetComponent<RectTransform>().anchoredPosition.x);
            if (target.Kind == Kind.Thorn)
            {
                _score = Mathf.Max(0, _score - 10);
                _combo = 1;
                _stage.React(EmotionType.Shock);
            }
            else
            {
                _score += (target.Kind == Kind.Golden ? 30 : 10) * _combo;
                _combo = Mathf.Min(MaxCombo, _combo + 1);
                if (target.Kind == Kind.Golden) _stage.React(EmotionType.Amazement);
                else if (_combo >= 4) _stage.React(EmotionType.Excitement);
            }
            StartCoroutine(SimpleTween.Flash(target.Image, Color.white, 0.1f));
            StartCoroutine(SimpleTween.ShrinkAndDestroy(target.Go, 0.12f));
            StartCoroutine(SimpleTween.PunchScale(_hud.transform, 0.1f, 0.15f));
        }

        private void RefreshHud()
        {
            if (_hud != null) _hud.text = $"{Mathf.CeilToInt(Mathf.Max(0f, _timeLeft))}s   Score {_score}   x{_combo}";
        }

        private void Finish()
        {
            _running = false;
            ClearTargets();
            bool won = _score >= WinScore;
            _stage.React(won ? EmotionType.Pride : EmotionType.Embarrassment);
            OnCompleted?.Invoke(MiniGameRewards.Build(Branch, _score, won, won ? "Great reflexes!" : "Nice warm-up. Try again!", _difficulty));
        }

        private void ClearTargets()
        {
            foreach (var target in _targets)
                if (target.Go != null) Destroy(target.Go);
            _targets.Clear();
        }
    }
}
