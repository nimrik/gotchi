using System;
using System.IO;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Economy;
using Gotchi.Systems;
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
            var needs = new NeedsSystem(data, automation);
            var emotions = new EmotionSystem(needs, clock);
            var skills = new SkillTreeSystem(data);
            var care = new CareActionService(needs, emotions, clock);
            var wallet = new CurrencyWallet(data);
            var boosts = new BoostSystem(data, clock);
            care.CooldownScale = () => boosts.CooldownScale;
            var shop = new ShopService(data, wallet, needs, boosts, new MockPurchaseService());
            var scheduler = new NotificationScheduler(new LogNotificationChannel(), clock);

            // Emotion catalog completeness
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
            Check(total == 45 && Enum.GetValues(typeof(EmotionType)).Length == 45, "45 emotion states");

            // Needs decay: hourly and per-frame
            float hunger = needs.Get(NeedType.Hunger);
            needs.Tick(3600f);
            Check(Mathf.Approximately(needs.Get(NeedType.Hunger), hunger - 9f), "hunger decays 9 per hour");
            float before = needs.Get(NeedType.Hunger);
            for (int i = 0; i < 600; i++) needs.Tick(1f / 60f);
            Check(needs.Get(NeedType.Hunger) < before - 0.02f, "per-frame decay accumulates");

            needs.ApplyOfflineElapsed(TimeSpan.FromHours(1000));
            foreach (NeedType need in Enum.GetValues(typeof(NeedType)))
                Check(needs.Get(need) >= 0f && needs.Get(need) <= 100f, $"{need} stays within bounds after long absence");

            // Emotions follow needs
            foreach (NeedType need in Enum.GetValues(typeof(NeedType))) needs.Set(need, 90f);
            emotions.Refresh();
            Check(emotions.Current == EmotionType.Love, "all needs high -> Love");
            needs.Set(NeedType.Hunger, 10f);
            emotions.Refresh();
            Check(emotions.Current == EmotionType.Fury, "critical hunger -> Fury");
            needs.Set(NeedType.Hunger, 30f);
            emotions.Refresh();
            Check(emotions.Current == EmotionType.Irritation, "low hunger -> Irritation");

            // Care: rescue, cooldown, override expiry
            needs.Set(NeedType.Hunger, 10f);
            Check(care.TryPerform(CareAction.Feed), "feed succeeds");
            Check(Mathf.Approximately(needs.Get(NeedType.Hunger), 40f), "feed restores 30");
            Check(emotions.Current == EmotionType.Gratitude, "rescue feed -> Gratitude");
            Check(!care.TryPerform(CareAction.Feed), "feed blocked by cooldown");
            clock.Now = clock.Now.AddSeconds(21);
            Check(care.TryPerform(CareAction.Feed), "feed allowed after cooldown");
            Check(emotions.Current == EmotionType.Satisfaction, "normal feed -> Satisfaction");
            emotions.Refresh();
            Check(emotions.Current == EmotionType.Satisfaction, "override persists before expiry");
            clock.Now = clock.Now.AddSeconds(30);
            emotions.Refresh();
            Check(emotions.Current != EmotionType.Satisfaction, "override expires back to ambient");

            // Skill tree: lock-in and single-branch evolution
            skills.AddXp(SkillBranch.Sport, 499);
            Check(!skills.IsLocked, "not locked below threshold");
            skills.AddXp(SkillBranch.Sport, 1);
            Check(skills.IsLocked && skills.LockedBranch == SkillBranch.Sport, "locks into Sport at 500 XP");
            Check(skills.EvolutionStage == 2, "stage 2 at 500 XP");
            skills.AddXp(SkillBranch.Science, 5000);
            Check(skills.EvolutionBranch == SkillBranch.Sport && skills.EvolutionStage == 2, "other branches do not change a locked evolution");
            skills.AddXp(SkillBranch.Sport, 5000);
            Check(skills.EvolutionStage == SkillTreeSystem.MaxStage, "stage caps at max");

            // Economy
            Check(wallet.Get(CurrencyType.Soft) == 100, "starts with 100 coins");
            bool ok = false;
            shop.Buy(ShopCatalog.Find("treat_box"), r => ok = r.Success);
            Check(ok && wallet.Get(CurrencyType.Soft) == 60, "treat box costs 40 coins");
            Check(Mathf.Approximately(needs.Get(NeedType.Hunger), 100f), "treat box refills needs");
            shop.Buy(ShopCatalog.Find("scarf_star"), r => ok = r.Success);
            Check(!ok, "cannot afford 250-coin scarf");
            shop.Buy(ShopCatalog.Find("gems_small"), r => ok = r.Success);
            Check(ok && wallet.Get(CurrencyType.Premium) == 50, "mock real-money purchase grants gems");
            shop.Buy(ShopCatalog.Find("coins_pack"), r => ok = r.Success);
            Check(ok && wallet.Get(CurrencyType.Premium) == 30 && wallet.Get(CurrencyType.Soft) == 560, "gems buy coins");
            shop.Buy(ShopCatalog.Find("scarf_star"), r => ok = r.Success);
            Check(ok && shop.Owns(ShopCatalog.Find("scarf_star")) && data.equippedCosmeticId == "scarf_star", "cosmetic owned and worn after purchase");
            shop.Buy(ShopCatalog.Find("scarf_star"), r => ok = r.Success);
            Check(!ok, "cannot buy an owned cosmetic twice");
            shop.ToggleEquip(ShopCatalog.Find("scarf_star"));
            Check(data.equippedCosmeticId == "", "toggle unequips");
            shop.Buy(ShopCatalog.Find("zoomies"), r => ok = r.Success);
            Check(ok && wallet.Get(CurrencyType.Premium) == 15 && boosts.CooldownBoostActive, "zoomies activates the cooldown boost");
            Check(care.TryPerform(CareAction.Clean) && care.TryPerform(CareAction.Clean), "no cooldown while zoomies is active");
            clock.Now = clock.Now.AddHours(2);
            Check(!boosts.CooldownBoostActive, "boost expires");
            shop.Buy(ShopCatalog.Find("streak_shield"), r => ok = r.Success);
            Check(ok && boosts.StreakShields == 1 && boosts.ConsumeStreakShield() && boosts.StreakShields == 0, "streak shield bought and consumed");
            shop.Buy(ShopCatalog.Find("rug_mint"), r => ok = r.Success);
            Check(ok && data.rugId == "rug_mint" && shop.IsEquipped(ShopCatalog.Find("rug_mint")), "rug bought and applied");

            // Automation
            var toybox = AutomationSystem.Catalog[3];
            Check(automation.TryUnlock(toybox, skills, wallet), "toy box unlocks with Sport XP and coins");
            Check(Mathf.Approximately(automation.GetDecayMultiplier(NeedType.Happiness), 0.6f), "toy box slows happiness decay");
            Check(!automation.TryUnlock(toybox, skills, wallet), "cannot unlock twice");

            // Save round-trip
            string json = JsonUtility.ToJson(data);
            var restored = JsonUtility.FromJson<PetSaveData>(json);
            Check(restored.needs.Count == 4 && restored.GetXp(SkillBranch.Sport) == data.GetXp(SkillBranch.Sport), "JSON round-trip keeps needs and XP");
            Check(restored.ownedCosmeticIds.Contains("scarf_star") && restored.unlockedAutomationIds.Contains("auto_toybox"), "JSON round-trip keeps unlocks");
            Check(restored.evolutionBranchLocked && restored.lockedEvolutionBranch == SkillBranch.Sport, "JSON round-trip keeps evolution lock");

            // Quiet hours
            var lateNight = new DateTime(2026, 1, 1, 22, 30, 0);
            Check(scheduler.ClampOutsideQuietHours(lateNight) == new DateTime(2026, 1, 2, 9, 0, 0), "22:30 pushed to next 09:00");
            Check(scheduler.ClampOutsideQuietHours(new DateTime(2026, 1, 1, 3, 0, 0)) == new DateTime(2026, 1, 1, 9, 0, 0), "03:00 pushed to 09:00");
            Check(scheduler.ClampOutsideQuietHours(new DateTime(2026, 1, 1, 12, 0, 0)) == new DateTime(2026, 1, 1, 12, 0, 0), "noon untouched");

            Debug.Log("[Gotchi smoke test] ALL CHECKS PASSED");
        }
    }
}
