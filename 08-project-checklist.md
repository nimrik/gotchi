# Gotchi — Project Checklist

Status: v0.3 (2026-09-21). The working to-do list, by the stages in `06-roadmap.md`. `[x]` done, `[~]` partly
done, `[ ]` open. Edit it constantly; it is not fixed scope.

## Done

- [x] Vision, audience (tweens, teens, young adults), two-person side-project scope (`01-vision.md`)
- [x] Unity 6000.6.2f1 project, built-in render pipeline, uGUI, everything built in code, iOS export compiles
- [x] Tooling: Editor menu, headless smoke test (258 checks), capture runs, `-tempsave`, `-hour`, creature lab
- [x] The cat: model, rig, 27 clips, face meshes, accessories, coats (`03-art-direction.md`)
- [x] First release is cats only: onboarding names the cat, keepers' cats differ by coat
- [x] Home screen: wallet, story, leaderboards, news, settings, shop; the room with the time-of-day window;
      status block (name, level, leaning, XP, Treat, health and mana block bars); battle menu; camp block
- [x] Touching the cat: part reactions, petting, pick up and throw; taps wear its patience until it walks off
- [x] Battle rules: styles, 15 moves, mana, stat stages, priority, crits, recoil, items, charms, rival AI
- [x] Battle screen: back view, rival coats, move info with mana cost, self-playing mode for captures
- [x] Battle Club: Club / Train / Moves / Bag pages, leagues, rating, first-win bonus, daily quests, rival card
- [x] The Market: buy, sell at half price, daily trade board (mock keepers)
- [x] Health and mana persist after a fight and refill with time, also offline; worn out under a tenth
- [x] The camp: REST, FOCUS, FEED, GROOM, the Treat; helpers improve camp actions; nothing decays
- [x] Leanings (style × most trained stat) in the status block
- [x] Level and story chapters, battle XP and evolution stages, login streak, streak shield
- [x] Shop: hearts packs (mock), bonuses, outfits, backgrounds, room decor, helpers; YES / NO before every spend
- [x] Leaderboards (levels, battle rating) and keeper profiles against 24 mock keepers
- [x] News centre, settings, promo and invite codes, accounts (mock), age band with under-13 restrictions
- [x] Removed: the four needs, the emotion system, six mini-games and their branches, the species picker
- [x] Original-characters rule written down (`05-monetization-compliance.md`)

## Stage 1 — Make the fight feel good

- [ ] Author the six battle clips: defensive, attacking, screaming, healing, defeated, lightly wounded
      (`13-pvp-design.md`, section 7). `BattleMiniGame.PlayCue` plays stand-ins until then
- [ ] Battle effects: hit sparks, style-coloured flashes, heal glow
- [ ] Sound: hits, menu, win and loss, camp actions (there is no audio in the game yet)
- [ ] Balance pass: rest loop, mana costs, Bronze and Silver rivals (open questions in `13-pvp-design.md`)
- [ ] Run on a real iPhone (code signing is the only step left) and check bar and text sizes in the hand
- [ ] Play the first hour end to end and write down where it drags

## Stage 2 — Depth for the first release

- [ ] Evolution stages: what each looks like and unlocks
- [ ] Rewrite the story chapters for a fighter's journey
- [ ] Decide the two leaks in the hearts rule (coins exchange, the helper in the Starter Pack)
- [ ] Rival charms from Gold up, if the top leagues turn into stat checks
- [ ] Remove or archive the 2D creature code if no other character will use it

## Stage 3 — Online

- [ ] Supabase: accounts, cloud save, leaderboards, news (`04-tech-plan.md`)
- [ ] Asynchronous rivals (an AI copy of a real player's build), then live PvP with server-side rolls
- [ ] Real trading: only after a moderation plan and the under-13 decision
- [ ] Display-name filter

## Stage 4 — Store readiness

- [ ] StoreKit purchases, receipt validation, Restore Purchases
- [~] Notifications: scheduling and quiet hours done, delivery is a log stub (`com.unity.mobile.notifications`)
- [ ] Privacy policy and terms, linked from Settings
- [ ] Age rating questionnaire; SDK audit (`07-apple-compliance-questionnaire.md`)
- [ ] IP review by a lawyer
- [ ] Store metadata: name, icon, screenshots, description for the older audience
- [ ] TestFlight

## Stage 5 — Soft launch

- [ ] A small release and a way to hear from players
- [ ] Tune pacing and economy from real numbers

## After the first release

- [ ] Design the world to explore that grows out of the parked Wild (`GameFeatures.Wild`)
- [ ] More characters, all original (a patent check before any catching mechanic)
- [ ] Friend battles, tournaments, seasons, clubs
- [ ] Android, Hardcore mode, several pets
