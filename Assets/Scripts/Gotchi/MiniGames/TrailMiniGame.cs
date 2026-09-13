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
    // Explorer: remember the route. A sequence of directions flashes; repeat it to move along the trail.
    public class TrailMiniGame : MonoBehaviour, IMiniGame
    {
        private const int Rounds = 5;

        private MiniGameDifficulty _difficulty;
        private MiniGameStage _stage;
        private Text _hud;
        private Text _banner;
        private readonly Button[] _arrows = new Button[4];
        private readonly List<int> _sequence = new List<int>();
        private readonly List<RectTransform> _trailDots = new List<RectTransform>();
        private RectTransform _stageAnchor;
        private int _round, _inputIndex, _correctRounds, _score;
        private bool _running, _accepting;

        public SkillBranch Branch => SkillBranch.ExplorerAdventure;
        public event Action<MiniGameResult> OnCompleted;

        public void Begin(RectTransform playArea, MiniGameDifficulty difficulty)
        {
            _difficulty = difficulty;
            Canvas.ForceUpdateCanvases();

            _hud = UIFactory.CreateText("HUD", playArea, "", 32, UIFactory.Ink, TextAnchor.UpperCenter, true);
            UIFactory.Place(_hud.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -82f), new Vector2(0f, -10f));
            _banner = UIFactory.CreateText("Banner", playArea, "", 40, UIFactory.PinkDark, TextAnchor.MiddleCenter, true);
            UIFactory.Place(_banner.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 150f), new Vector2(0f, 220f));

            // Trail across the top: one dot per round, the pet hops along it.
            for (int i = 0; i <= Rounds; i++)
            {
                float x = Mathf.Lerp(0.12f, 0.88f, i / (float)Rounds);
                var dot = UIFactory.CreateCircle("Dot", playArea, i == 0 ? UIFactory.Mint : UIFactory.Hex("E4DEE6"), 24f);
                UIFactory.Place(dot.rectTransform, new Vector2(x, 0.86f), new Vector2(x, 0.86f), new Vector2(-12f, -12f), new Vector2(12f, 12f));
                dot.raycastTarget = false;
                _trailDots.Add(dot.rectTransform);
            }
            _stage = new MiniGameStage(playArea, new Vector2(0.12f, 0.86f), 120f, this);
            _stageAnchor = _stage.Anchor;
            _stageAnchor.anchoredPosition = new Vector2(0f, 70f);

            // Compass buttons: up, right, down, left.
            var centre = new Vector2(0.5f, 0.3f);
            var offsets = new[] { new Vector2(0f, 150f), new Vector2(150f, 0f), new Vector2(0f, -150f), new Vector2(-150f, 0f) };
            var rotations = new[] { 0f, -90f, 180f, 90f };
            var colors = new[] { UIFactory.Sky, UIFactory.Butter, UIFactory.Coral, UIFactory.Mint };
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var button = UIFactory.CreateIconButton("Arrow" + i, playArea, colors[i], UIFactory.IconKind.Compass, 130f, () => OnArrow(index), this, true);
                UIFactory.Place((RectTransform)button.transform, centre, centre, offsets[i] - new Vector2(65f, 65f), offsets[i] + new Vector2(65f, 65f));
                var arrow = UIFactory.CreateRoundedRadius("Arrow", button.transform, UIFactory.Ink, 8f);
                arrow.rectTransform.sizeDelta = new Vector2(14f, 56f);
                arrow.rectTransform.anchoredPosition = new Vector2(0f, 6f);
                arrow.raycastTarget = false;
                var head = UIFactory.CreateRoundedRadius("Head", button.transform, UIFactory.Ink, 8f);
                head.rectTransform.sizeDelta = new Vector2(14f, 34f);
                head.rectTransform.anchoredPosition = new Vector2(-12f, 22f);
                head.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                head.raycastTarget = false;
                var head2 = UIFactory.CreateRoundedRadius("Head2", button.transform, UIFactory.Ink, 8f);
                head2.rectTransform.sizeDelta = new Vector2(14f, 34f);
                head2.rectTransform.anchoredPosition = new Vector2(12f, 22f);
                head2.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                head2.raycastTarget = false;
                button.transform.localRotation = Quaternion.Euler(0f, 0f, rotations[i]);
                foreach (var g in button.GetComponentsInChildren<Graphic>()) if (g.gameObject.name.EndsWith("Icon") || g.transform.parent.name.EndsWith("Icon")) g.gameObject.SetActive(false);
                _arrows[i] = button;
            }

            _round = 0;
            _correctRounds = 0;
            _score = 0;
            _running = true;
            StartCoroutine(PlayRounds());
        }

        public void Abort()
        {
            _running = false;
            StopAllCoroutines();
        }

        private IEnumerator PlayRounds()
        {
            while (_round < Rounds && _running)
            {
                _round++;
                int length = 2 + _round + (_difficulty.Tier - 1) / 2;
                _sequence.Clear();
                for (int i = 0; i < length; i++) _sequence.Add(UnityEngine.Random.Range(0, 4));
                _hud.text = $"Route {_round}/{Rounds} · watch";
                _accepting = false;
                _stage.React(EmotionType.Curiosity);
                yield return new WaitForSeconds(0.6f);
                float gap = Mathf.Lerp(0.55f, 0.32f, _difficulty.Intensity);
                foreach (int step in _sequence)
                {
                    StartCoroutine(SimpleTween.PunchScale(_arrows[step].transform, 0.2f, gap * 0.8f));
                    StartCoroutine(SimpleTween.Flash(_arrows[step].image, Color.white, gap * 0.8f));
                    yield return new WaitForSeconds(gap);
                }
                _hud.text = $"Route {_round}/{Rounds} · your turn";
                _inputIndex = 0;
                _accepting = true;
                float wait = 0f;
                while (_accepting && _running && wait < 12f) { wait += Time.deltaTime; yield return null; }
                if (_accepting) { _accepting = false; StartCoroutine(Miss()); yield return new WaitForSeconds(0.9f); }
                yield return new WaitForSeconds(0.3f);
            }
            if (_running) Finish();
        }

        private void OnArrow(int index)
        {
            if (!_running || !_accepting) return;
            StartCoroutine(SimpleTween.PunchScale(_arrows[index].transform, 0.12f, 0.15f));
            if (index != _sequence[_inputIndex])
            {
                _accepting = false;
                StartCoroutine(Miss());
                return;
            }
            _inputIndex++;
            if (_inputIndex < _sequence.Count) return;
            _accepting = false;
            _correctRounds++;
            _score += 20 * _sequence.Count;
            _stage.React(EmotionType.Excitement);
            StartCoroutine(Advance());
        }

        private IEnumerator Miss()
        {
            _stage.React(EmotionType.Embarrassment);
            _banner.text = "Lost the way…";
            StartCoroutine(SimpleTween.PopIn(_banner.transform, 0.2f));
            yield return new WaitForSeconds(0.8f);
            _banner.text = "";
        }

        private IEnumerator Advance()
        {
            float from = _trailDots[_correctRounds - 1].anchorMin.x, to = _trailDots[_correctRounds].anchorMin.x;
            _trailDots[_correctRounds].GetComponent<Image>().color = UIFactory.Mint;
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = SimpleTween.EaseOutCubic(elapsed / 0.5f);
                float x = Mathf.Lerp(from, to, t);
                _stageAnchor.anchorMin = _stageAnchor.anchorMax = new Vector2(x, 0.86f);
                _stageAnchor.anchoredPosition = new Vector2(0f, 70f + Mathf.Sin(t * Mathf.PI) * 40f);
                yield return null;
            }
            _banner.text = "Found the way!";
            StartCoroutine(SimpleTween.PopIn(_banner.transform, 0.2f));
            yield return new WaitForSeconds(0.6f);
            _banner.text = "";
        }

        private void Finish()
        {
            _running = false;
            bool won = _correctRounds >= 3;
            _stage.React(won ? EmotionType.Wonder : EmotionType.Sorrow);
            OnCompleted?.Invoke(MiniGameRewards.Build(Branch, _score, won, won ? "Expedition complete!" : "The map got soggy. Again?", _difficulty));
        }
    }
}
