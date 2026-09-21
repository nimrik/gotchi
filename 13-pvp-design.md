# Gotchi — Battle Design (Battle Club, the Wild, the Market)

Status: v0.4 (2026-09-21). First playable version is in the build; numbers are first guesses to be tuned
against real play. Rules live in `Assets/Scripts/Gotchi/Systems/BattleSystem.cs`, `CampSystem.cs` and
`CampaignSystem.cs`, the fight in `MiniGames/BattleMiniGame.cs`, the screens in `UI/BattleClubPanelView.cs`,
`MarketPanelView.cs` and `CampaignPanelView.cs`. Change this file with the code.

**The game is built around battles (decided 2026-09-21).** Every other mini-game is gone, Meadow Hunt included.
The first release has one mode, the **Battle Club** (ranked, against other keepers' cats). Everything that builds
the cat for it is one tap from the home screen: BATTLE, TRAIN, MOVES and BAG; the Market hangs off the Bag page.

**The Wild is parked** (2026-09-21, the same day it was built). It is to become a whole world to explore, which
is not first-release work. `GameFeatures.Wild` is off: no door, no panel, nothing on screen leads there. The
trail-of-steps version described in section 3a stays in the code, compiled and covered by the smoke test, as
something to build the world on.

## 1. What it is

A turn-based battle in the classic handheld monster-battler mould, one cat against one cat. The player's cat stands in the
foreground **seen from behind, mouth shut**, looking up the field at a rival that faces the camera. The rival is
another keeper's cat (mock roster today, match-making later), told apart by its coat: ginger, smoke, night,
cream. The player's own cat is always the painted cocoa cat.

The Sport, Social, Science, Nature, Explorer and Hunter games were removed on 2026-09-21; their branch ids stay
in the save format only. Battle XP (the PvP branch) is the one skill track, and it drives the evolution stage.

**Original characters only.** The genre is borrowed, nothing else is: no names, creatures, move names, phrases,
sounds or screen layouts from any existing monster-battling game. Battle lines are written in our own words
("goes for", "a strong match-up", "is worn out"). See `05-monetization-compliance.md`, "Intellectual property".

Four ideas hold the design together:

1. **The camp prepares, it never punishes.** Between fights the player rests the cat, lets it focus, feeds and
   grooms it (section 2b). Feeding and grooming lend attack and defense to the next few battles. Nothing drains
   while the player is away, and nothing at the camp can block a fight except a cat that is worn out.
2. **A fight costs health and mana, and they stay spent.** Both bars end a fight where the fight left them and
   are on the home screen's status block, in blocks of 250 points. Back home the cat rests to get them back
   (section 2a). This is the pacing of the game: fight, rest, fight.
3. **A build, not a number.** Style, four stats, four move slots, a held charm and a small bag give real
   choices; the rival is shown before the fight so the choices matter.
4. **Coins buy power, hearts buy looks.** Nothing that changes who wins is sold for hearts or money.

## 2. The battle

**Menu.** FIGHT / ITEM / CHEER / RUN. FIGHT opens the four carried moves; the first tap on a move shows its
mana cost against the mana left, style, power, accuracy and whether it is STRONG or WEAK against this rival, the
second tap uses it. A move the cat cannot pay for is greyed out. ITEM opens
the bag (3 item turns per battle). CHEER spends the turn to raise ATTACK by one stage. RUN forfeits.

**Order.** A priority move goes first; otherwise the higher SPEED (with its stage) does; the player wins a tie.

**Styles.** A cat has one fighting style, and every move has one.

| Attacker | beats | loses to |
|---|---|---|
| CLAW (fierce, coral) | TRICK | FLUFF |
| FLUFF (sturdy, mint) | CLAW | TRICK |
| TRICK (sly, lavender) | FLUFF | CLAW |

Strong ×2, weak ×0.5, NORMAL and a mirror match ×1. A move of the cat's own style hits ×1.5.
Why these three: thick fluff blunts claws, tricks get around a slow tank, raw aggression runs over tricks.

**Damage.** `(((2·L/5 + 2) · Power · Attack / Defense) / 25 + 2) × 40`, times own-style ×1.5, style match-up,
critical ×2 and a random 0.85–1.0 (`BattleSystem.BaseDamage`). The ×40 is the damage scale: health is counted
in the thousands so that a bar block of 250 points means something, and a basic hit between two level-1 cats
takes about one block. Attack, Defense and Speed carry stages −6…+6 (`(2+s)/2` up, `2/(2−s)` down); a critical
hit ignores the attacker's lowered Attack and the defender's raised Defense. Critical chance is 1/16, tripled
by "crits" moves.

**Moves** (15). Four are carried. Every move spends **mana** except SCRATCH, which is free, so a cat can always
do something. League = the league that has to be reached before the club teaches it.

| Move | Style | Power | Acc | MP | Effect | Get it |
|---|---|---|---|---|---|---|
| SCRATCH | Normal | 40 | 100 | 0 | | starter |
| POUNCE | Normal | 65 | 85 | 160 | | starter |
| HISS | Normal | — | 100 | 80 | rival ATTACK −1 | starter |
| QUICK PAW | Normal | 35 | 100 | 120 | always first | Bronze, 120 coins |
| CATNAP | Normal | — | — | 320 | heal 45% | Silver, 250 |
| CLAW SWIPE | Claw | 50 | 100 | 120 | | free with Claw, else 150 |
| FURY CLAWS | Claw | 75 | 90 | 240 | crits ×3 | Silver, 300 |
| FRENZY | Claw | 95 | 85 | 320 | 25% recoil | Crystal, 600 |
| FLUFF BUMP | Fluff | 50 | 100 | 120 | | free with Fluff, else 150 |
| FLUFF UP | Fluff | — | — | 160 | own DEFENSE +1 | Bronze, 120 |
| BODY SLAM | Fluff | 75 | 90 | 240 | | Silver, 300 |
| SNEAK ATTACK | Trick | 50 | 100 | 120 | | free with Trick, else 150 |
| SURPRISE! | Trick | 80 | 80 | 280 | crits ×3 | Silver, 300 |
| YARN TRAP | Trick | 40 | 95 | 200 | rival SPEED −1 | Gold, 400 |
| ZOOMIES | Trick | — | — | 200 | own SPEED +2 | Gold, 350 |

Buying the other styles' basics is the point: a Claw cat that carries SNEAK ATTACK has an answer to Fluff.

**Items** (coins; the bag holds 9 of each; 3 item turns per battle so a fight cannot be bought).

| Item | Does | Coins |
|---|---|---|
| Fish Treat | heal 40% | 30 |
| Big Tuna | heal 100% | 90 |
| Catnip | ATTACK +2 | 60 |
| Warm Milk | restore all mana, undo lowered stats | 50 |

**Charms** (one held).

| Charm | Does | Get it |
|---|---|---|
| Spiked Collar / Padded Vest / Bell Collar / Heart Locket | +10% ATTACK / DEFENSE / SPEED / HP | 300 coins each |
| Lucky Coin | +25% coins from every battle | 500 coins |
| Leftover Fishbone | heal 6% after every turn | Silver promotion |
| Focus Ribbon | once per battle, survive a knockout at 1 HP | Gold promotion |

**Mana.** The pool is `1000 + 100 × level` (1100 at level 1), the same for every cat; a rival always starts a
fight full. Costs run from 80 (HISS) to 320 (CATNAP, FRENZY), none more than a third of a level-1 pool, so a
fight of style basics spends about half a pool. There is no mana regeneration inside a fight; Warm Milk is the
only refill. A loadout without SCRATCH that runs dry falls back to WILD SWING (weak, with recoil).

**Rival AI.** Bronze picks moves at random (among those it can pay for). From Silver it takes the best-scoring move three turns out of four
(power × match-up × own-style × accuracy; HISS and FLUFF UP while they still help).

## 2a. Health and mana between fights

Both bars persist (`BattleSave.hp / mp`, with the time they were last brought up to date). The cat walks into
every fight with what it has and walks out with what is left; the results card says so ("goes home with
240/1100 HP and 610/1100 MP. Rest to recover."). **Under a tenth of its health the cat is worn out**: it lies
down at home, the FIGHT button reads WORN OUT, and no fight starts (`BattleSystem.CanFight`). They come back
six ways:

| How | Gives back |
|---|---|
| **REST** (camp, 60 s cooldown) | 40% of the health; 60% with the Cozy Nest |
| **FOCUS** (camp, 60 s cooldown) | 40% of the mana; 60% with the Quiet Corner |
| **Treat** (the chip in the status block, 60 s cooldown) | 15% of both |
| **A bag item** used from the Bag page | Fish Treat 40% HP, Big Tuna all HP, Warm Milk all mana |
| **Full Recovery** (shop, 40 coins) | both to full at once |
| **Time** | both refill in ten minutes, the app open or not |

Numbers to tune: ten minutes of doing nothing is deliberately short, so the rest loop paces a session without
ever locking the player out; the camp and the bag are there for the player who wants the next fight now.

**How the bars look.** Health and mana are drawn as slanted blocks of 250 points with white 2 px separators and
the numbers right after the bar; the bar's width is fixed, so a bigger pool means narrower blocks
(`UI/SegmentedBar`, rules in `12-ui-guide.md`). That is why the stats are in the thousands.

## 2b. The camp: buffs counted in battles

The camp block on the home screen replaced the four needs (`Systems/CampSystem`). REST and FOCUS are in the
table above. The other two lend an edge to the next fights:

| Action | Buff | Lasts | With its helper |
|---|---|---|---|
| **FEED** | "Fed": ATTACK +10% | the next 3 battles | 5 battles (Snack Dispenser) |
| **GROOM** | "Groomed": DEFENSE +10% | the next 3 battles | 5 battles (Grooming Kit) |

- A buff is counted in battles, not minutes, so it never runs out while the player is away. Every fight that is
  fought to the end uses one charge of each; running from a fight uses none.
- Feeding tops the count back up to its full length; it cannot be stacked beyond it.
- The buffs multiply the stats a fight starts with (`BattleSystem.CurrentStats`); the permanent numbers on the
  Train page leave them out (`TrainedStats`). The club lists what is running ("Fed: ATK +10% · 2 battles left").
- `CampSystem` reaches the rules through one interface, `IBattleBuffs` (multiplier, lines, a fought battle).

## 3. Building the cat: the upgrade paths

| Path | How it grows | Paid with | Limit |
|---|---|---|---|
| **Pet level** | battles and the camp (the pet's level XP) | time | base stats `HP 1000+100L, MP 1000+100L, ATK 10+2L, DEF 9+2L, SPD 8+2L` |
| **Style** | first pick free (comes with 4 moves and 3 Fish Treats); +10% to its stat | 150 coins to change | one at a time; every move learned is kept |
| **Training** | four rank tracks, +5% of the base stat per rank | coins: 40, 55, 80, 110, 155, 215, 300, 420, 590, 825 | rank ≤ pet level + 1, max 10 |
| **Moves** | learned in the club | coins | gated by league |
| **Bag** | consumables | coins | 9 of each, 3 uses per battle |
| **Charm** | bought, or earned by promotion | coins / rating | one held |
| **Camp buffs** | FEED and GROOM | a press, 60 s cooldown | +10% ATTACK / DEFENSE for 3 battles (5 with the helper) |
| **League** | rating | winning | unlocks moves, raises coin rewards |

Full training is about 2,800 coins per stat, 11,000 for all four: a months-long sink that pet level paces.

**Leaning.** The style and the most trained stat together name the build, and the name is the chip after the
level on the home screen: Claw gives Brawler / Fighter / Bruiser / Striker for health / attack / defense /
speed, Fluff gives Tank / Crusher / Guardian / Bouncer, Trick gives Survivor / Ambusher / Trickster / Shadow.
No style is a Rookie; every stat at rank 3 or more and within one rank of each other is an All-rounder; a tie
goes to the style's own stat. A label only (`BattleSystem.Leaning`), it changes nothing in a fight. It replaced
the mood chip, and later it is how other players read a cat at a glance.

## 3a. The Wild (campaign) — PARKED, not in the first release

Off behind `GameFeatures.Wild`; kept as the starting point for the world to explore. What was built:

A chain of areas, each a short trail. The player **searches** one step at a time: a step is a wild cat, a find,
or the area's boss at the end. Beating the boss clears the area and opens the next.

| Area | Wild cats | Boss | Steps | First clear |
|---|---|---|---|---|
| Back Garden | Lv 1–2 | Bramble Lv 3 (Claw) | 6 | 100 coins, 3 hearts |
| Bin Alley | Lv 2–3 | Rusty Lv 4 (Trick) | 6 | 200 coins, 4 hearts |
| Long Meadow | Lv 3–5 | Clover Lv 6 (Fluff) | 7 | 300 coins, 5 hearts |
| Old Barn | Lv 5–7 | Old Soot Lv 8 (Fluff) | 7 | 400 coins, 6 hearts |
| High Roofs | Lv 7–9 | Gable Lv 10 (Trick) | 8 | 500 coins, 7 hearts |
| Night Forest | Lv 9–11 | Umbra Lv 12 (Claw) | 8 | 600 coins, 8 hearts |

What makes it different from the club:

- **Levels are absolute.** Ranked rivals always match the player; the Wild does not, so it is the yardstick the
  cat grows against. Wild cats carry `tier − 1` training ranks, the boss two more and a smart AI.
- **Health and mana carry over** from one encounter to the next, as they now do after every fight. Treats, tuna
  and warm milk can be used on the trail between fights; Catnip only in one. An expedition is about how far
  the cat gets on what is in the bag.
- **Nothing is lost.** Finds go into the bag at once. GO HOME is always allowed; a knockout carries the cat
  home with everything found. RUN leaves a wild cat behind (the trail goes on) but not a boss.
- **No rating.** A wild win pays `12 × tier` coins and `15 + 5 × tier` battle XP, the boss `40 + 10 × tier` XP;
  a first clear pays the table above, later clears `30 × tier` coins. Daily quests count these fights too.
- **Finds are fixed** per step (a Fish Treat, 20 coins, …) and come back once a day. Which wild cat turns up
  is seeded by the run and the step, so reopening the panel cannot reroll it. No paid chance anywhere.

Every wild animal is a cat in another coat today (tabby, ash, rust, shadow and the keepers' coats): there is
one character model. `WildAreaDef.Species` and `BattleFighterSetup.Species` are where new characters plug in.

## 3b. The Market: buy, sell, trade

Reached from the Bag page ("To the Market"); its door on the home menu was removed on 2026-09-21 to keep that row
to the four things done every session.

| Page | What it does |
|---|---|
| **Buy** | items, charms and arenas at list price |
| **Sell** | items and bought charms at **half price**; promotion rewards are keepsakes; a held charm must be taken off first |
| **Trade** | the daily board: three swaps with other keepers, the same for everyone that day, each once |

A swap never asks the player for more value than it gives and gives at most 35% more, so trading always beats
selling at half and buying at full. The board is a **mock of player-to-player trading**: `BattleSystem.TodayTrades`
and `TryTrade` are the seam for the real thing (friends' offers, posting your own). Before that ships, decide how
trades are moderated and whether under-13 accounts can trade at all (see `05-monetization-compliance.md`).

## 4. Ladder

Win: +25 rating, +5 for each win already in the streak (max +40). Loss −15, RUN −10. **A league, once reached,
is never lost**: rating stops at the league's floor. Promotion pays once.

| League | Rating | Battle coins | Rival | Promotion reward |
|---|---|---|---|---|
| Bronze | 0 | ×1 | level −1…0, untrained, random moves | — |
| Silver | 200 | ×1.25 | same level, 2 ranks, smart | 100 coins, 5 hearts, Leftover Fishbone |
| Gold | 500 | ×1.5 | level 0…+1, 4 ranks | 200 coins, 10 hearts, Focus Ribbon |
| Crystal | 900 | ×1.75 | level +1, 6 ranks | 300 coins, 15 hearts, Moonlit Roof arena |
| Champion | 1400 | ×2 | level +1…+2, 8 ranks | 500 coins, 25 hearts |

The next rival is fixed until that battle is fought (seeded by the pet and the number of battles), so opening
the club twice shows the same cat and preparing for its style is possible. Rivals are keepers from the rating
board whose rating is closest to the player's.

## 5. Coins and hearts

**Per battle.** Win `25 × league × (1 + 0.1 × streak, max +50%)`, +25% with the Lucky Coin; loss `8 × league`;
RUN nothing. Battle XP 40 + 10 per league on a win, 15 on a loss, 5 for running; half of it again as pet XP.
The shop's Double Rewards boost doubles battle coins and XP. **What a battle costs is health and mana**
(section 2a), and only a worn-out cat is kept from fighting.

| | Coins | Hearts |
|---|---|---|
| **In** | ranked wins and losses, wild wins, area clears 100–600, finds, first ranked win of the day +50, daily quests 30–80 each, promotions 100–500, selling at half price | promotions 5–25, first clears 3–8, all three daily quests +3 |
| **Out** | training (about 11,000 in all), moves (about 3,000), charms (1,700), items (30–90 each), style change (150), Back Garden arena (400) | Sunset Beach arena (30). Looks only |

Guard-rails, in line with `05-monetization-compliance.md`: hearts are bought with money, so **hearts never buy
stats, moves, items, charms or rating**; there are no random rewards, chests or drops; losing never takes
coins; nothing is lost by not playing (no rating decay, no league loss).

## 6. What brings the player back

- **First win of the day** +50 coins, and **three daily quests** on a fixed calendar rotation (the same for
  everyone: win a battle, finish 3, land 3 hits with a strong match-up, win without items, use 6 own-style
  moves, win with more than half HP, win 3). Ranked and wild fights both count. Quests pay the moment they
  complete; all three pay 3 hearts.
- **The Wild**: the next area is always visible with its first-clear purse, and a boss that was too strong is a
  reason to train, shop and come back.
- **Win streak**: more rating and more coins per win, shown on the results card.
- **Promotion**: a lump of coins, hearts and a charm or arena that cannot be bought.
- **The rival card**: who is next, its level and style, and one line of advice ("Fluff beats your Claw style.
  Bring a Trick move.").
- **Camp buff lines** in the club turn looking after the cat into visible power ("+ Fed: ATK +10% · 2 battles
  left"), and a buff that is about to run out is a reason to visit the camp before the next fight.
- **Rating board** in the leaderboards, with every keeper's league.
- A battle ends back in the club, with the next rival already waiting.

## 7. Required animations (NOT authored yet)

The battle needs six clips of its own. They are **required, not implemented**: until they exist
`BattleMiniGame.PlayCue` plays the closest clip the cat already has. Author them in `Tools/blender/build_cat2.py`
like the other one-shots (additive, never keyed on frame 0, back to rest; L parts at +X), and check each from
**behind** as well as from the front, because the player's cat is always seen from the back.

| Clip | When | What it should read as | Stand-in today |
|---|---|---|---|
| **Defensive** | FLUFF UP, DEFENSE rising, bracing for a hit | low crouch, ears flat, fur puffed (body wider), tail wrapped | `Nod` |
| **Attacking** | every damaging move | wind-up back, fast lunge with a paw swipe, overshoot, recover; the stage already dashes the cat up the field | `Attack` |
| **Screaming** | HISS, CHEER, Catnip, ATTACK/SPEED rising, the intro | planted feet, arched back, head thrown forward, mouth wide open (the rival only; ours keeps it shut), tail bristling | `Yawn` |
| **Healing** | treats, CATNAP, Warm Milk, Fishbone | a calm settle: sit, slow blink, a lick of the paw, a small happy shiver | `Eat` |
| **Defeated** | HP reaches 0 | wobble, sink, roll onto the side, eyes shut; holds until the battle ends | `Faint` + `Fainted` loop |
| **Wounded (lightly)** | a hit under a quarter of max HP | a quick flinch: head turns away, one ear down, half a step back; the heavy flinch stays `Hurt` | `Wiggle` |

## 8. Branches for later

Ways this can grow, roughly by cost. None is built.

- **Battle-side depth**: move practice levels (+10% power per level, coins); status conditions (dizzy skips a
  turn, sleepy halves speed) with moves and items that cause and cure them; arena effects (Garden boosts Fluff,
  Roof boosts Trick) that would make arenas more than looks, which needs the hearts rule rethought; signature
  moves unlocked by evolution stage; a fourth style.
- **More characters**: the Wild is where they would first appear (each area its own animals), then as keepers'
  pets, then as a second and third member of the player's team, which turns one-on-one fights into switching.
  All original designs. A catching or befriending mechanic needs a patent check first (see the IP section).
- **The Wild grows**: branching trails, weather per area, rare wanderers on certain days, area-specific finds
  that are cooked into battle items, a second loop of every area at higher levels.
- **Other players**: live PvP through Supabase (same `BattleSystem` rules, server-side rolls); friend battles by
  code with no rating; asynchronous defence, where other players fight an AI copy of the player's build;
  2-vs-2 tag battles with a friend's cat; weekly tournaments with brackets; seasons with a soft rating reset
  and a cosmetic season reward; clubs (shared goal, club board).
- **Meta**: rival rematches and named club bosses at each league gate; battle replays; spectating friends;
  titles on the rating board; style mastery tracks (a cosmetic aura after 50 wins in a style).
- **Looks (heart sinks)**: more arenas, battle entrances, victory poses, outlines and auras, a cosmetic battle
  pass. All safe under the hearts rule.
- **Camp tie-ins**: more camp actions (a warm-up that lends SPEED, a pep talk that lends critical hits), a
  sparring-partner helper that trains one rank slowly over a day, leanings that carry a small passive.

## 9. Status

| Piece | State |
|---|---|
| Rules, catalogue, save, rewards, daily block | built; the smoke test (`RunLogicSmokeTest`, 258 checks in all) covers them |
| Mana instead of move uses; health and mana in the thousands that persist after a fight and come back with the camp, a treat, a bag item, Full Recovery or time; worn out under a tenth | built |
| The camp (REST, FOCUS, FEED, GROOM, the Treat), buffs counted in battles, helpers that improve camp actions | built; it replaced the needs and the battle condition |
| Leanings; block bars for health and mana on the home screen, in the club and in the fight | built |
| The Wild: six areas, expeditions, finds, bosses, first clears | built, then **parked** (`GameFeatures.Wild` off) until it becomes a world to explore |
| The Market: buy, sell at half price, daily trade board (mock keepers) | built; reached from the Bag page |
| Battle menu on the home screen (BATTLE / TRAIN / MOVES / BAG) | built |
| Battle with styles, 15 moves, stages, priority, recoil, items, charms, back view, rival coats | built |
| Club hub: Club / Train / Moves / Bag pages | built |
| Rating board, keeper profiles with coats | built (mock keepers) |
| Six battle clips | **required**, see section 7 |
| Battle-only visual effects (hit sparks, style-coloured flashes, heal glow) | not built |
| Live PvP, match-making, anti-cheat | not built; the rival source is one delegate (`BattleSystem.RivalSource`) |

## Open questions

- Is the rest loop right? A fight of style basics spends about half the mana; REST and FOCUS each give back 40%
  once a minute and time refills everything in ten. Too generous and the bars mean nothing, too stingy and it
  is a wall.
- Are +10% buffs for three battles worth a visit to the camp? Too small and nobody feeds the cat, too large and
  feeding becomes a chore before every fight.
- Should mana be trainable (a fifth rank track) or come from a charm, so a caster build exists?
- Only the rest loop limits how many ranked battles fit in a day. Is that the intent for rating too, or should
  rating gains taper after the tenth win of a day?
- Is the first boss (Bramble, Lv 3) too strong for a brand-new Lv 1 cat? It is meant to send the player to TRAIN
  and the Market once, but it must not feel like a wall.
- Is 3 item turns per battle right, or should items cost the turn *and* have a per-item limit?
- Should CHEER stay free and unlimited? It is the keeper's only direct action, but it competes with Catnip.
- Rating numbers assume about five battles a day. The Bronze to Silver climb is about seven wins, which a keen
  player does in one session. Too fast?
- Should a style change be free once per league, so trying styles is not punished early?
- Do rivals get charms from Gold up, to keep the top leagues from being stat checks?
