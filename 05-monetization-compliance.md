# Gotchi — Monetization & Compliance

Status: draft v0.1 — based on Apple's published guidelines as of mid-2026; re-verify before submission,
as these policies do get updated.

## Monetization model

- Free with in-app purchases (confirmed direction).
- Ads: not planned by default — and heavily constrained anyway if pursuing the Kids Category (see below).

## Apple's Kids Category — why it matters here

Our audience (5–13, skewing girls) overlaps heavily with Apple's Kids Category, which is a curated,
higher-trust section of the App Store — but it comes with binding requirements:

- Kids Category apps are placed into an age band by the developer: **5 and under, 6–8, or 9–11**. Apple's
  formal bands stop at 11, while our stated audience goes to 13 — this needs an explicit decision: either
  target the Kids Category strictly (framing the app for the ≤11 crowd) or position as a general-audience
  app appropriate for younger users without opting into the Kids Category (loses that discoverability
  shelf, but fits the full 5–13 range and eases some restrictions below).
- **No third-party analytics or advertising** in the general case. Limited exceptions exist for analytics
  that don't collect/transmit IDFA or any identifying info, and for contextual (non-behavioral) ads from
  networks with documented Kids-Category policies and human-reviewed ad creative.
- **No links out of the app, no purchase opportunities, no other "distractions"** unless placed behind a
  parental gate (e.g. a simple math problem or typed confirmation an adult would need to complete).
- **No PII or device info sent to third parties**, and general compliance with child-privacy laws
  (e.g. COPPA in the US) around data collected from children.
- Age rating is set via a questionnaire in App Store Connect; opting into "Made for Kids" there is a
  one-way decision once approved — all future updates must keep meeting Kids Category rules.

## What this means for design decisions already made

- **IAP**: fine, but must sit behind a parental gate — this needs to be designed into the purchase flow
  from the start, not bolted on later.
- **Analytics/telemetry**: default third-party SDKs are likely off the table in their standard
  configuration; plan for either a Kids-Category-compliant analytics provider or minimal first-party-only
  telemetry.
- **Notifications**: not directly an App Store compliance issue, but the "never wake the kid at night,
  never guilt-trip framing" principle from the design doc aligns with the general spirit of the Kids
  Category's child-safety intent, and is worth holding to regardless of which category we land in.
- **Social features (leaderboards, rankings)**: need care — anything resembling chat, user-generated
  content, or contact between children needs its own scrutiny pass; this hasn't been designed in detail
  yet and should be revisited with these constraints in mind.

## Decisions (updated)

- **Audience repositioned toward tweens/teens/nostalgic young adults**, not primarily under-13 — a real
  shift in tone, marketing, and content, not just a rating label (see `01-vision.md`). This is what makes
  skipping the Kids Category and aiming for a 13+ posture defensible.
- **Not opting into the Kids Category.** Note the age rating itself is *calculated* from a content
  questionnaire, not freely chosen — a cute pet-raising game may still calculate low regardless of
  marketing intent, and Apple can override a declared rating if actual design/marketing still reads as
  "for kids under 11" (Guideline 1.3). See `07-apple-compliance-questionnaire.md` for the full nuance.
- **IAP planned for cosmetics/visual effects and bonuses** — fine under Apple's rules either way. A
  lightweight purchase-confirmation step is being kept as good practice even without a strict Kids
  Category mandate, since this genre draws younger players in practice regardless of marketing.
- **Data collection limited to age (or age band) only** — no name, email, photos, or location. Stored as
  a flag, not tied to persistent identifiers, never sent to third parties — a sound default regardless of
  audience positioning.

See `07-apple-compliance-questionnaire.md` for the full working checklist and decision log.

## Open questions

- Whether to revisit the Kids Category decision once real usage data shows the actual audience age.
- Which specific IAP items are planned in detail (cosmetics? boosts? currency packs?) — affects the
  parental-gate flow design.
- Legal/compliance review — this document is a planning aid, not legal advice; a proper review (COPPA,
  GDPR-K if targeting EU, etc.) should happen before submission.
