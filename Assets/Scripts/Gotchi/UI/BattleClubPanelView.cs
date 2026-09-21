using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Creature3D;
using Gotchi.Data;
using Gotchi.Economy;
using Gotchi.MiniGames;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // The Battle Club: ranked battles and everything that builds the cat for them (rules in Systems/BattleSystem.cs,
    // design in 13-pvp-design.md). Four pages, each also reachable straight from the home menu:
    //   CLUB   league and rating, today's condition, the next rival, daily quests, FIGHT
    //   TRAIN  the four stat ranks and the fighting style
    //   MOVES  the loadout of four and every move that can be learned
    //   BAG    what the cat owns: battle items, held charms, arenas (the Market is where they are bought and sold)
    // The panel is rebuilt from the save whenever something changes; nothing here keeps state of its own.
    public class BattleClubPanelView
    {
        public const int ClubPage = 0, TrainPage = 1, MovesPage = 2, BagPage = 3;

        private readonly GameContext _ctx;
        private readonly BattleSystem _battle;
        private readonly Action<string> _toast;
        private readonly Action _onFight, _onMarket;
        private readonly MonoBehaviour _host;
        private readonly DialogBoxView _dialog;
        private readonly PagedPanel _panel;
        private bool _dirty;

        public GameObject Root => _panel.Root;

        public BattleClubPanelView(Transform parent, GameContext ctx, Action<string> toast, Action onFight, Action onMarket, Action onClose, MonoBehaviour host, DialogBoxView dialog)
        {
            _ctx = ctx;
            _battle = ctx.Battle;
            _toast = toast;
            _onFight = onFight;
            _onMarket = onMarket;
            _host = host;
            _dialog = dialog;
            _panel = new PagedPanel(parent, "BattleClubRoot", "Battle Club", new[] { "Club", "Train", "Moves", "Bag" },
                new[] { UIFactory.IconKind.Trophy, UIFactory.IconKind.Sparkle, UIFactory.IconKind.Paw, UIFactory.IconKind.Bag }, ctx, onClose, host);
            _battle.OnChanged += MarkDirty;
            _ctx.Wallet.OnBalanceChanged += (_, __) => MarkDirty();
            Refresh();
        }

        // Several things can change in one call (a purchase moves coins and the save); rebuild once, and only while shown.
        private void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            if (Root.activeInHierarchy) _host.StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            if (_dirty) Refresh();
        }

        public void ShowPage(int index) => _panel.ShowPage(index);

        public void Refresh()
        {
            _dirty = false;
            _battle.RollDailyIfNeeded();
            _panel.Rebuild(lists =>
            {
                BuildClub(lists[ClubPage]);
                BuildTrain(lists[TrainPage]);
                BuildMoves(lists[MovesPage]);
                BuildBag(lists[BagPage]);
            });
        }

        // ---------------------------------------------------------------- CLUB

        private void BuildClub(RectTransform list)
        {
            if (!_battle.HasStyle) { BuildStylePicker(list, true); return; }

            // League and rating.
            var league = _battle.League;
            var next = _battle.NextLeague;
            var leagueBox = PanelRows.Box(list, "League", 204f, Color.white);
            PanelRows.Pixel(leagueBox, $"{league.Name.ToUpperInvariant()} LEAGUE", 32, UIFactory.MenuInk, 24f, -20f, -24f, 42f);
            PanelRows.Body(leagueBox, $"Rating {_battle.Rating}" + (next != null ? $" · {next.MinRating - _battle.Rating} to {next.Name}" : " · the top league"), 24, UIFactory.Ink, 24f, -66f, -24f, 32f);
            var bar = UIFactory.CreatePillBar("Bar", leagueBox, UIFactory.Butter, out RectTransform fill);
            UIFactory.Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -124f), new Vector2(-24f, -106f));
            bar.raycastTarget = false;
            fill.anchorMax = new Vector2(Mathf.Max(0.03f, _battle.LeagueProgress), 1f);
            PanelRows.Body(leagueBox, $"Won {_battle.Wins} · lost {_battle.Losses} · streak {_battle.WinStreak} (best {_battle.BestWinStreak})", 21, UIFactory.Muted, 24f, -134f, -24f, 28f);
            if (next != null) PanelRows.Body(leagueBox, $"{next.Name} pays {next.RewardCoins} coins and {next.RewardHearts} hearts.", 21, UIFactory.Muted, 24f, -162f, -24f, 28f);

            // The cat as it enters the next battle.
            var stats = _battle.CurrentStats();
            var conditions = _battle.Conditions();
            var charm = BattleSystem.FindCharm(_battle.EquippedCharm);
            int hp = _battle.Hp, maxHp = _battle.MaxHp, mp = _battle.Mp, maxMp = _battle.MaxMp;
            bool tired = hp < maxHp || mp < maxMp;
            var catBox = PanelRows.Box(list, "Cat", 246f + Mathf.Max(1, conditions.Count) * 32f + (tired ? 30f : 0f), Color.white);
            PanelRows.Pixel(catBox, _ctx.Data.petName.ToUpperInvariant(), 32, UIFactory.MenuInk, 24f, -20f, -200f, 42f);
            PanelRows.Chip(catBox, _battle.Style.ToString().ToUpperInvariant(), BattleMiniGame.StyleColor(_battle.Style), -24f, -22f, 150f);
            PanelRows.Pixel(catBox, $"ATK {stats.Attack}   DEF {stats.Defense}   SPD {stats.Speed}", 26, UIFactory.MenuInk, 24f, -68f, -24f, 36f);
            // Health and mana as they stand: the cat walks into the next fight with exactly this.
            bool low = hp * 5 <= maxHp;
            VitalRow(catBox, "HP", hp, maxHp, low ? BattleMiniGame.HpLowFill : BattleMiniGame.HpFill, low ? BattleMiniGame.HpLowLine : BattleMiniGame.HpLine, -112f);
            VitalRow(catBox, "MP", mp, maxMp, BattleMiniGame.MpFill, BattleMiniGame.MpLine, -148f);
            float y = -184f;
            if (tired) { PanelRows.Body(catBox, "A fight leaves both bars as they end. REST and FOCUS in the camp, a treat, a bag item or ten quiet minutes refill them.", 20, UIFactory.Muted, 24f, y, -24f, 30f); y -= 30f; }
            PanelRows.Body(catBox, charm != null ? $"Holding {charm.Name}: {charm.Description}" : "No charm held. The Market sells them.", 21, UIFactory.Muted, 24f, y, -24f, 30f);
            y -= 36f;
            if (conditions.Count == 0) PanelRows.Body(catBox, "No camp buff right now. FEED buys ATTACK and GROOM buys DEFENSE for the next battles.", 21, UIFactory.Muted, 24f, y, -24f, 30f);
            foreach (var line in conditions)
            {
                PanelRows.Body(catBox, (line.Good ? "+ " : "- ") + line.Text, 22, line.Good ? PanelRows.Good : UIFactory.PinkDark, 24f, y, -24f, 30f);
                y -= 32f;
            }

            // The next rival: fixed until fought, so the player can prepare for its style.
            var rival = _battle.NextRival();
            var rivalBox = PanelRows.Box(list, "Rival", 206f, Color.white);
            var portrait = UIFactory.CreateRect("Portrait", rivalBox);
            UIFactory.Place(portrait, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(100f, 18f), new Vector2(100f, 18f));
            new PetPortraitView(portrait, rival.Species, _host, 120f, CatCoat.Find(rival.CoatId)).SetFace(EmotionType.Contempt, false);
            PanelRows.Pixel(rivalBox, "NEXT RIVAL", 20, UIFactory.Muted, 200f, -18f, -24f, 28f);
            PanelRows.Pixel(rivalBox, $"{rival.Name.ToUpperInvariant()} · LV {rival.Level}", 30, UIFactory.MenuInk, 200f, -46f, -170f, 40f);
            PanelRows.Chip(rivalBox, rival.Style.ToString().ToUpperInvariant(), BattleMiniGame.StyleColor(rival.Style), -24f, -48f, 140f);
            PanelRows.Body(rivalBox, $"{rival.OwnerName}'s cat · rating {rival.Rating}", 21, UIFactory.Muted, 200f, -88f, -24f, 30f);
            float matchup = BattleSystem.Effectiveness(_battle.Style, rival.Style);
            string hint = matchup > 1f ? $"Your {_battle.Style} style beats {rival.Style}."
                : matchup < 1f ? $"{rival.Style} beats your {_battle.Style} style. Bring a {BattleSystem.WeakAgainst(rival.Style)} move."
                : $"Same style. A {BattleSystem.WeakAgainst(rival.Style)} move would hit hard.";
            PanelRows.Body(rivalBox, hint, 22, matchup < 1f ? UIFactory.PinkDark : UIFactory.Ink, 200f, -120f, -24f, 64f);

            // Today.
            var quests = _battle.TodayQuests();
            var dailyBox = PanelRows.Box(list, "Daily", 146f + quests.Count * 36f, Color.white);
            PanelRows.Pixel(dailyBox, "TODAY", 26, UIFactory.MenuInk, 24f, -18f, -24f, 34f);
            PanelRows.Body(dailyBox, _battle.FirstWinAvailable ? $"First ranked win of the day: +{BattleSystem.FirstWinBonus} coins" : "First win bonus collected.", 22, _battle.FirstWinAvailable ? UIFactory.Ink : UIFactory.Muted, 24f, -56f, -24f, 30f);
            y = -92f;
            foreach (var quest in quests)
            {
                PanelRows.Body(dailyBox, (quest.Done ? "Done: " : "") + quest.Def.Text, 22, quest.Done ? UIFactory.Muted : UIFactory.Ink, 24f, y, PanelRows.TextRight, 32f);
                var reward = PanelRows.Body(dailyBox, quest.Done ? "paid" : $"{quest.Progress}/{quest.Def.Goal} · {quest.Def.RewardCoins} coins", 22, quest.Done ? UIFactory.Muted : UIFactory.PinkDark, 24f, y, -24f, 32f);
                reward.alignment = TextAnchor.UpperRight;
                y -= 36f;
            }
            PanelRows.Body(dailyBox, _battle.DailyChestClaimed ? "All three done. See you tomorrow!" : $"Finish all three: +{BattleSystem.DailyChestHearts} hearts." + (GameFeatures.Wild ? " Fights in the Wild count too." : ""), 21, UIFactory.Muted, 24f, y - 4f, -24f, 30f);

            // Fight. The needs never stop a battle; an empty health bar does (rest for a moment, it refills with time).
            PanelRows.Primary(list, _battle.CanFight ? "Fight!" : "Worn out", UIFactory.Pink, _battle.CanFight, () => _onFight?.Invoke(), _host);
            if (!_battle.CanFight) PanelRows.Note(list, $"{_ctx.Data.petName} is under a tenth of its health. REST on the home screen, or wait a little.", 50f);
        }

        // "HP ▰▰▰▰▱ 1000/1200" inside a box, `top` down from the box's top: the same block bar as the home screen.
        private static void VitalRow(RectTransform box, string label, int value, int max, Color fill, Color line, float top)
        {
            PanelRows.Pixel(box, label, 22, line, 24f, top, -24f, 30f);
            var bar = SegmentedBar.Create(label + "Bar", box, fill, line);
            UIFactory.Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(76f, top - 26f), new Vector2(-190f, top - 4f));
            bar.Set(value, max);
            var numbers = PanelRows.Pixel(box, $"{value}/{max}", 22, UIFactory.MenuInk, 24f, top, -24f, 30f);
            numbers.alignment = TextAnchor.MiddleRight;
        }

        private void BuildStylePicker(RectTransform list, bool first)
        {
            var intro = UIFactory.CreateText("Intro", list, first
                ? "Every battling cat has a style. CLAW beats TRICK, TRICK beats FLUFF, FLUFF beats CLAW. The first pick is free and comes with four moves and three Fish Treats."
                : $"Changing style costs {BattleSystem.RespecCost} coins. You keep every move you know.", 22, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            UIFactory.SetPreferredHeight(intro.gameObject, first ? 124f : 64f);
            foreach (BattleStyle style in new[] { BattleStyle.Claw, BattleStyle.Fluff, BattleStyle.Trick })
            {
                BattleStyle captured = style;
                bool current = _battle.Style == style;
                var box = PanelRows.Box(list, style.ToString(), 140f, Color.Lerp(Color.white, BattleMiniGame.StyleColor(style), 0.35f));
                PanelRows.Pixel(box, style.ToString().ToUpperInvariant(), 32, UIFactory.MenuInk, 24f, -20f, PanelRows.TextRight, 42f);
                PanelRows.Body(box, BattleSystem.StyleBlurb(style), 22, UIFactory.Ink, 24f, -66f, PanelRows.TextRight, 62f);
                string label = current ? "Yours" : first ? "Choose" : $"Switch · {BattleSystem.RespecCost}";
                PanelRows.ActionButton(box, label, current ? PanelRows.Off : UIFactory.Pink, !current && (first || _ctx.Wallet.Get(CurrencyType.Soft) >= BattleSystem.RespecCost), () =>
                {
                    if (first) { Pick(captured); return; }
                    _dialog.Ask($"Switch to the {captured} style for {BattleSystem.RespecCost} coins?", new[] { "Yes", "No" }, choice => { if (choice == 0) Pick(captured); });
                }, _host);
            }
        }

        private void Pick(BattleStyle style)
        {
            bool ok = _battle.ChooseStyle(style, out string message);
            _toast(message);
            if (ok) Refresh();
        }

        // ---------------------------------------------------------------- TRAIN

        private void BuildTrain(RectTransform list)
        {
            if (!_battle.HasStyle) { PanelRows.Note(list, "Pick a fighting style on the Club page first.", 80f); return; }
            PanelRows.Note(list, $"Coins buy training: every rank adds 5% to a stat. A rank can be at most one above the pet's level (now Lv {_ctx.Level.Level}), so looking after {_ctx.Data.petName} is what unlocks more.", 96f);

            var trained = _battle.TrainedStats();
            foreach (BattleStat stat in Enum.GetValues(typeof(BattleStat)))
            {
                BattleStat captured = stat;
                int rank = _battle.Rank(stat);
                int value = stat == BattleStat.Hp ? trained.MaxHp : stat == BattleStat.Attack ? trained.Attack : stat == BattleStat.Defense ? trained.Defense : trained.Speed;
                bool can = _battle.CanTrain(stat, out string reason);
                var box = PanelRows.Box(list, stat.ToString(), 132f, Color.white);
                PanelRows.Pixel(box, StatName(stat), 30, UIFactory.MenuInk, 24f, -18f, PanelRows.TextRight, 40f);
                PanelRows.Body(box, $"{value} now · rank {rank}/{BattleSystem.MaxRank}" + (can || rank >= BattleSystem.MaxRank ? "" : " · " + reason), 22, can ? UIFactory.Ink : UIFactory.Muted, 24f, -60f, PanelRows.TextRight, 30f);
                var bar = UIFactory.CreatePillBar("Bar", box, StatColor(stat), out RectTransform fill);
                UIFactory.Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 18f), new Vector2(PanelRows.TextRight, 34f));
                bar.raycastTarget = false;
                fill.anchorMax = new Vector2(Mathf.Max(0.03f, rank / (float)BattleSystem.MaxRank), 1f);
                PanelRows.ActionButton(box, rank >= BattleSystem.MaxRank ? "Max" : $"Train · {_battle.TrainCost(stat)}", UIFactory.Butter, can, () =>
                {
                    if (_battle.TryTrain(captured)) _toast($"{StatName(captured)} rank {_battle.Rank(captured)}!");
                }, _host);
            }

            PanelRows.Note(list, "Fighting style", 44f, true);
            BuildStylePicker(list, false);
        }

        private static string StatName(BattleStat stat) => stat == BattleStat.Hp ? "HP" : stat.ToString().ToUpperInvariant();

        private static Color StatColor(BattleStat stat)
        {
            switch (stat)
            {
                case BattleStat.Hp: return UIFactory.Hex("58D080");
                case BattleStat.Attack: return UIFactory.Coral;
                case BattleStat.Defense: return UIFactory.Sky;
                default: return UIFactory.Butter;
            }
        }

        // ---------------------------------------------------------------- MOVES

        private void BuildMoves(RectTransform list)
        {
            if (!_battle.HasStyle) { PanelRows.Note(list, "Pick a fighting style on the Club page first.", 80f); return; }
            PanelRows.Note(list, $"The cat carries {BattleSystem.LoadoutSize} moves into battle ({_battle.Loadout.Count} chosen). Moves cost mana ({_battle.Mp}/{_battle.MaxMp} now); SCRATCH is free. Own-style moves hit 50% harder; bring one that beats the rival's style.", 120f);

            var ordered = new List<BattleMoveDef>(BattleSystem.Moves);
            ordered.Sort((a, b) => MoveOrder(a).CompareTo(MoveOrder(b)));
            foreach (var move in ordered)
            {
                BattleMoveDef captured = move;
                bool owned = _battle.Owns(move.Id), carried = _battle.InLoadout(move.Id);
                var box = PanelRows.Box(list, move.Id, 146f, carried ? PanelRows.Held : owned ? Color.white : PanelRows.Locked);
                PanelRows.Pixel(box, move.Name, 28, UIFactory.MenuInk, 24f, -16f, -400f, 38f);
                PanelRows.Chip(box, move.Style.ToString().ToUpperInvariant(), BattleMiniGame.StyleColor(move.Style), PanelRows.TextRight, -18f, 130f);
                string mana = move.Mana > 0 ? $"{move.Mana} MP" : "free";
                PanelRows.Body(box, (move.Power > 0 ? $"Power {move.Power} · {move.Accuracy}% · {mana}" : $"Status · {mana}") + (move.Priority ? " · first" : "") + (move.HighCrit ? " · crits" : ""), 21, UIFactory.Ink, 24f, -58f, PanelRows.TextRight, 28f);
                PanelRows.Body(box, move.Description, 21, UIFactory.Muted, 24f, -88f, PanelRows.TextRight, 54f);

                if (carried) PanelRows.ActionButton(box, "Remove", UIFactory.Card, _battle.Loadout.Count > 1, () => _battle.ToggleLoadout(captured.Id), _host);
                else if (owned)
                {
                    bool room = _battle.Loadout.Count < BattleSystem.LoadoutSize;
                    PanelRows.ActionButton(box, room ? "Carry" : "Full", UIFactory.Mint, room, () => _battle.ToggleLoadout(captured.Id), _host);
                }
                else
                {
                    bool can = _battle.CanLearn(move, out _);
                    bool leagueLocked = _battle.LeagueIndex < move.League;
                    PanelRows.ActionButton(box, leagueLocked ? BattleSystem.Leagues[move.League].Name : $"Learn · {move.Cost}", UIFactory.Butter, can, () =>
                        _dialog.Ask($"Learn {captured.Name} for {captured.Cost} coins?", new[] { "Yes", "No" }, choice =>
                        {
                            if (choice == 0 && _battle.TryLearn(captured)) _toast($"{_ctx.Data.petName} learned {captured.Name}!");
                        }), _host);
                }
            }
        }

        private int MoveOrder(BattleMoveDef move) => _battle.InLoadout(move.Id) ? 0 : _battle.Owns(move.Id) ? 1 : _battle.LeagueIndex >= move.League ? 2 + move.League : 10 + move.League;

        // ---------------------------------------------------------------- BAG (what the cat owns; the Market trades it)

        private void BuildBag(RectTransform list)
        {
            if (!_battle.HasStyle) { PanelRows.Note(list, "Pick a fighting style on the Club page first.", 80f); return; }

            PanelRows.Note(list, $"Items · {BattleSystem.MaxItemsPerBattle} item turns per battle, the bag holds {BattleSystem.MaxCarry} of each", 44f, true);
            foreach (var item in BattleSystem.Items)
            {
                BattleItemDef captured = item;
                int count = _battle.Count(item.Id);
                var box = PanelRows.Box(list, item.Id, 100f, count > 0 ? Color.white : PanelRows.Locked);
                PanelRows.Pixel(box, $"{item.Name.ToUpperInvariant()}  x{count}", 26, UIFactory.MenuInk, 24f, -14f, PanelRows.TextRight, 36f);
                PanelRows.Body(box, item.Description, 21, UIFactory.Muted, 24f, -54f, PanelRows.TextRight, 40f);
                // Health and mana carry over between fights, so what heals in a fight heals at home too.
                bool fightOnly = item.Effect == BattleItemEffect.RaiseAttack;
                PanelRows.ActionButton(box, fightOnly ? "In a fight" : "Use", fightOnly ? PanelRows.Off : UIFactory.Mint, !fightOnly && count > 0, () =>
                {
                    _toast(_battle.TryUseAtHome(captured, out string message) ? $"{captured.Name}: {message}" : message);
                }, _host);
            }

            PanelRows.Note(list, "Charms · the cat holds one", 44f, true);
            bool anyCharm = false;
            foreach (var charm in BattleSystem.Charms)
            {
                if (!_battle.OwnsCharm(charm.Id)) continue;
                anyCharm = true;
                BattleCharmDef captured = charm;
                bool held = _battle.EquippedCharm == charm.Id;
                var box = PanelRows.Box(list, charm.Id, 112f, held ? PanelRows.Held : Color.white);
                PanelRows.Pixel(box, charm.Name.ToUpperInvariant(), 26, UIFactory.MenuInk, 24f, -16f, PanelRows.TextRight, 36f);
                PanelRows.Body(box, charm.Description, 21, UIFactory.Muted, 24f, -58f, PanelRows.TextRight, 50f);
                PanelRows.ActionButton(box, held ? "Take off" : "Hold", held ? UIFactory.Card : UIFactory.Mint, true, () => _battle.EquipCharm(captured.Id), _host);
            }
            if (!anyCharm) PanelRows.Note(list, "No charms yet. The Market sells five; two more come with league promotions.", 60f);

            PanelRows.Note(list, "Arenas · looks only", 44f, true);
            foreach (var arena in BattleSystem.Arenas)
            {
                if (!_battle.OwnsArena(arena.Id)) continue;
                ArenaDef captured = arena;
                bool used = _battle.Arena.Id == arena.Id;
                var box = PanelRows.Box(list, arena.Id, 112f, used ? PanelRows.Held : Color.white);
                ArenaSwatch(box, arena);
                PanelRows.Pixel(box, arena.Name.ToUpperInvariant(), 26, UIFactory.MenuInk, 116f, -16f, PanelRows.TextRight, 36f);
                PanelRows.Body(box, arena.Description, 21, UIFactory.Muted, 116f, -58f, PanelRows.TextRight, 50f);
                PanelRows.ActionButton(box, used ? "In use" : "Use", UIFactory.Mint, !used, () => _battle.UseArena(captured.Id), _host);
            }

            PanelRows.Primary(list, "To the Market", UIFactory.Butter, true, () => _onMarket?.Invoke(), _host);
        }

        public static void ArenaSwatch(RectTransform box, ArenaDef arena)
        {
            var swatch = UIFactory.CreatePanel("Swatch", box, UIFactory.Hex(arena.SkyTop));
            swatch.sprite = UIFactory.ThinFrameSprite; swatch.type = Image.Type.Sliced; swatch.raycastTarget = false;
            UIFactory.Place(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, -34f), new Vector2(100f, 34f));
            var ground = UIFactory.CreatePanel("Ground", swatch.transform, UIFactory.Hex(arena.Ground));
            UIFactory.Place(ground.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.45f), new Vector2(4f, 4f), new Vector2(-4f, 0f));
            ground.raycastTarget = false;
        }
    }
}
