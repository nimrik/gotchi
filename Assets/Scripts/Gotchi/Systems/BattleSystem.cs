using System;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Economy;

namespace Gotchi.Systems
{
    // ---------------------------------------------------------------------------------------------------------
    // Battle Club rules (the PvP branch). Design and numbers: 13-pvp-design.md.
    //
    // The cat is built for battle along five lines, all paid for with COINS and time, never with hearts:
    //   style   CLAW / FLUFF / TRICK — the cat's battle type. Claw beats Trick, Trick beats Fluff, Fluff beats Claw.
    //   stats   four training ranks (HP, Attack, Defense, Speed), capped by the pet's level, so care feeds power.
    //   moves   a pool of 15; four are carried. Bought in the club, gated by league.
    //   items   consumables carried into battle (3 uses per battle).
    //   charm   one held item with a passive bonus; the two best ones are league promotion rewards.
    // On top of that the CAMP prepares the cat (Systems/CampSystem.cs): feeding it buys ATTACK for the next few
    // battles, grooming buys DEFENSE, resting and focusing bring health and mana back. The build also gives the
    // cat a LEANING, the label on its status block (Fighter, Guardian, Shadow ...): what its style and training add up to.
    //
    // Progress is a rating ladder with five leagues (ranked battles against other keepers' cats) and the Wild
    // (campaign, Systems/CampaignSystem.cs). Hearts only flow OUT of battles (promotion rewards, the daily chest,
    // first clears) and can only be spent on looks (arenas): nothing sold for hearts changes who wins.
    // Rewards are fixed; only hit rolls inside a battle and which wild cat turns up are random. No paid chance.
    //
    // What a fight costs is HEALTH and MANA (in blocks of 250 on screen, so the numbers are in the thousands). Every move except the basic SCRATCH spends mana, and both bars
    // stay where the fight left them: back home the cat has to rest to get them back (REST and FOCUS in the camp,
    // a treat, a bag item, or simply time, online or not). Items can be bought, sold back at half price, and swapped on the trade board.
    // ---------------------------------------------------------------------------------------------------------

    public enum MoveEffect { None, LowerAttack, LowerDefense, LowerSpeed, RaiseAttack, RaiseDefense, RaiseSpeed, Heal }

    public class BattleMoveDef
    {
        public string Id, Name, Description;
        public BattleStyle Style;
        public int Power;            // 0 = status move
        public int Accuracy = 100;   // status moves that target the user never miss
        public int Mana;             // mana the move costs; SCRATCH is free, so a cat can always do something
        public MoveEffect Effect;
        public int Amount;           // stat stages, or percent of max HP for Heal
        public bool Priority;        // goes first whatever the speed
        public bool HighCrit;
        public int RecoilPercent;
        public int League;           // league index needed to learn it
        public int Cost;             // coins; 0 = part of the starter set
        public BattleStyle StarterFor = BattleStyle.Normal;   // given free with that style
    }

    public enum BattleItemEffect { Heal, RaiseAttack, Restore }

    public class BattleItemDef
    {
        public string Id, Name, Description;
        public BattleItemEffect Effect;
        public int Amount;
        public int Cost;
    }

    public enum CharmEffect { Hp, Attack, Defense, Speed, Coins, Regen, Endure }

    public class BattleCharmDef
    {
        public string Id, Name, Description;
        public CharmEffect Effect;
        public int Amount;           // percent
        public int Cost;             // coins; 0 = cannot be bought
        public int RewardLeague = -1; // league index that hands it out on promotion
    }

    public class LeagueDef
    {
        public string Id, Name;
        public int MinRating;
        public float CoinMultiplier;
        public int RewardCoins, RewardHearts;
        public string RewardCharmId, RewardArenaId;
        public int RivalLevelMin, RivalLevelMax;   // offset from the player's level
        public int RivalRanks;                     // training ranks the rival has in every stat
        public bool SmartAi;
    }

    public class ArenaDef
    {
        public string Id, Name, Description;
        public CurrencyType Currency;
        public int Cost;             // 0 = free or reward only
        public bool RewardOnly;
        public string SkyTop, SkyBottom, Ground, Platform;   // hex colours
    }

    public enum BattleQuestKind { Wins, SuperEffective, NoItems, StyleMoves, HealthyWin, Battles }

    public class BattleQuestDef
    {
        public string Id, Text;
        public BattleQuestKind Kind;
        public int Goal;
        public int RewardCoins;
    }

    public struct BattleStats
    {
        public int MaxHp, MaxMp, Attack, Defense, Speed;
        public float CritChance;
    }

    public class BattleFighterSetup
    {
        public string Name, OwnerName, CoatId = "";
        public SpeciesType Species = SpeciesType.Cat;   // one character today; wild areas and keepers get more later
        public int Level;
        public BattleStyle Style;
        public BattleStats Stats;
        public List<string> MoveIds = new List<string>();
        public string CharmId = "";
        public bool SmartAi;
        public int Rating;
    }

    // What happened in one battle; filled in by BattleMiniGame and turned into rewards by the encounter's Finish.
    public class BattleReport
    {
        public bool Won, Ran;
        public int DamageDealt, SuperEffectiveHits, StyleMovesUsed, ItemsUsed, HpLeft, MaxHp;
        public int MpLeft = -1;      // mana left; below zero = not reported (tests), leave the saved mana alone
    }

    public enum BattleKind { Ranked, Wild, Boss }

    // One fight, as handed to BattleMiniGame: who the rival is, where it happens and who books the result. Null in
    // MiniGameContext means a ranked battle against BattleSystem.NextRival(). Whatever the fight, the cat walks in
    // with the health and mana it has (BattleSystem.Hp / Mp) and walks out with what is left.
    public class BattleEncounter
    {
        public BattleKind Kind;
        public BattleFighterSetup Rival;
        public ArenaDef Arena;
        public string IntroLine;
        public Func<BattleReport, BattleRewards> Finish;
    }

    public class TradeOffer
    {
        public string Id, Keeper;
        public string GiveItemId, GetItemId;     // the player gives GiveCount of one item and gets GetCount of the other
        public int GiveCount, GetCount;
    }

    public class BattleRewards
    {
        public int Coins, Xp, Hearts, RatingDelta, NewRating;
        public LeagueDef PromotedTo;
        public readonly List<string> Lines = new List<string>();
    }

    public class RivalIdentity
    {
        public string OwnerName, PetName, CoatId;
        public int Rating;
    }

    // What the camp lends the cat for its next battles (CampSystem implements it; null = nothing).
    public interface IBattleBuffs
    {
        float Multiplier(BattleStat stat);
        List<BattleSystem.ConditionLine> Lines();
        void OnBattleFought();
    }

    // What the cat's build adds up to: shown on the status block where the mood used to be.
    public class BattleLeaning
    {
        public string Name, Description;
        public BattleStyle Style;
    }

    public class BattleSystem
    {
        public const int MaxRank = 10;
        public const int MaxCarry = 9;
        public const int MaxItemsPerBattle = 3;
        public const int LoadoutSize = 4;
        public const int RespecCost = 150;
        public const int FirstWinBonus = 50;
        public const int DailyChestHearts = 3;
        public const int QuestsPerDay = 3;
        public const int TradesPerDay = 3;
        public const float FullRegenSeconds = 600f;  // doing nothing at home (or away) refills both bars in ten minutes
        public const float FightThreshold = 0.10f;   // under a tenth of its health the cat is worn out and will not fight
        public const float DamageScale = 40f;        // health is counted in the thousands (a bar block is 250), so damage is too
        public const float TrainStep = 0.05f;        // each rank adds 5% of the base stat
        public const float StyleBonus = 0.10f;
        public const float BaseCrit = 1f / 16f;
        public const float Stab = 1.5f;              // a move of the cat's own style

        // ---------------------------------------------------------------- catalog

        public static readonly BattleMoveDef[] Moves =
        {
            new BattleMoveDef { Id = "scratch",    Name = "SCRATCH",      Style = BattleStyle.Normal, Power = 40, Mana = 0, Description = "A plain swipe. Costs no mana, so it never lets you down." },
            new BattleMoveDef { Id = "pounce",     Name = "POUNCE",       Style = BattleStyle.Normal, Power = 65, Accuracy = 85, Mana = 160, Description = "A big leap. Strong, but it can miss." },
            new BattleMoveDef { Id = "hiss",       Name = "HISS",         Style = BattleStyle.Normal, Mana = 80, Effect = MoveEffect.LowerAttack, Amount = 1, Description = "Scares the rival: its ATTACK falls." },
            new BattleMoveDef { Id = "quick_paw",  Name = "QUICK PAW",    Style = BattleStyle.Normal, Power = 35, Mana = 120, Priority = true, League = 0, Cost = 120, Description = "Always strikes first." },
            new BattleMoveDef { Id = "catnap",     Name = "CATNAP",       Style = BattleStyle.Normal, Mana = 320, Effect = MoveEffect.Heal, Amount = 45, League = 1, Cost = 250, Description = "A quick nap restores 45% HP." },

            new BattleMoveDef { Id = "claw_swipe", Name = "CLAW SWIPE",   Style = BattleStyle.Claw, Power = 50, Mana = 120, League = 0, Cost = 150, StarterFor = BattleStyle.Claw, Description = "The Claw basic. Strong against TRICK." },
            new BattleMoveDef { Id = "fury_claws", Name = "FURY CLAWS",   Style = BattleStyle.Claw, Power = 75, Accuracy = 90, Mana = 240, HighCrit = true, League = 1, Cost = 300, Description = "A flurry that often lands a critical hit." },
            new BattleMoveDef { Id = "frenzy",     Name = "FRENZY",       Style = BattleStyle.Claw, Power = 95, Accuracy = 85, Mana = 320, RecoilPercent = 25, League = 3, Cost = 600, Description = "All-out attack. The user is hurt by a quarter of the damage." },

            new BattleMoveDef { Id = "fluff_bump", Name = "FLUFF BUMP",   Style = BattleStyle.Fluff, Power = 50, Mana = 120, League = 0, Cost = 150, StarterFor = BattleStyle.Fluff, Description = "The Fluff basic. Strong against CLAW." },
            new BattleMoveDef { Id = "fluff_up",   Name = "FLUFF UP",     Style = BattleStyle.Fluff, Mana = 160, Effect = MoveEffect.RaiseDefense, Amount = 1, League = 0, Cost = 120, Description = "Puffs the fur out: DEFENSE rises." },
            new BattleMoveDef { Id = "body_slam",  Name = "BODY SLAM",    Style = BattleStyle.Fluff, Power = 75, Accuracy = 90, Mana = 240, League = 1, Cost = 300, Description = "The whole cat, all at once." },

            new BattleMoveDef { Id = "sneak",      Name = "SNEAK ATTACK", Style = BattleStyle.Trick, Power = 50, Mana = 120, League = 0, Cost = 150, StarterFor = BattleStyle.Trick, Description = "The Trick basic. Strong against FLUFF." },
            new BattleMoveDef { Id = "surprise",   Name = "SURPRISE!",    Style = BattleStyle.Trick, Power = 80, Accuracy = 80, Mana = 280, HighCrit = true, League = 1, Cost = 300, Description = "From behind the sofa. Misses now and then, hits hard." },
            new BattleMoveDef { Id = "yarn_trap",  Name = "YARN TRAP",    Style = BattleStyle.Trick, Power = 40, Accuracy = 95, Mana = 200, Effect = MoveEffect.LowerSpeed, Amount = 1, League = 2, Cost = 400, Description = "Tangles the rival: damage, and its SPEED falls." },
            new BattleMoveDef { Id = "zoomies",    Name = "ZOOMIES",      Style = BattleStyle.Trick, Mana = 200, Effect = MoveEffect.RaiseSpeed, Amount = 2, League = 2, Cost = 350, Description = "A lap around the room: SPEED rises sharply." },
        };

        public static readonly string[] StarterMoves = { "scratch", "pounce", "hiss" };

        public static readonly BattleItemDef[] Items =
        {
            new BattleItemDef { Id = "treat",  Name = "Fish Treat", Description = "Restores 40% HP.", Effect = BattleItemEffect.Heal, Amount = 40, Cost = 30 },
            new BattleItemDef { Id = "tuna",   Name = "Big Tuna",   Description = "Restores all HP.", Effect = BattleItemEffect.Heal, Amount = 100, Cost = 90 },
            new BattleItemDef { Id = "catnip", Name = "Catnip",     Description = "ATTACK rises sharply.", Effect = BattleItemEffect.RaiseAttack, Amount = 2, Cost = 60 },
            new BattleItemDef { Id = "milk",   Name = "Warm Milk",  Description = "Restores all mana and undoes lowered stats.", Effect = BattleItemEffect.Restore, Cost = 50 },
        };

        public static readonly BattleCharmDef[] Charms =
        {
            new BattleCharmDef { Id = "spike",    Name = "Spiked Collar",    Description = "+10% ATTACK.",  Effect = CharmEffect.Attack,  Amount = 10, Cost = 300 },
            new BattleCharmDef { Id = "vest",     Name = "Padded Vest",      Description = "+10% DEFENSE.", Effect = CharmEffect.Defense, Amount = 10, Cost = 300 },
            new BattleCharmDef { Id = "bell",     Name = "Bell Collar",      Description = "+10% SPEED.",   Effect = CharmEffect.Speed,   Amount = 10, Cost = 300 },
            new BattleCharmDef { Id = "locket",   Name = "Heart Locket",     Description = "+10% HP.",      Effect = CharmEffect.Hp,      Amount = 10, Cost = 300 },
            new BattleCharmDef { Id = "lucky",    Name = "Lucky Coin",       Description = "+25% coins from every battle.", Effect = CharmEffect.Coins, Amount = 25, Cost = 500 },
            new BattleCharmDef { Id = "fishbone", Name = "Leftover Fishbone", Description = "Restores 6% HP after every turn. Silver League reward.", Effect = CharmEffect.Regen, Amount = 6, RewardLeague = 1 },
            new BattleCharmDef { Id = "ribbon",   Name = "Focus Ribbon",     Description = "Once per battle, survives a knockout hit with 1 HP. Gold League reward.", Effect = CharmEffect.Endure, Amount = 1, RewardLeague = 2 },
        };

        public static readonly LeagueDef[] Leagues =
        {
            new LeagueDef { Id = "bronze",   Name = "Bronze",   MinRating = 0,    CoinMultiplier = 1f,    RivalLevelMin = -1, RivalLevelMax = 0, RivalRanks = 0, SmartAi = false },
            new LeagueDef { Id = "silver",   Name = "Silver",   MinRating = 200,  CoinMultiplier = 1.25f, RewardCoins = 100, RewardHearts = 5,  RewardCharmId = "fishbone", RivalLevelMin = 0, RivalLevelMax = 0, RivalRanks = 2, SmartAi = true },
            new LeagueDef { Id = "gold",     Name = "Gold",     MinRating = 500,  CoinMultiplier = 1.5f,  RewardCoins = 200, RewardHearts = 10, RewardCharmId = "ribbon",   RivalLevelMin = 0, RivalLevelMax = 1, RivalRanks = 4, SmartAi = true },
            new LeagueDef { Id = "crystal",  Name = "Crystal",  MinRating = 900,  CoinMultiplier = 1.75f, RewardCoins = 300, RewardHearts = 15, RewardArenaId = "arena_roof", RivalLevelMin = 1, RivalLevelMax = 1, RivalRanks = 6, SmartAi = true },
            new LeagueDef { Id = "champion", Name = "Champion", MinRating = 1400, CoinMultiplier = 2f,    RewardCoins = 500, RewardHearts = 25, RivalLevelMin = 1, RivalLevelMax = 2, RivalRanks = 8, SmartAi = true },
        };

        public static readonly ArenaDef[] Arenas =
        {
            new ArenaDef { Id = "arena_dojo",   Name = "Rug Dojo",     Description = "The living-room rug. Where it all starts.", SkyTop = "FFF1E2", SkyBottom = "FFE1D0", Ground = "F6D9C4", Platform = "DFF3E4" },
            new ArenaDef { Id = "arena_garden", Name = "Back Garden",  Description = "Grass, a fence and a very blue sky.", Currency = CurrencyType.Soft, Cost = 400, SkyTop = "8FD3F7", SkyBottom = "DDF3FF", Ground = "A8DFB8", Platform = "C9EFC7" },
            new ArenaDef { Id = "arena_beach",  Name = "Sunset Beach", Description = "Warm sand under an orange sky.", Currency = CurrencyType.Premium, Cost = 30, SkyTop = "FF9E7A", SkyBottom = "FFE0B0", Ground = "FBE8C2", Platform = "FFF3D6" },
            new ArenaDef { Id = "arena_roof",   Name = "Moonlit Roof", Description = "Rooftops at night. Crystal League reward.", RewardOnly = true, SkyTop = "1B1840", SkyBottom = "4A3F7A", Ground = "3B3A6B", Platform = "59579A" },
        };

        public static readonly BattleQuestDef[] Quests =
        {
            new BattleQuestDef { Id = "q_win1",    Text = "Win a battle",                     Kind = BattleQuestKind.Wins, Goal = 1, RewardCoins = 30 },
            new BattleQuestDef { Id = "q_battle3", Text = "Finish 3 battles",                 Kind = BattleQuestKind.Battles, Goal = 3, RewardCoins = 40 },
            new BattleQuestDef { Id = "q_super",   Text = "Land 3 hits with a strong match-up", Kind = BattleQuestKind.SuperEffective, Goal = 3, RewardCoins = 40 },
            new BattleQuestDef { Id = "q_noitems", Text = "Win without using an item",        Kind = BattleQuestKind.NoItems, Goal = 1, RewardCoins = 50 },
            new BattleQuestDef { Id = "q_style",   Text = "Use moves of your own style 6 times", Kind = BattleQuestKind.StyleMoves, Goal = 6, RewardCoins = 40 },
            new BattleQuestDef { Id = "q_healthy", Text = "Win with more than half your HP",  Kind = BattleQuestKind.HealthyWin, Goal = 1, RewardCoins = 50 },
            new BattleQuestDef { Id = "q_win3",    Text = "Win 3 battles",                    Kind = BattleQuestKind.Wins, Goal = 3, RewardCoins = 80 },
        };

        public static BattleMoveDef FindMove(string id) { foreach (var m in Moves) if (m.Id == id) return m; return null; }
        public static BattleItemDef FindItem(string id) { foreach (var i in Items) if (i.Id == id) return i; return null; }
        public static BattleCharmDef FindCharm(string id) { foreach (var c in Charms) if (c.Id == id) return c; return null; }
        public static ArenaDef FindArena(string id) { foreach (var a in Arenas) if (a.Id == id) return a; return Arenas[0]; }
        public static BattleQuestDef FindQuest(string id) { foreach (var q in Quests) if (q.Id == id) return q; return null; }

        // CLAW beats TRICK, TRICK beats FLUFF, FLUFF beats CLAW. Normal is neutral both ways.
        public static float Effectiveness(BattleStyle attack, BattleStyle defend)
        {
            if (attack == BattleStyle.Normal || defend == BattleStyle.Normal || attack == defend) return 1f;
            bool strong = (attack == BattleStyle.Claw && defend == BattleStyle.Trick)
                       || (attack == BattleStyle.Trick && defend == BattleStyle.Fluff)
                       || (attack == BattleStyle.Fluff && defend == BattleStyle.Claw);
            return strong ? 2f : 0.5f;
        }

        public static BattleStyle StrongAgainst(BattleStyle style) =>
            style == BattleStyle.Claw ? BattleStyle.Trick : style == BattleStyle.Trick ? BattleStyle.Fluff : style == BattleStyle.Fluff ? BattleStyle.Claw : BattleStyle.Normal;

        public static BattleStyle WeakAgainst(BattleStyle style) =>
            style == BattleStyle.Claw ? BattleStyle.Fluff : style == BattleStyle.Fluff ? BattleStyle.Trick : style == BattleStyle.Trick ? BattleStyle.Claw : BattleStyle.Normal;

        public static string StyleBlurb(BattleStyle style)
        {
            switch (style)
            {
                case BattleStyle.Claw: return "Fierce. +10% ATTACK. Beats TRICK, loses to FLUFF.";
                case BattleStyle.Fluff: return "Sturdy. +10% DEFENSE. Beats CLAW, loses to TRICK.";
                case BattleStyle.Trick: return "Sly. +10% SPEED. Beats FLUFF, loses to CLAW.";
                default: return "No style yet.";
            }
        }

        public static float StageMultiplier(int stage) => stage >= 0 ? (2f + stage) / 2f : 2f / (2f - stage);

        // Coins for the next rank: 40, 55, 80, 110, 155, 215, 300, 420, 590, 825 (x1.4 each, rounded to 5).
        public static int TrainCostForRank(int rank) => (int)(Math.Round(40.0 * Math.Pow(1.4, rank) / 5.0) * 5.0);

        public static BattleStats BaseStats(int level)
        {
            level = Math.Max(1, level);
            return new BattleStats { MaxHp = 1000 + level * 100, MaxMp = 1000 + level * 100, Attack = 10 + level * 2, Defense = 9 + level * 2, Speed = 8 + level * 2, CritChance = BaseCrit };
        }

        // Damage before style, match-up, critical and the random spread. The classic level/power/attack/defense
        // shape, scaled so that a hit on a 1100-health cat is a few hundred.
        public static float BaseDamage(int level, int power, float attack, float defense) =>
            (((2f * level / 5f + 2f) * power * attack / Math.Max(1f, defense)) / 25f + 2f) * DamageScale;


        // ---------------------------------------------------------------- state

        private readonly PetSaveData _data;
        private readonly CurrencyWallet _wallet;
        private readonly LevelSystem _level;
        private readonly GameClock _clock;

        // Supplies other players' cats to fight (the mock leaderboard today, a server later): (rating, seed) → identity.
        public Func<int, int, RivalIdentity> RivalSource;

        public event Action OnChanged;

        private BattleSave Save => _data.battle ?? (_data.battle = new BattleSave());

        // What the camp lends the cat (fed, groomed). Set after construction, because the camp needs this system too.
        public IBattleBuffs Buffs;

        public BattleSystem(PetSaveData data, CurrencyWallet wallet, LevelSystem level, GameClock clock)
        {
            _data = data;
            _wallet = wallet;
            _level = level;
            _clock = clock;
            RollDailyIfNeeded();
        }

        private void Changed() => OnChanged?.Invoke();

        // ---------------------------------------------------------------- style

        public bool HasStyle => Style != BattleStyle.Normal;

        public BattleStyle Style => Enum.TryParse(Save.style, out BattleStyle parsed) ? parsed : BattleStyle.Normal;

        // The first pick is free and comes with the starter kit; changing style later costs coins.
        public bool ChooseStyle(BattleStyle style, out string message)
        {
            message = "";
            if (style == BattleStyle.Normal) { message = "Pick Claw, Fluff or Trick."; return false; }
            if (style == Style) { message = "Already your style."; return false; }
            bool first = !HasStyle;
            if (!first && !_wallet.TrySpend(CurrencyType.Soft, RespecCost)) { message = $"Changing style costs {RespecCost} coins."; return false; }

            Save.style = style.ToString();
            foreach (string id in StarterMoves) Own(id);
            BattleMoveDef basic = null;
            foreach (var move in Moves) if (move.StarterFor == style) basic = move;
            if (basic != null) Own(basic.Id);

            if (first)
            {
                Save.loadout.Clear();
                Save.loadout.AddRange(StarterMoves);
                if (basic != null) Save.loadout.Insert(1, basic.Id);
                if (!Save.starterKitGranted) { Save.starterKitGranted = true; AddItem("treat", 3); }
                if (!Save.ownedArenas.Contains(Arenas[0].Id)) Save.ownedArenas.Add(Arenas[0].Id);
                if (string.IsNullOrEmpty(Save.arenaId)) Save.arenaId = Arenas[0].Id;
            }
            else if (basic != null && !Save.loadout.Contains(basic.Id))
            {
                // Swap the old style's basic for the new one so the cat is never left without a move of its style.
                int slot = Save.loadout.FindIndex(id => { var m = FindMove(id); return m != null && m.StarterFor != BattleStyle.Normal; });
                if (slot >= 0) Save.loadout[slot] = basic.Id;
                else if (Save.loadout.Count < LoadoutSize) Save.loadout.Add(basic.Id);
            }
            message = first ? $"{style} style chosen. Starter moves and 3 Fish Treats are yours." : $"Now a {style} cat.";
            Changed();
            return true;
        }

        // ---------------------------------------------------------------- training

        public int Rank(BattleStat stat)
        {
            switch (stat)
            {
                case BattleStat.Hp: return Save.hpRank;
                case BattleStat.Attack: return Save.attackRank;
                case BattleStat.Defense: return Save.defenseRank;
                default: return Save.speedRank;
            }
        }

        private void SetRank(BattleStat stat, int value)
        {
            switch (stat)
            {
                case BattleStat.Hp: Save.hpRank = value; break;
                case BattleStat.Attack: Save.attackRank = value; break;
                case BattleStat.Defense: Save.defenseRank = value; break;
                default: Save.speedRank = value; break;
            }
        }

        // Care sets the ceiling: a rank can never be more than one above the pet's level.
        public int RankCap => Math.Min(MaxRank, _level.Level + 1);

        public int TrainCost(BattleStat stat) => TrainCostForRank(Rank(stat));

        public bool CanTrain(BattleStat stat, out string reason)
        {
            reason = "";
            int rank = Rank(stat);
            if (rank >= MaxRank) { reason = "Fully trained."; return false; }
            if (rank >= RankCap) { reason = $"Reach pet level {rank} first."; return false; }   // cap = level + 1
            if (_wallet.Get(CurrencyType.Soft) < TrainCost(stat)) { reason = "Not enough coins."; return false; }
            return true;
        }

        public bool TryTrain(BattleStat stat)
        {
            if (!CanTrain(stat, out _)) return false;
            if (!_wallet.TrySpend(CurrencyType.Soft, TrainCost(stat))) return false;
            SetRank(stat, Rank(stat) + 1);
            Changed();
            return true;
        }

        // ---------------------------------------------------------------- stats and condition

        public struct ConditionLine { public string Text; public bool Good; }

        // ---------------------------------------------------------------- leaning

        // What the build adds up to, shown on the status block. The style says HOW the cat fights, the most trained
        // stat says what it is good at; together they name it. With no style it is a Rookie; with every stat trained
        // evenly it is an All-rounder. A label only: it changes nothing in a fight, it tells the player (and later
        // other players) what kind of cat this is.
        private static readonly string[,] LeaningNames =
        {
            //  HP           Attack       Defense       Speed
            { "Brawler",   "Fighter",   "Bruiser",    "Striker"   },   // Claw
            { "Tank",      "Crusher",   "Guardian",   "Bouncer"   },   // Fluff
            { "Survivor",  "Ambusher",  "Trickster",  "Shadow"    },   // Trick
        };

        public BattleLeaning Leaning
        {
            get
            {
                var style = Style;
                if (style == BattleStyle.Normal) return new BattleLeaning { Name = "Rookie", Style = style, Description = "No fighting style yet. Pick one in BATTLE." };
                int[] ranks = { Save.hpRank, Save.attackRank, Save.defenseRank, Save.speedRank };
                int max = Math.Max(Math.Max(ranks[0], ranks[1]), Math.Max(ranks[2], ranks[3])), min = Math.Min(Math.Min(ranks[0], ranks[1]), Math.Min(ranks[2], ranks[3]));
                if (max >= 3 && max - min <= 1) return new BattleLeaning { Name = "All-rounder", Style = style, Description = $"A {style} cat trained evenly in everything." };
                // The most trained stat; the style's own stat wins a tie (and names an untrained cat).
                int own = style == BattleStyle.Claw ? 1 : style == BattleStyle.Fluff ? 2 : 3, stat = own;
                for (int i = 0; i < ranks.Length; i++) if (ranks[i] > ranks[stat]) stat = i;
                string[] statNames = { "health", "attack", "defense", "speed" };
                return new BattleLeaning
                {
                    Name = LeaningNames[(int)style - 1, stat], Style = style,
                    Description = max == 0 ? $"A {style} cat with no training yet. Training decides what it leans to." : $"A {style} cat trained mostly for {statNames[stat]}.",
                };
            }
        }

        // What the camp is lending the cat right now ("Fed: ATK +10% · 2 battles left"). Shown in the club.
        public List<ConditionLine> Conditions() => Buffs != null ? Buffs.Lines() : new List<ConditionLine>();

        // Stats with training only (what the Train page shows as the permanent numbers).
        public BattleStats TrainedStats() => Compose(_level.Level, Save.hpRank, Save.attackRank, Save.defenseRank, Save.speedRank, Style, Save.equippedCharm, false);

        // Stats as they enter the next battle: training, style, charm and today's condition.
        public BattleStats CurrentStats() => Compose(_level.Level, Save.hpRank, Save.attackRank, Save.defenseRank, Save.speedRank, Style, Save.equippedCharm, true);

        // Stats of any other cat (a keeper's, or a wild one): level, the same rank in every stat, its style.
        public BattleStats StatsFor(int level, int ranks, BattleStyle style) => Compose(level, ranks, ranks, ranks, ranks, style, "", false);

        private BattleStats Compose(int level, int hpRank, int attackRank, int defenseRank, int speedRank, BattleStyle style, string charmId, bool withCondition)
        {
            var b = BaseStats(level);
            float hp = 1f + hpRank * TrainStep, attack = 1f + attackRank * TrainStep, defense = 1f + defenseRank * TrainStep, speed = 1f + speedRank * TrainStep;
            if (style == BattleStyle.Claw) attack += StyleBonus;
            if (style == BattleStyle.Fluff) defense += StyleBonus;
            if (style == BattleStyle.Trick) speed += StyleBonus;
            var charm = FindCharm(charmId);
            if (charm != null)
            {
                if (charm.Effect == CharmEffect.Hp) hp += charm.Amount / 100f;
                if (charm.Effect == CharmEffect.Attack) attack += charm.Amount / 100f;
                if (charm.Effect == CharmEffect.Defense) defense += charm.Amount / 100f;
                if (charm.Effect == CharmEffect.Speed) speed += charm.Amount / 100f;
            }
            float crit = BaseCrit;
            if (withCondition && Buffs != null)
            {
                attack *= Buffs.Multiplier(BattleStat.Attack);
                defense *= Buffs.Multiplier(BattleStat.Defense);
                speed *= Buffs.Multiplier(BattleStat.Speed);
            }
            return new BattleStats
            {
                MaxHp = (int)Math.Round(b.MaxHp * hp), MaxMp = b.MaxMp, Attack = (int)Math.Round(b.Attack * attack),
                Defense = (int)Math.Round(b.Defense * defense), Speed = (int)Math.Round(b.Speed * speed), CritChance = crit,
            };
        }

        // ---------------------------------------------------------------- health and mana between fights

        // Both bars stay where the last fight left them. At home they come back through the camp (REST for health,
        // FOCUS for mana, the Treat for a little of both), a healing item from the bag, and plain time (full in ten
        // minutes, the app open or not).
        public int MaxHp => CurrentStats().MaxHp;
        public int MaxMp => CurrentStats().MaxMp;
        public int Hp { get { UpdateVitals(); return (int)Math.Floor(Save.hp + 0.0001f); } }
        public int Mp { get { UpdateVitals(); return (int)Math.Floor(Save.mp + 0.0001f); } }
        public bool CanFight => Hp >= MaxHp * FightThreshold;   // a worn-out cat lies down until a tenth of its health is back

        // Brings both bars up to date with the clock. Cheap; called by every reader, so nothing has to tick it.
        public void UpdateVitals()
        {
            int maxHp = MaxHp, maxMp = MaxMp;
            long now = _clock.UtcNow.Ticks;
            if (!Save.vitalsStarted)
            {
                Save.vitalsStarted = true;          // a new pet, or a save from before health and mana carried over
                Save.hp = maxHp; Save.mp = maxMp; Save.vitalsUtcTicks = now;
                return;
            }
            double seconds = Math.Max(0d, (now - Save.vitalsUtcTicks) / (double)TimeSpan.TicksPerSecond);
            Save.vitalsUtcTicks = now;
            Save.hp = Math.Min(maxHp, Save.hp + (float)(maxHp * seconds / FullRegenSeconds));
            Save.mp = Math.Min(maxMp, Save.mp + (float)(maxMp * seconds / FullRegenSeconds));
        }

        // What a fight left behind.
        public void SetVitals(int hp, int mp)
        {
            UpdateVitals();
            Save.hp = Math.Max(0, Math.Min(MaxHp, hp));
            if (mp >= 0) Save.mp = Math.Max(0, Math.Min(MaxMp, mp));
            Changed();
        }

        // Resting, focusing, a treat: gives back a share of each maximum.
        public void Recover(float hpShare, float mpShare)
        {
            UpdateVitals();
            Save.hp = Math.Min(MaxHp, Save.hp + MaxHp * hpShare);
            Save.mp = Math.Min(MaxMp, Save.mp + MaxMp * mpShare);
            Changed();
        }

        public void RecoverFully() => Recover(1f, 1f);   // the shop's Full Recovery

        // Seconds until both bars are full again with no help (for the "rested and ready" reminder).
        public double SecondsUntilFull()
        {
            UpdateVitals();
            double hpShare = 1d - Save.hp / Math.Max(1, MaxHp), mpShare = 1d - Save.mp / Math.Max(1, MaxMp);
            return Math.Max(0d, Math.Max(hpShare, mpShare) * FullRegenSeconds);
        }

        // A bag item used outside a fight: treats and tuna heal, warm milk restores mana. Catnip only works in one.
        public bool TryUseAtHome(BattleItemDef item, out string message)
        {
            message = "";
            if (item == null) return false;
            UpdateVitals();
            if (item.Effect == BattleItemEffect.RaiseAttack) { message = "That one only works in a fight."; return false; }
            if (item.Effect == BattleItemEffect.Heal && Hp >= MaxHp) { message = "Already at full health."; return false; }
            if (item.Effect == BattleItemEffect.Restore && Mp >= MaxMp) { message = "Already at full mana."; return false; }
            if (Count(item.Id) <= 0) { message = $"No {item.Name} in the bag."; return false; }
            AddItem(item.Id, -1);
            if (item.Effect == BattleItemEffect.Heal)
            {
                Save.hp = Math.Min(MaxHp, Save.hp + (float)Math.Round(MaxHp * item.Amount / 100.0));
                message = $"Back to {Hp}/{MaxHp} HP.";
            }
            else
            {
                Save.mp = MaxMp;
                message = $"Mana back to {Mp}/{MaxMp}.";
            }
            Changed();
            return true;
        }

        // ---------------------------------------------------------------- moves

        public bool Owns(string moveId) => Save.ownedMoves.Contains(moveId);
        public IReadOnlyList<string> Loadout => Save.loadout;
        public bool InLoadout(string moveId) => Save.loadout.Contains(moveId);

        private void Own(string moveId) { if (!Save.ownedMoves.Contains(moveId)) Save.ownedMoves.Add(moveId); }

        public bool CanLearn(BattleMoveDef move, out string reason)
        {
            reason = "";
            if (move == null) { reason = "Unknown move."; return false; }
            if (Owns(move.Id)) { reason = "Already known."; return false; }
            if (LeagueIndex < move.League) { reason = $"Reach the {Leagues[move.League].Name} League."; return false; }
            if (_wallet.Get(CurrencyType.Soft) < move.Cost) { reason = "Not enough coins."; return false; }
            return true;
        }

        public bool TryLearn(BattleMoveDef move)
        {
            if (!CanLearn(move, out _)) return false;
            if (!_wallet.TrySpend(CurrencyType.Soft, move.Cost)) return false;
            Own(move.Id);
            if (Save.loadout.Count < LoadoutSize) Save.loadout.Add(move.Id);
            Changed();
            return true;
        }

        // Puts a known move into the loadout (replacing `replaceId` when it is full) or takes it out. One move stays.
        public bool ToggleLoadout(string moveId, string replaceId = null)
        {
            if (!Owns(moveId)) return false;
            if (Save.loadout.Contains(moveId))
            {
                if (Save.loadout.Count <= 1) return false;
                Save.loadout.Remove(moveId);
            }
            else if (Save.loadout.Count < LoadoutSize) Save.loadout.Add(moveId);
            else
            {
                int slot = replaceId != null ? Save.loadout.IndexOf(replaceId) : -1;
                if (slot < 0) return false;
                Save.loadout[slot] = moveId;
            }
            Changed();
            return true;
        }

        // ---------------------------------------------------------------- items

        public int Count(string itemId)
        {
            foreach (var entry in Save.items) if (entry.id == itemId) return entry.count;
            return 0;
        }

        private void AddItem(string itemId, int delta)
        {
            for (int i = 0; i < Save.items.Count; i++)
            {
                if (Save.items[i].id != itemId) continue;
                Save.items[i] = new ItemCount { id = itemId, count = Math.Max(0, Math.Min(MaxCarry, Save.items[i].count + delta)) };
                return;
            }
            if (delta > 0) Save.items.Add(new ItemCount { id = itemId, count = Math.Min(MaxCarry, delta) });
        }

        public bool CanBuyItem(BattleItemDef item, out string reason)
        {
            reason = "";
            if (Count(item.Id) >= MaxCarry) { reason = $"The bag holds {MaxCarry}."; return false; }
            if (_wallet.Get(CurrencyType.Soft) < item.Cost) { reason = "Not enough coins."; return false; }
            return true;
        }

        public bool TryBuyItem(BattleItemDef item)
        {
            if (!CanBuyItem(item, out _)) return false;
            if (!_wallet.TrySpend(CurrencyType.Soft, item.Cost)) return false;
            AddItem(item.Id, 1);
            Changed();
            return true;
        }

        public bool Consume(string itemId)
        {
            if (Count(itemId) <= 0) return false;
            AddItem(itemId, -1);
            Changed();
            return true;
        }

        // Found in the Wild or handed over in a trade. Returns how many fitted in the bag.
        public int Give(string itemId, int count)
        {
            if (FindItem(itemId) == null || count <= 0) return 0;
            int before = Count(itemId);
            AddItem(itemId, count);
            Changed();
            return Count(itemId) - before;
        }

        public void GiveCoins(int coins) { if (coins > 0) _wallet.Add(CurrencyType.Soft, coins); }

        // The Market buys things back at half price.
        public static int SellPrice(int cost) => cost / 2;

        public bool TrySellItem(BattleItemDef item)
        {
            if (item == null || Count(item.Id) <= 0) return false;
            AddItem(item.Id, -1);
            _wallet.Add(CurrencyType.Soft, SellPrice(item.Cost));
            Changed();
            return true;
        }

        // Only charms that were bought can be sold (promotion rewards are keepsakes), and not the one being held.
        public bool CanSellCharm(BattleCharmDef charm) => charm != null && charm.Cost > 0 && OwnsCharm(charm.Id) && EquippedCharm != charm.Id;

        public bool TrySellCharm(BattleCharmDef charm)
        {
            if (!CanSellCharm(charm)) return false;
            Save.ownedCharms.Remove(charm.Id);
            _wallet.Add(CurrencyType.Soft, SellPrice(charm.Cost));
            Changed();
            return true;
        }

        // ---------------------------------------------------------------- trade board

        private static readonly string[] TradeKeepers = { "Luna Lee", "Pebble Po", "Fern Finn", "Kiwi Kai", "Toffee Tom", "Poppy Pat", "Maple Moe" };

        // Three swaps a day with other keepers, the same for everyone that day, each good once. An offer is worth
        // about the same on both sides, which beats selling at half price and buying at full. The board is a mock
        // of what player-to-player trading will be; the seam for the real thing is this method and TryTrade.
        public List<TradeOffer> TodayTrades()
        {
            RollDailyIfNeeded();
            int day = _clock.LocalNow.DayOfYear;
            var offers = new List<TradeOffer>();
            for (int i = 0; i < TradesPerDay; i++)
            {
                var get = Items[(day + i * 3) % Items.Length];
                var give = Items[(day + i * 3 + 1 + i % 2) % Items.Length];
                if (give.Id == get.Id) give = Items[(day + i * 3 + 2) % Items.Length];
                FairCounts(give.Cost, get.Cost, out int giveCount, out int getCount);
                offers.Add(new TradeOffer { Id = TodayKey + "#" + i, Keeper = TradeKeepers[(day * 3 + i) % TradeKeepers.Length], GiveItemId = give.Id, GiveCount = giveCount, GetItemId = get.Id, GetCount = getCount });
            }
            return offers;
        }

        // The smallest swap in which the player never gives more value than they get, and gets at most 35% more.
        private static void FairCounts(int giveCost, int getCost, out int giveCount, out int getCount)
        {
            giveCount = getCount = 1;
            int bestSize = int.MaxValue, bestGap = int.MaxValue;
            for (int g = 1; g <= 4; g++)
                for (int t = 1; t <= 3; t++)
                {
                    int given = giveCost * g, got = getCost * t;
                    if (given > got || got > given * 1.35f) continue;
                    if (g + t < bestSize || (g + t == bestSize && got - given < bestGap)) { bestSize = g + t; bestGap = got - given; giveCount = g; getCount = t; }
                }
        }

        public bool TradeDone(TradeOffer offer) => Save.tradesKey == TodayKey && Save.tradesDone.Contains(offer.Id);

        public bool CanTrade(TradeOffer offer, out string reason)
        {
            reason = "";
            if (TradeDone(offer)) { reason = "Already traded today."; return false; }
            if (Count(offer.GiveItemId) < offer.GiveCount) { reason = $"You need {offer.GiveCount}x {FindItem(offer.GiveItemId).Name}."; return false; }
            if (Count(offer.GetItemId) + offer.GetCount > MaxCarry) { reason = $"The bag holds {MaxCarry}."; return false; }
            return true;
        }

        public bool TryTrade(TradeOffer offer)
        {
            if (!CanTrade(offer, out _)) return false;
            if (Save.tradesKey != TodayKey) { Save.tradesKey = TodayKey; Save.tradesDone.Clear(); }
            AddItem(offer.GiveItemId, -offer.GiveCount);
            AddItem(offer.GetItemId, offer.GetCount);
            Save.tradesDone.Add(offer.Id);
            Changed();
            return true;
        }

        // ---------------------------------------------------------------- charms and arenas

        public bool OwnsCharm(string charmId) => Save.ownedCharms.Contains(charmId);
        public string EquippedCharm => Save.equippedCharm ?? "";

        public bool TryBuyCharm(BattleCharmDef charm)
        {
            if (charm == null || charm.Cost <= 0 || OwnsCharm(charm.Id)) return false;
            if (!_wallet.TrySpend(CurrencyType.Soft, charm.Cost)) return false;
            Save.ownedCharms.Add(charm.Id);
            if (string.IsNullOrEmpty(Save.equippedCharm)) Save.equippedCharm = charm.Id;
            Changed();
            return true;
        }

        public void EquipCharm(string charmId)
        {
            if (!string.IsNullOrEmpty(charmId) && !OwnsCharm(charmId)) return;
            Save.equippedCharm = Save.equippedCharm == charmId ? "" : charmId ?? "";
            Changed();
        }

        public bool OwnsArena(string arenaId) => arenaId == Arenas[0].Id || Save.ownedArenas.Contains(arenaId);
        public ArenaDef Arena => FindArena(OwnsArena(Save.arenaId) ? Save.arenaId : Arenas[0].Id);

        public bool TryBuyArena(ArenaDef arena)
        {
            if (arena == null || arena.RewardOnly || arena.Cost <= 0 || OwnsArena(arena.Id)) return false;
            if (!_wallet.TrySpend(arena.Currency, arena.Cost)) return false;
            Save.ownedArenas.Add(arena.Id);
            Save.arenaId = arena.Id;
            Changed();
            return true;
        }

        public void UseArena(string arenaId)
        {
            if (!OwnsArena(arenaId)) return;
            Save.arenaId = arenaId;
            Changed();
        }

        // ---------------------------------------------------------------- ladder

        public int Rating => Save.rating;
        public int Wins => Save.wins;
        public int Losses => Save.losses;
        public int WinStreak => Save.winStreak;
        public int BestWinStreak => Save.bestWinStreak;

        public static int LeagueIndexFor(int rating)
        {
            int index = 0;
            for (int i = 0; i < Leagues.Length; i++) if (rating >= Leagues[i].MinRating) index = i;
            return index;
        }

        public int LeagueIndex => LeagueIndexFor(Save.rating);
        public LeagueDef League => Leagues[LeagueIndex];
        public LeagueDef NextLeague => LeagueIndex + 1 < Leagues.Length ? Leagues[LeagueIndex + 1] : null;

        // 0..1 toward the next league (1 in the top league).
        public float LeagueProgress
        {
            get
            {
                var next = NextLeague;
                if (next == null) return 1f;
                return Math.Max(0f, Math.Min(1f, (Save.rating - League.MinRating) / (float)(next.MinRating - League.MinRating)));
            }
        }

        // ---------------------------------------------------------------- daily block

        private string TodayKey => _clock.LocalNow.ToString("yyyy-MM-dd");

        public void RollDailyIfNeeded()
        {
            if (Save.dailyKey == TodayKey && Save.quests.Count == QuestsPerDay) return;
            Save.dailyKey = TodayKey;
            Save.dailyWins = 0;
            Save.dailyChestClaimed = false;
            Save.quests.Clear();
            // A fixed rotation by calendar day: everyone gets the same three, and they change every day.
            int start = _clock.LocalNow.DayOfYear % Quests.Length;
            for (int i = 0; i < QuestsPerDay; i++)
                Save.quests.Add(new QuestState { id = Quests[(start + i * 2) % Quests.Length].Id, progress = 0, done = false });
        }

        public bool FirstWinAvailable { get { RollDailyIfNeeded(); return Save.dailyWins == 0; } }

        public struct QuestView { public BattleQuestDef Def; public int Progress; public bool Done; }

        public List<QuestView> TodayQuests()
        {
            RollDailyIfNeeded();
            var list = new List<QuestView>();
            foreach (var state in Save.quests)
            {
                var def = FindQuest(state.id);
                if (def != null) list.Add(new QuestView { Def = def, Progress = Math.Min(def.Goal, state.progress), Done = state.done });
            }
            return list;
        }

        public bool DailyChestClaimed => Save.dailyChestClaimed;

        // ---------------------------------------------------------------- rivals

        // The next rival is fixed until that battle has been fought, so opening the club twice shows the same cat
        // and the player can prepare for its style.
        public BattleFighterSetup NextRival()
        {
            int fought = Save.wins + Save.losses;
            int seed = ((_data.petId ?? "gotchi").GetHashCode() * 31) ^ (fought * 7919);
            var random = new Random(seed);
            var league = League;
            var identity = RivalSource != null ? RivalSource(Save.rating, seed) : null;

            var styles = new[] { BattleStyle.Claw, BattleStyle.Fluff, BattleStyle.Trick };
            var style = styles[random.Next(styles.Length)];
            int level = Math.Max(1, Math.Min(LevelSystem.MaxLevel + 2, _level.Level + random.Next(league.RivalLevelMin, league.RivalLevelMax + 1)));
            int ranks = Math.Max(0, league.RivalRanks + random.Next(-1, 2));

            var rival = new BattleFighterSetup
            {
                Name = identity != null ? identity.PetName : "Rival",
                OwnerName = identity != null ? identity.OwnerName : "A stranger",
                CoatId = identity != null ? identity.CoatId : "ginger",
                Level = level,
                Style = style,
                Stats = StatsFor(level, ranks, style),
                SmartAi = league.SmartAi,
                Rating = identity != null ? identity.Rating : Save.rating,
            };

            // Rival moves: the three starters, its style basic, and its style's bigger move once the league sells it.
            rival.MoveIds.Add("scratch");
            foreach (var move in Moves) if (move.StarterFor == style) rival.MoveIds.Add(move.Id);
            BattleMoveDef big = null;
            foreach (var move in Moves)
                if (move.Style == style && move.Power >= 70 && move.League <= LeagueIndex && (big == null || move.Power > big.Power)) big = move;
            rival.MoveIds.Add(big != null ? big.Id : "pounce");
            rival.MoveIds.Add(LeagueIndex >= 2 && style == BattleStyle.Fluff ? "fluff_up" : "hiss");
            return rival;
        }

        public BattleFighterSetup PlayerSetup(string petName)
        {
            var setup = new BattleFighterSetup
            {
                Name = petName, OwnerName = _data.displayName, Level = _level.Level, Style = Style, Stats = CurrentStats(),
                CharmId = EquippedCharm, Rating = Save.rating,
            };
            foreach (string id in Save.loadout) if (FindMove(id) != null) setup.MoveIds.Add(id);
            if (setup.MoveIds.Count == 0) setup.MoveIds.AddRange(StarterMoves);
            return setup;
        }

        // ---------------------------------------------------------------- results

        public int CoinsForWin(int streakAfterWin)
        {
            float coins = 25f * League.CoinMultiplier * (1f + 0.1f * Math.Min(5, Math.Max(0, streakAfterWin - 1)));
            var charm = FindCharm(EquippedCharm);
            if (charm != null && charm.Effect == CharmEffect.Coins) coins *= 1f + charm.Amount / 100f;
            return (int)Math.Round(coins);
        }

        // Books one finished battle: record, rating, streak, promotion, daily bonus and quests. Coins and XP are
        // returned for the caller to pay out (so the Double Rewards boost applies to them like to any game);
        // hearts and promotion rewards are paid here because no boost touches them.
        public BattleRewards Finish(BattleReport report)
        {
            RollDailyIfNeeded();
            SetVitals(report.HpLeft, report.MpLeft);   // the bars stay where the fight left them
            if (!report.Ran) Buffs?.OnBattleFought();  // a fed or groomed cat is so for a number of battles
            var rewards = new BattleRewards();
            int leagueBefore = LeagueIndex;
            int floor = Leagues[leagueBefore].MinRating;   // a league, once reached, is never lost

            if (report.Won)
            {
                Save.wins++;
                Save.winStreak++;
                Save.bestWinStreak = Math.Max(Save.bestWinStreak, Save.winStreak);
                rewards.RatingDelta = 25 + 5 * Math.Min(3, Save.winStreak - 1);
                rewards.Coins = CoinsForWin(Save.winStreak);
                rewards.Xp = 40 + 10 * leagueBefore;
                if (Save.winStreak >= 2) rewards.Lines.Add($"Win streak x{Save.winStreak}: +{10 * Math.Min(5, Save.winStreak - 1)}% coins");
                if (Save.dailyWins == 0) { rewards.Coins += FirstWinBonus; rewards.Lines.Add($"First win today: +{FirstWinBonus} coins"); }
                Save.dailyWins++;
            }
            else
            {
                Save.losses++;
                Save.winStreak = 0;
                rewards.RatingDelta = report.Ran ? -10 : -15;
                rewards.Coins = report.Ran ? 0 : (int)Math.Round(8f * League.CoinMultiplier);
                rewards.Xp = report.Ran ? 5 : 15;
            }

            int rating = Math.Max(floor, Save.rating + rewards.RatingDelta);
            rewards.RatingDelta = rating - Save.rating;
            Save.rating = rating;
            Save.bestRating = Math.Max(Save.bestRating, rating);
            rewards.NewRating = rating;

            // Promotions: every league crossed pays out once.
            for (int i = leagueBefore + 1; i <= LeagueIndex; i++)
            {
                var league = Leagues[i];
                if (Save.claimedLeagues.Contains(league.Id)) continue;
                Save.claimedLeagues.Add(league.Id);
                rewards.PromotedTo = league;
                _wallet.Add(CurrencyType.Soft, league.RewardCoins);
                rewards.Hearts += league.RewardHearts;
                string extra = "";
                if (!string.IsNullOrEmpty(league.RewardCharmId) && !OwnsCharm(league.RewardCharmId))
                {
                    Save.ownedCharms.Add(league.RewardCharmId);
                    extra = ", " + FindCharm(league.RewardCharmId).Name;
                }
                if (!string.IsNullOrEmpty(league.RewardArenaId) && !Save.ownedArenas.Contains(league.RewardArenaId))
                {
                    Save.ownedArenas.Add(league.RewardArenaId);
                    extra += ", " + FindArena(league.RewardArenaId).Name;
                }
                rewards.Lines.Add($"Promoted to {league.Name}! +{league.RewardCoins} coins, +{league.RewardHearts} hearts{extra}");
            }

            ProgressQuests(report, rewards);
            if (rewards.Hearts > 0) _wallet.Add(CurrencyType.Premium, rewards.Hearts);
            Changed();
            return rewards;
        }

        // Books a fight in the Wild: no rating, fixed coins and XP from the area (the Lucky Coin still adds its
        // share), and the same daily quests as the club. `hearts` is a first-clear reward, paid here.
        public BattleRewards FinishWild(BattleReport report, int coins, int xp, int hearts)
        {
            RollDailyIfNeeded();
            SetVitals(report.HpLeft, report.MpLeft);
            if (!report.Ran) Buffs?.OnBattleFought();
            var rewards = new BattleRewards { Xp = xp, NewRating = Save.rating, Hearts = hearts };
            var charm = FindCharm(EquippedCharm);
            rewards.Coins = charm != null && charm.Effect == CharmEffect.Coins ? (int)Math.Round(coins * (1f + charm.Amount / 100f)) : coins;
            ProgressQuests(report, rewards);
            if (rewards.Hearts > 0) _wallet.Add(CurrencyType.Premium, rewards.Hearts);
            Changed();
            return rewards;
        }

        // Daily quests count every battle, ranked or wild, and pay the moment they complete.
        private void ProgressQuests(BattleReport report, BattleRewards rewards)
        {
            for (int i = 0; i < Save.quests.Count; i++)
            {
                var state = Save.quests[i];
                var def = FindQuest(state.id);
                if (def == null || state.done) continue;
                state.progress += QuestGain(def.Kind, report);
                if (state.progress >= def.Goal)
                {
                    state.done = true;
                    _wallet.Add(CurrencyType.Soft, def.RewardCoins);
                    rewards.Lines.Add($"Quest done: {def.Text} +{def.RewardCoins} coins");
                }
                Save.quests[i] = state;
            }
            if (!Save.dailyChestClaimed && Save.quests.Count > 0 && Save.quests.TrueForAll(q => q.done))
            {
                Save.dailyChestClaimed = true;
                rewards.Hearts += DailyChestHearts;
                rewards.Lines.Add($"All three quests: +{DailyChestHearts} hearts");
            }
        }

        private static int QuestGain(BattleQuestKind kind, BattleReport report)
        {
            switch (kind)
            {
                case BattleQuestKind.Wins: return report.Won ? 1 : 0;
                case BattleQuestKind.Battles: return report.Ran ? 0 : 1;
                case BattleQuestKind.SuperEffective: return report.SuperEffectiveHits;
                case BattleQuestKind.NoItems: return report.Won && report.ItemsUsed == 0 ? 1 : 0;
                case BattleQuestKind.StyleMoves: return report.StyleMovesUsed;
                case BattleQuestKind.HealthyWin: return report.Won && report.HpLeft * 2 > report.MaxHp ? 1 : 0;
                default: return 0;
            }
        }
    }
}
