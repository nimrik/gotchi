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

        private static Sprite _cozy;

        // The default room is a lofi study at dusk (the "lofi hip hop radio" look): a big window onto a purple
        // night city, a desk with a glowing laptop, mug and lamp, headphones, string lights, posters, plants.
        // Painted once by ScenePainter; only the string lights and a few stars twinkle.
        private static void BuildCozy(RectTransform layer, MonoBehaviour host)
        {
            var art = UIFactory.CreatePanel("Art", layer, Color.white);
            art.sprite = _cozy ?? (_cozy = PaintCozy());
            art.type = Image.Type.Simple;
            art.preserveAspect = false;
            UIFactory.Fill(art.rectTransform);

            for (int i = 0; i < 9; i++)
            {
                float t = (i + 0.5f) / 9f;
                float y = StringY(t * 1080f) / 1920f;
                var bulb = UIFactory.CreateCircle("Bulb", layer, i % 3 == 0 ? UIFactory.Hex("FFE9A8") : i % 3 == 1 ? UIFactory.Hex("FFC9A0") : UIFactory.Hex("FFD6E0"), 18f);
                UIFactory.Place(bulb.rectTransform, new Vector2(t, y), new Vector2(t, y), new Vector2(-9f, -22f), new Vector2(9f, -4f));
                host.StartCoroutine(Twinkle(bulb, 1.6f + i * 0.23f));
            }
            float[] sx = { 0.14f, 0.3f, 0.42f, 0.22f }, sy = { 0.86f, 0.9f, 0.84f, 0.8f };
            for (int i = 0; i < sx.Length; i++)
            {
                var star = UIFactory.CreateCircle("Star", layer, Color.white, 6f);
                UIFactory.Place(star.rectTransform, new Vector2(sx[i], sy[i]), new Vector2(sx[i], sy[i]), new Vector2(-3f, -3f), new Vector2(3f, 3f));
                host.StartCoroutine(Twinkle(star, 2.2f + i * 0.5f));
            }
        }

        // Height of the string-light cable (canvas units) at x — a shallow sag between the top corners.
        private static float StringY(float x)
        {
            float u = x / 1080f - 0.5f;
            return 1770f - (0.25f - u * u) * 160f;
        }

        // Full-screen design (1080×1920, y up). Floor line at 0.444 of the height so the rug and the pet stand on it.
        private static Sprite PaintCozy()
        {
            const float W = 1080f, H = 1920f, FloorY = 852f;
            var p = new ScenePainter(W, H, 0.7f, UIFactory.Hex("574B7E"));
            Color H_(string hex) => UIFactory.Hex(hex);
            Color A(string hex, float a) { var c = UIFactory.Hex(hex); c.a = a; return c; }

            // Wall: dusky purple, warmer low on the left where the lamp is; skirting; dark wood floor.
            p.RoundedRect(0f, FloorY, W, H - FloorY, 0f, H_("4E4374"), H_("6A5A8E"));
            p.Circle(230f, 1080f, 420f, A("FFB070", 0.22f), 260f);
            p.RoundedRect(0f, FloorY - 2f, W, 24f, 0f, H_("3F3560"), H_("352C52"));
            p.Rect(0f, FloorY + 20f, W, 3f, A("FFFFFF", 0.14f));
            p.RoundedRect(0f, 0f, W, FloorY, 0f, H_("8A6650"), H_("6A4C3C"));
            for (int i = 1; i < 17; i++)
            {
                float y = FloorY - i * 50f;
                p.Line(0f, y, W, y, 3f, A("4E3628", 0.55f));
                for (int j = 0; j < 5; j++)
                {
                    float x = (j * 236f + (i % 2) * 118f) % W;
                    p.Line(x, y, x, y + 50f, 3f, A("4E3628", 0.4f));
                }
            }
            p.Ellipse(230f, FloorY - 40f, 300f, 80f, 0f, A("FFB070", 0.16f), 60f);   // lamp light pooling on the floor

            // Window onto the city: frame, dusk sky, stars, moon, skyline with lit windows, cross bars, sill.
            float wx = 96f, wy = 1120f, ww = 470f, wh = 500f;
            p.Shadow(wx + 8f, wy - 10f, ww, wh, 20f, 30f, 0.25f);
            p.RoundedRect(wx - 16f, wy - 16f, ww + 32f, wh + 32f, 22f, H_("3A3158"), H_("2E2748"));
            p.RoundedRect(wx, wy, ww, wh, 12f, H_("241F45"), H_("F0A27C"));
            p.SetClip(wx, wy, ww, wh);
            p.RoundedRect(wx, wy + wh * 0.35f, ww, wh * 0.65f, 0f, H_("241F45"), H_("7A4E8C"));
            p.RoundedRect(wx, wy, ww, wh * 0.36f, 0f, H_("7A4E8C"), H_("F0A27C"));
            for (int i = 0; i < 26; i++)
            {
                float sx = wx + 20f + Frac(i * 0.618f + 0.11f) * (ww - 40f), sy = wy + wh * 0.5f + Frac(i * i * 0.137f + i * 0.291f) * wh * 0.48f;
                p.Circle(sx, sy, 2.2f + (i % 3) * 1.2f, A("FFFFFF", 0.85f));
            }
            p.Circle(wx + 360f, wy + 400f, 46f, A("FFF1B8", 0.35f), 40f);
            p.Circle(wx + 360f, wy + 400f, 34f, H_("FFF1B8"));
            p.Circle(wx + 378f, wy + 410f, 30f, H_("2E2750"));
            float[] bw = { 46f, 70f, 38f, 90f, 54f, 64f, 42f, 78f, 50f };
            float[] bh = { 120f, 190f, 90f, 240f, 150f, 200f, 110f, 170f, 130f };
            float bx = wx - 10f;
            for (int i = 0; i < bw.Length; i++)
            {
                p.Rect(bx, wy, bw[i], bh[i], H_("221C40"));
                for (float yy = wy + 14f; yy < wy + bh[i] - 12f; yy += 22f)
                    for (float xx = bx + 8f; xx < bx + bw[i] - 10f; xx += 16f)
                        if (Frac((xx * 7.13f + yy * 3.71f) * 0.01f) > 0.45f) p.Rect(xx, yy, 7f, 10f, A("FFE08A", 0.9f));
                bx += bw[i] + 6f;
            }
            p.ClearClip();
            p.Rect(wx + ww * 0.5f - 5f, wy, 10f, wh, H_("3A3158"));
            p.Rect(wx, wy + wh * 0.55f, ww, 10f, H_("3A3158"));
            p.Shadow(wx - 30f, wy - 40f, ww + 60f, 22f, 6f, 14f, 0.25f);
            p.RoundedRect(wx - 36f, wy - 34f, ww + 72f, 22f, 6f, H_("6E5A8C"), H_("4E4374"));

            // Desk under the window: wooden top, legs, and everything on it.
            float dy = FloorY + 150f;
            p.ShadowEllipse(340f, FloorY + 6f, 320f, 26f, 26f, 0.25f);
            p.Rect(70f, FloorY, 18f, 150f, H_("5C4234"));
            p.Rect(590f, FloorY, 18f, 150f, H_("5C4234"));
            p.RoundedRect(40f, dy - 8f, 600f, 30f, 8f, H_("B98A6A"), H_("8E6248"));
            p.Rect(40f, dy + 16f, 600f, 4f, A("FFFFFF", 0.22f));
            // Laptop with a glowing screen.
            p.Circle(360f, dy + 100f, 150f, A("9FD7FF", 0.16f), 110f);
            p.RoundedRect(250f, dy + 22f, 230f, 14f, 6f, H_("D9D6E6"), H_("B7B2CC"));
            p.RoundedRect(268f, dy + 34f, 190f, 130f, 8f, H_("2B2750"), H_("221C40"));
            p.RoundedRect(278f, dy + 44f, 170f, 110f, 5f, H_("BFE6FF"), H_("7FC2F5"));
            p.Rect(292f, dy + 120f, 90f, 6f, A("FFFFFF", 0.6f));
            p.Rect(292f, dy + 100f, 130f, 6f, A("FFFFFF", 0.45f));
            p.Rect(292f, dy + 80f, 60f, 6f, A("FFFFFF", 0.45f));
            // Mug with steam.
            p.RoundedRect(510f, dy + 22f, 54f, 62f, 10f, H_("FFE1EA"), H_("F5B8C8"));
            p.Circle(572f, dy + 52f, 16f, H_("F5B8C8"));
            p.Circle(572f, dy + 52f, 8f, H_("6A5A8E"));
            p.Line(524f, dy + 96f, 530f, dy + 130f, 4f, A("FFFFFF", 0.35f), 3f);
            p.Line(546f, dy + 96f, 540f, dy + 134f, 4f, A("FFFFFF", 0.3f), 3f);
            // Desk lamp on the left with a warm cone of light.
            p.Ellipse(150f, dy + 24f, 44f, 10f, 0f, H_("3A3158"), H_("2E2748"));
            p.Line(150f, dy + 26f, 150f, dy + 150f, 8f, H_("3A3158"));
            p.Line(150f, dy + 150f, 200f, dy + 190f, 8f, H_("3A3158"));
            p.Ellipse(230f, dy + 120f, 120f, 70f, -25f, A("FFC985", 0.28f), 40f);
            p.RoundedRect(176f, dy + 168f, 82f, 46f, 14f, H_("FFB59E"), H_("F2997F"));
            p.Circle(217f, dy + 172f, 22f, A("FFF1C2", 0.9f), 8f);
            // Books and headphones on the right.
            p.RoundedRect(456f, dy + 22f, 90f, 18f, 4f, H_("CDBBFF"), H_("B39DFF"));
            p.RoundedRect(462f, dy + 40f, 78f, 16f, 4f, H_("A8E6CF"), H_("8CD3B5"));
            p.RoundedRect(470f, dy + 56f, 64f, 14f, 4f, H_("FFD98E"), H_("F2C14E"));
            p.Circle(110f, dy + 44f, 24f, H_("3A3158"));
            p.Circle(174f, dy + 44f, 24f, H_("3A3158"));
            p.Line(112f, dy + 62f, 142f, dy + 92f, 8f, H_("3A3158"));
            p.Line(142f, dy + 92f, 172f, dy + 62f, 8f, H_("3A3158"));
            p.Circle(110f, dy + 44f, 12f, H_("FFB59E"));
            p.Circle(174f, dy + 44f, 12f, H_("FFB59E"));

            // Posters and a shelf with a radio on the right wall.
            p.Shadow(700f, 1360f, 190f, 250f, 10f, 20f, 0.25f);
            p.RoundedRect(694f, 1366f, 190f, 250f, 10f, H_("F7EEE4"), H_("E8DCCF"));
            p.RoundedRect(708f, 1380f, 162f, 222f, 6f, H_("7A4E8C"), H_("F0A27C"));
            p.SetClip(708f, 1380f, 162f, 222f);
            p.Ellipse(760f, 1400f, 90f, 70f, 0f, H_("3A3158"), H_("2E2748"));
            p.Ellipse(850f, 1390f, 80f, 90f, 0f, H_("2E2748"), H_("241F45"));
            p.Circle(830f, 1560f, 20f, H_("FFF1B8"));
            p.ClearClip();
            p.Shadow(900f, 1170f, 150f, 200f, 10f, 18f, 0.25f);
            p.RoundedRect(896f, 1176f, 150f, 200f, 10f, H_("F7EEE4"), H_("E8DCCF"));
            p.RoundedRect(908f, 1188f, 126f, 176f, 6f, H_("FFD6DF"), H_("FF9EBB"));
            p.Circle(958f, 1290f, 26f, H_("FFFFFF"));
            p.Circle(984f, 1290f, 26f, H_("FFFFFF"));
            p.Ellipse(971f, 1262f, 44f, 34f, 0f, H_("FFFFFF"));
            p.Shadow(700f, 1116f, 350f, 18f, 6f, 16f, 0.28f);
            p.RoundedRect(694f, 1122f, 350f, 18f, 6f, H_("B98A6A"), H_("8E6248"));
            p.RoundedRect(720f, 1140f, 150f, 80f, 12f, H_("D9C6B0"), H_("B8A08A"));
            p.Circle(760f, 1180f, 24f, H_("3A3158"));
            p.Circle(760f, 1180f, 10f, H_("6A5A8E"));
            p.RoundedRect(800f, 1160f, 56f, 8f, 4f, H_("3A3158"));
            p.RoundedRect(800f, 1178f, 56f, 8f, 4f, H_("3A3158"));
            p.RoundedRect(800f, 1196f, 30f, 8f, 4f, H_("3A3158"));
            p.RoundedRect(896f, 1140f, 40f, 34f, 10f, H_("FFB59E"), H_("F2997F"));
            p.Ellipse(916f, 1196f, 12f, 26f, 0f, H_("7FCB97"), H_("5FAE7C"));
            p.Ellipse(900f, 1188f, 10f, 20f, 30f, H_("7FCB97"), H_("5FAE7C"));
            p.Ellipse(932f, 1188f, 10f, 20f, -30f, H_("7FCB97"), H_("5FAE7C"));

            // Big plant on the right, darker leaves for the night room.
            p.ShadowEllipse(900f, FloorY + 4f, 96f, 22f, 18f, 0.3f);
            p.RoundedRect(838f, FloorY + 8f, 122f, 112f, 26f, H_("D08A70"), H_("A86A54"));
            p.RoundedRect(830f, FloorY + 106f, 138f, 26f, 10f, H_("E0A088"), H_("D08A70"));
            Color leafTop = H_("6FB98C"), leafBottom = H_("4E9A6E");
            p.Ellipse(899f, FloorY + 235f, 36f, 96f, 0f, leafTop, leafBottom);
            p.Ellipse(855f, FloorY + 205f, 32f, 84f, 30f, leafTop, leafBottom);
            p.Ellipse(943f, FloorY + 205f, 32f, 84f, -30f, leafTop, leafBottom);
            p.Ellipse(822f, FloorY + 162f, 28f, 66f, 58f, H_("5FAE7C"), H_("3F8A5E"));
            p.Ellipse(976f, FloorY + 162f, 28f, 66f, -58f, H_("5FAE7C"), H_("3F8A5E"));

            // String lights along the top: cable, then bulbs with a warm glow (the live bulbs twinkle over these).
            float px = 0f, py = StringY(0f);
            for (int i = 1; i <= 24; i++)
            {
                float x = i * (W / 24f), y = StringY(x);
                p.Line(px, py, x, y, 3f, H_("3A3158"));
                px = x; py = y;
            }
            for (int i = 0; i < 9; i++)
            {
                float x = (i + 0.5f) / 9f * W, y = StringY(x) - 12f;
                p.Circle(x, y, 26f, A("FFE9A8", 0.28f), 22f);
                p.RoundedRect(x - 4f, y + 6f, 8f, 10f, 2f, H_("3A3158"));
            }

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
