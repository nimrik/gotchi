# Gotchi — Game Design

Status: draft v0.1 — captures core loop decisions; numbers/tuning are placeholders until prototyped.

## Core loop — three layers

### Layer 1: Basic needs (early game)
- 4 basic needs (e.g. hunger, hygiene, energy, happiness — exact names TBD).
- Player manually satisfies these; this is the emotional "direct care" hook the genre is loved for.
- Pet **never dies** and is generally forgiving — appropriate for the target age group and for
  Kids Category sentiment. Neglect should have visible-but-gentle consequences (sad/sluggish states),
  not loss states.

### Layer 2: Automation
- As the player progresses, tools/upgrades let basic needs become partially automated.
- Automation should reduce *friction*, not remove the emotional core — the pet should still be visibly
  present and interacted with, just less about manual meter-topping.

### Layer 3: Skill tree / strategy
- Branches: sport / social / fashion / science / others TBD.
- Choices here drive the creature's **evolution branch** — this is the long-term differentiation from
  "pet that never changes."
- Cost/time curves should scale gradually and mathematically (Clash of Clans town-hall-style scaling),
  designed and tuned only after the core loop is validated in a prototype — don't hand-tune numbers on
  paper before that.

## Session design

- Target core session: **2–3 minutes** (Brawl Stars-style: quick, complete, satisfying).
- Should support optional longer play (queue up another "match"/mini-game round rather than being forced out).
- Real-time simulation: needs progress while app is closed.
- Push notifications used to prompt care — must never be sleep-hour-intrusive, and (per Kids Category
  norms) must avoid manipulative "your pet is suffering" framing.

## Difficulty modes

- **Normal** — standard pacing (default, and only mode for MVP).
- **Hardcore** (future / v2+) — 1:1 real time-to-game time ratio. Meaningfully different simulation model;
  deliberately deferred past MVP to avoid inflating QA/scope before the core loop is proven.

## Pets

- v1: single pet per player.
- Multiple/simultaneous pets, breeding, trading, etc. — explicitly deferred, revisit post-launch.

## Evolution & progression

- Creature is level-based.
- Branching evolutions driven by which skill-tree paths the player invests in.
- Exact branch count, visual distinctiveness per branch, and "how many endings" — open, needs design pass
  once skill tree categories are locked.

## Mini-games

- Confirmed direction: yes, mini-games are part of the plan.
- Both single-player and multiplayer variants under consideration.
- Some rendered as 2D (pixel-art world), some as 3D (Brawl Stars-style arena) — ties into art direction.
- Specific mini-game concepts: **open, needs a dedicated brainstorm doc.**

## Retention & long-term hooks

- Login streaks.
- Mini-game leaderboards / social rankings.
- Push notifications (bounded — see session design).
- Seasonal updates.
- Unique/limited-time cosmetic items for creatures.

## Open questions

- Exact names/count of basic needs.
- Exact skill-tree categories beyond the four named (sport/social/fashion/science).
- Mini-game concepts and which are single- vs multiplayer.
- Whether automation tools are IAP-able or purely progression-earned (affects monetization doc).
