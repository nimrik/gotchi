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

**Type** (Fredoka for headings/buttons, Varela Round for body — both OFL; Pixelify Sans is kept only as a fallback): page title 44–52 · card title 28–32 · body 24–26 ·
caption 18–22 · numbers in meters 32. Headings and buttons use the heading face; never wrap button labels.

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
| Panel | scrim + `CreateCard(Cream)` filled 28/28/100/120 | header 44 top, Back button bottom (400×72) |
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
- Tweens are restart-safe: `SimpleTween` remembers each transform's rest pose and cancels the previous tween,
  so repeated taps never compound. Use `PunchScale` / `PopIn` / `Hop`; never stack your own scale maths.
- Character: sprite split into head+ears and body slices — ears twitch, eyes blink (eyelid overlays), the whole
  body breathes and hops with squash-and-stretch. Reactions via `PetPortraitView.Boop()`.
- Pages: `PagedScroll` for swipeable category pages with snapping; tabs and swipe stay in sync.
- Meters: `FillTo` / `AnchorMaxXTo` 0.35 s only for jumps ≥2 points; otherwise set directly.
- Idle: pet breathes (2.8 s), cloud drifts, plant sways — subtle, never faster than 0.5 Hz.

## Layout of the home screen
Header (84) → room (flexible; status bar docked at its bottom, full width) → Skills card (170) →
dock (236). All blocks are full column width and separated by `Section`. Panels open over the safe area
with a scrim; only one panel is open at a time.

## Writing
Short, warm, second person ("Snack time?", "Take them home"). Never guilt the player ("your pet is
suffering" is banned). Numbers are shown, not hidden, when they help ("+30 Energy", "8%").
