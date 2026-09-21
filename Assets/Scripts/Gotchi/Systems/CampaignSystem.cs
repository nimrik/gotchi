using System;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;

namespace Gotchi.Systems
{
    // ---------------------------------------------------------------------------------------------------------
    // The Wild: the campaign. Design: 13-pvp-design.md, "The Wild".
    //
    // PARKED (2026-09-21): not in the first release. The Wild is to become a whole world to explore, which is a
    // bigger piece of work than this trail of steps. GameFeatures.Wild is off, so nothing on screen leads here;
    // the rules stay compiled and covered by the smoke test so the encounter hand-off, the absolute-level areas
    // and the boss logic are there to build on.
    //
    // The world is a chain of areas, each a short trail of steps. The player SEARCHES one step at a time: a step
    // is a wild cat to fight, something lying in the grass, or the area's boss at the end. Beating the boss clears
    // the area and opens the next one. What makes it different from the Battle Club:
    //   * levels are ABSOLUTE (the garden is level 1-2, the night forest 9-11), so the Wild is the yardstick the
    //     cat grows against, while ranked rivals always match the player;
    //   * health and mana carry over from one encounter to the next (they do after every fight now, see
    //     BattleSystem), so an expedition is about how far the cat gets on what is in the bag; going home keeps
    //     everything found, and so does being carried home after a knockout;
    //   * there is no rating. Coins and XP per fight are fixed by the area; a first clear pays coins and hearts.
    // Finds are fixed per step and come back once a day. Which wild cat turns up is seeded, never bought.
    //
    // Every wild animal is a cat in another coat today (one character model); WildAreaDef.Species and
    // BattleFighterSetup.Species are where other characters plug in later.
    // ---------------------------------------------------------------------------------------------------------

    public enum WildStepKind { Wild, Find, Boss }

    public class WildStepDef
    {
        public WildStepKind Kind;
        public string ItemId;   // Find: the item lying there, or
        public int Coins;       // Find: a few coins

        public static WildStepDef Cat() => new WildStepDef { Kind = WildStepKind.Wild };
        public static WildStepDef Item(string id) => new WildStepDef { Kind = WildStepKind.Find, ItemId = id };
        public static WildStepDef Purse(int coins) => new WildStepDef { Kind = WildStepKind.Find, Coins = coins };
        public static WildStepDef Boss() => new WildStepDef { Kind = WildStepKind.Boss };
    }

    public class WildAreaDef
    {
        public string Id, Name, Description;
        public int Tier;                         // 1-based; scales every reward
        public SpeciesType Species = SpeciesType.Cat;
        public int MinLevel, MaxLevel, BossLevel;
        public int Ranks;                        // training ranks of the wild cats; the boss has two more
        public string[] WildNames, Coats;
        public string BossName, BossCoat;
        public BattleStyle BossStyle;
        public WildStepDef[] Steps;
        public ArenaDef Scenery;

        public int WildCoins => 12 * Tier;
        public int WildXp => 15 + 5 * Tier;
        public int BossXp => 40 + 10 * Tier;
        public int ClearCoins => 100 * Tier;     // first clear
        public int ClearHearts => 2 + Tier;      // first clear
        public int RepeatCoins => 30 * Tier;     // every clear after that
    }

    public enum SearchOutcome { Nothing, Found, PickedClean, Encounter }

    public class SearchResult
    {
        public SearchOutcome Outcome;
        public string Text;
    }

    public class CampaignSystem
    {
        public static readonly WildAreaDef[] Areas =
        {
            new WildAreaDef
            {
                Id = "garden", Name = "Back Garden", Description = "Tall grass behind the house. A good place to start.", Tier = 1,
                MinLevel = 1, MaxLevel = 2, BossLevel = 3, Ranks = 0,
                WildNames = new[] { "Stray Tabby", "Garden Mouser", "Hedge Kitten" }, Coats = new[] { "tabby", "ginger", "cream" },
                BossName = "Bramble", BossCoat = "rust", BossStyle = BattleStyle.Claw,
                Steps = new[] { WildStepDef.Cat(), WildStepDef.Item("treat"), WildStepDef.Cat(), WildStepDef.Purse(20), WildStepDef.Cat(), WildStepDef.Boss() },
                Scenery = new ArenaDef { Id = "wild_garden", Name = "Back Garden", SkyTop = "8FD3F7", SkyBottom = "DDF3FF", Ground = "A8DFB8", Platform = "C9EFC7" },
            },
            new WildAreaDef
            {
                Id = "alley", Name = "Bin Alley", Description = "Narrow, smelly and full of cats with opinions.", Tier = 2,
                MinLevel = 2, MaxLevel = 3, BossLevel = 4, Ranks = 1,
                WildNames = new[] { "Alley Cat", "Bin Diver", "Fence Walker" }, Coats = new[] { "smoke", "night", "tabby" },
                BossName = "Rusty", BossCoat = "rust", BossStyle = BattleStyle.Trick,
                Steps = new[] { WildStepDef.Cat(), WildStepDef.Cat(), WildStepDef.Item("treat"), WildStepDef.Cat(), WildStepDef.Item("catnip"), WildStepDef.Boss() },
                Scenery = new ArenaDef { Id = "wild_alley", Name = "Bin Alley", SkyTop = "B9B4C7", SkyBottom = "E4DFEA", Ground = "9C96A8", Platform = "C4BFD0" },
            },
            new WildAreaDef
            {
                Id = "meadow", Name = "Long Meadow", Description = "Open ground, fast cats, nowhere to hide.", Tier = 3,
                MinLevel = 3, MaxLevel = 5, BossLevel = 6, Ranks = 2,
                WildNames = new[] { "Meadow Prowler", "Clover Cat", "Field Sprinter" }, Coats = new[] { "ginger", "cream", "ash" },
                BossName = "Clover", BossCoat = "ash", BossStyle = BattleStyle.Fluff,
                Steps = new[] { WildStepDef.Cat(), WildStepDef.Purse(30), WildStepDef.Cat(), WildStepDef.Cat(), WildStepDef.Item("milk"), WildStepDef.Cat(), WildStepDef.Boss() },
                Scenery = new ArenaDef { Id = "wild_meadow", Name = "Long Meadow", SkyTop = "9AD9F5", SkyBottom = "FFF4C9", Ground = "BFE39A", Platform = "DDF2B8" },
            },
            new WildAreaDef
            {
                Id = "barn", Name = "Old Barn", Description = "Hay, dust and the cats that run the place.", Tier = 4,
                MinLevel = 5, MaxLevel = 7, BossLevel = 8, Ranks = 3,
                WildNames = new[] { "Barn Cat", "Hay Sleeper", "Rafter Watcher" }, Coats = new[] { "tabby", "smoke", "rust" },
                BossName = "Old Soot", BossCoat = "shadow", BossStyle = BattleStyle.Fluff,
                Steps = new[] { WildStepDef.Cat(), WildStepDef.Cat(), WildStepDef.Item("tuna"), WildStepDef.Cat(), WildStepDef.Cat(), WildStepDef.Purse(50), WildStepDef.Boss() },
                Scenery = new ArenaDef { Id = "wild_barn", Name = "Old Barn", SkyTop = "C99A6B", SkyBottom = "EFD3A8", Ground = "D9B06E", Platform = "F0D59A" },
            },
            new WildAreaDef
            {
                Id = "roofs", Name = "High Roofs", Description = "One slip and it is a long way down.", Tier = 5,
                MinLevel = 7, MaxLevel = 9, BossLevel = 10, Ranks = 4,
                WildNames = new[] { "Roof Walker", "Chimney Cat", "Gutter Runner" }, Coats = new[] { "night", "ash", "smoke" },
                BossName = "Gable", BossCoat = "ash", BossStyle = BattleStyle.Trick,
                Steps = new[] { WildStepDef.Cat(), WildStepDef.Cat(), WildStepDef.Item("catnip"), WildStepDef.Cat(), WildStepDef.Item("tuna"), WildStepDef.Cat(), WildStepDef.Cat(), WildStepDef.Boss() },
                Scenery = new ArenaDef { Id = "wild_roofs", Name = "High Roofs", SkyTop = "F2A27C", SkyBottom = "FFE0B0", Ground = "B5705A", Platform = "D9917A" },
            },
            new WildAreaDef
            {
                Id = "forest", Name = "Night Forest", Description = "Eyes between the trees. The end of the map, for now.", Tier = 6,
                MinLevel = 9, MaxLevel = 11, BossLevel = 12, Ranks = 5,
                WildNames = new[] { "Forest Shadow", "Moon Hunter", "Owl Chaser" }, Coats = new[] { "night", "shadow", "tabby" },
                BossName = "Umbra", BossCoat = "shadow", BossStyle = BattleStyle.Claw,
                Steps = new[] { WildStepDef.Cat(), WildStepDef.Cat(), WildStepDef.Item("milk"), WildStepDef.Cat(), WildStepDef.Cat(), WildStepDef.Item("tuna"), WildStepDef.Cat(), WildStepDef.Boss() },
                Scenery = new ArenaDef { Id = "wild_forest", Name = "Night Forest", SkyTop = "1B1840", SkyBottom = "3A4A6A", Ground = "2F4A3E", Platform = "456A58" },
            },
        };

        public static WildAreaDef FindArea(string id) { foreach (var a in Areas) if (a.Id == id) return a; return null; }

        private readonly PetSaveData _data;
        private readonly BattleSystem _battle;
        private readonly GameClock _clock;

        public event Action OnChanged;

        private CampaignSave Save => _data.campaign ?? (_data.campaign = new CampaignSave());

        public CampaignSystem(PetSaveData data, BattleSystem battle, GameClock clock)
        {
            _data = data;
            _battle = battle;
            _clock = clock;
            if (Save.expeditionActive && FindArea(Save.areaId) == null) Save.expeditionActive = false;
        }

        private void Changed() => OnChanged?.Invoke();

        // ---------------------------------------------------------------- the map

        public bool IsCleared(WildAreaDef area) => Save.clearedAreas.Contains(area.Id);

        // The first area is open; every other one opens when the area before it is cleared.
        public bool IsUnlocked(WildAreaDef area)
        {
            int index = Array.IndexOf(Areas, area);
            return index == 0 || (index > 0 && IsCleared(Areas[index - 1]));
        }

        public int ClearedCount => Save.clearedAreas.Count;
        public int WildWins => Save.wildWins;

        // ---------------------------------------------------------------- an expedition

        public bool ExpeditionActive => Save.expeditionActive;
        public WildAreaDef CurrentArea => Save.expeditionActive ? FindArea(Save.areaId) : null;
        public int Step => Save.step;
        public int MaxHp => _battle.MaxHp;
        public int Hp => _battle.Hp;
        public WildStepDef NextStep => Save.expeditionActive && Save.step < CurrentArea.Steps.Length ? CurrentArea.Steps[Save.step] : null;

        public bool Begin(WildAreaDef area, out string message)
        {
            message = "";
            if (area == null || !IsUnlocked(area)) { message = "Clear the area before it first."; return false; }
            if (!_battle.HasStyle) { message = "Pick a fighting style in the Battle Club first."; return false; }
            if (!_battle.CanFight) { message = "Too worn out to set out. Rest first."; return false; }
            Save.expeditionActive = true;
            Save.areaId = area.Id;
            Save.step = 0;
            Save.runs++;
            Changed();
            return true;
        }

        // Going home is always allowed and costs nothing: everything found has already gone into the bag.
        public void GoHome()
        {
            if (!Save.expeditionActive) return;
            Save.expeditionActive = false;
            Changed();
        }

        private string TodayKey => _clock.LocalNow.ToString("yyyy-MM-dd");

        private bool FoundToday(string key)
        {
            if (Save.findsKey != TodayKey) { Save.findsKey = TodayKey; Save.foundToday.Clear(); }
            return Save.foundToday.Contains(key);
        }

        // Searches the next step. A find is picked up on the spot (once a day per spot) and the trail moves on;
        // a wild cat or the boss stops the search until BuildEncounter's battle has been fought.
        public SearchResult Search(string petName)
        {
            var step = NextStep;
            if (step == null) return new SearchResult { Outcome = SearchOutcome.Nothing, Text = "There is nothing further along this trail." };
            if (step.Kind != WildStepKind.Find)
                return new SearchResult { Outcome = SearchOutcome.Encounter, Text = step.Kind == WildStepKind.Boss ? $"{CurrentArea.BossName} blocks the way!" : "Something moves in front of you..." };

            string key = Save.areaId + ":" + Save.step;
            Save.step++;
            if (FoundToday(key))
            {
                Changed();
                return new SearchResult { Outcome = SearchOutcome.PickedClean, Text = "This spot was picked clean earlier today." };
            }
            Save.foundToday.Add(key);
            string text;
            if (!string.IsNullOrEmpty(step.ItemId))
            {
                var item = BattleSystem.FindItem(step.ItemId);
                text = _battle.Give(step.ItemId, 1) > 0 ? $"{petName} found a {item.Name}!" : $"{petName} found a {item.Name}, but the bag is full.";
            }
            else
            {
                _battle.GiveCoins(step.Coins);
                text = $"{petName} found {step.Coins} coins!";
            }
            Changed();
            return new SearchResult { Outcome = SearchOutcome.Found, Text = text };
        }

        // The fight waiting at the current step. The same step of the same expedition always holds the same cat.
        public BattleEncounter BuildEncounter()
        {
            var area = CurrentArea;
            var step = NextStep;
            if (area == null || step == null || step.Kind == WildStepKind.Find) return null;
            bool boss = step.Kind == WildStepKind.Boss;
            var random = new Random(((_data.petId ?? "gotchi").GetHashCode() * 17) ^ (Save.runs * 7919) ^ (Save.step * 104729) ^ area.Id.GetHashCode());
            var styles = new[] { BattleStyle.Claw, BattleStyle.Fluff, BattleStyle.Trick };
            var style = boss ? area.BossStyle : styles[random.Next(styles.Length)];
            int level = boss ? area.BossLevel : random.Next(area.MinLevel, area.MaxLevel + 1);
            int pick = random.Next(area.WildNames.Length);

            var rival = new BattleFighterSetup
            {
                Name = boss ? area.BossName : area.WildNames[pick],
                OwnerName = "",
                Species = area.Species,
                CoatId = boss ? area.BossCoat : area.Coats[pick % area.Coats.Length],
                Level = level,
                Style = style,
                Stats = _battle.StatsFor(level, area.Ranks + (boss ? 2 : 0), style),
                SmartAi = boss,
            };
            rival.MoveIds.Add("scratch");
            foreach (var move in BattleSystem.Moves) if (move.StarterFor == style) rival.MoveIds.Add(move.Id);
            BattleMoveDef big = null;
            if (boss || area.Tier >= 3)
                foreach (var move in BattleSystem.Moves)
                    if (move.Style == style && move.Power >= 70 && move.League <= (boss ? 3 : 1) && (big == null || move.Power > big.Power)) big = move;
            rival.MoveIds.Add(big != null ? big.Id : "pounce");
            rival.MoveIds.Add("hiss");

            return new BattleEncounter
            {
                Kind = boss ? BattleKind.Boss : BattleKind.Wild,
                Rival = rival,
                Arena = area.Scenery,
                IntroLine = boss ? $"{area.BossName}, who runs the {area.Name}, steps out!" : $"A {area.WildNames[pick]} jumps out of hiding!",
                Finish = FinishEncounter,
            };
        }

        // Books the fight at the current step: what the cat has left, where the trail goes, what it paid.
        public BattleRewards FinishEncounter(BattleReport report)
        {
            var area = CurrentArea;
            var step = NextStep;
            if (area == null || step == null) return new BattleRewards();
            bool boss = step.Kind == WildStepKind.Boss;

            int coins = 0, xp = 5, hearts = 0;
            var lines = new List<string>();
            if (report.Won)
            {
                Save.wildWins++;
                coins = area.WildCoins;
                xp = boss ? area.BossXp : area.WildXp;
                if (boss)
                {
                    bool first = !IsCleared(area);
                    if (first) Save.clearedAreas.Add(area.Id);
                    coins = first ? area.ClearCoins : area.RepeatCoins;
                    hearts = first ? area.ClearHearts : 0;
                    int index = Array.IndexOf(Areas, area);
                    string next = index + 1 < Areas.Length ? $" {Areas[index + 1].Name} is open." : " That was the last area on the map.";
                    lines.Add(first ? $"{area.Name} cleared!{next}" : $"{area.Name} cleared again.");
                    Save.expeditionActive = false;
                }
                else Save.step++;
            }
            else if (report.Ran)
            {
                // Running leaves a wild cat behind and the trail goes on; a boss stays where it is.
                if (!boss) Save.step++;
                lines.Add(boss ? $"{area.BossName} is still waiting." : "Got away safely.");
            }
            else
            {
                Save.expeditionActive = false;
                lines.Add("Carried home to rest. Everything found is kept.");
            }

            var rewards = _battle.FinishWild(report, coins, xp, hearts);
            rewards.Lines.InsertRange(0, lines);
            if (hearts > 0) rewards.Lines.Insert(1, $"First clear: +{hearts} hearts");
            Changed();
            return rewards;
        }

        // Between fights the bag can patch the cat up: treats and tuna heal, warm milk restores mana.
        public bool UseItem(BattleItemDef item, out string message)
        {
            message = "";
            return Save.expeditionActive && _battle.TryUseAtHome(item, out message);
        }
    }
}
