# Gotchi — Game Design

Status: draft v0.1 — captures core loop decisions; numbers/tuning are placeholders until prototyped.

## Core loop — three layers

### Layer 1: Basic needs (early game)
- 4 basic needs, **finalized**: **Hunger, Hygiene, Energy, Happiness** (classic Tamagotchi set).
- Player manually satisfies these; this is the emotional "direct care" hook the genre is loved for.
- Pet **never dies** and is generally forgiving — appropriate for the target age group and for
  Kids Category sentiment. Neglect should have visible-but-gentle consequences (sad/sluggish states),
  not loss states.

### Layer 2: Automation
- As the player progresses, tools/upgrades let basic needs become partially automated.
- Automation should reduce *friction*, not remove the emotional core — the pet should still be visibly
  present and interacted with, just less about manual meter-topping.

### Layer 3: Skill tree / strategy — finalized branches

| Branch | Focus | Mini-game type | Notes |
|---|---|---|---|
| **Sport** | Physical stats | Quick tap/reflex mini-games (2D) | Straightforward, fast core-loop-friendly sessions. |
| **Social** | Language quizzes | Puzzle/matching-style quiz format | Genuine edutainment angle — a real differentiator, not just a flavor branch. Worth highlighting in marketing. |
| **Warrior** | Combat | PvP, multiplayer arena battles (3D, Brawl Stars-style) | Keep visually stylized/kawaii rather than gritty — mild cartoon-violence framing is fine and expected to land around a 9+ content rating, not a concern outside the Kids Category, but worth keeping intentional rather than accidental. |
| **Hunter** | Survival/tracking | PvE mini-games, solo | Singleplayer only — co-op lives in Explorer/Adventure instead (see below), since the two are mechanically distinct (solo PvE vs. co-op-with-a-friend). |
| **Science** | General knowledge | Puzzle/matching-style quiz format | Second edutainment branch, pairs with Social for a "smart pet" positioning angle. |
| **Fashion** *(retained from earlier planning)* | Style/cosmetics | Styling/dress-up interactions | Deliberately kept — ties directly into the cosmetics IAP model, making monetization progression-driven rather than a bolted-on shop. |
| **Explorer/Adventure** *(7th branch, confirmed)* | World exploration, resource gathering | Co-op PvE (play with a friend vs. enemies) | Dedicated home for co-op — one side (player + friend) against enemies, distinct from Hunter's solo PvE. |

- Branch choices drive the creature's **evolution branch** — this is the long-term differentiation from
  "pet that never changes." **Decided: single-branch-locked** — the creature evolves down whichever one
  branch the player has invested in most, rather than a blend across branches. Chosen for simpler art
  scope (one distinct evolved form per branch) and simpler save-state representation, matching the team's
  beginner Unity/C# capacity.
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

- v1: single pet per player, chosen at start from a roster of 13 species — see `09-pets-and-emotions.md`.
- Multiple/simultaneous pets, breeding, trading, etc. — explicitly deferred, revisit post-launch.

## Emotional states

- Full emotion taxonomy (10 categories, 45 sub-emotions) locked, each with a unique pixel-art state per
  creature — see `09-pets-and-emotions.md` for the full list and the art-production scope notes. This is a
  large art-asset surface; sequencing (which creature first) and variation count per state are still open.
- Emotion → gameplay-state wiring (what triggers each state) is handled in code by the team, not specified
  in the design docs.

## Evolution & progression

- Creature is level-based.
- Branching evolutions driven by which skill-tree paths the player invests in — **single-branch-locked**:
  the evolved form reflects the one branch invested in most, not a blend.
- Visual distinctiveness per branch (7 branches → 7 evolved forms) — open, needs an art pass once the
  mood board is locked.

## Mini-games — directions confirmed

- **Quick tap/reflex (2D)** — maps to the Sport branch.
- **Multiplayer arena battles (3D, Brawl Stars-style)** — maps to the Warrior branch (PvP).
- **Puzzle/matching format** — used for both Social (language quizzes) and Science (general knowledge)
  branches; same underlying mini-game structure, different content/question sets.
- **PvE (solo)** — maps to the Hunter branch.
- **Co-op** — maps to the Explorer/Adventure branch: player + friend on one side against enemies.
- Not currently planned: rhythm/music mini-games, racing/obstacle mini-games — dropped from consideration
  for now, can revisit post-launch.

## Retention & long-term hooks

- Login streaks.
- Mini-game leaderboards / social rankings.
- Push notifications (bounded — see session design).
- Seasonal updates.
- Unique/limited-time cosmetic items for creatures.

## Open questions

- Whether automation tools are IAP-able or purely progression-earned (affects monetization doc).
