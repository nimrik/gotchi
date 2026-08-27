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
| **Hunter** | Survival/tracking | PvE mini-games, supports co-op | Co-op mode confirmed as a direction — this branch is a natural home for it (e.g. cooperative hunts/expeditions). |
| **Science** | General knowledge | Puzzle/matching-style quiz format | Second edutainment branch, pairs with Social for a "smart pet" positioning angle. |
| **Fashion** *(retained from earlier planning)* | Style/cosmetics | Styling/dress-up interactions | Deliberately kept — ties directly into the cosmetics IAP model, making monetization progression-driven rather than a bolted-on shop. |

Open candidate for a 7th branch: an **Explorer/Adventure** branch (world exploration, resource gathering)
would give co-op an even more natural home alongside Hunter — worth a quick yes/no before finalizing,
not blocking for now.

- Branch choices drive the creature's **evolution branch** — this is the long-term differentiation from
  "pet that never changes." With 6 branches, worth deciding early whether evolution is single-branch-locked
  (pick one path) or blended (creature reflects a mix of invested branches) — this materially affects both
  art (how many distinct forms) and tech (state representation).
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

## Mini-games — directions confirmed

- **Quick tap/reflex (2D)** — maps to the Sport branch.
- **Multiplayer arena battles (3D, Brawl Stars-style)** — maps to the Warrior branch (PvP).
- **Puzzle/matching format** — used for both Social (language quizzes) and Science (general knowledge)
  branches; same underlying mini-game structure, different content/question sets.
- **PvE** — maps to the Hunter branch.
- **Co-op** — confirmed direction; Hunter (and possibly a future Explorer branch) are the natural home for
  cooperative play rather than treating it as a fully separate system.
- Not currently planned: rhythm/music mini-games, racing/obstacle mini-games — dropped from consideration
  for now, can revisit post-launch.

## Retention & long-term hooks

- Login streaks.
- Mini-game leaderboards / social rankings.
- Push notifications (bounded — see session design).
- Seasonal updates.
- Unique/limited-time cosmetic items for creatures.

## Open questions

- Whether to add a 7th **Explorer/Adventure** branch as a dedicated home for co-op, or fold co-op fully
  into Hunter.
- Whether evolution is single-branch-locked or reflects a blend of invested branches.
- Whether automation tools are IAP-able or purely progression-earned (affects monetization doc).
