# Gotchi — Vision

Status: draft v0.1 — decisions captured from early planning conversations, revisit as the project evolves.

## Elevator pitch

A cute, kawaii virtual pet you raise and grow — built with a genuinely premium pixel-art/3D visual style
(think Celeste-inspired pixel-art world with a 3D creature at its center), instead of the cluttered,
low-effort look common in the current virtual pet market.

## Why this, why now

The virtual pet genre is active but visually and mechanically shallow — most competitors (Pou, My Tamagotchi
Forever, Bubbu, Moy) are 3D-cartoon or flat-vector, ad-heavy, and the pet itself barely changes once you've
filled its meters. Reviewers of the genre explicitly call this out as the category's biggest weakness.

Our opening: a genuinely well-art-directed pixel/3D hybrid pet, with real long-term depth (skill-tree driven
evolution, not just meters), aimed squarely at kids 5–13 (primarily girls) who are underserved by apps that
look "trashy" to a design-conscious audience (and to parents evaluating the app).

## Target audience (revised)

Shifted deliberately away from a "5–13, mostly girls" framing toward a broader audience — this is a real
repositioning (art tone, marketing, content), not just a label change on the App Store rating.

- Primary: tweens, teens, and nostalgia-driven young adults who like cute/kawaii aesthetics — not
  specifically children under 13.
- The kawaii/pixel-art visual identity stays; what changes is who we're designing and marketing *for*,
  so the app's actual content and store presence genuinely support a 13+ rating rather than fighting it.
- Rationale: Apple's age rating is calculated from a content questionnaire, and Apple's Guideline 1.3 can
  still treat an app as "for kids" based on its actual design/marketing regardless of a self-declared
  rating — so this only works if the repositioning is genuine (tone, marketing copy, store screenshots,
  content) and not just a settings change. See `07-apple-compliance-questionnaire.md`.
- Reality check to keep in mind: virtual pet games organically attract younger players regardless of
  marketing intent. This doesn't have to block the repositioning, but it means we should keep sensible
  baseline practices (age-neutral advertising if any is added later, no targeted ads to anyone who
  self-identifies as under 13, minimal data collection) rather than assuming a 13+ rating removes all
  child-safety considerations in practice.
- Parents are no longer the primary discoverability gatekeeper in the same way a Kids Category app would
  need — but a trustworthy, non-manipulative monetization approach is still worth keeping regardless.

**Open decision:** exact age floor/ceiling for marketing purposes (e.g. "9+", "13+", "all ages but styled
for teens+") — worth pinning down before writing store copy or finalizing character/tone direction.

## Differentiation vs. market

| Them | Us |
|---|---|
| Cartoon/flat 3D art, generic | Pixel-art world (Celeste-inspired) + stylized 3D creature |
| Pet plateaus after basic stats are filled | Long-term skill-tree driven evolution, branching creature forms |
| Ad-heavy, third-party trackers common | Free + IAP, minimal data collection by design |
| Simple meters, no strategy layer | Needs → automation → skill-tree strategy layer (Clash of Clans-style scaling) |
| Purely entertainment, no learning value | Two skill-tree branches (Social, Science) are genuine edutainment — language and general-knowledge mini-games woven into progression, not a bolted-on "educational mode" |

## Team & scope reality check

- First game for both of us; treat this as a real learning project as well as a serious release attempt.
- Two-person team (you + one colleague on implementation).
- We want to publish and monetize seriously — not just a portfolio piece — which means compliance,
  polish, and store requirements matter from day one, not as an afterthought.

## Open questions (revisit as design firms up)

- Exact age-band targeting for Apple's Kids Category (bands cap at 11; audience goes to 13) — see
  `06-monetization-compliance.md`.
- Whether "kawaii" leans more cute-Japanese-style or more Western-cute — affects art direction and
  reference gathering.
