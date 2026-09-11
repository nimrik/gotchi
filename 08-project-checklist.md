# Gotchi — Project Checklist

Status: draft v0.1 — a working checklist tied to the phases in `06-roadmap.md`. Check items off as you go;
add new ones as decisions get made. This is meant to be edited constantly, not treated as fixed scope.

## Phase 0 — Foundations

- [x] Vision & differentiation locked (`01-vision.md`)
- [x] Basic needs finalized: Hunger / Hygiene / Energy / Happiness
- [x] Skill-tree branches finalized (6): Sport / Social / Warrior / Hunter / Science / Fashion
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
- [ ] Lock the internal render resolution for the 2D pixel-art layer — **decided to defer**: build a small
      test scene at 2–3 candidate resolutions (e.g. 320×180, 384×216) on an actual device before locking,
      rather than picking on paper
- [x] Assess team weekly time budget and current Unity/C# comfort level, honestly — **confirmed:
      side-project pace (a few hours/week each), both beginners at Unity/C#.** Roadmap should build in a
      real learning-curve buffer, especially through Phase 1.
- [ ] Sketch real store positioning (icon direction, screenshot style, description tone) supporting the
      13+ audience shift — needs to be genuine, not just a label — left open

## Phase 1 — Core loop prototype

- [ ] Set up the Unity project (version, render pipeline — see `04-tech-plan.md`)
- [ ] Build a placeholder pet with the 4 basic-need meters (no art polish, primitives/placeholder shapes fine)
- [ ] Implement manual care interactions for each need
- [ ] Implement real-time simulation (needs progress while app is closed, not just while open)
- [ ] Internally playtest the 2–3 minute core session — validate it's actually satisfying
- [ ] Explicitly hold off on: skill tree, monetization, notifications, mini-games (Phase 1 is loop-only)

## Phase 2 — Progression layer

- [ ] Implement automation mechanics for basic needs (reduce friction, don't remove the care feeling)
- [ ] Build 1–2 skill-tree branches first (not all 7) to validate the system before full build-out
- [ ] Implement a first evolution branch point (even one split is enough to validate the concept)
- [ ] Design and tune cost/time scaling curves (Clash of Clans-style) — only after the prototype feel is
      validated, not on paper beforehand

## Phase 3 — Art pass

- [ ] Finalize mood board and internal render resolution (carried from Phase 0 if not done yet)
- [ ] Lock the art style guide (palette, proportions, shading rules) via the Nano Banana → PixelLab.ai
      pipeline in `03-art-direction.md`, before starting bulk sprite production
- [ ] Source/generate 2D world & UI assets (PixelLab.ai and similar tools)
- [ ] Commission or build the 3D creature model + rig + core animations (recommended: extra care here,
      possibly a freelance artist — see `03-art-direction.md`)
- [ ] Produce the full 45-state emotion sprite set for one creature first to validate the pipeline, before
      scaling to all 13 species (see open questions in `09-pets-and-emotions.md`)
- [ ] Apply real art to the already-validated prototype from Phases 1–2

## Phase 4 — Retention & mini-games

- [ ] Build the Sport branch mini-game (tap/reflex, 2D) — likely simplest, good first build
- [ ] Build the Social + Science quiz/matching mini-game format (shared structure, different content sets)
- [ ] Build the Hunter branch PvE mini-game (solo)
- [ ] Build the Explorer/Adventure branch co-op mini-game (play with a friend vs. enemies)
- [ ] Build the Warrior branch PvP arena (3D, Brawl Stars-style) — likely the most technically involved,
      consider sequencing this later within the phase
- [ ] Login streak system
- [ ] Push notifications with quiet-hours logic (no nighttime pings)
- [ ] Leaderboards / social rankings — only after compliance questions below are resolved
- [ ] Seasonal content system groundwork (can be minimal for v1)

## Phase 5 — Compliance & store readiness

- [ ] Complete Apple's age rating questionnaire honestly; confirm what it actually calculates to
- [ ] Write the privacy policy
- [ ] Implement the age-band collection flow (local/anonymous, not tied to persistent identifiers)
- [ ] Audit every third-party SDK (analytics, crash reporting, IAP) against the compliance notes in
      `07-apple-compliance-questionnaire.md`
- [ ] Implement the purchase-confirmation friction step for the IAP/shop flow
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
