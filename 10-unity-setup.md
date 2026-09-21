# Gotchi — Unity Setup & Code Map

Status: v0.4 (2026-09-21). The repo **is** the Unity project: Unity **6000.6.2f1**, built-in render pipeline,
uGUI. Verified: the Editor compiles with no errors or warnings, the logic smoke test passes (258 checks), the
Mac build runs with a clean player log, and the iOS export compiles through IL2CPP for arm64. Only Apple code
signing stands between that and a real iPhone.

## Opening the project

1. Unity Hub → Installs: Unity 6000.6.2f1 with iOS Build Support.
2. Unity Hub → Add → Add project from disk → this folder. The first open builds `Library/` (git-ignored) and
   takes a few minutes.
3. Open `Assets/Scenes/Main.unity` and press Play. The scene holds one `Game` object with `GameBootstrap`;
   everything else is created in code.

## The Gotchi menu, and the same from a shell

`Assets/Editor/GotchiEditorTools.cs` adds a **Gotchi** menu. Every item also runs headless.

| Menu item | What it does |
|---|---|
| Create Main Scene | recreates `Assets/Scenes/Main.unity` and registers it in Build Settings |
| Configure Player Settings | bundle id `com.gotchi.pet`, portrait, IL2CPP, iOS 15+, a 594 × 1056 window on desktop, Run In Background |
| Run Logic Smoke Test | drives every rule system with a fake clock: battle rules, the camp, leanings, vitals, the block-bar layout, the Market, the parked Wild, shop, helpers, saves, quiet hours. Fails loudly on a regression |
| Build Mac | `Builds/Mac/Gotchi.app`, the everyday way to run the game |
| Build iOS (Xcode project) | `Builds/iOS/`; open `Unity-iPhone.xcodeproj`, pick a team under Signing, run on the phone |

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -nographics -quit -projectPath "$PWD" \
  -executeMethod Gotchi.EditorTools.GotchiEditorTools.RunLogicSmokeTest -logFile smoke.log
grep -E "error CS|FAILED|ALL CHECKS PASSED" smoke.log
```

Quit the running game before a build (`pkill -f "Builds/Mac/Gotchi.app/Contents/MacOS/Gotchi"`), and if a
batch run was killed, delete `Temp/UnityLockfile`.

## Player flags (dev and QA)

`Builds/Mac/Gotchi.app/Contents/MacOS/Gotchi <flags>`. The player takes its own screenshots, so none of this
needs screen-recording permission and all of it works from scripts.

| Flag | What it does |
|---|---|
| `-tempsave` | a brand-new pet that is never written to disk. **Use it for every run that spends coins or picks a style** |
| `-hour 21.5` | pins the local hour, so the room's window can be reviewed at any time of day |
| `-fresh` | starts at onboarding even when a save exists. Finishing the onboarding writes the new pet over the old save, so pair it with `-tempsave` unless that is wanted |
| `-screenshot <base>` | home, shop pages, the five backgrounds, settings, leaderboard, news, story; with `-fresh`, onboarding |
| `-screenshot-home <base>` | home-screen work: two home frames, the info boxes, the wallet, the room at four hours |
| `-screenshot-battle <base>` | poking the cat until it leaves, every Battle Club and Market page, a ranked battle that plays itself through to the results, home with the health and mana it left, the Treat, REST and FEED, the rating board |
| `-lab <dir>` | the creature lab: the character, the face sheet and animation strips as PNGs |

Player log: `~/Library/Logs/Gotchi/Gotchi/Player.log`. Grep it for `Exception` after a capture run.

## Code map (`Assets/Scripts/Gotchi/`)

| Folder | What lives there |
|---|---|
| `Core/` | `GameBootstrap` (boot, wiring, autosave, dev flags, capture routines), `GameContext` (the systems a screen can reach), `GameClock`, `GameFeatures` (switches for parked features), `GameSettings`, `SimpleTween`, `CreatureLab` |
| `Data/` | `PetSaveData` with `BattleSave`, `CampSave`, `CampaignSave`; `Enums`; `EmotionCatalog` (the 45 named faces); `NewsItem` |
| `Systems/` | **`BattleSystem`** (rules, catalogue, vitals, leaning, ladder, quests, trades), **`CampSystem`** (REST, FOCUS, FEED, GROOM, the Treat), `CampaignSystem` (the parked Wild), `AutomationSystem` (helpers), `LevelSystem` + `StoryBook`, `SkillTreeSystem` (battle XP and evolution stage), `BoostSystem`, `Leaderboards`, `NewsService`, `NotificationScheduler` |
| `Economy/` | `CurrencyWallet`, `ShopCatalog`, `ShopService`, `IPurchaseService` with the mock and the StoreKit seam |
| `Persistence/` | `ISaveService` (local JSON, Supabase stub), `IAuthService` (mock) |
| `MiniGames/` | `BattleMiniGame` (the fight on screen), `MiniGameStage` (staging and movement), `IMiniGame` (context, result, registry). The battle is the only game |
| `Creature3D/` | `Cat3DView` (the 3D cat: stage, clips, face, touch), `CatCoat` |
| `Creature/` | `VectorMesh` (anti-aliased vector drawing, used by the block bars), `Expressions`; the rest is the 2D animal rig, which the first release does not show |
| `UI/` | `UIFactory` (tokens and every component helper), `HUDController` (the home screen), `RoomView` (room and status block), `PetPortraitView`, `SegmentedBar`, `CampCellView`, `InfoTooltip`, `DialogBoxView`, `PagedPanel` + `PanelRows`, `TabBarView`, `PagedScroll`, the panels (`BattleClubPanelView`, `MarketPanelView`, `CampaignPanelView`, `ShopPanelView`, `LeaderboardPanelView`, `SettingsPanelView`, `StoryPanelView`, `NewsPanelView`), `MiniGameOverlayView`, `OnboardingView`, `RoomScenes` + `ScenePainter` |

Also: `Assets/Editor/` (the menu above and `CatModelImporter`), `Assets/Resources/` (the cat FBX, toon shaders,
fonts with their OFL licences), `Tools/blender/` (the model scripts), `references/` (the painted references and
review renders).

How a screen is put together: `GameBootstrap` builds the systems into a `GameContext`, `HUDController` builds the
home screen from `UIFactory` helpers and subscribes to the systems' events, and panels open over it one at a
time. Rules never live in a view. Flows are in `11-game-flows.md`, UI rules in `12-ui-guide.md`.

## The cat model

- Source of truth: `Tools/blender/build_cat2.py` (Blender 5.2; `brew install --cask blender`). It exports
  `Assets/Resources/Creatures/Cat3D/cat.fbx`; `CatModelImporter` sets the import (legacy rig, looping clips).

```sh
B=/Applications/Blender.app/Contents/MacOS/Blender
$B -b -P Tools/blender/build_cat2.py -- --fbx "$PWD/Assets/Resources/Creatures/Cat3D/cat.fbx"
$B -b -P Tools/blender/build_cat2.py -- --preview /tmp/gotchi-cat2           # turnaround + compare.png
$B -b -P Tools/blender/build_cat2.py -- --anim /tmp/gotchi-anim --anim-only Happy,Idle
```

- In Unity: the model sits on a private off-screen stage, rendered by its own camera into a render texture
  shown through a `RawImage`. Look: flat colours plus an inverted-hull outline
  (`Assets/Resources/Shaders/CatToon*.shader`, no scene lights). Loops play on layer 0 and one-shots additively
  on layer 1 (legacy `Animation`). The face is feature meshes toggled by name plus eye and brow bones. Colours
  come from FBX materials named by palette key, which is how coats work. Touch is a ray against bone spheres
  through the render texture.
- **Live work with Claude.** Blender has the "MCP for Blender" add-on (a socket server on port 9876 that starts
  with Blender) and Claude Code has a matching `blender` MCP server registered for this repo. `Tools/blender/live.sh`
  rebuilds the cat into `/tmp/gotchi-cat2/cat2.blend` and opens it; pass a `.blend` path to open another file.
  A Blender started before the add-on was installed has no server, so restart it. The script stays the source of
  truth: what is tried live is ported back into `build_cat2.py`.
- `Tools/blender/build_cat.py` is the retired first cat.

## Gotchas

- A bare Unity project lacks uGUI; the package is in the manifest, leave it there.
- A layout group with force-expand overrides its children's preferred sizes.
- Desktop players pause when unfocused unless Run In Background is on (capture runs depend on it).
- Unity draws the ink outline per mesh, so any feature that can overlap itself must be one mesh.
- `Destroy` waits for the end of the frame: deactivate a row before destroying it when a list is rebuilt, or the
  layout still counts it (`PagedPanel.Rebuild` does this).
