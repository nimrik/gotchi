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
        private readonly Text _bubbleText;
        private readonly Image _bubbleDot;
        private readonly RectTransform _bubble;
        private readonly RectTransform _petAnchor;
        private readonly Image _wishChip;
        private readonly Text _wishText;
        private readonly Action<CareAction> _onWish;
        private readonly Action _onCuddle;
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
        private readonly RectTransform _levelFill;
        private bool _cuddleMode;
        private RectTransform _wishIcon;
        private UIFactory.IconKind? _wishIconKind;
        private CareAction? _wishAction;

        public readonly RectTransform Root;
        public readonly PetPortraitView Pet;

        public RoomView(Transform parent, Transform sceneParent, string petName, SpeciesType species, Action<CareAction> onWish, Action onCuddle, Action onOpenStory, Action<PetPart> onPetTap, MonoBehaviour host)
        {
            _host = host;
            _onWish = onWish;
            _onCuddle = onCuddle;
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

            // Status bar: name + species on the left, a tappable "wish" chip on the right.
            var status = UIFactory.CreateCard("Status", Root, new Color(1f, 1f, 1f, 0.95f), 0.9f);
            UIFactory.Place((RectTransform)status.transform.parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 104f));
            _petName = petName;
            _nameText = UIFactory.CreateText("Name", status.transform, petName, 32, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            _nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.55f, 1f), new Vector2(UIFactory.Spacing.Pad, 0f), new Vector2(0f, -8f));
            var speciesText = UIFactory.CreateText("Species", status.transform, UIFactory.PrettyName(species.ToString()) + " · tap for story", 20, UIFactory.Muted, TextAnchor.MiddleLeft);
            UIFactory.Place(speciesText.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 0.5f), new Vector2(UIFactory.Spacing.Pad, 22f), new Vector2(0f, 4f));
            // XP to the next level: bar + caption on the same line as the name, right after "Lv N" (positioned in SetLevel).
            var levelTrack = UIFactory.CreatePillBar("LevelBar", status.transform, UIFactory.Butter, out _levelFill);
            _levelTrack = levelTrack.rectTransform;
            UIFactory.Place(_levelTrack, new Vector2(0f, 0.71f), new Vector2(1f, 0.71f), new Vector2(260f, -8f), new Vector2(-500f, 8f));
            _xpText = UIFactory.CreateText("Xp", status.transform, "", 18, UIFactory.Muted, TextAnchor.MiddleRight);
            _xpText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Place(_xpText.rectTransform, new Vector2(1f, 0.71f), new Vector2(1f, 0.71f), new Vector2(-640f, -14f), new Vector2(-360f, 14f));
            var storyHit = UIFactory.CreatePanel("StoryTap", status.transform, Color.clear);
            UIFactory.Place(storyHit.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);
            UIFactory.MakePressable(storyHit, () => onOpenStory?.Invoke());
            _wishChip = UIFactory.CreatePill("Wish", status.transform, UIFactory.Butter);
            UIFactory.Place(_wishChip.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-330f, -34f), new Vector2(-16f, 34f));
            UIFactory.MakePressable(_wishChip, () =>
            {
                if (_cuddleMode) _onCuddle?.Invoke();
                else if (_wishAction.HasValue) _onWish?.Invoke(_wishAction.Value);
            });
            var wishDisc = UIFactory.CreateCircle("Disc", _wishChip.transform, Color.white, 38f);
            UIFactory.Place(wishDisc.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -19f), new Vector2(56f, 19f));
            wishDisc.raycastTarget = false;
            _wishText = UIFactory.CreateText("Text", _wishChip.transform, "", 24, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Place(_wishText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(66f, 0f), new Vector2(-26f, 0f));

            // Mood bubble: a speech box beside the pet's head with a tail cut into its border, pointing at the head.
            // The tail is two rotated squares (anti-aliased): a dark one behind the box, a white one inside it.
            var tailOutline = UIFactory.CreateRounded("BubbleTail", Root, UIFactory.FrameDark, 0.25f);
            UIFactory.Place(tailOutline.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(70f, 60f), new Vector2(106f, 96f));
            tailOutline.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tailOutline.raycastTarget = false;
            var bubble = UIFactory.CreateFrame("Bubble", Root, Color.white);
            bubble.raycastTarget = false;
            _bubble = bubble.rectTransform;
            UIFactory.Place(_bubble, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(50f, 78f), new Vector2(390f, 158f));
            var tailFill = UIFactory.CreateRounded("TailFill", _bubble, Color.white, 0.25f);
            UIFactory.Place(tailFill.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(26f, -8f), new Vector2(50f, 16f));
            tailFill.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tailFill.raycastTarget = false;
            _bubbleDot = UIFactory.CreateCircle("Dot", _bubble, UIFactory.Butter, 24f);
            UIFactory.Place(_bubbleDot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, -12f), new Vector2(50f, 12f));
            _bubbleDot.raycastTarget = false;
            _bubbleText = UIFactory.CreatePixelText("Text", _bubble, "", 27, UIFactory.MenuInk, TextAnchor.MiddleLeft);
            UIFactory.Fill(_bubbleText.rectTransform, 62f, 16f, 0f, 0f);
        }

        public void SetEmotion(EmotionType emotion, bool animate)
        {
            Pet.SetEmotion(emotion, animate);
            _bubbleText.text = UIFactory.PrettyName(emotion.ToString());
            Color mood = PetPortraitView.MoodColor(EmotionCatalog.GetCategory(emotion));
            if (animate)
            {
                _host.StartCoroutine(SimpleTween.ColorTo(_bubbleDot, mood, 0.3f));
                _host.StartCoroutine(SimpleTween.PunchScale(_bubble, 0.12f, 0.25f));
            }
            else _bubbleDot.color = mood;
        }

        public void SetWish(NeedType lowest, float value, bool canCuddle, float cuddleCooldown)
        {
            UIFactory.IconKind icon;
            string text;
            Color color;
            _cuddleMode = false;
            if (value >= 70f)
            {
                _cuddleMode = canCuddle;
                icon = UIFactory.IconKind.Heart;
                text = canCuddle ? "Cuddle!" : cuddleCooldown > 0f ? $"Cozy · {Mathf.CeilToInt(cuddleCooldown)}s" : "All cozy!";
                color = canCuddle ? UIFactory.Primary : UIFactory.Hex("FFE1EA"); _wishAction = null;
            }
            else
            {
                switch (lowest)
                {
                    case NeedType.Hunger: icon = UIFactory.IconKind.Cookie; text = "Snack time?"; color = UIFactory.Hex("FFDCCB"); _wishAction = CareAction.Feed; break;
                    case NeedType.Hygiene: icon = UIFactory.IconKind.Shower; text = "Bath time?"; color = UIFactory.Hex("D9EEFF"); _wishAction = CareAction.Clean; break;
                    case NeedType.Energy: icon = UIFactory.IconKind.Moon; text = "Nap time?"; color = UIFactory.Hex("E6DDFF"); _wishAction = CareAction.Rest; break;
                    default: icon = UIFactory.IconKind.Ball; text = "Play time?"; color = UIFactory.Hex("FFD9E5"); _wishAction = CareAction.Play; break;
                }
            }
            _wishText.text = text;
            _wishChip.color = color;
            _wishText.color = UIFactory.LabelColorFor(color);
            float width = 66f + _wishText.preferredWidth + 40f;
            _wishChip.rectTransform.offsetMin = new Vector2(-16f - width, -34f);
            if (!Mathf.Approximately(width, _wishWidth)) { _wishWidth = width; LayoutNameLine(); }
            if (_wishIconKind == icon) return;
            _wishIconKind = icon;
            if (_wishIcon != null) UnityEngine.Object.Destroy(_wishIcon.gameObject);
            _wishIcon = UIFactory.CreateIcon(icon, _wishChip.transform, 26f, Color.white);
            UIFactory.Place(_wishIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, -13f), new Vector2(50f, 13f));
        }

        public void ApplyRoom(string rugId, bool fairyLights, string backgroundId)
        {
            if (_sceneId != backgroundId)
            {
                _sceneId = backgroundId;
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

        public void SetLevel(int level, float progress)
        {
            _nameText.text = $"{_petName} · Lv {level}";
            _levelFill.anchorMax = new Vector2(Mathf.Max(0.03f, progress), 1f);
            _xpText.text = $"{Mathf.RoundToInt(progress * 100f)}% to Lv {level + 1}";
            LayoutNameLine();
        }

        // Name line: "Name · Lv N" | XP bar stretched over the free width | "80% to Lv 5" | wish chip.
        private void LayoutNameLine()
        {
            float left = UIFactory.Spacing.Pad + _nameText.preferredWidth + 18f;
            float captionWidth = _xpText.preferredWidth + 4f;
            float right = 16f + _wishWidth + 16f;                     // wish chip + gaps
            _xpText.rectTransform.offsetMin = new Vector2(-right - captionWidth, -14f);
            _xpText.rectTransform.offsetMax = new Vector2(-right, 14f);
            _levelTrack.offsetMin = new Vector2(left, -8f);
            _levelTrack.offsetMax = new Vector2(-right - captionWidth - 12f, 8f);
        }

        public void SetConditions(float hunger, float hygiene, float energy, float happiness) =>
            Pet.SetConditions(hunger, hygiene, energy, happiness);

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

        // Reaction to the player tapping the pet.
        public void Boop(PetPart part)
        {
            Pet.React(part);
            if (part != PetPart.Tail) _host.StartCoroutine(HeartBurst());
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
