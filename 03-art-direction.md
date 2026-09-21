# Gotchi — Art Direction

Status: v0.3 (2026-09-21). The cat and the home screen are in the build. UI rules are in `12-ui-guide.md`.

## The look in one paragraph

A **3D chibi cat** with flat colours and one thick wine-coloured ink outline stands in a **calm painted room**
(a wall, a floor, one window whose sky follows the time of day), framed by a **handheld-RPG box UI**: dark
outlined boxes, a pixel typeface for labels and numbers, a round typeface for body copy. The cat is the only
thing on screen with depth and motion of its own, so the eye goes to it. Nothing is pixelated on purpose except
the typeface; shapes are anti-aliased at any resolution.

## The cat

Rebuilt from scratch on 2026-09-21 from one painted reference, the tuxedo cat in
`references/creature-character/cat/reference-tuxedo-cat-fish-pair.png` (left cat, the fish is ignored), with the
body-part layout of the chibi turnaround sheet `references/creature-character/cat/reference-turnaround-sheet.png`.
A standing chibi cat: a wide elliptic head straight on a short rounded torso, no neck or shoulders, ears on the
top corners, short arms hanging at the sides, two feet, a tail curling out to the side.

**Palette** (sampled from the painting): fur `#5e4142` (shadow `#4d3435`), whites `#fdf1df` (shadow `#e6dbd0`),
ear pink `#fc85ad` with a lighter core `#fd9dbb`, eyes `#fbc437`, pupils `#4a1c25`, toe pads `#cfc6c0`,
whiskers `#d9cbc0`, outline deep wine `#47102a` (never black).

**Rules the model follows**

- **Outline.** One thick stroke, about 2.5% of the head width, on the silhouette, wherever parts overlap, around
  each eye and around the pink of the ear. None on whiskers, blaze or pads. The silhouette stroke is heavier
  than internal lines (0.085 against 0.055 in head-radius units).
- **Shading.** Flat colour plus one soft shadow tone on downward-facing surfaces: soft on the whites, almost
  none on the fur. No highlights.
- **Proportions.** The head is about 55% of the ear-less height and wider than the body. Big wide-based ears
  splayed about 27°, the pink face covering about three quarters of the ear and turned about 35° forward.
- **Eyes.** Huge amber ovals, slightly taller than wide (0.60 × 0.66 head-radius units), a small nub aiming about
  18° down at the nose, a thin wine rim, thin vertical pill pupils (about 65% of the eye height) sitting toward
  the nose. They are shallow domes laid onto the curved head, so nothing reads as a plate from the side. The
  slant in the painting is a head roll, a pose, and not the eye.
- **Face mark.** ONE piece: a white triangle (tip at the eyes' upper third) whose sides flow through smooth
  fillets into two plump lobes that merge at the bottom, with one continuous outline. Built flush on the head
  as a painted region, the lobes as gentle domes, a thin dark nose wedge as the only relief. Three short thick
  whisker strokes low on each cheek.
- **Markings.** White blaze and muzzle; the whole chest and belly white up to the armpits; white paw ends on
  dark arms; white feet with grey soles; three soft darker stripes across the upper back.
- **Ears.** Thin dark leaves with sharp tips. The pink is the ear's own outline inset by a thin band, with a dark
  fur band along the inner edge, a deeper pink spot low down and a small grey-mauve tip cap.
- **No invented markings.** What is not in the reference does not go on the model.

**One mesh per feature.** Unity draws the ink outline per mesh (an inverted hull), so a shape assembled from
parts shows each part's outline through its neighbours. Every feature that can overlap itself is a single mesh.
The floating heart was two lobes and a tip and came out with a dark cut across it; it is now one pillow swept
from the heart curve, with its cleft and tip slightly rounded so the hull cannot fold over itself.

**Coats.** The player's cat is always the painted cocoa cat. Other keepers' cats and wild cats are the same
model in another coat (`Creature3D/CatCoat`: ginger, smoke, night, cream, tabby, ash, rust, shadow), which
re-colours the palette keys. The model carries no textures: every colour is a plain FBX material named by
palette key.

## Animation

27 clips authored in Blender: 7 loops (Idle, Happy, Sad, Sleep, Alert, Walk, Fainted) and 20 one-shots. Rules:

- The Body bone's squash and stretch is the main tool (bones inherit scale, so squashing the body squashes the
  whole cat). The head is about 45% of the silhouette and leads every action: it anticipates down and
  overshoots up. Ears and tail are the big thin shapes and swing 20 to 50°. Arms and legs are stubs that ride
  the body and only swing wide when the pose must read (waves, celebrate, stretch, groom).
- One-shots are additive in Unity, so they never key frame 0 and always return to rest.
- Naming: every "L" part sits at +X, the cat's own left. A world-Y roll moves an up-pointing part and a
  down-hanging part in opposite directions, so EarL splays outward with a positive Y and ArmL with a negative Y.
- **The amber eyes stay open.** A happy squint only narrows them. The closed happy arcs show for under a second
  while the cat is being petted; shut lines are for sleep and fainting. The everyday Happy loop is a planted
  bounce (feet on the floor); jumps belong to the Hop and Celebrate one-shots, which answer an event.
- **In battle** our cat is seen from behind with its mouth shut (`Cat3DView.Stage`), so every battle clip has to
  read from the back. Six battle clips are required and not authored yet: defensive, attacking, screaming,
  healing, defeated, lightly wounded. Brief in `13-pvp-design.md`, section 7.
- **Faces.** The model has toggleable face meshes (happy arcs, shut lines, smile, frown, open mouth, brows,
  blush, tear, sweat, heart) and the code names 45 faces built from them (`09-pets-and-emotions.md`). Nothing
  picks a face from the cat's state any more; they are the vocabulary for reactions and cutscenes.

## Pipeline

- `Tools/blender/build_cat2.py` (Blender 5.2, Eevee flat emission, inverted-hull outline) builds the model,
  rigs it, adds the face meshes, the four shop accessories and the ground shadow, authors the clips and exports
  `Assets/Resources/Creatures/Cat3D/cat.fbx`. `Assets/Editor/CatModelImporter.cs` fixes the import settings.
- `--preview <dir>` renders turnaround views and a `compare.png` sheet next to the painted reference.
  `--anim <dir>` renders six frames of every clip (`--anim-only Happy,Idle` for some). Current strips are in
  `references/creature-character/cat/3d-take2/animation/`.
- Review every model change against **zoomed details of the reference**, from the front, the side and the back,
  before exporting. `Gotchi -lab <dir>` renders the in-game result.
- Live work with Claude through the Blender MCP add-on is described in `10-unity-setup.md`.

## The room

The default background is almost empty on purpose: a wall, a skirting board, a plain floor with faint board
lines, one window and the soft light it throws on the floor. The window tells the time (dawn, day, dusk,
night, with the sun or the moon on its arc) and the room goes light or dark with it. Four more sets are sold in
the shop (Meadow, Beach Day, Snow Day, Starry Night). All are painted in code by `ScenePainter`
(anti-aliased shapes with gradients and soft shadows). Details in `12-ui-guide.md`.

## Tooling

No 2D or 3D artist on the team. The cat is built procedurally in Blender by script, with Claude driving Blender
live; the UI and the rooms are drawn in code, so the game ships with no hand-made image assets. The earlier plan
to generate 2D art with AI image tools is on hold together with the pixel-portrait direction (see History).
Whatever tool makes a character, it never starts from another game's characters
(`05-monetization-compliance.md`).

## Open questions

- What the five evolution stages look like on the cat (size, markings, an accessory, an aura?).
- The look of battle effects: hit sparks, style-coloured flashes, heal glow.
- The art of the world to explore, and the style rules new characters must share with the cat so the roster
  reads as one game (the wine outline, flat colour, the amber-eye treatment?).
- Store art: icon direction and screenshot style for the older audience.

## History

- **Pixel-art portraits (v0.1).** The first plan was a Celeste-like pixel world with 2D creature portraits: a
  seated chibi pose, a 2 px outline, two-tone cel shading, six colours, one portrait per species and emotion
  (585 states), generated with AI tools from prompt templates (DaVinci and ChatGPT matched the style from text;
  Nano Banana only when editing from a reference image; PixelLab was planned for production sprites). Dropped:
  the cat is 3D and the UI is drawn in code.
- **The 2D animal rig (2026-09-14).** A four-legged vector animal drawn every frame (`Creature/CreatureBody`,
  `CreatureBrain`, `VectorMesh`) with a walk gait, stances and idle behaviours, for thirteen species. The code
  is still in the project, the creature lab still renders it and the battle bars reuse its `VectorMesh`; the
  first release shows only the 3D cat.
- **The first 3D cat (2026-09-14).** A different model with baked textures (`build_cat.py`). Retired.
