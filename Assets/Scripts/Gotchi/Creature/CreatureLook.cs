using System;
using System.Collections.Generic;
using Gotchi.Data;
using Gotchi.UI;
using UnityEngine;

namespace Gotchi.Creature
{
    public enum EarShape { None, Round, Tall, Pointy, Tiny, Floppy }
    public enum TailShape { None, Curl, Bushy, Stub, ThinCurl }
    public enum NoseKind { None, PinkTriangle, DarkDot, Snout, Beak }
    public enum MouthKind { Smile, Cat }
    public enum BodyPlan { Quadruped, Upright, Flat }

    [Flags]
    public enum Marks
    {
        None = 0, Stripes = 1, TailTip = 2, TailRings = 4, BrowSpots = 8, EyePatches = 16, DogPatch = 32,
        Tongue = 64, Cotton = 128, Spikes = 256, Flippers = 512, Muzzle = 1024, ChestPatch = 2048, OrangeFeet = 4096,
    }

    // Everything species-specific, as data: palette, silhouette, ears, tail, nose, markings.
    public struct CreatureLook
    {
        public Color Body, Belly, Ear, EarInner, TailTip;
        public EarShape Ears;
        public TailShape Tail;
        public NoseKind Nose;
        public MouthKind Mouth;
        public Marks Marks;
        public int Whiskers;
        public float BodyW, BodyH, Squircle, Taper, EyeSize;
        public BodyPlan Plan;

        public bool Has(Marks mark) => (Marks & mark) != 0;

        public static readonly Color Outline = UIFactory.Hex("3B2F45");
        public static readonly Color Ink = new Color(0.22f, 0.15f, 0.19f);

        // Outline tinted from the body colour (warm brown on orange, grey on white) — never plain black.
        public Color OutlineColor => Color.Lerp(Color.Lerp(Body, new Color(0.25f, 0.15f, 0.2f), 0.55f), Outline, 0.2f);

        // One flat shadow tone per species (the reference style: base colour + one darker band).
        public Color Shade => Color.Lerp(Body, new Color(0.35f, 0.2f, 0.3f), 0.12f);

        public Color EyeAccent
        {
            get
            {
                Color.RGBToHSV(EarInner, out float h, out float sat, out float v);
                if (sat < 0.15f) Color.RGBToHSV(Body, out h, out sat, out v);
                return Color.HSVToRGB(h, Mathf.Clamp01(Mathf.Max(sat, 0.45f)), 0.55f);
            }
        }

        private static CreatureLook Make(string body, string belly, string ear, string earInner, EarShape ears, TailShape tail, NoseKind nose,
            Marks marks = Marks.None, int whiskers = 0, MouthKind mouth = MouthKind.Smile, float w = 64f, float h = 56f, float squircle = 2.8f,
            float taper = 0.05f, float eyeSize = 1f, string tailTip = null, BodyPlan plan = BodyPlan.Quadruped)
        {
            return new CreatureLook
            {
                Plan = plan,
                Body = UIFactory.Hex(body), Belly = UIFactory.Hex(belly), Ear = UIFactory.Hex(ear), EarInner = UIFactory.Hex(earInner),
                TailTip = tailTip != null ? UIFactory.Hex(tailTip) : UIFactory.Hex(belly),
                Ears = ears, Tail = tail, Nose = nose, Marks = marks, Whiskers = whiskers, Mouth = mouth,
                BodyW = w, BodyH = h, Squircle = squircle, Taper = taper, EyeSize = eyeSize,
            };
        }

        public static readonly Dictionary<SpeciesType, CreatureLook> Looks = new Dictionary<SpeciesType, CreatureLook>
        {
            { SpeciesType.Bunny,    Make("FBF3F6", "FFE6EE", "FBF3F6", "FFB7CB", EarShape.Tall, TailShape.Stub, NoseKind.PinkTriangle, Marks.Cotton, 2, MouthKind.Smile, 62f, 58f, 2.8f, 0.06f) },
            { SpeciesType.Cat,      Make("F9B56E", "FFF3E0", "F9B56E", "F4A0B4", EarShape.Pointy, TailShape.Curl, NoseKind.PinkTriangle, Marks.Stripes | Marks.TailTip, 3, MouthKind.Cat) },
            { SpeciesType.Panda,    Make("FFFFFF", "F6F1F4", "3E3A45", "3E3A45", EarShape.Round, TailShape.Stub, NoseKind.DarkDot, Marks.EyePatches, 0, MouthKind.Smile, 66f, 58f, 2.6f, 0.04f) },
            { SpeciesType.RedPanda, Make("E8834F", "FFF1DC", "8C4A2F", "FFF1DC", EarShape.Round, TailShape.Bushy, NoseKind.DarkDot, Marks.Muzzle | Marks.BrowSpots | Marks.TailRings) },
            { SpeciesType.Seal,     Make("C3CDD8", "EDF1F5", "C3CDD8", "C3CDD8", EarShape.None, TailShape.None, NoseKind.DarkDot, Marks.Flippers, 3, MouthKind.Cat, 70f, 52f, 2.4f, 0.1f, 1.05f, null, BodyPlan.Flat) },
            { SpeciesType.Raccoon,  Make("A3ABB5", "E4E8EC", "6E7680", "E4E8EC", EarShape.Pointy, TailShape.Bushy, NoseKind.DarkDot, Marks.EyePatches | Marks.TailRings) },
            { SpeciesType.Penguin,  Make("47536A", "FFFFFF", "47536A", "47536A", EarShape.None, TailShape.Stub, NoseKind.Beak, Marks.Flippers | Marks.ChestPatch | Marks.OrangeFeet, 0, MouthKind.Smile, 60f, 62f, 2.4f, 0.14f, 1f, null, BodyPlan.Upright) },
            { SpeciesType.Fennec,   Make("F5DCB0", "FFF6E6", "F5DCB0", "FFC7D1", EarShape.Tall, TailShape.Bushy, NoseKind.DarkDot, Marks.Muzzle | Marks.TailTip, 0, MouthKind.Smile, 64f, 56f, 2.8f, 0.05f, 1f, "8A6A50") },
            { SpeciesType.Fox,      Make("FF9B54", "FFF6E6", "FF9B54", "FFFFFF", EarShape.Pointy, TailShape.Bushy, NoseKind.DarkDot, Marks.Muzzle | Marks.TailTip, 0, MouthKind.Smile, 64f, 56f, 2.8f, 0.05f, 1f, "FFFFFF") },
            { SpeciesType.Pig,      Make("FFB6C1", "FFD3DA", "FFB6C1", "FF8FA8", EarShape.Tiny, TailShape.ThinCurl, NoseKind.Snout, Marks.None, 0, MouthKind.Smile, 66f, 56f, 2.6f, 0.04f) },
            { SpeciesType.Otter,    Make("A8785A", "E3C8A6", "A8785A", "E3C8A6", EarShape.Tiny, TailShape.Curl, NoseKind.DarkDot, Marks.Muzzle, 2, MouthKind.Cat, 62f, 58f, 2.6f, 0.06f) },
            { SpeciesType.Hedgehog, Make("B69072", "FFF1DC", "8A6A50", "FFF1DC", EarShape.Tiny, TailShape.Stub, NoseKind.DarkDot, Marks.Spikes, 0, MouthKind.Smile, 64f, 54f, 2.4f, 0.06f) },
            { SpeciesType.Dog,      Make("DDA86B", "FFF1DC", "B8834B", "FFD9B0", EarShape.Floppy, TailShape.Curl, NoseKind.DarkDot, Marks.DogPatch | Marks.Tongue) },
        };

        public static CreatureLook For(SpeciesType species) => Looks.TryGetValue(species, out var look) ? look : Looks[SpeciesType.Cat];
    }
}
