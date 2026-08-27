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

## Tooling

- No in-house 2D/3D art skill on the team currently — AI generation tools (e.g. PixelLab.ai and others
  TBD) planned for asset generation.
- Recommended split: lean on AI tools for high-volume, lower-stakes 2D assets (icons, backgrounds, item
  variations, UI elements). Treat the 3D creature model + rig/animations as a higher-stakes asset worth
  extra care (possibly a freelance 3D artist) since it's the thing players look at constantly.

## Open questions (needs its own research pass)

- Concrete mood board / reference set beyond Celeste (kawaii character reference sources, palette studies).
- Final internal render resolution and pixel scale for the 2D layer.
- Visual identity of the creature across evolution branches — how distinct should each branch look?
- Consistent AI-tool pipeline: which tools, how outputs get cleaned up/made consistent, style-guide for
  prompting so assets don't look mismatched.
- UI style specifically — "premium, non-trashy" needs to be turned into concrete rules (palette limits,
  iconography style, typography) rather than staying a vibe.
