# Gotchi

A cat battler for iOS, built in Unity. You take in one cat, pick its fighting style, train it, and climb the
leagues of the Battle Club in turn-based fights against other keepers' cats. Between fights you look after it
at the camp. Every character, name and line of text is our own.

**State (2026-09-21).** Playable on Mac, exports to iOS. The first release has one mode, the Battle Club.
Rivals, accounts, purchases, leaderboards, trading and news are mocks behind interfaces, ready to be swapped
for real services. The logic smoke test passes 258 checks.

## The guides

Each guide describes the game **as it is now**. When the code changes, the guide changes in the same commit.
Older directions are summarised in a short history note at the end of the guide they belonged to; the full
text is in git history.

| # | Guide | What it holds |
|---|---|---|
| 01 | [Vision](01-vision.md) | the pitch, who it is for, what sets it apart, the team |
| 02 | [Game design](02-game-design.md) | the loop (fight, rest, build), progression, sessions, what is deferred |
| 03 | [Art direction](03-art-direction.md) | the cat's look and animation rules, the Blender pipeline, the room |
| 04 | [Tech plan](04-tech-plan.md) | engine, rendering, save, services behind interfaces, testing |
| 05 | [Monetization & compliance](05-monetization-compliance.md) | coins and hearts, what money can buy, Apple and IP rules |
| 06 | [Roadmap](06-roadmap.md) | what is done, what comes next, what waits |
| 07 | [Apple compliance questionnaire](07-apple-compliance-questionnaire.md) | decision log and the checklist before submission |
| 08 | [Project checklist](08-project-checklist.md) | the working to-do list, by roadmap stage |
| 09 | [Characters & faces](09-pets-and-emotions.md) | the cat, coats, leanings, the face vocabulary, later characters |
| 10 | [Unity setup & code map](10-unity-setup.md) | opening the project, building, dev flags, where the code lives |
| 11 | [Game flows](11-game-flows.md) | every player flow with the class behind it, numbers, QA paths |
| 12 | [UI guide](12-ui-guide.md) | tokens, components, the block bars, layout and motion rules |
| 13 | [Battle design](13-pvp-design.md) | battle rules, moves, the camp, the ladder, the Market, required clips |

## The game in one page

- **The fight.** One cat against one cat, turn-based: FIGHT, ITEM, CHEER, RUN. Three styles beat each other
  (Claw beats Trick beats Fluff beats Claw). Four carried moves out of fifteen, and every move but SCRATCH
  costs mana. Our cat is seen from behind, the rival faces us. (13)
- **Health and mana stay spent.** Both bars end a fight where the fight left them and are on the home screen,
  drawn as slanted blocks of 250 points. Under a tenth of its health the cat is worn out and will not fight.
  Ten quiet minutes refill both, the app open or not. (13, 12)
- **The camp.** Four actions between fights: REST (health +40%), FOCUS (mana +40%), FEED (attack +10% for three
  battles), GROOM (defense +10% for three battles), plus a Treat (15% of both bars). Each has a 60 second
  cooldown. Nothing drains while the player is away and the cat never dies. (02, 11)
- **The build.** A free first style, four stats trained with coins up to the pet's level + 1, moves learned by
  league, one held charm, a small bag. The status block names what the build leans to: Fighter, Guardian,
  Shadow and so on. (13, 09)
- **The ladder.** Bronze, Silver, Gold, Crystal, Champion. A league, once reached, is never lost. First win of
  the day, three daily quests, win streaks and promotion rewards bring the player back. (13)
- **Money.** Coins buy power, hearts buy looks. Hearts can be bought, so hearts never buy stats, moves, items,
  charms or rating. No random rewards, no ads. (05)
- **Parked.** The Wild (a campaign of areas and bosses) is built and switched off until it becomes a world to
  explore. Other characters, live PvP and real trading come later. (06)

## Running it

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity
# logic smoke test, then a Mac build
$UNITY -batchmode -nographics -quit -projectPath "$PWD" -executeMethod Gotchi.EditorTools.GotchiEditorTools.RunLogicSmokeTest -logFile smoke.log
$UNITY -batchmode -nographics -quit -projectPath "$PWD" -executeMethod Gotchi.EditorTools.GotchiEditorTools.BuildMac -logFile build.log
open Builds/Mac/Gotchi.app
# a throwaway pet that is never saved, and a capture run of the battle screens
Builds/Mac/Gotchi.app/Contents/MacOS/Gotchi -tempsave -screenshot-battle /tmp/gotchi/shot
```

Everything else (Editor menu, iPhone build, dev flags, the Blender pipeline) is in [10](10-unity-setup.md).

## Working rules

- The game is built in code: no hand-made prefabs or scenes. Runtime code lives in `Assets/Scripts/Gotchi/`.
- Run the smoke test after any change to a system; use `-tempsave` for any run that spends coins.
- Original characters only: no names, creatures, move names, phrases or layouts from another game. (05)
- Never guilt the player. Nothing is lost by staying away.
