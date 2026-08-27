# Gotchi — Tech Plan

Status: draft v0.1

## Engine & platform

- Engine: Unity.
- Primary launch platform: iOS.
- Android: technically straightforward to add from the same Unity project (install Android Build Support
  module, switch active platform) — the real cost is everything *around* the engine: Google Play Billing
  vs Apple IAP integration, wider device/screen fragmentation testing, a second store listing and review
  process, and duplicated compliance work. Recommendation: build platform-agnostic from day one (avoid
  iOS-only APIs where avoidable), but treat Android as a genuine post-iOS-launch follow-up, not a parallel
  simultaneous release, given this is a first project for a two-person team.

## Rendering approach

- 2D pixel-art world/UI at a fixed low internal resolution, scaled with nearest-neighbor filtering, to get
  Celeste-like crispness rather than jagged scaling on Retina screens.
- 3D creature (and some mini-games) rendered in the same scene — needs a defined camera/rendering setup
  that reconciles a 2D-styled world with a 3D character convincingly.

## Data & save system

- Needs: pet state, needs/stats, skill-tree progress, evolution state, owned cosmetics — all TBD in detail,
  but should be designed for **real-time simulation** (state must be computed correctly even after the app
  has been closed for hours/days), not just "tick while open."
- Cloud save (for device transfer / reinstall protection) — recommended given the sunk emotional investment
  in a raised pet — implementation TBD (Apple's Game Center / CloudKit-backed or custom backend).

## Third-party services — constrained by Kids Category compliance

Kids Category apps cannot include third-party analytics or advertising in the general case, and can't
send personally identifiable or device info to third parties. This affects tooling choices directly:

- Avoid default third-party analytics SDKs (e.g. standard Firebase Analytics) unless a Kids-Category-safe
  configuration is confirmed.
- Any ad monetization (if considered later) would need contextual, human-reviewed, non-behavioral ad
  partners with documented Kids Category compliance — not standard behavioral ad networks.
- In-app purchases should go through the platform's own IAP system, gated behind a parental gate.

See `06-monetization-compliance.md` for the full compliance picture.

## Notifications

- Push notifications for care reminders — must respect quiet hours (no nighttime pings) and avoid
  manipulative "pet is suffering" framing.

## Open questions

- Backend needs: is a lightweight custom backend required (for leaderboards, seasonal content, social
  rankings) or can platform-native services (Game Center / Google Play Games) cover v1?
- Specific save/sync architecture.
- Team's current Unity/C# experience level — affects how much learning-curve buffer to build into the roadmap.
