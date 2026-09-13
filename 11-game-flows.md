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
- Header: coin/gem pills, Leaderboards, Settings; the Shop button floats top-right of the room, under the gear.
- Room: the creature, a mood speech bubble, decor; status bar with name/species and the **wish chip**
  (lowest need under 70 → "Snack time?" etc.; tapping performs that care action; "All cozy!" otherwise).
- **Skills card**: stage, leading branch + progress, **Train** → Skills panel.
- The **dock** (Feed / Clean / Rest / Play): each button sits inside a radial ring that is the meter of its
  need (half a ring = 50%), with a cooldown badge; under 10% a tooltip with the exact percentage pops above
  the button for 5 s and re-shows each time it ticks down (`NeedRingView`).
- Needs drain every frame (`NeedsSystem.Tick`); emotion refreshes 4×/s; autosave every 30 s and on
  pause/quit (`OnApplicationPause`, `OnApplicationQuit`), which also schedules care reminders.

### 1.3a Tapping the pet
A hop with squash-and-stretch, hearts, a random happy mood in the bubble (Joy / Love / Excitement / Curiosity /
Gladness for 4 s), a "Boop!" float, and +1 Happiness at most every 10 s.

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
1. Skills panel lists all 7 branches with XP and stage progress. A branch is **Later** while any need it
   draws on is under 30 (`SkillGate`): Sport = Hunger+Energy, Social = Happiness+Hygiene, Warrior =
   Energy+Happiness, Hunter = Hunger+Energy, Science = Energy, Nature = Happiness, Explorer = Energy+Hunger.
   Every branch has a playable game today; online PvP / co-op versions of Arena and Trail are future work.
2. Play → `MiniGameOverlayView.Open` attaches the game (`MiniGameRegistry`) to the play area. Your own
   creature appears in every game (`MiniGameStage`) and reacts: it lunges at popped bubbles and caught
   critters, cheers on combos, winces at thorn bubbles, a friend NPC asks the quiz questions, and it waters
   the sprout in the garden.
   Sport *Bubble Dash* (tap bubbles, combo; golden bubbles pay 3×, thorn bubbles from tier 2 cost points and reset the combo), Social *Word Pals* / Science *Curious Minds* (5-question
   quiz), Hunter *Meadow Hunt* (3 waves of drifting critters), Warrior *Arena* (solo duel: block the telegraphed
   attack in a 0.4 s window — perfect ≤0.18 s — then strike while the rival is open; 3 hearts each, rival HP
   grows with tier), Explorer *Trail Memory* (a route of 3–7 directions flashes, repeat it; 5 routes; the pet hops
   along the trail), Nature *Bloom Sort* (12 seeds, tap the pot of the seed's colour before it wilts; flowers
   bloom in the pots).
   **Difficulty** (`MiniGameDifficulty.For`): tier = 1 + branch stage (XP/250) + character level ÷ 4, capped at 9;
   intensity = (tier−1)/8. Bubble Dash: faster spawns, shorter/smaller bubbles, win score +60/tier. Quizzes:
   5→8 questions, per-question timer from tier 3 (10 s → 4 s). Meadow Hunt: +1 wave and +1 critter per 2 tiers,
   speed ×(1+0.6·intensity). Bloom Sort: seed timer 2.6 s → 1.3 s. Arena: wind-up 1.1 s → 0.55 s, rival HP 3 → 6. Trail: +1 step per 2 tiers, flash gap 0.55 s → 0.32 s.
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
Swipeable pages (or tap the tabs): Gems (real money; hidden for under-13) · Boosts (coins↔gems exchange, Treat Box, Zoomies, Lucky Hour,
Streak Shield) · Style (outfits — bought items are worn immediately, tap to swap) · Room (rugs, fairy
lights — applied to the room) · Helpers (automation). Boost timers show remaining minutes on their rows.
See the economy section of `05-monetization-compliance.md`.
- Coin/gem items spend from the wallet immediately. Real-money items go through `IPurchaseService`
  (mock: instant success; production: `StoreKitPurchaseService`) and are hidden for the under-13 band.
- Helpers (automation) need coins + XP in a branch; each halves-ish the decay of one need (×0.6).
- Every item is a fixed, visible purchase — no loot boxes.

### 1.7b Leaderboards & profiles
Trophy in the header → tabs for Level and each of the 7 skills (top 25). Your entry is inserted at its real
rank and highlighted; tapping any entry opens that player's **profile** (name, pet + species preview, level,
stage/path, all branch XP, streak). `ILeaderboardService` is mocked with 24 stable fake players
(`MockLeaderboardService`); Supabase implements the same interface later.

### 1.8 Settings
Category menu (Sound & reminders / Account / Friends & codes / Purchases & about), each a single page with
no scrolling; Back returns to the menu, then closes.
Sound sliders (PlayerPrefs, `AudioListener.volume`), care-reminder toggle, account (log out keeps the pet;
sign in re-links), invite code (copy) and friend's code (+50 coins once), promo codes (`COZY` +100 coins,
`SPARKLE` +20 gems, once each), top-up → Shop, restore purchases (stub), privacy/terms (stub), and
**Start over** (two taps → delete save → scene reload → onboarding).

### 1.9 Reminders
On pause/quit `NotificationScheduler` computes when each need would hit 30 and schedules a gentle message,
pushed out of 21:00–09:00. Delivery is a log stub until `com.unity.mobile.notifications` is wired.

### 1.10 Numbers (tunable constants)
Decay per hour: Hunger 9, Happiness 7, Energy 6, Hygiene 4 → full to empty ≈ 11 h / 14 h / 17 h / 25 h
(`NeedsSystem`). Restore: +30 (Play +25), 20 s cooldown (`CareActionService`). Offline catch-up cap 36 h.
Helpers multiply decay by 0.6 (`AutomationSystem`).

## 2. Technical paths

| Path | Where | Notes |
|---|---|---|
| Boot | `Core/GameBootstrap` | Awake: settings + save service. Start: onboarding or `BootWith` → systems → `HUDController.Initialize`. |
| Save/load | `Persistence/LocalJsonSaveService` | JSON in `Application.persistentDataPath/gotchi_save.json`, lists not dictionaries (JsonUtility). `SupabaseSaveService` is the stub for cloud. |
| Auth | `Persistence/IAuthService` | `MockAuthService` today; Supabase Auth later. Token in save, password never. |
| Purchases | `Economy/IPurchaseService` | Mock now; StoreKit via Unity IAP for iOS (Guideline 3.1.1 — no Stripe for in-app digital goods). |
| Emotion engine | `Systems/EmotionSystem` | Ambient from needs (thresholds 15/35/55/72/85) + timed event overrides; all 45 states reachable. |
| Rendering | `UI/UIFactory` sprites | Native-resolution, anti-aliased UI; the character alone is pixel art via `CreatureSprites` (112-px resample). |
| UI | `UI/UIFactory` + views | Everything built in code; procedural icons; fonts in `Resources/Fonts`. |
| Creature | `UI/PetPortraitView` + `CreatureSprites` | Uses `Resources/Creatures/<species>.png` when present (cat, seal today), resampled to a fixed 112-px grid so the character stays chunky at any screen resolution, with emotion + condition overlays; otherwise the procedural chibi. |
| Sprite prep | `Editor/CreatureSpriteTools` | Gotchi ▸ Prepare Creature Sprites keys/crops the reference art; the postprocessor forces point filtering, no compression. |

## 3. Dev & QA paths

- **Editor menu** Gotchi ▸ Create Main Scene / Configure Player Settings / Prepare Creature Sprites /
  Run Logic Smoke Test / Build Mac / Build iOS. All also run headless with `-executeMethod`.
- **Smoke test** (`RunLogicSmokeTest`) exercises needs, emotions, cooldowns, evolution lock-in, shop,
  automation, save round-trip and quiet hours with a fake clock — run it after any systems change.
- **Screenshot hook**: `Gotchi -screenshot <base>` captures home / skills / shop / settings / mini-game /
  results and quits; `Gotchi -fresh -screenshot <base>` captures the three onboarding screens.
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
