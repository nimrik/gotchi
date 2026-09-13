using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.MiniGames;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    public class HUDController : MonoBehaviour
    {
        private static readonly Dictionary<NeedType, Color> NeedColors = new Dictionary<NeedType, Color>
        {
            { NeedType.Hunger, UIFactory.Coral },
            { NeedType.Hygiene, UIFactory.Sky },
            { NeedType.Energy, UIFactory.Lavender },
            { NeedType.Happiness, UIFactory.Pink },
        };

        private GameContext _ctx;
        private Action<MiniGameResult> _onMiniGameResult;
        private RoomView _room;
        private readonly Dictionary<CareAction, NeedRingView> _rings = new Dictionary<CareAction, NeedRingView>();
        private Text _skillsStage;
        private Text _skillsBranch;
        private RectTransform _skillsFill;
        private SettingsPanelView _settingsPanel;
        private StoryPanelView _storyPanel;
        private LeaderboardPanelView _leaderboardPanel;
        private Text _coins;
        private Text _gems;
        private Image _toastPill;
        private Text _toastText;
        private SkillTreePanelView _skillsPanel;
        private ShopPanelView _shopPanel;
        private MiniGameOverlayView _miniGames;
        private int _lastCoins;
        private int _lastGems;

        public void Initialize(GameContext ctx, Action<MiniGameResult> onMiniGameResult)
        {
            _ctx = ctx;
            _onMiniGameResult = onMiniGameResult;
            Build();
            Subscribe();
            RefreshAll();
        }

        private static UIFactory.IconKind IconFor(CareAction action)
        {
            switch (action)
            {
                case CareAction.Feed: return UIFactory.IconKind.Cookie;
                case CareAction.Clean: return UIFactory.IconKind.Bubbles;
                case CareAction.Rest: return UIFactory.IconKind.Moon;
                default: return UIFactory.IconKind.Ball;
            }
        }

        private void Build()
        {
            var canvas = UIFactory.CreateCanvas("GotchiCanvas");
            var background = UIFactory.CreatePanel("Background", canvas.transform, UIFactory.Cream);
            UIFactory.Fill(background.rectTransform);

            var glow = UIFactory.CreateGradient("Glow", background.transform, UIFactory.Peach);
            UIFactory.Place(glow.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var safe = UIFactory.CreateRect("SafeArea", background.transform);
            UIFactory.ApplySafeArea(safe);

            var column = UIFactory.CreateRect("Column", safe);
            UIFactory.Fill(column, UIFactory.Spacing.Gutter, UIFactory.Spacing.Gutter, 16f, 16f);
            UIFactory.AddVerticalLayout(column.gameObject, UIFactory.Spacing.Section, new RectOffset(0, 0, 0, 0));

            // Header: currency pills left, round icon buttons right.
            var header = UIFactory.CreateRect("Header", column);
            UIFactory.SetPreferredHeight(header.gameObject, 84f);
            UIFactory.AddHorizontalLayout(header.gameObject, 12f, new RectOffset(0, 0, 0, 0));
            _coins = CurrencyPill(header, UIFactory.Hex("FFF0C2"), UIFactory.IconKind.Coin);
            _gems = CurrencyPill(header, UIFactory.Hex("E9E0FF"), UIFactory.IconKind.Gem);
            var spacer = UIFactory.CreateRect("Spacer", header);
            UIFactory.SetWidth(spacer.gameObject, 0f, 1f);
            var trophy = UIFactory.CreateIconButton("TrophyButton", header, Color.white, UIFactory.IconKind.Trophy, 84f, () => { _leaderboardPanel.Refresh(); TogglePanel(_leaderboardPanel.Root); }, this);
            UIFactory.SetWidth(trophy.gameObject, 84f, 0f);
            var settings = UIFactory.CreateIconButton("SettingsButton", header, Color.white, UIFactory.IconKind.Gear, 84f, () => TogglePanel(_settingsPanel.Root), this);
            UIFactory.SetWidth(settings.gameObject, 84f, 0f);

            // Room takes all remaining height.
            var roomHolder = UIFactory.CreateRect("RoomHolder", column);
            var roomElement = roomHolder.gameObject.AddComponent<LayoutElement>();
            roomElement.flexibleHeight = 1f;
            roomElement.minHeight = 500f;
            _room = new RoomView(roomHolder, _ctx.Data.petName, _ctx.Data.species, OnCare, OnCuddle, () => { _storyPanel.Refresh(); TogglePanel(_storyPanel.Root); }, OnPetTap, this);
            UIFactory.Fill(_room.Root);
            var shop = UIFactory.CreateIconButton("ShopButton", roomHolder, Color.white, UIFactory.IconKind.Bag, 84f, () => TogglePanel(_shopPanel.Root), this);
            UIFactory.Place((RectTransform)shop.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-84f, -84f), new Vector2(0f, 0f));

            // One Skills item: stage, leading branch, availability dots, and a Train button.
            var skillsCard = UIFactory.CreateCard("SkillsCard", column, new Color(1f, 1f, 1f, 0.95f), 1f);
            UIFactory.SetPreferredHeight(skillsCard.transform.parent.gameObject, 148f);
            var skillsIcon = UIFactory.CreateIcon(UIFactory.IconKind.Sparkle, skillsCard.transform, 60f, Color.white);
            UIFactory.Place(skillsIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(UIFactory.Spacing.Pad, -30f), new Vector2(UIFactory.Spacing.Pad + 60f, 30f));
            _skillsStage = UIFactory.CreateText("Stage", skillsCard.transform, "", 30, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Place(_skillsStage.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(100f, -66f), new Vector2(-200f, -20f));
            _skillsBranch = UIFactory.CreateText("Branch", skillsCard.transform, "", 22, UIFactory.Muted, TextAnchor.MiddleLeft);
            _skillsBranch.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_skillsBranch.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(100f, -104f), new Vector2(-200f, -66f));
            var skillsTrack = UIFactory.CreatePillBar("Bar", skillsCard.transform, UIFactory.Pink, out _skillsFill);
            UIFactory.Place(skillsTrack.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(100f, -128f), new Vector2(-200f, -112f));
            var train = UIFactory.CreateButton("Train", skillsCard.transform, "Train", UIFactory.Pink, () => TogglePanel(_skillsPanel.Root), 28, this);
            UIFactory.Place((RectTransform)train.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f - 160f, -34f), new Vector2(-UIFactory.Spacing.Pad, 34f));

            // Dock: each care button wrapped in the radial meter of its need.
            var dock = UIFactory.CreateCard("Dock", column, new Color(1f, 1f, 1f, 0.95f), 1.2f);
            UIFactory.SetPreferredHeight(dock.transform.parent.gameObject, 236f);
            int index = 0;
            foreach (CareAction action in Enum.GetValues(typeof(CareAction)))
            {
                CareAction captured = action;
                float x = (index + 0.5f) / 4f;
                _rings[action] = new NeedRingView(dock.transform, action, NeedColors[CareActionService.NeedFor(action)], x, () => OnCare(captured), this);
                index++;
            }

            _skillsPanel = new SkillTreePanelView(safe, _ctx.Skills, _ctx.Needs, OpenMiniGame, () => _skillsPanel.Root.SetActive(false), this);
            UIFactory.Fill((RectTransform)_skillsPanel.Root.transform);
            _skillsPanel.Root.SetActive(false);

            _shopPanel = new ShopPanelView(safe, _ctx, ShowToast, () => _shopPanel.Root.SetActive(false), this, _ctx.Data.ageBand != "under13");
            UIFactory.Fill((RectTransform)_shopPanel.Root.transform);
            _shopPanel.Root.SetActive(false);

            _settingsPanel = new SettingsPanelView(safe, _ctx, ShowToast, () => _settingsPanel.Root.SetActive(false), () => TogglePanel(_shopPanel.Root), this);
            UIFactory.Fill((RectTransform)_settingsPanel.Root.transform);
            _settingsPanel.Root.SetActive(false);

            _storyPanel = new StoryPanelView(safe, _ctx, () => _storyPanel.Root.SetActive(false), this);
            UIFactory.Fill((RectTransform)_storyPanel.Root.transform);
            _storyPanel.Root.SetActive(false);

            _leaderboardPanel = new LeaderboardPanelView(safe, _ctx, () => _leaderboardPanel.Root.SetActive(false), this);
            UIFactory.Fill((RectTransform)_leaderboardPanel.Root.transform);
            _leaderboardPanel.Root.SetActive(false);

            _miniGames = new MiniGameOverlayView(background.transform, HandleMiniGameResult, branch => MiniGameDifficulty.For(_ctx.Skills.GetXp(branch) / SkillTreeSystem.XpPerStage, _ctx.Level.Level), this);

            _toastPill = UIFactory.CreatePill("Toast", safe, UIFactory.Ink);
            UIFactory.Place(_toastPill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-400f, 340f), new Vector2(400f, 418f));
            _toastPill.raycastTarget = false;
            _toastText = UIFactory.CreateText("Text", _toastPill.transform, "", 28, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(_toastText.rectTransform, 20f, 20f, 4f, 4f);
            _toastPill.gameObject.SetActive(false);
        }

        private static Text CurrencyPill(Transform parent, Color color, UIFactory.IconKind icon)
        {
            var pill = UIFactory.CreatePill("Pill", parent, color);
            UIFactory.SetWidth(pill.gameObject, 210f, 0f);
            var iconRect = UIFactory.CreateIcon(icon, pill.transform, 44f, color);
            UIFactory.Place(iconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -22f), new Vector2(58f, 22f));
            var text = UIFactory.CreateText("Text", pill.transform, "", 32, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Place(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(68f, 0f), new Vector2(-10f, 0f));
            return text;
        }

        private void Subscribe()
        {
            _ctx.Needs.OnNeedChanged += (need, value) => { foreach (var ring in _rings.Values) if (ring.Need == need) ring.SetValue(value, true); };
            _ctx.Emotions.OnEmotionChanged += emotion => _room.SetEmotion(emotion, true);
            _ctx.Wallet.OnBalanceChanged += (_, __) => RefreshCurrency(true);
            _ctx.Skills.OnXpChanged += (_, __) => { _skillsPanel.Refresh(); RefreshJourney(); };
            _ctx.Skills.OnEvolutionStageChanged += stage =>
            {
                _room.Celebrate();
                ShowToast($"{_ctx.Data.petName} evolved to stage {stage}!");
                _skillsPanel.Refresh();
                RefreshJourney();
            };
            _ctx.Skills.OnBranchLocked += branch =>
            {
                ShowToast($"{_ctx.Data.petName} chose the {UIFactory.PrettyName(branch.ToString())} path!");
                _skillsPanel.Refresh();
                RefreshJourney();
            };
            _ctx.Automation.OnUnlocked += _ => { _shopPanel.Refresh(); _ctx.Level.AddXp(20); };
            _ctx.Level.OnXpChanged += _ => _room.SetLevel(_ctx.Level.Level, _ctx.Level.ProgressToNext);
            _ctx.Level.OnLevelUp += level =>
            {
                _room.Celebrate();
                ShowToast($"{_ctx.Data.petName} reached level {level}! New story chapter.");
            };
            _ctx.Shop.OnItemGranted += _ => _shopPanel.Refresh();
            _ctx.Shop.OnEquippedChanged += ApplyLook;
            _ctx.Boosts.OnChanged += () => _shopPanel.Refresh();
        }

        private void RefreshAll()
        {
            foreach (var ring in _rings.Values) ring.SetValue(_ctx.Needs.Get(ring.Need), false);
            _room.SetEmotion(_ctx.Emotions.Current, false);
            RefreshJourney();
            RefreshWish();
            _room.SetLevel(_ctx.Level.Level, _ctx.Level.ProgressToNext);
            ApplyLook();
            RefreshCurrency(false);
            _skillsPanel.Refresh();
            _shopPanel.Refresh();
        }

        private void ApplyLook()
        {
            _room.Pet.SetAccessory(_ctx.Data.equippedCosmeticId);
            _room.ApplyRoom(_ctx.Data.rugId, _ctx.Data.ownedRoomIds.Contains("fairy_lights"));
        }

        private void RefreshJourney()
        {
            var skills = _ctx.Skills;
            float progress = skills.XpIntoCurrentStage(skills.EvolutionBranch) / (float)SkillTreeSystem.XpPerStage;
            if (skills.EvolutionStage >= SkillTreeSystem.MaxStage) progress = 1f;
            _skillsStage.text = $"Skills · Stage {skills.EvolutionStage}/{SkillTreeSystem.MaxStage}";
            _skillsBranch.text = $"{UIFactory.PrettyName(skills.EvolutionBranch.ToString())} {(skills.IsLocked ? "path" : "leading")} · day {_ctx.Data.loginStreakDays} streak";
            _skillsFill.anchorMax = new Vector2(Mathf.Max(0.03f, progress), 1f);
            _skillsFill.GetComponent<Image>().color = UIFactory.Pink;
        }

        private void RefreshWish()
        {
            NeedType lowest = _ctx.Needs.LowestNeed;
            _room.SetWish(lowest, _ctx.Needs.Get(lowest), _ctx.Care.CanCuddle, _ctx.Care.CuddleCooldownRemaining);
            _room.SetConditions(_ctx.Needs.Get(NeedType.Hunger), _ctx.Needs.Get(NeedType.Hygiene), _ctx.Needs.Get(NeedType.Energy), _ctx.Needs.Get(NeedType.Happiness));
        }

        private float _wishTimer;

        private void RefreshCurrency(bool animate)
        {
            int coins = _ctx.Wallet.Get(CurrencyType.Soft);
            int gems = _ctx.Wallet.Get(CurrencyType.Premium);
            if (animate)
            {
                StartCoroutine(SimpleTween.CountUp(_coins, _lastCoins, coins, 0.5f));
                StartCoroutine(SimpleTween.CountUp(_gems, _lastGems, gems, 0.5f));
            }
            else
            {
                _coins.text = coins.ToString();
                _gems.text = gems.ToString();
            }
            _lastCoins = coins;
            _lastGems = gems;
        }

        private void Update()
        {
            if (_ctx == null || _room == null || _miniGames == null) return;
            _wishTimer += Time.deltaTime;
            if (_wishTimer >= 0.5f) { _wishTimer = 0f; RefreshWish(); _skillsPanel.Refresh(); }
            foreach (var pair in _rings) pair.Value.SetCooldown(_ctx.Care.RemainingCooldown(pair.Key));
        }

        private void OnCare(CareAction action)
        {
            if (!_ctx.Care.TryPerform(action)) return;
            _room.Celebrate();
            _room.FloatText($"+{(int)CareActionService.RestoreAmount(action)} {CareActionService.NeedFor(action)}", NeedColors[CareActionService.NeedFor(action)]);
        }

        private static readonly EmotionType[] BoopMoods = { EmotionType.Joy, EmotionType.Love, EmotionType.Excitement, EmotionType.Curiosity, EmotionType.Gladness };
        private static readonly string[] BoopWords = { "Boop!", "Purr~", "Hehe", "<3", "Again!" };
        private float _lastBoopTime = -100f;

        private void OnPetTap()
        {
            _room.Boop();
            _ctx.Emotions.TriggerEvent(BoopMoods[UnityEngine.Random.Range(0, BoopMoods.Length)], 4f);
            _room.FloatText(BoopWords[UnityEngine.Random.Range(0, BoopWords.Length)], UIFactory.PinkDark);
            if (Time.time - _lastBoopTime >= 10f)
            {
                _lastBoopTime = Time.time;
                _ctx.Needs.Add(NeedType.Happiness, 1f);
            }
        }

        private void OnCuddle()
        {
            if (!_ctx.Care.TryCuddle()) return;
            _room.Celebrate();
            _room.FloatText("+15 XP", UIFactory.PinkDark);
        }

        private void TogglePanel(GameObject panel)
        {
            bool show = !panel.activeSelf;
            if (show && panel == _shopPanel.Root) _shopPanel.Refresh();
            _skillsPanel.Root.SetActive(false);
            _shopPanel.Root.SetActive(false);
            _settingsPanel.Root.SetActive(false);
            _storyPanel.Root.SetActive(false);
            _leaderboardPanel.Root.SetActive(false);
            panel.SetActive(show);
            if (show) StartCoroutine(SimpleTween.PopIn(panel.transform.GetChild(1), 0.22f));
        }

        private void OpenMiniGame(SkillBranch branch)
        {
            var info = MiniGameRegistry.For(branch);
            if (info == null || !info.Implemented)
            {
                ShowToast(info?.UnavailableReason ?? "Coming soon.");
                return;
            }
            if (!SkillGate.IsAvailable(branch, _ctx.Needs, out string reason))
            {
                ShowToast(reason);
                return;
            }
            _skillsPanel.Root.SetActive(false);
            _shopPanel.Root.SetActive(false);
            MiniGameContext.Species = _ctx.Data.species;
            _miniGames.Open(info);
        }

        private void HandleMiniGameResult(MiniGameResult result)
        {
            _onMiniGameResult?.Invoke(result);
            _skillsPanel.Refresh();
        }

        public void ShowSkills() => TogglePanel(_skillsPanel.Root);
        public void ShowShop() => TogglePanel(_shopPanel.Root);
        public void ShowSettings() => TogglePanel(_settingsPanel.Root);
        public void ShowLeaderboard() { _leaderboardPanel.Refresh(); TogglePanel(_leaderboardPanel.Root); }
        public void ShowMiniGame(SkillBranch branch) => OpenMiniGame(branch);

        public void ShowToast(string message)
        {
            StopCoroutine(nameof(ToastRoutine));
            StartCoroutine(nameof(ToastRoutine), message);
        }

        private IEnumerator ToastRoutine(string message)
        {
            _toastText.text = message;
            _toastPill.color = UIFactory.Ink;
            _toastText.color = Color.white;
            _toastPill.gameObject.SetActive(true);
            yield return SimpleTween.PopIn(_toastPill.transform, 0.2f);
            yield return new WaitForSeconds(2.2f);
            var pillClear = UIFactory.Ink; pillClear.a = 0f;
            StartCoroutine(SimpleTween.ColorTo(_toastText, new Color(1f, 1f, 1f, 0f), 0.35f));
            yield return SimpleTween.ColorTo(_toastPill, pillClear, 0.35f);
            _toastPill.gameObject.SetActive(false);
        }
    }
}
