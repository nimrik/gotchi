# Gotchi — Art Direction

Status: draft v0.1 — directional notes only; needs a dedicated reference-gathering pass before locking.

## Reference point

Celeste-inspired pixel art for the world/UI, combined with a **3D creature** as the pet itself — a
2D-environment + 3D-character hybrid (comparable in spirit to Paper Mario or Octopath Traveler's
"2D-HD" look, though the target mood is cute/kawaii rather than those games' tone).

## Key technical note (important for crispness)

Celeste's pixel art reads as crisp because it's rendered at a fixed low internal resolution and then
scaled up with nearest-neighbor filtering — not because the source art uses giant literal pixels.
On modern Retina iPhone screens, replicating "1 pixel ≈ 1mm" literally would look chunky/jagged rather
than crisp. Recommendation: lock an internal render resolution (e.g. 320×180 or similar) for the 2D
world/UI layer, and treat the "big pixel" look as a deliberate scale choice validated by prototyping,
not a literal 1:1 pixel-to-millimeter target.

## Creature rendering

- Base creature: 3D model, likely low-poly or stylized with pixelated/dithered shading to sit visually
  with the 2D world.
- Some mini-games render the character as 2D (matches world), others as full 3D (multiplayer arena style,
  Brawl Stars-comparison) — needs a clear rule for *when* each mode is used, so it doesn't feel arbitrary.

## Creature portrait style guide (draft v0.1)

Extracted from the reference images in `references/creature-character/` — specifically the consistent
cluster (plain sitting panda, red panda, both bunnies), not the two outliers in that folder (a ninja panda
with a sword, and a photo-realistic "scared panda in a cage" meme) which are a different rendering style
entirely and are excluded from this pattern unless confirmed otherwise. This style is aimed at the
45-sub-emotion × 13-species portrait art from `09-pets-and-emotions.md`, not necessarily the explorable
world (still open, see below).

| Element | Rule |
|---|---|
| Canvas | Square, clean pixel grid (~128×128 effective resolution), no blur/smoothing |
| Proportions | Chibi/super-deformed — oversized rounded head, small simplified seated body, minimal limb detail |
| Pose | Seated, three-quarter idle "loaf" pose, facing slightly left — universal base pose every species/emotion variant builds from |
| Outline | Uniform ~2px near-black outline around the whole silhouette |
| Shading | Flat cel-shading — exactly 2 tone values per surface (base + one shadow), no gradients, no dithering, one small highlight fleck on eyes/nose only |
| Eyes | Large, round, dark pupils with a white sparkle catchlight in the same corner every time |
| Cheeks | Soft pink blush marks on both cheeks — same pink across every species regardless of fur color |
| Palette | Max 6 flat colors per character (fur base, fur shadow, outline, eye, blush pink, one species accent) |
| Background | Flat/transparent, no props, no decorative sparkles or text baked into the art |

The blush-pink and sparkle-catchlight rules are deliberately kept constant across every species — that's
the intended "Gotchi signature," so the roster reads as one brand rather than a reused generic asset pack
(some reference images carry visible third-party watermarks — "Shoebox Games," "SCicek" — treat those as
inspiration only, never reused/traced directly).

### Nano Banana (Gemini 2.5 Flash Image) prompt templates

**1. Establish the style — run once, no reference image, on the first species:**
```
A single {SPECIES} rendered as chunky kawaii pixel art, chibi proportions with an
oversized rounded head and a small simplified seated body, drawn on a clean 128x128
pixel grid with soft anti-aliased edges (no dithering, no gradients). Flat cel-shading
with exactly two tone values per color area plus a single small highlight fleck on the
eyes and nose. Uniform 2px near-black outline around the entire silhouette. Large round
dark eyes with one white sparkle catchlight in the upper-left of each pupil. Soft pink
blush marks on both cheeks. Seated three-quarter idle pose, facing slightly left,
centered in frame. Maximum 6 flat colors total. Solid transparent background, no props,
no decorative elements, no text, no watermark. Clean, polished, mobile-game icon
quality — not a rough sketch.
```

**2. New species, same style — attach the locked reference image from step 1:**
```
Using the exact same art style, proportions, outline weight, shading rules, eye design,
and color palette structure as the attached reference image, create a new character: a
{SPECIES} in the same seated three-quarter idle pose. Keep species-accurate features
(ears, markings, fur pattern) but do not copy the reference creature's specific design —
only match its rendering style. Solid transparent background, no props, no text, no
watermark.
```

**3. Emotion variant of an existing species — attach that species' locked base image:**
```
Using the exact same art style, proportions, outline, and palette as the attached
reference image of this {SPECIES}, create a new pose/expression showing {EMOTION}:
{one-line description of how it reads on this face/body}. Keep the same seated base
pose, same colors, same proportions — change only the facial expression and minor body
language needed to convey the emotion. Solid transparent background, no props, no text,
no watermark.
```

Nano Banana is generative, not a strict pixel-grid tool, so outputs may drift slightly off-grid — treat
its output as the design-lock step, then run final picks through PixelLab.ai (see Tooling below) to snap
them to a true palette-locked pixel grid before they become production assets.

### First validation result (cat, confirmed)

Tested prompt template #1 (text-only, no reference image) on two tools:
- **AI Studio (Nano Banana / Gemini 2.5 Flash Image), text-only:** off-pattern — rendered smooth/soft
  vector-style rather than true pixel art, no visible pixel grid, no sparkle catchlight, soft blurry
  outline instead of a crisp uniform stroke. Nano Banana's weak point is from-scratch text-only generation.
- **DaVinci (AI art app), text-only:** strong match to the style guide on the first try — chunky visible
  pixel grid, consistent thick outline, flat 2-tone cel shading, sparkle catchlight, blush, tight palette,
  transparent background. Locked as the first reference image:
  `references/creature-character/ai-creatures/davinci_a_single_cat_rendered_as_chunky_kawaii_pixel_art__.png`
- **Takeaway:** the prompt itself works well; DaVinci handles from-scratch text-only generation better,
  while Nano Banana's actual strength (per its own model behavior) is consistent *editing from a reference
  image* rather than from-scratch generation — so feed it the DaVinci result + prompt template #2/#3
  instead of asking it to generate from text alone.

### Second validation result (seal, confirmed)

ChatGPT's image generation also produced a strong match on the seal prompt (template #2) — chunky pixel
grid, consistent outline, 2-tone cel shading, sparkle catchlights, blush, transparent background:
`references/creature-character/ai-creatures/chatgpt seal.png`. Now three tools confirmed capable of
hitting this style: DaVinci, ChatGPT (both from-scratch/text-guided), and Nano Banana (reference-editing
mode only, not yet tested — text-only failed per the cat test above).

**Prompt refinement learned from this run:** ChatGPT returned two seals side-by-side (a comparison pair)
instead of one isolated creature. Add "single creature only, no comparison pairs, no duplicate poses in
frame" to future prompts so each generation is one clean, individually-usable asset.

**Still open:** whether the explorable 2D world shares this exact style or a different (but complementary)
treatment; per-species accent colors; whether the ninja-panda/scared-panda outliers represent a wanted
alternate style (e.g. a battle-pose variant) worth folding in later.

## The animal rig (decided 2026-09-14, third pass — replaces the blob)

The user rejected every blob-shaped version ("scary, not cute") and, via their reference folder plus a
"Collect cute pets" screenshot, pinned the look: **a real four-legged animal** with a big round head, small
face on the front half of the head, dot eyes with one catchlight, pink cheeks, flat colour with **one darker
shadow tone**, a **thin dark outline**, standing on a soft shadow disc. Smooth vector, not pixel (a pixel pass
can be added later as a render setting). And "full-fledged animation, not crude".

**Rig** (`Creature/CreatureBody`, one uGUI `MaskableGraphic` per creature, rebuilt every frame from
`Creature/VectorMesh`): spine (hip + shoulder points), head with pitch and yaw, four two-bone legs solved by
analytic IK (front knees bend back, hind knees forward; far legs drawn behind and in the shadow tone), tail
chain of three joints, ears, all as `Spring`s. Body plans: `Quadruped` (11 species), `Upright` (penguin),
`Flat` (seal). Species = data in `Creature/CreatureLook` (palette, ears, tail, nose, marks).

**Animation** (`Creature/CreatureBrain`): a lateral-sequence walk gait drives the feet (stance/swing, stride
scales with speed, body bob, head bob, tail sway); stances Stand / Sit / Lie / Sleep blend through the joint
springs and cycle on a calm schedule when the pet is left alone (stand 6–14 s → sit 14–30 s → lie …); idle
behaviours every 7–16 s (look around, ear twitch, tail flick, groom with a raised paw, the full cat stretch,
sniff, shake, yawn); breathing, blinks, gaze saccades; nothing hops on its own. Game reactions are `OneShot`s
(hop, wiggle, pat, wave, tail flick, pounce attack, hurt, faint on its side, eat, celebrate, dance, nod, shiver,
stretch, yawn, shake, ear twitch, look around, sniff, groom). Physics from the previous pass stays: gravity,
friction, walls, pick-up (hangs from the grab point), throw, landing crouch; in the air the legs dangle.

**Layout (units, ground = 0, +x = facing):** hip (−12, 19), shoulder (9, 20), head centre = shoulder + (6, 19),
head 18.5 × 17, face centre = head + (2, −1): eyes at ±6.5 (3.4 × 3.8), blush at ±11.5, nose at (0.5, −3.2),
mouth 5.5 wide just below; legs attach at hip/shoulder, segments 10 + 10, rest feet at x = −15 / 7 (far),
−11 / 12 (near); tail root = hip + (−7, 2), joints 9 / 8 / 7 long, rest curl 125° → 85° → 50°.

**Review tooling:** `Gotchi -lab <dir>` writes `lab-species.png`, `lab-emotions.png` and strips/stills for
idle, walk, sit, groom, lie, sleep, stretch, poke, hold, dangle, drop, attack, joy, faint, beanie, scarf.

Style boards used to get here (Artifacts): "Gotchi Style Board" (12 rendering styles, all rejected — the
silhouette was the problem, not the shading) and "Mochi Anatomy Board" (three sitting poses; "better").

## The 3D cat (2026-09-14, from the user's tuxedo-cat reference)

The user supplied a flat-colour illustration (chibi tuxedo cat hugging a fish plush, left cat) and asked for a
full 3D version in the same cartoon style. Built procedurally in Blender (`Tools/blender/build_cat.py`):
head 1.20 × 0.98 × 0.90 over a 0.70 body, big pointed ears with pink inners, slanted almond eyes (yellow,
vertical dark pupils, one small glint), a small white muzzle joining a white chest bib, one white glove arm
and a white-tipped other paw, white socks, short curled tail; fur is warm dark brown (#3C2A26), outline
near-black, three cream whiskers per side. Patch borders are baked to textures (`cat_head.png`,
`cat_body.png`) so they stay smooth. 45 emotions map onto: happy arcs / shut lines / open eyes with a
scalable eye bone, smile / frown / open mouth, tiltable brows, blush, tear, sweat, heart, plus dirt, drool
and four accessories. 27 clips: 7 loops + 20 one-shots (see `Creature3D/Cat3DView`). Review with
`Gotchi -lab <dir>`; the other 12 species remain the 2D animal rig below until they get models.

## Tooling

- No in-house 2D/3D art skill on the team currently — AI generation tools (e.g. PixelLab.ai and others
  TBD) planned for asset generation.
- Recommended split: lean on AI tools for high-volume, lower-stakes 2D assets (icons, backgrounds, item
  variations, UI elements). Treat the 3D creature model + rig/animations as a higher-stakes asset worth
  extra care (possibly a freelance 3D artist) since it's the thing players look at constantly.
- **Recommended two-tool pipeline for locking and reusing "our" style:**
  1. **Concepting/style-lock phase:** use Gemini 2.5 Flash Image ("Nano Banana") to explore the
     look and lock a written style guide (palette, proportions, shading/outline rules) — it's strong at
     editing/re-posing the *same* character consistently from a reference image, which is exactly what's
     needed to test "does our style hold up across many creatures/poses" before committing to it.
  2. **Production phase:** once the style is locked, produce actual game-ready sprites in PixelLab.ai,
     since it's purpose-built for consistent pixel-art sprite sheets/variations (palette-locked, animation
     frames) rather than one-off illustrations. Feed it the reference images + written rules from step 1.
  - This matters a lot for `09-pets-and-emotions.md`'s emotion-state art (585+ planned states across 13
    creatures) — the whole point of locking a style guide first is to make each new state fast/cheap to
    produce consistently, rather than re-deriving the look every time.

## Open questions (needs its own research pass)

- Concrete mood board / reference set for the explorable *world* specifically (the creature portrait style
  is now drafted above, but the world/UI layer's visual direction is still unresolved).
- Internal render resolution: **implemented as a fixed internal height of 540 px** (`PixelRenderer` in code)
  with nearest-neighbour upscaling — a bit under a third of the 1080×1920 UI canvas. Still worth validating
  on a real iPhone; it's a one-constant change.
- Visual identity of the creature across evolution branches — how distinct should each branch look?
- UI style specifically — "premium, non-trashy" needs to be turned into concrete rules (palette limits,
  iconography style, typography) rather than staying a vibe.
