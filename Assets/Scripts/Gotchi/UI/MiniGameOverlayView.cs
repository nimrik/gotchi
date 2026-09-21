using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.MiniGames;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    public class MiniGameOverlayView
    {
        private readonly MonoBehaviour _host;
        private readonly Action<MiniGameResult> _onResult;
        private readonly Func<SkillBranch, MiniGameDifficulty> _difficultyFor;
        private readonly Action<MiniGameInfo> _onClosed;
        private readonly RectTransform _playArea;
        private readonly Text _title;
        private readonly Text _result;
        private GameObject _gameObject;
        private IMiniGame _game;
        private bool _finished;
        private MiniGameInfo _currentInfo;

        public readonly GameObject Root;

        // `onClosed` tells the owner which game the player just left (the HUD reopens the Battle Club after a battle).
        public MiniGameOverlayView(Transform parent, Action<MiniGameResult> onResult, Func<SkillBranch, MiniGameDifficulty> difficultyFor, MonoBehaviour host, Action<MiniGameInfo> onClosed = null)
        {
            _host = host;
            _onResult = onResult;
            _difficultyFor = difficultyFor;
            _onClosed = onClosed;

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
            UIFactory.FitToLabel(close, 200f);

            Root.SetActive(false);
        }

        public void Open(MiniGameInfo info)
        {
            if (info == null || !info.Implemented) return;
            Cleanup();
            Root.SetActive(true);
            _currentInfo = info;
            var difficulty = _difficultyFor(info.Branch);
            var encounter = MiniGameContext.Encounter;
            if (encounter != null && encounter.Arena != null)
            {
                _title.text = encounter.Arena.Name;
                _result.text = encounter.Kind == BattleKind.Boss ? "The area boss. Win to clear the area." : "A wild cat. Health and moves carry over to the next fight.";
            }
            else if (MiniGameContext.Battle != null)
            {
                _title.text = $"{MiniGameContext.Battle.League.Name} League";
                _result.text = $"Rating {MiniGameContext.Battle.Rating} · coins ×{difficulty.RewardMultiplier:0.##}";
            }
            else
            {
                _title.text = info.DisplayName;
                _result.text = info.Tagline;
            }
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

            // Battles add lines of their own (rating, streak, quests, promotion); the card grows downward for them.
            int extraLines = result.Lines != null ? result.Lines.Count : 0;
            float extra = extraLines * 34f;
            var card = UIFactory.CreateCard("Results", _playArea, UIFactory.Cream, 1f);
            var holder = (RectTransform)card.transform.parent;
            UIFactory.Place(holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380f, -310f - extra * 0.5f), new Vector2(380f, 310f + extra * 0.5f));

            var title = UIFactory.CreateText("Title", card.transform, result.Summary, 40, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -120f), new Vector2(-20f, -30f));
            for (int i = 0; i < extraLines; i++)
            {
                var line = UIFactory.CreateText("Line", card.transform, result.Lines[i], 22, i == 0 ? UIFactory.Ink : UIFactory.PinkDark, TextAnchor.MiddleCenter, i == 0);
                UIFactory.Place(line.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 124f + (extraLines - 1 - i) * 34f), new Vector2(-20f, 158f + (extraLines - 1 - i) * 34f));
            }

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

            // Score, rewards and the needs line hang from the top so extra battle lines fit between them and the button.
            var score = UIFactory.CreateText("Score", card.transform, "Score 0", 46, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(score.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -320f), new Vector2(-20f, -240f));
            var rewards = UIFactory.CreateText("Rewards", card.transform, $"+{result.XpReward} XP   +{result.CoinReward} coins" + (result.Tier > 1 && extraLines == 0 ? $"\nTier {result.Tier} · ×{result.RewardMultiplier:0.##}" : ""), 30, UIFactory.PinkDark, TextAnchor.MiddleCenter, true);
            UIFactory.Place(rewards.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -390f), new Vector2(-20f, -330f));
            var note = UIFactory.CreateText("Note", card.transform, "Health and mana stay as the fight left them. The camp brings them back.", 20, UIFactory.Muted, TextAnchor.MiddleCenter);
            UIFactory.Place(note.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -426f), new Vector2(-20f, -394f));

            // A ranked battle offers the next one; a fight in the Wild goes back to the trail.
            var again = result.NoReplay
                ? UIFactory.CreateButton("Again", card.transform, "Continue", UIFactory.Pink, Close, 30, _host)
                : UIFactory.CreateButton("Again", card.transform, "Next battle", UIFactory.Pink, () => Open(_currentInfo), 30, _host);
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

        // Leaves the game for good: tears it down and tells the owner which one it was.
        public void Close()
        {
            bool wasOpen = Root.activeSelf;
            var info = _currentInfo;
            Cleanup();
            if (wasOpen) _onClosed?.Invoke(info);
        }

        private void Cleanup()
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
