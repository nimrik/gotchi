# Gotchi — Project Checklist

Status: draft v0.2 — a working checklist tied to the phases in `06-roadmap.md`. `[~]` = partially done. Check items off as you go;
add new ones as decisions get made. This is meant to be edited constantly, not treated as fixed scope.

## Phase 0 — Foundations

- [x] Vision & differentiation locked (`01-vision.md`)
- [x] Basic needs finalized: Hunger / Hygiene / Energy / Happiness
- [x] Skill-tree branches finalized (7): Sport / Social / PvP (was Warrior) / Hunter / Science / Nature / Explorer-Adventure (Fashion dropped 2026-09-13)
- [x] Mini-game types confirmed: tap/reflex, 3D multiplayer arena (PvP), quiz/matching format, PvE, co-op
- [x] Audience repositioned toward tweens/teens/nostalgic young adults (not primarily under-13)
- [x] Pet species roster finalized (13): Bunny, Cat, Panda, Red Panda, Seal, Raccoon, Penguin, Fennec,
      Fox, Pig, Otter, Hedgehog, Dog — see `09-pets-and-emotions.md`
- [x] Full emotion taxonomy finalized (10 categories, 45 sub-emotions, unique art per sub-emotion) — see
      `09-pets-and-emotions.md`. Note: this is a large art-asset surface (585+ states before variation
      counts) — production sequencing still open, see that doc's open questions
- [x] Decide on a 7th **Explorer/Adventure** branch as a co-op home, or fold co-op fully into Hunter —
      **decided: add Explorer/Adventure as a 7th branch.** Hunter stays solo PvE; co-op (play with a
      friend vs. enemies) lives in Explorer/Adventure instead, since the two are mechanically distinct
      (singleplayer vs. co-op-with-a-friend), not a natural fit for one branch.
- [x] Decide evolution model: single-branch-locked vs. blended — **decided: single-branch-locked** (one
      dominant invested branch determines the evolved form). Simpler art scope (one form per branch) and
      simpler save-state representation; matches the team's beginner Unity/C# capacity.
- [ ] Build the art mood board / concrete references beyond "Celeste-inspired" — left open, needs a
      dedicated reference-gathering session
- [x] Internal render resolution — **decided: the UI renders natively (anti-aliased); only creature sprites are
      pixel art, resampled to a fixed 112-px grid** (`CreatureSprites.PixelHeight`). The 2D world art, when it
      comes, should follow the same per-sprite grid rather than a global low-res pass.
- [x] Assess team weekly time budget and current Unity/C# comfort level, honestly — **confirmed:
      side-project pace (a few hours/week each), both beginners at Unity/C#.** Roadmap should build in a
      real learning-curve buffer, especially through Phase 1.
- [ ] Sketch real store positioning (icon direction, screenshot style, description tone) supporting the
      13+ audience shift — needs to be genuine, not just a label — left open

## Phase 1 — Core loop prototype

- [x] Set up the Unity project — **Unity 6000.6.0f1, built-in RP + uGUI, iOS build target; the repo is the
      project.** Compiles with zero errors/warnings, logic smoke test passes (140 checks), `Main.unity` wired.
      URP deferred to the art phase. See `10-unity-setup.md`.
- [x] Build a placeholder pet with the 4 basic-need meters — `NeedsSystem`, `HUDController`
- [x] Implement manual care interactions for each need — `CareActionService` (cooldowns, rescue → gratitude)
- [x] Implement real-time simulation — `NeedsSystem.ApplyOfflineElapsed` (capped at 36h so the pet never dies)
- [ ] Internally playtest the 2–3 minute core session — validate it's actually satisfying
- [ ] Explicitly hold off on: skill tree, monetization, notifications, mini-games (Phase 1 is loop-only)

## Phase 2 — Progression layer

- [x] Implement automation mechanics for basic needs — `AutomationSystem` (4 unlockable helpers that slow decay)
- [x] Skill-tree system built for all 7 branches (`SkillTreeSystem`); 5 have playable mini-games, 2 are stubs
- [x] Implement a first evolution branch point — single-branch lock-in at 500 XP, 5 stages (`SkillTreeSystem`)
- [ ] Design and tune cost/time scaling curves (Clash of Clans-style) — only after the prototype feel is
      validated, not on paper beforehand

## Phase 3 — Art pass

- [ ] Finalize mood board and internal render resolution (carried from Phase 0 if not done yet)
- [ ] Lock the art style guide (palette, proportions, shading rules) via the Nano Banana → PixelLab.ai
      pipeline in `03-art-direction.md`, before starting bulk sprite production
- [ ] Source/generate 2D world & UI assets (PixelLab.ai and similar tools)
- [~] Background sets — five procedural scenes (`RoomScenes`: Cozy Room free, Meadow / Beach Day / Snow Day /
      Starry Night sold in the shop); swap the painted props for drawn art in the art pass
- [~] Creature rig + core animations — **built in code** (`Creature/`): part skeleton, pixel-painted parts,
      base loops and one-shot clips, 45 expressions. Remaining: replace painted parts with drawn part PNGs
      per species (spec in `03-art-direction.md`); the 3D model idea is dropped
- [ ] Produce the full 45-state emotion sprite set for one creature first to validate the pipeline, before
      scaling to all 13 species (see open questions in `09-pets-and-emotions.md`)
- [ ] Apply real art to the already-validated prototype from Phases 1–2

## Phase 4 — Retention & mini-games

- [x] Build the Sport branch mini-game — `TapReflexMiniGame` ("Bubble Dash")
- [x] Build the Social + Science quiz mini-game — `QuizMiniGame` + `QuizBank` (placeholder questions)
- [x] Build the Hunter branch PvE mini-game (solo) — `HuntMiniGame` (three waves of drifting critters)
- [x] Build the Nature branch mini-game — `BloomMiniGame` ("Bloom Sort" colour-matching under a wilt timer; replaced the timing bar)
- [~] Explorer/Adventure branch — solo *Trail Memory* built; the co-op (friend vs. enemies) version needs networking
- [~] PvP branch — turn-based *Battle* (Sapphire-style: moves, PP, types, STAB, crits, stat stages, items) built
      against mock rival pets; live PvP over the network remains future work
- [x] Login streak system — `GameBootstrap.ApplyLoginStreak` (escalating coin reward, capped at 50)
- [~] Push notifications with quiet-hours logic — scheduling logic done (`NotificationScheduler`); delivery is
      a log stub until `com.unity.mobile.notifications` is wired
- [ ] Leaderboards / social rankings — only after compliance questions below are resolved
- [~] Notification center (bell in the header): announcements, updates, bug fixes, events — mock feed now,
      Supabase `news` table later; read state in the save
- [ ] Seasonal content system groundwork (can be minimal for v1)

## Phase 5 — Compliance & store readiness

- [ ] Complete Apple's age rating questionnaire honestly; confirm what it actually calculates to
- [ ] Write the privacy policy
- [~] Implement the age-band collection flow — asked during onboarding, stored only in the local save;
      under-13 hides real-money shop items. Review wording/legal copy before store submission.
- [ ] Audit every third-party SDK (analytics, crash reporting, IAP) against the compliance notes in
      `07-apple-compliance-questionnaire.md`
- [~] Purchase-confirmation friction step — every Buy now asks YES / NO in the text box before spending; the
      purchase abstraction exists (`IPurchaseService`, mock for MVP); **real purchases must use Apple StoreKit,
      not Stripe** (Guideline 3.1.1)
- [ ] Finalize the IAP catalog (which cosmetics/bonuses, pricing)
- [ ] Finalize store metadata (screenshots, description, icon) consistent with the 13+ positioning

## Phase 6 — Soft launch / beta

- [ ] Limited release to a small audience
- [ ] Set up a feedback collection mechanism
- [ ] Iterate on core loop / progression pacing based on real feedback before a full marketing push

## Explicitly deferred (not v1 — revisit post-launch)

- [ ] Hardcore (1:1 real-time) mode
- [ ] Multiple/simultaneous pets, breeding, trading
- [ ] Android release
- [ ] Rhythm/music and racing/obstacle mini-game types
