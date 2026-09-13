using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.MiniGames;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    public class MiniGameOverlayView
    {
        private readonly MonoBehaviour _host;
        private readonly Action<MiniGameResult> _onResult;
        private readonly Func<SkillBranch, MiniGameDifficulty> _difficultyFor;
        private readonly RectTransform _playArea;
        private readonly Text _title;
        private readonly Text _result;
        private GameObject _gameObject;
        private IMiniGame _game;
        private bool _finished;
        private MiniGameInfo _currentInfo;

        public readonly GameObject Root;

        public MiniGameOverlayView(Transform parent, Action<MiniGameResult> onResult, Func<SkillBranch, MiniGameDifficulty> difficultyFor, MonoBehaviour host)
        {
            _host = host;
            _onResult = onResult;
            _difficultyFor = difficultyFor;

            var panel = UIFactory.CreatePanel("MiniGameOverlay", parent, UIFactory.Cream);
            Root = panel.gameObject;
            UIFactory.Fill(panel.rectTransform);
            var glow = UIFactory.CreateGradient("Glow", panel.transform, UIFactory.Peach);
            UIFactory.Place(glow.rectTransform, new Vector2(0f, 0.6f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var safe = UIFactory.CreateRect("Safe", panel.transform);
            UIFactory.ApplySafeArea(safe);

            _title = UIFactory.CreateText("Title", safe, "", 50, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -120f), new Vector2(0f, -20f));

            var card = UIFactory.CreateCard("PlayArea", safe, UIFactory.Card);
            UIFactory.Place((RectTransform)card.transform.parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 230f), new Vector2(-28f, -130f));
            card.gameObject.AddComponent<RectMask2D>();
            _playArea = card.rectTransform;

            _result = UIFactory.CreateText("Result", safe, "", 32, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(_result.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 118f), new Vector2(-28f, 218f));

            var close = UIFactory.CreateButton("Close", safe, "Back", UIFactory.Card, Close, 30, host);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-170f, 28f), new Vector2(170f, 104f));

            Root.SetActive(false);
        }

        public void Open(MiniGameInfo info)
        {
            if (info == null || !info.Implemented) return;
            Close();
            Root.SetActive(true);
            _currentInfo = info;
            var difficulty = _difficultyFor(info.Branch);
            _title.text = $"{info.DisplayName} · Tier {difficulty.Tier}";
            _result.text = info.Tagline + (difficulty.Tier > 1 ? $"   Rewards ×{difficulty.RewardMultiplier:0.##}" : "");
            _finished = false;

            _gameObject = new GameObject("MiniGame", typeof(RectTransform));
            _gameObject.transform.SetParent(_playArea, false);
            _game = info.Attach(_gameObject);
            _game.OnCompleted += HandleCompleted;
            _game.Begin(_playArea, difficulty);
            _host.StartCoroutine(SimpleTween.PopIn(_playArea.parent, 0.25f));
        }

        private void HandleCompleted(MiniGameResult result)
        {
            _finished = true;
            _result.text = "";
            _onResult?.Invoke(result);
            _host.StartCoroutine(ShowResults(result));
        }

        private static readonly Color[] Confetti = { UIFactory.Pink, UIFactory.Sky, UIFactory.Mint, UIFactory.Lavender, UIFactory.Butter, UIFactory.Coral };

        private IEnumerator ShowResults(MiniGameResult result)
        {
            yield return new WaitForSeconds(0.35f);
            if (!_finished || !Root.activeSelf) yield break;

            var card = UIFactory.CreateCard("Results", _playArea, UIFactory.Cream, 1f);
            var holder = (RectTransform)card.transform.parent;
            UIFactory.Place(holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-340f, -310f), new Vector2(340f, 310f));

            var title = UIFactory.CreateText("Title", card.transform, result.Summary, 40, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -120f), new Vector2(-20f, -30f));

            int lit = !result.Won ? 1 : result.XpReward >= 60 ? 3 : 2;
            var stars = new List<RectTransform>();
            for (int i = 0; i < 3; i++)
            {
                var star = UIFactory.CreateIcon(UIFactory.IconKind.Sparkle, card.transform, 84f, UIFactory.Cream);
                UIFactory.Place(star, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-42f + (i - 1) * 120f, -230f), new Vector2(42f + (i - 1) * 120f, -146f));
                if (i >= lit)
                    foreach (var g in star.GetComponentsInChildren<Graphic>()) { var c = g.color; c.a = 0.22f; g.color = c; }
                else
                    star.gameObject.SetActive(false);
                stars.Add(star);
            }

            var score = UIFactory.CreateText("Score", card.transform, "Score 0", 46, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(score.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, -10f), new Vector2(-20f, 70f));
            var rewards = UIFactory.CreateText("Rewards", card.transform, $"+{result.XpReward} XP   +{result.CoinReward} coins" + (result.Tier > 1 ? $"\nTier {result.Tier} · ×{result.RewardMultiplier:0.##}" : ""), 30, UIFactory.PinkDark, TextAnchor.MiddleCenter, true);
            UIFactory.Place(rewards.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, -80f), new Vector2(-20f, -20f));

            var again = UIFactory.CreateButton("Again", card.transform, "Play again", UIFactory.Pink, () => Open(_currentInfo), 30, _host);
            UIFactory.Place((RectTransform)again.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-170f, 34f), new Vector2(170f, 110f));

            _host.StartCoroutine(SimpleTween.PopIn(holder, 0.3f));
            _host.StartCoroutine(SimpleTween.CountUp(score, 0, result.Score, 0.9f, "", "Score "));
            _host.StartCoroutine(ConfettiBurst(holder));
            for (int i = 0; i < lit; i++)
            {
                yield return new WaitForSeconds(0.25f);
                stars[i].gameObject.SetActive(true);
                _host.StartCoroutine(SimpleTween.PopIn(stars[i], 0.35f));
            }
        }

        private IEnumerator ConfettiBurst(RectTransform origin)
        {
            var pieces = new List<(RectTransform rect, Vector2 velocity, Graphic graphic)>();
            for (int i = 0; i < 26; i++)
            {
                var piece = UIFactory.CreateCircle("Confetti", _playArea, Confetti[i % Confetti.Length], UnityEngine.Random.Range(14f, 28f));
                piece.raycastTarget = false;
                piece.rectTransform.anchoredPosition = origin.anchoredPosition + new Vector2(0f, 200f);
                float angle = UnityEngine.Random.Range(30f, 150f) * Mathf.Deg2Rad;
                float speed = UnityEngine.Random.Range(500f, 1000f);
                pieces.Add((piece.rectTransform, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed, piece));
            }
            float elapsed = 0f;
            const float duration = 1.3f;
            while (elapsed < duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                for (int i = 0; i < pieces.Count; i++)
                {
                    var (rect, velocity, graphic) = pieces[i];
                    if (rect == null) continue;
                    velocity += Vector2.down * 1600f * dt;
                    rect.anchoredPosition += velocity * dt;
                    var c = graphic.color; c.a = 1f - Mathf.Clamp01((elapsed - 0.6f) / (duration - 0.6f)); graphic.color = c;
                    pieces[i] = (rect, velocity, graphic);
                }
                yield return null;
            }
            foreach (var (rect, _, __) in pieces) if (rect != null) UnityEngine.Object.Destroy(rect.gameObject);
        }

        public void Close()
        {
            if (_game != null)
            {
                _game.OnCompleted -= HandleCompleted;
                if (!_finished) _game.Abort();
                _game = null;
            }
            if (_gameObject != null) UnityEngine.Object.Destroy(_gameObject);
            _gameObject = null;
            for (int i = _playArea.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_playArea.GetChild(i).gameObject);
            Root.SetActive(false);
        }
    }
}
