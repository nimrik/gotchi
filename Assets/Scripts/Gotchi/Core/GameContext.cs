using Gotchi.Data;
using Gotchi.Economy;
using Gotchi.Persistence;
using Gotchi.Systems;

namespace Gotchi.Core
{
    public class GameContext
    {
        public PetSaveData Data;
        public GameClock Clock;
        public ISaveService SaveService;
        public IAuthService Auth;
        public AutomationSystem Automation;
        public NeedsSystem Needs;
        public EmotionSystem Emotions;
        public SkillTreeSystem Skills;
        public CareActionService Care;
        public LevelSystem Level;
        public BoostSystem Boosts;
        public ILeaderboardService Leaderboards;
        public CurrencyWallet Wallet;
        public ShopService Shop;
        public NotificationScheduler Notifications;
        public NewsCenter News;
    }
}
