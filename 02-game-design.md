# Gotchi — Game Design

Status: draft v0.1 — captures core loop decisions; numbers/tuning are placeholders until prototyped.

## Core loop — three layers

### Layer 1: Basic needs (early game)
- 4 basic needs, **finalized**: **Hunger, Hygiene, Energy, Happiness** (classic Tamagotchi set).
- Player manually satisfies these; this is the emotional "direct care" hook the genre is loved for.
- Mini-games spend needs and give one back: each round costs a little food, hygiene and energy (more at
  higher tiers) and raises happiness, so training and caring alternate instead of competing.
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
| **PvP** *(renamed from Warrior 2026-09-14)* | Battles | Turn-based battles in the Pokémon Sapphire mould: FIGHT / ITEM / CHEER / RUN, four moves with PP, four types (Normal, Fire, Water, Grass), speed order, STAB, critical hits, stat stages, narrated text box. Solo vs. other players' pets (mock roster) now; live PvP later. | Keep visually stylized/kawaii rather than gritty — mild cartoon-violence framing is fine and expected to land around a 9+ content rating, but worth keeping intentional rather than accidental. |
| **Hunter** | Survival/tracking | PvE mini-games, solo | Singleplayer only — co-op lives in Explorer/Adventure instead (see below), since the two are mechanically distinct (solo PvE vs. co-op-with-a-friend). |
| **Science** | General knowledge | Puzzle/matching-style quiz format | Second edutainment branch, pairs with Social for a "smart pet" positioning angle. |
| **Nature** *(replaced Fashion on 2026-09-13)* | Garden / growing | Timing mini-game (tap when the marker is in the green zone; the sprout grows per hit) | Cosmetics stay a pure shop feature; Nature gives the roster a calm, cozy branch and a distinct mechanic. |
| **Explorer/Adventure** *(7th branch, confirmed)* | World exploration, resource gathering | Co-op PvE (play with a friend vs. enemies) | Dedicated home for co-op — one side (player + friend) against enemies, distinct from Hunter's solo PvE. |

- Branch choices drive the creature's **evolution branch** — this is the long-term differentiation from
  "pet that never changes." **Decided: single-branch-locked** — the creature evolves down whichever one
  branch the player has invested in most, rather than a blend across branches. Chosen for simpler art
  scope (one distinct evolved form per branch) and simpler save-state representation, matching the team's
  beginner Unity/C# capacity.
- Cost/time curves scale gradually and mathematically (Clash of Clans town-hall-style scaling) — see the
  **Economy curve** section below for the proposed numbers; tune them against real play, not on paper.

## Economy curve (proposal, 2026-09-14 — not yet implemented)

Goal: an easy, generous start, then a slope that keeps getting steeper so every next level, stage and
helper asks for visibly more resources than the last. Three resources, each with one job:

| Resource | Role | Earned by | Spent on |
|---|---|---|---|
| **XP** | progress (level, story chapters) | playing: care, cuddles, mini-games | nothing — it only accumulates |
| **Coins** | the working currency | mini-games, login streak, level-ups, codes | care refills, helpers, helper upgrades, decor, evolution ceremonies |
| **Hearts** | love the pet gives back (premium) | level-ups (+5), every 7th streak day (+10), bought | bonuses, premium looks, Starry Night; never required to progress |

**Level curve (XP to reach the next level).** Geometric instead of the current quadratic so the first
levels fly and the last ones take real commitment: `XP(L→L+1) = 100 × 1.45^(L−1)`.

| Level | XP for next | Cumulative | Expected time (daily 3-min sessions) |
|---|---|---|---|
| 1 → 2 | 100 | 100 | first session |
| 2 → 3 | 145 | 245 | day 1 |
| 3 → 4 | 210 | 455 | day 2 |
| 5 → 6 | 442 | 1,340 | end of week 1 |
| 8 → 9 | 1,350 | 4,300 | week 3 |
| 11 → 12 | 4,100 | 12,200 | month 2–3 |

XP income per session (unchanged sources, all fixed, no randomness): care +2 each (20 s cooldown), cuddle
+15 (45 s), mini-game 5–90 × tier multiplier — roughly 120 XP/session at the start, 250+ once tiers rise, so
the table above holds without grinding.

**Evolution stages (skill XP, per branch).** Today every stage costs a flat 250 XP. Proposed cumulative
thresholds `250 × 1.6^(n−1)`: stage 2 at 250, 3 at 650, 4 at 1,290, 5 at 2,310 — plus an **evolution
ceremony** that costs coins (`200 × 2^(n−2)`: 200, 400, 800, 1,600) so evolving is a real save-up goal,
the "you need more resources to progress" gate the design asks for. Lock-in stays at 500 XP in one branch.

**Coins: income and sinks that both scale.**
- Income grows with the pet: mini-game coins `(25 win / 10 lose) × tier multiplier × (1 + 0.1 × level)`;
  level-up bonus `50 × level`; streak `10 × day` capped at 50 (unchanged).
- Helpers cost more each time you buy one: 150, 225, 340, 500 (×1.5 per helper owned), and each helper
  has three upgrade tiers (decay ×0.6 → ×0.45 → ×0.3) at 2× the previous price.
- Needs decay a little faster at each evolution stage (`× (1 + 0.1 × (stage − 1))`), so a stage-5 pet
  needs helpers to stay cozy — automation becomes the answer to difficulty instead of more tapping.
- Care refill (Full Refill) price rises with level: `40 + 10 × level`.
- Decor/outfit prices are tiered by level (a few items unlock at L3, L6, L9) so there is always a next
  thing to save for.

**Guard-rails.** Everything stays deterministic (no drops, no gacha). Hearts are never on the critical
path: every stage and helper is reachable with coins and time alone (Apple 13+ positioning, no pay-to-win).
The pet never dies; "harder" only ever means "slower and more to save up", never "punished".

**Rollout.** Implement the level curve and stage thresholds first (they are two formulas), then the
ceremony cost and helper price ladder, then the decay scaling — each behind constants in `LevelSystem`,
`SkillTreeSystem`, `AutomationSystem` and `NeedsSystem`, with the smoke test asserting the tables above.
Old saves keep their XP; levels are recomputed from the new table on load.

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
- **Turn-based Pokémon-style battles** — maps to the PvP branch (solo vs. mock rivals now, live PvP later).
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
