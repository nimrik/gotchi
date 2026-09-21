using System;
using System.IO;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Economy;
using Gotchi.Systems;
using Gotchi.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Gotchi.EditorTools
{
    // Menu: Gotchi ▸ Create Main Scene / Run Logic Smoke Test.
    // Batch: Unity -batchmode -quit -projectPath <p> -executeMethod Gotchi.EditorTools.GotchiEditorTools.<Method>
    public static class GotchiEditorTools
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Gotchi/Create Main Scene")]
        public static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var game = new GameObject("Game");
            game.AddComponent<GameBootstrap>();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Gotchi] Created {ScenePath} with GameBootstrap.");
        }

        [MenuItem("Gotchi/Configure Player Settings")]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Gotchi";
            PlayerSettings.productName = "Gotchi";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.gotchi.pet");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.gotchi.pet");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 594;
            PlayerSettings.defaultScreenHeight = 1056;
            // Without this the desktop player pauses while unfocused — the screenshot hook would never run.
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("[Gotchi] Player settings configured.");
        }

        private static void Build(BuildTarget target, string location)
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(Directory.GetCurrentDirectory(), location),
                target = target,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"[Gotchi] {target} build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
            Debug.Log($"[Gotchi] {target} build succeeded -> {options.locationPathName} ({report.summary.totalSize / 1048576} MB)");
        }

        [MenuItem("Gotchi/Build Mac")]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "Builds/Mac/Gotchi.app");

        [MenuItem("Gotchi/Build iOS (Xcode project)")]
        public static void BuildIOS() => Build(BuildTarget.iOS, "Builds/iOS");

        private class FakeClock : GameClock
        {
            public DateTime Now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            public override DateTime UtcNow => Now;
            public override DateTime LocalNow => Now.ToLocalTime();
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("[Gotchi smoke test] FAILED: " + message);
            Debug.Log("[Gotchi smoke test] ok: " + message);
        }

        [MenuItem("Gotchi/Run Logic Smoke Test")]
        public static void RunLogicSmokeTest()
        {
            var clock = new FakeClock();
            var data = PetSaveData.CreateNew(SpeciesType.Seal, "Test");
            var automation = new AutomationSystem(data);
            var skills = new SkillTreeSystem(data);
            var wallet = new CurrencyWallet(data);
            var boosts = new BoostSystem(data, clock);
            var homeBattle = new BattleSystem(data, wallet, new LevelSystem(data), clock);
            var homeCamp = new CampSystem(data, homeBattle, automation, clock);
            homeCamp.CooldownScale = () => boosts.CooldownScale;
            homeBattle.Buffs = homeCamp;
            var shop = new ShopService(data, wallet, homeBattle, boosts, new MockPurchaseService());
            var scheduler = new NotificationScheduler(new LogNotificationChannel(), clock);

            // The faces the renderer can draw. The emotion SYSTEM is gone (2026-09-21: nothing picks a mood from the
            // cat's state any more); the 45 named faces stay as the animation vocabulary, so the catalog must stay whole.
            int total = 0;
            foreach (EmotionCategory category in Enum.GetValues(typeof(EmotionCategory)))
            {
                var list = EmotionCatalog.EmotionsIn(category);
                Check(list.Count > 0, $"category {category} has emotions");
                foreach (var emotion in list)
                {
                    Check(EmotionCatalog.GetCategory(emotion) == category, $"{emotion} maps back to {category}");
                    Check(EmotionCatalog.GetDescription(emotion) != emotion.ToString(), $"{emotion} has an expression description");
                }
                total += list.Count;
            }
            Check(total == 45 && Enum.GetValues(typeof(EmotionType)).Length == 45, "45 named faces");

            // Skill tree: lock-in and single-branch evolution
            skills.AddXp(SkillBranch.Sport, 900);   // a branch whose game was removed: its XP no longer leads
            Check(!skills.IsLocked && skills.DominantBranch != SkillBranch.Sport, "XP in a removed branch neither leads nor locks");
            skills.AddXp(SkillBranch.PvP, 499);
            Check(!skills.IsLocked, "not locked below threshold");
            skills.AddXp(SkillBranch.PvP, 1);
            Check(skills.IsLocked && skills.LockedBranch == SkillBranch.PvP, "locks into the battle branch at 500 XP");
            Check(skills.EvolutionStage == 2, "stage 2 at 500 XP");
            skills.AddXp(SkillBranch.Hunter, 5000);
            Check(skills.EvolutionBranch == SkillBranch.PvP && skills.EvolutionStage == 2, "other branches do not change a locked evolution");
            skills.AddXp(SkillBranch.PvP, 5000);
            Check(skills.EvolutionStage == SkillTreeSystem.MaxStage, "stage caps at max");
            Check(SkillBranches.Active.Length == 1 && MiniGames.MiniGameRegistry.All.Length == 1 && MiniGames.MiniGameRegistry.For(SkillBranch.PvP) != null, "the battle is the one game and the one branch");

            // Economy
            Check(wallet.Get(CurrencyType.Soft) == 100, "starts with 100 coins");
            bool ok = false;
            homeBattle.SetVitals(10, 10);
            shop.Buy(ShopCatalog.Find("treat_box"), r => ok = r.Success);
            Check(ok && wallet.Get(CurrencyType.Soft) == 60, "Full Recovery costs 40 coins");
            Check(homeBattle.Hp == homeBattle.MaxHp && homeBattle.Mp == homeBattle.MaxMp, "Full Recovery fills health and mana");
            shop.Buy(ShopCatalog.Find("scarf_star"), r => ok = r.Success);
            Check(!ok, "cannot afford 250-coin scarf");
            shop.Buy(ShopCatalog.Find("gems_small"), r => ok = r.Success);
            Check(ok && wallet.Get(CurrencyType.Premium) == 50, "mock real-money purchase grants hearts");
            shop.Buy(ShopCatalog.Find("coins_pack"), r => ok = r.Success);
            Check(ok && wallet.Get(CurrencyType.Premium) == 30 && wallet.Get(CurrencyType.Soft) == 560, "hearts buy coins");
            shop.Buy(ShopCatalog.Find("scarf_star"), r => ok = r.Success);
            Check(ok && shop.Owns(ShopCatalog.Find("scarf_star")) && data.equippedCosmeticId == "scarf_star", "cosmetic owned and worn after purchase");
            shop.Buy(ShopCatalog.Find("scarf_star"), r => ok = r.Success);
            Check(!ok, "cannot buy an owned cosmetic twice");
            shop.ToggleEquip(ShopCatalog.Find("scarf_star"));
            Check(data.equippedCosmeticId == "", "toggle unequips");
            shop.Buy(ShopCatalog.Find("zoomies"), r => ok = r.Success);
            Check(ok && wallet.Get(CurrencyType.Premium) == 15 && boosts.CooldownBoostActive, "zoomies activates the cooldown boost");
            homeBattle.SetVitals(0, 0);
            Check(homeCamp.TryPerform(CampAction.Rest) && homeCamp.TryPerform(CampAction.Rest) && homeCamp.TryTreat() && homeCamp.TryTreat(), "no waits at camp while No Cooldowns is active");
            clock.Now = clock.Now.AddHours(2);
            Check(!boosts.CooldownBoostActive, "boost expires");
            shop.Buy(ShopCatalog.Find("streak_shield"), r => ok = r.Success);
            Check(ok && boosts.StreakShields == 1 && boosts.ConsumeStreakShield() && boosts.StreakShields == 0, "streak shield bought and consumed");
            shop.Buy(ShopCatalog.Find("rug_mint"), r => ok = r.Success);
            Check(ok && data.rugId == "rug_mint" && shop.IsEquipped(ShopCatalog.Find("rug_mint")), "rug bought and applied");
            Check(shop.CurrentBackground == ShopService.DefaultBackgroundId && shop.OwnsBackground(ShopService.DefaultBackgroundId), "default background is owned from the start");
            shop.SetBackground("bg_night");
            Check(shop.CurrentBackground == ShopService.DefaultBackgroundId, "cannot switch to a background you do not own");
            wallet.Add(CurrencyType.Soft, 300);
            shop.Buy(ShopCatalog.Find("bg_meadow"), r => ok = r.Success);
            Check(ok && shop.CurrentBackground == "bg_meadow" && shop.IsEquipped(ShopCatalog.Find("bg_meadow")), "background bought and applied");
            shop.SetBackground(ShopService.DefaultBackgroundId);
            Check(shop.CurrentBackground == ShopService.DefaultBackgroundId && shop.Owns(ShopCatalog.Find("bg_meadow")), "switching back to the default keeps the bought set");

            // Automation
            var corner = AutomationSystem.Catalog[3];
            Check(Array.TrueForAll(AutomationSystem.Catalog, h => SkillBranches.IsActive(h.RequiredBranch) && CampSystem.Def(h.Action).HelperId == h.Id), "every helper asks for battle XP and belongs to one camp action");
            Check(!homeCamp.Helped(CampAction.Focus) && Mathf.Approximately(homeCamp.RecoveryShare(CampAction.Focus), 0.4f), "FOCUS gives back 40% on its own");
            Check(automation.TryUnlock(corner, skills, wallet), "the Quiet Corner unlocks with battle XP and coins");
            Check(homeCamp.Helped(CampAction.Focus) && Mathf.Approximately(homeCamp.RecoveryShare(CampAction.Focus), CampSystem.HelperRecovery), "with the Quiet Corner FOCUS gives back 60%");
            Check(!automation.TryUnlock(corner, skills, wallet), "cannot unlock twice");

            // Save round-trip
            Check(homeCamp.TryPerform(CampAction.Feed), "the home cat is fed before the save");
            string json = JsonUtility.ToJson(data);
            var restored = JsonUtility.FromJson<PetSaveData>(json);
            Check(restored.camp.fedBattles == 3 && restored.GetXp(SkillBranch.PvP) == data.GetXp(SkillBranch.PvP), "JSON round-trip keeps the camp buff and XP");
            Check(restored.ownedCosmeticIds.Contains("scarf_star") && restored.unlockedAutomationIds.Contains("auto_toybox"), "JSON round-trip keeps unlocks");
            Check(restored.evolutionBranchLocked && restored.lockedEvolutionBranch == SkillBranch.PvP, "JSON round-trip keeps evolution lock");

            // A save locked into a removed branch is set free when it loads
            var oldSave = PetSaveData.CreateNew(SpeciesType.Cat, "Old");
            oldSave.evolutionBranchLocked = true; oldSave.lockedEvolutionBranch = SkillBranch.Nature; oldSave.SetXp(SkillBranch.Nature, 700); oldSave.SetXp(SkillBranch.PvP, 260); oldSave.evolutionStage = 2;
            var oldSkills = new SkillTreeSystem(oldSave);
            Check(!oldSkills.IsLocked && oldSkills.EvolutionBranch == SkillBranch.PvP && oldSkills.EvolutionStage == 1, "a lock on a removed branch is lifted and the stage follows the active leader");

            // ---- Battle Club (13-pvp-design.md)
            var bData = PetSaveData.CreateNew(SpeciesType.Cat, "Fighter");
            var bWallet = new CurrencyWallet(bData);
            var bLevel = new LevelSystem(bData);
            var battle = new BattleSystem(bData, bWallet, bLevel, clock);
            Check(!battle.HasStyle && battle.Loadout.Count == 0, "a new pet has no style and no moves");
            Check(BattleSystem.Effectiveness(BattleStyle.Claw, BattleStyle.Trick) == 2f && BattleSystem.Effectiveness(BattleStyle.Trick, BattleStyle.Fluff) == 2f && BattleSystem.Effectiveness(BattleStyle.Fluff, BattleStyle.Claw) == 2f, "Claw > Trick > Fluff > Claw");
            Check(BattleSystem.Effectiveness(BattleStyle.Trick, BattleStyle.Claw) == 0.5f && BattleSystem.Effectiveness(BattleStyle.Normal, BattleStyle.Claw) == 1f && BattleSystem.Effectiveness(BattleStyle.Claw, BattleStyle.Claw) == 1f, "reverse is weak, Normal and mirror are neutral");
            Check(battle.ChooseStyle(BattleStyle.Claw, out _) && battle.Style == BattleStyle.Claw && bWallet.Get(CurrencyType.Soft) == 100, "the first style is free");
            Check(battle.Loadout.Count == 4 && battle.InLoadout("claw_swipe") && battle.Count("treat") == 3, "the starter kit: four moves and three treats");
            Check(!battle.ChooseStyle(BattleStyle.Fluff, out _), "changing style needs 150 coins");
            bWallet.Add(CurrencyType.Soft, 2000);
            Check(battle.ChooseStyle(BattleStyle.Fluff, out _) && bWallet.Get(CurrencyType.Soft) == 1950 && battle.InLoadout("fluff_bump") && !battle.InLoadout("claw_swipe") && battle.Owns("claw_swipe"), "a paid style change swaps the basic move and keeps the old one");
            battle.ChooseStyle(BattleStyle.Claw, out _);

            var baseStats = BattleSystem.BaseStats(1);
            Check(battle.TrainedStats().Attack == Mathf.RoundToInt(baseStats.Attack * 1.10f), "Claw adds 10% attack");
            Check(BattleSystem.TrainCostForRank(0) == 40 && BattleSystem.TrainCostForRank(1) == 55 && BattleSystem.TrainCostForRank(9) == 825, "training costs 40, 55 ... 825");
            Check(battle.RankCap == 2 && battle.TryTrain(BattleStat.Attack) && battle.TryTrain(BattleStat.Attack) && !battle.TryTrain(BattleStat.Attack), "level 1 caps training at rank 2");
            Check(battle.TrainedStats().Attack == Mathf.RoundToInt(baseStats.Attack * 1.20f), "two ranks add 10% attack");
            bLevel.AddXp(LevelSystem.XpRequiredForLevel(3));
            Check(battle.RankCap == 4 && battle.TryTrain(BattleStat.Attack), "levelling the pet raises the cap");

            // Health and mana are counted in the thousands, so that a bar block of 250 points means something.
            Check(BattleSystem.BaseStats(1).MaxHp == 1100 && BattleSystem.BaseStats(1).MaxMp == 1100 && BattleSystem.BaseStats(2).MaxHp == 1200, "health and mana: 1000 plus 100 a level");
            float basicHit = BattleSystem.BaseDamage(1, 40, BattleSystem.BaseStats(1).Attack, BattleSystem.BaseStats(1).Defense);
            Check(basicHit > 200f && basicHit < 300f, "a basic hit between two level-1 cats takes about one 250-point block");
            Check(battle.Conditions().Count == 0 && battle.CurrentStats().Attack == battle.TrainedStats().Attack, "with no camp attached the cat fights on its training alone");

            var pricey = BattleSystem.FindMove("fury_claws");
            Check(!battle.CanLearn(pricey, out _), "Silver moves are locked in Bronze");
            Check(battle.TryLearn(BattleSystem.FindMove("quick_paw")) && battle.Owns("quick_paw") && !battle.InLoadout("quick_paw"), "a learned move waits outside a full loadout");
            Check(battle.ToggleLoadout("hiss") && battle.ToggleLoadout("quick_paw") && battle.InLoadout("quick_paw") && battle.Loadout.Count == 4, "loadout swap");
            Check(battle.TryBuyItem(BattleSystem.FindItem("tuna")) && battle.Count("tuna") == 1 && battle.Consume("tuna") && battle.Count("tuna") == 0 && !battle.Consume("tuna"), "items are bought and used up");
            Check(battle.TryBuyCharm(BattleSystem.FindCharm("lucky")) && battle.EquippedCharm == "lucky" && !battle.TryBuyCharm(BattleSystem.FindCharm("fishbone")), "charms: coins buy some, reward charms are not for sale");
            Check(!battle.TryBuyArena(BattleSystem.FindArena("arena_beach")), "a hearts arena needs hearts");

            var rivalA = battle.NextRival(); var rivalB = battle.NextRival();
            Check(rivalA.Style == rivalB.Style && rivalA.Level == rivalB.Level && rivalA.MoveIds.Count == 4, "the next rival is fixed until it is fought");

            int coinsBefore = bWallet.Get(CurrencyType.Soft);
            var win = battle.Finish(new BattleReport { Won = true, HpLeft = 30, MaxHp = 40, SuperEffectiveHits = 3, StyleMovesUsed = 6 });
            Check(win.RatingDelta == 25 && battle.Rating == 25 && battle.Wins == 1 && battle.WinStreak == 1, "a win is worth 25 rating");
            Check(win.Coins == Mathf.RoundToInt(25f * 1.25f) + BattleSystem.FirstWinBonus, "win coins: 25, +25% Lucky Coin, +50 first win of the day");
            Check(bWallet.Get(CurrencyType.Soft) > coinsBefore, "finished daily quests pay at once");
            var second = battle.Finish(new BattleReport { Won = true, HpLeft = 10, MaxHp = 40 });
            Check(second.RatingDelta == 30 && second.Coins == Mathf.RoundToInt(25f * 1.1f * 1.25f), "a streak adds rating and 10% coins; the first-win bonus is paid once");
            var loss = battle.Finish(new BattleReport { Won = false });
            Check(loss.RatingDelta == -15 && battle.WinStreak == 0 && battle.Losses == 1, "a loss costs 15 rating and the streak");
            for (int i = 0; i < 8; i++) battle.Finish(new BattleReport { Won = false });
            Check(battle.Rating == 0, "rating never drops below the league floor");

            int heartsBefore = bWallet.Get(CurrencyType.Premium);
            BattleRewards promoted = null;
            for (int i = 0; i < 12 && promoted == null; i++) { var r = battle.Finish(new BattleReport { Won = true, HpLeft = 5, MaxHp = 40 }); if (r.PromotedTo != null) promoted = r; }
            Check(promoted != null && promoted.PromotedTo.Id == "silver" && battle.LeagueIndex == 1, "200 rating promotes to Silver");
            Check(bWallet.Get(CurrencyType.Premium) >= heartsBefore + 5 && battle.OwnsCharm("fishbone"), "promotion pays hearts and the Leftover Fishbone");
            Check(battle.CanLearn(pricey, out _), "Silver unlocks its moves");
            for (int i = 0; i < 30; i++) battle.Finish(new BattleReport { Won = false });
            Check(battle.Rating == 200 && battle.LeagueIndex == 1, "a league, once reached, is never lost");

            clock.Now = clock.Now.AddDays(1);
            Check(battle.FirstWinAvailable && battle.TodayQuests().Count == BattleSystem.QuestsPerDay && battle.TodayQuests().TrueForAll(q => !q.Done), "a new day brings the first-win bonus and three fresh quests");

            var bRestored = JsonUtility.FromJson<PetSaveData>(JsonUtility.ToJson(bData));
            Check(bRestored.battle.style == "Claw" && bRestored.battle.rating == 200 && bRestored.battle.ownedCharms.Contains("fishbone") && bRestored.battle.loadout.Count == 4 && bRestored.battle.quests.Count == 3, "the battle save survives a JSON round-trip");
            // ---- Health and mana: what a fight leaves, and how it comes back
            Check(BattleSystem.FindMove("scratch").Mana == 0 && Array.TrueForAll(BattleSystem.Moves, m => m.Id == "scratch" || (m.Mana > 0 && m.Mana <= BattleSystem.BaseStats(1).MaxMp / 3)), "SCRATCH is free, every other move costs mana, none more than a third of a level-1 pool");
            var fresh = new BattleSystem(PetSaveData.CreateNew(SpeciesType.Cat, "Fresh"), bWallet, bLevel, clock);
            Check(fresh.Hp == fresh.MaxHp && fresh.Mp == fresh.MaxMp && fresh.CanFight, "a new cat starts with full health and mana");
            battle.SetVitals(battle.MaxHp, battle.MaxMp);
            var spent = battle.Finish(new BattleReport { Won = true, HpLeft = 5, MpLeft = 3, MaxHp = battle.MaxHp });
            Check(battle.Hp == 5 && battle.Mp == 3, "a fight leaves health and mana where it ended");
            clock.Now = clock.Now.AddSeconds(60);
            int hpAfterMinute = battle.Hp;
            Check(hpAfterMinute > 5 && hpAfterMinute < battle.MaxHp && battle.Mp > 3, "a quiet minute brings a tenth back");
            battle.Recover(0.4f, 0f);
            Check(battle.Hp >= hpAfterMinute + (int)(battle.MaxHp * 0.39f) && battle.Hp < battle.MaxHp && battle.Mp < battle.MaxMp / 2, "health can come back without the mana");
            int treatsForHome = battle.Count("treat");
            battle.Give("treat", 1);
            Check(battle.TryUseAtHome(BattleSystem.FindItem("treat"), out _) && battle.Count("treat") == treatsForHome, "a Fish Treat heals at home too");
            Check(!battle.TryUseAtHome(BattleSystem.FindItem("catnip"), out _), "catnip is for fights only");
            battle.SetVitals(battle.MaxHp / 10 - 1, 0);
            Check(!battle.CanFight, "under a tenth of its health the cat is worn out");
            battle.SetVitals(battle.MaxHp / 10 + 1, 0);
            Check(battle.CanFight, "a tenth of its health is enough to fight");
            battle.SetVitals(0, 0);
            Check(!battle.CanFight && Math.Abs(battle.SecondsUntilFull() - BattleSystem.FullRegenSeconds) < 1d, "no health, no fight; ten minutes until rested");
            scheduler.ScheduleReadyReminder(battle.SecondsUntilFull(), "Fighter");
            clock.Now = clock.Now.AddMinutes(10);
            Check(battle.Hp == battle.MaxHp && battle.Mp == battle.MaxMp && battle.CanFight && battle.SecondsUntilFull() < 1d, "ten minutes refill both bars, the app open or not");
            Check(!battle.TryUseAtHome(BattleSystem.FindItem("treat"), out _), "nothing to heal at full health");
            var vRestored = JsonUtility.FromJson<PetSaveData>(JsonUtility.ToJson(bData));
            Check(vRestored.battle.vitalsStarted && Mathf.Approximately(vRestored.battle.hp, bData.battle.hp) && vRestored.battle.vitalsUtcTicks == bData.battle.vitalsUtcTicks, "health and mana survive a JSON round-trip");

            // ---- The camp: what is done for the cat between fights (it replaced the four needs on 2026-09-21)
            var cData = PetSaveData.CreateNew(SpeciesType.Cat, "Camper");
            var cWallet = new CurrencyWallet(cData);
            var cSkills = new SkillTreeSystem(cData);
            var cHelpers = new AutomationSystem(cData);
            var cBattle = new BattleSystem(cData, cWallet, new LevelSystem(cData), clock);
            var camp = new CampSystem(cData, cBattle, cHelpers, clock);
            cBattle.Buffs = camp;
            Check(cBattle.Leaning.Name == "Rookie", "a cat with no style leans to nothing yet: Rookie");
            cBattle.ChooseStyle(BattleStyle.Claw, out _);
            Check(CampSystem.Actions.Length == 4 && cBattle.Conditions().Count == 0 && cBattle.CurrentStats().Attack == cBattle.TrainedStats().Attack, "four camp actions; with none of them done the cat fights on its training alone");
            Check(!camp.WouldHelp(CampAction.Rest) && !camp.TryPerform(CampAction.Rest) && !camp.TryPerform(CampAction.Focus), "full bars have nothing to rest or focus for");
            cBattle.SetVitals(100, 50);
            Check(camp.TryPerform(CampAction.Rest) && cBattle.Hp == 100 + Mathf.RoundToInt(cBattle.MaxHp * 0.4f) && cBattle.Mp == 50, "REST gives back 40% of the health and no mana");
            Check(!camp.TryPerform(CampAction.Rest) && camp.RemainingCooldown(CampAction.Rest) > 0f, "REST then waits out its cooldown");
            Check(camp.TryPerform(CampAction.Focus) && cBattle.Mp == 50 + Mathf.RoundToInt(cBattle.MaxMp * 0.4f), "FOCUS has its own cooldown and gives back 40% of the mana");
            clock.Now = clock.Now.AddSeconds(CampSystem.CooldownSeconds - 1);
            Check(!camp.TryPerform(CampAction.Rest), "still waiting a second before the cooldown ends");
            clock.Now = clock.Now.AddSeconds(2);
            int hpBeforeRest = cBattle.Hp;
            Check(camp.TryPerform(CampAction.Rest) && cBattle.Hp > hpBeforeRest, "REST again once the cooldown is over");

            int plainAttack = cBattle.CurrentStats().Attack, plainDefense = cBattle.CurrentStats().Defense;
            Check(camp.TryPerform(CampAction.Feed) && camp.Charges(CampAction.Feed) == 3 && Mathf.Approximately(camp.Multiplier(BattleStat.Attack), 1.1f) && cBattle.CurrentStats().Attack > plainAttack && cBattle.CurrentStats().Defense == plainDefense, "FEED: attack +10% for the next three battles");
            Check(camp.TryPerform(CampAction.Groom) && camp.Charges(CampAction.Groom) == 3 && cBattle.CurrentStats().Defense > plainDefense && cBattle.Conditions().Count == 2 && cBattle.Conditions().TrueForAll(c => c.Good), "GROOM: defense +10% for three battles; both show as conditions");
            Check(cBattle.TrainedStats().Attack == plainAttack, "the permanent numbers on the Train page leave the camp out");
            clock.Now = clock.Now.AddSeconds(CampSystem.CooldownSeconds + 1);
            Check(!camp.WouldHelp(CampAction.Feed) && !camp.TryPerform(CampAction.Feed), "a fed cat cannot be fed again until a battle has used a charge");
            cBattle.Finish(new BattleReport { Won = true, HpLeft = 600, MpLeft = 600, MaxHp = cBattle.MaxHp });
            Check(camp.Charges(CampAction.Feed) == 2 && camp.Charges(CampAction.Groom) == 2, "a fought battle uses one charge of each buff");
            cBattle.Finish(new BattleReport { Ran = true, HpLeft = 600, MpLeft = 600, MaxHp = cBattle.MaxHp });
            Check(camp.Charges(CampAction.Feed) == 2, "running from a fight uses none");
            Check(camp.TryPerform(CampAction.Feed) && camp.Charges(CampAction.Feed) == 3, "feeding tops the buff back up to three");
            for (int i = 0; i < 3; i++) cBattle.Finish(new BattleReport { Won = false, HpLeft = 600, MpLeft = 600, MaxHp = cBattle.MaxHp });
            Check(camp.Charges(CampAction.Feed) == 0 && camp.Charges(CampAction.Groom) == 0 && cBattle.CurrentStats().Attack == plainAttack && cBattle.Conditions().Count == 0, "after three battles the buffs are gone");

            cSkills.AddXp(SkillBranch.PvP, 400);
            cWallet.Add(CurrencyType.Soft, 5000);
            Check(cHelpers.TryUnlock(AutomationSystem.Catalog[0], cSkills, cWallet) && cHelpers.TryUnlock(AutomationSystem.Catalog[2], cSkills, cWallet), "helpers are bought with coins once the battle XP is there");
            clock.Now = clock.Now.AddSeconds(CampSystem.CooldownSeconds + 1);
            Check(camp.BuffLength(CampAction.Feed) == CampSystem.HelperBuffBattles && camp.BuffLength(CampAction.Groom) == 3 && camp.TryPerform(CampAction.Feed) && camp.Charges(CampAction.Feed) == 5, "the Snack Dispenser makes FEED last five battles");
            cBattle.SetVitals(0, 0);
            Check(camp.TryPerform(CampAction.Rest) && cBattle.Hp == Mathf.RoundToInt(cBattle.MaxHp * CampSystem.HelperRecovery), "the Cozy Nest makes REST give back 60%");
            cBattle.SetVitals(100, 100);
            Check(camp.TryTreat() && cBattle.Hp == 100 + Mathf.RoundToInt(cBattle.MaxHp * CampSystem.TreatRecovery) && cBattle.Mp == 100 + Mathf.RoundToInt(cBattle.MaxMp * CampSystem.TreatRecovery), "a Treat gives back 15% of both bars");
            Check(!camp.TryTreat() && camp.TreatCooldownRemaining > 0f, "the treat has a cooldown");
            var campRestored = JsonUtility.FromJson<PetSaveData>(JsonUtility.ToJson(cData));
            Check(campRestored.camp.fedBattles == 5 && campRestored.camp.groomedBattles == 0, "the camp buffs survive a JSON round-trip");

            // ---- Leaning: the style and the most trained stat name the cat (it took the mood's place on the status block)
            Check(cBattle.Leaning.Name == "Fighter" && cBattle.Leaning.Style == BattleStyle.Claw, "an untrained Claw cat leans to its own stat: Fighter");
            Check(cBattle.TryTrain(BattleStat.Hp) && cBattle.Leaning.Name == "Brawler", "health trained most: Brawler");
            Check(cBattle.TryTrain(BattleStat.Attack) && cBattle.Leaning.Name == "Fighter", "a tie goes to the style's own stat");
            var tData = PetSaveData.CreateNew(SpeciesType.Cat, "Sneak");
            var tWallet = new CurrencyWallet(tData);
            tWallet.Add(CurrencyType.Soft, 5000);
            var tLevel = new LevelSystem(tData);
            var tBattle = new BattleSystem(tData, tWallet, tLevel, clock);
            tBattle.ChooseStyle(BattleStyle.Trick, out _);
            Check(tBattle.Leaning.Name == "Shadow", "a Trick cat leans to speed: Shadow");
            Check(tBattle.TryTrain(BattleStat.Defense) && tBattle.Leaning.Name == "Trickster", "defense trained most on a Trick cat: Trickster");
            tLevel.AddXp(LevelSystem.XpRequiredForLevel(3));
            foreach (BattleStat stat in Enum.GetValues(typeof(BattleStat)))
                for (int i = 0; i < 3 && tBattle.Rank(stat) < 3; i++) tBattle.TryTrain(stat);
            Check(tBattle.Leaning.Name == "All-rounder", "every stat trained evenly: All-rounder");
            var leanings = new System.Collections.Generic.HashSet<string>();
            foreach (BattleStyle style in new[] { BattleStyle.Claw, BattleStyle.Fluff, BattleStyle.Trick })
                foreach (BattleStat stat in Enum.GetValues(typeof(BattleStat)))
                {
                    var lData = PetSaveData.CreateNew(SpeciesType.Cat, "L");
                    var lBattle = new BattleSystem(lData, new CurrencyWallet(lData), new LevelSystem(lData), clock);
                    lBattle.ChooseStyle(style, out _);
                    lBattle.TryTrain(stat);
                    leanings.Add(lBattle.Leaning.Name);
                }
            Check(leanings.Count == 12 && !leanings.Contains("Rookie") && !leanings.Contains("All-rounder"), "three styles and four stats give twelve different leanings");

            // ---- The block bars: one block per 250 points, the bar's width fixed
            Check(SegmentedBar.UnitsPerBlock == 250f && SegmentedBar.BlockCount(1200f) == 5 && Mathf.Approximately(SegmentedBar.BlockShare(1200f, 0), 1f) && Mathf.Approximately(SegmentedBar.BlockShare(1200f, 4), 0.8f), "1200 health is four full blocks and a fifth that is 4/5 as wide");
            Check(SegmentedBar.BlockFill(1000f, 1200f, 3) == 1f && SegmentedBar.BlockFill(1000f, 1200f, 4) == 0f && Mathf.Approximately(SegmentedBar.BlockFill(1100f, 1200f, 4), 0.5f), "1000 of 1200 fills four blocks and leaves the last one outlined");
            Check(SegmentedBar.BlockCount(1000f) == 4 && SegmentedBar.BlockCount(1001f) == 5, "a block is added only when the maximum needs it");

            // ---- Market: sell and trade
            int sellCoins = bWallet.Get(CurrencyType.Soft), treatsHad = battle.Count("treat");
            Check(battle.TrySellItem(BattleSystem.FindItem("treat")) && battle.Count("treat") == treatsHad - 1 && bWallet.Get(CurrencyType.Soft) == sellCoins + 15, "the Market buys a Fish Treat back at half price");
            Check(!battle.TrySellItem(BattleSystem.FindItem("tuna")), "nothing to sell, nothing sold");
            Check(!battle.CanSellCharm(BattleSystem.FindCharm("lucky")) && !battle.CanSellCharm(BattleSystem.FindCharm("fishbone")), "a held charm and a promotion reward cannot be sold");
            battle.EquipCharm("fishbone");
            Check(battle.TrySellCharm(BattleSystem.FindCharm("lucky")) && !battle.OwnsCharm("lucky") && bWallet.Get(CurrencyType.Soft) == sellCoins + 15 + 250, "a bought charm sells for half once it is taken off");
            var trades = battle.TodayTrades();
            Check(trades.Count == BattleSystem.TradesPerDay && trades.TrueForAll(t => t.GiveItemId != t.GetItemId && t.GiveCount > 0 && t.GetCount > 0), "three swaps a day, each between two different goods");
            Check(trades.TrueForAll(t => { int given = BattleSystem.FindItem(t.GiveItemId).Cost * t.GiveCount, got = BattleSystem.FindItem(t.GetItemId).Cost * t.GetCount; return given <= got && got <= given * 1.35f; }), "a swap never asks for more value than it gives");
            var swap = trades[0];
            battle.Give(swap.GiveItemId, BattleSystem.MaxCarry);
            while (battle.Count(swap.GetItemId) > 0) battle.Consume(swap.GetItemId);
            int gaveBefore = battle.Count(swap.GiveItemId);
            Check(battle.TryTrade(swap) && battle.Count(swap.GiveItemId) == gaveBefore - swap.GiveCount && battle.Count(swap.GetItemId) == swap.GetCount, "a trade moves both goods");
            Check(battle.TradeDone(swap) && !battle.TryTrade(swap), "each swap can be taken once a day");

            // ---- The Wild (campaign)
            var campaign = new CampaignSystem(bData, battle, clock);
            var garden = CampaignSystem.Areas[0]; var alley = CampaignSystem.Areas[1];
            Check(campaign.IsUnlocked(garden) && !campaign.IsUnlocked(alley) && !campaign.Begin(alley, out _), "only the first area is open at the start");
            foreach (var area in CampaignSystem.Areas)
                Check(area.Steps.Length >= 6 && area.Steps[area.Steps.Length - 1].Kind == WildStepKind.Boss && area.MinLevel <= area.MaxLevel && area.MaxLevel < area.BossLevel, $"{area.Name}: a trail that ends in a boss above the wild levels");
            Check(campaign.Begin(garden, out _) && campaign.ExpeditionActive && campaign.Step == 0 && campaign.Hp == campaign.MaxHp, "an expedition starts at full health");
            var wild = campaign.BuildEncounter();
            Check(wild != null && wild.Kind == BattleKind.Wild && wild.Rival.Level >= garden.MinLevel && wild.Rival.Level <= garden.MaxLevel && wild.Rival.MoveIds.Count == 4 && wild.Rival.Stats.MaxMp > 0, "the first step is a wild cat of the area's level");
            Check(campaign.BuildEncounter().Rival.Name == wild.Rival.Name && campaign.BuildEncounter().Rival.Style == wild.Rival.Style, "the same step holds the same wild cat");
            int ratingBeforeWild = battle.Rating, wildCoinsBefore = bWallet.Get(CurrencyType.Soft);
            var hurt = new BattleReport { Won = true, HpLeft = 9, MpLeft = 12, MaxHp = campaign.MaxHp };
            var wildRewards = wild.Finish(hurt);
            Check(wildRewards.Coins == garden.WildCoins && wildRewards.Xp == garden.WildXp && battle.Rating == ratingBeforeWild && campaign.Step == 1 && campaign.WildWins == 1, "a wild win pays the area's coins and XP, moves the trail on and leaves the rating alone");
            Check(campaign.Hp == 9 && battle.Mp == 12 && campaign.BuildEncounter() == null, "health and mana carry over; the next step is a find, not a fight");
            int treatsBefore = battle.Count("treat");
            var found = campaign.Search("Fighter");
            Check(found.Outcome == SearchOutcome.Found && battle.Count("treat") == treatsBefore + 1 && campaign.Step == 2, "a find goes straight into the bag");
            Check(campaign.UseItem(BattleSystem.FindItem("treat"), out _) && campaign.Hp > 9 && battle.Count("treat") == treatsBefore, "a treat heals between fights");
            Check(!campaign.UseItem(BattleSystem.FindItem("catnip"), out _), "catnip only works in a fight");
            var next = campaign.BuildEncounter();
            Check(next != null && battle.Mp == 12 && campaign.Hp < campaign.MaxHp, "the next fight starts with the health and mana that were left");
            var ranAway = next.Finish(new BattleReport { Ran = true, HpLeft = campaign.Hp, MaxHp = campaign.MaxHp });
            Check(ranAway.Coins == 0 && campaign.Step == 3 && campaign.ExpeditionActive, "running leaves the wild cat behind and the trail goes on");
            campaign.GoHome();
            Check(!campaign.ExpeditionActive && bWallet.Get(CurrencyType.Soft) >= wildCoinsBefore, "going home keeps everything");
            campaign.Begin(garden, out _);
            campaign.BuildEncounter().Finish(new BattleReport { Won = true, HpLeft = 20, MaxHp = campaign.MaxHp });
            Check(campaign.Search("Fighter").Outcome == SearchOutcome.PickedClean, "a find comes back once a day, not once a run");
            campaign.BuildEncounter().Finish(new BattleReport { Won = false, HpLeft = 0, MaxHp = campaign.MaxHp });
            Check(!campaign.ExpeditionActive && !campaign.IsCleared(garden), "a knockout ends the expedition");
            Check(!campaign.Begin(garden, out _), "a worn-out cat cannot set out");
            battle.RecoverFully();
            Check(!GameFeatures.Wild, "the Wild is parked: nothing on screen leads to it in this release");
            campaign.Begin(garden, out _);
            int heartsBeforeClear = bWallet.Get(CurrencyType.Premium);
            BattleRewards clear = null;
            for (int guard = 0; guard < 12 && campaign.ExpeditionActive; guard++)
            {
                var step = campaign.NextStep;
                if (step.Kind == WildStepKind.Find) { campaign.Search("Fighter"); continue; }
                var fight = campaign.BuildEncounter();
                bool bossFight = fight.Kind == BattleKind.Boss;
                Check(!bossFight || (fight.Rival.Level == garden.BossLevel && fight.Rival.SmartAi && fight.Rival.Name == garden.BossName), "the last step is the area boss");
                var r = fight.Finish(new BattleReport { Won = true, HpLeft = 600, MaxHp = campaign.MaxHp });
                if (bossFight) clear = r;
            }
            Check(clear != null && campaign.IsCleared(garden) && campaign.IsUnlocked(alley) && !campaign.ExpeditionActive, "beating the boss clears the area and opens the next");
            Check(clear.Coins == garden.ClearCoins && bWallet.Get(CurrencyType.Premium) == heartsBeforeClear + garden.ClearHearts, "a first clear pays coins and hearts");
            campaign.Begin(garden, out _);
            BattleRewards again = null;
            for (int guard = 0; guard < 12 && campaign.ExpeditionActive; guard++)
            {
                if (campaign.NextStep.Kind == WildStepKind.Find) { campaign.Search("Fighter"); continue; }
                again = campaign.BuildEncounter().Finish(new BattleReport { Won = true, HpLeft = 600, MaxHp = campaign.MaxHp });
            }
            Check(again != null && again.Coins == garden.RepeatCoins && again.Hearts == 0, "a repeat clear pays the smaller purse and no hearts");
            var cRestored = JsonUtility.FromJson<PetSaveData>(JsonUtility.ToJson(bData));
            Check(cRestored.campaign.clearedAreas.Contains("garden") && cRestored.campaign.wildWins == campaign.WildWins && cRestored.battle.tradesDone.Count == 1, "the campaign and the trade board survive a JSON round-trip");

            var legacy = JsonUtility.FromJson<PetSaveData>("{\"petName\":\"Legacy\",\"softCurrency\":5,\"needs\":[{\"type\":0,\"value\":12.5}]}");
            var legacyBattle = new BattleSystem(legacy, new CurrencyWallet(legacy), new LevelSystem(legacy), clock);
            Check(legacy.battle != null && legacyBattle.Rating == 0 && legacyBattle.League.Id == "bronze" && !legacyBattle.HasStyle, "a save from before the Battle Club loads into Bronze with no style");
            Check(legacy.camp != null && legacy.camp.fedBattles == 0 && legacyBattle.Hp == legacyBattle.MaxHp, "a save that still lists the old needs loads: they are ignored, the camp starts empty, the bars full");

            // Quiet hours
            var lateNight = new DateTime(2026, 1, 1, 22, 30, 0);
            Check(scheduler.ClampOutsideQuietHours(lateNight) == new DateTime(2026, 1, 2, 9, 0, 0), "22:30 pushed to next 09:00");
            Check(scheduler.ClampOutsideQuietHours(new DateTime(2026, 1, 1, 3, 0, 0)) == new DateTime(2026, 1, 1, 9, 0, 0), "03:00 pushed to 09:00");
            Check(scheduler.ClampOutsideQuietHours(new DateTime(2026, 1, 1, 12, 0, 0)) == new DateTime(2026, 1, 1, 12, 0, 0), "noon untouched");

            Debug.Log("[Gotchi smoke test] ALL CHECKS PASSED");
        }
    }
}
