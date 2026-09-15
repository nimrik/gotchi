using System;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    public class LeaderboardPanelView
    {
        private readonly GameContext _ctx;
        private readonly MonoBehaviour _host;
        private readonly RectTransform _list;
        private readonly Text _title;
        private readonly RectTransform _profileRoot;
        private readonly RectTransform _profileContent;
        private int _tab;
        private readonly TabBarView _boards;

        public readonly GameObject Root;

        public LeaderboardPanelView(Transform parent, GameContext ctx, Action onClose, MonoBehaviour host)
        {
            _ctx = ctx;
            _host = host;
            var root = UIFactory.CreateRect("LeaderboardRoot", parent);
            Root = root.gameObject;
            var scrim = UIFactory.CreatePanel("Scrim", root, UIFactory.Scrim);
            UIFactory.Fill(scrim.rectTransform);
            scrim.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());

            var card = UIFactory.CreateCard("Panel", root, UIFactory.PanelBlue);
            UIFactory.Fill((RectTransform)card.transform.parent, 28f, 28f, 100f, 120f);
            _title = UIFactory.CreatePixelText("Header", card.transform, "LEADERBOARD", 52, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            UIFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -84f), new Vector2(0f, -24f));

            // One ◀ BOARD ▶ pager (same component as the shop): Levels first, then one board per branch.
            var tabs = new List<TabBarView.Tab> { new TabBarView.Tab("Levels", UIFactory.IconKind.Trophy) };
            foreach (SkillBranch branch in Enum.GetValues(typeof(SkillBranch)))
                tabs.Add(new TabBarView.Tab(branch == SkillBranch.ExplorerAdventure ? "Explorer" : branch == SkillBranch.PvP ? "PvP" : branch.ToString(), UIFactory.BranchIcon(branch)));
            _boards = TabBarView.Arrows("Boards", card.transform, tabs.ToArray(), host);
            UIFactory.Place(_boards.Root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -172f), new Vector2(-24f, -100f));
            _boards.OnSelected += index => { _tab = index; Refresh(); };
            _boards.Select(0, false);

            _list = UIFactory.CreateScrollColumn("List", card.transform, UIFactory.Spacing.List, new RectOffset(24, 24, 4, 4));
            UIFactory.Place((RectTransform)_list.parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 110f), new Vector2(0f, -190f));

            var back = UIFactory.CreateButton("Back", card.transform, "Back", UIFactory.Card, onClose, 30, host);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 96f));
            UIFactory.FitToLabel(back);

            _profileRoot = UIFactory.CreateRect("ProfileRoot", root);
            UIFactory.Fill(_profileRoot);
            var profileScrim = UIFactory.CreatePanel("Scrim", _profileRoot, UIFactory.Scrim);
            UIFactory.Fill(profileScrim.rectTransform);
            profileScrim.gameObject.AddComponent<Button>().onClick.AddListener(() => _profileRoot.gameObject.SetActive(false));
            var profileCard = UIFactory.CreateCard("Profile", _profileRoot, UIFactory.Cream);
            UIFactory.Place((RectTransform)profileCard.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460f, -560f), new Vector2(460f, 560f));
            _profileContent = profileCard.rectTransform;
            _profileRoot.gameObject.SetActive(false);
        }

        public void ShowSkillsTab() => _boards.Select(1);

        public void Refresh()
        {
            bool level = _tab == 0;
            SkillBranch branch = level ? SkillBranch.Sport : (SkillBranch)(_tab - 1);

            for (int i = _list.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_list.GetChild(i).gameObject);
            var kind = level ? LeaderboardKind.Level : LeaderboardKind.Skill;
            // You first (with your true rank), a divider, then the top of the board without you.
            var everyone = _ctx.Leaderboards.Top(kind, branch, int.MaxValue);
            var me = everyone.Find(e => e.IsLocal);
            if (me != null)
            {
                Row(me, level);
                var divider = UIFactory.CreateRect("Divider", _list);
                UIFactory.SetPreferredHeight(divider.gameObject, 30f);
                var line = UIFactory.CreatePanel("Line", divider, UIFactory.FrameDark);
                UIFactory.Place(line.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(8f, -2f), new Vector2(-8f, 2f));
                line.raycastTarget = false;
                var label = UIFactory.CreatePixelText("Label", divider, "TOP 25", 16, UIFactory.MenuInk, TextAnchor.MiddleCenter);
                UIFactory.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-60f, -12f), new Vector2(60f, 12f));
                var labelBg = UIFactory.CreatePanel("Bg", divider, UIFactory.PanelBlue);
                UIFactory.Place(labelBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-52f, -12f), new Vector2(52f, 12f));
                labelBg.raycastTarget = false;
                labelBg.transform.SetSiblingIndex(1);
            }
            int shown = 0;
            foreach (var entry in everyone)
            {
                if (entry.IsLocal || shown >= 25) continue;
                Row(entry, level);
                shown++;
            }
        }

        private void Row(LeaderboardEntry entry, bool level)
        {
            LeaderboardEntry captured = entry;
            var row = UIFactory.CreateCard("Entry", _list, entry.IsLocal ? UIFactory.Hex("FFE1EA") : UIFactory.Card, 0.8f);
            UIFactory.SetPreferredHeight(row.transform.parent.gameObject, 92f);
            var rank = UIFactory.CreateText("Rank", row.transform, "#" + entry.Rank, 28, entry.Rank <= 3 ? UIFactory.PinkDark : UIFactory.Muted, TextAnchor.MiddleCenter, true);
            UIFactory.Place(rank.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(10f, 0f), new Vector2(90f, 0f));
            var name = UIFactory.CreateText("Name", row.transform, entry.DisplayName + (entry.IsLocal ? " (you)" : ""), 26, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(96f, -2f), new Vector2(-190f, -8f));
            var pet = UIFactory.CreateText("Pet", row.transform, $"{entry.PetName} the {UIFactory.PrettyName(entry.Species.ToString())}", 20, UIFactory.Muted, TextAnchor.MiddleLeft);
            UIFactory.Place(pet.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(96f, 8f), new Vector2(-190f, 2f));
            var value = UIFactory.CreateText("Value", row.transform, level ? "Lv " + entry.Value : entry.Value + " XP", 26, UIFactory.Ink, TextAnchor.MiddleRight, true);
            UIFactory.Place(value.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-180f, 0f), new Vector2(-20f, 0f));
            UIFactory.MakePressable(row, () => ShowProfile(captured.PlayerId));
        }

        private void ShowProfile(string playerId)
        {
            var profile = _ctx.Leaderboards.Profile(playerId);
            if (profile == null) return;
            for (int i = _profileContent.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_profileContent.GetChild(i).gameObject);

            var preview = UIFactory.CreateRect("Preview", _profileContent);
            UIFactory.Place(preview, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(0f, -250f));
            new PetPortraitView(preview, profile.Species, _host, 240f).SetEmotion(EmotionType.Joy, false);

            var name = UIFactory.CreateText("Name", _profileContent, profile.DisplayName, 40, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -500f), new Vector2(-20f, -440f));
            var pet = UIFactory.CreateText("Pet", _profileContent, $"{profile.PetName} the {UIFactory.PrettyName(profile.Species.ToString())} · Lv {profile.Level} · day {profile.StreakDays} streak", 24, UIFactory.Muted, TextAnchor.MiddleCenter);
            UIFactory.Place(pet.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -540f), new Vector2(-20f, -500f));
            var evo = UIFactory.CreateText("Evo", _profileContent, $"Stage {profile.EvolutionStage}/{SkillTreeSystem.MaxStage} on the {UIFactory.PrettyName(profile.EvolutionBranch.ToString())} path", 24, UIFactory.PinkDark, TextAnchor.MiddleCenter, true);
            UIFactory.Place(evo.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -580f), new Vector2(-20f, -540f));

            float y = -620f;
            foreach (SkillBranch branch in Enum.GetValues(typeof(SkillBranch)))
            {
                var label = UIFactory.CreateText("B", _profileContent, UIFactory.BranchShortName(branch), 22, UIFactory.Ink, TextAnchor.MiddleLeft, true);
                UIFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, y - 40f), new Vector2(220f, y));
                var track = UIFactory.CreatePillBar("Bar", _profileContent, SkillTreePanelView.BranchColor(branch), out RectTransform fill);
                UIFactory.Place(track.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, y - 28f), new Vector2(-150f, y - 12f));
                fill.anchorMax = new Vector2(Mathf.Clamp(profile.BranchXp[(int)branch] / 1500f, 0.03f, 1f), 1f);
                var xp = UIFactory.CreateText("Xp", _profileContent, profile.BranchXp[(int)branch] + " XP", 20, UIFactory.Muted, TextAnchor.MiddleRight);
                UIFactory.Place(xp.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-140f, y - 40f), new Vector2(-30f, y));
                y -= 50f;
            }

            var close = UIFactory.CreateButton("Close", _profileContent, "Back", UIFactory.Card, () => _profileRoot.gameObject.SetActive(false), 28, _host);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 24f), new Vector2(160f, 92f));
            UIFactory.FitToLabel(close);

            _profileRoot.gameObject.SetActive(true);
            _host.StartCoroutine(SimpleTween.PopIn(_profileContent.parent, 0.25f));
        }
    }
}
