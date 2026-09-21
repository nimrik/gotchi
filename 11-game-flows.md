# Gotchi — Game Flows

Status: v0.2 (2026-09-21). How the game moves: every player flow with the class behind it, the numbers, and the
dev and QA paths. When a flow changes, change it here in the same commit. Battle rules are in
`13-pvp-design.md`, UI rules in `12-ui-guide.md`.

## 1. Player flows

### 1.1 First launch (no save)
1. `GameBootstrap.Start` finds no save and shows `OnboardingView` on its own canvas.
2. **Account.** Display name (shown on leaderboards), email and password go to `IAuthService.SignUp` (a mock
   today; the password is never stored, the session token is). Or **Play as guest**.
3. **Someone is coming.** There is nothing to choose, the first release is cats only: a mystery "?" and a name
   field (default "Mochi"), then **Open the door**. A night doorstep: a bundle in a basket, three taps peel the
   blanket back, the cat is there shivering. The text box asks "Take Mochi home?" YES / NO; NO goes back to the
   name. This is story chapter 1. (A doorstep and not an egg, on purpose.)
4. **Two questions.** Age band (`under13` / `13-17` / `18+`; under-13 hides real-money items) and the usual play
   time (morning, afternoon, evening).
5. `CreateFromOnboarding` builds `PetSaveData`, saves it, and `BootWith` builds the systems and the home screen.

### 1.2 Returning launch
1. The save loads. **Nothing has decayed.** Health and mana are brought up to date against the clock the first
   time they are read (`BattleSystem.UpdateVitals`): full after ten minutes away.
2. `ApplyLoginStreak`: the same calendar day does nothing; the next day adds one to the streak and pays
   `10 × streak` coins (max 50) with a toast, and 10 hearts every seventh day; a gap resets the streak to day 1
   unless a Streak Shield is spent.
3. The daily block (first-win bonus, three quests, the trade board) rolls over by the local calendar day.

### 1.3 The home screen
Top to bottom (`HUDController`, `RoomView`):
- **Header.** One wallet box as wide as its content ("1385 COINS  85 HEARTS"; tapping an amount opens the shop on
  Bonuses or Get Hearts), then Story (book), Leaderboards (trophy), News (bell, pink dot when unread),
  Settings (gear). The Shop button sits under the gear.
- **The room.** The cat on its rug in the chosen background. In the default room the window shows the local time
  of day and the room goes light or dark with it (`RoomScenes.CozyPaletteAt`; repainted every ten minutes).
- **Status block**, two rows. Row 1: "Mochi · Lv 4", the **leaning chip** (FIGHTER, GUARDIAN, SHADOW ... in the
  style's colour; ROOKIE before a style is picked), the XP bar with its caption **in experience points**
  ("104 / 220 XP", "MAX" at the top level), and the **Treat** chip. Row 2: **HP and MP as block bars** with
  their numbers right after them ("HP ▰▰▰▰▱ 1000/1200").
- **Battle menu.** The league line ("BRONZE LEAGUE ▬▬▬ 25 / 200") over one row of four doors: BATTLE, TRAIN,
  MOVES, BAG. Before a style is picked the line reads "PICK A STYLE IN BATTLE" and every door leads to the
  style picker.
- **Camp block.** Four cells, 2 × 2: REST, FOCUS, FEED, GROOM (1.5).
- **Info boxes** (`InfoTooltip`): pressing the XP bar shows level XP and what is left, health and mana, the
  evolution stage, the leaning with a one-line description and any camp buffs. Pressing the league bar shows
  style and battle stats, rating and the distance to the next league, the ranked record, battle XP and the camp
  buffs. Six seconds, or a tap anywhere.
- Autosave every 30 s and on pause or quit, which also schedules the reminder (1.11).

### 1.4 Touching the cat
- **Tap**: the part under the finger (head, body, paws, tail) answers with its own reaction, hearts and a word
  ("Purr~", "Boop!", "High five!", "Not the tail!"). **A tap never changes the face.** Taps wear the cat's
  patience instead: each adds 1 to a poke heat (the tail 2) that cools by 1 every 2.5 s. From 8 the cat is
  **irritated** (no hearts, "Enough!", "Stop it.") for 8 s after the last poke. From 12 every further tap has a
  50% chance to make it say "Hmph!" and **walk off the screen for 5 s** (`RoomView.StormOff`), still irritated
  when it comes back.
- **Hold**: it leans into the finger and purrs; hearts keep coming.
- **Rub** on the head or body: petting, the closed happy arcs for a moment, a heart every 0.7 s.
- **Drag**: it slides along the floor, can be lifted and hangs from where it is held, and can be thrown; it
  bounces, slides and rights itself.
- Left alone it idles: blinks, looks around, ear and tail flicks, a stretch, a yawn, a short walk across the rug.
- Under a tenth of its health it lies down, worn out, until it has rested (`PetPortraitView.SetWornOut`).

### 1.5 The camp (`Systems/CampSystem`, `UI/CampCellView`)
A press calls `CampSystem.TryPerform`, which refuses during the cooldown and when the press would be wasted.

| Cell | Does | The cat | Floating text |
|---|---|---|---|
| REST | health +40% (+60% with the Cozy Nest) | yawns | "+440 HP" |
| FOCUS | mana +40% (+60% with the Quiet Corner) | nods | "+440 MP" |
| FEED | ATTACK +10% for the next 3 battles (5 with the Snack Dispenser) | eats | "Fed! ATK +10% · 3 battles" |
| GROOM | DEFENSE +10% for the next 3 battles (5 with the Grooming Kit) | grooms | "Groomed! DEF +10% · 3 battles" |

- Each cell shows its icon, its name, what it does under the name, and on the right where it stands: **READY**,
  the seconds of cooldown left (**45S**), **FULL** when the bar has nothing to gain, or the battles a buff still
  lasts (**2 LEFT**, green). The icon dims while a press would do nothing.
- Cooldown 60 s per action (none while the No Cooldowns boost runs). Each action gives 5 pet XP.
- A buff loses one charge for every fight fought to the end (`IBattleBuffs.OnBattleFought`); running uses none.
- **The Treat** (the chip in the status block, fish icon): 15% of both bars, then "Treat · 42s" while its 60 s
  cooldown runs (`CampSystem.TryTreat`).

### 1.6 Battles
One battle engine (`MiniGames/BattleMiniGame`), rules in `Systems/BattleSystem`.
1. **First visit.** Any battle door opens the style picker: CLAW, FLUFF or TRICK. Free the first time, with four
   moves and three Fish Treats; 150 coins to change later.
2. **Battle Club** (`BattleClubPanelView`, four pages, each also a door on the home menu):
   - **Club**: league and rating bar; the cat's stats, its HP and MP bars and the camp buffs; the next rival
     (another keeper's cat, fixed until it is fought) with a line of advice; the first-win bonus and the three
     daily quests; **FIGHT!**, which reads WORN OUT under a tenth of the health.
   - **Train**: HP / ATTACK / DEFENSE / SPEED ranks for coins (40 to 825), capped at pet level + 1; style change.
   - **Moves**: carry 4 of 15; the others are learned for coins, gated by league.
   - **Bag**: item counts with **Use** (treats, tuna and milk work at home), charms to hold, arenas to use, and
     **To the Market**.
3. **The fight.** Our cat in the foreground **from behind, mouth shut**; the rival faces us in its own coat.
   FIGHT (the first tap on a move shows its mana cost, style, power and STRONG or WEAK; the second uses it; a
   move the cat cannot pay for is greyed out), ITEM (3 per battle), CHEER (ATTACK +1), RUN. Both status boxes
   show block bars; ours adds the MP bar and the numbers. The cat walks in with the health and mana it has.
4. **The end** goes through `BattleSystem.Finish`: the bars stay where the fight left them, one camp charge is
   used, rating (+25 to +40, -15 for a loss, -10 for running, never below the league floor), coins, quests paid
   at once, promotion rewards. The results card lists every line and says what the cat goes home with; **Next
   battle** or Back to the club.
5. `GameBootstrap.HandleMiniGameResult` pays battle XP (which drives the evolution stage), half of it again as
   pet XP, and the coins; the Double Rewards boost doubles XP and coins. Then it saves.
6. **The Wild is parked** (`GameFeatures.Wild` off): no door, no panel. What was built is in `13-pvp-design.md`,
   section 3a.

### 1.7 The Market (`MarketPanelView`, opened from the Bag page)
**Buy** items, charms and arenas. **Sell** items and bought charms at half price (a held charm comes off first;
promotion rewards cannot be sold). **Trade**: the daily board, three value-for-value swaps with other keepers,
each once a day, a mock of player-to-player trading.

### 1.8 Level, story, evolution
- Pet XP: half of every fight's battle XP (at least 5), +5 per camp action, +20 per helper unlocked.
  `LevelSystem.XpRequiredForLevel(n) = 60(n-1)² + 40(n-1)`, max level 12. A level-up toasts, bounces the cat
  and pays 5 hearts.
- Each level opens a chapter in `StoryBook`. The Story button plays it like a cutscene: the cat on a framed
  scene, the text typed out in the text box, tap to continue. Chapters above the level read "Reach level N".
- Battle XP sets the evolution stage: one per 250 XP, five stages (`SkillTreeSystem`). A new stage toasts and
  the cat celebrates. The stages have no looks of their own yet.

### 1.9 Shop and helpers (`ShopPanelView`)
- Every Buy opens a YES / NO text box first. Pages, swipeable or by arrows: **Get Hearts** (real money; hidden
  for under-13: 50 / 150 / 400 Hearts, Starter Pack), **Bonuses** (500 Coins for 20 hearts, Full Recovery 40
  coins, No Cooldowns 1 h, Double Rewards 1 h, Streak Shield), **Style** (outfits, worn at once, tap to swap),
  **Backgrounds** (the free Cozy Room, then Meadow, Beach Day, Snow Day for coins and Starry Night for hearts;
  one in use at a time), **Room** (rugs, fairy lights, on top of any background), **Helpers**.
- Helpers cost 150 coins and need battle XP (100 / 200 / 300 / 400). Each makes one camp action better for
  good: Snack Dispenser (FEED 5 battles), Grooming Kit (GROOM 5 battles), Cozy Nest (REST +60%), Quiet Corner
  (FOCUS +60%).
- Coin and heart items spend from the wallet at once. Real-money items go through `IPurchaseService` (mock:
  instant success). No random items anywhere.

### 1.10 Leaderboards, news, settings
- **Leaderboards**: ◀ BOARD ▶ pager over Levels and Battle rating (with each keeper's league). Your row sits at
  the top with your true rank, then a TOP 25 divider. Tapping a row opens that keeper's profile: their cat in its
  own coat, level, stage, league and style, streak. 24 stable mock keepers behind `ILeaderboardService`.
- **News**: the bell opens team messages (NEWS / UPDATE / BUG FIX / EVENT). Opening marks them read, in the save.
  `MockNewsService` behind `INewsService`.
- **Settings**: Sound & reminders (two sliders, the **Rested reminder** switch), Account (log out keeps the pet,
  sign in re-links), Friends & codes (your invite code; a friend's code pays 50 coins once; promo codes `COZY`
  +100 coins and `SPARKLE` +20 hearts, once each), Purchases & about (top up, restore purchases and
  privacy/terms are stubs, **Start over**: two taps delete the save and return to onboarding).

### 1.11 The reminder
On pause or quit, and only when the switch in Settings is on, `NotificationScheduler.ScheduleReadyReminder`
schedules **one** message for the moment health and mana are full again: "Mochi has its health and mana back,
and is ready for the Battle Club whenever you are." Skipped when that is under a minute away, pushed out of
21:00 to 09:00. Nothing decays, so there is nothing to nag about. Delivery is a log stub today.

## 2. Numbers in one place

| What | Value | Where |
|---|---|---|
| Health and mana | `1000 + 100 × level` each; training and the Heart Locket raise health | `BattleSystem.BaseStats` |
| Full refill by time | 600 s, the app open or not | `BattleSystem.FullRegenSeconds` |
| Worn out | under 10% health | `BattleSystem.FightThreshold` |
| Camp cooldown, Treat cooldown | 60 s, 60 s | `CampSystem` |
| REST / FOCUS / Treat | 40% (60% with helper) / 40% (60%) / 15% of both | `CampSystem` |
| FEED / GROOM | +10% ATTACK / DEFENSE for 3 battles (5 with helper) | `CampSystem` |
| Bar block | 250 points, 2 px gaps | `SegmentedBar` |
| Poke heat | irritated from 8, may leave from 12, cools 1 per 2.5 s, away 5 s | `HUDController` |
| Pet XP | `60(n-1)² + 40(n-1)` to level n, max 12 | `LevelSystem` |
| Evolution | one stage per 250 battle XP, max 5 | `SkillTreeSystem` |
| Login streak | `10 × day` coins, max 50; 10 hearts every 7th day | `GameBootstrap.ApplyLoginStreak` |
| Quiet hours | 21:00 to 09:00 | `NotificationScheduler` |

## 3. Technical paths

| Path | Where | Notes |
|---|---|---|
| Boot | `Core/GameBootstrap` | Awake: settings, clock, save service. Start: onboarding or `BootWith`, which builds the systems into a `GameContext` and calls `HUDController.Initialize` |
| Wiring | `GameBootstrap.BootWith` | `CampSystem` is handed to `BattleSystem.Buffs`; its cooldown scale comes from `BoostSystem`; level-ups pay hearts |
| Save and load | `Persistence/LocalJsonSaveService` | JSON in `Application.persistentDataPath/gotchi_save.json`; lists, not dictionaries; unknown old fields are ignored |
| Time | `Core/GameClock` | every system takes one; tests use a fake clock, `-hour` pins the local hour |
| Battle rules | `Systems/BattleSystem` | pure C#: catalogue, stats, damage, vitals, leaning, ladder, quests, trades; no Unity types |
| The fight | `MiniGames/BattleMiniGame` + `MiniGameStage` | turn loop, AI, staging, cues; `AutoPlay` for capture runs |
| The cat | `UI/PetPortraitView` → `Creature3D/Cat3DView` | off-screen stage into a render texture; `SetFace`, `Play`, `SetWornOut`, `StageForBattle` |
| Purchases | `Economy/IPurchaseService` | mock now; StoreKit later (Guideline 3.1.1) |

## 4. Dev and QA paths

- The Gotchi menu, the headless commands and every player flag are in `10-unity-setup.md`.
- After any change to a system: run the smoke test. After any change to a screen: run the matching capture
  (`-tempsave -screenshot-battle <base>` covers the home blocks, the club, the Market, a whole fight and the
  camp) and look at the images, then grep the player log for exceptions.
- Always `-tempsave` for runs that spend coins or pick a style, so the real save is left alone.

## 5. Not built yet

The six battle clips and battle effects, sound, evolution looks, a story rewritten for a fighter, the world to
explore and more characters, live PvP and match-making, real trading, cloud save and real accounts, real
leaderboards and news, real notifications, StoreKit with receipt validation, the privacy policy and terms,
store metadata. Tracked in `08-project-checklist.md`.
