using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.UI
{
    // Background sets behind the pet: the free Cozy Room plus the sets sold under Shop → Backgrounds.
    // A set paints its scene into the room's scene layer and tints the whole-screen backdrop and glow.
    // Everything is procedural (circles, rounded rects, the ▶ triangle) so a set is ~40 lines, no art files.
    public static class RoomScenes
    {
        public class Scene
        {
            public string Id;
            public string Name;
            public Color Backdrop;   // screen colour behind everything
            public Color Glow;       // the soft gradient at the top of the screen
            public Color Sky;        // shop preview: tile colour
            public Color Ground;     // shop preview: ground ellipse
            public Color Accent;     // shop preview: sun / moon dot
            public bool Night;
            public Action<RectTransform, MonoBehaviour> Build;
        }

        public const string DefaultId = "bg_cozy";

        public static readonly Scene[] All =
        {
            new Scene { Id = "bg_cozy",   Name = "Cozy Room",    Backdrop = UIFactory.Hex("574B7E"), Glow = UIFactory.Hex("6E5C90"), Sky = UIFactory.Hex("4A4070"), Ground = UIFactory.Hex("8E6248"), Accent = UIFactory.Hex("FFF1B8"), Night = true, Build = BuildCozy },
            new Scene { Id = "bg_meadow", Name = "Meadow",       Backdrop = UIFactory.Hex("DDF1FF"), Glow = UIFactory.Hex("FFF4C9"), Sky = UIFactory.Hex("DDF1FF"), Ground = UIFactory.Hex("A8DFB8"), Accent = UIFactory.Butter,        Build = BuildMeadow },
            new Scene { Id = "bg_beach",  Name = "Beach Day",    Backdrop = UIFactory.Hex("D6F0FF"), Glow = UIFactory.Hex("FFF0C8"), Sky = UIFactory.Hex("8ED8F2"), Ground = UIFactory.Hex("FBE8C2"), Accent = UIFactory.Butter,        Build = BuildBeach },
            new Scene { Id = "bg_snow",   Name = "Snow Day",     Backdrop = UIFactory.Hex("E6EEF8"), Glow = UIFactory.Hex("F7FAFF"), Sky = UIFactory.Hex("E6EEF8"), Ground = Color.white,             Accent = UIFactory.Hex("BFD3EA"), Build = BuildSnow },
            new Scene { Id = "bg_night",  Name = "Starry Night", Backdrop = UIFactory.Hex("3B3A6B"), Glow = UIFactory.Hex("55508F"), Sky = UIFactory.Hex("3B3A6B"), Ground = UIFactory.Hex("59579A"), Accent = UIFactory.Hex("FFF1B8"), Night = true, Build = BuildNight },
        };

        public static Scene Find(string id)
        {
            foreach (var scene in All) if (scene.Id == id) return scene;
            return All[0];
        }

        // Paints `scene` into `layer` (clearing what was there). Nothing in a scene catches taps.
        public static void Apply(RectTransform layer, Scene scene, MonoBehaviour host)
        {
            for (int i = layer.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(layer.GetChild(i).gameObject);
            scene.Build(layer, host);
            foreach (var graphic in layer.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        // Small tile for shop cards: sky, a ground ellipse and the sun or moon.
        public static RectTransform Preview(RectTransform parent, Scene scene, float width = 200f, float height = 116f)
        {
            var tile = UIFactory.CreateRounded("Preview", parent, scene.Sky, 0.6f);
            tile.rectTransform.sizeDelta = new Vector2(width, height);
            tile.gameObject.AddComponent<RectMask2D>();
            var ground = UIFactory.CreateCircle("Ground", tile.transform, scene.Ground, width * 1.4f);
            UIFactory.Place(ground.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-width * 0.7f, -width * 0.7f + height * 0.28f), new Vector2(width * 0.7f, width * 0.7f + height * 0.28f));
            ground.rectTransform.localScale = new Vector3(1f, 0.45f, 1f);
            var accent = UIFactory.CreateCircle("Accent", tile.transform, scene.Accent, 34f);
            UIFactory.Place(accent.rectTransform, new Vector2(0.24f, 0.7f), new Vector2(0.24f, 0.7f), new Vector2(-17f, -17f), new Vector2(17f, 17f));
            if (scene.Night)
                for (int i = 0; i < 4; i++)
                {
                    var star = UIFactory.CreateCircle("Star", tile.transform, Color.white, 6f);
                    UIFactory.Place(star.rectTransform, new Vector2(0.45f + i * 0.14f, 0.82f - (i % 2) * 0.22f), new Vector2(0.45f + i * 0.14f, 0.82f - (i % 2) * 0.22f), new Vector2(-3f, -3f), new Vector2(3f, 3f));
                }
            foreach (var graphic in tile.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            return tile.rectTransform;
        }

        // ---------- scenes ----------

        // ---------- the default room: nearly empty, and the window tells the time ----------
        // Asked for 2026-09-21: the lofi study (desk, laptop, lamp, posters, shelf, plant, string lights) pulled the eye
        // away from the pet. What is left is a wall, a floor and one window; the sky in it follows the device's local
        // time (dawn, day, dusk, night, with the sun or the moon on its arc) and the wall takes a light or dark tone
        // with it. Repainted when the ten-minute bucket changes (RoomView.ApplyRoom, polled by HUDController).

        public static Func<DateTime> LocalNow = () => DateTime.Now;
        public static int CozyBucket(DateTime time) => time.Hour * 6 + time.Minute / 10;

        public struct CozyPalette
        {
            public Color SkyTop, SkyBottom, WallTop, WallBottom, FloorTop, FloorBottom, Frame, Backdrop, Glow;
            public float Daylight;   // 0 night … 1 full day
        }

        private static CozyPalette Pal(string skyTop, string skyBottom, string wallTop, string wallBottom, string floorTop, string floorBottom, string frame, string backdrop, string glow, float daylight) =>
            new CozyPalette
            {
                SkyTop = UIFactory.Hex(skyTop), SkyBottom = UIFactory.Hex(skyBottom), WallTop = UIFactory.Hex(wallTop), WallBottom = UIFactory.Hex(wallBottom),
                FloorTop = UIFactory.Hex(floorTop), FloorBottom = UIFactory.Hex(floorBottom), Frame = UIFactory.Hex(frame),
                Backdrop = UIFactory.Hex(backdrop), Glow = UIFactory.Hex(glow), Daylight = daylight,
            };

        private static readonly CozyPalette NightPal = Pal("1B1840", "4A3F7A", "4E4374", "6A5A8E", "7C5B48", "5E4334", "3A3158", "574B7E", "6E5C90", 0f);
        private static readonly CozyPalette DawnPal  = Pal("7FA8DD", "FFC7A6", "BFAED4", "DCCBE2", "A98268", "86624C", "8F7FA6", "CDBBDD", "FFD9C2", 0.55f);
        private static readonly CozyPalette DayPal   = Pal("6FBBF2", "D3EEFF", "F1E4D6", "FBF1E6", "C9A07E", "A98062", "F7EEE4", "F3E6DA", "FFE9D6", 1f);
        private static readonly CozyPalette DuskPal  = Pal("463B86", "F0A27C", "8C789F", "B59AB6", "94705A", "72523F", "5E4E7E", "8C789F", "C9A0B0", 0.35f);
        private static readonly float[] KeyHours = { 0f, 5f, 6.5f, 8.5f, 16.5f, 18.5f, 20.5f, 24f };
        private static readonly CozyPalette[] KeyPals = { NightPal, NightPal, DawnPal, DayPal, DayPal, DuskPal, NightPal, NightPal };

        public static CozyPalette CozyPaletteAt(DateTime time)
        {
            float hour = time.Hour + time.Minute / 60f;
            for (int i = 0; i < KeyHours.Length - 1; i++)
            {
                if (hour > KeyHours[i + 1]) continue;
                float t = Mathf.InverseLerp(KeyHours[i], KeyHours[i + 1], hour);
                CozyPalette a = KeyPals[i], b = KeyPals[i + 1];
                return new CozyPalette
                {
                    SkyTop = Color.Lerp(a.SkyTop, b.SkyTop, t), SkyBottom = Color.Lerp(a.SkyBottom, b.SkyBottom, t),
                    WallTop = Color.Lerp(a.WallTop, b.WallTop, t), WallBottom = Color.Lerp(a.WallBottom, b.WallBottom, t),
                    FloorTop = Color.Lerp(a.FloorTop, b.FloorTop, t), FloorBottom = Color.Lerp(a.FloorBottom, b.FloorBottom, t),
                    Frame = Color.Lerp(a.Frame, b.Frame, t), Backdrop = Color.Lerp(a.Backdrop, b.Backdrop, t), Glow = Color.Lerp(a.Glow, b.Glow, t),
                    Daylight = Mathf.Lerp(a.Daylight, b.Daylight, t),
                };
            }
            return NightPal;
        }

        private static Sprite _cozy;
        private static int _cozyBucket = -1;

        // Window rectangle in the 1080×1920 design (y up): centred above the pet, clear of the header.
        private const float WinW = 500f, WinH = 430f, WinX = (1080f - WinW) * 0.5f, WinY = 1190f;

        private static void BuildCozy(RectTransform layer, MonoBehaviour host)
        {
            DateTime now = LocalNow();
            int bucket = CozyBucket(now);
            if (_cozy == null || bucket != _cozyBucket)
            {
                if (_cozy != null) { UnityEngine.Object.Destroy(_cozy.texture); UnityEngine.Object.Destroy(_cozy); }
                _cozy = PaintCozy(now);
                _cozyBucket = bucket;
            }
            var art = UIFactory.CreatePanel("Art", layer, Color.white);
            art.sprite = _cozy;
            art.type = Image.Type.Simple;
            art.preserveAspect = false;
            UIFactory.Fill(art.rectTransform);

            if (CozyPaletteAt(now).Daylight > 0.25f) return;
            float[] sx = { 0.34f, 0.47f, 0.62f, 0.69f }, sy = { 0.80f, 0.74f, 0.82f, 0.70f };   // a few live stars inside the window
            for (int i = 0; i < sx.Length; i++)
            {
                var star = UIFactory.CreateCircle("Star", layer, Color.white, 6f);
                UIFactory.Place(star.rectTransform, new Vector2(sx[i], sy[i]), new Vector2(sx[i], sy[i]), new Vector2(-3f, -3f), new Vector2(3f, 3f));
                host.StartCoroutine(Twinkle(star, 2.2f + i * 0.5f));
            }
        }

        // Full-screen design (1080×1920, y up). Floor line at 0.444 of the height so the rug and the pet stand on it.
        private static Sprite PaintCozy(DateTime now)
        {
            const float W = 1080f, H = 1920f, FloorY = 852f;
            CozyPalette pal = CozyPaletteAt(now);
            var p = new ScenePainter(W, H, 0.6f, pal.WallBottom);
            Color A(Color c, float a) { c.a = a; return c; }
            float hour = now.Hour + now.Minute / 60f;
            float dark = 1f - pal.Daylight;

            // Wall, skirting board, plain floor with a few faint board lines.
            p.RoundedRect(0f, FloorY, W, H - FloorY, 0f, pal.WallTop, pal.WallBottom);
            p.RoundedRect(0f, FloorY - 2f, W, 22f, 0f, Color.Lerp(pal.WallBottom, Color.black, 0.16f), Color.Lerp(pal.WallBottom, Color.black, 0.24f));
            p.RoundedRect(0f, 0f, W, FloorY, 0f, pal.FloorTop, pal.FloorBottom);
            for (int i = 1; i < 6; i++) p.Line(0f, FloorY - i * 150f, W, FloorY - i * 150f, 3f, A(Color.black, 0.06f));

            // The window: frame, sky, then whatever the hour puts in it.
            p.Shadow(WinX + 6f, WinY - 10f, WinW, WinH, 18f, 26f, 0.18f);
            p.RoundedRect(WinX - 16f, WinY - 16f, WinW + 32f, WinH + 32f, 22f, pal.Frame, Color.Lerp(pal.Frame, Color.black, 0.12f));
            p.RoundedRect(WinX, WinY, WinW, WinH, 10f, pal.SkyTop, pal.SkyBottom);
            p.SetClip(WinX, WinY, WinW, WinH);
            if (dark > 0.35f)
            {
                float starAlpha = 0.85f * Mathf.InverseLerp(0.35f, 0.85f, dark);
                for (int i = 0; i < 22; i++)
                {
                    float x = WinX + 18f + Frac(i * 0.618f + 0.11f) * (WinW - 36f), y = WinY + WinH * 0.32f + Frac(i * i * 0.137f + i * 0.291f) * WinH * 0.64f;
                    p.Circle(x, y, 2f + (i % 3) * 1.1f, A(Color.white, starAlpha));
                }
            }
            if (hour >= 5.5f && hour <= 20.5f)
            {
                float t = Mathf.InverseLerp(5.5f, 20.5f, hour), lift = Mathf.Sin(t * Mathf.PI);
                float x = WinX + WinW * (0.10f + 0.80f * t), y = WinY + WinH * (-0.10f + 0.92f * lift);
                Color sun = Color.Lerp(UIFactory.Hex("FFB070"), UIFactory.Hex("FFE9A8"), lift);
                p.Circle(x, y, 84f, A(sun, 0.28f), 60f);
                p.Circle(x, y, 36f, sun);
            }
            if (hour >= 19f || hour <= 7f)
            {
                float t = hour >= 19f ? (hour - 19f) / 12f : (hour + 5f) / 12f, lift = Mathf.Sin(t * Mathf.PI);
                float x = WinX + WinW * (0.12f + 0.76f * t), y = WinY + WinH * (0.10f + 0.72f * lift);
                float moonAlpha = Mathf.InverseLerp(0.2f, 0.7f, dark);
                p.Circle(x, y, 50f, A(UIFactory.Hex("FFF1B8"), 0.30f * moonAlpha), 40f);
                p.Circle(x, y, 32f, A(UIFactory.Hex("FFF1B8"), moonAlpha));
                p.Circle(x + 17f, y + 9f, 28f, A(Color.Lerp(pal.SkyTop, pal.SkyBottom, 0.35f), moonAlpha));   // the bite that makes it a crescent
            }
            if (pal.Daylight > 0.5f)
            {
                float cloudAlpha = 0.92f * Mathf.InverseLerp(0.5f, 0.9f, pal.Daylight);
                p.Ellipse(WinX + 130f, WinY + 300f, 62f, 26f, 0f, A(Color.white, cloudAlpha), 6f);
                p.Ellipse(WinX + 176f, WinY + 316f, 46f, 24f, 0f, A(Color.white, cloudAlpha), 6f);
                p.Ellipse(WinX + 96f, WinY + 290f, 38f, 18f, 0f, A(Color.white, cloudAlpha), 6f);
            }
            Color hill = Color.Lerp(Color.Lerp(pal.SkyBottom, UIFactory.Hex("2A2350"), 0.55f), UIFactory.Hex("9FD6A8"), pal.Daylight * 0.75f);
            p.Ellipse(WinX + 150f, WinY - 60f, 330f, 150f, 0f, hill);
            p.Ellipse(WinX + 420f, WinY - 80f, 300f, 150f, 0f, Color.Lerp(hill, Color.black, 0.10f));
            p.ClearClip();

            // Cross bars and sill.
            p.Rect(WinX + WinW * 0.5f - 5f, WinY, 10f, WinH, pal.Frame);
            p.Rect(WinX, WinY + WinH * 0.5f - 5f, WinW, 10f, pal.Frame);
            p.Shadow(WinX - 28f, WinY - 40f, WinW + 56f, 22f, 6f, 14f, 0.20f);
            p.RoundedRect(WinX - 34f, WinY - 34f, WinW + 68f, 22f, 6f, Color.Lerp(pal.Frame, Color.white, 0.12f), Color.Lerp(pal.Frame, Color.black, 0.10f));

            // Light from the window pooling on the floor: warm by day, faint and cool at night.
            Color pool = Color.Lerp(UIFactory.Hex("BFC8FF"), UIFactory.Hex("FFE9C2"), pal.Daylight);
            p.Ellipse(W * 0.5f, FloorY - 150f, 360f, 84f, 0f, A(pool, Mathf.Lerp(0.07f, 0.16f, pal.Daylight)), 70f);

            return p.ToSprite();
        }

        private static void BuildMeadow(RectTransform layer, MonoBehaviour host)
        {
            Sun(layer, 0.16f, 0.86f, 120f);
            host.StartCoroutine(Drift(Cloud(layer, 0.62f, 0.84f, 1.1f), 30f, 9f));
            host.StartCoroutine(Drift(Cloud(layer, 0.88f, 0.72f, 0.8f), 22f, 12f));
            Hill(layer, UIFactory.Hex("A8DFB8"), 0.2f, 0.42f, 1000f);
            Hill(layer, UIFactory.Hex("93D5A8"), 0.84f, 0.4f, 820f);
            Floor(layer, UIFactory.Hex("BFE9D0"), 0.444f);

            var tree = At(layer, "Tree", 0.9f, 0.47f);
            var trunk = UIFactory.CreateRounded("Trunk", tree, UIFactory.Hex("B98A6A"), 0.6f);
            trunk.rectTransform.sizeDelta = new Vector2(28f, 120f);
            trunk.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            var canopy = UIFactory.CreateRect("Canopy", tree);
            canopy.anchoredPosition = new Vector2(0f, 110f);
            Dot(canopy, UIFactory.Hex("7FCB97"), 130f, -40f, -10f);
            Dot(canopy, UIFactory.Hex("7FCB97"), 130f, 40f, -10f);
            Dot(canopy, UIFactory.Hex("8ED7A6"), 150f, 0f, 20f);
            host.StartCoroutine(Sway(canopy, 2.5f, 4f));

            Color[] petals = { UIFactory.Pink, UIFactory.Lavender, UIFactory.Butter, UIFactory.Pink, UIFactory.Lavender };
            float[] xs = { 0.06f, 0.14f, 0.9f, 0.95f, 0.84f };
            float[] ys = { 0.38f, 0.32f, 0.34f, 0.4f, 0.29f };
            for (int i = 0; i < petals.Length; i++) Flower(layer, xs[i], ys[i], petals[i], 14f);
        }

        private static void BuildBeach(RectTransform layer, MonoBehaviour host)
        {
            Sun(layer, 0.84f, 0.86f, 120f);
            host.StartCoroutine(Drift(Cloud(layer, 0.3f, 0.8f, 0.9f), 24f, 10f));
            var sea = UIFactory.CreatePanel("Sea", layer, UIFactory.Hex("8ED8F2"));
            UIFactory.Place(sea.rectTransform, new Vector2(0f, 0.4f), new Vector2(1f, 0.62f), new Vector2(-40f, 0f), new Vector2(40f, 0f));
            for (int i = 0; i < 8; i++)
            {
                var foam = UIFactory.CreateCircle("Foam", layer, Color.white, 44f + (i % 3) * 8f);
                float x = (i + 0.5f) / 8f;
                UIFactory.Place(foam.rectTransform, new Vector2(x, 0.62f), new Vector2(x, 0.62f), new Vector2(-24f, -20f), new Vector2(24f, 20f));
                foam.rectTransform.localScale = new Vector3(1.4f, 0.55f, 1f);
                host.StartCoroutine(Drift(foam.rectTransform, 12f + i * 2f, 3f + i * 0.3f));
            }
            Floor(layer, UIFactory.Hex("FBE8C2"), 0.444f);

            var palm = At(layer, "Palm", 0.12f, 0.45f);
            var trunk = UIFactory.CreateRounded("Trunk", palm, UIFactory.Hex("B98A6A"), 0.6f);
            trunk.rectTransform.sizeDelta = new Vector2(26f, 170f);
            trunk.rectTransform.anchoredPosition = new Vector2(0f, 55f);
            trunk.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            var fronds = UIFactory.CreateRect("Fronds", palm);
            fronds.anchoredPosition = new Vector2(12f, 140f);
            for (int i = 0; i < 5; i++)
            {
                float angle = -80f + i * 40f;
                var frond = UIFactory.CreateCircle("Frond", fronds, i % 2 == 0 ? UIFactory.Hex("7FCB97") : UIFactory.Hex("8ED7A6"), 110f);
                frond.rectTransform.pivot = new Vector2(0.5f, 0f);
                frond.rectTransform.anchoredPosition = Vector2.zero;
                frond.rectTransform.localScale = new Vector3(0.36f, 1f, 1f);
                frond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            host.StartCoroutine(Sway(fronds, 3f, 3.6f));

            Flower(layer, 0.8f, 0.33f, UIFactory.Coral, 18f);
            var shell = UIFactory.CreateCircle("Shell", layer, UIFactory.Hex("FFD9E3"), 40f);
            UIFactory.Place(shell.rectTransform, new Vector2(0.9f, 0.3f), new Vector2(0.9f, 0.3f), new Vector2(-20f, -14f), new Vector2(20f, 14f));
            shell.rectTransform.localScale = new Vector3(1f, 0.7f, 1f);
        }

        private static void BuildSnow(RectTransform layer, MonoBehaviour host)
        {
            Hill(layer, UIFactory.Hex("F4F8FD"), 0.22f, 0.42f, 1000f);
            Hill(layer, UIFactory.Hex("EAF1F9"), 0.84f, 0.4f, 820f);
            Floor(layer, Color.white, 0.444f);

            var pine = At(layer, "Pine", 0.14f, 0.44f);
            var trunk = UIFactory.CreateRounded("Trunk", pine, UIFactory.Hex("B98A6A"), 0.6f);
            trunk.rectTransform.sizeDelta = new Vector2(22f, 60f);
            trunk.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            Tier(pine, 180f, 110f, 80f, UIFactory.Hex("7FBF9A"));
            Tier(pine, 140f, 100f, 140f, UIFactory.Hex("8ACB9F"));
            Tier(pine, 100f, 90f, 195f, UIFactory.Hex("97D5A8"));
            var cap = UIFactory.CreateCircle("Cap", pine, Color.white, 40f);
            cap.rectTransform.anchoredPosition = new Vector2(0f, 232f);
            cap.rectTransform.localScale = new Vector3(1f, 0.5f, 1f);

            var snowman = At(layer, "Snowman", 0.86f, 0.43f);
            Dot(snowman, Color.white, 120f, 0f, 0f);
            Dot(snowman, Color.white, 86f, 0f, 84f);
            Dot(snowman, UIFactory.MenuInk, 9f, -14f, 96f);
            Dot(snowman, UIFactory.MenuInk, 9f, 14f, 96f);
            var nose = UIFactory.CreateCursor(snowman, 20f, UIFactory.Coral);
            nose.rectTransform.anchoredPosition = new Vector2(10f, 82f);
            nose.rectTransform.localScale = new Vector3(1.2f, 0.7f, 1f);
            var scarf = UIFactory.CreateRounded("Scarf", snowman, UIFactory.Coral, 1f);
            scarf.rectTransform.sizeDelta = new Vector2(78f, 18f);
            scarf.rectTransform.anchoredPosition = new Vector2(0f, 48f);
            Dot(snowman, UIFactory.MenuInk, 8f, 0f, 14f);
            Dot(snowman, UIFactory.MenuInk, 8f, 0f, -8f);

            for (int i = 0; i < 14; i++)
            {
                var flake = UIFactory.CreateCircle("Flake", layer, new Color(1f, 1f, 1f, 0.9f), 9f + (i % 3) * 4f);
                float x = Frac(i * 0.618f);
                UIFactory.Place(flake.rectTransform, new Vector2(x, 0f), new Vector2(x, 0f), new Vector2(-8f, -8f), new Vector2(8f, 8f));
                host.StartCoroutine(Fall(flake.rectTransform, layer, 0.3f + Frac(i * 0.37f) * 0.7f, 40f + i * 4f, 14f + (i % 4) * 6f));
            }
        }

        private static void BuildNight(RectTransform layer, MonoBehaviour host)
        {
            for (int i = 0; i < 18; i++)
            {
                float x = Frac(i * 0.618f), y = 0.55f + Frac(i * 0.383f) * 0.38f;
                var star = UIFactory.CreateCircle("Star", layer, Color.white, 6f + (i % 3) * 4f);
                UIFactory.Place(star.rectTransform, new Vector2(x, y), new Vector2(x, y), new Vector2(-7f, -7f), new Vector2(7f, 7f));
                host.StartCoroutine(Twinkle(star, 1.4f + Frac(i * 0.29f) * 2f));
            }
            var moon = At(layer, "Moon", 0.18f, 0.84f);
            Dot(moon, UIFactory.Hex("FFF1B8"), 130f, 0f, 0f);
            Dot(moon, UIFactory.Hex("F3E2A0"), 26f, -28f, 16f);
            Dot(moon, UIFactory.Hex("F3E2A0"), 16f, 18f, -26f);
            Dot(moon, UIFactory.Hex("F3E2A0"), 12f, 24f, 24f);
            Hill(layer, UIFactory.Hex("4F4C8C"), 0.2f, 0.42f, 1000f);
            Hill(layer, UIFactory.Hex("55529A"), 0.84f, 0.4f, 820f);
            Floor(layer, UIFactory.Hex("605DA5"), 0.444f);
            var sleepy = At(layer, "Bush", 0.88f, 0.46f);
            Dot(sleepy, UIFactory.Hex("4A4785"), 110f, -30f, 0f);
            Dot(sleepy, UIFactory.Hex("4A4785"), 130f, 30f, 10f);
            Dot(sleepy, UIFactory.Hex("55529A"), 90f, 0f, 40f);
        }

        // ---------- pieces ----------

        private static RectTransform At(RectTransform layer, string name, float ax, float ay)
        {
            var rect = UIFactory.CreateRect(name, layer);
            UIFactory.Place(rect, new Vector2(ax, ay), new Vector2(ax, ay), Vector2.zero, Vector2.zero);
            return rect;
        }

        private static void Floor(RectTransform layer, Color color, float height)
        {
            var floor = UIFactory.CreateRounded("Floor", layer, color, 1.6f);
            UIFactory.Place(floor.rectTransform, new Vector2(0f, 0f), new Vector2(1f, height), new Vector2(-40f, -80f), new Vector2(40f, 0f));
        }

        private static void Hill(RectTransform layer, Color color, float ax, float ay, float diameter)
        {
            var hill = UIFactory.CreateCircle("Hill", layer, color, diameter);
            UIFactory.Place(hill.rectTransform, new Vector2(ax, ay), new Vector2(ax, ay), new Vector2(-diameter * 0.5f, -diameter * 0.5f), new Vector2(diameter * 0.5f, diameter * 0.5f));
            hill.rectTransform.localScale = new Vector3(1f, 0.42f, 1f);
        }

        private static void Sun(RectTransform layer, float ax, float ay, float size)
        {
            var glow = UIFactory.CreateCircle("SunGlow", layer, new Color(1f, 0.92f, 0.6f, 0.35f), size * 1.5f);
            UIFactory.Place(glow.rectTransform, new Vector2(ax, ay), new Vector2(ax, ay), new Vector2(-size * 0.75f, -size * 0.75f), new Vector2(size * 0.75f, size * 0.75f));
            var sun = UIFactory.CreateCircle("Sun", layer, UIFactory.Butter, size);
            UIFactory.Place(sun.rectTransform, new Vector2(ax, ay), new Vector2(ax, ay), new Vector2(-size * 0.5f, -size * 0.5f), new Vector2(size * 0.5f, size * 0.5f));
        }

        private static RectTransform Cloud(Transform parent, float ax, float ay, float scale)
        {
            var cloud = UIFactory.CreateRect("Cloud", parent);
            UIFactory.Place(cloud, new Vector2(ax, ay), new Vector2(ax, ay), Vector2.zero, Vector2.zero);
            cloud.localScale = Vector3.one * scale;
            Dot(cloud, Color.white, 64f, 0f, 8f);
            Dot(cloud, Color.white, 48f, -34f, -6f);
            Dot(cloud, Color.white, 52f, 34f, -4f);
            return cloud;
        }

        private static void Dot(Transform parent, Color color, float size, float x, float y)
        {
            var dot = UIFactory.CreateCircle("Dot", parent, color, size);
            dot.rectTransform.anchoredPosition = new Vector2(x, y);
        }

        private static void Flower(RectTransform layer, float ax, float ay, Color petal, float size)
        {
            var flower = At(layer, "Flower", ax, ay);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                Dot(flower, petal, size, Mathf.Cos(a) * size * 0.75f, Mathf.Sin(a) * size * 0.75f);
            }
            Dot(flower, UIFactory.Butter, size * 0.8f, 0f, 0f);
        }

        // One tier of a pine: a flattened circle (the 16 px triangle sprite goes jagged at this size).
        private static void Tier(Transform parent, float width, float height, float y, Color color)
        {
            var tier = UIFactory.CreateCircle("Tier", parent, color, width);
            tier.rectTransform.localScale = new Vector3(1f, height / width, 1f);
            tier.rectTransform.anchoredPosition = new Vector2(0f, y);
        }

        private static float Frac(float v) => v - Mathf.Floor(v);

        // ---------- motion ----------

        public static IEnumerator Drift(RectTransform target, float amplitude, float period)
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

        public static IEnumerator Sway(RectTransform target, float degrees, float period)
        {
            float time = 0f;
            while (target != null)
            {
                time += Time.deltaTime;
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time / period * Mathf.PI * 2f) * degrees);
                yield return null;
            }
        }

        public static IEnumerator Twinkle(Image bulb, float period)
        {
            Color baseColor = bulb.color;
            float time = 0f;
            while (bulb != null)
            {
                time += Time.deltaTime;
                var c = baseColor; c.a = baseColor.a * (0.55f + 0.45f * Mathf.Sin(time / period * Mathf.PI * 2f));
                bulb.color = c;
                yield return null;
            }
        }

        // Snow: falls from the top of the layer to the floor line, then starts over higher up.
        private static IEnumerator Fall(RectTransform flake, RectTransform layer, float startT, float speed, float sway)
        {
            float t = startT, phase = startT * 6f;
            while (flake != null && layer != null)
            {
                float height = layer.rect.height;
                t += Time.deltaTime * speed / Mathf.Max(1f, height);
                if (t > 1f) t -= 1f;
                phase += Time.deltaTime;
                float y = height * (1f - t) * 0.6f + height * 0.4f;
                flake.anchoredPosition = new Vector2(Mathf.Sin(phase * 1.3f) * sway, y);
                yield return null;
            }
        }
    }
}
