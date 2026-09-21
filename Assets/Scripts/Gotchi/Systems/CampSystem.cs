using System;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;

namespace Gotchi.Systems
{
    // ---------------------------------------------------------------------------------------------------------
    // The camp: what the player does for the cat between fights. It replaced the four Tamagotchi needs (hunger,
    // hygiene, energy, happiness, draining by the hour) on 2026-09-21, because the game is about battles now and
    // meters that tick down while the app is closed had nothing to do with them. Everything here is about the
    // next fight instead:
    //   REST    gives health back            FEED    "Fed": ATTACK +10% for the next 3 battles
    //   FOCUS   gives mana back              GROOM   "Groomed": DEFENSE +10% for the next 3 battles
    // plus the TREAT (the chip in the status block): a little of both bars. Each action has a cooldown; nothing
    // decays with time, nothing is lost by staying away, and health and mana refill on their own (BattleSystem).
    // A buff is counted in battles, not minutes: every fight that is not run from uses one charge.
    // Helpers (AutomationSystem) make an action better: a longer buff, a bigger rest.
    // ---------------------------------------------------------------------------------------------------------

    public enum CampAction { Rest, Focus, Feed, Groom }

    public class CampActionDef
    {
        public CampAction Action;
        public string Name, Effect;
        public float RecoverHp, RecoverMp;        // share of the maximum given back
        public BattleStat BuffStat;                // for Feed / Groom
        public float BuffPercent;
        public int BuffBattles;
        public string HelperId, HelperEffect;      // the helper that improves this action, and how
    }

    public class CampSystem : IBattleBuffs
    {
        public const float CooldownSeconds = 60f;
        public const float TreatCooldownSeconds = 60f;
        public const float TreatRecovery = 0.15f;
        public const int HelperBuffBattles = 5;
        public const float HelperRecovery = 0.6f;

        public static readonly CampActionDef[] Actions =
        {
            new CampActionDef { Action = CampAction.Rest,  Name = "REST",  Effect = "HP +40%", RecoverHp = 0.4f, HelperId = "auto_bed", HelperEffect = "HP +60%" },
            new CampActionDef { Action = CampAction.Focus, Name = "FOCUS", Effect = "MP +40%", RecoverMp = 0.4f, HelperId = "auto_toybox", HelperEffect = "MP +60%" },
            new CampActionDef { Action = CampAction.Feed,  Name = "FEED",  Effect = "ATK +10%", BuffStat = BattleStat.Attack, BuffPercent = 10f, BuffBattles = 3, HelperId = "auto_feeder", HelperEffect = "5 battles" },
            new CampActionDef { Action = CampAction.Groom, Name = "GROOM", Effect = "DEF +10%", BuffStat = BattleStat.Defense, BuffPercent = 10f, BuffBattles = 3, HelperId = "auto_bath", HelperEffect = "5 battles" },
        };

        public static CampActionDef Def(CampAction action) { foreach (var d in Actions) if (d.Action == action) return d; return Actions[0]; }

        private readonly PetSaveData _data;
        private readonly BattleSystem _battle;
        private readonly AutomationSystem _helpers;
        private readonly GameClock _clock;
        private readonly Dictionary<CampAction, DateTime> _lastUsedUtc = new Dictionary<CampAction, DateTime>();
        private DateTime _lastTreatUtc = DateTime.MinValue;

        public event Action<CampAction> OnActionPerformed;
        public event Action OnTreated;
        public event Action OnChanged;
        public Func<float> CooldownScale = () => 1f;     // the shop's No Cooldowns boost

        private CampSave Save => _data.camp ?? (_data.camp = new CampSave());

        public CampSystem(PetSaveData data, BattleSystem battle, AutomationSystem helpers, GameClock clock)
        {
            _data = data;
            _battle = battle;
            _helpers = helpers;
            _clock = clock;
        }

        // ---------------------------------------------------------------- actions

        public float RemainingCooldown(CampAction action)
        {
            if (!_lastUsedUtc.TryGetValue(action, out DateTime last)) return 0f;
            double remaining = CooldownSeconds * CooldownScale() - (_clock.UtcNow - last).TotalSeconds;
            return remaining <= 0d ? 0f : (float)remaining;
        }

        public bool Helped(CampAction action) => _helpers != null && _helpers.IsUnlocked(Def(action).HelperId);

        public float RecoveryShare(CampAction action)
        {
            var def = Def(action);
            float share = Math.Max(def.RecoverHp, def.RecoverMp);
            return share > 0f && Helped(action) ? HelperRecovery : share;
        }

        public int BuffLength(CampAction action) => Def(action).BuffBattles <= 0 ? 0 : Helped(action) ? HelperBuffBattles : Def(action).BuffBattles;

        // What the action would do right now; "" means it would be wasted (bar already full).
        public bool WouldHelp(CampAction action)
        {
            var def = Def(action);
            if (def.RecoverHp > 0f) return _battle.Hp < _battle.MaxHp;
            if (def.RecoverMp > 0f) return _battle.Mp < _battle.MaxMp;
            return Charges(action) < BuffLength(action);
        }

        public bool TryPerform(CampAction action)
        {
            if (RemainingCooldown(action) > 0f || !WouldHelp(action)) return false;
            var def = Def(action);
            if (def.RecoverHp > 0f) _battle.Recover(RecoveryShare(action), 0f);
            else if (def.RecoverMp > 0f) _battle.Recover(0f, RecoveryShare(action));
            else SetCharges(action, BuffLength(action));
            _lastUsedUtc[action] = _clock.UtcNow;
            OnActionPerformed?.Invoke(action);
            OnChanged?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------- buffs (IBattleBuffs)

        public int Charges(CampAction action) => action == CampAction.Feed ? Save.fedBattles : action == CampAction.Groom ? Save.groomedBattles : 0;

        private void SetCharges(CampAction action, int value)
        {
            if (action == CampAction.Feed) Save.fedBattles = Math.Max(0, value);
            else if (action == CampAction.Groom) Save.groomedBattles = Math.Max(0, value);
        }

        public float Multiplier(BattleStat stat)
        {
            float multiplier = 1f;
            foreach (var def in Actions)
                if (def.BuffBattles > 0 && def.BuffStat == stat && Charges(def.Action) > 0) multiplier *= 1f + def.BuffPercent / 100f;
            return multiplier;
        }

        public List<BattleSystem.ConditionLine> Lines()
        {
            var lines = new List<BattleSystem.ConditionLine>();
            foreach (var def in Actions)
            {
                int left = Charges(def.Action);
                if (def.BuffBattles > 0 && left > 0)
                    lines.Add(new BattleSystem.ConditionLine { Text = $"{(def.Action == CampAction.Feed ? "Fed" : "Groomed")}: {def.Effect} · {left} battle{(left == 1 ? "" : "s")} left", Good = true });
            }
            return lines;
        }

        // Every fight that was fought to the end uses one charge of each buff.
        public void OnBattleFought()
        {
            SetCharges(CampAction.Feed, Save.fedBattles - 1);
            SetCharges(CampAction.Groom, Save.groomedBattles - 1);
            OnChanged?.Invoke();
        }

        // ---------------------------------------------------------------- the treat

        public float TreatCooldownRemaining
        {
            get
            {
                double remaining = TreatCooldownSeconds * CooldownScale() - (_clock.UtcNow - _lastTreatUtc).TotalSeconds;
                return remaining <= 0d ? 0f : (float)remaining;
            }
        }

        public bool TryTreat()
        {
            if (TreatCooldownRemaining > 0f) return false;
            _lastTreatUtc = _clock.UtcNow;
            _battle.Recover(TreatRecovery, TreatRecovery);
            OnTreated?.Invoke();
            OnChanged?.Invoke();
            return true;
        }
    }
}
