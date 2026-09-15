using System;
using System.Collections.Generic;
using Gotchi.Data;
using Gotchi.MiniGames;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    public class SkillTreePanelView
    {
        private class Row
        {
            public SkillBranch Branch;
            public RectTransform Fill;
            public Text Xp;
            public Button Play;
            public Image Card;
            public bool Playable;
        }

        public static Color BranchColor(SkillBranch branch) => BranchColors[(int)branch % BranchColors.Length];

        private static readonly Color[] BranchColors =
        {
            UIFactory.Coral, UIFactory.Sky, UIFactory.Hex("FFB3B3"), UIFactory.Mint,
            UIFactory.Lavender, UIFactory.Pink, UIFactory.Butter,
        };

        private readonly SkillTreeSystem _skills;
        private readonly NeedsSystem _needs;
        private readonly Text _header;
        private readonly Text _subheader;
        private readonly List<Row> _rows = new List<Row>();

        public readonly GameObject Root;

        public SkillTreePanelView(Transform parent, SkillTreeSystem skills, NeedsSystem needs, Action<SkillBranch> onPlay, Action onClose, MonoBehaviour host)
        {
            _skills = skills;
            _needs = needs;

            var root = UIFactory.CreateRect("SkillTreeRoot", parent);
            Root = root.gameObject;
            var scrim = UIFactory.CreatePanel("Scrim", root, UIFactory.Scrim);
            UIFactory.Fill(scrim.rectTransform);
            scrim.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());

            var card = UIFactory.CreateCard("Panel", root, UIFactory.PanelBlue);
            UIFactory.Fill((RectTransform)card.transform.parent, 28f, 28f, 120f, 140f);
            UIFactory.AddVerticalLayout(card.gameObject, UIFactory.Spacing.List, new RectOffset(24, 24, 28, 24));

            _header = UIFactory.CreateText("Header", card.transform, "Skills", 44, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.SetPreferredHeight(_header.gameObject, 54f);
            _subheader = UIFactory.CreateText("Sub", card.transform, "", 26, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            UIFactory.SetPreferredHeight(_subheader.gameObject, 40f);
            var hint = UIFactory.CreateText("Hint", card.transform, "A branch rests while the needs it uses are below 30.", 22, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            UIFactory.SetPreferredHeight(hint.gameObject, 34f);

            int index = 0;
            foreach (SkillBranch branch in Enum.GetValues(typeof(SkillBranch)))
            {
                var info = MiniGameRegistry.For(branch);
                Color color = BranchColors[index++ % BranchColors.Length];

                var rowCard = UIFactory.CreateCard(branch + "Row", card.transform, UIFactory.Card, 0.8f);
                UIFactory.SetPreferredHeight(rowCard.transform.parent.gameObject, 134f);
                var row = rowCard.rectTransform;

                var icon = UIFactory.CreateIcon(UIFactory.BranchIcon(branch), row, 54f, UIFactory.Card);
                UIFactory.Place(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, -27f), new Vector2(70f, 27f));

                var name = UIFactory.CreateText("Name", row, UIFactory.PrettyName(branch.ToString()), 30, UIFactory.Ink, TextAnchor.MiddleLeft, true);
                UIFactory.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(84f, -4f), new Vector2(-200f, -10f));

                var xp = UIFactory.CreateText("Xp", row, "", 27, UIFactory.Muted, TextAnchor.MiddleLeft);
                UIFactory.Place(xp.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(84f, 8f), new Vector2(-200f, 10f));

                var track = UIFactory.CreatePillBar("Bar", row, color, out RectTransform fill);
                UIFactory.Place(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(84f, -6f), new Vector2(-200f, 6f));

                SkillBranch captured = branch;
                bool playable = info != null && info.Implemented;
                var play = UIFactory.CreateButton("Play", row, playable ? "Play" : "Soon", playable ? color : UIFactory.Hex("EEEAF0"), () => onPlay(captured), 28, host, playable ? UIFactory.Ink : UIFactory.Muted);
                UIFactory.Place((RectTransform)play.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-180f, -30f), new Vector2(-18f, 30f));

                _rows.Add(new Row { Branch = branch, Fill = fill, Xp = xp, Play = play, Card = rowCard, Playable = playable });
            }

            var backHolder = UIFactory.CreateRect("BackHolder", card.transform);
            UIFactory.SetPreferredHeight(backHolder.gameObject, 76f);
            var back = UIFactory.CreateButton("Back", backHolder, "Back", UIFactory.Card, onClose, 30, host);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-200f, 0f), new Vector2(200f, 0f));
            UIFactory.FitToLabel(back);

            Refresh();
        }

        public void Refresh()
        {
            string branchName = UIFactory.PrettyName(_skills.EvolutionBranch.ToString());
            string lockState = _skills.IsLocked ? "locked in" : "leading";
            _subheader.text = $"Evolution stage {_skills.EvolutionStage}/{SkillTreeSystem.MaxStage} · {branchName} path ({lockState})";
            foreach (var row in _rows)
            {
                float progress = _skills.XpIntoCurrentStage(row.Branch) / (float)SkillTreeSystem.XpPerStage;
                row.Fill.anchorMax = new Vector2(Mathf.Max(0.02f, progress), 1f);
                row.Xp.text = $"{_skills.GetXp(row.Branch)} XP";
                bool available = SkillGate.IsAvailable(row.Branch, _needs, out _);
                row.Card.color = available ? UIFactory.Card : UIFactory.Hex("F1ECF2");
                row.Play.interactable = row.Playable && available;
                UIFactory.SetButtonLabel(row.Play, !row.Playable ? "Soon" : available ? "Play" : "Later");
            }
        }
    }
}
