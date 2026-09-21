# Gotchi — Apple App Store Compliance Questionnaire

Status: v0.2 (2026-09-21). The working checklist before submission. Not legal advice; get a real legal and
compliance review before launch, and re-check Apple's live guidelines each time, because they change.

## Decision log

| Question | Decision | Notes |
|---|---|---|
| Audience | Tweens, teens, nostalgic young adults; not primarily under-13 | A real positioning in tone, store copy and content (`01-vision.md`). A label alone would not hold up. |
| Kids Category | **No** | It is for apps designed for ages 11 and under. |
| Age rating | Whatever the questionnaire calculates, answered honestly | Apple calculates the rating; it is not chosen. The game now has turn-based fights between cartoon cats, so expect the cartoon or fantasy violence question (mild, no blood, the loser lies down). Apple can override a declared rating if design and marketing read as "for young kids" (Guideline 1.3). |
| Kids-under-13 rules | Reduced, not gone | Pet games draw younger players in practice, so the defaults below stay conservative. |
| Monetization | Free with fixed-price purchases: hearts, looks, timed bonuses | No ads. No random paid rewards. Hearts never buy battle power (`05-monetization-compliance.md`, with two open leaks). |
| Purchase friction | A YES / NO confirmation before every spend; real-money items hidden for the under-13 band | Built. Kept as good practice, not because a strict parental gate is mandated under this positioning. |
| Payment | Apple's in-app purchase system only | Guideline 3.1.1. Mock today; `StoreKitPurchaseService` is the production seam. |
| Personal data | An age band, stored locally, tied to no identifier, sent to nobody | The optional account (display name, email) is a mock today and must be covered by the privacy policy before it is real. |
| Advertising | None | Keeps behavioural-ad and COPPA exposure off the table. |
| User interaction | None today: no chat, no user-generated text; leaderboards show a display name, a cat and numbers | Display names will need a filter once accounts are real. Player-to-player trading and live PvP change this answer when they arrive. |
| Intellectual property | Original characters, names and text only | Rules in `05-monetization-compliance.md`. IP review before launch. |
| Notifications | One kind ("rested and ready"), quiet hours 21:00 to 09:00, switchable in Settings | Built as a log stub. |

## To do before submission

### App Store Connect
- [ ] Fill in the age rating questionnaire honestly and see what it calculates. If it comes out lower than the
      positioning assumes, revisit the framing; do not force the number.
- [ ] Choose the category and up to two Games subcategories.
- [ ] Confirm "Made for Kids" is NOT selected.
- [ ] Keep name, icon, screenshots and description consistent with the older-audience positioning, and free of
      any other game's name or catchphrases.

### Purchases
- [ ] Replace the mock with StoreKit, with receipt validation and Restore Purchases.
- [ ] Decide whether the confirmation step needs to be stronger for real money (a typed answer or a device
      passcode) given that younger players will be present.
- [ ] Settle the two leaks in the hearts rule (`05-monetization-compliance.md`) before products are created.

### Privacy and data
- [ ] Write the privacy policy and the terms; link them from Settings (stubs today).
- [ ] Review the wording of the age-band question in onboarding.
- [ ] Audit every SDK before it goes in (analytics, crash reporting, purchases, notifications) for identifiers
      that could combine with the age band.
- [ ] Decide the COPPA approach directly (US law, separate from Apple): avoid the practices that trigger it, or
      build the notice and consent it requires. The same for GDPR-K if the game ships in the EU.
- [ ] A display-name filter before real accounts.

### Before online features
- [ ] Trading: a moderation plan, and whether under-13 accounts can trade at all.
- [ ] Live PvP: no free text between players; report and block if any identity is shown.

### Ongoing
- [ ] Re-read Apple's App Review Guidelines before each submission.
- [ ] If usage shows a younger audience than intended, revisit the under-13 rules and the Kids Category.

## Reference (paraphrased, not exhaustive)

- Apps primarily intended for kids under 13 must have a privacy policy, avoid behavioural advertising, and use
  a parental gate before link-outs or commerce.
- Kids Category apps must also avoid third-party analytics and advertising in the general case and cannot send
  personal or device information to third parties.
- A parental gate is something a young child cannot pass by accident (a sum, a typed confirmation), not an
  "Are you an adult?" button.
