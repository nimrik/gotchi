# Gotchi — UI Guide

Status: v0.1 (2026-09-13). The rules every screen follows. The same tokens live in code
(`UIFactory` — colours, `Spacing`, sprites, components), so build screens from those helpers and this
document stays true. When you add a pattern, add it here in the same commit.

## Principles
- **One thing per block.** A card holds one idea (the pet, skills, care). No block competes with the pet.
- **Icons carry actions, text carries information.** Controls are icon buttons with a short caption where
  meaning isn't obvious; numbers and names are text.
- **One accent.** Pink is the call-to-action colour. Need colours (coral/sky/lavender/pink) appear only on
  need meters and care buttons; branch colours only inside the Skills/leaderboard lists.
- **Same spacing everywhere.** Use the scale below; never eyeball a gap.
- **Everything tappable moves.** Every button, chip, row or card that reacts to a tap has the press animation.

## Tokens (`UIFactory`)
| Token | Value | Use |
|---|---|---|
| `Cream` | #FFF6EC | page background, panel cards |
| `Peach` | #FFE1D0 | top gradient glow |
| `Card` | white | content cards |
| `Ink` | #4A3F55 | primary text, icons on light |
| `Muted` | #9A8FA6 | secondary text |
| `Pink` / `PinkDark` | #FF9EBB / #E86F96 | CTA buttons, active states / emphasis text |
| `Coral` `Sky` `Lavender` `Butter` `Mint` | pastels | needs (Hunger/Hygiene/Energy/Happiness), rewards, nature |
| `Shadow` | rgba(115,77,89,0.14) | card drop shadow (offset −10 down) |
| `Scrim` | rgba(74,64,84,0.45) | behind panels |

**Spacing** (`UIFactory.Spacing`): `Section` 24 between blocks · `List` 16 between rows · `Pad` 24 inside
cards · `Gutter` 24 page margins. Canvas reference is 1080×1920; safe area applied at the root.

**Type** (all OFL): the pixel face is **Jersey 20** (`PixelFont`, drawn at ×1.1 via `PixelScale`) for headings,
labels, buttons and every number — chosen because its digits stay distinct (Pixelify Sans drew 8 like S and 2
like Z, so "Stage 8/8" and wallet amounts were unreadable; it is kept only as a fallback). Body copy is Varela
Round. Page title 44–52 · card title 28–32 · body 24–26 · caption 18–22 · wallet amounts 30. Never wrap
button labels.

**Shape** (`UIFactory.Radius`): cards 32 · rows 24 · inputs 24 · everything else that is a control — buttons,
chips, badges, tooltips, toasts, bars, sliders — is a **pill** (radius = half its height, kept exact by the
`PillRadius` component; use `CreatePill`). Icon buttons and meters are circles. All shapes are anti-aliased
and render at native resolution at any window size; only the character is pixel art (its sprite is resampled
to a 112-px grid).

**Button types** (`UIFactory.ButtonHeight`): Large 84 (primary actions, Back, onboarding CTAs) · Medium 64
(row actions: Play / Buy / Unlock / Train) · Small 52 (chips like Cuddle). Primary = `Primary` pink with a **white** label; secondary = white with ink; row actions may use the row's
context pastel with ink. Label colour is chosen by background luminance (`LabelColorFor`): white below 0.76,
ink above. No hover/press colour tint — feedback is the scale animation. Disabled text buttons dim; disabled icon
buttons keep their white disc and dim only the icon (45%) plus show a badge, so icons never blend into grey.
Any hand-built control must call `LabelColorFor` for its label.

## Components
| Component | Helper | Notes |
|---|---|---|
| Card | `CreateCard` | white, drop shadow, `Pad` inside; full column width |
| Row | `CreateCard(cornerScale 0.8)` | flat (no shadow) inside panels and lists — shadows are only for top-level cards |
| Panel | scrim + `CreateCard(Cream)` filled 28/28/100/120 | header 44 top, Back button bottom (72 tall, fitted to its label) |
| Wallet box | `CreateWalletBox` | Sapphire money window: one white box, both currencies on one line — `[icon] 1439 COINS   [icon] 85 HEARTS` (pixel 34); 620×84 in the header, centred in the shop |
| Primary button | `CreateButton(color: Pink)` | ink label, height 72–96 |
| Secondary button | `CreateButton(color: Card)` | e.g. Back |
| Icon button | `CreateIconButton` | round; 84 in header, 116 in dock; icon on white when the ring/background is coloured |
| Ring meter | `NeedRingView` | radial ring = value; the button inside is white |
| Pill / chip | `CreateRounded(cornerScale 0.7)` + text | sized to content (`Text.preferredWidth` + padding) |
| Pill bar | `CreatePillBar` | 14–16 tall, fill via `anchorMax.x` |
| Row | `CreateCard(cornerScale 0.8)` 92–120 tall | title 26–28 top-left, subtitle 21–22 below, action right (150×56) |
| Toast | ink pill, white text, bottom | pop in, 2.2 s, fade |
| Tooltip | ink pill above element | 5 s, e.g. low-need percentage |
| Input | `CreateInputField` | 84 tall, placeholder muted |

## Motion (`SimpleTween`, `PressFeedback`)
- Press: scale to 0.94 in 70 ms on touch; release overshoots to 1.03 then settles (`PressFeedback`, added by
  every button helper and `MakePressable`). Disabled buttons don't animate.
- Appear: `PopIn` 0.2–0.3 s (ease-out-back). Reward moments: `PunchScale`, hearts, confetti.
- **One box language (Pokémon Sapphire reference, 2026-09-14).** Every container in the chrome is the same
  GBA box: 6 px dark outline (`FrameDark`), 4 px light inner line (`FrameLight`), flat fill, corner pixel cut.
  It is one point-filtered, 9-sliced sprite with an opaque centre, so `CreateFrame`, `CreateCard` and
  `CreatePill` all return a single tinted `Image` (the bevel tints with the fill). No drop shadows, no pill
  radii on containers; `CreateRounded` / `CreateCircle` remain only for illustration shapes (room props, pots).
- **Meters** (`CreatePillBar`) are Pokémon HP bars: a 3 px outlined track with the fill inset 4 px. The care dock
  colours them green > 50, yellow > 20, red below; other bars keep their branch colour.
- **Surfaces.** Home: the background set runs the full screen, floor included, and the status, Skills and care
  boxes sit straight on it (the blue band was dropped 2026-09-14 — it cut the floor off above the buttons). Panels (Skills, Shop, Settings, Story, Leaderboard) are `PanelBlue` boxes with white
  rows. Text on blue uses `MenuInk`, never `Muted`.
- **Type.** Headings and labels use the pixel font (`CreateText(..., heading: true)` and `CreatePixelText`),
  with a 2 px light shadow like the GBA. Descriptions and long body copy stay in Varela Round for readability.
- **Buttons** (`CreateButton`): framed, upper-case pixel label aligned left, a ▶ cursor while held. Fill colour
  encodes the type (Primary pink with white label, white, butter/mint/coral secondary). Icon buttons are square
  boxes, including the care dock. Buttons that carry one short word (Back, Close) are shrunk to that word with
  `FitToLabel` (cursor gutter + text + padding, min 160 wide) — never leave a word alone in a 400 px box.
- **Choices.** Options the player picks between are `MenuGrey` boxes; the chosen one turns white (`Card`), like
  NEW GAME / OPTION on the title menu. Used for onboarding questions and the species grid.
- **Tabs / screen switchers** (`TabBarView`, one API: `Select(i)`, `OnSelected`). Four looks to choose from:
  - *Segmented* — one box split into segments, chosen one white, others grey. Used for Levels / Skills on the
    leaderboard.
  - *Chips* — separate boxes with a white icon disc and a label, wrapping into rows. Used for the seven branches
    on the leaderboard.
  - *Underline* — plain labels with a thick bar under the active one. Used across the settings sub-pages.
  - *Arrows* — ◀ TITLE ▶ pager with a 1/5 counter inside the title box, Pokémon Bag style; pairs with
    `PagedScroll` swiping. Used in the shop.
- **Care block.** One white box on the blue band holding the four care options in a 2×2 grid, like FIGHT /
  BAG / POKéMON / RUN: icon, plain pixel name (no shadow), and the need as a percentage on the right (ink,
  amber under 50, red under 20). No bars. Each cell is the button and shows ▶ while held. During cooldown the
  icon dims and the name reads the seconds left; below 10% a tooltip pops over the cell.
- **Mood bubble.** A framed speech box beside the pet's head with a tail cut into its border (two rotated
  squares: dark behind the box, white inside), so the status is attached to the character, never floating.
- **Panels share one shape** (Shop, Settings, Leaderboard): big pixel title (52) at the top, a ◀ PAGE ▶ arrows
  pager with a counter, swipeable `PagedScroll` pages, Back at the bottom. Settings pages: Sound & reminders,
  Account, Friends & codes, Purchases & about. Leaderboard pages: Levels, then one board per branch.
- **Status bar.** Name and level as one line ("Mochi · Lv 4"), no level chip; the XP bar (200 wide) and its
  caption ("79% to Lv 5") sit on that same line right after the level, positioned from the name's
  preferred width in `SetLevel`, so the second line is just species + "tap for story". The wish chip's disc is 38 px with a 26 px icon.
- **Leaderboards.** Your row first (pink, true rank), a "TOP 25" divider line, then the rest without you.
  Board selection is the same ◀ ▶ pager as the shop (the old segmented + chips tabs looked like list cards).
- **Shop cards.** Pastel art tile (thin outline, colour per category) on top, pixel name, short description,
  then a price chip (currency icon + "120 COINS" / "40 HEARTS", or APP STORE / FREE) and the action button on one
  row. Names say what you get ("500 Coins", "No Cooldowns", "Double Rewards"), never a nickname.
- **Header.** One wallet box on the left, one line: `[coin] 1439 COINS   [heart] 85 HEARTS`
  (tapping a line opens the shop on Bonuses / Get Hearts), then trophy, bell (news; pink dot when unread), gear; the
  shop box sits under the gear with the same 12 px gap. Options that are not selected are light grey
  (`MenuGrey` D4D5DF) with ink text.
- **Backgrounds** (`RoomScenes`). The room's scene layer holds one background set: Cozy Room (free default),
  Meadow, Beach Day, Snow Day, Starry Night. The Cozy Room is a **lofi study at dusk** (the "lofi hip hop
  radio" look, asked for 2026-09-14): a big window onto a purple night city with lit windows, moon and stars;
  a desk with a glowing laptop, mug with steam, lamp with a warm light cone, headphones and books; posters, a
  shelf with a radio, a plant, string lights along the top. Painted once by `ScenePainter` (anti-aliased SDF
  shapes with gradients and soft shadows — bilinear, never pixel-blocky); bulbs and a few stars twinkle as
  live UI circles. The other sets are lighter procedural scenes. Every set also
  tints the whole-screen backdrop and glow (`Scene.Backdrop/Glow`). Scenes are painted into a full-screen
  scene root behind the safe-area column (`HUDController` → `RoomView(sceneParent)`), so they run seamlessly
  to every screen edge; the floor line sits at 0.444 of the screen height, under the rug. Rug and fairy lights
  sit on top of any set. Shop cards show a `RoomScenes.Preview` tile.
- **Text box** (`DialogBoxView`). Typewriter at 45 chars/s, tap to finish or continue, bouncing ▼ when there is
  more. `Ask(text, options, onPick)` pops a YES / NO box with a ▶ cursor above the right corner. Used for story
  chapters, the doorstep intro, purchase confirmations, log-out and reset. The HUD owns one global box; the
  story panel and onboarding own their own.
- Tweens are restart-safe: `SimpleTween` remembers each transform's rest pose and cancels the previous tween,
  so repeated taps never compound. Use `PunchScale` / `PopIn` / `Hop`; never stack your own scale maths.
- Character: `CreatureRig` (Root → Anim → Body/Head/ears/arms/feet/tail/face), parts painted as pixel sprites,
  driven by `CreatureAnimator` (base loop + one-shot clips). Use `PetPortraitView` everywhere: `SetEmotion`,
  `Play(OneShot.X, direction)`, `SetLoop`, `React(PetPart)`, `EnableTouch`, `SetAccessory`, `SetConditions`.
  Mini-games get it through `MiniGameStage.Play / Face / LungeTo`. See `03-art-direction.md` for the part spec.
- Pages: `PagedScroll` for swipeable category pages with snapping; tabs and swipe stay in sync.
- Meters: `FillTo` / `AnchorMaxXTo` 0.35 s only for jumps ≥2 points; otherwise set directly.
- Idle: pet breathes (2.8 s), cloud drifts, plant sways — subtle, never faster than 0.5 Hz.

## Layout of the home screen
Header (84) → room (flexible; status bar docked at its bottom, full width) → Skills card (124; icon with a
40 px margin, "Skills · Stage 2/5" with the stage bar stretched beside it up to Train, branch line under) →
care block (164). All blocks are full column width and separated by 12 px (tighter than `Section`, so the
three bottom boxes read as one stack on the floor). Panels open over the safe area
with a scrim; only one panel is open at a time.

## Writing
Short, warm, second person ("Snack time?", "Take them home"). Never guilt the player ("your pet is
suffering" is banned). Numbers are shown, not hidden, when they help ("+30 Energy", "8%").
