# Gotchi — Tech Plan

Status: v0.2 (2026-09-21). How to open, build and navigate the project is in `10-unity-setup.md`.

## Engine and platform

- **Unity 6000.6.2f1**, built-in render pipeline, uGUI. iOS first (IL2CPP, iOS 15+, portrait). A Mac standalone
  build is the everyday way to run the game.
- **Everything is built in code.** One scene with one `GameBootstrap` object; no hand-made prefabs, no image
  assets for the UI. Screens are assembled from `UI/UIFactory` helpers. This keeps the project diffable and lets
  a two-person team change any screen from a text editor.
- Android comes after the iOS launch. Avoid iOS-only APIs where a neutral one exists; the real cost of Android
  is billing, device testing, a second store listing and a second compliance pass.

## Rendering

- The UI renders natively at any resolution: a 1080 × 1920 reference canvas, procedural sprites, anti-aliased
  vector meshes (`Creature/VectorMesh`) for custom graphics such as the block bars.
- The cat is a 3D model on a private off-screen stage, drawn by its own camera into a render texture that a
  `RawImage` shows inside the UI (`Creature3D/Cat3DView`). Flat colours plus an inverted-hull outline, no scene
  lights. Battles put two such stages on screen.
- Rooms and arenas are painted in code (`UI/ScenePainter`, `UI/RoomScenes`).

## Data and save

- One serializable `PetSaveData` (Unity `JsonUtility`, so lists and not dictionaries), written to
  `Application.persistentDataPath/gotchi_save.json` by `Persistence/LocalJsonSaveService`. Autosave every 30 s
  and on pause or quit.
- **Time is computed, never ticked.** Health and mana store a value and a timestamp and are brought up to date
  against the clock whenever they are read (`BattleSystem.UpdateVitals`), so they are right after hours away
  with no background work. Daily blocks (quests, trades, finds) are keyed by the local calendar day. All systems
  take a `GameClock`, which tests and dev flags replace.
- Saves are forward-tolerant: fields that no longer exist (the old needs list) are ignored on load, and a save
  from before a feature starts that feature empty.
- `-tempsave` swaps in a `NullSaveService` for throwaway runs.

## Services behind interfaces

Every outside dependency has an interface and a mock, so the game is fully playable offline today.

| Need | Interface | Today | Later |
|---|---|---|---|
| Accounts | `Persistence/IAuthService` | `MockAuthService` | Supabase Auth; the token is in the save, the password never |
| Cloud save | `ISaveService` | local JSON (`SupabaseSaveService` is a stub) | Supabase storage or CloudKit |
| Purchases | `Economy/IPurchaseService` | instant mock | StoreKit through Unity IAP, with receipt validation |
| Leaderboards | `ILeaderboardService` | 24 stable fake keepers | Supabase table or Game Center |
| Rivals | `BattleSystem.RivalSource` | keepers from the mock board | match-making |
| Trades | `BattleSystem.TodayTrades / TryTrade` | a daily mock board | player offers, moderated |
| News | `INewsService` | `MockNewsService` | Supabase `news` table |
| Notifications | `INotificationChannel` | logs | `com.unity.mobile.notifications` |

Live PvP will need a server that owns the dice: the same `BattleSystem` rules with server-side rolls,
match-making and anti-cheat. None of that is designed yet.

## Third-party SDKs

The audience positioning is 13+, but pet games draw younger players, so the defaults stay strict: no
advertising SDK, no behavioural analytics, nothing that sends a device identifier to a third party alongside
the age band. Every SDK is audited against `07-apple-compliance-questionnaire.md` before it goes in. Purchases
go through Apple's own system only (Guideline 3.1.1).

## Feature switches

`Core/GameFeatures` holds the switches for parked features. `GameFeatures.Wild` is off: the campaign
code stays compiled and smoke-tested but nothing on screen leads to it.

## Testing

- **Logic smoke test** (`GotchiEditorTools.RunLogicSmokeTest`, 258 checks): every rule system driven with a fake
  clock, headless. Run it after any change to a system.
- **Capture runs**: the player takes its own screenshots of scripted flows (`-screenshot-battle` and friends),
  with a battle that plays itself, so a UI change can be reviewed without touching the mouse.
- **Creature lab** (`-lab <dir>`): renders the character, faces and animation strips to PNGs.
- After a capture run, check `~/Library/Logs/Gotchi/Gotchi/Player.log` for exceptions.

## Open questions

- Backend: is Supabase enough for accounts, cloud save, boards and asynchronous battles, and what does live
  PvP need on top?
- Save conflicts between devices once cloud save exists.
- Whether to move to URP when the world to explore brings real 3D scenes.
