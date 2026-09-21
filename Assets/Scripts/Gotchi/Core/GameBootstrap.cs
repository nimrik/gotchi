using System;
using System.Collections;
using System.Globalization;
using Gotchi.Data;
using Gotchi.Economy;
using Gotchi.MiniGames;
using Gotchi.Persistence;
using Gotchi.Systems;
using Gotchi.UI;
using UnityEngine;

namespace Gotchi.Core
{
    // The only component that needs to exist in the scene. Setup: empty scene → empty
    // GameObject → add this component → Play. Everything else is built in code.
    public class GameBootstrap : MonoBehaviour
    {
        private const float AutosaveInterval = 30f;

        [SerializeField] private SpeciesType startingSpecies = SpeciesType.Cat;
        [SerializeField] private string petName = "Mochi";
        [SerializeField] private bool resetSaveOnStart = false;
        [SerializeField] private bool useMockPurchases = true;

        private GameContext _ctx;
        private HUDController _hud;
        private float _autosaveTimer;
        private string _pendingToast;

        private GameClock _clock;
        private DevClock _devClock;
        private ISaveService _saveService;
        private GameObject _onboardingRoot;
        private OnboardingView _onboarding;

        private void Awake()
        {
            GameSettings.Apply();
            _devClock = new DevClock();
            if (float.TryParse(GetCommandLineArg("-hour"), NumberStyles.Float, CultureInfo.InvariantCulture, out float hour)) _devClock.Hour = Mathf.Repeat(hour, 24f);
            _clock = _devClock;
            _saveService = HasCommandLineFlag("-tempsave") ? new NullSaveService() : (ISaveService)new LocalJsonSaveService();
            if (resetSaveOnStart) _saveService.Delete();
        }

        private void Start()
        {
            bool fresh = HasCommandLineFlag("-fresh");
            PetSaveData data = fresh ? null : _saveService.Load();
            string screenshotBase = GetCommandLineArg("-screenshot");
            string homeShotBase = GetCommandLineArg("-screenshot-home");
            string battleShotBase = GetCommandLineArg("-screenshot-battle");

            if (data == null && (!string.IsNullOrEmpty(screenshotBase) || !string.IsNullOrEmpty(homeShotBase) || !string.IsNullOrEmpty(battleShotBase) || HasCommandLineFlag("-tempsave")) && !fresh)
                data = PetSaveData.CreateNew(startingSpecies, petName);

            if (data == null)
            {
                var canvas = UIFactory.CreateCanvas("OnboardingCanvas");
                _onboardingRoot = canvas.gameObject;
                _onboarding = new OnboardingView(canvas.transform, new MockAuthService(), result =>
                {
                    Destroy(_onboardingRoot);
                    _onboarding = null;
                    BootWith(CreateFromOnboarding(result));
                }, this);
                if (!string.IsNullOrEmpty(screenshotBase)) StartCoroutine(CaptureOnboardingAndQuit(screenshotBase));
                return;
            }

            BootWith(data);
            if (!string.IsNullOrEmpty(screenshotBase)) StartCoroutine(CaptureScreenshotsAndQuit(screenshotBase));
            else if (!string.IsNullOrEmpty(homeShotBase)) StartCoroutine(CaptureHomeAndQuit(homeShotBase));
            else if (!string.IsNullOrEmpty(battleShotBase)) StartCoroutine(CaptureBattleAndQuit(battleShotBase));
        }

        // Dev/QA: `Gotchi -tempsave` plays a brand-new pet that is never written to disk, so capture runs (which pick
        // a style, spend coins, fight battles) cannot touch the real save.
        private sealed class NullSaveService : ISaveService
        {
            public bool HasSave() => false;
            public PetSaveData Load() => null;
            public void Save(PetSaveData data) { }
            public void Delete() { }
        }

        // Dev/QA hook: `Gotchi -tempsave -screenshot-battle /path/base` captures the pet being poked until it walks
        // off, every page of the Battle Club and the Market, a ranked battle from the first menu to the results, the
        // home screen with the health and mana the fight left, the Treat and REST bringing them back, and (when
        // GameFeatures.Wild is on) an expedition that plays itself. Use it with -tempsave: the run spends coins.
        private IEnumerator CaptureBattleAndQuit(string basePath)
        {
            yield return new WaitForSeconds(2f);
            _ctx.Wallet.Add(CurrencyType.Soft, 900);
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(basePath + "-home.png");
            yield return new WaitForSeconds(0.5f);

            _hud.DebugPoke(9);
            yield return new WaitForSeconds(0.8f);
            ScreenCapture.CaptureScreenshot(basePath + "-poke-annoyed.png");
            yield return new WaitForSeconds(0.4f);
            for (int i = 0; i < 12; i++) _hud.DebugPoke(3);      // well past the limit: it leaves
            yield return new WaitForSeconds(0.9f);
            ScreenCapture.CaptureScreenshot(basePath + "-poke-leaving.png");
            yield return new WaitForSeconds(2.5f);
            ScreenCapture.CaptureScreenshot(basePath + "-poke-away.png");
            yield return new WaitForSeconds(6.5f);
            ScreenCapture.CaptureScreenshot(basePath + "-poke-back.png");
            yield return new WaitForSeconds(0.5f);

            if (GameFeatures.Wild)
            {
                _hud.ShowCampaign();
                yield return new WaitForSeconds(1.2f);
                ScreenCapture.CaptureScreenshot(basePath + "-wild-nostyle.png");
                yield return new WaitForSeconds(0.5f);
            }
            _hud.ShowBattleClub();
            yield return new WaitForSeconds(1.2f);
            ScreenCapture.CaptureScreenshot(basePath + "-club-style.png");
            yield return new WaitForSeconds(0.5f);
            _ctx.Battle.ChooseStyle(BattleStyle.Claw, out _);
            _ctx.Battle.TryTrain(BattleStat.Attack);
            _ctx.Battle.TryLearn(BattleSystem.FindMove("sneak"));
            _ctx.Battle.TryBuyCharm(BattleSystem.FindCharm("spike"));
            string[] pages = { "club", "train", "moves", "bag" };
            for (int i = 0; i < pages.Length; i++)
            {
                _hud.ShowBattleClubPage(i);
                yield return new WaitForSeconds(1.5f);
                ScreenCapture.CaptureScreenshot(basePath + "-club-" + pages[i] + ".png");
                yield return new WaitForSeconds(0.5f);
            }

            _hud.CloseAllPanels();          // the home screen, now with the league line filled in
            yield return new WaitForSeconds(0.8f);
            _hud.ShowBattleInfo();
            yield return new WaitForSeconds(0.9f);
            ScreenCapture.CaptureScreenshot(basePath + "-info-battle.png");
            yield return new WaitForSeconds(0.5f);
            _hud.HideInfo();

            string[] marketPages = { "buy", "sell", "trade" };
            for (int i = 0; i < marketPages.Length; i++)
            {
                _hud.ShowMarket(i);
                yield return new WaitForSeconds(1.5f);
                ScreenCapture.CaptureScreenshot(basePath + "-market-" + marketPages[i] + ".png");
                yield return new WaitForSeconds(0.5f);
            }

            _hud.ShowBattle();
            yield return new WaitForSeconds(7f);
            ScreenCapture.CaptureScreenshot(basePath + "-battle.png");
            yield return new WaitForSeconds(0.5f);
            // Let the battle play itself out and catch a mid-fight frame and the results card.
            BattleMiniGame.AutoPlay = true;
            _hud.ShowBattle();
            yield return new WaitForSeconds(16f);
            ScreenCapture.CaptureScreenshot(basePath + "-battle-mid.png");
            int fought = _ctx.Battle.Wins + _ctx.Battle.Losses;
            for (float waited = 0f; waited < 120f && _ctx.Battle.Wins + _ctx.Battle.Losses == fought; waited += 0.5f) yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(2.5f);
            ScreenCapture.CaptureScreenshot(basePath + "-battle-results.png");
            yield return new WaitForSeconds(0.5f);
            _hud.CloseMiniGame();
            yield return new WaitForSeconds(0.8f);

            // Back home the bars show what the fight left; the Treat and REST bring them back.
            _hud.CloseAllPanels();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-home-after-battle.png");
            yield return new WaitForSeconds(0.5f);
            _hud.DebugTreat();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-home-treat.png");
            yield return new WaitForSeconds(0.5f);
            _hud.DebugCamp(CampAction.Rest);
            yield return new WaitForSeconds(0.6f);
            _hud.DebugCamp(CampAction.Feed);
            yield return new WaitForSeconds(1.2f);
            ScreenCapture.CaptureScreenshot(basePath + "-home-rested.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowBattleClubPage(BattleClubPanelView.ClubPage);
            yield return new WaitForSeconds(1.2f);
            ScreenCapture.CaptureScreenshot(basePath + "-club-after-battle.png");
            yield return new WaitForSeconds(0.5f);
            _hud.CloseAllPanels();

            if (GameFeatures.Wild)
            {
                // The Wild: the map, then an expedition that searches until the trail ends (wins, finds, the boss or a knockout).
                _hud.ShowCampaign();
                yield return new WaitForSeconds(1.2f);
                ScreenCapture.CaptureScreenshot(basePath + "-wild-map.png");
                yield return new WaitForSeconds(0.5f);
                _hud.CampaignPanel.DebugBegin("garden");
                yield return new WaitForSeconds(1.2f);
                ScreenCapture.CaptureScreenshot(basePath + "-wild-trail.png");
                bool battleShot = false;
                for (int guard = 0; guard < 12 && _ctx.Campaign.ExpeditionActive; guard++)
                {
                    int before = _ctx.Campaign.Step, wins = _ctx.Campaign.WildWins;
                    bool fight = _ctx.Campaign.NextStep != null && _ctx.Campaign.NextStep.Kind != WildStepKind.Find;
                    _hud.CampaignPanel.DebugSearch();
                    if (!fight) { yield return new WaitForSeconds(1f); continue; }
                    yield return new WaitForSeconds(8f);
                    if (!battleShot) { battleShot = true; ScreenCapture.CaptureScreenshot(basePath + "-wild-battle.png"); }
                    for (float waited = 0f; waited < 150f && _ctx.Campaign.ExpeditionActive && _ctx.Campaign.Step == before && _ctx.Campaign.WildWins == wins; waited += 0.5f) yield return new WaitForSeconds(0.5f);
                    yield return new WaitForSeconds(2.5f);
                    if (guard == 0) ScreenCapture.CaptureScreenshot(basePath + "-wild-results.png");
                    yield return new WaitForSeconds(0.5f);
                    _hud.CloseMiniGame();
                    yield return new WaitForSeconds(1f);
                    if (guard == 0) { ScreenCapture.CaptureScreenshot(basePath + "-wild-trail-after.png"); yield return new WaitForSeconds(0.5f); }
                }
                _hud.ShowCampaign();
                yield return new WaitForSeconds(1.2f);
                ScreenCapture.CaptureScreenshot(basePath + "-wild-end.png");
                yield return new WaitForSeconds(0.5f);
            }
            BattleMiniGame.AutoPlay = false;
            _hud.ShowLeaderboard();
            yield return new WaitForSeconds(0.2f);
            _hud.ShowLeaderboardSkills();
            yield return new WaitForSeconds(1.2f);
            ScreenCapture.CaptureScreenshot(basePath + "-leaderboard-battle.png");
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        // Dev/QA clock: `Gotchi -hour 21.5` pins the local time of day, which the default room's window follows.
        // The date and UtcNow stay real, so cooldowns, streaks and offline progress are untouched.
        private sealed class DevClock : GameClock
        {
            public float? Hour;
            public override DateTime LocalNow => Hour.HasValue ? DateTime.Today.AddHours(Hour.Value) : DateTime.Now;
        }

        // Dev/QA hook: `Gotchi -screenshot-home /path/base` captures the home screen only — as it is, with each info
        // box open, the skills panel with a branch box, the shop wallet, and the default room at four times of day.
        private IEnumerator CaptureHomeAndQuit(string basePath)
        {
            yield return new WaitForSeconds(2f);
            yield return new WaitForSeconds(2f);
            ScreenCapture.CaptureScreenshot(basePath + "-home.png");
            yield return new WaitForSeconds(0.7f);
            ScreenCapture.CaptureScreenshot(basePath + "-home-b.png");     // a second frame of the loop: eyes and pose
            yield return new WaitForSeconds(0.5f);
            _hud.ShowStatusInfo();
            yield return new WaitForSeconds(0.8f);
            ScreenCapture.CaptureScreenshot(basePath + "-info-status.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowBattleInfo();
            yield return new WaitForSeconds(0.8f);
            ScreenCapture.CaptureScreenshot(basePath + "-info-battle.png");
            yield return new WaitForSeconds(0.5f);
            _hud.HideInfo();
            _hud.ShowShop();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-shop.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowShop();
            foreach (float at in new[] { 6.5f, 13f, 18.5f, 23f })
            {
                _devClock.Hour = at;
                _hud.RefreshLook();
                yield return new WaitForSeconds(1f);
                ScreenCapture.CaptureScreenshot(basePath + "-hour-" + at.ToString("00.0", CultureInfo.InvariantCulture) + ".png");
                yield return new WaitForSeconds(0.5f);
            }
            Application.Quit();
        }

        private PetSaveData CreateFromOnboarding(OnboardingResult result)
        {
            var data = PetSaveData.CreateNew(result.Species, result.PetName);
            data.accountEmail = result.Email;
            data.displayName = string.IsNullOrEmpty(result.DisplayName) ? "Keeper " + data.ReferralCode : result.DisplayName;
            data.ageBand = result.AgeBand;
            data.preferredPlayHour = result.PreferredPlayHour;
            _saveService.Save(data);
            return data;
        }

        private void BootWith(PetSaveData data)
        {
            var automation = new AutomationSystem(data);
            ApplyLoginStreak(data, _clock, out int streakReward);

            var skills = new SkillTreeSystem(data);
            var wallet = new CurrencyWallet(data);
            var boosts = new BoostSystem(data, _clock);
            var level = new LevelSystem(data);
            level.OnLevelUp += _ => wallet.Add(CurrencyType.Premium, 5);
            var battle = new BattleSystem(data, wallet, level, _clock);
            // The camp is what the player does for the cat between fights: rest, focus, feed, groom, the treat. It gives
            // health and mana back and lends buffs to the next battles. Nothing drains while the player is away (the
            // four needs and the emotions that followed them were removed on 2026-09-21); time only ever refills.
            var camp = new CampSystem(data, battle, automation, _clock);
            camp.CooldownScale = () => boosts.CooldownScale;
            camp.OnActionPerformed += _ => level.AddXp(5);
            battle.Buffs = camp;
            var campaign = new CampaignSystem(data, battle, _clock);
            IPurchaseService purchases = useMockPurchases ? new MockPurchaseService() : (IPurchaseService)new StoreKitPurchaseService();
            var shop = new ShopService(data, wallet, battle, boosts, purchases);
            var notifications = new NotificationScheduler(new LogNotificationChannel(), _clock);

            if (streakReward > 0)
            {
                wallet.Add(CurrencyType.Soft, streakReward);
                _pendingToast = $"Day {data.loginStreakDays} streak! +{streakReward} coins";
            }

            _ctx = new GameContext
            {
                Data = data,
                Clock = _clock,
                SaveService = _saveService,
                Auth = new MockAuthService(),
                Automation = automation,
                Skills = skills,
                Battle = battle,
                Campaign = campaign,
                Camp = camp,
                Level = level,
                Boosts = boosts,
                Leaderboards = new MockLeaderboardService(() => new PlayerProfile
                {
                    PlayerId = "local",
                    DisplayName = string.IsNullOrEmpty(data.displayName) ? "Keeper " + data.ReferralCode : data.displayName,
                    PetName = data.petName,
                    Species = data.species,
                    BattleRating = battle.Rating,
                    BattleStyle = battle.Style,
                    Level = level.Level,
                    EvolutionStage = skills.EvolutionStage,
                    EvolutionBranch = skills.EvolutionBranch,
                    BranchXp = new[] { skills.GetXp(SkillBranch.Sport), skills.GetXp(SkillBranch.Social), skills.GetXp(SkillBranch.PvP), skills.GetXp(SkillBranch.Hunter), skills.GetXp(SkillBranch.Science), skills.GetXp(SkillBranch.Nature), skills.GetXp(SkillBranch.ExplorerAdventure) },
                    StreakDays = data.loginStreakDays,
                }),
                Wallet = wallet,
                Shop = shop,
                Notifications = notifications,
            };
            _ctx.News = new NewsCenter(new MockNewsService(), _ctx.Data);
            // Rivals are other keepers' cats near the player's rating (the mock board today, match-making later).
            battle.RivalSource = (rating, seed) =>
            {
                var keeper = _ctx.Leaderboards.RivalNear(rating, seed);
                return keeper == null ? null : new RivalIdentity { OwnerName = keeper.DisplayName, PetName = keeper.PetName, CoatId = keeper.CoatId, Rating = keeper.BattleRating };
            };

            _hud = new GameObject("HUD").AddComponent<HUDController>();
            _hud.Initialize(_ctx, HandleMiniGameResult);
            if (!string.IsNullOrEmpty(_pendingToast)) _hud.ShowToast(_pendingToast);
            _pendingToast = null;
        }

        private IEnumerator CaptureOnboardingAndQuit(string basePath)
        {
            yield return new WaitForSeconds(2f);
            ScreenCapture.CaptureScreenshot(basePath + "-onboard-account.png");
            yield return new WaitForSeconds(1f);
            _onboarding.ShowStep(1);
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(basePath + "-onboard-character.png");
            yield return new WaitForSeconds(1f);
            _onboarding.ShowStep(2);
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-onboard-doorstep.png");
            yield return new WaitForSeconds(0.5f);
            _onboarding.DebugHatch();
            yield return new WaitForSeconds(1.8f);
            ScreenCapture.CaptureScreenshot(basePath + "-onboard-found.png");
            yield return new WaitForSeconds(0.5f);
            _onboarding.ShowStep(3);
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(basePath + "-onboard-questions.png");
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }

        // Dev/QA hook: `Gotchi -screenshot /path/base` captures the main screens and exits.
        private IEnumerator CaptureScreenshotsAndQuit(string basePath)
        {
            yield return new WaitForSeconds(2f);
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-home.png");
            yield return new WaitForSeconds(1f);
            _hud.ShowShop();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-shop.png");
            yield return new WaitForSeconds(1f);
            _hud.ShowShop(ShopCategory.Backgrounds);
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-shop-backgrounds.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowShop();
            yield return new WaitForSeconds(0.5f);
            foreach (string sceneId in new[] { "bg_meadow", "bg_beach", "bg_snow", "bg_night" })
            {
                _ctx.Data.backgroundId = sceneId;
                _hud.RefreshLook();
                yield return new WaitForSeconds(1f);
                ScreenCapture.CaptureScreenshot(basePath + "-home-" + sceneId.Substring(3) + ".png");
                yield return new WaitForSeconds(0.5f);
            }
            _ctx.Data.backgroundId = ShopService.DefaultBackgroundId;
            _hud.RefreshLook();
            yield return new WaitForSeconds(0.5f);
            _hud.ShowSettings();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-settings.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowSettingsPage("Sound");
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-settings-sound.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowSettingsPage("Menu");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowLeaderboard();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-leaderboard.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowLeaderboardSkills();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-leaderboard-skills.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowNews();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-news.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowStory();
            yield return new WaitForSeconds(3f);
            ScreenCapture.CaptureScreenshot(basePath + "-story.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowStory();
            
            yield return new WaitForSeconds(1f);
            Application.Quit();   // battles, the Market and the Wild have their own run: -tempsave -screenshot-battle
        }

        private static string GetCommandLineArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        private static bool HasCommandLineFlag(string name)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
                if (arg == name) return true;
            return false;
        }

        private void Update()
        {
            if (_ctx == null) return;
            float dt = Time.deltaTime;
            _autosaveTimer += dt;   // health and mana refill against the clock (BattleSystem.UpdateVitals), so nothing else ticks here
            if (_autosaveTimer >= AutosaveInterval)
            {
                _autosaveTimer = 0f;
                _ctx.SaveService.Save(_ctx.Data);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveAndScheduleReminders();
        }

        private void OnApplicationQuit() => SaveAndScheduleReminders();

        private void SaveAndScheduleReminders()
        {
            if (_ctx == null) return;
            _ctx.SaveService.Save(_ctx.Data);
            // The one reminder the game sends ("rested and ready"), and only when the player left it switched on.
            if (GameSettings.NotificationsEnabled) _ctx.Notifications.ScheduleReadyReminder(_ctx.Battle.SecondsUntilFull(), _ctx.Data.petName);
            else _ctx.Notifications.CancelAll();
        }

        private void HandleMiniGameResult(MiniGameResult result)
        {
            float boost = _ctx.Boosts.RewardMultiplier;
            int xp = Mathf.RoundToInt(result.XpReward * boost), coins = Mathf.RoundToInt(result.CoinReward * boost);
            _ctx.Skills.AddXp(result.Branch, xp);
            _ctx.Level.AddXp(Mathf.Max(5, xp / 2));
            _ctx.Wallet.Add(CurrencyType.Soft, coins);
            _ctx.SaveService.Save(_ctx.Data);
        }

        private static void ApplyLoginStreak(PetSaveData data, GameClock clock, out int reward)
        {
            reward = 0;
            DateTime today = clock.LocalNow.Date;
            DateTime lastLogin = data.lastLoginUtcTicks > 0
                ? new DateTime(data.lastLoginUtcTicks, DateTimeKind.Utc).ToLocalTime().Date
                : today;

            if (lastLogin == today && data.loginStreakDays > 0) return;

            bool consecutive = lastLogin == today.AddDays(-1);
            if (!consecutive && data.streakShields > 0 && data.loginStreakDays > 0) { data.streakShields--; consecutive = true; }
            data.loginStreakDays = consecutive ? data.loginStreakDays + 1 : 1;
            data.lastLoginUtcTicks = clock.UtcNow.Ticks;
            reward = Math.Min(50, 10 * data.loginStreakDays);
            if (data.loginStreakDays % 7 == 0) data.premiumCurrency += 10;
        }
    }
}
