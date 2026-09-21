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
        private GameContext _ctx;
        private Action<MiniGameResult> _onMiniGameResult;
        private RoomView _room;
        private readonly Dictionary<CampAction, CampCellView> _campCells = new Dictionary<CampAction, CampCellView>();
        private Text _leagueText, _leagueValue;
        private RectTransform _leagueTrack, _leagueFill;
        private SettingsPanelView _settingsPanel;
        private StoryPanelView _storyPanel;
        private LeaderboardPanelView _leaderboardPanel;
        private Text _coins;
        private Text _gems;
        private Image _backdrop;
        private Image _glow;
        private Image _toastPill;
        private Text _toastText;
        private BattleClubPanelView _battleClub;
        private MarketPanelView _market;
        private CampaignPanelView _campaignPanel;
        private bool _lastBattleWild;
        private ShopPanelView _shopPanel;
        private DialogBoxView _dialog;
        private NewsPanelView _newsPanel;
        private Image _newsBadge;
        private MiniGameOverlayView _miniGames;
        private int _lastCoins;
        private int _lastGems;
        private LayoutElement _walletElement;
        private float _sceneTimer;

        public void Initialize(GameContext ctx, Action<MiniGameResult> onMiniGameResult)
        {
            _ctx = ctx;
            _onMiniGameResult = onMiniGameResult;
            Build();
            Subscribe();
            RefreshAll();
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
            _walletElement = UIFactory.SetWidth(wallet.gameObject, 620f, 0f);   // fitted to its content in RefreshCurrency
            var spacer = UIFactory.CreateRect("Spacer", header);
            UIFactory.SetWidth(spacer.gameObject, 0f, 1f);
            // The pet's story (one chapter per level) has its own button; it used to hide behind "tap for story" in the status block.
            var story = UIFactory.CreateIconButton("StoryButton", header, Color.white, UIFactory.IconKind.Book, 84f, ShowStory, this);
            UIFactory.SetWidth(story.gameObject, 84f, 0f);
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
            RoomScenes.LocalNow = () => _ctx.Clock.LocalNow;   // the default room's window follows the game clock's local time
            _room = new RoomView(roomHolder, sceneRoot, _ctx.Data.petName, _ctx.Data.species, OnTreat, OnPetTap, this, BuildStatusInfo);
            UIFactory.Fill(_room.Root);
            var shop = UIFactory.CreateIconButton("ShopButton", roomHolder, Color.white, UIFactory.IconKind.Shop, 84f, () => TogglePanel(_shopPanel.Root), this);
            UIFactory.Place((RectTransform)shop.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-84f, -84f), new Vector2(0f, 0f));   // 12 px under the gear, same as the header gaps

            // Battle menu: the game is built around battles, so the doors into them are on the home screen. The league
            // line on top (press the bar for the full numbers), then one row: BATTLE (ranked fights), TRAIN, MOVES and
            // BAG. Same cell language as the care block under it: icon, pixel name, ▶ while held. The Market left this
            // row on 2026-09-21 (the Bag page still leads to it), and so did WILD, which is parked (GameFeatures.Wild).
            var menu = UIFactory.CreateFrame("BattleMenu", column, Color.white);
            menu.raycastTarget = false;
            UIFactory.SetPreferredHeight(menu.gameObject, 138f);
            _leagueText = UIFactory.CreatePixelText("League", menu.transform, "", 26, UIFactory.MenuInk, TextAnchor.MiddleLeft, false);
            _leagueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_leagueText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -54f), new Vector2(-28f, -14f));
            _leagueValue = UIFactory.CreatePixelText("Value", menu.transform, "", 22, UIFactory.Muted, TextAnchor.MiddleRight, false);
            _leagueValue.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_leagueValue.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-260f, -54f), new Vector2(-28f, -14f));
            var leagueTrack = UIFactory.CreatePillBar("Bar", menu.transform, UIFactory.Butter, out _leagueFill);
            _leagueTrack = leagueTrack.rectTransform;
            UIFactory.Place(_leagueTrack, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(300f, -42f), new Vector2(-200f, -26f));
            var leagueInfoHit = UIFactory.CreatePanel("InfoTap", _leagueTrack, Color.clear);
            UIFactory.Place(leagueInfoHit.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6f, -26f), new Vector2(6f, 26f));
            UIFactory.MakePressable(leagueInfoHit, ShowBattleInfo);
            var grid = UIFactory.CreateRect("Grid", menu.transform);
            UIFactory.Place(grid, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 8f), new Vector2(-10f, -60f));
            var doors = new List<(string label, UIFactory.IconKind icon, Action open)>
            {
                ("Battle", UIFactory.IconKind.Shield, () => ShowBattleClubPage(BattleClubPanelView.ClubPage)),
                ("Train", UIFactory.IconKind.Sparkle, () => ShowBattleClubPage(BattleClubPanelView.TrainPage)),
                ("Moves", UIFactory.IconKind.Paw, () => ShowBattleClubPage(BattleClubPanelView.MovesPage)),
                ("Bag", UIFactory.IconKind.Bag, () => ShowBattleClubPage(BattleClubPanelView.BagPage)),
            };
            if (GameFeatures.Wild) doors.Insert(1, ("Wild", UIFactory.IconKind.Compass, ShowCampaign));
            for (int i = 0; i < doors.Count; i++) MenuCell(grid, doors[i].label, doors[i].icon, i, doors.Count, doors[i].open);

            // Camp block: what the player does for the cat between fights, four actions in a 2×2 grid (CampSystem).
            // REST gives health back, FOCUS gives mana back, FEED buys ATTACK and GROOM buys DEFENSE for the next few
            // battles. It replaced the four needs (hunger, hygiene, energy, happiness) on 2026-09-21: nothing here
            // drains while the player is away, everything is about the next fight.
            var dock = UIFactory.CreateFrame("Camp", column, Color.white);
            UIFactory.SetPreferredHeight(dock.gameObject, 176f);
            for (int i = 0; i < CampSystem.Actions.Length; i++)
            {
                CampActionDef def = CampSystem.Actions[i];
                _campCells[def.Action] = new CampCellView(dock.transform, def, i, () => OnCamp(def.Action));
            }

            _dialog = new DialogBoxView(safe, this, 30);
            UIFactory.Place(_dialog.Root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 150f), new Vector2(-24f, 400f));

            _battleClub = new BattleClubPanelView(safe, _ctx, ShowToast, () => StartBattle(null), () => ShowMarket(), () => _battleClub.Root.SetActive(false), this, _dialog);
            UIFactory.Fill((RectTransform)_battleClub.Root.transform);
            _battleClub.Root.SetActive(false);

            _market = new MarketPanelView(safe, _ctx, ShowToast, () => _market.Root.SetActive(false), this, _dialog);
            UIFactory.Fill((RectTransform)_market.Root.transform);
            _market.Root.SetActive(false);

            if (GameFeatures.Wild)   // parked: no panel, no door, until the Wild becomes a world to explore
            {
                _campaignPanel = new CampaignPanelView(safe, _ctx, ShowToast, StartBattle, () => ShowBattleClubPage(BattleClubPanelView.ClubPage), () => _campaignPanel.Root.SetActive(false), this);
                UIFactory.Fill((RectTransform)_campaignPanel.Root.transform);
                _campaignPanel.Root.SetActive(false);
            }

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

            _miniGames = new MiniGameOverlayView(background.transform, HandleMiniGameResult, DifficultyFor, this,
                _ => { if (_lastBattleWild && _campaignPanel != null) ShowCampaign(); else ShowBattleClub(); });   // a fight ends where it was started from

            _toastPill = UIFactory.CreatePill("Toast", safe, UIFactory.Ink);
            UIFactory.Place(_toastPill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-400f, 340f), new Vector2(400f, 418f));
            _toastPill.raycastTarget = false;
            _toastText = UIFactory.CreateText("Text", _toastPill.transform, "", 28, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(_toastText.rectTransform, 20f, 20f, 4f, 4f);
            _toastPill.gameObject.SetActive(false);
        }

        private void Subscribe()
        {
            _ctx.Camp.OnChanged += RefreshCamp;
            _ctx.Automation.OnUnlocked += _ => RefreshCamp();
            _ctx.Wallet.OnBalanceChanged += (_, __) => RefreshCurrency(true);
            _ctx.Battle.OnChanged += RefreshLeague;
            _ctx.Battle.OnChanged += RefreshVitals;
            _ctx.Battle.OnChanged += () => RefreshLeaning(true);
            _ctx.Skills.OnEvolutionStageChanged += stage =>
            {
                _room.Celebrate();
                ShowToast($"{_ctx.Data.petName} evolved to stage {stage}!");
            };
            _ctx.Automation.OnUnlocked += _ => { _shopPanel.Refresh(); _ctx.Level.AddXp(20); };
            _ctx.Level.OnXpChanged += _ => RefreshLevel();
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
            _room.Pet.SetFace(CalmFace, false);
            RefreshLeaning(false);
            RefreshCamp();
            RefreshLeague();
            RefreshTreat();
            RefreshLevel();
            RefreshVitals();
            ApplyLook();
            RefreshCurrency(false);
            _shopPanel.Refresh();
        }

        private void ApplyLook()
        {
            _room.Pet.SetAccessory(_ctx.Data.equippedCosmeticId);
            ApplyScene();
        }

        // Background, rug and the screen tints. Also polled from Update: the default room follows the local time.
        private void ApplyScene()
        {
            _room.ApplyRoom(_ctx.Data.rugId, _ctx.Data.ownedRoomIds.Contains("fairy_lights"), _ctx.Data.backgroundId);
            var scene = RoomScenes.Find(_ctx.Data.backgroundId);
            if (scene.Id == RoomScenes.DefaultId)
            {
                var pal = RoomScenes.CozyPaletteAt(RoomScenes.LocalNow());   // light by day, dark at night, like the window
                _backdrop.color = pal.Backdrop;
                _glow.color = pal.Glow;
            }
            else
            {
                _backdrop.color = scene.Backdrop;
                _glow.color = scene.Glow;
            }
        }

        // ---- info boxes: the full numbers behind the two progress bars on the home screen ----

        private (string title, string body) BuildStatusInfo()
        {
            var level = _ctx.Level;
            var lines = new List<string>();
            lines.Add($"Evolution stage {_ctx.Skills.EvolutionStage}/{SkillTreeSystem.MaxStage}");
            if (level.Level >= LevelSystem.MaxLevel) lines.Add($"XP: {level.Xp} · top level reached");
            else
            {
                int start = LevelSystem.XpRequiredForLevel(level.Level), end = LevelSystem.XpRequiredForLevel(level.Level + 1);
                lines.Add($"XP: {level.Xp - start} / {end - start} · {end - level.Xp} more to Lv {level.Level + 1}");
                lines.Add($"Total XP: {level.Xp}");
            }
            var leaning = _ctx.Battle.Leaning;
            lines.Add($"Leans to: {leaning.Name}. {leaning.Description}");
            foreach (var buff in _ctx.Battle.Conditions()) lines.Add("+ " + buff.Text);
            lines.Insert(1, $"Health {_ctx.Battle.Hp}/{_ctx.Battle.MaxHp} · mana {_ctx.Battle.Mp}/{_ctx.Battle.MaxMp}: a fight leaves them as they end, the camp and time bring them back");
            lines.Add("XP comes from battles, the camp and helpers.");
            return ($"{_ctx.Data.petName} · Level {level.Level}", string.Join("\n", lines));
        }

        private (string title, string body) BuildBattleInfo()
        {
            var battle = _ctx.Battle;
            var lines = new List<string>();
            if (!battle.HasStyle) lines.Add("No fighting style yet. BATTLE lets you pick one for free.");
            else
            {
                var stats = battle.CurrentStats();
                lines.Add($"{battle.Style} style · HP {stats.MaxHp} · ATK {stats.Attack} · DEF {stats.Defense} · SPD {stats.Speed}");
            }
            var next = battle.NextLeague;
            lines.Add($"Rating {battle.Rating}" + (next != null ? $" · {next.MinRating - battle.Rating} to the {next.Name} League" : " · the top league"));
            lines.Add($"Ranked: won {battle.Wins}, lost {battle.Losses}, streak {battle.WinStreak} (best {battle.BestWinStreak})");
            lines.Insert(0, $"Health {battle.Hp}/{battle.MaxHp} · mana {battle.Mp}/{battle.MaxMp}" + (battle.Hp < battle.MaxHp || battle.Mp < battle.MaxMp ? " · REST, FOCUS, a treat or time bring them back" : ""));
            if (GameFeatures.Wild) lines.Add($"The Wild: {_ctx.Campaign.ClearedCount} of {CampaignSystem.Areas.Length} areas cleared · {_ctx.Campaign.WildWins} wild cats beaten");
            var skills = _ctx.Skills;
            int into = skills.XpIntoCurrentStage(SkillBranch.PvP);
            lines.Add(skills.EvolutionStage >= SkillTreeSystem.MaxStage ? $"Battle XP {skills.GetXp(SkillBranch.PvP)} · final evolution stage"
                : $"Battle XP {skills.GetXp(SkillBranch.PvP)} · evolution stage {skills.EvolutionStage}/{SkillTreeSystem.MaxStage}, {SkillTreeSystem.XpPerStage - into} XP to the next");
            foreach (var condition in battle.Conditions()) lines.Add((condition.Good ? "+ " : "- ") + condition.Text);
            lines.Add("A fight costs health and mana. The camp brings them back, and lends ATTACK and DEFENSE.");
            return ($"{battle.League.Name} League", string.Join("\n", lines));
        }

        // Also dev/QA hooks (screenshots): open an info box as a press on its bar would.
        public void ShowBattleInfo() { var info = BuildBattleInfo(); InfoTooltip.Toggle(_leagueTrack, info.title, info.body, this); }
        public void ShowStatusInfo() { var info = BuildStatusInfo(); InfoTooltip.Toggle(_room.LevelBar, info.title, info.body, this); }
        public void HideInfo() => InfoTooltip.Hide();

        // Dev hook: re-applies outfit, decor and background after the save was edited directly.
        public void RefreshLook() => ApplyLook();

        // The league line of the battle menu: "BRONZE LEAGUE  ▬▬▬▬  25 / 200".
        private void RefreshLeague()
        {
            var battle = _ctx.Battle;
            var next = battle.NextLeague;
            _leagueText.text = battle.HasStyle ? $"{battle.League.Name.ToUpperInvariant()} LEAGUE" : "PICK A STYLE IN BATTLE";
            _leagueValue.text = !battle.HasStyle ? "" : next != null ? $"{battle.Rating} / {next.MinRating}" : battle.Rating.ToString();
            _leagueTrack.gameObject.SetActive(battle.HasStyle);
            _leagueTrack.offsetMin = new Vector2(28f + _leagueText.preferredWidth + 18f, -42f);
            _leagueTrack.offsetMax = new Vector2(-28f - _leagueValue.preferredWidth - 18f, -26f);
            _leagueFill.anchorMax = new Vector2(Mathf.Max(0.03f, battle.LeagueProgress), 1f);
        }

        // One door of the battle menu: icon + pixel name, the whole cell is the button, ▶ shows while it is held.
        private void MenuCell(RectTransform grid, string label, UIFactory.IconKind icon, int column, int columns, Action onClick)
        {
            var cell = UIFactory.CreatePanel(label + "Cell", grid, Color.clear);
            UIFactory.Place(cell.rectTransform, new Vector2(column / (float)columns, 0f), new Vector2((column + 1) / (float)columns, 1f), new Vector2(4f, 2f), new Vector2(-4f, -2f));
            UIFactory.ApplyTransition(UIFactory.MakePressable(cell, onClick), false);
            var cursor = UIFactory.CreateCursor(cell.transform, 18f, UIFactory.MenuInk);
            UIFactory.Place(cursor.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -9f), new Vector2(18f, 9f));
            cursor.enabled = false;
            cell.gameObject.AddComponent<PressFeedback>().OnPressedChanged = down => { if (cursor != null) cursor.enabled = down; };
            var glyph = UIFactory.CreateIcon(icon, cell.transform, 44f, Color.white);
            UIFactory.Place(glyph, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, -22f), new Vector2(66f, 22f));
            var caption = UIFactory.CreatePixelText("Caption", cell.transform, label.ToUpperInvariant(), 30, UIFactory.MenuInk, TextAnchor.MiddleLeft, false);
            caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(78f, 0f), new Vector2(0f, 0f));
        }

        // The XP bar's caption is in experience points: "104 / 220 XP" into the current level.
        private void RefreshLevel()
        {
            var level = _ctx.Level;
            bool top = level.Level >= LevelSystem.MaxLevel;
            int start = LevelSystem.XpRequiredForLevel(level.Level), end = top ? start : LevelSystem.XpRequiredForLevel(level.Level + 1);
            _room.SetLevel(level.Level, level.ProgressToNext, level.Xp - start, end - start);
        }

        // Health and mana in the status block: what the last fight left, plus the rest since (they refill with time).
        private void RefreshVitals()
        {
            _room.SetVitals(_ctx.Battle.Hp, _ctx.Battle.MaxHp, _ctx.Battle.Mp, _ctx.Battle.MaxMp);
            _room.Pet.SetWornOut(!_ctx.Battle.CanFight && !_room.PetAway);   // under a tenth of its health the cat lies down until it has rested
        }

        // The face the cat wears at home: content, idling. There is no emotion system behind it (dropped 2026-09-21).
        private const EmotionType CalmFace = EmotionType.Satisfaction;

        // The chip after the level: what kind of fighter the build adds up to, in its style's colour.
        private void RefreshLeaning(bool animate)
        {
            var leaning = _ctx.Battle.Leaning;
            _room.SetLeaning(leaning.Name, BattleMiniGame.StyleColor(leaning.Style), animate);
        }

        // The four camp cells: what each does (a helper changes the numbers), and where it stands right now.
        private void RefreshCamp()
        {
            foreach (var pair in _campCells)
            {
                var def = CampSystem.Def(pair.Key);
                bool recovers = def.RecoverHp > 0f || def.RecoverMp > 0f;
                string effect = recovers
                    ? $"{(def.RecoverHp > 0f ? "HP" : "MP")} +{Mathf.RoundToInt(_ctx.Camp.RecoveryShare(pair.Key) * 100f)}%"
                    : $"{def.Effect} · {_ctx.Camp.BuffLength(pair.Key)} BATTLES";
                pair.Value.Refresh(_ctx.Camp.RemainingCooldown(pair.Key), effect, _ctx.Camp.Charges(pair.Key), _ctx.Camp.WouldHelp(pair.Key));
            }
        }

        private void RefreshTreat() => _room.SetTreat(_ctx.Camp.TreatCooldownRemaining);

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
            float width = UIFactory.FitWalletBox(_coins, _gems, coins + " COINS", gems + " HEARTS");
            _walletElement.preferredWidth = width;
            _walletElement.minWidth = width;
        }

        private void Update()
        {
            if (_ctx == null || _room == null || _miniGames == null) return;
            _wishTimer += Time.deltaTime;
            if (_wishTimer >= 0.5f) { _wishTimer = 0f; RefreshTreat(); RefreshCamp(); RefreshVitals(); }
            if (_annoyedUntil > 0f && Time.time >= _annoyedUntil) { _annoyedUntil = 0f; _room.Pet.SetFace(CalmFace, true); }   // the huff wears off
            _sceneTimer += Time.deltaTime;
            if (_sceneTimer >= 20f) { _sceneTimer = 0f; ApplyScene(); }   // the default room repaints itself when its ten-minute bucket turns
        }

        private void OnCamp(CampAction action)
        {
            int hpBefore = _ctx.Battle.Hp, mpBefore = _ctx.Battle.Mp;
            if (!_ctx.Camp.TryPerform(action)) return;
            switch (action)
            {
                case CampAction.Rest:
                    _room.Pet.Play(Creature.OneShot.Yawn);
                    _room.FloatText($"+{_ctx.Battle.Hp - hpBefore} HP", BattleMiniGame.HpLine);
                    break;
                case CampAction.Focus:
                    _room.Pet.Play(Creature.OneShot.Nod);
                    _room.FloatText($"+{_ctx.Battle.Mp - mpBefore} MP", BattleMiniGame.MpLine);
                    break;
                case CampAction.Feed:
                    _room.Pet.Play(Creature.OneShot.Eat);
                    _room.FloatText($"Fed! ATK +10% · {_ctx.Camp.Charges(action)} battles", UIFactory.PinkDark);
                    break;
                default:
                    _room.Pet.Play(Creature.OneShot.Groom);
                    _room.FloatText($"Groomed! DEF +10% · {_ctx.Camp.Charges(action)} battles", UIFactory.PinkDark);
                    break;
            }
            RefreshVitals();
            RefreshCamp();
        }

        // The Treat: the chip at the end of the status line. A snack that gives back a little health and mana.
        private void OnTreat()
        {
            if (!_ctx.Camp.TryTreat())
            {
                if (_ctx.Camp.TreatCooldownRemaining > 0f) ShowToast($"{_ctx.Data.petName} is still chewing. Another treat in {Mathf.CeilToInt(_ctx.Camp.TreatCooldownRemaining)} s.");
                return;
            }
            _room.Pet.Play(Creature.OneShot.Eat);
            _room.FloatText("Yum!  +HP +MP", UIFactory.PinkDark);
            RefreshVitals();
            RefreshTreat();
        }

        // A tap gets a body reaction and a word. What taps build up is patience running out. Every tap adds to a "poke heat" that cools
        // by one point every 2.5 s; past AnnoyedHeat the pet turns irritated, and past LeaveHeat it may walk off
        // the screen for a few seconds before it comes back.
        private static readonly Dictionary<PetPart, string[]> TapWords = new Dictionary<PetPart, string[]>
        {
            { PetPart.Head, new[] { "Purr~", "Mmm...", "That's the spot" } },
            { PetPart.Body, new[] { "Hehe!", "Boop!", "Again!" } },
            { PetPart.Paws, new[] { "High five!", "Shake!", "Paw!" } },
            { PetPart.Tail, new[] { "Hey!", "Not the tail!", "Hmph." } },
        };
        private static readonly string[] AnnoyedWords = { "Enough!", "Stop it.", "Grr...", "Too much!" };
        private const float AnnoyedHeat = 8f, LeaveHeat = 12f, HeatCooling = 2.5f, AwaySeconds = 5f;
        private float _pokeHeat, _pokeHeatTime, _annoyedUntil;

        private void OnPetTap(PetPart part)
        {
            if (_room.PetAway) return;
            _pokeHeat = Mathf.Max(0f, _pokeHeat - (Time.time - _pokeHeatTime) / HeatCooling);
            _pokeHeatTime = Time.time;
            _pokeHeat += part == PetPart.Tail ? 2f : 1f;   // the tail is touchy

            if (_pokeHeat >= LeaveHeat && UnityEngine.Random.value < 0.5f)
            {
                _room.FloatText("Hmph!", UIFactory.Ink);
                _room.Pet.SetFace(EmotionType.Irritation, true);
                _annoyedUntil = Time.time + AwaySeconds + 8f;
                _pokeHeat = AnnoyedHeat * 0.5f;                // comes back calmer, but not forgetful
                _room.StormOff(AwaySeconds);
                return;
            }

            bool annoyed = _pokeHeat >= AnnoyedHeat;
            _room.Boop(part, !annoyed);
            if (annoyed)
            {
                if (_annoyedUntil <= 0f) _room.Pet.SetFace(EmotionType.Irritation, true);
                _annoyedUntil = Time.time + 8f;
                _room.FloatText(AnnoyedWords[UnityEngine.Random.Range(0, AnnoyedWords.Length)], UIFactory.Ink);
                return;
            }

            var words = TapWords[part];
            _room.FloatText(words[UnityEngine.Random.Range(0, words.Length)], part == PetPart.Tail ? UIFactory.Ink : UIFactory.PinkDark);
        }

        // Dev/QA hook: taps the pet `count` times at once, as an impatient player would.
        public void DebugPoke(int count) { for (int i = 0; i < count; i++) OnPetTap(PetPart.Body); }

        private void TogglePanel(GameObject panel)
        {
            bool show = !panel.activeSelf;
            _dialog.Hide();
            InfoTooltip.Hide();
            if (show && panel == _shopPanel.Root) _shopPanel.Refresh();
            if (show && panel == _battleClub.Root) _battleClub.Refresh();
            if (show && panel == _market.Root) _market.Refresh();
            if (show && _campaignPanel != null && panel == _campaignPanel.Root) _campaignPanel.Refresh();
            _battleClub.Root.SetActive(false);
            _market.Root.SetActive(false);
            _campaignPanel?.Root.SetActive(false);
            _shopPanel.Root.SetActive(false);
            _settingsPanel.Root.SetActive(false);
            _storyPanel.Root.SetActive(false);
            _leaderboardPanel.Root.SetActive(false);
            _newsPanel.Root.SetActive(false);
            panel.SetActive(show);
            if (show) StartCoroutine(SimpleTween.PopIn(panel.transform.GetChild(1), 0.22f));
        }

        // Starts a fight: `encounter` is a wild cat or an area boss from the campaign, null a ranked Battle Club match.
        // Two things can stop it: no fighting style yet, or a worn-out cat (under a tenth of its health).
        private void StartBattle(BattleEncounter encounter)
        {
            if (!_ctx.Battle.HasStyle) { ShowBattleClubPage(BattleClubPanelView.ClubPage); ShowToast("Pick a fighting style first."); return; }
            if (!_ctx.Battle.CanFight) { ShowToast($"{_ctx.Data.petName} is worn out. Rest first."); return; }
            _battleClub.Root.SetActive(false);
            _market.Root.SetActive(false);
            _campaignPanel?.Root.SetActive(false);
            _shopPanel.Root.SetActive(false);
            InfoTooltip.Hide();
            MiniGameContext.Species = _ctx.Data.species;
            MiniGameContext.PetName = _ctx.Data.petName;
            MiniGameContext.Level = _ctx.Level.Level;
            MiniGameContext.Battle = _ctx.Battle;
            MiniGameContext.Encounter = encounter;
            _lastBattleWild = encounter != null;
            _miniGames.Open(MiniGameRegistry.For(SkillBranch.PvP));
        }

        // Ranked battles are tiered by league (the Wild sets its own levels per area).
        private MiniGameDifficulty DifficultyFor(SkillBranch branch)
        {
            int tier = _ctx.Battle.LeagueIndex + 1;
            return new MiniGameDifficulty { Tier = tier, Intensity = (tier - 1) / 4f, RewardMultiplier = _ctx.Battle.League.CoinMultiplier };
        }


        private void HandleMiniGameResult(MiniGameResult result)
        {
            _onMiniGameResult?.Invoke(result);
            RefreshVitals();
            if (_lastBattleWild && _campaignPanel != null) _campaignPanel.SetLog(result.Lines != null && result.Lines.Count > 1 ? result.Lines[0] : result.Won ? "The way ahead is clear." : "");
        }

        public void ShowBattleClub() { if (!_battleClub.Root.activeSelf) TogglePanel(_battleClub.Root); else _battleClub.Refresh(); }
        // Dev/QA hooks: the Treat chip and a care button, as a tap would.
        public void DebugTreat() => OnTreat();
        public void DebugCamp(CampAction action) => OnCamp(action);

        // Dev/QA hook: back to the bare home screen.
        public void CloseAllPanels()
        {
            _campaignPanel?.Root.SetActive(false);
            foreach (var panel in new[] { _battleClub.Root, _market.Root, _shopPanel.Root, _settingsPanel.Root, _storyPanel.Root, _leaderboardPanel.Root, _newsPanel.Root }) panel.SetActive(false);
            _dialog.Hide();
            InfoTooltip.Hide();
        }
        public void ShowBattleClubPage(int page) { ShowBattleClub(); _battleClub.ShowPage(page); }
        public void ShowMarket(int page = 0) { if (!_market.Root.activeSelf) TogglePanel(_market.Root); else _market.Refresh(); _market.ShowPage(page); }
        public void ShowCampaign() { if (_campaignPanel == null) return; if (!_campaignPanel.Root.activeSelf) TogglePanel(_campaignPanel.Root); else _campaignPanel.Refresh(); }
        public CampaignPanelView CampaignPanel => _campaignPanel;
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
        public void ShowBattle() => StartBattle(null);
        public void CloseMiniGame() => _miniGames.Close();

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
