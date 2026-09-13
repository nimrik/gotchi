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
    public class QuizMiniGame : MonoBehaviour, IMiniGame
    {
        private MiniGameDifficulty _difficulty;
        private int QuestionsPerRound => Mathf.Min(8, 5 + (_difficulty.Tier - 1) / 2);
        private float SecondsPerQuestion => _difficulty.Tier >= 3 ? Mathf.Lerp(10f, 4f, _difficulty.Intensity) : 0f;
        private Image _timerFill;
        private float _timeLeft;
        private static readonly Color Correct = new Color(0.55f, 0.85f, 0.6f);
        private static readonly Color Wrong = new Color(0.95f, 0.55f, 0.55f);

        private SkillBranch _branch = SkillBranch.Science;
        private QuizQuestion[] _bank = QuizBank.Science;
        private readonly List<int> _order = new List<int>();
        private readonly List<Button> _choiceButtons = new List<Button>();
        private Text _prompt;
        private Text _progress;
        private int _index;
        private int _correct;
        private bool _locked;
        private bool _running;
        private MiniGameStage _stage;
        private MiniGameStage _friend;

        public SkillBranch Branch => _branch;
        public event Action<MiniGameResult> OnCompleted;

        public void Configure(SkillBranch branch, QuizQuestion[] bank)
        {
            _branch = branch;
            _bank = bank;
        }

        public void Begin(RectTransform playArea, MiniGameDifficulty difficulty)
        {
            _difficulty = difficulty;
            _order.Clear();
            for (int i = 0; i < _bank.Length; i++) _order.Add(i);
            for (int i = _order.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }
            int rounds = Mathf.Min(QuestionsPerRound, _order.Count);
            _order.RemoveRange(rounds, _order.Count - rounds);

            var friendSpecies = (SpeciesType)(((int)MiniGameContext.Species + 4) % Enum.GetValues(typeof(SpeciesType)).Length);
            _friend = new MiniGameStage(playArea, new Vector2(0.12f, 0.9f), 120f, this, friendSpecies);
            _friend.React(EmotionType.Curiosity);
            _stage = new MiniGameStage(playArea, new Vector2(0.86f, 0.1f), 150f, this);

            var column = UIFactory.CreateRect("Quiz", playArea);
            UIFactory.Fill(column, 24f, 24f, 24f, 24f);
            UIFactory.AddVerticalLayout(column.gameObject, 18f, new RectOffset(0, 0, 0, 0));

            _progress = UIFactory.CreateText("Progress", column, "", 30, UIFactory.Muted);
            UIFactory.SetPreferredHeight(_progress.gameObject, 40f);

            var promptCard = UIFactory.CreateRounded("PromptCard", column, UIFactory.Cream, 1f);
            UIFactory.SetPreferredHeight(promptCard.gameObject, 240f);
            _prompt = UIFactory.CreateText("Prompt", promptCard.transform, "", 40, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(_prompt.rectTransform, 24f, 24f, 16f, 16f);
            if (SecondsPerQuestion > 0f)
            {
                var timer = UIFactory.CreatePillBar("Timer", promptCard.transform, UIFactory.Coral, out RectTransform fillRect);
                UIFactory.Place(timer.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 10f), new Vector2(-24f, 22f));
                _timerFill = fillRect.GetComponent<Image>();
            }

            _choiceButtons.Clear();
            for (int i = 0; i < 4; i++)
            {
                int choice = i;
                var button = UIFactory.CreateButton($"Choice{i}", column, "", UIFactory.Hex("F3EEF7"), () => OnChoice(choice), 32, this);
                UIFactory.SetPreferredHeight(button.gameObject, 110f);
                _choiceButtons.Add(button);
            }

            _index = 0;
            _correct = 0;
            _running = true;
            ShowQuestion();
        }

        public void Abort()
        {
            _running = false;
            StopAllCoroutines();
        }

        private void ShowQuestion()
        {
            _locked = false;
            _timeLeft = SecondsPerQuestion;
            var question = _bank[_order[_index]];
            _progress.text = $"Question {_index + 1} of {_order.Count}";
            _prompt.text = question.Prompt;
            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                bool visible = i < question.Choices.Length;
                _choiceButtons[i].gameObject.SetActive(visible);
                if (!visible) continue;
                _choiceButtons[i].image.color = UIFactory.Hex("F3EEF7");
                UIFactory.SetButtonLabel(_choiceButtons[i], question.Choices[i]);
            }
            StartCoroutine(SimpleTween.PopIn(_prompt.transform, 0.2f));
        }

        private void Update()
        {
            if (!_running || _locked || SecondsPerQuestion <= 0f) return;
            _timeLeft -= Time.deltaTime;
            if (_timerFill != null) _timerFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_timeLeft / SecondsPerQuestion), 1f);
            if (_timeLeft <= 0f) OnChoice(-1);
        }

        private void OnChoice(int choice)
        {
            if (!_running || _locked) return;
            _locked = true;
            var question = _bank[_order[_index]];
            bool right = choice == question.CorrectIndex;
            if (right) _correct++;
            _stage.React(right ? EmotionType.Joy : EmotionType.Embarrassment);
            _friend.React(right ? EmotionType.Joy : EmotionType.Worry);
            if (choice >= 0) StartCoroutine(SimpleTween.ColorTo(_choiceButtons[choice].image, right ? Correct : Wrong, 0.15f));
            if (!right) StartCoroutine(SimpleTween.ColorTo(_choiceButtons[question.CorrectIndex].image, Correct, 0.15f));
            StartCoroutine(Advance());
        }

        private IEnumerator Advance()
        {
            yield return new WaitForSeconds(0.75f);
            if (!_running) yield break;
            _index++;
            if (_index < _order.Count) ShowQuestion();
            else Finish();
        }

        private void Finish()
        {
            _running = false;
            int total = _order.Count;
            bool won = _correct >= Mathf.CeilToInt(total * 0.6f);
            OnCompleted?.Invoke(MiniGameRewards.Build(_branch, _correct * 20, won, $"{_correct} of {total} correct!", _difficulty));
        }
    }
}
