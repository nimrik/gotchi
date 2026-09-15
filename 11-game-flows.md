# Gotchi — Game Flows & Paths

Status: v0.1 (2026-09-13). The reference for *how the game moves* — every player-facing flow, every
technical path behind it, and every dev/QA path — so nothing gets forgotten as the project grows. When a
flow changes, change it here in the same commit.

## 1. Player flows

### 1.1 First launch (no save on the device)
1. `GameBootstrap.Start` finds no save → shows `OnboardingView` (own canvas + pixel renderer).
2. **Account** — display name (shown on leaderboards) + email + password → `IAuthService.SignUp` (mock today, Supabase Auth later; passwords are
   never persisted) → session token stored in the save. Or **Play as guest** (no account).
3. **Who is at the door?** — grid of all 13 species with live previews; name field (default "Mochi") →
   **Open the door** → night doorstep scene: a bundle in a basket; three taps peel the blanket folds back, the
   friend is revealed shivering; **Take them home** warms the scene and the friend perks up (this is story
   chapter 1: found on the doorstep — deliberately not an egg, to avoid a Tamagotchi parody).
4. **Three questions** — age band (`under13` / `13-17` / `18+`; under-13 hides real-money shop items),
   usual play time (morning/afternoon/evening → `preferredPlayHour`, meant for reminder timing), and a
   "vibe" (Sporty/Curious/Kind/Cozy → 25 starter XP in Sport/Science/Social/Nature).
5. `CreateFromOnboarding` builds `PetSaveData`, saves, then `BootWith` builds systems + HUD.

### 1.2 Returning launch
1. Load save → `ApplyOfflineProgress`: needs decay for the time away, capped at 36 h (`NeedsSystem.MaxOfflineCatchupHours`) — the pet never dies.
2. `ApplyLoginStreak`: same calendar day → nothing; consecutive day → streak+1 and coins (10×streak, max 50) with a toast; a gap resets to day 1.
3. HUD appears; the emotion engine picks the ambient state from the needs.

### 1.3 Home screen loop
- Header: one wallet box ("1385 COINS" / "85 HEARTS" lines; tap → shop on Bonuses / Get Hearts), Leaderboards,
  News, Settings; the Shop button floats top-right of the room, under the gear. Hearts are the premium
  currency: the love your pet gives back (renamed from gems 2026-09-14 — gems had no place in the story).
- Status bar: "Mochi · Lv 4" followed on the same line by the XP bar to the next level and its caption
  ("79% to Lv 5"); species + "tap for story" underneath. The wish chip on the right (small white disc + icon) asks for the lowest need or a cuddle.
- Care block (on the floor, no band behind the bottom boxes): one box, four options in a 2×2 grid like the Sapphire battle menu — icon, name, need %; ▶ while
  held; on cooldown the name reads the seconds left and the icon dims.
- The mood bubble ("Satisfaction") is a speech box with a tail, attached beside the pet's head.
- Default background: a lofi study at dusk (window onto a night city, desk with laptop and lamp, string lights).
- Room: the creature, a mood speech bubble, decor; status bar with name/species and the **wish chip**
  (lowest need under 70 → "Snack time?" etc.; tapping performs that care action; "All cozy!" otherwise).
- **Skills card**: stage, leading branch + progress, **Train** → Skills panel.
- The **dock** (Feed / Clean / Rest / Play): each button sits inside a radial ring that is the meter of its
  need (half a ring = 50%), with a cooldown badge; under 10% a tooltip with the exact percentage pops above
  the button for 5 s and re-shows each time it ticks down (`NeedRingView`).
- Needs drain every frame (`NeedsSystem.Tick`); emotion refreshes 4×/s; autosave every 30 s and on
  pause/quit (`OnApplicationPause`, `OnApplicationQuit`), which also schedules care reminders.

### 1.3a Touching the pet
The whole creature is a soft body (`Creature/CreatureBody`): wherever a finger lands, the surface dents there and
the dent ripples around the silhouette; the creature blinks, looks at the finger and leans into it. Touch phases:
- **Tap** (< 0.35 s, no drag): the part under the finger reports as Head / Body / Paws / Tail → a part-specific
  reaction (pat / hop+wiggle / wave / tail flick), hearts, a random mood for 4 s ("Purr~", "Boop!", "High five!",
  "Not the tail!") and +1 Happiness at most every 10 s (`HUDController.OnPetTap`).
- **Hold**: the dent stays, the face melts into a content squint, it leans toward the finger and purrs (tiny
  vibration); after 0.9 s the hold counts as a tap interaction and hearts keep coming every 1.4 s.
- **Drag**: real physics. Along the floor the pet slides with the finger (pinched at the grab point); lift the
  finger and the pet comes off the ground and hangs from where it is held, swinging like a pendulum (dangling
  from an ear → worried face, held under the belly → content). Let go and it is **thrown** with the finger's
  velocity: it flies, spins, bounces off the floor and the room walls, slides and rights itself, with squash and a
  jelly ripple on every impact.
- **Calm when alone**: it never hops by itself. It stands, then sits, then lies down (and sleeps when Energy is
  low), with blinks, gaze, tail sways, ear twitches, grooming, a full stretch or a yawn every 7–16 s, and an
  occasional walk across the rug with a real four-legged gait.
- **Rub** (back-and-forth on the head or body): petting — blissful squint, sways with the hand, a heart every
  0.7 s (`PetPortraitView.EnableTouch`).
Ears, tail, paws and feet are hit-tested against their actual drawn shapes, so a press on an ear folds that ear.

### 1.3b Level & story
Level XP: +5 per care action, +15 per cuddle, +20 per helper unlocked, +half a mini-game's XP reward.
`LevelSystem.XpRequiredForLevel(n) = 60(n−1)² + 40(n−1)`, max level 12. Each level unlocks a chapter in
`StoryBook` (templated with the pet's name); level-ups toast and bounce the pet.

### 1.3c Condition visuals
Below 35 a need shows on the creature: Energy → eye bags, Hygiene → dirt smudges, Happiness → rain cloud,
Hunger → drool; a thought bubble shows the icon of the lowest low need (`PetPortraitView.SetConditions`).

### 1.4 Care action
Tap dock button (or wish chip) → `CareActionService.TryPerform` → cooldown check (20 s per action) →
need +30 (+25 for Play; a "+30 Hunger" floats over the pet) → emotion event (Satisfaction / Gladness / Relief / Joy; **Gratitude** if the need
was below 20 = a rescue) → hearts float up, portrait bounces.

### 1.5 Skills → mini-game → results
Every round costs needs and feeds one: Play (happiness) +12, Food −6, Wash −5, Energy −8 at tier 1, ×1.15 per
tier above that (`MiniGameNeeds`). The results card lists the change. Three rounds in a row take a healthy
pet under the SkillGate threshold, so play and care alternate by design.
1. Skills panel lists all 7 branches with XP and stage progress. A branch is **Later** while any need it
   draws on is under 30 (`SkillGate`): Sport = Hunger+Energy, Social = Happiness+Hygiene, Warrior =
   Energy+Happiness, Hunter = Hunger+Energy, Science = Energy, Nature = Happiness, Explorer = Energy+Hunger.
   Every branch has a playable game today; online PvP / co-op versions of Arena and Trail are future work.
2. Play → `MiniGameOverlayView.Open` attaches the game (`MiniGameRegistry`) to the play area. Your own
   creature appears in every game (`MiniGameStage`) and reacts: it lunges at popped bubbles and caught
   critters, cheers on combos, winces at thorn bubbles, a friend NPC asks the quiz questions, and it waters
   the sprout in the garden.
   Sport *Bubble Dash* (tap bubbles, combo; golden bubbles pay 3×, thorn bubbles from tier 2 cost points and reset the combo), Social *Word Pals* / Science *Curious Minds* (5-question
   quiz), Hunter *Meadow Hunt* (3 waves of drifting critters), PvP *Battle* (turn-based, Sapphire style: FIGHT opens four moves with PP — TACKLE 40, a
   type move 50 (EMBER / BUBBLE / VINE WHIP / QUICK ATTACK), HEADBUTT 65 @85%, GROWL lowers the rival's ATTACK;
   ITEM eats one of two treats for 40% HP; CHEER raises your ATTACK; RUN forfeits. Speed decides order,
   STAB ×1.5, Fire>Grass>Water>Fire ×2 / ×0.5, crits 1/16 ×2, damage = ((2L/5+2)·P·A/D)/25+2. First tap on a
   move shows PP and TYPE, second tap uses it. Rival is a random pet from the leaderboard roster at level
   L+tier−1; from tier 3 it picks its best move 70% of the time), Explorer *Trail Memory* (a route of 3–7 directions flashes, repeat it; 5 routes; the pet hops
   along the trail), Nature *Bloom Sort* (12 seeds, tap the pot of the seed's colour before it wilts; flowers
   bloom in the pots).
   **Difficulty** (`MiniGameDifficulty.For`): tier = 1 + branch stage (XP/250) + character level ÷ 4, capped at 9;
   intensity = (tier−1)/8. Bubble Dash: faster spawns, shorter/smaller bubbles, win score +60/tier. Quizzes:
   5→8 questions, per-question timer from tier 3 (10 s → 4 s). Meadow Hunt: +1 wave and +1 critter per 2 tiers,
   speed ×(1+0.6·intensity). Bloom Sort: seed timer 2.6 s → 1.3 s. Battle: rival level +1 per tier, smarter AI from tier 3. Trail: +1 step per 2 tiers, flash gap 0.55 s → 0.32 s.
   Rewards ×(1 + 0.25·(tier−1)); the overlay title and results card show the tier.
3. Game ends → `MiniGameRewards.Build` (XP 5–90 from score, 25/10 coins win/lose, times the tier multiplier, no randomness) →
   `GameBootstrap.HandleMiniGameResult`: XP to the branch, coins, Energy −8, Happiness +10, emotion
   (Excitement / Amazement ≥300 score / Embarrassment on a loss), save.
4. Results card: stars (1 lose, 2 win, 3 win with ≥60 XP), score count-up, confetti, **Play again**.

### 1.6 Evolution
XP per branch → stage = XP/250 (max 5) on the **evolution branch**. Until lock-in the evolution branch is
the leading one; when any branch reaches 500 XP it **locks** (single-branch-locked, see `02-game-design.md`)
and only that branch advances the form. Both events toast and refresh the Skills card.

### 1.7 Shop & helpers
Every Buy opens a YES / NO text box first ("Buy Treat Box for 40 coins?" / "…through the App Store?") — this
   is the purchase-confirmation friction step. Swipeable pages (or tap the arrows): Get Hearts (real money; hidden
for under-13: 50 / 150 / 400 Hearts, Starter Pack) · Bonuses (500 Coins for 20 hearts, Full Refill, No Cooldowns,
Double Rewards, Streak Shield) · Style (outfits — bought items are worn immediately, tap to swap) ·
Backgrounds (the free Cozy Room first, then Meadow / Beach Day / Snow Day for coins and Starry Night for
hearts; one set is in use at a time — Use swaps it, the room and the screen backdrop change at once, bought
sets stay owned) · Room (rugs, fairy lights — on top of any background) · Helpers (automation). Bonus timers
show remaining minutes on their cards.
See the economy section of `05-monetization-compliance.md`.
- Coin/heart items spend from the wallet immediately. Real-money items go through `IPurchaseService`
  (mock: instant success; production: `StoreKitPurchaseService`) and are hidden for the under-13 band.
- Helpers (automation) need coins + XP in a branch; each halves-ish the decay of one need (×0.6).
- Every item is a fixed, visible purchase — no loot boxes.

### 1.7b Leaderboards & profiles
LEADERBOARD title and a ◀ BOARD ▶ pager: Levels, then Sport · Social · PvP · Hunter · Science · Nature · Explorer.
Your own row (with your true rank, pink) sits at the top of every board above a "TOP 25" divider, then the top
25 without you — so your standing is the first thing you read.
Trophy in the header → tabs for Level and each of the 7 skills (top 25). Your entry is inserted at its real
rank and highlighted; tapping any entry opens that player's **profile** (name, pet + species preview, level,
stage/path, all branch XP, streak). `ILeaderboardService` is mocked with 24 stable fake players
(`MockLeaderboardService`); Supabase implements the same interface later.

### 1.8 Settings
Same layout as the shop: SETTINGS title, ◀ PAGE ▶ pager (Sound & reminders · Account · Friends & codes ·
Purchases & about) with swipeable pages, Back at the bottom.
Category menu (Sound & reminders / Account / Friends & codes / Purchases & about), each a single page with
no scrolling; Back returns to the menu, then closes.
Sound sliders (PlayerPrefs, `AudioListener.volume`), care-reminder toggle, account (log out keeps the pet;
sign in re-links), invite code (copy) and friend's code (+50 coins once), promo codes (`COZY` +100 coins,
`SPARKLE` +20 hearts, once each), top-up → Shop, restore purchases (stub), privacy/terms (stub), and
**Start over** (two taps → delete save → scene reload → onboarding).

### 1.9 Reminders
On pause/quit `NotificationScheduler` computes when each need would hit 30 and schedules a gentle message,
pushed out of 21:00–09:00. Delivery is a log stub until `com.unity.mobile.notifications` is wired.

### 1.10 Numbers (tunable constants)
Decay per hour: Hunger 9, Happiness 7, Energy 6, Hygiene 4 → full to empty ≈ 11 h / 14 h / 17 h / 25 h
(`NeedsSystem`). Restore: +30 (Play +25), 20 s cooldown (`CareActionService`). Offline catch-up cap 36 h.
Helpers multiply decay by 0.6 (`AutomationSystem`).

### 1.9 Story
Tap the name bar on the home screen. The story panel plays like an RPG cutscene: the pet on a framed scene,
a text box below with the chapter typed out, tap to continue. Chapters above the pet's level show
"Reach level N to read the next chapter." The doorstep intro uses the same text box and ends with a YES / NO
choice ("Take Mochi home?"); NO returns to character selection.

### 1.10 Notification center
Bell in the header (between trophy and gear) with a pink dot while anything is unread. Opens a list of
team messages (NEWS / UPDATE / BUG FIX / EVENT tags, date, title, body). Opening marks everything read and
saves. Feed is `MockNewsService` now; swap for the Supabase table behind `INewsService`.

### 1.11 Touching your pet
Four touch zones on the character: head (pat: tilt + blink, Love/Warmth), body (wiggle + hop, Joy),
paws (paw wave, Pride), tail (flick, Annoyance — no hearts). Each shows a word and nudges Happiness at
most once per 10 s. The pet also idles on its own: blinks, nods, waves and short walks across the rug (walk loop with
alternating feet). Emotions switch the base loop (happy bounce, sad droop, alert, sleep when energy is low)
and play an entrance clip (hop, shiver, wiggle, nod). In battle the rig plays attack, hurt and faint clips.

## 2. Technical paths

| Path | Where | Notes |
|---|---|---|
| Boot | `Core/GameBootstrap` | Awake: settings + save service. Start: onboarding or `BootWith` → systems → `HUDController.Initialize`. |
| Save/load | `Persistence/LocalJsonSaveService` | JSON in `Application.persistentDataPath/gotchi_save.json`, lists not dictionaries (JsonUtility). `SupabaseSaveService` is the stub for cloud. |
| Auth | `Persistence/IAuthService` | `MockAuthService` today; Supabase Auth later. Token in save, password never. |
| Purchases | `Economy/IPurchaseService` | Mock now; StoreKit via Unity IAP for iOS (Guideline 3.1.1 — no Stripe for in-app digital goods). |
| Emotion engine | `Systems/EmotionSystem` | Ambient from needs (thresholds 15/35/55/72/85) + timed event overrides; all 45 states reachable. |
| Rendering | `UI/UIFactory` sprites | Native-resolution, anti-aliased UI built from procedural sprites; the creature is a live vector mesh (see below). |
| UI | `UI/UIFactory` + views | Everything built in code; procedural icons; fonts in `Resources/Fonts`. |
| Creature | `Creature/CreatureBody` (+ `CreatureBrain`, `SoftBody`, `VectorMesh`, `Spring`, `CreatureLook`, `Expressions`) wrapped by `UI/PetPortraitView` | One uGUI `MaskableGraphic` per creature that regenerates an anti-aliased vector mesh every frame: soft-body silhouette, warped features, spring-driven face and pose, in-mesh mood glow, conditions and cosmetics. No sprites, no textures. |
| Creature lab | `Core/CreatureLab` | `Gotchi -lab <dir>` renders all species, an emotion sheet and frame bursts of hops/touches into PNGs and quits — use it to review any creature change. |

## 3. Dev & QA paths

- **Editor menu** Gotchi ▸ Create Main Scene / Configure Player Settings /
  Run Logic Smoke Test / Build Mac / Build iOS. All also run headless with `-executeMethod`.
- **Smoke test** (`RunLogicSmokeTest`) exercises needs, emotions, cooldowns, evolution lock-in, shop,
  automation, save round-trip and quiet hours with a fake clock — run it after any systems change.
- **Screenshot hook**: `Gotchi -screenshot <base>` captures home / skills / shop / shop-backgrounds /
  home-meadow·beach·snow·night (each set applied by editing the save, then reset) / settings / leaderboard /
  news / story / mini-games and quits; `Gotchi -fresh -screenshot <base>` captures the onboarding screens.
- **Builds**: Mac to `Builds/Mac/Gotchi.app`; iOS Xcode project to `Builds/iOS/` (arm64 compile verified).
- **Logs**: Mac player log at `~/Library/Logs/Gotchi/Gotchi/Player.log`.
- Gotchas worth remembering: bare projects lack uGUI; layout-group force-expand overrides child weights;
  desktop players pause when unfocused unless Run In Background is on; a killed batch run leaves
  `Temp/UnityLockfile`.

## 4. Flows not built yet (tracked in `08-project-checklist.md`)

Warrior PvP arena and Explorer co-op (networking + matchmaking), real sprite sets for the other 11 species
and the 45 emotion states (`09-pets-and-emotions.md`), cloud save + Supabase auth/storage, leaderboards,
seasonal content, real push notifications, StoreKit purchases with receipt validation, purchase-confirmation
friction step, Apple age-rating questionnaire, privacy policy/terms, store metadata, soft launch.
