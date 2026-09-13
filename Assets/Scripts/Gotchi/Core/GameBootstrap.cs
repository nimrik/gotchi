using System;
using System.Collections;
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
        private const float EmotionRefreshInterval = 0.25f;
        private const float MiniGameEnergyCost = 8f;
        private const float MiniGameHappinessGain = 10f;

        [SerializeField] private SpeciesType startingSpecies = SpeciesType.Cat;
        [SerializeField] private string petName = "Mochi";
        [SerializeField] private bool resetSaveOnStart = false;
        [SerializeField] private bool useMockPurchases = true;

        private GameContext _ctx;
        private HUDController _hud;
        private float _autosaveTimer;
        private float _emotionTimer;
        private string _pendingToast;

        private GameClock _clock;
        private ISaveService _saveService;
        private GameObject _onboardingRoot;
        private OnboardingView _onboarding;

        private void Awake()
        {
            GameSettings.Apply();
            _clock = new GameClock();
            _saveService = new LocalJsonSaveService();
            if (resetSaveOnStart) _saveService.Delete();
        }

        private void Start()
        {
            bool fresh = HasCommandLineFlag("-fresh");
            PetSaveData data = fresh ? null : _saveService.Load();
            string screenshotBase = GetCommandLineArg("-screenshot");

            if (data == null && !string.IsNullOrEmpty(screenshotBase) && !fresh)
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
        }

        private PetSaveData CreateFromOnboarding(OnboardingResult result)
        {
            var data = PetSaveData.CreateNew(result.Species, result.PetName);
            data.accountEmail = result.Email;
            data.displayName = string.IsNullOrEmpty(result.DisplayName) ? "Keeper " + data.ReferralCode : result.DisplayName;
            data.ageBand = result.AgeBand;
            data.preferredPlayHour = result.PreferredPlayHour;
            data.SetXp(result.Vibe, 25);
            _saveService.Save(data);
            return data;
        }

        private void BootWith(PetSaveData data)
        {
            var automation = new AutomationSystem(data);
            var needs = new NeedsSystem(data, automation);
            ApplyOfflineProgress(data, needs, _clock);
            ApplyLoginStreak(data, _clock, out int streakReward);

            var emotions = new EmotionSystem(needs, _clock);
            var skills = new SkillTreeSystem(data);
            var wallet = new CurrencyWallet(data);
            var care = new CareActionService(needs, emotions, _clock);
            var boosts = new BoostSystem(data, _clock);
            care.CooldownScale = () => boosts.CooldownScale;
            var level = new LevelSystem(data);
            level.OnLevelUp += _ => wallet.Add(CurrencyType.Premium, 5);
            care.OnActionPerformed += _ => level.AddXp(5);
            care.OnCuddled += () => level.AddXp(15);
            IPurchaseService purchases = useMockPurchases ? new MockPurchaseService() : (IPurchaseService)new StoreKitPurchaseService();
            var shop = new ShopService(data, wallet, needs, boosts, purchases);
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
                Needs = needs,
                Emotions = emotions,
                Skills = skills,
                Care = care,
                Level = level,
                Boosts = boosts,
                Leaderboards = new MockLeaderboardService(() => new PlayerProfile
                {
                    PlayerId = "local",
                    DisplayName = string.IsNullOrEmpty(data.displayName) ? "Keeper " + data.ReferralCode : data.displayName,
                    PetName = data.petName,
                    Species = data.species,
                    Level = level.Level,
                    EvolutionStage = skills.EvolutionStage,
                    EvolutionBranch = skills.EvolutionBranch,
                    BranchXp = new[] { skills.GetXp(SkillBranch.Sport), skills.GetXp(SkillBranch.Social), skills.GetXp(SkillBranch.Warrior), skills.GetXp(SkillBranch.Hunter), skills.GetXp(SkillBranch.Science), skills.GetXp(SkillBranch.Nature), skills.GetXp(SkillBranch.ExplorerAdventure) },
                    StreakDays = data.loginStreakDays,
                }),
                Wallet = wallet,
                Shop = shop,
                Notifications = notifications,
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
            _ctx.Care.TryPerform(CareAction.Feed);
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-home.png");
            yield return new WaitForSeconds(1f);
            _hud.ShowSkills();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-skills.png");
            yield return new WaitForSeconds(1f);
            _hud.ShowShop();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-shop.png");
            yield return new WaitForSeconds(1f);
            _hud.ShowSettings();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-settings.png");
            yield return new WaitForSeconds(1f);
            _hud.ShowLeaderboard();
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(basePath + "-leaderboard.png");
            yield return new WaitForSeconds(1f);
            _hud.ShowMiniGame(SkillBranch.Sport);
            yield return new WaitForSeconds(2.5f);
            ScreenCapture.CaptureScreenshot(basePath + "-minigame.png");
            yield return new WaitForSeconds(20f);
            ScreenCapture.CaptureScreenshot(basePath + "-results.png");
            yield return new WaitForSeconds(1f);
            _hud.ShowMiniGame(SkillBranch.Warrior);
            yield return new WaitForSeconds(2.2f);
            ScreenCapture.CaptureScreenshot(basePath + "-arena.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowMiniGame(SkillBranch.ExplorerAdventure);
            yield return new WaitForSeconds(2.2f);
            ScreenCapture.CaptureScreenshot(basePath + "-trail.png");
            yield return new WaitForSeconds(0.5f);
            _hud.ShowMiniGame(SkillBranch.Nature);
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(basePath + "-bloom.png");
            yield return new WaitForSeconds(1f);
            Application.Quit();
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
            _ctx.Needs.Tick(dt);

            _emotionTimer += dt;
            if (_emotionTimer >= EmotionRefreshInterval)
            {
                _emotionTimer = 0f;
                _ctx.Emotions.Refresh();
            }

            _autosaveTimer += dt;
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
            _ctx.Notifications.ScheduleCareReminders(_ctx.Needs, _ctx.Automation, _ctx.Data.petName);
        }

        private void HandleMiniGameResult(MiniGameResult result)
        {
            float boost = _ctx.Boosts.RewardMultiplier;
            int xp = Mathf.RoundToInt(result.XpReward * boost), coins = Mathf.RoundToInt(result.CoinReward * boost);
            _ctx.Skills.AddXp(result.Branch, xp);
            _ctx.Level.AddXp(Mathf.Max(5, xp / 2));
            _ctx.Wallet.Add(CurrencyType.Soft, coins);
            _ctx.Needs.Add(NeedType.Energy, -MiniGameEnergyCost);
            _ctx.Needs.Add(NeedType.Happiness, MiniGameHappinessGain);

            EmotionType reaction = !result.Won ? EmotionType.Embarrassment
                : result.Score >= 300 ? EmotionType.Amazement
                : EmotionType.Excitement;
            _ctx.Emotions.TriggerEvent(reaction, 10f);
            _ctx.SaveService.Save(_ctx.Data);
        }

        private static void ApplyOfflineProgress(PetSaveData data, NeedsSystem needs, GameClock clock)
        {
            if (data.lastSavedUtcTicks <= 0) return;
            TimeSpan elapsed = clock.UtcNow - new DateTime(data.lastSavedUtcTicks, DateTimeKind.Utc);
            needs.ApplyOfflineElapsed(elapsed);
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
