# Prompt for Claude Design: Gotchi home screen

Attach with it: `references/creature-character/cat/reference-tuxedo-cat-fish-pair.png` (the cat is the left one)
and a current screenshot of the home screen (`Gotchi -screenshot-home <base>`).

---

I'm building Gotchi, a cat battler with the heart of a virtual pet, for iPhone (portrait only), and I'd like you to design its UI. Start with the home screen, because the player spends most of every session there and every other screen is opened from it. Below is everything the game does, so you can decide what the home screen has to show, what it only has to lead to, and how much weight each thing deserves.

## The game in short

You keep one cat and take it into turn-based battles against other players' cats. A fight costs health and mana, and both stay where the fight left them, so between fights you look after the cat at the camp: REST gives health back, FOCUS gives mana back, FEED and GROOM lend attack and defense to the next few battles. Looking after the cat is preparation for battle, never a chore. Under a tenth of its health the cat is worn out and will not fight until it has rested.

The cat never dies and the game never guilts the player. Nothing drains while the player is away and nothing is lost by not playing; health and mana even refill on their own in ten minutes. Sessions are two to three minutes: a fight or two, a visit to the camp, coins spent on the build, leave.

The audience is tweens, teens and young adults who like cute, kawaii things and notice design. The competition (Pou, My Tamagotchi Forever, Bubbu) looks cluttered and cheap and is full of ads. Gotchi should look calm, deliberate and premium. There are no ads, no loot boxes and no random rewards, so the design needs no banner slots, chests or spin wheels.

## The cat

The cat is a 3D chibi tuxedo cat, rendered live by the game with flat colours and a thick wine-coloured outline (see the attached painting, left cat). It stands on two feet, the head is about half its height, and it has huge amber eyes, a white face blaze, a white chest and pink ear insides. Fur is `#5e4142`, whites `#fdf1df`, ear pink `#fc85ad`, eyes `#fbc437`, outline `#47102a`.

In your design, use the attached image or a simple placeholder where the cat stands. You are not designing the cat. What matters for the UI:

- The cat is the centre of the home screen and nothing competes with it. No panel, bubble or button overlaps it.
- It is a physical toy. The player taps it (head, body, paws and tail each react), holds a finger on it to make it purr, rubs it to pet it, drags it along the floor, lifts it so it dangles, and throws it so it bounces off the floor and the walls. The room therefore needs real empty space around the cat, and the side and top edges of the room act as walls.
- Left alone it is calm: it stands, sits, lies down, sleeps, grooms, stretches, and sometimes walks across the rug. Poked too often it gets irritated and walks off the screen for five seconds.
- Its state shows on the cat itself: worn out, it lies down until it has rested; poked too much, it looks irritated. There is no mood system. Floating feedback appears above it: hearts, a short word ("Purr~", "Boop!"), and "+440 HP" after a camp action. Leave air above its head for these.

## What the home screen holds today

From top to bottom. Treat this as the content to fit, not as a layout to keep. If a different arrangement serves the player better, propose it.

**Header.**
- A wallet showing both currencies on one line: coins (the working currency, e.g. 1439) and hearts (the premium currency, "the love your pet gives back", e.g. 85). Tapping the coins opens the shop on Bonuses, tapping the hearts opens it on Get Hearts.
- Four icon buttons: Story (book), Leaderboard (trophy), News (bell, with a pink dot while something is unread), Settings (gear).
- A Shop button, currently floating at the top right of the room, under the gear.

**The room.** A deliberately almost empty room, so nothing pulls the eye off the cat: a wall, a skirting board, a plain floor, a rug, and one window. The window tells the time: its sky follows the device clock through dawn, day, dusk and night, with the sun or a crescent moon on an arc, stars at night and one cloud by day, and the whole room goes lighter or darker with it. The chrome therefore has to look right over a bright daytime room and over a dark night room. The player can buy other backgrounds (Meadow, Beach Day, Snow Day, Starry Night), rugs and fairy lights, so the room is a full-screen scene that runs behind the UI to every edge, with the floor line a little below the middle of the screen.

**Status block**, docked at the bottom of the room, two rows. Row 1: the name and level ("Mochi · Lv 4"), a leaning chip (what kind of fighter the build adds up to, in capitals on the fighting style's colour: FIGHTER, GUARDIAN, SHADOW, or a grey ROOKIE before a style is picked), the XP bar to the next level with its caption in experience points ("104 / 220 XP"), and a Treat chip (fish icon; a snack worth 15% of health and mana, then "Treat · 42s" while it cools down). Row 2: health and mana as block bars, each followed directly by its numbers ("HP [bar] 1000/1200"). A block bar is a row of slanted blocks, one block per 250 points, with 2 px white separators and a fixed total width, so a bigger pool means narrower blocks: 1200 is four full blocks and a fifth that is 4/5 as wide. A block the value reaches is filled, one it does not reach is only outlined. Pressing the XP bar opens an info box with the full numbers (level XP, health and mana, the leaning, running buffs). The pet's name is chosen by the player and can be long.

**Battle menu.** One block with the league on top (league name, a progress bar to the next league, "25 / 200") and four doors under it: BATTLE (ranked fights), TRAIN, MOVES, BAG. Pressing the league bar opens an info box with the battle stats, rating, record and the camp buffs that are running. Before the player has picked a fighting style, the league line reads "PICK A STYLE IN BATTLE" and every door leads to the style picker. A fifth door, WILD (a campaign), exists in the code but is switched off for the first release, so the block should be able to take a fifth door later without a redesign.

**Camp block.** Four actions: REST (health +40%), FOCUS (mana +40%), FEED (ATTACK +10% for the next 3 battles), GROOM (DEFENSE +10% for the next 3 battles). Each shows its icon, its name, what it does under the name ("HP +40%", "ATK +10% · 3 BATTLES"), and on the right where it stands: READY, the seconds of cooldown left ("45S"), FULL when the bar has nothing to gain, or the battles a buff still lasts ("2 LEFT", green). This is how the home screen tells the player that looking after the cat is battle preparation, so it has to be readable at a glance. Each action has a 60-second cooldown, during which its icon dims. Helpers bought in the shop change the numbers (+60%, 5 battles).

**Transient layers.** Toasts at the bottom (login streak reward, level-up, new stage), tooltips, the info boxes, and a dialogue box that types text out and can ask YES / NO (used for purchase confirmations, story, log-out and reset).

## What the home screen leads to

You don't need to design these yet. They are here so the entry points, icons and hierarchy on the home screen make sense, and so the components you design can carry over. Every one of them opens as a panel over the home screen with a scrim, one at a time, with a title, optional pages the player swipes or steps through, and Back at the bottom.

- **Battle Club**, four pages that match the four doors. *Club*: league and rating, the cat's stats with its health and mana bars and the running camp buffs, the next rival (another player's cat in a different coat, shown before the fight so the player can prepare), the first-win-of-the-day bonus, three daily quests, and FIGHT!. *Train*: HP, ATTACK, DEFENSE and SPEED ranks bought with coins, capped by pet level. *Moves*: carry 4 of 15, learn the rest for coins, gated by league. *Bag*: items, a charm to hold, arenas, and a button to the Market.
- **The battle.** Our cat large in the foreground seen from behind, the rival smaller and facing us, each with a status box (name, style chip, level, the HP block bar; ours adds the MP bar and the numbers). A text box narrates. Actions: FIGHT (four moves, every one but the basic scratch costs mana; first tap shows the cost, details and STRONG / WEAK against this rival, second tap uses it), ITEM, CHEER, RUN. It ends in a results card with stars, a score count-up, rating change, coins and quest payouts, then Next battle or Back.
- **Fighting styles.** CLAW beats TRICK beats FLUFF beats CLAW, plus NORMAL. Their colours are fixed everywhere they appear: Claw coral, Fluff mint, Trick lavender, Normal grey. Leagues: Bronze, Silver, Gold, Crystal, Champion. A league once reached is never lost.
- **Market**: Buy (items, charms, arenas), Sell (at half price), Trade (three daily swaps with other players).
- **Shop**, six pages: Get Hearts (real money; hidden for players under 13), Bonuses (500 Coins, Full Recovery, No Cooldowns, Double Rewards, Streak Shield; timed ones show minutes left), Style (outfits the cat wears: Cherry Bow, Star Scarf, Cozy Beanie, Tiny Crown), Backgrounds, Room (rugs, fairy lights), Helpers (each makes one camp action better for good; costs coins and battle XP). Every purchase is fixed and visible, and every Buy asks YES / NO first. Hearts only ever buy looks and convenience, never battle power.
- **Story**: one chapter per pet level, played like an RPG cutscene (the cat in a framed scene, typed text below). Locked chapters say which level opens them.
- **Leaderboard**: Levels and Battle rating. Your own row first with your true rank, then the top 25. Tapping a row opens that player's profile with their cat.
- **News**: messages from the team, tagged NEWS / UPDATE / BUG FIX / EVENT.
- **Settings**, four pages: Sound & reminders, Account, Friends & codes (invite and promo codes), Purchases & about (restore, privacy, start over).
- **Onboarding**: account or guest, name the cat, a night doorstep scene where the player unwraps a bundle in a basket and takes the cat home, then two questions (age band, usual play time).
- **Progression that surfaces on the home screen**: pet level 1–12 from XP, a daily login streak that pays coins, battle XP that raises an evolution stage (a number only; the cat does not change shape), and one gentle reminder when the cat is rested and ready, never between 21:00 and 09:00.

## Visual direction

What exists today, as a starting point:

- Warm pastels: cream `#FFF6EC` and peach `#FFE1D0` backgrounds, white cards, ink `#4A3F55` text, muted `#9A8FA6` secondary text. One accent: pink `#FF9EBB` (dark `#E86F96`) for calls to action and active states. Coral, mint and lavender are the three fighting styles everywhere they appear; butter and sky are for rewards and held items. Health is a warm red-pink (`#FF5C7A` fill on a `#B8324E` line), mana a violet (`#8E7BFF` on `#5B49C9`).
- Two typefaces: Jersey 20, a pixel face, for headings, labels, buttons and every number (its digits stay distinct at small sizes); Varela Round for descriptions and long text.
- The chrome currently borrows the feel of a retro handheld RPG menu: framed boxes with a dark outline and a light inner line, flat fills, upper-case pixel labels, a ▶ cursor on the held option, HP-style bars. Only the chrome is retro; the room is painted smooth and the cat is smooth 3D.

I'm happy with the palette, the two typefaces and the rule of one accent colour. The box style is open: "premium, not trashy" has never been turned into concrete rules, and that is the main thing I want from you. If you keep the retro-handheld feel, make it our own. The design must not reproduce the screen layouts or look of any existing monster-battling game; the genre is borrowed, nothing else.

Principles I want kept:

- One idea per block, and no block competes with the cat.
- Icons carry actions, text carries information. Numbers are shown when they help ("+440 HP", "1000/1200"), never hidden.
- Everything tappable visibly reacts to a press (today: scale to 0.94, overshoot to 1.03, settle). Feedback is motion, not a colour change.
- Copy is short, warm and in the second person ("Take them home", "Rest first."). Nothing that blames the player; "your pet is suffering" style wording is banned.
- Touch targets fit a thumb, and the most used controls (camp, battle doors) sit in the lower half of the screen.

## Technical constraints

The UI is built in Unity in code, from simple parts: 9-sliced box sprites, flat or lightly graded fills, outlines, circles and pills, procedural icons, and the two fonts. Please design within that: no backdrop blur or glass effects, no photographic textures, no effects that would need a custom shader. Icons should be simple enough to draw as flat shapes in one or two colours.

The reference canvas is 1080 × 1920 with the iOS safe area applied at the root, and the layout stretches vertically by giving the room the spare height. Today the header is 84 tall, the status block 136, the battle menu about 140, the camp block 176, with 24 side margins and 12 between the bottom blocks. Please give sizes in these units so they map straight onto the canvas.

## What I'd like back

1. The home screen in high fidelity with this sample state: Mochi · Lv 4, leaning FIGHTER, 104 / 220 XP, the Treat chip ready, HP 1000/1400, MP 650/1400, 1439 coins, 85 hearts, Bronze League 25 / 200, REST ready, FOCUS on a 45 s cooldown, FEED with 2 battles left, GROOM ready, an unread news dot, the daytime room.
2. The same screen in the states that stress it: the night room; a worn-out cat (health under a tenth, lying down); full bars, so REST and FOCUS read FULL; every camp action on cooldown; no style picked yet ("PICK A STYLE IN BATTLE", the grey ROOKIE chip); a level-12 cat with full training (about 3500 health, fourteen blocks), to check that the blocks stay readable; an info box open over the XP bar; a toast showing; a 16-character pet name; the battle menu with the fifth WILD door. Check it on a short phone (iPhone SE proportions) as well as a tall one.
3. The component kit the rest of the game will reuse: box or card, row, primary / secondary / small buttons with pressed and disabled states, icon button, the block bar for health and mana, the thin progress bar, chip, tab or pager, toast, tooltip, info box, dialogue box with YES / NO, and the panel shell (scrim, title, pager, Back).
4. A short list of the rules you settled on (colour, type sizes, radii or frame, spacing, icon style), written so a developer can apply them to the screens you haven't drawn.

If the layout forces a trade-off, protect these in this order: the space around the cat, the health and mana bars and the camp block, the battle doors, then everything in the header.
