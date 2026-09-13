using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // Procedural chibi placeholder: oversized outlined head on a small seated body with paws and a tail,
    // cel-shade band under the head, sparkle eyes, blush, and an expression per emotion category.
    public class PetPortraitView
    {
        private enum Ears { None, Round, Tall, Pointy, Tiny, Floppy }

        private struct Look
        {
            public Color Body, Belly, Ear, EarInner;
            public Ears Ears;
            public bool EyePatches;
            public bool Tail;
            public Look(string body, string belly, string ear, string earInner, Ears ears, bool patches = false, bool tail = false)
            {
                Body = UIFactory.Hex(body); Belly = UIFactory.Hex(belly); Ear = UIFactory.Hex(ear); EarInner = UIFactory.Hex(earInner);
                Ears = ears; EyePatches = patches; Tail = tail;
            }
        }

        private static readonly Dictionary<SpeciesType, Look> Looks = new Dictionary<SpeciesType, Look>
        {
            { SpeciesType.Bunny,    new Look("FBF3F6", "FFE6EE", "FBF3F6", "FFB7CB", Ears.Tall) },
            { SpeciesType.Cat,      new Look("FFC078", "FFF1DC", "FFC078", "FFB7CB", Ears.Pointy, false, true) },
            { SpeciesType.Panda,    new Look("FFFFFF", "F6F1F4", "3E3A45", "3E3A45", Ears.Round, true) },
            { SpeciesType.RedPanda, new Look("E8834F", "FFF1DC", "8C4A2F", "FFF1DC", Ears.Round, false, true) },
            { SpeciesType.Seal,     new Look("BCC7D3", "E9EEF3", "BCC7D3", "BCC7D3", Ears.None) },
            { SpeciesType.Raccoon,  new Look("A3ABB5", "E4E8EC", "6E7680", "E4E8EC", Ears.Pointy, true, true) },
            { SpeciesType.Penguin,  new Look("47536A", "FFFFFF", "47536A", "47536A", Ears.None) },
            { SpeciesType.Fennec,   new Look("F5DCB0", "FFF6E6", "F5DCB0", "FFC7D1", Ears.Tall, false, true) },
            { SpeciesType.Fox,      new Look("FF9B54", "FFF6E6", "FF9B54", "FFFFFF", Ears.Pointy, false, true) },
            { SpeciesType.Pig,      new Look("FFB6C1", "FFD3DA", "FFB6C1", "FF8FA8", Ears.Tiny) },
            { SpeciesType.Otter,    new Look("A8785A", "E3C8A6", "A8785A", "E3C8A6", Ears.Tiny, false, true) },
            { SpeciesType.Hedgehog, new Look("B69072", "FFF1DC", "8A6A50", "FFF1DC", Ears.Tiny) },
            { SpeciesType.Dog,      new Look("DDA86B", "FFF1DC", "B8834B", "FFD9B0", Ears.Floppy, false, true) },
        };

        private static readonly Dictionary<EmotionCategory, Color> Moods = new Dictionary<EmotionCategory, Color>
        {
            { EmotionCategory.Joy, UIFactory.Butter },
            { EmotionCategory.Sadness, UIFactory.Sky },
            { EmotionCategory.Anger, UIFactory.Coral },
            { EmotionCategory.Fear, UIFactory.Lavender },
            { EmotionCategory.Disgust, UIFactory.Mint },
            { EmotionCategory.Surprise, UIFactory.Hex("FFC9A3") },
            { EmotionCategory.GuiltAndShame, UIFactory.Hex("F5C6D0") },
            { EmotionCategory.ConnectionAndCare, UIFactory.Pink },
            { EmotionCategory.Vulnerability, UIFactory.Hex("D8DCE8") },
            { EmotionCategory.InterestAndAwe, UIFactory.Hex("B5EAF2") },
        };

        // Eye centres and radius for blink overlays, as fractions of the displayed sprite (from its centre).
        private static readonly Dictionary<SpeciesType, (Vector2 left, Vector2 right, float radius)> SpriteEyes = new Dictionary<SpeciesType, (Vector2, Vector2, float)>
        {
            { SpeciesType.Cat, (new Vector2(-0.16f, 0.07f), new Vector2(0.09f, 0.07f), 0.075f) },
            { SpeciesType.Seal, (new Vector2(-0.14f, 0.1f), new Vector2(0.12f, 0.1f), 0.07f) },
        };

        private static readonly Color Outline = UIFactory.Hex("3B2F45");
        private const float FaceScale = 0.8f;

        private readonly float Size;
        private readonly float OutlineWidth;
        private readonly MonoBehaviour _host;
        private readonly RectTransform _pet;
        private readonly RectTransform _face;
        private readonly Image _moodGlow;
        private readonly RectTransform _eyeL, _eyeR, _pupilL, _pupilR, _sparkleL, _sparkleR, _extraSparkle;
        private readonly Image _blushL, _blushR;
        private readonly RectTransform _mouth;
        private readonly Image _mouthImage;
        private readonly Image _mouthArc;
        private readonly RectTransform _mouthMask;
        private readonly RectTransform _happyEyeL, _happyEyeR;
        private readonly Image _tear, _sweat, _heart;
        private readonly RectTransform _browL, _browR;
        private readonly bool _spriteMode;
        private RectTransform _bagL, _bagR, _drool, _rainCloud, _thought;
        private readonly System.Collections.Generic.List<RectTransform> _dirt = new System.Collections.Generic.List<RectTransform>();
        private RectTransform _thoughtIcon;
        private RectTransform _headSlice;
        private Image _blinkL, _blinkR;
        private bool _hasEars;
        private readonly System.Collections.Generic.Dictionary<string, RectTransform> _accessories = new System.Collections.Generic.Dictionary<string, RectTransform>();
        private UIFactory.IconKind? _thoughtKind;
        private readonly Image _overlayHeart, _overlayTear, _overlaySweat, _overlayBang, _overlayZzz, _overlayTwinkle;

        public Transform BodyTransform => _pet;
        public RectTransform Root { get; }

        public static Color MoodColor(EmotionCategory category) => Moods[category];

        public PetPortraitView(Transform parent, SpeciesType species, MonoBehaviour host, float size)
        {
            _host = host;
            Size = size;
            OutlineWidth = size * 0.03f;
            Look look = Looks[species];

            Root = UIFactory.CreateRect("Creature", parent);
            Center(Root, 0f, 0f);
            Root.sizeDelta = new Vector2(Size * 1.5f, Size * 1.5f);

            _moodGlow = UIFactory.CreateCircle("MoodGlow", Root, UIFactory.Butter, Size * 1.35f);
            _moodGlow.raycastTarget = false;
            Center(_moodGlow.rectTransform, 0f, -6f);
            var glowColor = _moodGlow.color; glowColor.a = 0.45f; _moodGlow.color = glowColor;

            _pet = UIFactory.CreateRect("Pet", Root);
            Center(_pet, 0f, 0f);
            _pet.sizeDelta = new Vector2(Size, Size);

            var ground = UIFactory.CreateCircle("Ground", _pet, new Color(0.3f, 0.2f, 0.3f, 0.12f), Size * 0.9f);
            Center(ground.rectTransform, 0f, -Size * 0.7f);
            ground.rectTransform.localScale = new Vector3(1f, 0.22f, 1f);
            ground.raycastTarget = false;

            Sprite sprite = CreatureSprites.For(species);
            if (sprite != null)
            {
                _spriteMode = true;
                _hasEars = look.Ears != Ears.None;
                float box = Size * 1.25f;
                float aspect = sprite.rect.width / sprite.rect.height;
                float drawW = aspect >= 1f ? box : box * aspect;
                float drawH = aspect >= 1f ? box / aspect : box;
                float bottom = -Size * 0.1f - drawH / 2f;
                const float seam = 0.62f;

                var bodySprite = Sprite.Create(sprite.texture, new Rect(0f, 0f, sprite.rect.width, sprite.rect.height * seam), new Vector2(0.5f, 0f), 100f);
                var body = UIFactory.CreatePanel("SpriteBody", _pet, Color.white);
                body.sprite = bodySprite;
                body.raycastTarget = false;
                body.rectTransform.anchorMin = body.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                body.rectTransform.pivot = new Vector2(0.5f, 0f);
                body.rectTransform.sizeDelta = new Vector2(drawW, drawH * seam);
                body.rectTransform.anchoredPosition = new Vector2(0f, bottom);

                var headSprite = Sprite.Create(sprite.texture, new Rect(0f, sprite.rect.height * seam, sprite.rect.width, sprite.rect.height * (1f - seam)), new Vector2(0.5f, 0f), 100f);
                var head = UIFactory.CreatePanel("SpriteHead", _pet, Color.white);
                head.sprite = headSprite;
                head.raycastTarget = false;
                head.rectTransform.anchorMin = head.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                head.rectTransform.pivot = new Vector2(0.5f, 0f);
                head.rectTransform.sizeDelta = new Vector2(drawW, drawH * (1f - seam));
                head.rectTransform.anchoredPosition = new Vector2(0f, bottom + drawH * seam);
                _headSlice = head.rectTransform;

                if (SpriteEyes.TryGetValue(species, out var eyes))
                {
                    float cy = bottom + drawH / 2f;
                    _blinkL = UIFactory.CreateCircle("BlinkL", _pet, look.Body, eyes.radius * 2f * drawW);
                    Center(_blinkL.rectTransform, eyes.left.x * drawW, cy + eyes.left.y * drawH);
                    _blinkR = UIFactory.CreateCircle("BlinkR", _pet, look.Body, eyes.radius * 2f * drawW);
                    Center(_blinkR.rectTransform, eyes.right.x * drawW, cy + eyes.right.y * drawH);
                    foreach (var b in new[] { _blinkL, _blinkR }) { b.rectTransform.localScale = new Vector3(1f, 0.55f, 1f); b.raycastTarget = false; b.gameObject.SetActive(false); }
                }
                host.StartCoroutine(IdleLife());

                _overlayHeart = Overlay(UIFactory.IconKind.Heart, Size * 0.42f, Size * 0.4f, 60f);
                _overlayTwinkle = Overlay(UIFactory.IconKind.Sparkle, -Size * 0.44f, Size * 0.36f, 52f);
                _overlayTear = UIFactory.CreateCircle("Tear", _pet, UIFactory.Sky, 26f);
                Center(_overlayTear.rectTransform, -Size * 0.2f, Size * 0.02f);
                _overlayTear.rectTransform.localScale = new Vector3(0.7f, 1.3f, 1f);
                _overlaySweat = UIFactory.CreateCircle("Sweat", _pet, UIFactory.Sky, 28f);
                Center(_overlaySweat.rectTransform, Size * 0.4f, Size * 0.3f);
                _overlaySweat.rectTransform.localScale = new Vector3(0.7f, 1.3f, 1f);
                _overlayBang = UIFactory.CreateRounded("Bang", _pet, UIFactory.PinkDark, 0.4f);
                Center(_overlayBang.rectTransform, Size * 0.42f, Size * 0.42f);
                _overlayBang.rectTransform.sizeDelta = new Vector2(16f, 54f);
                _overlayZzz = UIFactory.CreateCircle("Zzz", _pet, UIFactory.Lavender, 30f);
                Center(_overlayZzz.rectTransform, Size * 0.44f, Size * 0.4f);
                foreach (var o in new[] { _overlayHeart, _overlayTwinkle, _overlayTear, _overlaySweat, _overlayBang, _overlayZzz }) o.raycastTarget = false;

                BuildConditions(-Size * 0.15f, 0.0f, -Size * 0.02f);
                BuildAccessories(Size * 0.42f, -Size * 0.14f);
                host.StartCoroutine(SimpleTween.Breathe(Root, 0.02f, 2.8f));
                return;
            }

            if (look.Tail)
                Outlined(_pet, look.Body, Size * 0.42f, Size * 0.4f, -Size * 0.5f, new Vector3(0.55f, 0.26f, 1f), 28f);

            Outlined(_pet, look.Body, Size * 0.7f, 0f, -Size * 0.38f, new Vector3(1f, 0.72f, 1f));
            var belly = UIFactory.CreateCircle("Belly", _pet, look.Belly, Size * 0.42f);
            Center(belly.rectTransform, 0f, -Size * 0.4f);
            belly.rectTransform.localScale = new Vector3(1f, 0.8f, 1f);
            belly.raycastTarget = false;
            Outlined(_pet, look.Body, Size * 0.2f, -Size * 0.2f, -Size * 0.58f, Vector3.one);
            Outlined(_pet, look.Body, Size * 0.2f, Size * 0.2f, -Size * 0.58f, Vector3.one);

            _face = UIFactory.CreateRect("Face", _pet);
            Center(_face, 0f, Size * 0.12f);
            _face.localScale = Vector3.one * FaceScale;

            BuildEars(look);
            Outlined(_face, look.Body, Size, 0f, 0f, Vector3.one);

            var shade = UIFactory.CreateCircle("Shade", _face, Color.Lerp(look.Body, Outline, 0.18f), Size);
            Center(shade.rectTransform, 0f, 0f);
            shade.raycastTarget = false;
            var shadeMask = UIFactory.CreateCircle("Mask", shade.transform, look.Body, Size * 1.02f);
            Center(shadeMask.rectTransform, 0f, Size * 0.12f);
            shadeMask.raycastTarget = false;

            if (look.EyePatches)
            {
                Patch(look.Ear, -0.2f);
                Patch(look.Ear, 0.2f);
            }

            _blushL = Blush(-0.3f);
            _blushR = Blush(0.3f);

            Eye(-0.2f, out _eyeL, out _pupilL, out _sparkleL);
            Eye(0.2f, out _eyeR, out _pupilR, out _sparkleR);
            _extraSparkle = UIFactory.CreateCircle("Twinkle", _face, Color.white, 10f).rectTransform;
            Center(_extraSparkle, Size * 0.26f, Size * 0.02f);

            _browL = Brow(-0.2f);
            _browR = Brow(0.2f);

            _mouthImage = UIFactory.CreateRounded("Mouth", _face, Outline, 0.4f);
            _mouth = _mouthImage.rectTransform;
            _mouthImage.raycastTarget = false;
            Center(_mouth, 0f, -Size * 0.1f);

            _mouthArc = UIFactory.CreateCircle("MouthArc", _face, Outline, Size * 0.2f);
            Center(_mouthArc.rectTransform, 0f, -Size * 0.1f);
            _mouthArc.rectTransform.localScale = new Vector3(1f, 0.75f, 1f);
            _mouthArc.raycastTarget = false;
            var mouthMask = UIFactory.CreateCircle("Mask", _mouthArc.transform, look.Body, Size * 0.2f);
            Center(mouthMask.rectTransform, 0f, Size * 0.06f);
            mouthMask.rectTransform.localScale = new Vector3(1.15f, 1.15f, 1f);
            mouthMask.raycastTarget = false;
            _mouthMask = mouthMask.rectTransform;

            _happyEyeL = HappyEye(-0.2f, look.Body);
            _happyEyeR = HappyEye(0.2f, look.Body);

            _tear = UIFactory.CreateCircle("Tear", _face, UIFactory.Sky, 22f);
            Center(_tear.rectTransform, -Size * 0.2f, -Size * 0.06f);
            _tear.rectTransform.localScale = new Vector3(0.7f, 1.2f, 1f);
            _sweat = UIFactory.CreateCircle("Sweat", _face, UIFactory.Sky, 24f);
            Center(_sweat.rectTransform, Size * 0.42f, Size * 0.22f);
            _sweat.rectTransform.localScale = new Vector3(0.7f, 1.2f, 1f);
            _heart = UIFactory.CreateCircle("Heart", _face, UIFactory.PinkDark, 34f);
            Center(_heart.rectTransform, Size * 0.4f, Size * 0.36f);

            BuildConditions(-Size * 0.16f, Size * 0.12f, Size * 0.02f);
            BuildAccessories(Size * 0.5f, -Size * 0.1f);
            host.StartCoroutine(SimpleTween.Breathe(Root, 0.025f, 2.8f));
        }

        // Idle life: blinks every few seconds, ears twitch now and then.
        private System.Collections.IEnumerator IdleLife()
        {
            while (Root != null)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(2.5f, 5.5f));
                if (Root == null) yield break;
                _host.StartCoroutine(Blink());
                if (_hasEars && UnityEngine.Random.value < 0.5f) _host.StartCoroutine(EarTwitch());
            }
        }

        private System.Collections.IEnumerator Blink()
        {
            if (_blinkL == null) yield break;
            _blinkL.gameObject.SetActive(true);
            _blinkR.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.11f);
            if (_blinkL != null) _blinkL.gameObject.SetActive(false);
            if (_blinkR != null) _blinkR.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator EarTwitch()
        {
            if (_headSlice == null) yield break;
            float elapsed = 0f;
            const float duration = 0.32f;
            float direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            while (elapsed < duration && _headSlice != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float wobble = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * 5f * direction;
                _headSlice.localRotation = Quaternion.Euler(0f, 0f, wobble);
                _headSlice.localScale = new Vector3(1f, 1f + Mathf.Sin(t * Mathf.PI) * 0.05f, 1f);
                yield return null;
            }
            if (_headSlice != null) { _headSlice.localRotation = Quaternion.identity; _headSlice.localScale = Vector3.one; }
        }

        // Reaction to being tapped: blink, ear twitch and a squashy hop.
        public void Boop()
        {
            _host.StartCoroutine(Blink());
            if (_hasEars) _host.StartCoroutine(EarTwitch());
            _host.StartCoroutine(SimpleTween.Hop(_pet));
        }

        private Image Overlay(UIFactory.IconKind kind, float x, float y, float size)
        {
            var icon = UIFactory.CreateIcon(kind, _pet, size, Color.clear);
            Center(icon, x, y);
            var image = icon.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            return image;
        }

        // Need-driven visuals: eye bags, dirt, rain cloud, drool, and a thought bubble of the craving.
        private void BuildConditions(float eyeX, float eyeY, float bagOffset)
        {
            _bagL = Bag(eyeX, eyeY + bagOffset - Size * 0.09f);
            _bagR = Bag(-eyeX, eyeY + bagOffset - Size * 0.09f);
            foreach (var (x, y) in new[] { (-Size * 0.22f, -Size * 0.3f), (Size * 0.2f, -Size * 0.26f), (-Size * 0.02f, -Size * 0.42f) })
            {
                var smudge = UIFactory.CreateCircle("Dirt", _pet, new Color(0.45f, 0.3f, 0.2f, 0.32f), Size * 0.12f);
                Center(smudge.rectTransform, x, y);
                smudge.rectTransform.localScale = new Vector3(1.3f, 0.8f, 1f);
                smudge.raycastTarget = false;
                _dirt.Add(smudge.rectTransform);
            }
            var drool = UIFactory.CreateCircle("Drool", _pet, UIFactory.Sky, Size * 0.06f);
            Center(drool.rectTransform, Size * 0.1f, eyeY - Size * 0.2f);
            drool.rectTransform.localScale = new Vector3(0.6f, 1.6f, 1f);
            drool.raycastTarget = false;
            _drool = drool.rectTransform;

            _rainCloud = UIFactory.CreateRect("RainCloud", _pet);
            Center(_rainCloud, -Size * 0.42f, Size * 0.52f);
            foreach (var (x, y, d) in new[] { (0f, 6f, 64f), (-30f, -4f, 48f), (30f, -2f, 50f) })
            {
                var puff = UIFactory.CreateCircle("Puff", _rainCloud, UIFactory.Hex("C9C4D2"), d);
                puff.rectTransform.anchoredPosition = new Vector2(x, y);
                puff.raycastTarget = false;
            }
            foreach (float x in new[] { -22f, 0f, 22f })
            {
                var drop = UIFactory.CreateRounded("Drop", _rainCloud, UIFactory.Sky, 0.3f);
                drop.rectTransform.sizeDelta = new Vector2(8f, 22f);
                drop.rectTransform.anchoredPosition = new Vector2(x, -40f);
                drop.raycastTarget = false;
            }

            _thought = UIFactory.CreateRect("Thought", _pet);
            Center(_thought, Size * 0.42f, Size * 0.58f);
            foreach (var (x, y, d) in new[] { (-34f, -52f, 14f), (-20f, -34f, 22f), (0f, 0f, 96f) })
            {
                var puff = UIFactory.CreateCircle("Puff", _thought, Color.white, d);
                puff.rectTransform.anchoredPosition = new Vector2(x, y);
                puff.raycastTarget = false;
            }
            SetConditions(100f, 100f, 100f, 100f);
        }

        // Wearable cosmetics from the shop, drawn procedurally around the head.
        private void BuildAccessories(float headTop, float neckY)
        {
            var beanie = UIFactory.CreateRect("Beanie", _pet);
            Center(beanie, 0f, headTop - Size * 0.02f);
            var cap = UIFactory.CreateRoundedRadius("Cap", beanie, UIFactory.Primary, 40f);
            cap.rectTransform.sizeDelta = new Vector2(Size * 0.5f, Size * 0.2f);
            cap.rectTransform.anchoredPosition = new Vector2(0f, Size * 0.04f);
            cap.raycastTarget = false;
            var band = UIFactory.CreateRoundedRadius("Band", beanie, Color.white, 20f);
            band.rectTransform.sizeDelta = new Vector2(Size * 0.52f, Size * 0.07f);
            band.rectTransform.anchoredPosition = new Vector2(0f, -Size * 0.04f);
            band.raycastTarget = false;
            var pom = UIFactory.CreateCircle("Pom", beanie, Color.white, Size * 0.1f);
            pom.rectTransform.anchoredPosition = new Vector2(0f, Size * 0.15f);
            pom.raycastTarget = false;
            _accessories["hat_beanie"] = beanie;

            var scarf = UIFactory.CreateRect("Scarf", _pet);
            Center(scarf, 0f, neckY);
            var wrap = UIFactory.CreateRoundedRadius("Wrap", scarf, UIFactory.Hex("F5B942"), 30f);
            wrap.rectTransform.sizeDelta = new Vector2(Size * 0.46f, Size * 0.11f);
            wrap.raycastTarget = false;
            var tail = UIFactory.CreateRoundedRadius("Tail", scarf, UIFactory.Hex("F5B942"), 20f);
            tail.rectTransform.sizeDelta = new Vector2(Size * 0.1f, Size * 0.2f);
            tail.rectTransform.anchoredPosition = new Vector2(Size * 0.16f, -Size * 0.1f);
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -15f);
            tail.raycastTarget = false;
            foreach (var (x, y) in new[] { (-Size * 0.1f, 0f), (Size * 0.05f, Size * 0.02f) })
            {
                var star = UIFactory.CreateIcon(UIFactory.IconKind.Sparkle, scarf, Size * 0.07f, Color.clear);
                star.anchoredPosition = new Vector2(x, y);
            }
            _accessories["scarf_star"] = scarf;

            var bow = UIFactory.CreateRect("Bow", _pet);
            Center(bow, Size * 0.24f, headTop - Size * 0.06f);
            foreach (float side in new[] { -1f, 1f })
            {
                var loop = UIFactory.CreateCircle("Loop", bow, UIFactory.Coral, Size * 0.12f);
                loop.rectTransform.anchoredPosition = new Vector2(side * Size * 0.06f, 0f);
                loop.rectTransform.localScale = new Vector3(1.1f, 0.8f, 1f);
                loop.raycastTarget = false;
            }
            var knot = UIFactory.CreateCircle("Knot", bow, UIFactory.PinkDark, Size * 0.06f);
            knot.raycastTarget = false;
            _accessories["bow_cherry"] = bow;

            var crown = UIFactory.CreateRect("Crown", _pet);
            Center(crown, 0f, headTop + Size * 0.04f);
            var baseBand = UIFactory.CreateRoundedRadius("Base", crown, UIFactory.Hex("F5B942"), 12f);
            baseBand.rectTransform.sizeDelta = new Vector2(Size * 0.32f, Size * 0.1f);
            baseBand.raycastTarget = false;
            foreach (float x in new[] { -0.1f, 0f, 0.1f })
            {
                var jewel = UIFactory.CreateCircle("Jewel", crown, x == 0f ? UIFactory.Sky : UIFactory.Pink, Size * 0.07f);
                jewel.rectTransform.anchoredPosition = new Vector2(x * Size, Size * 0.07f);
                jewel.raycastTarget = false;
            }
            _accessories["crown_tiny"] = crown;

            SetAccessory("");
        }

        public void SetAccessory(string id)
        {
            foreach (var pair in _accessories) pair.Value.gameObject.SetActive(pair.Key == id);
        }

        private RectTransform Bag(float x, float y)
        {
            var bag = UIFactory.CreateCircle("Bag", _pet, new Color(0.35f, 0.25f, 0.45f, 0.28f), Size * 0.13f);
            Center(bag.rectTransform, x, y);
            bag.rectTransform.localScale = new Vector3(1f, 0.45f, 1f);
            bag.raycastTarget = false;
            return bag.rectTransform;
        }

        public void SetConditions(float hunger, float hygiene, float energy, float happiness)
        {
            const float low = 35f;
            bool tired = energy < low, dirty = hygiene < low, sad = happiness < low, hungry = hunger < low;
            _bagL.gameObject.SetActive(tired);
            _bagR.gameObject.SetActive(tired);
            foreach (var d in _dirt) d.gameObject.SetActive(dirty);
            _rainCloud.gameObject.SetActive(sad);
            _drool.gameObject.SetActive(hungry);

            UIFactory.IconKind? craving = null;
            float lowest = low;
            if (hunger < lowest) { lowest = hunger; craving = UIFactory.IconKind.Cookie; }
            if (hygiene < lowest) { lowest = hygiene; craving = UIFactory.IconKind.Bubbles; }
            if (energy < lowest) { lowest = energy; craving = UIFactory.IconKind.Moon; }
            if (happiness < lowest) { craving = UIFactory.IconKind.Heart; }
            _thought.gameObject.SetActive(craving.HasValue);
            if (craving == _thoughtKind) return;
            _thoughtKind = craving;
            if (_thoughtIcon != null) UnityEngine.Object.Destroy(_thoughtIcon.gameObject);
            if (!craving.HasValue) return;
            _thoughtIcon = UIFactory.CreateIcon(craving.Value, _thought, 54f, Color.white);
            _thoughtIcon.anchoredPosition = Vector2.zero;
        }

        private void SetOverlay(Image overlay, bool on)
        {
            if (overlay != null) overlay.gameObject.SetActive(on);
        }

        private void ApplySpriteExpression(EmotionCategory category)
        {
            SetOverlay(_overlayHeart, category == EmotionCategory.ConnectionAndCare);
            SetOverlay(_overlayTwinkle, category == EmotionCategory.InterestAndAwe || category == EmotionCategory.Joy);
            SetOverlay(_overlayTear, category == EmotionCategory.Sadness || category == EmotionCategory.GuiltAndShame);
            SetOverlay(_overlaySweat, category == EmotionCategory.Fear || category == EmotionCategory.Disgust);
            SetOverlay(_overlayBang, category == EmotionCategory.Surprise || category == EmotionCategory.Anger);
            SetOverlay(_overlayZzz, category == EmotionCategory.Vulnerability);
        }

        private static void Center(RectTransform rect, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
        }

        // A filled shape with a dark outline behind it.
        private Image Outlined(Transform parent, Color color, float diameter, float x, float y, Vector3 scale, float rotation = 0f)
        {
            var outline = UIFactory.CreateCircle("Outline", parent, Outline, diameter + OutlineWidth * 2f / Mathf.Min(scale.x, scale.y));
            Center(outline.rectTransform, x, y);
            outline.rectTransform.localScale = scale;
            outline.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            outline.raycastTarget = false;
            var fill = UIFactory.CreateCircle("Fill", parent, color, diameter);
            Center(fill.rectTransform, x, y);
            fill.rectTransform.localScale = scale;
            fill.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            fill.raycastTarget = false;
            return fill;
        }

        private void BuildEars(Look look)
        {
            if (look.Ears == Ears.None) return;
            foreach (float side in new[] { -1f, 1f })
            {
                Image ear;
                switch (look.Ears)
                {
                    case Ears.Tall:
                        ear = Outlined(_face, look.Ear, Size * 0.36f, side * Size * 0.2f, Size * 0.5f, new Vector3(0.62f, 1.55f, 1f));
                        Inner(ear, look.EarInner, 0.55f);
                        break;
                    case Ears.Pointy:
                        ear = Outlined(_face, look.Ear, Size * 0.36f, side * Size * 0.3f, Size * 0.38f, new Vector3(0.7f, 1.05f, 1f), -side * 28f);
                        Inner(ear, look.EarInner, 0.5f);
                        break;
                    case Ears.Round:
                        Outlined(_face, look.Ear, Size * 0.3f, side * Size * 0.36f, Size * 0.34f, Vector3.one);
                        break;
                    case Ears.Tiny:
                        Outlined(_face, look.Ear, Size * 0.2f, side * Size * 0.4f, Size * 0.22f, Vector3.one);
                        break;
                    default:
                        Outlined(_face, look.Ear, Size * 0.34f, side * Size * 0.44f, Size * 0.05f, new Vector3(0.6f, 1.2f, 1f));
                        break;
                }
            }
        }

        private static void Inner(Image ear, Color color, float scale)
        {
            var inner = UIFactory.CreateCircle("Inner", ear.transform, color, ear.rectTransform.sizeDelta.x * scale);
            Center(inner.rectTransform, 0f, 0f);
            inner.raycastTarget = false;
        }

        private void Patch(Color color, float x)
        {
            var patch = UIFactory.CreateCircle("Patch", _face, color, Size * 0.3f);
            Center(patch.rectTransform, x * Size, Size * 0.07f);
            patch.rectTransform.localScale = new Vector3(0.9f, 1.15f, 1f);
            patch.raycastTarget = false;
        }

        private Image Blush(float x)
        {
            var blush = UIFactory.CreateCircle("Blush", _face, new Color(1f, 0.55f, 0.68f, 0.55f), Size * 0.16f);
            Center(blush.rectTransform, x * Size, -Size * 0.04f);
            blush.rectTransform.localScale = new Vector3(1.3f, 0.8f, 1f);
            blush.raycastTarget = false;
            return blush;
        }

        private void Eye(float x, out RectTransform eye, out RectTransform pupil, out RectTransform sparkle)
        {
            var white = UIFactory.CreateCircle("Eye", _face, Color.white, Size * 0.24f);
            Center(white.rectTransform, x * Size, Size * 0.08f);
            white.raycastTarget = false;
            eye = white.rectTransform;
            var dark = UIFactory.CreateCircle("Pupil", white.transform, Outline, Size * 0.15f);
            Center(dark.rectTransform, 0f, 0f);
            dark.raycastTarget = false;
            pupil = dark.rectTransform;
            var shine = UIFactory.CreateCircle("Sparkle", dark.transform, Color.white, Size * 0.055f);
            Center(shine.rectTransform, -Size * 0.035f, Size * 0.035f);
            shine.raycastTarget = false;
            sparkle = shine.rectTransform;
        }

        // Upward crescent: dark circle with a body-coloured circle masking its lower half.
        private RectTransform HappyEye(float x, Color body)
        {
            var arc = UIFactory.CreateCircle("HappyEye", _face, Outline, Size * 0.2f);
            Center(arc.rectTransform, x * Size, Size * 0.1f);
            arc.rectTransform.localScale = new Vector3(1f, 0.8f, 1f);
            arc.raycastTarget = false;
            var mask = UIFactory.CreateCircle("Mask", arc.transform, body, Size * 0.2f);
            Center(mask.rectTransform, 0f, -Size * 0.06f);
            mask.rectTransform.localScale = new Vector3(1.15f, 1.15f, 1f);
            mask.raycastTarget = false;
            return arc.rectTransform;
        }

        private void SetMouth(bool visibleBar, bool visibleArc, bool smile, Color color, float arcScale)
        {
            _mouthImage.gameObject.SetActive(visibleBar);
            _mouthArc.gameObject.SetActive(visibleArc);
            _mouthArc.color = color;
            _mouthArc.rectTransform.localScale = new Vector3(arcScale, 0.75f * arcScale, 1f);
            _mouthMask.anchoredPosition = new Vector2(0f, (smile ? 1f : -1f) * Size * 0.06f);
        }

        private void SetHappyEyes(bool on)
        {
            _happyEyeL.gameObject.SetActive(on);
            _happyEyeR.gameObject.SetActive(on);
            _eyeL.gameObject.SetActive(!on);
            _eyeR.gameObject.SetActive(!on);
        }

        private RectTransform Brow(float x)
        {
            var brow = UIFactory.CreateRounded("Brow", _face, Outline, 0.3f);
            Center(brow.rectTransform, x * Size, Size * 0.27f);
            brow.rectTransform.sizeDelta = new Vector2(Size * 0.16f, Size * 0.04f);
            brow.raycastTarget = false;
            return brow.rectTransform;
        }

        public void SetEmotion(EmotionType emotion, bool animate)
        {
            EmotionCategory category = EmotionCatalog.GetCategory(emotion);
            if (_spriteMode) ApplySpriteExpression(category); else ApplyExpression(category);
            var glow = Moods[category]; glow.a = 0.45f;
            if (!animate)
            {
                _moodGlow.color = glow;
                return;
            }
            _host.StartCoroutine(SimpleTween.ColorTo(_moodGlow, glow, 0.4f));
            _host.StartCoroutine(SimpleTween.PunchScale(_pet, 0.1f, 0.3f));
        }

        private void ApplyExpression(EmotionCategory category)
        {
            SetEyes(1f, 1f, 1f, 0f, 0f);
            _blushL.rectTransform.localScale = _blushR.rectTransform.localScale = new Vector3(1.3f, 0.8f, 1f);
            SetBlushAlpha(0.55f);
            _browL.gameObject.SetActive(false);
            _browR.gameObject.SetActive(false);
            _tear.gameObject.SetActive(false);
            _sweat.gameObject.SetActive(false);
            _heart.gameObject.SetActive(false);
            _extraSparkle.gameObject.SetActive(false);
            _mouthImage.sprite = UIFactory.RoundedSprite;
            _mouthImage.type = Image.Type.Sliced;
            _mouthImage.color = Outline;
            _mouth.anchoredPosition = new Vector2(0f, -Size * 0.1f);
            _mouth.sizeDelta = new Vector2(Size * 0.16f, Size * 0.045f);
            SetHappyEyes(false);
            SetMouth(false, true, true, Outline, 0.8f);

            switch (category)
            {
                case EmotionCategory.Joy:
                    SetHappyEyes(true);
                    SetMouth(false, true, true, UIFactory.PinkDark, 1.35f);
                    break;
                case EmotionCategory.Sadness:
                    SetEyes(1f, 0.8f, 0.9f, 0f, -Size * 0.02f);
                    SetMouth(false, true, false, Outline, 0.7f);
                    _tear.gameObject.SetActive(true);
                    Brows(-14f, Size * 0.24f);
                    break;
                case EmotionCategory.Anger:
                    SetEyes(1f, 0.85f, 0.9f, 0f, 0f);
                    Brows(22f, Size * 0.22f);
                    SetMouth(true, false, true, Outline, 1f);
                    _mouth.sizeDelta = new Vector2(Size * 0.2f, Size * 0.035f);
                    SetBlushAlpha(0.8f);
                    break;
                case EmotionCategory.Fear:
                    SetEyes(1.15f, 1.15f, 0.55f, 0f, 0f);
                    _sweat.gameObject.SetActive(true);
                    SetMouth(false, true, false, Outline, 0.6f);
                    break;
                case EmotionCategory.Disgust:
                    SetEyes(1f, 0.9f, 0.9f, 0f, 0f);
                    _eyeR.localScale = new Vector3(1f, 0.5f, 1f);
                    SetMouth(true, false, true, Outline, 1f);
                    _mouth.anchoredPosition = new Vector2(-Size * 0.06f, -Size * 0.11f);
                    _mouth.sizeDelta = new Vector2(Size * 0.12f, Size * 0.035f);
                    Brows(0f, Size * 0.24f);
                    _browR.anchoredPosition += new Vector2(0f, Size * 0.04f);
                    break;
                case EmotionCategory.Surprise:
                    SetEyes(1.2f, 1.2f, 0.9f, 0f, 0f);
                    SetMouth(true, false, true, Outline, 1f);
                    _mouthImage.sprite = UIFactory.CircleSprite;
                    _mouthImage.type = Image.Type.Simple;
                    _mouth.sizeDelta = new Vector2(Size * 0.11f, Size * 0.11f);
                    _mouth.anchoredPosition = new Vector2(0f, -Size * 0.12f);
                    break;
                case EmotionCategory.GuiltAndShame:
                    SetEyes(1f, 0.9f, 0.9f, Size * 0.03f, -Size * 0.02f);
                    SetBlushAlpha(0.95f);
                    _blushL.rectTransform.localScale = _blushR.rectTransform.localScale = new Vector3(1.6f, 1f, 1f);
                    SetMouth(false, true, false, Outline, 0.55f);
                    break;
                case EmotionCategory.ConnectionAndCare:
                    SetHappyEyes(true);
                    SetMouth(false, true, true, UIFactory.PinkDark, 1.1f);
                    _heart.gameObject.SetActive(true);
                    SetBlushAlpha(0.75f);
                    break;
                case EmotionCategory.Vulnerability:
                    SetEyes(1.05f, 1.1f, 1.15f, 0f, -Size * 0.01f);
                    Brows(-12f, Size * 0.25f);
                    SetMouth(false, true, false, Outline, 0.55f);
                    break;
                case EmotionCategory.InterestAndAwe:
                    SetEyes(1.1f, 1.1f, 1.1f, 0f, Size * 0.01f);
                    _extraSparkle.gameObject.SetActive(true);
                    SetMouth(true, false, true, Outline, 1f);
                    _mouthImage.sprite = UIFactory.CircleSprite;
                    _mouthImage.type = Image.Type.Simple;
                    _mouth.sizeDelta = new Vector2(Size * 0.07f, Size * 0.07f);
                    Brows(0f, Size * 0.24f);
                    _browR.anchoredPosition += new Vector2(0f, Size * 0.05f);
                    _browL.gameObject.SetActive(false);
                    break;
            }
        }

        private void SetEyes(float eyeScaleX, float eyeScaleY, float pupilScale, float lookX, float lookY)
        {
            _eyeL.localScale = _eyeR.localScale = new Vector3(eyeScaleX, eyeScaleY, 1f);
            _pupilL.localScale = _pupilR.localScale = Vector3.one * pupilScale;
            _pupilL.anchoredPosition = _pupilR.anchoredPosition = new Vector2(lookX, lookY);
        }

        private void SetBlushAlpha(float alpha)
        {
            var c = _blushL.color; c.a = alpha;
            _blushL.color = c;
            _blushR.color = c;
        }

        private void Brows(float tiltDegrees, float y)
        {
            _browL.gameObject.SetActive(true);
            _browR.gameObject.SetActive(true);
            _browL.anchoredPosition = new Vector2(-0.2f * Size, y);
            _browR.anchoredPosition = new Vector2(0.2f * Size, y);
            _browL.localRotation = Quaternion.Euler(0f, 0f, -tiltDegrees);
            _browR.localRotation = Quaternion.Euler(0f, 0f, tiltDegrees);
        }

        public void Celebrate() => _host.StartCoroutine(SimpleTween.PunchScale(_pet, 0.28f, 0.5f));
    }
}
