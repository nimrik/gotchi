# Gotchi — Monetization & Compliance

Status: v0.2 (2026-09-21). A planning aid, not legal advice. Apple's rules change; re-check them before every
submission. The working checklist is `07-apple-compliance-questionnaire.md`.

## Model

Free, with fixed-price in-app purchases. No ads, no loot boxes, no random paid rewards, no subscriptions in the
first release. Real money buys **hearts**; hearts buy looks and convenience.

## Decisions

- **Audience: tweens, teens and nostalgic young adults**, a real positioning in tone, store copy and content
  (`01-vision.md`). That is what makes skipping the Kids Category defensible.
- **Not in Apple's Kids Category.** The age rating is calculated from a content questionnaire and not chosen,
  and Apple can treat an app as "for kids" whatever it declares (Guideline 1.3). A battler with mild cartoon
  fights will be asked about cartoon or fantasy violence; answer it honestly and see what the rating comes to.
- **Data: the game needs an age band and nothing else.** No photos, contacts or location. The band is a flag
  in the local save, tied to no identifier, sent to nobody. Guests play the whole game. An optional account
  (display name, email, password) exists as a mock; before it becomes real it goes into the privacy policy and
  the data audit, and the password is never stored.
- **Under-13 band:** real-money items are hidden entirely.
- **Every purchase asks first.** A YES / NO box opens before any spend, coins included.
- **Apple's purchase system only** for digital goods (Guideline 3.1.1). No Stripe, no links out.
- **No third-party analytics or advertising SDKs** by default. Anything added later is audited first.
- **Notifications:** one kind only ("rested and ready"), never between 21:00 and 09:00, never guilt.
- **Social:** no chat and no user-generated text. Leaderboards show a display name, a cat and numbers.

## Intellectual property: original characters only

Have an IP lawyer look at the game before launch. The working rules:

- **No existing characters, in any form.** Creatures, names, logos, artwork, music and sound from another
  company's monster-battling game are protected by copyright and trademark, and their owners enforce both
  against free fan games as well as paid ones. Apple removes infringing apps and can close the developer
  account. Re-drawn, recoloured or renamed versions of a recognisable character are still copies. Every animal
  in Gotchi is our own design, starting with the cat.
- **The genre is free, its expression is not.** Turn-based battles, types that beat each other, move lists,
  stats, levels, wild encounters, leagues and trading are ideas, and ideas are not protected. What is protected
  is the specific expression: character designs, names, distinctive move names, battle text, closely copied UI
  layouts, maps, music. So our styles are CLAW / FLUFF / TRICK, our moves and items are our own words, and the
  battle lines are written from scratch ("goes for", "a strong match-up", "is worn out").
- **Trademarks:** another game's name or catchphrases never appear in the app's name, subtitle, keywords,
  description or screenshots, not even as "like X".
- **Patents:** game mechanics can be patented, and large publishers hold patents on specific mechanics (for
  example capturing a creature by throwing an item at it, or riding creatures) and have sued over them. Plain
  turn-based battling is not such a case, but a future catching or befriending mechanic gets a patent check
  first.
- **Inspiration is fine**, in internal documents too. Shipping assets, text or names is not.

## Economy

Two currencies, in one wallet box at the top left of the home screen.

| | **Coins** (the working currency) | **Hearts** (premium: the love the cat gives back) |
|---|---|---|
| **In** | ranked battles (25 × league on a win, 8 × league on a loss, streak up to +50%), first ranked win of the day +50, daily quests 30 to 80 each, promotions 100 to 500, selling at half price, login streak (10 × streak day, max 50), promo and invite codes | +5 per pet level, +10 every seventh streak day, promotions 5 to 25, all three daily quests +3, promo codes, bought with money |
| **Out** | stat training (about 11,000 in all), moves (about 3,000), charms (1,700), battle items (30 to 90), helpers (150 each), Full Recovery (40), style change (150), Back Garden arena (400), outfits (120 to 250), backgrounds (300 to 350), room decor (150 to 200) | premium outfits (30, 80), Starry Night background (40), Sunset Beach arena (30), No Cooldowns 1 h (15), Double Rewards 1 h (20), Streak Shield (10), 500 Coins (20) |

- **The rule: hearts never buy stats, moves, items, charms or rating.** Hearts can be bought, and the rating
  board has to stay about play. Boosts shorten waits and multiply what a fight pays; they do not decide a fight.
- **Real money** (App Store only): 50 / 150 / 400 Hearts, and a one-time Starter Pack (120 hearts, the Cozy
  Beanie, the Snack Dispenser helper). In-game labels read "App Store" until the products exist.
- **Left out on purpose:** loot boxes and any random paid reward, ads, progress gates that only money opens,
  real-money items for the under-13 band, rating decay or anything else that punishes not playing.
- **Why it hangs together:** every coin sink has a coin source, so a player who never pays reaches everything
  except the premium looks. Battles are the biggest source and training is the biggest sink, so playing the game
  is what pays for getting better at it.
- The rules of what a battle pays are in `13-pvp-design.md`, section 5. Shop names say what you get ("500
  Coins", "No Cooldowns"), never a nickname.

## Open questions

- **Two leaks in the hearts rule.** The 500 Coins exchange turns bought hearts into training coins, and the
  Starter Pack contains a helper (FEED lasts five battles instead of three). Options: drop both, cap the
  exchange per day, or restate the rule as "power is never sold directly". Decide before real products exist.
- Player-to-player trading (the Market's trade board is a mock today) needs a moderation plan, and a decision
  on whether under-13 accounts can trade at all, before it goes live.
- A monthly pass with exclusive outfits was floated. It needs StoreKit subscription handling and its own review.
- Privacy policy, terms, COPPA and GDPR-K review before submission.
- Whether to revisit the Kids Category once real usage shows the actual audience age.

## History

v0.1 assumed a 5 to 13 audience and weighed Apple's Kids Category (age bands up to 11, no third-party
analytics or ads, a parental gate before any purchase or link out, a one-way "Made for Kids" flag). The
audience was repositioned and the category declined; the conservative defaults above are what remains of it.
Gems were renamed hearts on 2026-09-14. The shop's Full Refill became Full Recovery when the needs were removed.
