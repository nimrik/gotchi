using System;
using System.Collections;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.MiniGames
{
    // Warrior: a solo duel. The opponent telegraphs an attack; block in time, then strike while it's open.
    public class ArenaMiniGame : MonoBehaviour, IMiniGame
    {
        private const float BlockWindow = 0.4f;
        private const float StrikeWindow = 0.9f;

        private RectTransform _playArea;
        private MiniGameDifficulty _difficulty;
        private MiniGameStage _player;
        private MiniGameStage _opponent;
        private Image _telegraph;
        private Text _hud;
        private Text _banner;
        private Button _block;
        private Button _strike;
        private RectTransform _playerHearts, _opponentHearts;
        private int _playerHp, _opponentHp, _opponentMaxHp;
        private int _score;
        private bool _running, _armedBlock, _open;
        private float _blockArmedAt;

        public SkillBranch Branch => SkillBranch.Warrior;
        public event Action<MiniGameResult> OnCompleted;

        public void Begin(RectTransform playArea, MiniGameDifficulty difficulty)
        {
            _playArea = playArea;
            _difficulty = difficulty;
            Canvas.ForceUpdateCanvases();

            _hud = UIFactory.CreateText("HUD", playArea, "", 32, UIFactory.Ink, TextAnchor.UpperCenter, true);
            UIFactory.Place(_hud.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -82f), new Vector2(0f, -10f));
            _banner = UIFactory.CreateText("Banner", playArea, "", 46, UIFactory.PinkDark, TextAnchor.MiddleCenter, true);
            UIFactory.Place(_banner.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -170f), new Vector2(0f, -90f));

            var rivalSpecies = (SpeciesType)(((int)MiniGameContext.Species + 7) % Enum.GetValues(typeof(SpeciesType)).Length);
            _telegraph = UIFactory.CreateCircle("Telegraph", playArea, new Color(1f, 0.55f, 0.45f, 0.35f), 10f);
            UIFactory.Place(_telegraph.rectTransform, new Vector2(0.72f, 0.6f), new Vector2(0.72f, 0.6f), Vector2.zero, Vector2.zero);
            _telegraph.raycastTarget = false;
            _opponent = new MiniGameStage(playArea, new Vector2(0.72f, 0.6f), 200f, this, rivalSpecies);
            _opponent.React(EmotionType.Contempt);
            _player = new MiniGameStage(playArea, new Vector2(0.28f, 0.34f), 200f, this);

            _opponentHearts = Hearts(playArea, new Vector2(0.72f, 0.6f), 150f);
            _playerHearts = Hearts(playArea, new Vector2(0.28f, 0.34f), 150f);

            _block = UIFactory.CreateIconButton("Block", playArea, UIFactory.Sky, UIFactory.IconKind.Shield, 150f, OnBlock, this, true);
            UIFactory.Place((RectTransform)_block.transform, new Vector2(0.3f, 0f), new Vector2(0.3f, 0f), new Vector2(-75f, 40f), new Vector2(75f, 190f));
            _strike = UIFactory.CreateIconButton("Strike", playArea, UIFactory.Coral, UIFactory.IconKind.Sparkle, 150f, OnStrike, this, true);
            UIFactory.Place((RectTransform)_strike.transform, new Vector2(0.7f, 0f), new Vector2(0.7f, 0f), new Vector2(-75f, 40f), new Vector2(75f, 190f));
            Caption(_block.transform, "Block");
            Caption(_strike.transform, "Strike");

            _playerHp = 3;
            _opponentMaxHp = Mathf.Min(6, 3 + (_difficulty.Tier - 1) / 2);
            _opponentHp = _opponentMaxHp;
            _score = 0;
            _running = true;
            RefreshHud();
            StartCoroutine(Duel());
        }

        private static void Caption(Transform button, string text)
        {
            var caption = UIFactory.CreateText("Caption", button, text, 20, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-20f, -36f), new Vector2(20f, -4f));
        }

        private RectTransform Hearts(RectTransform playArea, Vector2 anchor, float yOffset)
        {
            var row = UIFactory.CreateRect("Hearts", playArea);
            UIFactory.Place(row, anchor, anchor, new Vector2(-120f, yOffset), new Vector2(120f, yOffset + 40f));
            UIFactory.AddHorizontalLayout(row.gameObject, 6f, new RectOffset(0, 0, 0, 0));
            return row;
        }

        private void DrawHearts(RectTransform row, int hp, int max)
        {
            for (int i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);
            for (int i = 0; i < max; i++)
            {
                var heart = UIFactory.CreateIcon(UIFactory.IconKind.Heart, row, 32f, Color.clear);
                var group = heart.gameObject.AddComponent<CanvasGroup>();
                group.alpha = i < hp ? 1f : 0.2f;
                UIFactory.SetWidth(heart.gameObject, 32f, 0f);
            }
        }

        public void Abort()
        {
            _running = false;
            StopAllCoroutines();
        }

        private IEnumerator Duel()
        {
            yield return Say("Ready…", 0.8f);
            while (_running)
            {
                float windup = Mathf.Lerp(1.1f, 0.55f, _difficulty.Intensity);
                _armedBlock = false;
                float elapsed = 0f;
                while (elapsed < windup && _running)
                {
                    elapsed += Time.deltaTime;
                    float d = Mathf.Lerp(40f, 320f, elapsed / windup);
                    _telegraph.rectTransform.sizeDelta = new Vector2(d, d);
                    yield return null;
                }
                _telegraph.rectTransform.sizeDelta = Vector2.zero;
                if (!_running) yield break;

                bool blocked = _armedBlock && Time.time - _blockArmedAt <= BlockWindow;
                bool perfect = blocked && Time.time - _blockArmedAt <= 0.18f;
                _opponent.LungeTo(-200f);
                if (blocked)
                {
                    _score += perfect ? 20 : 10;
                    _player.React(EmotionType.Pride);
                    _opponent.React(EmotionType.Shock);
                    StartCoroutine(SimpleTween.Flash(_block.image, Color.white, 0.2f));
                    yield return Say(perfect ? "Perfect block!" : "Blocked!", 0.35f);
                    _open = true;
                    StartCoroutine(SimpleTween.PunchScale(_strike.transform, 0.18f, 0.4f));
                    float open = 0f;
                    while (open < StrikeWindow && _open && _running) { open += Time.deltaTime; yield return null; }
                    _open = false;
                }
                else
                {
                    _playerHp--;
                    _player.React(EmotionType.Panic);
                    StartCoroutine(SimpleTween.Flash(_playerHearts.GetComponent<Graphic>() ?? _hud, UIFactory.Coral, 0.3f));
                    yield return Say("Ouch!", 0.5f);
                }
                RefreshHud();
                if (_playerHp <= 0 || _opponentHp <= 0) break;
                yield return new WaitForSeconds(0.4f);
            }
            if (_running) Finish();
        }

        private IEnumerator Say(string text, float seconds)
        {
            _banner.text = text;
            StartCoroutine(SimpleTween.PopIn(_banner.transform, 0.2f));
            yield return new WaitForSeconds(seconds);
            _banner.text = "";
        }

        private void OnBlock()
        {
            if (!_running) return;
            _armedBlock = true;
            _blockArmedAt = Time.time;
            StartCoroutine(SimpleTween.PunchScale(_player.Anchor, 0.1f, 0.2f));
        }

        private void OnStrike()
        {
            if (!_running || !_open) { if (_running) _player.React(EmotionType.Nervousness); return; }
            _open = false;
            _opponentHp--;
            _score += 30;
            _player.LungeTo(220f);
            _opponent.React(EmotionType.Grief);
            RefreshHud();
        }

        private void RefreshHud()
        {
            _hud.text = $"Score {_score}";
            DrawHearts(_playerHearts, _playerHp, 3);
            DrawHearts(_opponentHearts, _opponentHp, _opponentMaxHp);
        }

        private void Finish()
        {
            _running = false;
            bool won = _opponentHp <= 0;
            if (won) _score += _playerHp * 20;
            _player.React(won ? EmotionType.Pride : EmotionType.Sorrow);
            _opponent.React(won ? EmotionType.Humiliation : EmotionType.Pride);
            OnCompleted?.Invoke(MiniGameRewards.Build(Branch, _score, won, won ? "Champion of the rug!" : "Shake it off — try again.", _difficulty));
        }
    }
}
