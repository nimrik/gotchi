# Gotchi — Characters & Faces

Status: v0.2 (2026-09-21). The file keeps its old name so links hold. It used to be "Pet species & emotion
states"; the species picker and the emotion system are gone, and what is left of both is described here.

## One character: the cat

The first release has one animal. Onboarding asks for a name (default "Mochi") and nothing else about the pet.
How it looks and moves is in `03-art-direction.md`.

**Coats.** The player's cat is always the painted cocoa cat. Every other cat is the same model in another coat
(`Creature3D/CatCoat`), which re-colours the palette keys:

| Who | Coats |
|---|---|
| The player | cocoa |
| Other keepers (rivals, leaderboards, profiles) | ginger, smoke, night, cream |
| Wild cats (the parked campaign) | tabby, ash, rust, shadow |

## Leaning: what kind of fighter it is

The chip after the level in the status block. The **style** says how the cat fights, the **most trained stat**
says what it is good at, and together they name it. It took the place of the mood chip on 2026-09-21. It is a
label for the player, and later for other players. It changes nothing in a fight.

| Style | Health | Attack | Defense | Speed |
|---|---|---|---|---|
| **Claw** | Brawler | **Fighter** | Bruiser | Striker |
| **Fluff** | Tank | Crusher | **Guardian** | Bouncer |
| **Trick** | Survivor | Ambusher | Trickster | **Shadow** |

- No style yet: **Rookie**. Every stat at rank 3 or more and within one rank of each other: **All-rounder**.
- A tie goes to the style's own stat (bold), which also names an untrained cat.
- The chip takes the style's colour (Claw coral, Fluff mint, Trick lavender, Rookie grey).
- Code: `BattleSystem.Leaning`. Pressing the XP bar shows the leaning with a one-line description.

## Faces: a vocabulary, not a system

There is **no emotion system**. Until 2026-09-21 the game picked one of 45 moods from the pet's needs and showed
it as a chip; the needs are gone and so is the picking. What stayed is the renderer's vocabulary: 45 named faces
(`Data/EmotionCatalog`, `Creature/Expressions`, `PetPortraitView.SetFace`), built from the model's face meshes.
The game uses a handful today:

| When | Face |
|---|---|
| At home, idle | Satisfaction (calm, content) |
| Poked too much | Irritation, until the poke heat cools |
| Worn out (under a tenth of its health) | the fainted pose, eyes shut |
| Petting | the closed happy arcs, for under a second |
| Story and cutscenes | any of the 45 |

The amber eyes stay open in every face except sleep, fainting and the petting arcs (`03-art-direction.md`).

**The 45 faces**, in ten families, kept as the brief for any character that gets a face rig:

| Family | Faces |
|---|---|
| Joy | Joy, gladness, relief, love, pride, satisfaction |
| Sadness | Grief, sorrow, loneliness, despair, depression |
| Anger | Rage, fury, irritation, annoyance, resentment |
| Fear | Terror, panic, anxiety, worry, nervousness |
| Disgust | Dislike, revulsion, contempt, aversion |
| Surprise | Astonishment, amazement, shock |
| Guilt & shame | Remorse, regret, embarrassment, humiliation |
| Connection | Compassion, empathy, gratitude, affection, warmth |
| Vulnerability | Helplessness, powerlessness, inadequacy, overwhelmed |
| Interest & awe | Curiosity, wonder, inspiration, excitement |

Each has a one-line description in `EmotionCatalog.GetDescription` (for example Pride: chin raised, chest
puffed out, narrowed eyes with a small smile; Shock: very wide eyes with tiny pupils, a straight gasp, a frozen
body). `Gotchi -lab <dir>` renders the whole sheet. Keep faces readable at small size: exaggerated, simple
shapes.

## Later characters

More animals arrive with the world to explore, first as wild encounters, then as keepers' pets, then perhaps as
team members. Rules for all of them:

- **Original designs only.** Generic animal types, our own names. The early roster idea was Bunny, Cat, Panda,
  Red Panda, Seal, Raccoon, Penguin, Fennec, Fox, Pig, Otter, Hedgehog, Dog (a fawn was dropped because "Bambi"
  is a trademark). Nothing from another game, in any form (`05-monetization-compliance.md`).
- **What one character costs:** a Blender build script in the manner of `build_cat2.py` (model, rig, face
  meshes, accessories), the 27 clips plus the six battle clips, a coat table, and a review from the front, the
  side and the back. Plan for it as a project of its own, not as a content drop.
- **Where they plug in:** `BattleFighterSetup.Species` and `WildAreaDef.Species`; `PetPortraitView` routes a
  species to its view.

## Open questions

- What the five evolution stages change on the cat.
- Whether leanings should do something one day (a small passive per leaning) or stay labels.
- Which faces the battle should use (a grin on a strong hit, a wince on a weak one) once the battle clips exist.
- The second character: which animal, and which style rules it shares with the cat.

## History

v0.1 planned thirteen species chosen at the start, and 45 emotion states as unique pixel-art portraits per
species (585 images). The portraits were never made: the cat became a 3D model with a face rig. The emotion
engine (ambient mood from needs, timed overrides, a mood chip with the mood's colour) was removed on 2026-09-21
together with the needs.
