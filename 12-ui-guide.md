# Gotchi — UI Guide

Status: v0.2 (2026-09-21). The rules every screen follows. The same tokens live in code (`UI/UIFactory`), so
build screens from those helpers and this guide stays true. When you add a pattern, add it here in the same
commit.

## Principles

- **The cat is the picture, the UI is the frame.** Nothing competes with it: the room is almost empty, boxes are
  flat, and only the cat has depth and motion of its own.
- **One box language.** Every container is the same handheld-RPG box: a dark outline, a light inner line, a flat
  fill, a cut corner pixel. No drop shadows, no rounded cards.
- **Numbers are shown.** Health, mana, XP, prices and costs are read as numbers, never hidden behind a vague bar.
- **One block, one idea.** Status, battle menu, camp. A block never mixes two jobs.
- **Everything tappable moves.** Every control has the press animation and shows ▶ while it is held.
- **Never guilt.** No wording about suffering, no red alarms for staying away.

## Tokens (`UIFactory`)

| Token | Value | Use |
|---|---|---|
| `Cream` / `Peach` | #FFF6EC / #FFE1D0 | page backdrop and its top glow |
| `Card` | white | boxes on the home screen, rows inside panels |
| `PanelBlue` | #BCBFF5 | the fill of every panel |
| `FrameDark` / `FrameLight` | #5E5E7E / #D8DCEC | the box outline and its inner line |
| `MenuInk` | #4A4A56 | text on boxes and on blue |
| `Ink` | #4A3F55 | body text, toasts, tooltips |
| `Muted` | #9A8FA6 | secondary text on white only. Never on blue, and too faint under pixel 20 |
| `MenuGrey` | #D4D5DF | options that are not chosen; the ROOKIE and NORMAL chips |
| `Primary` | #F27FA5 | the one call-to-action colour, white label |
| `Pink` / `PinkDark` | #FF9EBB / #E86F96 | accents, emphasis text |
| `Coral` `Mint` `Lavender` | pastels | **the three styles: CLAW, FLUFF, TRICK**, everywhere a style shows |
| `Butter` `Sky` | pastels | rewards, held or in-use rows, the Treat chip |
| `PanelRows.Good` | #2E9E62 | "this is working for you": READY, a running buff |
| HP | fill #FF5C7A, line #B8324E; low (a fifth or less) fill #E02D4F, line #8F1C33 | health bars and the HP label |
| MP | fill #8E7BFF, line #5B49C9 | mana bars and the MP label |
| `Scrim` | rgba(74,64,84,0.45) | behind panels |

**Spacing** (`UIFactory.Spacing`): `Section` 24 between blocks, `List` 16 between rows, `Pad` 24 inside boxes,
`Gutter` 24 page margins. The canvas reference is 1080 × 1920 and the safe area is applied at the root.

**Type** (all under the Open Font License, in `Resources/Fonts`): the pixel face is **Jersey 20**, drawn at
×1.1, for headings, labels, buttons and every number, chosen because its digits stay distinct. Body copy is
**Varela Round**. Page title 44 to 52, box title 26 to 32, body 21 to 26, captions 18 to 22 (16 only for the
HP and MP tags inside a battle box). **Anything the player reads to decide is pixel 20 or more**: the camp
captions were 17 and could not be read. Button labels never wrap.

## Components

| Component | Helper | Notes |
|---|---|---|
| Box | `CreateFrame`, `CreateCard`, `CreatePill` | one tinted 9-sliced sprite; the bevel tints with the fill |
| Button | `CreateButton` | framed, upper-case pixel label, ▶ while held. Fill says the type: `Primary` pink, white, or a pastel. `FitToLabel` shrinks one-word buttons (Back) to their word |
| Pressable cell | `MakePressable` + `PressFeedback` | for cells that are buttons without a frame of their own: battle doors, camp cells |
| **Block bar** | `SegmentedBar.Create(name, parent, fill, line)` then `Set(value, max)` | health and mana, everywhere. See below |
| Thin bar | `CreatePillBar` | XP and league progress: a 3 px outlined track with the fill inset |
| Chip | `CreatePill` + pixel text | leaning, style, the Treat; sized to its text |
| Wallet box | `CreateWalletBox` + `FitWalletBox` | both currencies on one line, as wide as its content, refitted when amounts change |
| Info box | `InfoTooltip.Toggle(anchor, title, body, host)` | the full numbers behind a bar. Ink box above the bar, 6 s or a tap anywhere, one shared instance, never catches taps. The press area is ±30 px around the thin bar |
| Text box | `DialogBoxView` | typewriter at 45 chars/s, tap to continue, bouncing ▼. `Ask(text, options, onPick)` pops YES / NO. Story, the doorstep, every purchase confirmation, log-out, reset |
| Toast | ink box, white text, bottom | pops in, 2.2 s, fades |
| Tabs | `TabBarView.Arrows` + `PagedScroll` | ◀ TITLE ▶ with a 1/4 counter; pages also swipe. Used by every panel |
| Panel | `PagedPanel` + `PanelRows` | the shell of the battle panels: title, wallet, pager, scrolling lists, Back |
| Icons | `CreateIcon(IconKind.X)` | drawn in code from simple shapes: coin, heart, moon, sparkle, cookie, bubbles, shield, paw, bag, fish, book ... |
| Input | `CreateInputField` | 84 tall, muted placeholder |

### The block bar (`SegmentedBar`)

Health and mana are a row of **slanted blocks, one block per 250 points**.

- The bar's **total width is fixed**. A bigger maximum means more and narrower blocks, never a longer bar.
  `unit width = (width - skew - 2 px × (blocks - 1)) ÷ (max ÷ 250)`.
- The last block holds what is left and is narrower in proportion: **1200 is four full blocks and a fifth that is
  4/5 as wide.**
- A block the value reaches is filled. A block it does not reach is **only outlined**, white inside. The block
  the value ends in is filled part of the way. 1000 of 1200 is four filled blocks and one outlined.
- **2 px gaps**, white: a white strip runs under the whole row, so the separators are white on any background.
- The blocks lean right (skew 10 to 12 px over the bar's height), outline 2.5 px in the bar's dark line colour.
- **The numbers follow the bar**: `HP ▰▰▰▰▱ 1000/1200`, pixel 22, read from the left.
- Health switches to its low colours at a fifth or less.
- Drawn as an anti-aliased vector mesh, so the slanted edges stay smooth at any size. The layout rule is in
  three static helpers (`BlockCount`, `BlockShare`, `BlockFill`) that the smoke test checks.

## Layout of the home screen

Header (84) → room (flexible, with the status block, 136, docked at its bottom) → battle menu (138) → camp block
(176). Blocks are full column width with 12 px between them, so the three bottom boxes read as one stack on the
floor. The background runs the full screen behind everything. Panels open over the safe area on a scrim, one at
a time.

- **Status block** (`RoomView`, two rows). Row 1, centred 40 px down: "Mochi · Lv 4", the leaning chip, the XP
  bar over the free width, its caption in experience points, the Treat chip; laid out from preferred widths
  (`LayoutNameLine`), and with a long name the caption gives way first. Row 2, centred 34 px up: HP on the left
  half, MP on the right, each a pixel-22 label in the bar's line colour, a 26 px block bar, then the numbers in
  a 122 px slot.
- **Leaning chip.** The build's name in capitals (pixel 22) on the style's colour; grey ROOKIE before a style is
  picked. It punches its scale when the leaning changes. Pressing the XP bar explains it.
- **Treat chip.** 60 tall, a white disc with the fish icon, butter fill; paler with "Treat · 42s" while it
  cools down.
- **Battle menu.** A league line on top (pixel 26 name, a butter thin bar, "25 / 200"; press the bar for the info
  box) and ONE row of doors under it. A door is a pressable cell: 44 px icon, pixel 30 name. The row divides
  itself by the number of doors, so a fifth can come back.
- **Camp block.** Four pressable cells, 2 × 2 (`CampCellView`): 48 px icon, pixel 30 name, under it what the
  action does (pixel 20: "HP +40%", "ATK +10% · 3 BATTLES", green while the buff runs), and on the right where
  it stands (pixel 26): READY in green, "45S", FULL, or "2 LEFT" in green. While a press would do nothing the
  icon is at 40% and the name is greyed.

## Panels

- **One shape** (Shop, Settings, Leaderboard, Battle Club, Market): a `PanelBlue` box, a big pixel title, the
  wallet where money matters, the ◀ PAGE ▶ pager when there is more than one page, swipeable pages, Back at the
  bottom.
- **Rows** (`PanelRows`): a flat white framed box; pixel title top-left, body lines under it, a style chip where
  a style applies, ONE action button on the right, fitted to its word and centred. **The price lives in the
  button** ("TRAIN · 110", "LEARN · 300", "BUY · 30"). Texts stop 250 px from the right edge so the widest
  button never covers them. The fill says the state: butter = carried, held or in use; white = owned or
  buyable; grey = locked, with the league that unlocks it as the disabled button's label.
- Full-width actions inside a list (FIGHT!, TO THE MARKET) are `PanelRows.Primary`, centred, 96 tall.
- A panel rebuilds from the save on every change and keeps each list's scroll position (`PagedPanel.Rebuild`).
- **Shop cards**: a pastel art tile, pixel name, a short description, then a price chip and the action button.
- **Leaderboards**: your row first (pink, true rank), a TOP 25 divider, then the rest.
- **Choices** the player picks between are `MenuGrey` boxes; the chosen one turns white.

## The battle screen

Our cat bottom-left and large (340), seen from behind; the rival top-right (190), facing us; each on an ellipse
platform in the arena's colours. Status boxes: name, style chip, level, the HP block bar; ours adds the MP bar
and the numbers. Bottom band: the text box on the left, FIGHT / ITEM / CHEER / RUN on the right. FIGHT and ITEM
swap the text box for a 2 × 2 grid and the actions for an info box (what the highlighted entry does, and Back).
The first tap highlights, the second confirms. The move info leads with the cost ("MP 120 of 1100", or FREE).

## Backgrounds (`RoomScenes`, `ScenePainter`)

One set at a time behind the whole screen; the floor line sits at 0.444 of the screen height, under the rug. The
default Cozy Room is almost empty on purpose: a wall, a skirting board, a plain floor with faint board lines,
one window and the light it throws. **The window tells the time**: four sky palettes blended through the day
(night, dawn from 6:30, day 8:30 to 16:30, dusk from 18:30, night from 20:30), the sun on an arc from 5:30 to
20:30, a crescent moon from 19:00 to 7:00, stars as it darkens, one cloud by day; wall, floor and frame go
light or dark with it. Repainted when the ten-minute bucket turns. `-hour 21.5` pins the hour for review. The
other sets (Meadow, Beach Day, Snow Day, Starry Night) are lighter scenes with fixed tints. Shapes are
anti-aliased with gradients and soft shadows, never pixel-blocky. Rugs and fairy lights sit on top of any set.

## Motion (`SimpleTween`, `PressFeedback`)

- Press: scale to 0.94 in 70 ms, release overshoots to 1.03 and settles. Disabled controls do not animate.
- Appear: `PopIn` 0.2 to 0.3 s. Reward moments: `PunchScale`, hearts, confetti, floating text over the cat.
- Bars: animate a jump, set small changes directly. Count-ups for wallet amounts (0.5 s).
- Tweens are restart-safe: `SimpleTween` remembers each transform's rest pose and cancels the previous tween,
  so repeated taps never compound. Never stack your own scale maths.
- The cat: use `PetPortraitView` everywhere (`SetFace`, `Play(OneShot.X)`, `SetLoop`, `React(PetPart)`,
  `SetWornOut`, `SetAccessory`, `StageForBattle`). In a fight it moves through `MiniGameStage`.

## Writing

Short, warm, second person. Upper-case pixel labels for controls, sentence case for body copy. Say what the
player gets ("500 Coins", "HP +40%"), never a nickname. Battle lines are our own words ("goes for", "a strong
match-up", "is worn out"). Never guilt the player.

## History

v0.1 (2026-09-13) was a soft pastel UI with rounded cards, drop shadows, pills and round icon buttons, replaced
by the box language on 2026-09-14. The home screen then carried a mood chip, a wish chip that named the lowest
need, a Skills card and a care block with need percentages; on 2026-09-21 those became the leaning chip, the
Treat chip, the battle menu and the camp block, and health and mana moved from pill bars to block bars.
