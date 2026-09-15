# Gotchi — Unity Setup & Code Map

Status: v0.3 — the repo **is** the Unity project (Unity 6000.6.0f1, built-in render pipeline + uGUI).
Verified end-to-end on 2026-09-12: Editor compile with zero errors/warnings, logic smoke test (140
checks), Mac standalone build that runs with a clean Player log, and an iOS export whose Xcode project
compiles through IL2CPP for arm64 (`** BUILD SUCCEEDED **`). Only Apple code-signing stands between
that and a real iPhone. URP is deliberately not set up yet — the game is pure
uGUI today; add URP when the 2D-world / 3D-creature art phase starts (`03-art-direction.md`).

## Opening the project

1. **Unity Hub → Installs**: Unity **6000.6.0f1** with **iOS Build Support** (both already installed on the
   dev Mac; Hub will offer the exact version if it's missing elsewhere).
2. **Unity Hub → Add → Add project from disk** → this repo folder → open. First open builds `Library/`
   (git-ignored) and takes a few minutes.
3. Open `Assets/Scenes/Main.unity` and press **Play**. The scene holds one `Game` object with
   `GameBootstrap`; everything else is created in code.

## Editor menu (Gotchi ▸ …)

`Assets/Editor/GotchiEditorTools.cs` adds a **Gotchi** menu so no step is manual:

| Menu item | What it does |
|---|---|
| Create Main Scene | Recreates `Assets/Scenes/Main.unity` with `GameBootstrap` and registers it in Build Settings |
| Configure Player Settings | Bundle id `com.gotchi.pet`, portrait, IL2CPP, iOS 15+, windowed 540×960 for desktop testing |
| Run Logic Smoke Test | Exercises needs / emotions / skills / shop / automation / save / quiet hours with a fake clock — fails loudly on any regression |
| Build Mac | Standalone build to `Builds/Mac/Gotchi.app` (fast way to run the game outside the Editor) |
| Build iOS (Xcode project) | Exports `Builds/iOS/` — open `Unity-iPhone.xcodeproj` in Xcode to sign and run on an iPhone |

Every item also runs headless, e.g.
`Unity -batchmode -nographics -quit -projectPath . -executeMethod Gotchi.EditorTools.GotchiEditorTools.RunLogicSmokeTest`

## Seeing the game without the Editor

The player has a QA hook: `Builds/Mac/Gotchi.app/Contents/MacOS/Gotchi -screenshot /some/dir/shot`
launches the game, captures `shot-home.png`, `shot-skills.png`, `shot-shop.png` and `shot-minigame.png`
from its own renderer, then quits. It needs no macOS screen-recording permission, so it works from
scripts and CI.

## Running on an iPhone

1. Gotchi ▸ Build iOS (Xcode project) → `Builds/iOS/Unity-iPhone.xcodeproj`.
2. Open it in Xcode, select the **Unity-iPhone** target → Signing & Capabilities → pick your team.
3. Plug in the iPhone, select it as the run destination, press Run. The bundle id is `com.gotchi.pet`
   (change it in Gotchi ▸ Configure Player Settings if you own a different one).

## Look & feel

**Rendering.** The UI renders natively at any resolution with anti-aliased procedural shapes. The character
is not a sprite at all: `Creature/CreatureBody` is a custom `MaskableGraphic` that rebuilds a feather-edged
vector mesh every frame (see `03-art-direction.md`, "Living jelly character").

The UI is fully code-drawn (`UIFactory`), no image assets: rounded cards with soft shadows, a cream/peach
palette with pastel accents, Fredoka (headings) and Varela Round (body); Pixelify Sans kept as a fallback — all SIL Open Font License,
licenses in `Assets/Resources/Fonts/`. Controls are icon-only, drawn procedurally from circles and rounded
rects (`UIFactory.CreateIcon`: cookie, bubbles, moon, ball, sparkle, bag, heart, coin).

Home layout: header = currency pills + round Skills/Shop buttons; the middle is a cozy procedural room
(`RoomView`: floor, window with sun and a drifting cloud, swaying plant, rug) with the creature front and
centre, a speech bubble showing its mood, a tappable Journey card (evolution stage, leading branch,
login streak → opens Skills) and a status bar with a "wish" chip that names the lowest need and performs
that care action when tapped; a compact icon meter strip; and a bottom dock of four round
care buttons that show a cooldown badge. The creature (`PetPortraitView` → `Creature/CreatureBody`) is a soft-body
vector mesh — squircle marshmallow silhouette on 48 radial springs, warped features, glossy eyes with
morphing lids, per-species ears/tail/markings — driven by `CreatureBrain` (mood posture, breathing, gaze,
blinks, random fidgets, hop locomotion, one-shot reactions) and deformable by touch anywhere. It will be replaced by the real sprite set from
`09-pets-and-emotions.md`, which plugs into the same view.

**The Cat is 3D (2026-09-14).** `PetPortraitView` routes `SpeciesType.Cat` to `Creature3D/Cat3DView`: a Blender
model (`Assets/Resources/Creatures/Cat3D/cat.fbx` + baked patch textures) on a private off-screen stage,
rendered by its own camera into a render texture shown through a `RawImage` in the same pet holder, so
overlays, bubbles and layout are untouched. Look = flat colours + inverted-hull outline
(`Assets/Resources/Shaders/CatToon*.shader`, built-in RP, no scene lights). Animation = legacy `Animation`
clips authored in Blender (loops Idle/Happy/Sad/Sleep/Alert/Walk/Fainted on layer 0, one-shots additive on
layer 1); the face = feature meshes toggled by name (mouths, brows, blush, tears, hearts, dirt, accessories)
plus eye/brow bones scaled/rotated in `LateUpdate`. Touch = ray-vs-bone-sphere hit test through the render
texture (tap / hold / rub-to-pet / lift-and-drop). To change the model: edit `Tools/blender/build_cat.py` and run
`/Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/build_cat.py -- --fbx
Assets/Resources/Creatures/Cat3D/cat.fbx --preview /tmp/cat-preview` (Blender 5.2; brew install --cask
blender). `Assets/Editor/CatModelImporter.cs` fixes the import settings (legacy rig, loop clips).
