using System;
using System.Collections;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Economy;
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
        private RectTransform _skillsTrack;
        private Text _skillsBranch;
        private RectTransform _skillsFill;
        private SettingsPanelView _settingsPanel;
        private StoryPanelView _storyPanel;
        private LeaderboardPanelView _leaderboardPanel;
        private Text _coins;
        private Text _gems;
        private Image _backdrop;
        private Image _glow;
        private Image _toastPill;
        private Text _toastText;
        private SkillTreePanelView _skillsPanel;
        private ShopPanelView _shopPanel;
        private DialogBoxView _dialog;
        private NewsPanelView _newsPanel;
        private Image _newsBadge;
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
                case CareAction.Clean: return UIFactory.IconKind.Shower;
                case CareAction.Rest: return UIFactory.IconKind.Moon;
                default: return UIFactory.IconKind.Ball;
            }
        }

        private void Build()
        {
            var canvas = UIFactory.CreateCanvas("GotchiCanvas");
            var background = UIFactory.CreatePanel("Background", canvas.transform, UIFactory.Cream);
            UIFactory.Fill(background.rectTransform);
            _backdrop = background;

            var glow = UIFactory.CreateGradient("Glow", background.transform, UIFactory.Peach);
            UIFactory.Place(glow.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            _glow = glow;

            // Background sets paint here: full screen, behind the safe-area column, so scenes run edge to edge.
            var sceneRoot = UIFactory.CreateRect("SceneRoot", background.transform);
            UIFactory.Fill(sceneRoot);

            var safe = UIFactory.CreateRect("SafeArea", background.transform);
            UIFactory.ApplySafeArea(safe);

            var column = UIFactory.CreateRect("Column", safe);
            UIFactory.Fill(column, UIFactory.Spacing.Gutter, UIFactory.Spacing.Gutter, 16f, 12f);
            UIFactory.AddVerticalLayout(column.gameObject, 12f, new RectOffset(0, 0, 0, 0));   // tight gaps between the home blocks

            // Header: wallet boxes left (amount + unit; tap opens the shop), square icon buttons right.
            var header = UIFactory.CreateRect("Header", column);
            UIFactory.SetPreferredHeight(header.gameObject, 84f);
            UIFactory.AddHorizontalLayout(header.gameObject, 12f, new RectOffset(0, 0, 0, 0));
            var wallet = UIFactory.CreateWalletBox("Wallet", header, out _coins, out _gems, () => ShowShop(ShopCategory.Bonuses), () => ShowShop(_ctx.Data.ageBand != "under13" ? ShopCategory.Hearts : ShopCategory.Bonuses));
            UIFactory.SetWidth(wallet.gameObject, 620f, 0f);
            var spacer = UIFactory.CreateRect("Spacer", header);
            UIFactory.SetWidth(spacer.gameObject, 0f, 1f);
            var trophy = UIFactory.CreateIconButton("TrophyButton", header, Color.white, UIFactory.IconKind.Trophy, 84f, () => { _leaderboardPanel.Refresh(); TogglePanel(_leaderboardPanel.Root); }, this);
            UIFactory.SetWidth(trophy.gameObject, 84f, 0f);
            var news = UIFactory.CreateIconButton("NewsButton", header, Color.white, UIFactory.IconKind.Bell, 84f, ShowNews, this);
            UIFactory.SetWidth(news.gameObject, 84f, 0f);
            _newsBadge = UIFactory.CreateCircle("Badge", news.transform, UIFactory.PinkDark, 22f);
            UIFactory.Place(_newsBadge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(2f, 2f));
            _newsBadge.raycastTarget = false;
            var settings = UIFactory.CreateIconButton("SettingsButton", header, Color.white, UIFactory.IconKind.Gear, 84f, () => TogglePanel(_settingsPanel.Root), this);
            UIFactory.SetWidth(settings.gameObject, 84f, 0f);

            // Room takes all remaining height.
            var roomHolder = UIFactory.CreateRect("RoomHolder", column);
            var roomElement = roomHolder.gameObject.AddComponent<LayoutElement>();
            roomElement.flexibleHeight = 1f;
            roomElement.minHeight = 500f;
            _room = new RoomView(roomHolder, sceneRoot, _ctx.Data.petName, _ctx.Data.species, OnCare, OnCuddle, () => { _storyPanel.Refresh(); TogglePanel(_storyPanel.Root); }, OnPetTap, this);
            UIFactory.Fill(_room.Root);
            var shop = UIFactory.CreateIconButton("ShopButton", roomHolder, Color.white, UIFactory.IconKind.Shop, 84f, () => TogglePanel(_shopPanel.Root), this);
            UIFactory.Place((RectTransform)shop.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-84f, -84f), new Vector2(0f, 0f));   // 12 px under the gear, same as the header gaps

            // One Skills item: stage, leading branch, availability dots, and a Train button.
            var skillsCard = UIFactory.CreateCard("SkillsCard", column, new Color(1f, 1f, 1f, 0.95f), 1f);
            UIFactory.SetPreferredHeight(skillsCard.transform.parent.gameObject, 124f);
            var skillsIcon = UIFactory.CreateIcon(UIFactory.IconKind.Sparkle, skillsCard.transform, 60f, Color.white);
            UIFactory.Place(skillsIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, -30f), new Vector2(100f, 30f));
            _skillsStage = UIFactory.CreateText("Stage", skillsCard.transform, "", 30, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            _skillsStage.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_skillsStage.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(128f, -60f), new Vector2(-200f, -18f));
            _skillsBranch = UIFactory.CreateText("Branch", skillsCard.transform, "", 22, UIFactory.Muted, TextAnchor.MiddleLeft);
            _skillsBranch.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_skillsBranch.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(128f, -100f), new Vector2(-200f, -60f));
            // Stage bar on the same line as "Skills · Stage 2/5", stretched up to the Train button (laid out in RefreshJourney).
            var skillsTrack = UIFactory.CreatePillBar("Bar", skillsCard.transform, UIFactory.Pink, out _skillsFill);
            _skillsTrack = skillsTrack.rectTransform;
            UIFactory.Place(_skillsTrack, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(420f, -47f), new Vector2(-216f, -31f));
            var train = UIFactory.CreateButton("Train", skillsCard.transform, "Train", UIFactory.Pink, () => TogglePanel(_skillsPanel.Root), 28, this);
            UIFactory.Place((RectTransform)train.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f - 160f, -34f), new Vector2(-UIFactory.Spacing.Pad, 34f));

            // Care block: one box with the four care options in a 2×2 grid (Sapphire battle menu), icons + HP bars.
            var dock = UIFactory.CreateFrame("Dock", column, Color.white);
            UIFactory.SetPreferredHeight(dock.gameObject, 164f);
            int index = 0;
            foreach (CareAction action in Enum.GetValues(typeof(CareAction)))
            {
                CareAction captured = action;
                _rings[action] = new NeedRingView(dock.transform, action, NeedColors[CareActionService.NeedFor(action)], index, () => OnCare(captured), this);
                index++;
            }

            _skillsPanel = new SkillTreePanelView(safe, _ctx.Skills, _ctx.Needs, OpenMiniGame, () => _skillsPanel.Root.SetActive(false), this);
            UIFactory.Fill((RectTransform)_skillsPanel.Root.transform);
            _skillsPanel.Root.SetActive(false);

            _dialog = new DialogBoxView(safe, this, 30);
            UIFactory.Place(_dialog.Root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 150f), new Vector2(-24f, 400f));

            _shopPanel = new ShopPanelView(safe, _ctx, ShowToast, () => _shopPanel.Root.SetActive(false), this, _dialog, _ctx.Data.ageBand != "under13");
            UIFactory.Fill((RectTransform)_shopPanel.Root.transform);
            _shopPanel.Root.SetActive(false);

            _settingsPanel = new SettingsPanelView(safe, _ctx, ShowToast, () => _settingsPanel.Root.SetActive(false), () => TogglePanel(_shopPanel.Root), this, _dialog);
            UIFactory.Fill((RectTransform)_settingsPanel.Root.transform);
            _settingsPanel.Root.SetActive(false);

            _storyPanel = new StoryPanelView(safe, _ctx, () => _storyPanel.Root.SetActive(false), this);
            UIFactory.Fill((RectTransform)_storyPanel.Root.transform);
            _storyPanel.Root.SetActive(false);

            _leaderboardPanel = new LeaderboardPanelView(safe, _ctx, () => _leaderboardPanel.Root.SetActive(false), this);
            UIFactory.Fill((RectTransform)_leaderboardPanel.Root.transform);
            _leaderboardPanel.Root.SetActive(false);

            _newsPanel = new NewsPanelView(safe, _ctx, () => _newsPanel.Root.SetActive(false), this);
            UIFactory.Fill((RectTransform)_newsPanel.Root.transform);
            _newsPanel.Root.SetActive(false);
            RefreshNewsBadge();
            _dialog.Root.SetAsLastSibling();

            _miniGames = new MiniGameOverlayView(background.transform, HandleMiniGameResult, branch => MiniGameDifficulty.For(_ctx.Skills.GetXp(branch) / SkillTreeSystem.XpPerStage, _ctx.Level.Level), this);

            _toastPill = UIFactory.CreatePill("Toast", safe, UIFactory.Ink);
            UIFactory.Place(_toastPill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-400f, 340f), new Vector2(400f, 418f));
            _toastPill.raycastTarget = false;
            _toastText = UIFactory.CreateText("Text", _toastPill.transform, "", 28, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(_toastText.rectTransform, 20f, 20f, 4f, 4f);
            _toastPill.gameObject.SetActive(false);
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
            _room.ApplyRoom(_ctx.Data.rugId, _ctx.Data.ownedRoomIds.Contains("fairy_lights"), _ctx.Data.backgroundId);
            var scene = RoomScenes.Find(_ctx.Data.backgroundId);
            _backdrop.color = scene.Backdrop;
            _glow.color = scene.Glow;
        }

        // Dev hook: re-applies outfit, decor and background after the save was edited directly.
        public void RefreshLook() => ApplyLook();

        private void RefreshJourney()
        {
            var skills = _ctx.Skills;
            float progress = skills.XpIntoCurrentStage(skills.EvolutionBranch) / (float)SkillTreeSystem.XpPerStage;
            if (skills.EvolutionStage >= SkillTreeSystem.MaxStage) progress = 1f;
            _skillsStage.text = $"Skills · Stage {skills.EvolutionStage}/{SkillTreeSystem.MaxStage}";
            _skillsTrack.offsetMin = new Vector2(128f + _skillsStage.preferredWidth + 18f, -47f);
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
                StartCoroutine(SimpleTween.CountUp(_coins, _lastCoins, coins, 0.5f, " COINS"));
                StartCoroutine(SimpleTween.CountUp(_gems, _lastGems, gems, 0.5f, " HEARTS"));
            }
            else
            {
                _coins.text = coins + " COINS";
                _gems.text = gems + " HEARTS";
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

        private static readonly Dictionary<PetPart, (EmotionType[] moods, string[] words)> TapReactions = new Dictionary<PetPart, (EmotionType[], string[])>
        {
            { PetPart.Head, (new[] { EmotionType.Love, EmotionType.Warmth, EmotionType.Satisfaction }, new[] { "Purr~", "Mmm...", "That's the spot" }) },
            { PetPart.Body, (new[] { EmotionType.Joy, EmotionType.Gladness, EmotionType.Excitement }, new[] { "Hehe!", "Boop!", "Again!" }) },
            { PetPart.Paws, (new[] { EmotionType.Pride, EmotionType.Excitement, EmotionType.Curiosity }, new[] { "High five!", "Shake!", "Paw!" }) },
            { PetPart.Tail, (new[] { EmotionType.Annoyance, EmotionType.Shock, EmotionType.Irritation }, new[] { "Hey!", "Not the tail!", "Hmph." }) },
        };
        private float _lastBoopTime = -100f;

        private void OnPetTap(PetPart part)
        {
            _room.Boop(part);
            var (moods, words) = TapReactions[part];
            _ctx.Emotions.TriggerEvent(moods[UnityEngine.Random.Range(0, moods.Length)], 4f);
            _room.FloatText(words[UnityEngine.Random.Range(0, words.Length)], part == PetPart.Tail ? UIFactory.Ink : UIFactory.PinkDark);
            if (part == PetPart.Tail) return;
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
            _dialog.Hide();
            if (show && panel == _shopPanel.Root) _shopPanel.Refresh();
            _skillsPanel.Root.SetActive(false);
            _shopPanel.Root.SetActive(false);
            _settingsPanel.Root.SetActive(false);
            _storyPanel.Root.SetActive(false);
            _leaderboardPanel.Root.SetActive(false);
            _newsPanel.Root.SetActive(false);
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
            MiniGameContext.PetName = _ctx.Data.petName;
            MiniGameContext.Level = _ctx.Level.Level;
            var rivals = _ctx.Leaderboards.Top(LeaderboardKind.Level, SkillBranch.Sport, 25).FindAll(e => !e.IsLocal);
            if (rivals.Count > 0)
            {
                var rival = rivals[UnityEngine.Random.Range(0, rivals.Count)];
                MiniGameContext.RivalName = rival.PetName;
                MiniGameContext.RivalSpecies = rival.Species;
            }
            _miniGames.Open(info);
        }

        private void HandleMiniGameResult(MiniGameResult result)
        {
            _onMiniGameResult?.Invoke(result);
            _skillsPanel.Refresh();
        }

        public void ShowSkills() => TogglePanel(_skillsPanel.Root);
        public void ShowShop() => TogglePanel(_shopPanel.Root);
        public void ShowShop(ShopCategory category)
        {
            if (!_shopPanel.Root.activeSelf) TogglePanel(_shopPanel.Root);
            _shopPanel.Show(category);
        }
        public void ShowSettings() => TogglePanel(_settingsPanel.Root);
        public void ShowLeaderboard() { _leaderboardPanel.Refresh(); TogglePanel(_leaderboardPanel.Root); }
        public void ShowStory() { _storyPanel.Refresh(); TogglePanel(_storyPanel.Root); }
        public void ShowLeaderboardSkills() => _leaderboardPanel.ShowSkillsTab();

        public void ShowNews()
        {
            _newsPanel.Refresh();
            TogglePanel(_newsPanel.Root);
            if (_newsPanel.Root.activeSelf && _ctx.News.UnreadCount > 0)
            {
                _ctx.News.MarkAllRead();
                _ctx.SaveService.Save(_ctx.Data);
                RefreshNewsBadge();
            }
        }

        private void RefreshNewsBadge() => _newsBadge.gameObject.SetActive(_ctx.News.UnreadCount > 0);
        public void ShowSettingsPage(string id) => _settingsPanel.ShowPage(id);
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
