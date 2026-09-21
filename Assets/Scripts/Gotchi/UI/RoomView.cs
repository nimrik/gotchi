using System;
using System.Collections;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // The room: a background set (RoomScenes) behind, rug and fairy lights, the pet in front, status bar below.
    public class RoomView
    {
        private readonly MonoBehaviour _host;
        private readonly Image _moodChip;
        private readonly Text _moodText;
        private readonly RectTransform _petAnchor;
        private readonly Image _wishChip;
        private readonly Text _wishText;
        private readonly Action _onTreat;
        private SegmentedBar _hpBar, _mpBar;
        private Text _hpText, _mpText;
        private readonly Text _nameText;
        private readonly Text _xpText;
        private readonly RectTransform _levelTrack;
        private float _wishWidth = 200f;
        private readonly string _petName;
        private readonly Image _rug;
        private readonly Image _rugInner;
        private readonly RectTransform _fairyLights;
        private readonly RectTransform _sceneLayer;
        private string _sceneId;
        private int _sceneBucket = -1;
        private readonly RectTransform _levelFill;
        private RectTransform _wishIcon;
        private UIFactory.IconKind? _wishIconKind;

        public const float StatusHeight = 136f;     // two rows: the name line (72) and health + mana (52), plus the frame
        private const float Row1 = -40f;            // centre of the name line, down from the top of the status box

        public readonly RectTransform Root;
        public readonly PetPortraitView Pet;

        // `statusInfo` supplies the title and body of the info box that opens when the XP bar or the mood chip is pressed.
        public RoomView(Transform parent, Transform sceneParent, string petName, SpeciesType species, Action onTreat, Action<PetPart> onPetTap, MonoBehaviour host, Func<(string title, string body)> statusInfo = null)
        {
            _host = host;
            _onTreat = onTreat;
            Root = UIFactory.CreateRect("Room", parent);

            // Background set (floor, window/hills, sky props) — painted by RoomScenes into the full-screen
            // scene root so it runs seamlessly to the screen edges; swapped from the shop.
            _sceneLayer = UIFactory.CreateRect("Scene", sceneParent);
            UIFactory.Fill(_sceneLayer);

            var rug = UIFactory.CreateCircle("Rug", Root, UIFactory.Hex("FFD9E3"), 640f);
            _rug = rug;
            UIFactory.Place(rug.rectTransform, new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.24f), new Vector2(-320f, -320f), new Vector2(320f, 320f));
            rug.rectTransform.localScale = new Vector3(1f, 0.3f, 1f);
            rug.raycastTarget = false;
            var rugInner = UIFactory.CreateCircle("RugInner", rug.transform, UIFactory.Hex("FFC4D6"), 440f);
            rugInner.raycastTarget = false;
            _rugInner = rugInner;

            _fairyLights = UIFactory.CreateRect("FairyLights", Root);
            UIFactory.Place(_fairyLights, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -60f), new Vector2(-40f, -20f));
            for (int i = 0; i < 9; i++)
            {
                float t = (i + 0.5f) / 9f;
                var bulb = UIFactory.CreateCircle("Bulb", _fairyLights, i % 3 == 0 ? UIFactory.Butter : i % 3 == 1 ? UIFactory.Pink : UIFactory.Sky, 22f);
                UIFactory.Place(bulb.rectTransform, new Vector2(t, 0.5f), new Vector2(t, 0.5f), new Vector2(-11f, -11f + (i % 2) * 12f), new Vector2(11f, 11f + (i % 2) * 12f));
                bulb.raycastTarget = false;
                host.StartCoroutine(RoomScenes.Twinkle(bulb, 1.2f + i * 0.17f));
            }
            _fairyLights.gameObject.SetActive(false);

            _petAnchor = UIFactory.CreateRect("PetAnchor", Root);
            UIFactory.Place(_petAnchor, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), Vector2.zero, Vector2.zero);
            Pet = new PetPortraitView(_petAnchor, species, host, 440f);
            Pet.EnableTouch(part => onPetTap?.Invoke(part));
            Pet.AllowWander = true;

            // Status block, two rows. Row 1: name and level, the LEANING chip (what kind of fighter the build adds up
            // to: Fighter, Guardian, Shadow ... in the style's colour; it replaced the mood chip when emotions were
            // dropped on 2026-09-21), the XP bar with its "104 / 220 XP" caption, and the Treat button on the right.
            // Row 2: HEALTH and MANA as slanted block bars, one block per 250 points, as the last fight left them and
            // as resting brings them back, so the home screen always says whether the cat is fit to fight.
            var status = UIFactory.CreateCard("Status", Root, new Color(1f, 1f, 1f, 0.95f), 0.9f);
            UIFactory.Place((RectTransform)status.transform.parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, StatusHeight));
            _petName = petName;
            _nameText = UIFactory.CreateText("Name", status.transform, petName, 32, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            _nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_nameText.rectTransform, new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(UIFactory.Spacing.Pad, Row1 - 32f), new Vector2(0f, Row1 + 32f));
            // XP to the next level: bar + caption on the same line as the name, right after the mood chip (LayoutNameLine).
            var levelTrack = UIFactory.CreatePillBar("LevelBar", status.transform, UIFactory.Butter, out _levelFill);
            _levelTrack = levelTrack.rectTransform;
            UIFactory.Place(_levelTrack, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(260f, Row1 - 8f), new Vector2(-500f, Row1 + 8f));
            _xpText = UIFactory.CreateText("Xp", status.transform, "", 18, UIFactory.Muted, TextAnchor.MiddleRight);
            _xpText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_xpText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-640f, Row1 - 14f), new Vector2(-360f, Row1 + 14f));

            // Leaning: a chip in the style's colour, right after "Name · Lv N" (laid out in LayoutNameLine).
            _moodChip = UIFactory.CreatePill("Mood", status.transform, UIFactory.Butter);
            UIFactory.Place(_moodChip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(240f, Row1 - 22f), new Vector2(400f, Row1 + 22f));
            _moodText = UIFactory.CreatePixelText("Text", _moodChip.transform, "", 22, UIFactory.MenuInk, TextAnchor.MiddleCenter, false);
            _moodText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _moodText.raycastTarget = false;
            UIFactory.Fill(_moodText.rectTransform);
            // Pressing the XP bar (a generous hit area around the thin bar) or the leaning chip opens the block's full info.
            var infoHit = UIFactory.CreatePanel("InfoTap", levelTrack.transform, Color.clear);
            UIFactory.Place(infoHit.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6f, -30f), new Vector2(6f, 30f));
            if (statusInfo != null)
            {
                UIFactory.MakePressable(infoHit, () => { var info = statusInfo(); InfoTooltip.Toggle(_levelTrack, info.title, info.body, host); });
                UIFactory.MakePressable(_moodChip, () => { var info = statusInfo(); InfoTooltip.Toggle(_moodChip.rectTransform, info.title, info.body, host); });
            }
            else { infoHit.raycastTarget = false; _moodChip.raycastTarget = false; }
            _wishChip = UIFactory.CreatePill("Wish", status.transform, UIFactory.Butter);
            UIFactory.Place(_wishChip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-330f, Row1 - 30f), new Vector2(-14f, Row1 + 30f));
            UIFactory.MakePressable(_wishChip, () => _onTreat?.Invoke());
            var wishDisc = UIFactory.CreateCircle("Disc", _wishChip.transform, Color.white, 38f);
            UIFactory.Place(wishDisc.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -19f), new Vector2(56f, 19f));
            wishDisc.raycastTarget = false;
            _wishText = UIFactory.CreateText("Text", _wishChip.transform, "", 24, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Place(_wishText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(66f, 0f), new Vector2(-26f, 0f));

            // Row 2: health on the left half, mana on the right, each "HP ▰▰▰▰▱ 1000/1200".
            _hpBar = VitalBar(status.transform, "HP", MiniGames.BattleMiniGame.HpFill, MiniGames.BattleMiniGame.HpLine, 0f, out _hpText);
            _mpBar = VitalBar(status.transform, "MP", MiniGames.BattleMiniGame.MpFill, MiniGames.BattleMiniGame.MpLine, 0.5f, out _mpText);
        }

        private static SegmentedBar VitalBar(Transform parent, string label, Color fill, Color line, float from, out Text value)
        {
            const float y = 34f;   // centre of row 2, up from the bottom of the status box
            float left = from == 0f ? UIFactory.Spacing.Pad : 12f;
            var name = UIFactory.CreatePixelText(label, parent, label, 22, line, TextAnchor.MiddleLeft, false);
            UIFactory.Place(name.rectTransform, new Vector2(from, 0f), new Vector2(from, 0f), new Vector2(left, y - 16f), new Vector2(left + 44f, y + 16f));
            // The numbers follow the bar directly ("HP ▰▰▰▰▱ 1000/1200"): a slot wide enough for "1100/1100", read from its left.
            float right = from == 0f ? 12f : UIFactory.Spacing.Pad;
            const float numbers = 122f;
            var bar = SegmentedBar.Create(label + "Bar", parent, fill, line);
            bar.Skew = 12f;
            UIFactory.Place(bar.rectTransform, new Vector2(from, 0f), new Vector2(from + 0.5f, 0f), new Vector2(left + 50f, y - 13f), new Vector2(-right - numbers - 8f, y + 13f));
            value = UIFactory.CreatePixelText(label + "Value", parent, "", 22, UIFactory.MenuInk, TextAnchor.MiddleLeft, false);
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(value.rectTransform, new Vector2(from + 0.5f, 0f), new Vector2(from + 0.5f, 0f), new Vector2(-right - numbers, y - 16f), new Vector2(-right, y + 16f));
            return bar;
        }

        // Health and mana as they stand: what the last fight left plus the rest since.
        public void SetVitals(int hp, int maxHp, int mp, int maxMp)
        {
            bool low = hp * 5 <= maxHp;
            _hpBar.SetColors(low ? MiniGames.BattleMiniGame.HpLowFill : MiniGames.BattleMiniGame.HpFill, low ? MiniGames.BattleMiniGame.HpLowLine : MiniGames.BattleMiniGame.HpLine);
            _hpBar.Set(hp, maxHp);
            _mpBar.Set(mp, maxMp);
            _hpText.text = $"{hp}/{maxHp}";
            _mpText.text = $"{mp}/{maxMp}";
        }

        // Dev/QA hook: opens the status info box as a press on the XP bar would.
        public RectTransform LevelBar => _levelTrack;

        // The chip after the level: what the cat leans to, in its style's colour. A label, nothing more.
        public void SetLeaning(string name, Color color, bool animate)
        {
            string text = name.ToUpperInvariant();
            bool changed = _moodText.text != text;
            _moodText.text = text;
            _moodText.color = UIFactory.LabelColorFor(color);
            if (animate && changed)
            {
                _host.StartCoroutine(SimpleTween.ColorTo(_moodChip, color, 0.3f));
                _host.StartCoroutine(SimpleTween.PunchScale(_moodChip.rectTransform, 0.12f, 0.25f));
            }
            else _moodChip.color = color;
            LayoutNameLine();
        }

        // The chip on the right is the TREAT button (fish icon): a snack that gives back a little health and mana,
        // then waits out its cooldown ("Treat · 42s"). It used to ask for the lowest need; the needs are gone.
        public void SetTreat(float treatCooldown)
        {
            UIFactory.IconKind icon = UIFactory.IconKind.Fish;
            string text = treatCooldown > 0f ? $"Treat · {Mathf.CeilToInt(treatCooldown)}s" : "Treat";
            Color color = treatCooldown > 0f ? UIFactory.Hex("FFF1D2") : UIFactory.Butter;
            _wishText.text = text;
            _wishChip.color = color;
            _wishText.color = UIFactory.LabelColorFor(color);
            float width = 66f + _wishText.preferredWidth + 40f;
            _wishChip.rectTransform.offsetMin = new Vector2(-14f - width, Row1 - 30f);
            if (!Mathf.Approximately(width, _wishWidth)) { _wishWidth = width; LayoutNameLine(); }
            if (_wishIconKind == icon) return;
            _wishIconKind = icon;
            if (_wishIcon != null) UnityEngine.Object.Destroy(_wishIcon.gameObject);
            _wishIcon = UIFactory.CreateIcon(icon, _wishChip.transform, 26f, Color.white);
            UIFactory.Place(_wishIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, -13f), new Vector2(50f, 13f));
        }

        public void ApplyRoom(string rugId, bool fairyLights, string backgroundId)
        {
            // The default room follows the local time, so it is repainted when its ten-minute bucket changes.
            int bucket = backgroundId == RoomScenes.DefaultId || RoomScenes.Find(backgroundId).Id == RoomScenes.DefaultId ? RoomScenes.CozyBucket(RoomScenes.LocalNow()) : -1;
            if (_sceneId != backgroundId || bucket != _sceneBucket)
            {
                _sceneId = backgroundId;
                _sceneBucket = bucket;
                RoomScenes.Apply(_sceneLayer, RoomScenes.Find(backgroundId), _host);
            }
            switch (rugId)
            {
                case "rug_mint": _rug.color = UIFactory.Hex("D9F3E3"); _rugInner.color = UIFactory.Hex("BFE9D0"); break;
                case "rug_sky": _rug.color = UIFactory.Hex("D9EEFF"); _rugInner.color = UIFactory.Hex("BFE0FF"); break;
                default: _rug.color = UIFactory.Hex("FFD9E3"); _rugInner.color = UIFactory.Hex("FFC4D6"); break;
            }
            _fairyLights.gameObject.SetActive(fairyLights);
        }

        // `xpInto` of `xpSpan` experience points into the current level; a span of 0 means the top level.
        public void SetLevel(int level, float progress, int xpInto, int xpSpan)
        {
            _nameText.text = $"{_petName} · Lv {level}";
            _levelFill.anchorMax = new Vector2(Mathf.Max(0.03f, progress), 1f);
            _xpText.text = xpSpan > 0 ? $"{xpInto} / {xpSpan} XP" : "MAX";   // experience, not a percentage (asked 2026-09-21)
            LayoutNameLine();
        }

        // Name line: "Name · Lv N" | leaning chip | XP bar stretched over the free width | "104 / 220 XP" | wish chip.
        // When a long name leaves too little room, the caption goes first (the info box still has the numbers).
        private void LayoutNameLine()
        {
            float chipLeft = UIFactory.Spacing.Pad + _nameText.preferredWidth + 14f;
            float chipWidth = _moodText.preferredWidth + 36f;
            _moodChip.rectTransform.offsetMin = new Vector2(chipLeft, Row1 - 22f);
            _moodChip.rectTransform.offsetMax = new Vector2(chipLeft + chipWidth, Row1 + 22f);

            float left = chipLeft + chipWidth + 16f;
            float right = 14f + _wishWidth + 16f;                     // wish chip + gaps
            float cardWidth = Root.rect.width > 1f ? Root.rect.width : 1032f;
            float captionWidth = _xpText.preferredWidth + 4f;
            bool showCaption = cardWidth - left - right - captionWidth - 12f >= 90f;
            _xpText.gameObject.SetActive(showCaption);
            if (!showCaption) captionWidth = -12f;
            _xpText.rectTransform.offsetMin = new Vector2(-right - captionWidth, Row1 - 14f);
            _xpText.rectTransform.offsetMax = new Vector2(-right, Row1 + 14f);
            _levelTrack.offsetMin = new Vector2(left, Row1 - 8f);
            _levelTrack.offsetMax = new Vector2(-right - captionWidth - 12f, Row1 + 8f);
            _levelTrack.gameObject.SetActive(cardWidth - left - right - captionWidth - 12f >= 40f);
        }

        public void FloatText(string text, Color color)
        {
            var label = UIFactory.CreateText("Float", _petAnchor, text, 40, color, TextAnchor.MiddleCenter, true);
            label.rectTransform.sizeDelta = new Vector2(300f, 60f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 150f);
            _host.StartCoroutine(FloatAway(label.rectTransform, 1.1f));
        }

        public void Celebrate()
        {
            Pet.Celebrate();
            _host.StartCoroutine(HeartBurst());
        }

        // Reaction to the player tapping the pet. Hearts only while the pet still enjoys it.
        public void Boop(PetPart part, bool happy = true)
        {
            Pet.React(part);
            if (happy && part != PetPart.Tail) _host.StartCoroutine(HeartBurst());
        }

        // Poked once too often: the pet walks off the screen, stays away for `awaySeconds` and comes back.
        public bool PetAway { get; private set; }

        public void StormOff(float awaySeconds, Action onBack = null)
        {
            if (PetAway) return;
            _host.StartCoroutine(StormOffRoutine(awaySeconds, onBack));
        }

        private IEnumerator StormOffRoutine(float awaySeconds, Action onBack)
        {
            PetAway = true;
            float direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float exitX = direction * (Root.rect.width * 0.5f + 360f);   // the whole pet, shadow and overlays past the screen edge
            Pet.March(direction, 1.8f);
            yield return SlidePet(0f, exitX, 1.5f);
            yield return new WaitForSeconds(awaySeconds);
            Pet.March(-direction, 1.3f);
            yield return SlidePet(exitX, 0f, 2.0f);
            Pet.StopMarch();
            PetAway = false;
            onBack?.Invoke();
        }

        private IEnumerator SlidePet(float fromX, float toX, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && _petAnchor != null)
            {
                elapsed += Time.deltaTime;
                _petAnchor.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, Mathf.SmoothStep(0f, 1f, elapsed / duration)), 0f);
                yield return null;
            }
            if (_petAnchor != null) _petAnchor.anchoredPosition = new Vector2(toX, 0f);
        }

        private IEnumerator HeartBurst()
        {
            for (int i = 0; i < 3; i++)
            {
                var heart = UIFactory.CreateIcon(UIFactory.IconKind.Heart, _petAnchor, 44f + i * 6f, Color.clear);
                heart.anchoredPosition = new Vector2(-60f + i * 60f, 200f);
                _host.StartCoroutine(FloatAway(heart, 0.9f + i * 0.15f));
                yield return new WaitForSeconds(0.08f);
            }
        }

        private static IEnumerator FloatAway(RectTransform heart, float duration)
        {
            var graphics = heart.GetComponentsInChildren<Graphic>(true);
            var self = heart.GetComponent<Graphic>();
            if (graphics.Length == 0 && self != null) graphics = new[] { self };
            Vector2 start = heart.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration && heart != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                heart.anchoredPosition = start + new Vector2(Mathf.Sin(t * 6f) * 14f, t * 160f);
                float alpha = 1f - SimpleTween.EaseOutCubic(t);
                foreach (var g in graphics) { var c = g.color; c.a = alpha; g.color = c; }
                yield return null;
            }
            if (heart != null) UnityEngine.Object.Destroy(heart.gameObject);
        }
    }
}
