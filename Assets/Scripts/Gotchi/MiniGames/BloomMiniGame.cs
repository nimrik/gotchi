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
    // Nature: seeds appear one at a time; plant each in the pot of its colour before it wilts.
    public class BloomMiniGame : MonoBehaviour, IMiniGame
    {
        private const int Seeds = 12;
        private static readonly Color[] Palette = { UIFactory.Pink, UIFactory.Mint, UIFactory.Sky };

        private RectTransform _playArea;
        private MiniGameDifficulty _difficulty;
        private MiniGameStage _stage;
        private Text _hud;
        private readonly Button[] _pots = new Button[3];
        private readonly RectTransform[] _potTops = new RectTransform[3];
        private readonly int[] _blooms = new int[3];
        private Image _seed;
        private RectTransform _wiltFill;
        private int _seedColor = -1;
        private int _planted, _correct, _score;
        private float _timeLeft, _seedTime;
        private bool _running, _locked;

        public SkillBranch Branch => SkillBranch.Nature;
        public event Action<MiniGameResult> OnCompleted;

        public void Begin(RectTransform playArea, MiniGameDifficulty difficulty)
        {
            _playArea = playArea;
            _difficulty = difficulty;
            Canvas.ForceUpdateCanvases();

            _hud = UIFactory.CreateText("HUD", playArea, "", 32, UIFactory.Ink, TextAnchor.UpperCenter, true);
            UIFactory.Place(_hud.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -82f), new Vector2(0f, -10f));
            _stage = new MiniGameStage(playArea, new Vector2(0.5f, 0.5f), 150f, this);

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                float x = 0.2f + i * 0.3f;
                var pot = UIFactory.CreateRoundedRadius("Pot" + i, playArea, Palette[i], 28f);
                UIFactory.Place(pot.rectTransform, new Vector2(x, 0f), new Vector2(x, 0f), new Vector2(-100f, 60f), new Vector2(100f, 200f));
                var rim = UIFactory.CreateRoundedRadius("Rim", pot.transform, Color.Lerp(Palette[i], UIFactory.Ink, 0.15f), 16f);
                UIFactory.Place(rim.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -34f), new Vector2(10f, 0f));
                rim.raycastTarget = false;
                _pots[i] = UIFactory.MakePressable(pot, () => OnPot(index));
                var top = UIFactory.CreateRect("Blooms", playArea);
                UIFactory.Place(top, new Vector2(x, 0f), new Vector2(x, 0f), new Vector2(0f, 200f), new Vector2(0f, 200f));
                _potTops[i] = top;
            }

            var wilt = UIFactory.CreatePillBar("Wilt", playArea, UIFactory.Mint, out _wiltFill);
            UIFactory.Place(wilt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-160f, -322f), new Vector2(160f, -298f));
            wilt.raycastTarget = false;

            _planted = 0;
            _correct = 0;
            _score = 0;
            _running = true;
            NextSeed();
        }

        public void Abort()
        {
            _running = false;
            StopAllCoroutines();
        }

        private void NextSeed()
        {
            if (_seed != null) Destroy(_seed.gameObject);
            _seedColor = UnityEngine.Random.Range(0, 3);
            _seed = UIFactory.CreateCircle("Seed", _playArea, Palette[_seedColor], 110f);
            UIFactory.Place(_seed.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-55f, -250f), new Vector2(55f, -140f));
            _seed.raycastTarget = false;
            var leaf = UIFactory.CreateIcon(UIFactory.IconKind.Leaf, _seed.transform, 60f, Palette[_seedColor]);
            leaf.anchoredPosition = Vector2.zero;
            StartCoroutine(SimpleTween.PopIn(_seed.transform, 0.25f));
            _seedTime = Mathf.Lerp(2.6f, 1.3f, _difficulty.Intensity);
            _timeLeft = _seedTime;
            _locked = false;
            RefreshHud();
        }

        private void Update()
        {
            if (!_running || _locked) return;
            _timeLeft -= Time.deltaTime;
            _wiltFill.anchorMax = new Vector2(Mathf.Clamp01(_timeLeft / _seedTime), 1f);
            if (_timeLeft <= 0f) Resolve(-1);
        }

        private void OnPot(int index)
        {
            if (!_running || _locked) return;
            Resolve(index);
        }

        private void Resolve(int potIndex)
        {
            _locked = true;
            _planted++;
            bool right = potIndex == _seedColor;
            if (right)
            {
                _correct++;
                _score += 20 + Mathf.RoundToInt(10f * _timeLeft / _seedTime);
                _stage.React(EmotionType.Joy);
                Bloom(potIndex);
                StartCoroutine(SimpleTween.PunchScale(_pots[potIndex].transform, 0.12f, 0.25f));
            }
            else
            {
                _stage.React(potIndex < 0 ? EmotionType.Worry : EmotionType.Embarrassment);
                if (potIndex >= 0) StartCoroutine(SimpleTween.Flash(_pots[potIndex].image, UIFactory.Coral, 0.3f));
            }
            if (_seed != null) StartCoroutine(SimpleTween.ShrinkAndDestroy(_seed.gameObject, 0.2f));
            _seed = null;
            RefreshHud();
            StartCoroutine(Advance());
        }

        private void Bloom(int pot)
        {
            int n = _blooms[pot]++;
            var flower = UIFactory.CreateRect("Flower", _potTops[pot]);
            flower.anchoredPosition = new Vector2(-60f + (n % 3) * 60f, (n / 3) * 40f + 20f);
            foreach (float angle in new[] { 0f, 72f, 144f, 216f, 288f })
            {
                var petal = UIFactory.CreateCircle("Petal", flower, Color.Lerp(Palette[pot], Color.white, 0.35f), 22f);
                petal.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 14f;
                petal.raycastTarget = false;
            }
            var centre = UIFactory.CreateCircle("Centre", flower, UIFactory.Hex("F5B942"), 16f);
            centre.raycastTarget = false;
            StartCoroutine(SimpleTween.PopIn(flower, 0.35f));
        }

        private IEnumerator Advance()
        {
            yield return new WaitForSeconds(0.45f);
            if (!_running) yield break;
            if (_planted < Seeds) NextSeed();
            else Finish();
        }

        private void RefreshHud()
        {
            _hud.text = $"Seed {Mathf.Min(_planted + 1, Seeds)}/{Seeds}   Score {_score}";
        }

        private void Finish()
        {
            _running = false;
            bool won = _correct >= 8;
            _stage.React(won ? EmotionType.Pride : EmotionType.Sorrow);
            OnCompleted?.Invoke(MiniGameRewards.Build(Branch, _score, won, won ? "A whole garden!" : "A few seeds went astray.", _difficulty));
        }
    }
}
