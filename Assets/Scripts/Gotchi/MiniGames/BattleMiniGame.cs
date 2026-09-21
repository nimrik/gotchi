using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Creature;
using Gotchi.Creature3D;
using Gotchi.Data;
using Gotchi.Systems;
using Gotchi.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.MiniGames
{
    // The fight, staged for the Battle Club (ranked) and for the Wild (campaign encounters): a turn-based battle in
    // the classic handheld monster-battler mould, with our own cast, moves and wording. FIGHT / ITEM / CHEER / RUN,
    // four moves that spend MANA (the basic SCRATCH is free), three fighting styles plus Normal, speed order with priority moves, own-style bonus, critical
    // hits, stat stages, held charms and a narrated text box. Rules, stats, moves and rewards come from
    // Systems/BattleSystem.cs (design: 13-pvp-design.md); this class is the staging and the turn loop.
    //
    // Staging follows the handheld classics: the player's cat stands in the foreground seen FROM BEHIND, looking up
    // the field at its rival, mouth shut; the rival (another player's cat in its own coat) faces the camera.
    public class BattleMiniGame : MonoBehaviour, IMiniGame
    {
        private enum Choice { Fight, Item, Cheer }

        // Battle animation cues. The dedicated clips (guard, attack, scream, heal, defeated, lightly wounded) are
        // REQUIRED but not authored yet — see "Required animations" in 13-pvp-design.md. Until they exist every cue
        // plays the closest clip the cat already has, so swapping them in later is a change to PlayCue alone.
        private enum Cue { Attack, Defensive, Scream, Heal, Defeated, WoundedLightly }

        private class Move
        {
            public BattleMoveDef Def;
        }

        private class Fighter
        {
            public string Name; public int Level; public BattleStyle Style;
            public int MaxHp, Hp, MaxMp, Mp, Attack, Defense, Speed;
            public int AttackStage, DefenseStage, SpeedStage;
            public float CritChance;
            public BattleCharmDef Charm; public bool EndureUsed;
            public bool SmartAi;
            public Move[] Moves; public MiniGameStage Stage; public CanvasGroup Group;
            public SegmentedBar HpBar, MpBar; public Text HpText, MpText;
            public bool CanUse(Move move) => Mp >= move.Def.Mana;
            public float EffectiveSpeed => Speed * BattleSystem.StageMultiplier(SpeedStage);
        }

        // Bar colours, shared with the home screen and the club: health rose (deep red when nearly gone), mana violet.
        public static readonly Color HpFill = UIFactory.Hex("FF5C7A"), HpLine = UIFactory.Hex("B8324E"), HpLowFill = UIFactory.Hex("E02D4F"), HpLowLine = UIFactory.Hex("8F1C33");
        public static readonly Color MpFill = UIFactory.Hex("8E7BFF"), MpLine = UIFactory.Hex("5B49C9");

        public static Color StyleColor(BattleStyle style)
        {
            switch (style)
            {
                case BattleStyle.Claw: return UIFactory.Coral;
                case BattleStyle.Fluff: return UIFactory.Mint;
                case BattleStyle.Trick: return UIFactory.Lavender;
                default: return UIFactory.MenuGrey;
            }
        }

        private MiniGameDifficulty _difficulty;
        private BattleSystem _battle;
        private BattleEncounter _encounter;      // null = a ranked Battle Club fight
        private DialogBoxView _dialog;
        private RectTransform _actionBox, _moveBox, _itemBox, _infoBox;
        private readonly Button[] _moveButtons = new Button[4];
        private readonly Image[] _moveCursors = new Image[4];
        private readonly Button[] _itemButtons = new Button[4];
        private readonly Image[] _itemCursors = new Image[4];
        private Text _infoTitle, _infoBody;
        private Fighter _player, _rival;
        private readonly Dictionary<string, int> _fallbackItems = new Dictionary<string, int> { { "treat", 2 } };
        private int _selectedMove = -1, _selectedItem = -1;
        private readonly BattleReport _report = new BattleReport();
        private bool _running, _busy;

        public SkillBranch Branch => SkillBranch.PvP;
        public event Action<MiniGameResult> OnCompleted;

        // Dev/QA: the player's side plays itself (strongest usable move, a treat when low), so a capture run can
        // watch a whole battle through to the results card.
        public static bool AutoPlay;

        public void Begin(RectTransform playArea, MiniGameDifficulty difficulty)
        {
            _difficulty = difficulty;
            _battle = MiniGameContext.Battle;
            _encounter = MiniGameContext.Encounter;
            MiniGameContext.Encounter = null;        // one fight per encounter; "Next battle" is always a ranked one
            Canvas.ForceUpdateCanvases();

            BattleFighterSetup playerSetup, rivalSetup;
            if (_battle != null)
            {
                playerSetup = _battle.PlayerSetup(MiniGameContext.PetName);
                rivalSetup = _encounter != null ? _encounter.Rival : _battle.NextRival();
            }
            else
            {
                int level = Mathf.Max(1, MiniGameContext.Level);
                playerSetup = DefaultSetup(MiniGameContext.PetName, level, BattleStyle.Claw, "claw_swipe", "");
                rivalSetup = DefaultSetup("Rival", level, BattleStyle.Fluff, "fluff_bump", "ginger");
            }

            BuildArena(playArea, _encounter != null && _encounter.Arena != null ? _encounter.Arena : _battle != null ? _battle.Arena : BattleSystem.Arenas[0]);
            // Rival up the field, facing us; our cat in the foreground, bigger, seen from behind, mouth shut.
            var rivalStage = new MiniGameStage(playArea, new Vector2(0.72f, 0.66f), 190f, this, rivalSetup.Species, CatCoat.Find(rivalSetup.CoatId));
            var playerStage = new MiniGameStage(playArea, new Vector2(0.27f, 0.405f), 340f, this, SpeciesType.Cat);
            rivalStage.Pet.StageForBattle(-1f, false, false);
            playerStage.Pet.StageForBattle(1f, true, true);
            _rival = MakeFighter(rivalSetup, rivalStage);
            _player = MakeFighter(playerSetup, playerStage);
            if (_battle != null)
            {
                // The cat walks in with the health and mana it has: what the last fight left, plus the rest since.
                _player.Hp = Mathf.Clamp(_battle.Hp, 1, _player.MaxHp);   // the club does not start a fight under a tenth of health
                _player.Mp = Mathf.Clamp(_battle.Mp, 0, _player.MaxMp);
            }
            _rival.Stage.React(EmotionType.Contempt);

            StatusBox(playArea, _rival, new Vector2(0.04f, 0.81f), new Vector2(0.50f, 0.90f), false);
            StatusBox(playArea, _player, new Vector2(0.50f, 0.43f), new Vector2(0.96f, 0.54f), true);

            _dialog = new DialogBoxView(playArea, this, 28) { AutoAdvanceSeconds = 1.1f };
            UIFactory.Place(_dialog.Root, new Vector2(0.02f, 0.03f), new Vector2(0.58f, 0.25f), Vector2.zero, Vector2.zero);
            BuildActionBox(playArea);
            BuildMoveBox(playArea);
            BuildItemBox(playArea);
            BuildInfoBox(playArea);
            HideMenus();

            _running = true;
            StartCoroutine(Intro(rivalSetup));
        }

        public void Abort()
        {
            _running = false;
            StopAllCoroutines();
            if (_dialog != null) _dialog.Hide();
        }

        // ---------- setup ----------

        private static BattleFighterSetup DefaultSetup(string name, int level, BattleStyle style, string styleMove, string coat)
        {
            var setup = new BattleFighterSetup { Name = name, Level = level, Style = style, Stats = BattleSystem.BaseStats(level), CoatId = coat };
            setup.MoveIds.AddRange(new[] { "scratch", styleMove, "pounce", "hiss" });
            return setup;
        }

        private static void BuildArena(RectTransform playArea, ArenaDef arena)
        {
            var sky = UIFactory.CreatePanel("ArenaSky", playArea, UIFactory.Hex(arena.SkyBottom));
            UIFactory.Fill(sky.rectTransform);
            sky.raycastTarget = false;
            var glow = UIFactory.CreateGradient("ArenaGlow", sky.transform, UIFactory.Hex(arena.SkyTop));
            UIFactory.Place(glow.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            glow.raycastTarget = false;
            var ground = UIFactory.CreatePanel("ArenaGround", sky.transform, UIFactory.Hex(arena.Ground));
            UIFactory.Place(ground.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.56f), Vector2.zero, Vector2.zero);
            ground.raycastTarget = false;
            Platform(playArea, new Vector2(0.72f, 0.60f), 380f, UIFactory.Hex(arena.Platform));
            Platform(playArea, new Vector2(0.27f, 0.30f), 600f, UIFactory.Hex(arena.Platform));
        }

        private static void Platform(RectTransform playArea, Vector2 anchor, float width, Color color)
        {
            var disc = UIFactory.CreateCircle("Platform", playArea, color, width);
            UIFactory.Place(disc.rectTransform, anchor, anchor, new Vector2(-width / 2f, -width / 2f), new Vector2(width / 2f, width / 2f));
            disc.rectTransform.localScale = new Vector3(1f, 0.32f, 1f);
            disc.raycastTarget = false;
        }

        private static Fighter MakeFighter(BattleFighterSetup setup, MiniGameStage stage)
        {
            var moves = new List<Move>();
            foreach (string id in setup.MoveIds)
            {
                var def = BattleSystem.FindMove(id);
                if (def != null && moves.Count < BattleSystem.LoadoutSize) moves.Add(new Move { Def = def });
            }
            if (moves.Count == 0) moves.Add(new Move { Def = BattleSystem.FindMove("scratch") });
            var fighter = new Fighter
            {
                Name = (setup.Name ?? "Cat").ToUpperInvariant(), Level = setup.Level, Style = setup.Style, Stage = stage,
                MaxHp = setup.Stats.MaxHp, MaxMp = Mathf.Max(1, setup.Stats.MaxMp), Attack = setup.Stats.Attack, Defense = setup.Stats.Defense, Speed = setup.Stats.Speed,
                CritChance = setup.Stats.CritChance > 0f ? setup.Stats.CritChance : BattleSystem.BaseCrit,
                Charm = BattleSystem.FindCharm(setup.CharmId), SmartAi = setup.SmartAi, Moves = moves.ToArray(),
            };
            fighter.Hp = fighter.MaxHp;
            fighter.Mp = fighter.MaxMp;
            fighter.Group = stage.Anchor.gameObject.AddComponent<CanvasGroup>();
            return fighter;
        }

        private void StatusBox(RectTransform playArea, Fighter fighter, Vector2 min, Vector2 max, bool showNumbers)
        {
            var box = UIFactory.CreateFrame("Status", playArea, Color.white);
            box.raycastTarget = false;
            UIFactory.Place(box.rectTransform, min, max, Vector2.zero, Vector2.zero);
            var name = UIFactory.CreatePixelText("Name", box.transform, fighter.Name, 22, UIFactory.MenuInk, TextAnchor.MiddleLeft);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(22f, -54f), new Vector2(0f, -16f));
            var level = UIFactory.CreatePixelText("Level", box.transform, "Lv" + fighter.Level, 20, UIFactory.MenuInk, TextAnchor.MiddleRight);
            UIFactory.Place(level.rectTransform, new Vector2(0.7f, 1f), new Vector2(1f, 1f), new Vector2(0f, -54f), new Vector2(-22f, -16f));
            // Style chip between the name and the level: the colour is the type, as everywhere in the club.
            var chip = UIFactory.CreatePill("Style", box.transform, StyleColor(fighter.Style));
            chip.raycastTarget = false;
            UIFactory.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-10f, -52f), new Vector2(86f, -18f));
            var chipText = UIFactory.CreatePixelText("Text", chip.transform, fighter.Style.ToString().ToUpperInvariant(), 18, UIFactory.MenuInk, TextAnchor.MiddleCenter, false);
            UIFactory.Fill(chipText.rectTransform);
            float numbers = showNumbers ? 150f : 0f;   // room for "1000/1200" right of the bars
            var hpLabel = UIFactory.CreatePixelText("HpLabel", box.transform, "HP", 16, HpLine, TextAnchor.MiddleLeft);
            UIFactory.Place(hpLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -84f), new Vector2(62f, -58f));
            fighter.HpBar = SegmentedBar.Create("Hp", box.transform, HpFill, HpLine);
            UIFactory.Place(fighter.HpBar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(62f, -82f), new Vector2(-22f - numbers, -60f));
            if (showNumbers)
            {
                fighter.HpText = UIFactory.CreatePixelText("HpText", box.transform, "", 20, UIFactory.MenuInk, TextAnchor.MiddleRight);
                fighter.HpText.horizontalOverflow = HorizontalWrapMode.Overflow;
                UIFactory.Place(fighter.HpText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f - numbers, -86f), new Vector2(-22f, -56f));
                // Mana, under the health bar: what the moves are paid with.
                var mpLabel = UIFactory.CreatePixelText("MpLabel", box.transform, "MP", 16, MpLine, TextAnchor.MiddleLeft);
                UIFactory.Place(mpLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -118f), new Vector2(62f, -92f));
                fighter.MpBar = SegmentedBar.Create("Mp", box.transform, MpFill, MpLine);
                UIFactory.Place(fighter.MpBar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(62f, -116f), new Vector2(-22f - numbers, -94f));
                fighter.MpText = UIFactory.CreatePixelText("MpText", box.transform, "", 20, UIFactory.MenuInk, TextAnchor.MiddleRight);
                fighter.MpText.horizontalOverflow = HorizontalWrapMode.Overflow;
                UIFactory.Place(fighter.MpText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f - numbers, -120f), new Vector2(-22f, -90f));
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

        private Button MenuButton(Transform parent, string label, int column, int row, Action onClick, int fontSize = 24)
        {
            var button = UIFactory.CreateButton(label, parent, label, UIFactory.Card, onClick, fontSize, this);
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
                bool has = i < _player.Moves.Length;
                _moveButtons[i] = MenuButton(box.transform, has ? _player.Moves[i].Def.Name : "-", i % 2, i / 2, () => SelectMove(index), 22);
                if (has) _moveButtons[i].image.color = Color.Lerp(Color.white, StyleColor(_player.Moves[i].Def.Style), _player.Moves[i].Def.Style == BattleStyle.Normal ? 0f : 0.55f);
                _moveCursors[i] = UIFactory.CreateCursor(_moveButtons[i].transform, 16f, UIFactory.PinkDark);
                UIFactory.Place(_moveCursors[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -8f), new Vector2(34f, 8f));
                _moveCursors[i].enabled = false;
            }
        }

        private void BuildItemBox(RectTransform playArea)
        {
            var box = UIFactory.CreateFrame("Items", playArea, Color.white);
            box.raycastTarget = false;
            UIFactory.Place(box.rectTransform, new Vector2(0.02f, 0.03f), new Vector2(0.64f, 0.25f), Vector2.zero, Vector2.zero);
            _itemBox = box.rectTransform;
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                _itemButtons[i] = MenuButton(box.transform, BattleSystem.Items[i].Name, i % 2, i / 2, () => SelectItem(index), 20);
                _itemCursors[i] = UIFactory.CreateCursor(_itemButtons[i].transform, 16f, UIFactory.PinkDark);
                UIFactory.Place(_itemCursors[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -8f), new Vector2(34f, 8f));
                _itemCursors[i].enabled = false;
            }
        }

        // Shared by the move and the item menus: what the highlighted entry does, plus Back.
        private void BuildInfoBox(RectTransform playArea)
        {
            var info = UIFactory.CreateFrame("Info", playArea, Color.white);
            info.raycastTarget = false;
            UIFactory.Place(info.rectTransform, new Vector2(0.66f, 0.03f), new Vector2(0.98f, 0.25f), Vector2.zero, Vector2.zero);
            _infoBox = info.rectTransform;
            _infoTitle = UIFactory.CreatePixelText("Title", info.transform, "", 20, UIFactory.MenuInk, TextAnchor.UpperLeft);
            _infoTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_infoTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -78f), new Vector2(-14f, -16f));
            _infoBody = UIFactory.CreateText("Body", info.transform, "", 17, UIFactory.Muted, TextAnchor.UpperLeft);
            UIFactory.Place(_infoBody.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 78f), new Vector2(-14f, -80f));
            var back = UIFactory.CreateButton("Back", info.transform, "Back", UIFactory.Card, () => { if (!_busy) ShowMenu(); }, 22, this);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 14f), new Vector2(-14f, 74f));
        }

        // ---------- menus ----------

        private void HideMenus()
        {
            _actionBox.gameObject.SetActive(false);
            _moveBox.gameObject.SetActive(false);
            _itemBox.gameObject.SetActive(false);
            _infoBox.gameObject.SetActive(false);
        }

        private void ShowMenu()
        {
            if (!_running) return;
            _busy = false;
            _selectedMove = _selectedItem = -1;
            _dialog.Prompt($"{_player.Name} is waiting for your call.");
            HideMenus();
            _actionBox.gameObject.SetActive(true);
            StartCoroutine(SimpleTween.PopIn(_actionBox, 0.15f));
            if (AutoPlay) StartCoroutine(AutoTurn());
        }

        private IEnumerator AutoTurn()
        {
            yield return new WaitForSeconds(0.4f);
            if (!_running || _busy) yield break;
            var treat = BattleSystem.FindItem("treat");
            if (_player.Hp * 3 < _player.MaxHp && ItemCount(treat.Id) > 0 && _report.ItemsUsed < BattleSystem.MaxItemsPerBattle) { StartCoroutine(Turn(Choice.Item, null, treat)); yield break; }
            Move best = null;
            float bestScore = -1f;
            foreach (var move in _player.Moves)
            {
                if (!_player.CanUse(move) || move.Def.Power <= 0) continue;
                float score = move.Def.Power * BattleSystem.Effectiveness(move.Def.Style, _rival.Style) * (move.Def.Style == _player.Style ? BattleSystem.Stab : 1f) * move.Def.Accuracy;
                if (score > bestScore) { bestScore = score; best = move; }
            }
            StartCoroutine(Turn(Choice.Fight, best ?? Struggle()));
        }

        private void OnFight()
        {
            if (_busy) return;
            HideMenus();
            _moveBox.gameObject.SetActive(true);
            _infoBox.gameObject.SetActive(true);
            int first = -1;
            for (int i = 0; i < 4; i++)
            {
                bool usable = i < _player.Moves.Length && _player.CanUse(_player.Moves[i]);
                _moveButtons[i].interactable = usable;
                if (usable && first < 0) first = i;
            }
            if (first < 0) { StartCoroutine(Turn(Choice.Fight, Struggle())); return; }   // not enough mana for anything it carries
            _selectedMove = -1;
            SelectMove(first);
        }

        // First tap moves the cursor and shows what the move does; a second tap on the same move uses it.
        private void SelectMove(int index)
        {
            if (_busy || index >= _player.Moves.Length) return;
            var move = _player.Moves[index];
            if (_selectedMove == index)
            {
                if (!_player.CanUse(move)) return;
                StartCoroutine(Turn(Choice.Fight, move));
                return;
            }
            _selectedMove = index;
            for (int i = 0; i < 4; i++) _moveCursors[i].enabled = i == index;
            float effect = BattleSystem.Effectiveness(move.Def.Style, _rival.Style);
            string versus = move.Def.Power <= 0 ? "" : effect > 1f ? "  STRONG" : effect < 1f ? "  WEAK" : "";
            _infoTitle.text = (move.Def.Mana > 0 ? $"MP {move.Def.Mana} of {_player.Mp}" : "FREE") + $"\n{move.Def.Style.ToString().ToUpperInvariant()}{versus}";
            _infoBody.text = (move.Def.Power > 0 ? $"Power {move.Def.Power} · {move.Def.Accuracy}%\n" : "") + move.Def.Description;
            StartCoroutine(SimpleTween.PunchScale(_moveButtons[index].transform, 0.04f, 0.15f));
        }

        private int ItemCount(string id) => _battle != null ? _battle.Count(id) : _fallbackItems.TryGetValue(id, out int n) ? n : 0;

        private void OnItem()
        {
            if (_busy) return;
            if (_report.ItemsUsed >= BattleSystem.MaxItemsPerBattle) { StartCoroutine(Notice($"Only {BattleSystem.MaxItemsPerBattle} items per battle!")); return; }
            bool any = false;
            foreach (var item in BattleSystem.Items) if (ItemCount(item.Id) > 0) any = true;
            if (!any) { StartCoroutine(Notice("The bag is empty. Stock up in the Battle Club.")); return; }
            HideMenus();
            _itemBox.gameObject.SetActive(true);
            _infoBox.gameObject.SetActive(true);
            int first = -1;
            for (int i = 0; i < 4; i++)
            {
                int count = ItemCount(BattleSystem.Items[i].Id);
                UIFactory.SetButtonLabel(_itemButtons[i], $"{BattleSystem.Items[i].Name} x{count}");
                _itemButtons[i].interactable = count > 0;
                if (count > 0 && first < 0) first = i;
            }
            _selectedItem = -1;
            SelectItem(first);
        }

        private void SelectItem(int index)
        {
            if (_busy || index < 0) return;
            var item = BattleSystem.Items[index];
            if (_selectedItem == index)
            {
                if (ItemCount(item.Id) <= 0) return;
                if (item.Effect == BattleItemEffect.Heal && _player.Hp >= _player.MaxHp) { StartCoroutine(Notice($"{_player.Name} is already at full health.")); return; }
                StartCoroutine(Turn(Choice.Item, null, item));
                return;
            }
            _selectedItem = index;
            for (int i = 0; i < 4; i++) _itemCursors[i].enabled = i == index;
            _infoTitle.text = $"{item.Name.ToUpperInvariant()}\nx{ItemCount(item.Id)}";
            _infoBody.text = item.Description + $"\n{BattleSystem.MaxItemsPerBattle - _report.ItemsUsed} item turns left.";
            StartCoroutine(SimpleTween.PunchScale(_itemButtons[index].transform, 0.04f, 0.15f));
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
            HideMenus();
            yield return Line(text);
            ShowMenu();
        }

        private IEnumerator RunAway()
        {
            _busy = true;
            HideMenus();
            _player.Stage.React(EmotionType.Anxiety);
            yield return Line($"{_player.Name} slips away...");
            yield return Outro(false, true, _encounter != null ? "Back on the trail." : "Next time, stand your ground!");
        }

        // ---------- battle flow ----------

        private IEnumerator Intro(BattleFighterSetup rivalSetup)
        {
            _player.Group.alpha = 0f;
            string owner = string.IsNullOrEmpty(rivalSetup.OwnerName) ? "" : $"{rivalSetup.OwnerName}'s ";
            yield return Line(_encounter != null && !string.IsNullOrEmpty(_encounter.IntroLine) ? _encounter.IntroLine : $"{owner}{_rival.Name} steps up for a match!");
            yield return Line($"Your turn, {_player.Name}!");
            _player.Group.alpha = 1f;
            StartCoroutine(SimpleTween.PopIn(_player.Stage.Anchor, 0.3f));
            _player.Stage.React(EmotionType.Excitement);
            yield return new WaitForSeconds(0.4f);
            float matchup = BattleSystem.Effectiveness(_player.Style, _rival.Style);
            if (matchup > 1f) yield return Line($"{_player.Style.ToString().ToUpperInvariant()} beats {_rival.Style.ToString().ToUpperInvariant()}. A good match-up!");
            else if (matchup < 1f) yield return Line($"{_rival.Style.ToString().ToUpperInvariant()} beats {_player.Style.ToString().ToUpperInvariant()}. Careful!");
            ShowMenu();
        }

        private IEnumerator Line(string text)
        {
            bool done = false;
            _dialog.Say(text, () => done = true);
            while (!done && _running) yield return null;
        }

        // What a cat does when it has no mana for anything it carries (SCRATCH is free, so only a loadout without it gets here).
        private static Move Struggle() => new Move { Def = new BattleMoveDef { Id = "wild_swing", Name = "WILD SWING", Style = BattleStyle.Normal, Power = 30, RecoilPercent = 25 } };

        private IEnumerator Turn(Choice choice, Move playerMove, BattleItemDef item = null)
        {
            _busy = true;
            HideMenus();
            Move rivalMove = ChooseRivalMove();

            if (choice == Choice.Item) yield return UseItem(item);
            else if (choice == Choice.Cheer)
            {
                _player.AttackStage = Mathf.Min(6, _player.AttackStage + 1);
                _player.Stage.React(EmotionType.Pride);
                PlayCue(_player, Cue.Scream);
                StartCoroutine(SimpleTween.PunchScale(_player.Stage.Anchor, 0.15f, 0.3f));
                yield return Line($"You cheered! {_player.Name}'s ATTACK rose!");
            }

            if (choice != Choice.Fight)
            {
                yield return Execute(_rival, _player, rivalMove);
                if (_player.Hp <= 0 || _rival.Hp <= 0) { yield return Finish(_player.Hp <= 0 ? _player : _rival); yield break; }
                yield return EndOfTurn();
                ShowMenu();
                yield break;
            }

            // Priority moves go first; otherwise the faster cat does (the player wins a tie).
            bool playerFirst = playerMove.Def.Priority != rivalMove.Def.Priority ? playerMove.Def.Priority : _player.EffectiveSpeed >= _rival.EffectiveSpeed;
            var order = playerFirst
                ? new[] { (_player, _rival, playerMove), (_rival, _player, rivalMove) }
                : new[] { (_rival, _player, rivalMove), (_player, _rival, playerMove) };
            foreach (var (attacker, defender, move) in order)
            {
                if (attacker.Hp <= 0 || !_running) continue;
                yield return Execute(attacker, defender, move);
                Fighter down = defender.Hp <= 0 ? defender : attacker.Hp <= 0 ? attacker : null;   // recoil can drop the attacker
                if (down != null) { yield return Finish(down); yield break; }
            }
            yield return EndOfTurn();
            ShowMenu();
        }

        private IEnumerator Finish(Fighter down)
        {
            yield return Faint(down);
            bool won = down == _rival;
            yield return Outro(won, false, won ? "Champion of the rug!" : "A close one. Train and try again!");
        }

        private IEnumerator UseItem(BattleItemDef item)
        {
            if (_battle != null) _battle.Consume(item.Id); else _fallbackItems[item.Id] = Mathf.Max(0, ItemCount(item.Id) - 1);
            _report.ItemsUsed++;
            switch (item.Effect)
            {
                case BattleItemEffect.Heal:
                    int heal = Mathf.Min(_player.MaxHp - _player.Hp, Mathf.RoundToInt(_player.MaxHp * item.Amount / 100f));
                    yield return Line($"{_player.Name} ate a {item.Name}!");
                    _player.Stage.React(EmotionType.Satisfaction);
                    PlayCue(_player, Cue.Heal);
                    yield return DrainHp(_player, _player.Hp + heal);
                    yield return Line($"{_player.Name} got {heal} HP back.");
                    break;
                case BattleItemEffect.RaiseAttack:
                    _player.AttackStage = Mathf.Min(6, _player.AttackStage + item.Amount);
                    _player.Stage.React(EmotionType.Excitement);
                    PlayCue(_player, Cue.Scream);
                    yield return Line($"{_player.Name} rolled in the {item.Name}! ATTACK goes way up!");
                    break;
                default:
                    _player.Mp = _player.MaxMp;
                    RefreshHp(_player);
                    _player.AttackStage = Mathf.Max(0, _player.AttackStage);
                    _player.DefenseStage = Mathf.Max(0, _player.DefenseStage);
                    _player.SpeedStage = Mathf.Max(0, _player.SpeedStage);
                    _player.Stage.React(EmotionType.Relief);
                    PlayCue(_player, Cue.Heal);
                    yield return Line($"{_player.Name} lapped up the {item.Name}. Mana is full, lowered stats are back!");
                    break;
            }
        }

        // Held charms that work between turns.
        private IEnumerator EndOfTurn()
        {
            foreach (var fighter in new[] { _player, _rival })
            {
                if (fighter.Hp <= 0 || fighter.Hp >= fighter.MaxHp || fighter.Charm == null || fighter.Charm.Effect != CharmEffect.Regen) continue;
                int heal = Mathf.Max(1, Mathf.RoundToInt(fighter.MaxHp * fighter.Charm.Amount / 100f));
                yield return DrainHp(fighter, fighter.Hp + heal);
                yield return Line($"{fighter.Name} nibbled its {fighter.Charm.Name}.");
            }
        }

        private Move ChooseRivalMove()
        {
            var usable = new List<Move>();
            foreach (var move in _rival.Moves) if (_rival.CanUse(move)) usable.Add(move);
            if (usable.Count == 0) return Struggle();
            if (_rival.SmartAi && UnityEngine.Random.value < 0.75f)
            {
                Move best = null;
                float bestScore = -1f;
                foreach (var move in usable)
                {
                    float score;
                    if (move.Def.Power > 0)
                        score = move.Def.Power * BattleSystem.Effectiveness(move.Def.Style, _player.Style) * (move.Def.Style == _rival.Style ? BattleSystem.Stab : 1f) * move.Def.Accuracy / 100f;
                    else if (move.Def.Effect == MoveEffect.LowerAttack) score = _player.AttackStage > -2 ? 45f : 0f;
                    else if (move.Def.Effect == MoveEffect.RaiseDefense) score = _rival.DefenseStage < 2 ? 45f : 0f;
                    else score = 20f;
                    if (score > bestScore) { bestScore = score; best = move; }
                }
                if (best != null) return best;
            }
            return usable[UnityEngine.Random.Range(0, usable.Count)];
        }

        private IEnumerator Execute(Fighter attacker, Fighter defender, Move move)
        {
            var def = move.Def;
            attacker.Mp = Mathf.Max(0, attacker.Mp - def.Mana);
            RefreshHp(attacker);
            if (attacker == _player && def.Style == _player.Style && def.Style != BattleStyle.Normal) _report.StyleMovesUsed++;
            yield return Line($"{attacker.Name} goes for {def.Name}!");

            bool targetsSelf = def.Power <= 0 && (def.Effect == MoveEffect.Heal || def.Effect == MoveEffect.RaiseAttack || def.Effect == MoveEffect.RaiseDefense || def.Effect == MoveEffect.RaiseSpeed);
            if (targetsSelf)
            {
                yield return ApplyEffect(attacker, attacker, def);
                yield break;
            }

            attacker.Stage.React(EmotionType.Excitement);
            if (def.Power > 0)
            {
                PlayCue(attacker, Cue.Attack);
                attacker.Stage.LungeBy(attacker == _player ? new Vector2(200f, 150f) : new Vector2(-200f, -150f));
            }
            else PlayCue(attacker, Cue.Scream);
            yield return new WaitForSeconds(0.2f);

            if (UnityEngine.Random.Range(0, 100) >= def.Accuracy)
            {
                defender.Stage.React(EmotionType.Relief);
                yield return Line($"{attacker.Name} misses!");
                yield break;
            }

            if (def.Power <= 0)
            {
                yield return ApplyEffect(attacker, defender, def);
                yield break;
            }

            bool critical = UnityEngine.Random.value < attacker.CritChance * (def.HighCrit ? 3f : 1f);
            float effectiveness = BattleSystem.Effectiveness(def.Style, defender.Style);
            float attack = attacker.Attack * BattleSystem.StageMultiplier(critical ? Mathf.Max(0, attacker.AttackStage) : attacker.AttackStage);
            float defense = defender.Defense * BattleSystem.StageMultiplier(critical ? Mathf.Min(0, defender.DefenseStage) : defender.DefenseStage);
            float baseDamage = BattleSystem.BaseDamage(attacker.Level, def.Power, attack, defense);
            float modifier = (def.Style == attacker.Style && def.Style != BattleStyle.Normal ? BattleSystem.Stab : 1f) * effectiveness * (critical ? 2f : 1f) * UnityEngine.Random.Range(0.85f, 1f);
            int damage = Mathf.Max(1, Mathf.FloorToInt(baseDamage * modifier));

            bool endured = false;
            if (damage >= defender.Hp && defender.Hp > 1 && defender.Charm != null && defender.Charm.Effect == CharmEffect.Endure && !defender.EndureUsed)
            {
                defender.EndureUsed = true;
                endured = true;
                damage = defender.Hp - 1;
            }
            if (attacker == _player)
            {
                _report.DamageDealt += damage;
                if (effectiveness > 1f) _report.SuperEffectiveHits++;
            }

            defender.Stage.React(damage >= defender.Hp ? EmotionType.Shock : EmotionType.Panic);
            if (damage * 4 >= defender.MaxHp) defender.Stage.Play(OneShot.Hurt, defender == _player ? 1f : -1f);   // a heavy hit: the existing flinch
            else PlayCue(defender, Cue.WoundedLightly);
            StartCoroutine(Shake(defender));
            yield return DrainHp(defender, defender.Hp - damage);
            if (critical) yield return Line("Right on the mark! Double damage.");
            if (effectiveness > 1f) yield return Line($"{def.Style.ToString().ToUpperInvariant()} beats {defender.Style.ToString().ToUpperInvariant()}: a strong match-up!");
            else if (effectiveness < 1f) yield return Line($"{defender.Style.ToString().ToUpperInvariant()} shrugs off {def.Style.ToString().ToUpperInvariant()}: a weak match-up.");
            if (endured) yield return Line($"{defender.Name} held on thanks to its {defender.Charm.Name}!");

            if (def.RecoilPercent > 0 && attacker.Hp > 0)
            {
                int recoil = Mathf.Max(1, damage * def.RecoilPercent / 100);
                yield return DrainHp(attacker, attacker.Hp - recoil);
                yield return Line($"The effort costs {attacker.Name} some HP.");
            }
            if (def.Effect != MoveEffect.None && defender.Hp > 0) yield return ApplyEffect(attacker, defender, def);
        }

        private IEnumerator ApplyEffect(Fighter user, Fighter target, BattleMoveDef def)
        {
            switch (def.Effect)
            {
                case MoveEffect.Heal:
                    if (user.Hp >= user.MaxHp) { yield return Line($"{user.Name} is already at full health."); yield break; }
                    int heal = Mathf.Min(user.MaxHp - user.Hp, Mathf.RoundToInt(user.MaxHp * def.Amount / 100f));
                    user.Stage.React(EmotionType.Relief);
                    PlayCue(user, Cue.Heal);
                    yield return DrainHp(user, user.Hp + heal);
                    yield return Line($"{user.Name} got {heal} HP back.");
                    break;
                case MoveEffect.RaiseAttack: yield return ChangeStage(user, BattleStat.Attack, def.Amount); break;
                case MoveEffect.RaiseDefense: yield return ChangeStage(user, BattleStat.Defense, def.Amount); break;
                case MoveEffect.RaiseSpeed: yield return ChangeStage(user, BattleStat.Speed, def.Amount); break;
                case MoveEffect.LowerAttack: yield return ChangeStage(target, BattleStat.Attack, -def.Amount); break;
                case MoveEffect.LowerDefense: yield return ChangeStage(target, BattleStat.Defense, -def.Amount); break;
                case MoveEffect.LowerSpeed: yield return ChangeStage(target, BattleStat.Speed, -def.Amount); break;
            }
        }

        private IEnumerator ChangeStage(Fighter fighter, BattleStat stat, int delta)
        {
            int current = stat == BattleStat.Attack ? fighter.AttackStage : stat == BattleStat.Defense ? fighter.DefenseStage : fighter.SpeedStage;
            string name = stat.ToString().ToUpperInvariant();
            int next = Mathf.Clamp(current + delta, -6, 6);
            if (next == current) { yield return Line($"{fighter.Name}'s {name} cannot go any {(delta > 0 ? "higher" : "lower")}."); yield break; }
            if (stat == BattleStat.Attack) fighter.AttackStage = next; else if (stat == BattleStat.Defense) fighter.DefenseStage = next; else fighter.SpeedStage = next;
            if (delta > 0)
            {
                fighter.Stage.React(EmotionType.Pride);
                PlayCue(fighter, stat == BattleStat.Defense ? Cue.Defensive : Cue.Scream);
                StartCoroutine(SimpleTween.PunchScale(fighter.Stage.Anchor, 0.12f, 0.3f));
            }
            else
            {
                fighter.Stage.React(EmotionType.Irritation);
                StartCoroutine(Shake(fighter));
            }
            yield return Line($"{fighter.Name}'s {name} {(delta > 1 ? "goes way up" : delta > 0 ? "goes up" : delta < -1 ? "drops a lot" : "drops")}!");
        }

        private IEnumerator Faint(Fighter fighter)
        {
            fighter.Stage.React(EmotionType.Grief);
            PlayCue(fighter, Cue.Defeated);
            yield return Line($"{fighter.Name} is worn out!");
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

        private IEnumerator Outro(bool won, bool ran, string message)
        {
            _running = false;
            if (won)
            {
                _player.Stage.React(EmotionType.Pride);
                StartCoroutine(SimpleTween.Hop(_player.Stage.Anchor, 40f, 0.5f));
                yield return Line($"{_player.Name} wins the match!");
            }
            else if (_player.Hp > 0) _player.Stage.React(EmotionType.Embarrassment);

            _report.Won = won; _report.Ran = ran; _report.HpLeft = Mathf.Max(0, _player.Hp); _report.MaxHp = _player.MaxHp;
            _report.MpLeft = Mathf.Max(0, _player.Mp);
            int score = _report.DamageDealt + (won ? 100 + _player.Hp * 4 : 0);
            if (_battle == null) { OnCompleted?.Invoke(MiniGameRewards.Build(Branch, score, won, message, _difficulty)); yield break; }

            if (_encounter != null && _encounter.Finish != null)
            {
                // A fight in the Wild: the campaign books it (no rating) and the trail goes on from the results card.
                var wild = _encounter.Finish(_report);
                string title = won ? (_encounter.Kind == BattleKind.Boss ? "Area boss beaten!" : $"{_rival.Name} backs off.") : ran ? message : "Worn out. Time to go home.";
                OnCompleted?.Invoke(new MiniGameResult
                {
                    Branch = Branch, Score = score, Won = won, XpReward = wild.Xp, CoinReward = wild.Coins, Summary = title,
                    Tier = 1, RewardMultiplier = 1f, Lines = new List<string>(wild.Lines) { VitalsLine() }, NoReplay = true,
                });
                yield break;
            }

            int ratingBefore = _battle.Rating;
            var league = _battle.League;
            var rewards = _battle.Finish(_report);
            var lines = new List<string> { $"Rating {ratingBefore} > {rewards.NewRating} ({(rewards.RatingDelta >= 0 ? "+" : "")}{rewards.RatingDelta}) · {_battle.League.Name} League" };
            lines.AddRange(rewards.Lines);
            lines.Add(VitalsLine());
            OnCompleted?.Invoke(new MiniGameResult
            {
                Branch = Branch, Score = score, Won = won, XpReward = rewards.Xp, CoinReward = rewards.Coins,
                Summary = rewards.PromotedTo != null ? $"{rewards.PromotedTo.Name} League!" : message,
                Tier = BattleSystem.LeagueIndexFor(ratingBefore) + 1, RewardMultiplier = league.CoinMultiplier, Lines = lines,
            });
        }

        // Health and mana stay as the fight left them; the results card says so, because it is why the cat has to rest.
        private string VitalsLine() => $"{_player.Name} goes home with {_report.HpLeft}/{_report.MaxHp} HP and {_report.MpLeft}/{_player.MaxMp} MP. Rest to recover.";

        // ---------- presentation ----------

        private void PlayCue(Fighter fighter, Cue cue)
        {
            float direction = fighter == _player ? 1f : -1f;
            switch (cue)
            {
                case Cue.Attack: break;                                                          // stand-in: the Attack clip, played by MiniGameStage.LungeBy
                case Cue.Defensive: fighter.Stage.Play(OneShot.Nod, direction); break;           // stand-in for a braced, guarding pose
                case Cue.Scream: fighter.Stage.Play(OneShot.Yawn, direction); break;             // stand-in for the battle cry (hiss, cheer, power-ups)
                case Cue.Heal: fighter.Stage.Play(OneShot.Eat, direction); break;                // stand-in for the healing moment
                case Cue.Defeated: fighter.Stage.Play(OneShot.Faint, direction); break;          // stand-in: the Fainted loop
                case Cue.WoundedLightly: fighter.Stage.Play(OneShot.Wiggle, direction); break;   // stand-in for a light flinch (Hurt is the heavy one)
            }
        }

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
            bool low = fighter.Hp * 5 <= fighter.MaxHp;
            fighter.HpBar.SetColors(low ? HpLowFill : HpFill, low ? HpLowLine : HpLine);
            fighter.HpBar.Set(fighter.Hp, fighter.MaxHp);
            if (fighter.HpText != null) fighter.HpText.text = $"{fighter.Hp}/{fighter.MaxHp}";
            if (fighter.MpBar != null) fighter.MpBar.Set(fighter.Mp, fighter.MaxMp);
            if (fighter.MpText != null) fighter.MpText.text = $"{fighter.Mp}/{fighter.MaxMp}";
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
