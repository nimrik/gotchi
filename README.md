# Gotchi — Project Docs

Planning documents for Gotchi, a kawaii pixel-art/3D virtual pet game for iOS built in Unity.

These are living drafts (v0.1) meant to be argued with and revised — not final specs. Every doc has an
"Open questions" section at the bottom; treat those as the actual to-do list for planning.

- [01 — Vision](01-vision.md) — pitch, audience, differentiation
- [02 — Game Design](02-game-design.md) — core loop, progression, sessions, mini-games
- [03 — Art Direction](03-art-direction.md) — visual style, references, tooling
- [04 — Tech Plan](04-tech-plan.md) — Unity, platforms, rendering, save system
- [05 — Monetization & Compliance](05-monetization-compliance.md) — IAP, Apple Kids Category
- [06 — Roadmap](06-roadmap.md) — phased plan from prototype to soft launch
- [07 — Apple Compliance Questionnaire](07-apple-compliance-questionnaire.md) — working checklist + decision log for App Store submission
- [08 — Project Checklist](08-project-checklist.md) — concrete action items mapped to each roadmap phase
- [09 — Pets & Emotions](09-pets-and-emotions.md) — species roster, emotion taxonomy, art production scope
- [10 — Unity Setup & Code Map](10-unity-setup.md) — how to open the project, what's implemented vs. stubbed
- [11 — Game Flows & Paths](11-game-flows.md) — every player flow, technical path and dev/QA path; update it with the code
- [12 — UI Guide](12-ui-guide.md) — tokens, components, spacing and motion rules every screen follows

## Code

The game's runtime code lives in `Assets/Scripts/Gotchi/` (plain C#, built entirely in code — no hand-made
prefabs or scenes). See `10-unity-setup.md` for the one-time Unity project setup and a verification checklist.

## Suggested next step

Generate the Unity project shell and run the game for the first time — follow `10-unity-setup.md` step by
step, fix any compile errors the Editor reports, then work through its verification checklist. After that:
the art mood board / style lock (`03-art-direction.md`) and the first real sprite set (`09-pets-and-emotions.md`).
