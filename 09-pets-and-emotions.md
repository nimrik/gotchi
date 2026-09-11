# Gotchi — Pet Species & Emotion States

Status: draft v0.1 — species roster and emotion taxonomy locked; art production plan (variation counts,
sequencing) still open.

## Species roster (13, finalized)

Player chooses their starting creature from these 13 species. All are generic animal types (no
copyrighted/trademarked character names) to stay clear of IP issues in store copy and marketing:

Bunny, Cat, Panda, Red Panda, Seal, Raccoon, Penguin, Fennec, Fox, Pig, Otter, Hedgehog, Dog.

(A deer/fawn species was considered and dropped — "Bambi" is a specific Disney trademark, not a generic
name, and the team chose not to include a reworked version of it either.)

## Emotion taxonomy (finalized categories/sub-emotions)

Every sub-emotion below gets its own unique pixel-art state per creature (not shared/collapsed with
siblings). The emotion → gameplay-state wiring (what triggers each state) is handled in code by the team,
not specified here — this doc only defines the art states needed.

| Category | Sub-emotions |
|---|---|
| Joy / Happiness | Joy, gladness, relief, love, pride, satisfaction |
| Sadness | Grief, sorrow, loneliness, despair, depression |
| Anger | Rage, fury, irritation, annoyance, resentment |
| Fear | Terror, panic, anxiety, worry, nervousness |
| Disgust | Dislike, revulsion, contempt, aversion |
| Surprise | Astonishment, amazement, shock |
| Guilt & Shame | Remorse, regret, embarrassment, humiliation |
| Connection & Care | Compassion, empathy, gratitude, affection, warmth |
| Vulnerability | Helplessness, powerlessness, inadequacy, overwhelmed |
| Interest & Awe | Curiosity, wonder, inspiration, excitement |

45 sub-emotions total across 10 categories.

## Art production scope (open — needs a plan before Phase 3 starts)

- **Scale:** 45 sub-emotions × 13 creatures = 585 unique pixel-art states minimum, before counting
  multiple pose/expression variations per sub-emotion ("several per emotion" was floated but not decided —
  see open question below). This is a very large art production job for a two-person, beginner-Unity/C#,
  side-project-pace team (see `01-vision.md` team & scope reality check) — plan for it explicitly rather
  than treating it as a normal-sized task.
- **Recommended sequencing (not yet decided):** produce the full 45-state set for one creature first,
  validate the pipeline (style consistency, production time per state, in-game legibility at target
  resolution) before committing to all 13 — mirrors the "1-2 branches before full build-out" approach
  already used for the skill tree in `08-project-checklist.md` Phase 2.
- **Where this art is used:** likely a 2D portrait/status representation (icon or small sprite) rather
  than full-body 3D animation for all 45 states on the 3D creature model — full 3D emotional animation at
  this granularity would be a much larger scope again. Needs a decision alongside the open "when is the
  creature 2D vs 3D" question in `03-art-direction.md`.

## Open questions

- How many pose/expression variations per sub-emotion ("several" was mentioned but not quantified) —
  1 canonical state per sub-emotion, or a small set (e.g. 2-3) per state?
- Which creature gets the full 45-state set first, to validate the art pipeline before scaling to all 13?
- Confirm these states render as 2D portraits/icons rather than full 3D creature animations (affects both
  art cost and the tech rendering-mode question in `03-art-direction.md`).
