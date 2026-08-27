# Gotchi — Apple App Store Compliance Questionnaire

Status: draft v0.1 — working checklist to answer before submission. Not legal advice; a real legal/
compliance review should still happen before launch given the child audience. Re-verify against Apple's
live guidelines closer to submission, since these do get updated.

## Decision log (answered so far)

| Question | Decision | Notes |
|---|---|---|
| Target audience | **Repositioned to tweens/teens/nostalgic young adults**, not primarily under-13 | Genuine repositioning (tone, marketing, content) — see `01-vision.md`. This is what makes a 13+ posture defensible; a label change alone would not be. |
| Opt into Apple's Kids Category? | **No** | Consistent with the repositioned audience — Kids Category is for apps designed for ages 11 and under. |
| Age rating target | **Aiming for content that genuinely supports 13+** | Important: Apple *calculates* the rating from a content questionnaire — it isn't freely chosen. A cute pet-raising game with no mature content may still calculate lower (4+/9+) regardless of marketing intent, and Apple's Guideline 1.3 can override a declared rating if actual design/marketing still reads as "for kids under 11." Revisit this honestly once store copy, screenshots, and tone are finalized. |
| Does guideline 24.x (kids-under-13 rules) still apply? | **Likely reduced, not eliminated** | Guideline 24.x applies to apps "primarily intended for kids under 13." With a genuine older-audience repositioning this is less clearly triggered, but virtual pet games organically attract younger players in practice — worth keeping conservative defaults (see below) rather than assuming zero exposure. |
| Monetization | Free + IAP for **visual effects / cosmetics and bonuses** | No ads planned by default. |
| IAP access | **Parental-gate-style friction kept as good practice**, not treated as a hard Apple mandate under this positioning | Since the app may still be genuinely used by younger kids in practice, a lightweight purchase-confirmation step is still worth keeping even without a strict Kids Category obligation. |
| Personal data collected | **Age (or age band) only** | No name, email, photos, location, or other PII. Stored as a flag, not linked to persistent identifiers or sent to third parties. Keeping this minimal is a sound default regardless of audience positioning. |
| Behavioral advertising | **Not planned** | Keeping this off the table avoids reopening COPPA exposure even under the older-audience positioning, since actual users skewing younger is a real possibility for this genre. |

## Open questions to work through before submission

### App Store Connect setup
- [ ] Complete the age rating questionnaire in App Store Connect honestly — check what it actually
      calculates out to before assuming 13+ is achievable; if it lands at 4+/9+ anyway, revisit the
      audience/compliance framing rather than trying to force a higher number.
- [ ] Decide final category placement (Games subcategory choices — up to two).
- [ ] Confirm: NOT selecting "Made for Kids" checkbox, consistent with the repositioned audience.
- [ ] Keep store metadata (screenshots, app name, description, icon) consistent with the older-audience
      positioning — Apple has explicitly rejected apps where declared rating and actual presentation/
      content don't match (Guideline 1.3), including cases where developers tried to raise their rating
      specifically to escape Kids Category treatment.

### Parental gate implementation
- [ ] Design the parental gate screen(s) — needs to guard: entry to the IAP/shop screen, and any future
      links out of the app (social links, cross-promotion, support/contact links).
- [ ] Decide gate mechanism: math problem, typed confirmation code, hold-to-confirm, or platform-level
      (e.g. Face ID / device passcode re-entry) — needs a UX pass, not just a compliance checkbox.

### Privacy & data
- [ ] Write a privacy policy (required for apps primarily intended for kids under 13, regardless of category).
- [ ] Confirm the age-band collection flow: single yes/no or bracket question, stored locally/anonymously,
      never transmitted to third-party SDKs.
- [ ] Audit every third-party SDK before integration (analytics, crash reporting, IAP processing) for
      whether it transmits any identifier that could combine with the age flag to identify a specific child.
- [ ] Decide on COPPA compliance approach directly (this is a US federal law, separate from Apple's rules) —
      likely requires either avoiding data practices that trigger COPPA obligations entirely, or building
      the notice/consent mechanisms COPPA requires if any personal info collection expands later.

### Ads (if ever reconsidered later)
- [ ] If ads are added in the future: contextual only, no behavioral targeting, ad creative must be
      appropriate for the young audience — and Kids Category ad rules would apply anyway if the app is
      still "primarily intended for kids under 13," category opt-in or not.

### Ongoing
- [ ] Re-check this document against Apple's live App Review Guidelines before each submission — children's
      privacy rules are an area Apple updates periodically.
- [ ] If usage data later shows a meaningfully older audience than expected, revisit whether guideline 24.x
      framing still applies, and whether Kids Category becomes worth reconsidering.

## Reference (for context, not exhaustive)

Apple's relevant guideline language (paraphrased, not quoted verbatim):
- Apps primarily intended for kids under 13 must include a privacy policy, avoid behavioral advertising,
  and use a parental gate before allowing link-outs or commerce.
- Kids Category apps specifically must additionally avoid third-party analytics/advertising in the general
  case, and can't transmit PII or device info to third parties.
- A parental gate is meant to be something a young child can't reliably pass by accident (e.g. a math
  problem or typed confirmation), not just an "Are you an adult? Yes/No" tap.
