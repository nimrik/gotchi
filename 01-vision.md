# Gotchi — Vision

Status: v0.2 (2026-09-21). Rewritten after the game was refocused on battles.

## Pitch

A cute cat you take in, build into a fighter and take up a ladder of turn-based battles, with the look of a
well-made handheld RPG instead of the cluttered look common on the store. It keeps the heart of a virtual pet
(one animal that is yours, that you feed, groom and let rest) and gives it something to be good at.

## Why this

Virtual pet games are active on the store but shallow: once the meters are full the pet has nothing to do, and
most of them lean on ads and guilt ("your pet is starving"). Monster battlers have depth but hand the player a
hundred creatures and no attachment to any of them. Gotchi sits between the two: **one animal, a real build,
a ladder to climb, and nothing that punishes the player for having a life.**

## Who it is for

- Tweens, teens and nostalgic young adults who like cute, kawaii things and grew up on handheld RPGs. Not
  primarily children under 13.
- This is a real positioning, in tone, store copy and content, and not a label. Apple calculates the age rating
  from a content questionnaire and can treat an app as "for kids" whatever it declares (Guideline 1.3). A
  battler with mild cartoon fights supports an older rating more honestly than a pure pet game did. See
  `07-apple-compliance-questionnaire.md`.
- Pet games draw younger players whatever the marketing says, so the defaults stay conservative: the only data
  asked for is an age band, real-money items are hidden for under-13, there are no ads, no chat, and every
  purchase asks for confirmation.

## What sets it apart

| Them | Us |
|---|---|
| Generic cartoon 3D or flat vector art | One hand-built chibi cat with flat colours and an ink outline, in a calm room, framed by a handheld-RPG box UI |
| The pet plateaus once its meters are full | A build to grow (style, stats, moves, charm), a league ladder, daily quests |
| Meters that drain while you are away, guilt notifications | Nothing decays, the cat never dies, one reminder at most: "rested and ready" |
| Ads, trackers, loot boxes | Free with fixed-price purchases; hearts buy looks, never power; minimal data |
| A hundred creatures, none of them yours | One animal with a name, a story told in chapters, and a face |

## The team

- Two people, side-project pace (a few hours a week each), first game for both, beginners at Unity and C#.
- The aim is a real release that earns money, so compliance, polish and store rules count from the start.
- Consequence for every plan: small scope, one mode at a time, systems before content, mocks before servers.

## Open questions

- The exact age wording for the store ("9+", "13+", "all ages, styled for teens") once the rating questionnaire
  has been filled in honestly.
- How much "pet" the battler keeps in the long run: is the camp enough, or does the home need more to do that
  is not about the next fight?
- Whether kawaii leans Japanese-cute or Western-cute in store art and later characters.

## History

Until 2026-09-21 the pitch was a virtual pet with four draining needs, a seven-branch skill tree with a
mini-game per branch (two of them educational quizzes), thirteen species and forty-five emotion states. The
mini-games, the needs, the emotion system and the species picker were removed in favour of one cat and one
battle system done well. The old text is in git history.
