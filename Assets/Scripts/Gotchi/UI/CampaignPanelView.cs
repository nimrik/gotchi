using System;
using System.Collections;
using Gotchi.Core;
using Gotchi.Creature3D;
using Gotchi.Data;
using Gotchi.MiniGames;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // The Wild: the campaign (rules in Systems/CampaignSystem.cs). With no expedition under way it is the map: the
    // areas in order, each open once the one before it is cleared. On an expedition it is the trail: the steps, the
    // cat's health and moves as they carry from fight to fight, what the bag can do about them, SEARCH and GO HOME.
    public class CampaignPanelView
    {
        private readonly GameContext _ctx;
        private readonly CampaignSystem _campaign;
        private readonly BattleSystem _battle;
        private readonly Action<string> _toast;
        private readonly Action<BattleEncounter> _onEncounter;
        private readonly Action _onNeedStyle;
        private readonly MonoBehaviour _host;
        private readonly PagedPanel _panel;
        private string _log = "";
        private bool _dirty;

        public GameObject Root => _panel.Root;

        public CampaignPanelView(Transform parent, GameContext ctx, Action<string> toast, Action<BattleEncounter> onEncounter, Action onNeedStyle, Action onClose, MonoBehaviour host)
        {
            _ctx = ctx;
            _campaign = ctx.Campaign;
            _battle = ctx.Battle;
            _toast = toast;
            _onEncounter = onEncounter;
            _onNeedStyle = onNeedStyle;
            _host = host;
            _panel = new PagedPanel(parent, "CampaignRoot", "The Wild", new[] { "Wild" }, null, ctx, onClose, host);
            _campaign.OnChanged += MarkDirty;
            _battle.OnChanged += MarkDirty;
            Refresh();
        }

        private void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            if (Root.activeInHierarchy) _host.StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            if (_dirty) Refresh();
        }

        // What the last fight or search had to say; shown at the top of the trail.
        public void SetLog(string text) { _log = text ?? ""; }

        public void Refresh()
        {
            _dirty = false;
            _panel.Rebuild(lists =>
            {
                if (!_battle.HasStyle) BuildNeedStyle(lists[0]);
                else if (_campaign.ExpeditionActive) BuildTrail(lists[0]);
                else BuildMap(lists[0]);
            });
        }

        private void BuildNeedStyle(RectTransform list)
        {
            PanelRows.Note(list, "The Wild is full of cats that fight back. Pick a fighting style in the Battle Club first; it is free and comes with moves and treats.", 120f);
            PanelRows.Primary(list, "To the Club", UIFactory.Pink, true, () => _onNeedStyle?.Invoke(), _host);
        }

        // ---------------------------------------------------------------- the map

        private void BuildMap(RectTransform list)
        {
            PanelRows.Note(list, "Search an area step by step. Health and moves carry over from fight to fight, so pack the bag. Going home keeps everything found; beating the boss opens the next area.", 124f);
            if (!string.IsNullOrEmpty(_log)) PanelRows.Note(list, _log, 56f);

            foreach (var area in CampaignSystem.Areas)
            {
                WildAreaDef captured = area;
                bool open = _campaign.IsUnlocked(area), cleared = _campaign.IsCleared(area);
                var box = PanelRows.Box(list, area.Id, 176f, open ? Color.white : PanelRows.Locked);
                BattleClubPanelView.ArenaSwatch(box, area.Scenery);
                PanelRows.Pixel(box, area.Name.ToUpperInvariant(), 28, UIFactory.MenuInk, 116f, -16f, PanelRows.TextRight, 38f);
                PanelRows.Body(box, $"Wild cats Lv {area.MinLevel}-{area.MaxLevel} · boss {area.BossName} Lv {area.BossLevel} · {area.Steps.Length} steps", 21, UIFactory.Ink, 116f, -58f, PanelRows.TextRight, 28f);
                PanelRows.Body(box, area.Description, 21, UIFactory.Muted, 116f, -88f, PanelRows.TextRight, 52f);
                PanelRows.Body(box, cleared ? $"Cleared. Each fight pays {area.WildCoins} coins, the boss {area.RepeatCoins}." : $"First clear: {area.ClearCoins} coins and {area.ClearHearts} hearts.", 21, cleared ? PanelRows.Good : UIFactory.PinkDark, 24f, -140f, PanelRows.TextRight, 28f);
                PanelRows.ActionButton(box, !open ? "Locked" : cleared ? "Again" : "Set out", open ? UIFactory.Pink : PanelRows.Off, open, () =>
                {
                    if (_campaign.Begin(captured, out string message)) { _log = $"{_ctx.Data.petName} sets out into the {captured.Name}."; Refresh(); }
                    else _toast(message);
                }, _host);
            }
        }

        // ---------------------------------------------------------------- the trail

        private void BuildTrail(RectTransform list)
        {
            var area = _campaign.CurrentArea;
            var next = _campaign.NextStep;

            // Where we are: the steps as a row of markers.
            var trailBox = PanelRows.Box(list, "Trail", 190f, Color.white);
            PanelRows.Pixel(trailBox, area.Name.ToUpperInvariant(), 30, UIFactory.MenuInk, 24f, -16f, -24f, 40f);
            PanelRows.Body(trailBox, $"Step {Mathf.Min(_campaign.Step + 1, area.Steps.Length)} of {area.Steps.Length} · wild cats Lv {area.MinLevel}-{area.MaxLevel} · boss {area.BossName} Lv {area.BossLevel}", 21, UIFactory.Muted, 24f, -58f, -24f, 28f);
            float slot = 1f / area.Steps.Length;
            for (int i = 0; i < area.Steps.Length; i++)
            {
                var step = area.Steps[i];
                bool done = i < _campaign.Step, current = i == _campaign.Step;
                Color color = done ? UIFactory.Mint : step.Kind == WildStepKind.Boss ? UIFactory.Coral : step.Kind == WildStepKind.Find ? UIFactory.Butter : UIFactory.MenuGrey;
                float size = current ? 56f : 40f;
                var marker = UIFactory.CreateCircle("Step" + i, trailBox, color, size);
                UIFactory.Place(marker.rectTransform, new Vector2((i + 0.5f) * slot, 0f), new Vector2((i + 0.5f) * slot, 0f), new Vector2(-size * 0.5f, 52f - size * 0.5f), new Vector2(size * 0.5f, 52f + size * 0.5f));
                marker.raycastTarget = false;
                var letter = UIFactory.CreatePixelText("Kind", marker.transform, done ? "OK" : step.Kind == WildStepKind.Boss ? "B" : step.Kind == WildStepKind.Find ? "?" : "!", current ? 24 : 18, UIFactory.MenuInk, TextAnchor.MiddleCenter, false);
                UIFactory.Fill(letter.rectTransform);
            }

            if (!string.IsNullOrEmpty(_log)) PanelRows.Note(list, _log, 60f);

            // The cat: what carries over.
            int hp = _campaign.Hp, maxHp = _campaign.MaxHp;
            var catBox = PanelRows.Box(list, "Cat", 150f, Color.white);
            PanelRows.Pixel(catBox, $"{_ctx.Data.petName.ToUpperInvariant()} · HP {hp}/{maxHp}", 30, UIFactory.MenuInk, 24f, -16f, -200f, 40f);
            PanelRows.Chip(catBox, _battle.Style.ToString().ToUpperInvariant(), BattleMiniGame.StyleColor(_battle.Style), -24f, -18f, 150f);
            bool low = hp * 5 <= maxHp;
            var bar = SegmentedBar.Create("Hp", catBox, low ? BattleMiniGame.HpLowFill : BattleMiniGame.HpFill, low ? BattleMiniGame.HpLowLine : BattleMiniGame.HpLine);
            UIFactory.Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -86f), new Vector2(-24f, -64f));
            bar.Set(hp, maxHp);
            PanelRows.Body(catBox, $"Mana {_battle.Mp}/{_battle.MaxMp}. Health and mana stay as each fight leaves them. Treats, tuna and warm milk work between fights.", 21, UIFactory.Muted, 24f, -96f, -24f, 52f);

            // Search, or face the boss.
            string label = next == null ? "Trail done" : next.Kind == WildStepKind.Boss ? $"Face {area.BossName}" : "Search";
            PanelRows.Primary(list, label, UIFactory.Pink, next != null, OnSearch, _host, 520f);

            // The bag, for patching up between fights.
            PanelRows.Note(list, "Bag", 44f, true);
            foreach (var item in BattleSystem.Items)
            {
                if (item.Effect == BattleItemEffect.RaiseAttack) continue;   // catnip only works in a fight
                BattleItemDef captured = item;
                int count = _battle.Count(item.Id);
                var box = PanelRows.Box(list, item.Id, 100f, count > 0 ? Color.white : PanelRows.Locked);
                PanelRows.Pixel(box, $"{item.Name.ToUpperInvariant()}  x{count}", 26, UIFactory.MenuInk, 24f, -14f, PanelRows.TextRight, 36f);
                PanelRows.Body(box, item.Description, 21, UIFactory.Muted, 24f, -54f, PanelRows.TextRight, 30f);
                PanelRows.ActionButton(box, "Use", UIFactory.Mint, count > 0, () =>
                {
                    bool ok = _campaign.UseItem(captured, out string message);
                    if (ok) { _log = $"{captured.Name}: {message}"; Refresh(); } else _toast(message);
                }, _host);
            }

            PanelRows.Primary(list, "Go home", UIFactory.Card, true, () =>
            {
                _campaign.GoHome();
                _log = $"{_ctx.Data.petName} is back home. Everything found is in the bag.";
                Refresh();
            }, _host, 360f);
        }

        private void OnSearch()
        {
            var result = _campaign.Search(_ctx.Data.petName);
            _log = result.Text;
            if (result.Outcome == SearchOutcome.Encounter)
            {
                var encounter = _campaign.BuildEncounter();
                if (encounter != null) { _onEncounter?.Invoke(encounter); return; }
            }
            Refresh();
        }

        // Dev/QA hooks: drive the Wild as the buttons would.
        public void DebugBegin(string areaId) { if (_campaign.Begin(CampaignSystem.FindArea(areaId), out _)) { _log = "Setting out."; Refresh(); } }
        public void DebugSearch() => OnSearch();
    }
}
