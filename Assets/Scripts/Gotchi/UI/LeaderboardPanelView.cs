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
        private readonly Dictionary<int, Image> _tabs = new Dictionary<int, Image>();
        private readonly RectTransform _profileRoot;
        private readonly RectTransform _profileContent;
        private int _tab;

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

            var card = UIFactory.CreateCard("Panel", root, UIFactory.Cream);
            UIFactory.Fill((RectTransform)card.transform.parent, 28f, 28f, 100f, 120f);
            _title = UIFactory.CreateText("Header", card.transform, "Leaderboards", 44, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -84f), new Vector2(0f, -24f));

            var tabs = UIFactory.CreateRect("Tabs", card.transform);
            UIFactory.Place(tabs, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -190f), new Vector2(-20f, -96f));
            UIFactory.AddHorizontalLayout(tabs.gameObject, 8f, new RectOffset(0, 0, 0, 0), true);
            AddTab(tabs, 0, UIFactory.IconKind.Sparkle);
            foreach (SkillBranch branch in Enum.GetValues(typeof(SkillBranch)))
                AddTab(tabs, 1 + (int)branch, UIFactory.BranchIcon(branch));

            _list = UIFactory.CreateScrollColumn("List", card.transform, UIFactory.Spacing.List, new RectOffset(24, 24, 4, 4));
            UIFactory.Place((RectTransform)_list.parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 110f), new Vector2(0f, -200f));

            var back = UIFactory.CreateButton("Back", card.transform, "Back", UIFactory.Card, onClose, 30, host);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 96f));

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

        private void AddTab(Transform parent, int index, UIFactory.IconKind icon)
        {
            var cell = UIFactory.CreateRect("Tab" + index, parent);
            var button = UIFactory.CreateIconButton("Button", cell, UIFactory.Card, icon, 76f, () => { _tab = index; Refresh(); }, _host);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-38f, -38f), new Vector2(38f, 38f));
            _tabs[index] = button.image;
        }

        public void Refresh()
        {
            bool level = _tab == 0;
            SkillBranch branch = level ? SkillBranch.Sport : (SkillBranch)(_tab - 1);
            _title.text = level ? "Top levels" : $"Top {UIFactory.PrettyName(branch.ToString())}";
            foreach (var pair in _tabs) pair.Value.color = pair.Key == _tab ? UIFactory.Hex("FFE1EA") : UIFactory.Card;

            for (int i = _list.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_list.GetChild(i).gameObject);
            var entries = _ctx.Leaderboards.Top(level ? LeaderboardKind.Level : LeaderboardKind.Skill, branch, 25);
            foreach (var entry in entries)
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

            _profileRoot.gameObject.SetActive(true);
            _host.StartCoroutine(SimpleTween.PopIn(_profileContent.parent, 0.25f));
        }
    }
}
