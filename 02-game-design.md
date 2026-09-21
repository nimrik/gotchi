# Gotchi — Game Design

Status: v0.2 (2026-09-21). The rules of the battle itself are in `13-pvp-design.md`; this guide is the shape
of the whole game around it. Numbers are first guesses until they have been played.

## The loop

**Fight, rest, build, fight again.**

1. **Fight.** A ranked battle in the Battle Club against another keeper's cat. It costs health and mana, and
   both bars stay where the fight left them.
2. **Rest.** Back home the cat gets them back: the camp (REST, FOCUS), a Treat, an item from the bag, or ten
   quiet minutes. FEED and GROOM lend attack and defense to the next few fights.
3. **Build.** Coins from fights go into training, moves, charms and items; the rival for the next fight is
   shown in advance, so the build can answer it.
4. Rating, leagues, a first-win bonus and three daily quests give the next fight a reason.

Everything the loop needs is one tap from the home screen: BATTLE, TRAIN, MOVES, BAG, and the camp block.

## The camp (replaced the four needs on 2026-09-21)

| Action | Gives | With its helper |
|---|---|---|
| REST | health +40% | +60% (Cozy Nest) |
| FOCUS | mana +40% | +60% (Quiet Corner) |
| FEED | "Fed": ATTACK +10% for the next 3 battles | 5 battles (Snack Dispenser) |
| GROOM | "Groomed": DEFENSE +10% for the next 3 battles | 5 battles (Grooming Kit) |
| Treat (chip in the status block) | 15% of both bars | |

- Each action has a 60 second cooldown. A press that would do nothing is refused (full bar, full buff).
- A buff is counted in battles, not minutes. Every fight that is fought to the end uses one charge; running
  from a fight uses none.
- **Nothing decays.** No meter runs down while the player is away, nothing is lost by not playing, and the cat
  never dies. Health and mana even refill on their own while the app is closed.
- A camp action gives 5 pet XP, so looking after the cat is never wasted time.
- Code: `Systems/CampSystem`. It plugs into the battle rules through `IBattleBuffs`.

## Progression

| Track | Grows by | Gives |
|---|---|---|
| **Pet level** (max 12) | battles (half the battle XP), camp actions (+5), helpers (+20) | base stats, the training cap (level + 1), a story chapter and 5 hearts per level |
| **Battle XP** | every fight (40 + 10 per league on a win, 15 on a loss, 5 for running) | the evolution stage: one per 250 XP, five stages; helpers unlock at 100 / 200 / 300 / 400 |
| **Training** | coins, four stats, ten ranks each | +5% of the base stat per rank |
| **Moves, charms, bag** | coins, gated by league | the choices inside a fight |
| **Rating** | winning | leagues, which raise coin rewards and unlock moves; promotion rewards |

Level XP: `XP(n) = 60(n-1)² + 40(n-1)` in total to reach level n (`LevelSystem`).

**Leaning.** The status block names what the build adds up to: the style and the most trained stat together
(Claw + attack = Fighter, Fluff + defense = Guardian, Trick + speed = Shadow, twelve in all, plus Rookie and
All-rounder). It is a label for the player, and later for other players. It changes nothing in a fight. Table in
`09-pets-and-emotions.md`.

**Evolution.** Five stages driven by battle XP. The stages have no looks of their own yet (open question in
`03-art-direction.md`).

## Helpers

Bought once with coins (150 each) when the battle XP is there. Each makes one camp action better for good (see
the camp table). They used to slow the decay of a need; the ids stayed so old saves keep what they own.

## Sessions

- A core session is **2 to 3 minutes**: one or two fights and a visit to the camp.
- Longer play is possible and paced by health and mana, not by a wall: REST and FOCUS once a minute, items from
  the bag, Full Recovery for 40 coins. Ten minutes away refills everything.
- One notification at most: "rested and ready", never between 21:00 and 09:00, never about suffering.

## What brings the player back

First win of the day (+50 coins), three daily quests on a calendar rotation (all three pay 3 hearts), win
streaks, promotion rewards that cannot be bought, the rival card for the next fight, the login streak (10 coins
per streak day up to 50, 10 hearts every seventh day), a story chapter per level, the rating board.

## Deferred

- **The Wild.** A campaign of six areas with wild cats, finds and bosses was built and parked the same day
  (`GameFeatures.Wild`). It comes back as a world to explore.
- **More characters.** One cat today; other keepers' cats differ by coat. New animals arrive with the world.
- **Live PvP, friend battles, real trading.** The rival source and the trade board are single seams in
  `BattleSystem` for that.
- **Hardcore mode** (1:1 real time), several pets, breeding, Android.

## Open questions

- Is the rest loop right (a fight of style basics spends about half the mana, FOCUS gives 40% a minute)? See
  the open questions in `13-pvp-design.md`.
- Should rating gains taper after many wins in a day, now that nothing else limits the number of fights?
- A geometric level curve (`100 × 1.45^(L-1)` per level) and coin costs for evolution stages were proposed on
  2026-09-14 for a steeper late game. Not implemented; revisit once the first ten levels have been played.
- Does the home need something to do that is not preparation for a fight?

## History

v0.1 (2026-09-13) was a virtual pet in three layers: four needs that drained by the hour (hunger, hygiene,
energy, happiness), helpers that slowed the drain, and a seven-branch skill tree (Sport, Social, PvP, Hunter,
Science, Nature, Explorer) with one mini-game per branch and a single-branch-locked evolution. On 2026-09-21
the game was refocused on battles: the six other mini-games were removed (the branch ids stay in the save
format), the needs first became the battle condition and then gave way to the camp, and the emotion system was
dropped (`09-pets-and-emotions.md`).
