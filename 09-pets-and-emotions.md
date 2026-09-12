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

## Emotion visual expression key (draft v0.1)

One-line facial-expression/body-language description per sub-emotion, written for the seated chibi base
pose in `03-art-direction.md`'s creature portrait style guide. Species-agnostic — plug the relevant row
into prompt template #3 (emotion variant) for any species. Keep expressions readable at small pixel-art
scale: exaggerated, simple shapes, not subtle.

| Sub-emotion | Expression |
|---|---|
| Joy | Wide open smile, eyes closed in a happy arc, slight forward bounce |
| Gladness | Soft closed-eye smile, relaxed shoulders, gentle head tilt |
| Relief | Eyes half-closed, slumped relaxed posture, small exhale mark above head |
| Love | Heart-shaped eyes (or small heart above head), both paws clasped near chest |
| Pride | Chin raised, chest puffed out, confidently narrowed eyes with a small smile |
| Satisfaction | Content closed-mouth smile, half-lidded eyes, one paw resting on belly |
| Grief | Eyes shut tight, deep downturned frown, one large tear, hunched posture |
| Sorrow | Droopy eyes, small tear, head tilted down |
| Loneliness | Small hunched posture, paws wrapped around self, eyes looking down and aside |
| Despair | Eyes wide and hollow, wavering open frown, shoulders slumped forward |
| Depression | Flat half-lidded eyes, straight neutral-to-down mouth, body slumped low |
| Rage | Furrowed brow, bared teeth, flushed cheeks, clenched paws, anger marks above head |
| Fury | Sharp angled eyebrows, wide shouting open mouth, whole body leaning forward |
| Irritation | One eyebrow raised, tight flat mouth line, slight squint |
| Annoyance | Narrowed eyes, small flat mouth, arms crossed |
| Resentment | Sideways glare, tight closed mouth, arms crossed, body turned slightly away |
| Terror | Eyes wide with shrunk pupils, open scream-shaped mouth, body shrinking back, sweat drop |
| Panic | Wide shaking eyes, open trembling mouth, paws raised near face |
| Anxiety | Small worried eyes, subtle frown, one paw fidgeting, sweat drop |
| Worry | Furrowed brow, small "o"-shaped mouth, eyes glancing sideways |
| Nervousness | Half-closed shifting eyes, small awkward smile, one paw scratching head |
| Dislike | One eye squinted, slight downward smirk, head tilted away |
| Revulsion | Scrunched nose, tongue out, eyes squeezed shut, leaning back |
| Contempt | One eyebrow raised, small smirk, eyes half-lidded looking down at viewer |
| Aversion | Head turned away, eyes averted, mouth in a small grimace |
| Astonishment | Round wide eyes, small round open mouth, both paws raised beside face |
| Amazement | Sparkling wide eyes, open smiling mouth, leaning forward with interest |
| Shock | Extremely wide eyes with tiny pupils, straight-line open gasp mouth, stiff/frozen body |
| Remorse | Downcast eyes, small frown, one paw rubbing back of head |
| Regret | Closed eyes, furrowed brow, head hanging low |
| Embarrassment | Deep blush, small awkward smile, eyes looking away |
| Humiliation | Very deep blush, eyes squeezed shut, body curled small, head down |
| Compassion | Soft gentle eyes, warm smile, both paws reaching forward |
| Empathy | Soft downturned eyebrows, gentle closed-mouth smile, head tilted, one paw extended |
| Gratitude | Closed happy eyes, paws pressed together near chest, small sparkle nearby |
| Affection | Flushed cheeks, closed content eyes, small smile, paws hugging self |
| Warmth | Soft half-closed eyes, gentle smile, faint pink glow on cheeks |
| Helplessness | Droopy wide eyes, small open frown, paws hanging limp at sides |
| Powerlessness | Eyes looking down, slumped shoulders, one paw half-raised then dropped |
| Inadequacy | Small hunched posture, eyes averted downward, tiny frown |
| Overwhelmed | Swirling/dizzy eyes, small zigzag mouth, paws pressed to head |
| Curiosity | One eyebrow raised, head tilted, wide inquisitive eyes, one paw touching chin |
| Wonder | Sparkling wide eyes looking upward, small open smile, paws clasped |
| Inspiration | Bright wide eyes with a tiny sparkle/star above head, confident smile |
| Excitement | Big open-mouth smile, sparkling eyes, both paws raised up, slight jump/bounce pose |

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
