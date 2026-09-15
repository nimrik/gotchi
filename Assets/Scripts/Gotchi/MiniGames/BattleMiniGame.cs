using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Creature;
using Gotchi.Data;
using Gotchi.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.MiniGames
{
    // PvP branch: a turn-based battle in the Pokémon Sapphire mould. FIGHT / ITEM / CHEER / RUN, four moves
    // with PP, three elemental types plus Normal, speed order, STAB, critical hits, stat stages and a narrated
    // text box. The rival is another player's pet (mock roster) played by a simple AI.
    public class BattleMiniGame : MonoBehaviour, IMiniGame
    {
        private enum BattleType { Normal, Fire, Water, Grass }
        private enum MoveEffect { Damage, LowerAttack }
        private enum Choice { Fight, Item, Cheer }

        private class Move
        {
            public string Name; public BattleType Type; public int Power; public int Accuracy; public int MaxPp; public int Pp; public MoveEffect Effect;
        }

        private class Fighter
        {
            public string Name; public int Level; public BattleType Type;
            public int MaxHp, Hp, Attack, Defense, Speed, AttackStage;
            public Move[] Moves; public MiniGameStage Stage; public CanvasGroup Group;
            public RectTransform HpFill; public Image HpImage; public Text HpText;
            public float AttackMultiplier => AttackStage >= 0 ? (2f + AttackStage) / 2f : 2f / (2f - AttackStage);
        }

        private static readonly Dictionary<SpeciesType, BattleType> SpeciesTypes = new Dictionary<SpeciesType, BattleType>
        {
            { SpeciesType.Cat, BattleType.Fire }, { SpeciesType.Fox, BattleType.Fire }, { SpeciesType.Fennec, BattleType.Fire }, { SpeciesType.RedPanda, BattleType.Fire },
            { SpeciesType.Seal, BattleType.Water }, { SpeciesType.Otter, BattleType.Water }, { SpeciesType.Penguin, BattleType.Water },
            { SpeciesType.Bunny, BattleType.Grass }, { SpeciesType.Panda, BattleType.Grass }, { SpeciesType.Pig, BattleType.Grass }, { SpeciesType.Hedgehog, BattleType.Grass },
            { SpeciesType.Raccoon, BattleType.Normal }, { SpeciesType.Dog, BattleType.Normal },
        };

        private static readonly Dictionary<BattleType, string> TypeMoveNames = new Dictionary<BattleType, string>
        {
            { BattleType.Normal, "QUICK ATTACK" }, { BattleType.Fire, "EMBER" }, { BattleType.Water, "BUBBLE" }, { BattleType.Grass, "VINE WHIP" },
        };

        private static readonly Color HpGreen = UIFactory.Hex("58D080"), HpYellow = UIFactory.Hex("F8D048"), HpRed = UIFactory.Hex("F06070");

        private MiniGameDifficulty _difficulty;
        private DialogBoxView _dialog;
        private RectTransform _actionBox, _moveBox, _infoBox;
        private readonly Button[] _moveButtons = new Button[4];
        private readonly Image[] _moveCursors = new Image[4];
        private Text _ppText, _typeText;
        private Fighter _player, _rival;
        private int _treats = 2;
        private int _selectedMove = -1;
        private int _damageDealt;
        private bool _running, _busy;

        public SkillBranch Branch => SkillBranch.PvP;
        public event Action<MiniGameResult> OnCompleted;

        public void Begin(RectTransform playArea, MiniGameDifficulty difficulty)
        {
            _difficulty = difficulty;
            Canvas.ForceUpdateCanvases();
            int level = Mathf.Max(1, MiniGameContext.Level);
            int rivalLevel = Mathf.Max(1, level + difficulty.Tier - 1);

            Platform(playArea, new Vector2(0.72f, 0.60f), 380f);
            Platform(playArea, new Vector2(0.28f, 0.28f), 440f);
            var rivalStage = new MiniGameStage(playArea, new Vector2(0.72f, 0.66f), 190f, this, MiniGameContext.RivalSpecies);
            var playerStage = new MiniGameStage(playArea, new Vector2(0.28f, 0.36f), 220f, this);
            _rival = MakeFighter(MiniGameContext.RivalName, MiniGameContext.RivalSpecies, rivalLevel, rivalStage);
            _player = MakeFighter(MiniGameContext.PetName, MiniGameContext.Species, level, playerStage);
            _rival.Speed += UnityEngine.Random.Range(-2, 3);
            _rival.Stage.React(EmotionType.Contempt);
            _rival.Stage.Face(-1f);

            StatusBox(playArea, _rival, new Vector2(0.04f, 0.81f), new Vector2(0.50f, 0.90f), false);
            StatusBox(playArea, _player, new Vector2(0.50f, 0.43f), new Vector2(0.96f, 0.54f), true);

            _dialog = new DialogBoxView(playArea, this, 28) { AutoAdvanceSeconds = 1.1f };
            UIFactory.Place(_dialog.Root, new Vector2(0.02f, 0.03f), new Vector2(0.58f, 0.25f), Vector2.zero, Vector2.zero);
            BuildActionBox(playArea);
            BuildMoveBox(playArea);
            HideMenus();

            _running = true;
            _damageDealt = 0;
            StartCoroutine(Intro());
        }

        public void Abort()
        {
            _running = false;
            StopAllCoroutines();
            if (_dialog != null) _dialog.Hide();
        }

        // ---------- setup ----------

        private static void Platform(RectTransform playArea, Vector2 anchor, float width)
        {
            var disc = UIFactory.CreateCircle("Platform", playArea, UIFactory.Hex("DFF3E4"), width);
            UIFactory.Place(disc.rectTransform, anchor, anchor, new Vector2(-width / 2f, -width / 2f), new Vector2(width / 2f, width / 2f));
            disc.rectTransform.localScale = new Vector3(1f, 0.32f, 1f);
            disc.raycastTarget = false;
        }

        private static Fighter MakeFighter(string name, SpeciesType species, int level, MiniGameStage stage)
        {
            var type = SpeciesTypes.TryGetValue(species, out var t) ? t : BattleType.Normal;
            var fighter = new Fighter
            {
                Name = name.ToUpperInvariant(), Level = level, Type = type, Stage = stage,
                MaxHp = 24 + level * 3, Attack = 10 + level * 2, Defense = 9 + level * 2, Speed = 8 + level * 2,
                Moves = new[]
                {
                    new Move { Name = "TACKLE", Type = BattleType.Normal, Power = 40, Accuracy = 100, MaxPp = 35, Pp = 35 },
                    new Move { Name = TypeMoveNames[type], Type = type, Power = 50, Accuracy = 100, MaxPp = 25, Pp = 25 },
                    new Move { Name = "HEADBUTT", Type = BattleType.Normal, Power = 65, Accuracy = 85, MaxPp = 15, Pp = 15 },
                    new Move { Name = "GROWL", Type = BattleType.Normal, Power = 0, Accuracy = 100, MaxPp = 40, Pp = 40, Effect = MoveEffect.LowerAttack },
                },
            };
            fighter.Hp = fighter.MaxHp;
            fighter.Group = stage.Anchor.gameObject.AddComponent<CanvasGroup>();
            return fighter;
        }

        private void StatusBox(RectTransform playArea, Fighter fighter, Vector2 min, Vector2 max, bool showNumbers)
        {
            var box = UIFactory.CreateFrame("Status", playArea, Color.white);
            box.raycastTarget = false;
            UIFactory.Place(box.rectTransform, min, max, Vector2.zero, Vector2.zero);
            var name = UIFactory.CreatePixelText("Name", box.transform, fighter.Name, 22, UIFactory.MenuInk, TextAnchor.MiddleLeft);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0.7f, 1f), new Vector2(22f, -54f), new Vector2(0f, -16f));
            var level = UIFactory.CreatePixelText("Level", box.transform, "Lv" + fighter.Level, 20, UIFactory.MenuInk, TextAnchor.MiddleRight);
            UIFactory.Place(level.rectTransform, new Vector2(0.7f, 1f), new Vector2(1f, 1f), new Vector2(0f, -54f), new Vector2(-22f, -16f));
            var hpLabel = UIFactory.CreatePixelText("HpLabel", box.transform, "HP", 16, UIFactory.PinkDark, TextAnchor.MiddleLeft);
            UIFactory.Place(hpLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -84f), new Vector2(62f, -58f));
            var bar = UIFactory.CreatePillBar("Hp", box.transform, HpGreen, out fighter.HpFill);
            UIFactory.Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(62f, -82f), new Vector2(-22f, -60f));
            bar.raycastTarget = false;
            fighter.HpImage = fighter.HpFill.GetComponent<Image>();
            if (showNumbers)
            {
                fighter.HpText = UIFactory.CreatePixelText("HpText", box.transform, "", 20, UIFactory.MenuInk, TextAnchor.MiddleRight);
                UIFactory.Place(fighter.HpText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -122f), new Vector2(-22f, -88f));
            }
            RefreshHp(fighter);
        }

        private void BuildActionBox(RectTransform playArea)
        {
            var box = UIFactory.CreateFrame("Actions", playArea, Color.white);
            box.raycastTarget = false;
            UIFactory.Place(box.rectTransform, new Vector2(0.60f, 0.03f), new Vector2(0.98f, 0.25f), Vector2.zero, Vector2.zero);
            _actionBox = box.rectTransform;
            MenuButton(box.transform, "Fight", 0, 0, OnFight);
            MenuButton(box.transform, "Item", 1, 0, OnItem);
            MenuButton(box.transform, "Cheer", 0, 1, OnCheer);
            MenuButton(box.transform, "Run", 1, 1, OnRun);
        }

        private Button MenuButton(Transform parent, string label, int column, int row, Action onClick)
        {
            var button = UIFactory.CreateButton(label, parent, label, UIFactory.Card, onClick, 24, this);
            float x0 = column == 0 ? 0f : 0.5f, x1 = column == 0 ? 0.5f : 1f;
            float y0 = row == 0 ? 0.5f : 0f, y1 = row == 0 ? 1f : 0.5f;
            UIFactory.Place((RectTransform)button.transform, new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(column == 0 ? 14f : 4f, row == 0 ? 4f : 14f), new Vector2(column == 0 ? -4f : -14f, row == 0 ? -14f : -4f));
            return button;
        }

        private void BuildMoveBox(RectTransform playArea)
        {
            var box = UIFactory.CreateFrame("Moves", playArea, Color.white);
            box.raycastTarget = false;
            UIFactory.Place(box.rectTransform, new Vector2(0.02f, 0.03f), new Vector2(0.64f, 0.25f), Vector2.zero, Vector2.zero);
            _moveBox = box.rectTransform;
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                _moveButtons[i] = MenuButton(box.transform, _player.Moves[i].Name, i % 2, i / 2, () => SelectMove(index));
                _moveCursors[i] = UIFactory.CreateCursor(_moveButtons[i].transform, 16f, UIFactory.PinkDark);
                UIFactory.Place(_moveCursors[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -8f), new Vector2(34f, 8f));
                _moveCursors[i].enabled = false;
            }

            var info = UIFactory.CreateFrame("Info", playArea, Color.white);
            info.raycastTarget = false;
            UIFactory.Place(info.rectTransform, new Vector2(0.66f, 0.03f), new Vector2(0.98f, 0.25f), Vector2.zero, Vector2.zero);
            _infoBox = info.rectTransform;
            _ppText = UIFactory.CreatePixelText("Pp", info.transform, "", 22, UIFactory.MenuInk, TextAnchor.MiddleLeft);
            UIFactory.Place(_ppText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -60f), new Vector2(-18f, -18f));
            _typeText = UIFactory.CreatePixelText("Type", info.transform, "", 20, UIFactory.MenuInk, TextAnchor.MiddleLeft);
            UIFactory.Place(_typeText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -100f), new Vector2(-18f, -62f));
            var back = UIFactory.CreateButton("Back", info.transform, "Back", UIFactory.Card, () => { if (!_busy) ShowMenu(); }, 22, this);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 14f), new Vector2(-14f, 74f));
        }

        // ---------- menus ----------

        private void HideMenus()
        {
            _actionBox.gameObject.SetActive(false);
            _moveBox.gameObject.SetActive(false);
            _infoBox.gameObject.SetActive(false);
        }

        private void ShowMenu()
        {
            if (!_running) return;
            _busy = false;
            _selectedMove = -1;
            _dialog.Prompt($"What will {_player.Name} do?");
            _moveBox.gameObject.SetActive(false);
            _infoBox.gameObject.SetActive(false);
            _actionBox.gameObject.SetActive(true);
            StartCoroutine(SimpleTween.PopIn(_actionBox, 0.15f));
        }

        private void OnFight()
        {
            if (_busy) return;
            _actionBox.gameObject.SetActive(false);
            _moveBox.gameObject.SetActive(true);
            _infoBox.gameObject.SetActive(true);
            for (int i = 0; i < 4; i++) _moveButtons[i].interactable = _player.Moves[i].Pp > 0;
            int first = 0;
            while (first < 3 && _player.Moves[first].Pp <= 0) first++;
            _selectedMove = -1;
            SelectMove(first);
        }

        // First tap moves the cursor and shows PP / type; a second tap on the same move uses it.
        private void SelectMove(int index)
        {
            if (_busy) return;
            var move = _player.Moves[index];
            if (_selectedMove == index)
            {
                if (move.Pp <= 0) return;
                StartCoroutine(Turn(Choice.Fight, move));
                return;
            }
            _selectedMove = index;
            for (int i = 0; i < 4; i++) _moveCursors[i].enabled = i == index;
            _ppText.text = $"PP {move.Pp}/{move.MaxPp}";
            _typeText.text = "TYPE/" + move.Type.ToString().ToUpperInvariant();
            StartCoroutine(SimpleTween.PunchScale(_moveButtons[index].transform, 0.04f, 0.15f));
        }

        private void OnItem()
        {
            if (_busy) return;
            if (_treats <= 0) { StartCoroutine(Notice("No treats left!")); return; }
            if (_player.Hp >= _player.MaxHp) { StartCoroutine(Notice($"{_player.Name} is already full of energy.")); return; }
            StartCoroutine(Turn(Choice.Item, null));
        }

        private void OnCheer()
        {
            if (_busy) return;
            StartCoroutine(Turn(Choice.Cheer, null));
        }

        private void OnRun()
        {
            if (_busy) return;
            StartCoroutine(RunAway());
        }

        private IEnumerator Notice(string text)
        {
            _busy = true;
            _actionBox.gameObject.SetActive(false);
            yield return Line(text);
            ShowMenu();
        }

        private IEnumerator RunAway()
        {
            _busy = true;
            HideMenus();
            _player.Stage.React(EmotionType.Anxiety);
            yield return Line($"{_player.Name} ran away...");
            yield return Outro(false, "Next time, stand your ground!");
        }

        // ---------- battle flow ----------

        private IEnumerator Intro()
        {
            _player.Group.alpha = 0f;
            yield return Line($"{_rival.Name} wants to battle!");
            yield return Line($"Go, {_player.Name}!");
            _player.Group.alpha = 1f;
            StartCoroutine(SimpleTween.PopIn(_player.Stage.Anchor, 0.3f));
            _player.Stage.React(EmotionType.Excitement);
            yield return new WaitForSeconds(0.4f);
            ShowMenu();
        }

        private IEnumerator Line(string text)
        {
            bool done = false;
            _dialog.Say(text, () => done = true);
            while (!done && _running) yield return null;
        }

        private IEnumerator Turn(Choice choice, Move playerMove)
        {
            _busy = true;
            HideMenus();
            Move rivalMove = ChooseRivalMove();

            if (choice == Choice.Item)
            {
                _treats--;
                int heal = Mathf.Min(_player.MaxHp - _player.Hp, Mathf.RoundToInt(_player.MaxHp * 0.4f));
                yield return Line($"{_player.Name} ate a treat!");
                _player.Stage.React(EmotionType.Satisfaction);
                yield return DrainHp(_player, _player.Hp + heal);
                yield return Line($"{_player.Name} recovered {heal} HP! ({_treats} left)");
            }
            else if (choice == Choice.Cheer)
            {
                _player.AttackStage = Mathf.Min(6, _player.AttackStage + 1);
                _player.Stage.React(EmotionType.Pride);
                StartCoroutine(SimpleTween.PunchScale(_player.Stage.Anchor, 0.15f, 0.3f));
                yield return Line($"You cheered! {_player.Name}'s ATTACK rose!");
            }

            if (choice != Choice.Fight)
            {
                yield return Execute(_rival, _player, rivalMove);
                if (_player.Hp <= 0) { yield return Faint(_player); yield return Outro(false, "A close one. Train and try again!"); yield break; }
                ShowMenu();
                yield break;
            }

            bool playerFirst = _player.Speed >= _rival.Speed;
            var order = playerFirst
                ? new[] { (_player, _rival, playerMove), (_rival, _player, rivalMove) }
                : new[] { (_rival, _player, rivalMove), (_player, _rival, playerMove) };
            foreach (var (attacker, defender, move) in order)
            {
                if (attacker.Hp <= 0 || !_running) continue;
                yield return Execute(attacker, defender, move);
                if (defender.Hp <= 0)
                {
                    yield return Faint(defender);
                    bool won = defender == _rival;
                    yield return Outro(won, won ? "Champion of the rug!" : "A close one. Train and try again!");
                    yield break;
                }
            }
            ShowMenu();
        }

        private Move ChooseRivalMove()
        {
            var usable = new List<Move>();
            foreach (var move in _rival.Moves) if (move.Pp > 0) usable.Add(move);
            if (usable.Count == 0) return new Move { Name = "STRUGGLE", Type = BattleType.Normal, Power = 30, Accuracy = 100, MaxPp = 1, Pp = 1 };
            if (_difficulty.Tier >= 3 && UnityEngine.Random.value < 0.7f)
            {
                Move best = null;
                float bestScore = -1f;
                foreach (var move in usable)
                {
                    if (move.Effect != MoveEffect.Damage) continue;
                    float score = move.Power * Effectiveness(move.Type, _player.Type) * (move.Type == _rival.Type ? 1.5f : 1f) * move.Accuracy / 100f;
                    if (score > bestScore) { bestScore = score; best = move; }
                }
                if (best != null) return best;
            }
            return usable[UnityEngine.Random.Range(0, usable.Count)];
        }

        private static float Effectiveness(BattleType attack, BattleType defend)
        {
            if (attack == BattleType.Normal || defend == BattleType.Normal) return 1f;
            if ((attack == BattleType.Fire && defend == BattleType.Grass) || (attack == BattleType.Water && defend == BattleType.Fire) || (attack == BattleType.Grass && defend == BattleType.Water)) return 2f;
            if ((attack == BattleType.Fire && defend == BattleType.Water) || (attack == BattleType.Water && defend == BattleType.Grass) || (attack == BattleType.Grass && defend == BattleType.Fire)) return 0.5f;
            return 1f;
        }

        private IEnumerator Execute(Fighter attacker, Fighter defender, Move move)
        {
            move.Pp = Mathf.Max(0, move.Pp - 1);
            yield return Line($"{attacker.Name} used {move.Name}!");
            attacker.Stage.React(EmotionType.Excitement);
            attacker.Stage.LungeTo(attacker == _player ? 220f : -220f);
            yield return new WaitForSeconds(0.2f);

            if (UnityEngine.Random.Range(0, 100) >= move.Accuracy)
            {
                defender.Stage.React(EmotionType.Relief);
                yield return Line($"{attacker.Name}'s attack missed!");
                yield break;
            }

            if (move.Effect == MoveEffect.LowerAttack)
            {
                if (defender.AttackStage <= -6) { yield return Line($"{defender.Name}'s ATTACK won't go lower!"); yield break; }
                defender.AttackStage--;
                defender.Stage.React(EmotionType.Irritation);
                StartCoroutine(Shake(defender));
                yield return Line($"{defender.Name}'s ATTACK fell!");
                yield break;
            }

            bool critical = UnityEngine.Random.Range(0, 16) == 0;
            float effectiveness = Effectiveness(move.Type, defender.Type);
            float attack = attacker.Attack * attacker.AttackMultiplier;
            float baseDamage = ((2f * attacker.Level / 5f + 2f) * move.Power * attack / defender.Defense) / 25f + 2f;
            float modifier = (move.Type == attacker.Type ? 1.5f : 1f) * effectiveness * (critical ? 2f : 1f) * UnityEngine.Random.Range(0.85f, 1f);
            int damage = Mathf.Max(1, Mathf.FloorToInt(baseDamage * modifier));
            if (attacker == _player) _damageDealt += damage;

            defender.Stage.React(damage >= defender.Hp ? EmotionType.Shock : EmotionType.Panic);
            defender.Stage.Play(OneShot.Hurt, defender == _player ? 1f : -1f);
            StartCoroutine(Shake(defender));
            yield return DrainHp(defender, defender.Hp - damage);
            if (critical) yield return Line("A critical hit!");
            if (effectiveness > 1f) yield return Line("It's super effective!");
            else if (effectiveness < 1f) yield return Line("It's not very effective...");
        }

        private IEnumerator Faint(Fighter fighter)
        {
            fighter.Stage.React(EmotionType.Grief);
            fighter.Stage.Play(OneShot.Faint, fighter == _player ? 1f : -1f);
            yield return Line($"{fighter.Name} fainted!");
            var anchor = fighter.Stage.Anchor;
            Vector2 start = anchor.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.5f);
                anchor.anchoredPosition = start + new Vector2(0f, -40f * t);
                fighter.Group.alpha = 1f - t * 0.7f;
                yield return null;
            }
        }

        private IEnumerator Outro(bool won, string message)
        {
            _running = false;
            if (won)
            {
                _player.Stage.React(EmotionType.Pride);
                StartCoroutine(SimpleTween.Hop(_player.Stage.Anchor, 40f, 0.5f));
                yield return Line($"{_player.Name} won the battle!");
            }
            else if (_player.Hp > 0) _player.Stage.React(EmotionType.Embarrassment);
            int score = _damageDealt + (won ? 100 + _player.Hp * 4 : 0);
            OnCompleted?.Invoke(MiniGameRewards.Build(Branch, score, won, message, _difficulty));
        }

        // ---------- presentation ----------

        private IEnumerator DrainHp(Fighter fighter, int target)
        {
            target = Mathf.Clamp(target, 0, fighter.MaxHp);
            int start = fighter.Hp;
            float elapsed = 0f;
            const float duration = 0.6f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fighter.Hp = Mathf.RoundToInt(Mathf.Lerp(start, target, SimpleTween.EaseOutCubic(Mathf.Clamp01(elapsed / duration))));
                RefreshHp(fighter);
                yield return null;
            }
            fighter.Hp = target;
            RefreshHp(fighter);
        }

        private static void RefreshHp(Fighter fighter)
        {
            float ratio = fighter.MaxHp > 0 ? (float)fighter.Hp / fighter.MaxHp : 0f;
            fighter.HpFill.anchorMax = new Vector2(ratio, 1f);
            fighter.HpImage.color = ratio > 0.5f ? HpGreen : ratio > 0.2f ? HpYellow : HpRed;
            if (fighter.HpText != null) fighter.HpText.text = $"{fighter.Hp}/{fighter.MaxHp}";
        }

        private IEnumerator Shake(Fighter fighter)
        {
            var anchor = fighter.Stage.Anchor;
            Vector2 start = anchor.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < 0.32f)
            {
                elapsed += Time.deltaTime;
                anchor.anchoredPosition = start + new Vector2(Mathf.Sin(elapsed * 60f) * 10f * (1f - elapsed / 0.32f), 0f);
                yield return null;
            }
            anchor.anchoredPosition = start;
        }
    }
}
