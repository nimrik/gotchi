using System;
using System.Collections;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // Cozy procedural room: floor, window with sun and drifting cloud, plant, rug — the pet in front.
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
        private readonly Text _levelText;
        private readonly Image _rug;
        private readonly Image _rugInner;
        private readonly RectTransform _fairyLights;
        private readonly RectTransform _levelFill;
        private bool _cuddleMode;
        private RectTransform _wishIcon;
        private UIFactory.IconKind? _wishIconKind;
        private CareAction? _wishAction;

        public readonly RectTransform Root;
        public readonly PetPortraitView Pet;

        public RoomView(Transform parent, string petName, SpeciesType species, Action<CareAction> onWish, Action onCuddle, Action onOpenStory, Action onPetTap, MonoBehaviour host)
        {
            _host = host;
            _onWish = onWish;
            _onCuddle = onCuddle;
            Root = UIFactory.CreateRect("Room", parent);

            var floor = UIFactory.CreateRounded("Floor", Root, UIFactory.Hex("F7E6D6"), 1.6f);
            UIFactory.Place(floor.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.36f), new Vector2(-40f, -80f), new Vector2(40f, 0f));
            floor.raycastTarget = false;

            var frame = UIFactory.CreateRounded("WindowFrame", Root, Color.white, 1f);
            UIFactory.Place(frame.rectTransform, new Vector2(0.2f, 0.8f), new Vector2(0.2f, 0.8f), new Vector2(-150f, -130f), new Vector2(150f, 130f));
            frame.raycastTarget = false;
            var sky = UIFactory.CreateRounded("Sky", frame.transform, UIFactory.Hex("C9E7FF"), 0.8f);
            UIFactory.Fill(sky.rectTransform, 14f, 14f, 14f, 14f);
            sky.raycastTarget = false;
            var sun = UIFactory.CreateCircle("Sun", sky.transform, UIFactory.Butter, 76f);
            UIFactory.Place(sun.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-116f, -110f), new Vector2(-40f, -34f));
            sun.raycastTarget = false;
            var cloud = UIFactory.CreateRect("Cloud", sky.transform);
            UIFactory.Place(cloud, new Vector2(0.35f, 0.42f), new Vector2(0.35f, 0.42f), Vector2.zero, Vector2.zero);
            Puff(cloud, 0f, 8f, 64f);
            Puff(cloud, -34f, -6f, 48f);
            Puff(cloud, 34f, -4f, 52f);
            var barV = UIFactory.CreatePanel("BarV", sky.transform, Color.white);
            UIFactory.Place(barV.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-6f, 0f), new Vector2(6f, 0f));
            barV.raycastTarget = false;
            var barH = UIFactory.CreatePanel("BarH", sky.transform, Color.white);
            UIFactory.Place(barH.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -6f), new Vector2(0f, 6f));
            barH.raycastTarget = false;
            host.StartCoroutine(Drift(cloud, 26f, 7f));

            var plant = UIFactory.CreateRect("Plant", Root);
            UIFactory.Place(plant, new Vector2(0.86f, 0.4f), new Vector2(0.86f, 0.4f), Vector2.zero, Vector2.zero);
            var pot = UIFactory.CreateRounded("Pot", plant, UIFactory.Coral, 0.8f);
            pot.rectTransform.sizeDelta = new Vector2(120f, 110f);
            pot.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            pot.raycastTarget = false;
            var leaves = UIFactory.CreateRect("Leaves", plant);
            leaves.anchoredPosition = new Vector2(0f, 20f);
            Leaf(leaves, -30f, 60f, -28f);
            Leaf(leaves, 0f, 92f, 0f);
            Leaf(leaves, 30f, 60f, 28f);
            host.StartCoroutine(Sway(leaves, 4f, 3.2f));

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
                host.StartCoroutine(Twinkle(bulb, 1.2f + i * 0.17f));
            }
            _fairyLights.gameObject.SetActive(false);

            _petAnchor = UIFactory.CreateRect("PetAnchor", Root);
            UIFactory.Place(_petAnchor, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), Vector2.zero, Vector2.zero);
            Pet = new PetPortraitView(_petAnchor, species, host, 440f);
            var petHit = UIFactory.CreatePanel("PetTap", _petAnchor, Color.clear);
            petHit.rectTransform.sizeDelta = new Vector2(460f, 500f);
            petHit.rectTransform.anchoredPosition = new Vector2(0f, -20f);
            var petButton = petHit.gameObject.AddComponent<Button>();
            petButton.targetGraphic = petHit;
            UIFactory.ApplyTransition(petButton, false);
            petButton.onClick.AddListener(() => onPetTap?.Invoke());

            // Status bar: name + species on the left, a tappable "wish" chip on the right.
            var status = UIFactory.CreateCard("Status", Root, new Color(1f, 1f, 1f, 0.95f), 0.9f);
            UIFactory.Place((RectTransform)status.transform.parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 104f));
            var name = UIFactory.CreateText("Name", status.transform, petName, 32, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.55f, 1f), new Vector2(UIFactory.Spacing.Pad, 0f), new Vector2(0f, -8f));
            var levelPill = UIFactory.CreatePill("Level", status.transform, UIFactory.Hex("FFF0C2"));
            UIFactory.Place(levelPill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(148f, 6f), new Vector2(248f, -10f));
            levelPill.raycastTarget = false;
            _levelText = UIFactory.CreateText("Text", levelPill.transform, "Lv 1", 22, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(_levelText.rectTransform);
            var speciesText = UIFactory.CreateText("Species", status.transform, UIFactory.PrettyName(species.ToString()) + " · tap for story", 20, UIFactory.Muted, TextAnchor.MiddleLeft);
            UIFactory.Place(speciesText.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 0.5f), new Vector2(UIFactory.Spacing.Pad, 22f), new Vector2(0f, 4f));
            var levelTrack = UIFactory.CreatePillBar("LevelBar", status.transform, UIFactory.Butter, out _levelFill);
            UIFactory.Place(levelTrack.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 0f), new Vector2(UIFactory.Spacing.Pad, 10f), new Vector2(0f, 20f));
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
            var wishDisc = UIFactory.CreateCircle("Disc", _wishChip.transform, Color.white, 52f);
            UIFactory.Place(wishDisc.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -26f), new Vector2(62f, 26f));
            wishDisc.raycastTarget = false;
            _wishText = UIFactory.CreateText("Text", _wishChip.transform, "", 24, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Place(_wishText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(72f, 0f), new Vector2(-12f, 0f));

            var bubble = UIFactory.CreateCard("Bubble", Root, Color.white, 0.9f);
            _bubble = (RectTransform)bubble.transform.parent;
            UIFactory.Place(_bubble, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(150f, 150f), new Vector2(480f, 240f));
            var tail = UIFactory.CreateCircle("Tail", _bubble, Color.white, 30f);
            UIFactory.Place(tail.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, -18f), new Vector2(40f, 12f));
            tail.raycastTarget = false;
            _bubbleDot = UIFactory.CreateCircle("Dot", bubble.transform, UIFactory.Butter, 26f);
            UIFactory.Place(_bubbleDot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, -13f), new Vector2(48f, 13f));
            _bubbleDot.raycastTarget = false;
            _bubbleText = UIFactory.CreateText("Text", bubble.transform, "", 28, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Fill(_bubbleText.rectTransform, 52f, 16f, 0f, 0f);
        }

        private static void Puff(Transform parent, float x, float y, float size)
        {
            var puff = UIFactory.CreateCircle("Puff", parent, Color.white, size);
            puff.rectTransform.anchoredPosition = new Vector2(x, y);
            puff.raycastTarget = false;
        }

        private static void Leaf(Transform parent, float x, float y, float tilt)
        {
            var leaf = UIFactory.CreateCircle("Leaf", parent, UIFactory.Hex("9ED9B5"), 96f);
            leaf.rectTransform.anchoredPosition = new Vector2(x, y);
            leaf.rectTransform.localScale = new Vector3(0.62f, 1.15f, 1f);
            leaf.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            leaf.raycastTarget = false;
        }

        private static IEnumerator Drift(RectTransform target, float amplitude, float period)
        {
            Vector2 origin = target.anchoredPosition;
            float time = 0f;
            while (target != null)
            {
                time += Time.deltaTime;
                target.anchoredPosition = origin + new Vector2(Mathf.Sin(time / period * Mathf.PI * 2f) * amplitude, 0f);
                yield return null;
            }
        }

        private static IEnumerator Sway(RectTransform target, float degrees, float period)
        {
            float time = 0f;
            while (target != null)
            {
                time += Time.deltaTime;
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time / period * Mathf.PI * 2f) * degrees);
                yield return null;
            }
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
                    case NeedType.Hygiene: icon = UIFactory.IconKind.Bubbles; text = "Bath time?"; color = UIFactory.Hex("D9EEFF"); _wishAction = CareAction.Clean; break;
                    case NeedType.Energy: icon = UIFactory.IconKind.Moon; text = "Nap time?"; color = UIFactory.Hex("E6DDFF"); _wishAction = CareAction.Rest; break;
                    default: icon = UIFactory.IconKind.Ball; text = "Play time?"; color = UIFactory.Hex("FFD9E5"); _wishAction = CareAction.Play; break;
                }
            }
            _wishText.text = text;
            _wishChip.color = color;
            _wishText.color = UIFactory.LabelColorFor(color);
            float width = 72f + _wishText.preferredWidth + 24f;
            _wishChip.rectTransform.offsetMin = new Vector2(-16f - width, -34f);
            if (_wishIconKind == icon) return;
            _wishIconKind = icon;
            if (_wishIcon != null) UnityEngine.Object.Destroy(_wishIcon.gameObject);
            _wishIcon = UIFactory.CreateIcon(icon, _wishChip.transform, 36f, Color.white);
            UIFactory.Place(_wishIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -18f), new Vector2(54f, 18f));
        }

        public void ApplyRoom(string rugId, bool fairyLights)
        {
            switch (rugId)
            {
                case "rug_mint": _rug.color = UIFactory.Hex("D9F3E3"); _rugInner.color = UIFactory.Hex("BFE9D0"); break;
                case "rug_sky": _rug.color = UIFactory.Hex("D9EEFF"); _rugInner.color = UIFactory.Hex("BFE0FF"); break;
                default: _rug.color = UIFactory.Hex("FFD9E3"); _rugInner.color = UIFactory.Hex("FFC4D6"); break;
            }
            _fairyLights.gameObject.SetActive(fairyLights);
        }

        private static IEnumerator Twinkle(Image bulb, float period)
        {
            Color baseColor = bulb.color;
            float time = 0f;
            while (bulb != null)
            {
                time += Time.deltaTime;
                var c = baseColor; c.a = 0.55f + 0.45f * Mathf.Sin(time / period * Mathf.PI * 2f);
                bulb.color = c;
                yield return null;
            }
        }

        public void SetLevel(int level, float progress)
        {
            _levelText.text = "Lv " + level;
            _levelFill.anchorMax = new Vector2(Mathf.Max(0.03f, progress), 1f);
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
        public void Boop()
        {
            Pet.Boop();
            _host.StartCoroutine(HeartBurst());
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
