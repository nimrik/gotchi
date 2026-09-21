# Gotchi — Roadmap

Status: v0.2 (2026-09-21). Stages, not dates: two beginners at side-project pace, so velocity is unknown. The
to-do list for each stage is `08-project-checklist.md`.

## Done

- **Foundations.** Vision, audience, art direction for the cat, the UI language, the Unity project, the
  build-in-code approach, the smoke test and capture tooling.
- **Pet prototype** (2026-09-12 to 14). Needs, care, helpers, a skill tree with six mini-games, shop, story,
  leaderboards, news, settings, onboarding. Most of it was later cut or reshaped; the shell survived.
- **The cat** (2026-09-21). Rebuilt from the painted reference: model, rig, 27 clips, coats.
- **The battle refocus** (2026-09-21). Battle rules, 15 moves, mana, items, charms, leagues, daily quests, the
  Battle Club, the Market, health and mana that persist, the camp, leanings, the block bars. The Wild campaign
  was built and parked. Emotions, needs and the other mini-games were removed.

## Stage 1 — Make the fight feel good (next)

The rules exist; the fight does not feel like one yet.

- The six battle clips (defensive, attacking, screaming, healing, defeated, lightly wounded).
- Battle effects and sound: hit sparks, style-coloured flashes, heal glow, a hit sound, a win jingle.
- A balance pass on the rest loop, mana costs and the first leagues, played on a real iPhone.
- Play the first hour end to end: onboarding, the first style, the first ten fights, Silver.

## Stage 2 — Depth for the first release

- What the evolution stages look like and unlock.
- The story chapters rewritten for a fighter's journey.
- More moves, charms and arenas if Stage 1 shows the build gets stale; rival charms in the top leagues.
- Resolve the open monetization questions (`05-monetization-compliance.md`).

## Stage 3 — Online

- Accounts and cloud save (Supabase), real leaderboards and news.
- Other players as rivals: asynchronous first (fight an AI copy of a real build), live PvP after.
- Real trading, only once moderation and the under-13 question are settled.

## Stage 4 — Store readiness

StoreKit purchases with receipt validation, real notifications, the privacy policy and terms, the age rating
questionnaire, the IP review, store metadata that matches the positioning, TestFlight.

## Stage 5 — Soft launch

A small release, a way to hear from players, tuning from real numbers before any marketing.

## After the first release

- **The world to explore**, growing out of the parked Wild campaign: a map, movement, encounters, and with it
  **more characters**, all original.
- Friend battles, tournaments, seasons, clubs (`13-pvp-design.md`, section 8).
- Android. Hardcore mode. Several pets.

## Open questions

- Whether to timebox Stage 1 so it does not turn into endless polish before anyone outside has played.
- What is the smallest online feature set worth a first release: is the mock ladder enough for a soft launch?
