using System.Collections.Generic;
using Gotchi.Data;
using UnityEngine;

namespace Gotchi.UI
{
    // Real pixel-art sprites live in Resources/Creatures/<species>.png; species without one fall back
    // to the procedural creature.
    public static class CreatureSprites
    {
        private static readonly Dictionary<SpeciesType, Sprite> Cache = new Dictionary<SpeciesType, Sprite>();

        // Height of the resampled pixel grid; the character keeps this chunkiness on every screen.
        public const int PixelHeight = 112;

        public static Sprite For(SpeciesType species)
        {
            if (Cache.TryGetValue(species, out var cached)) return cached;
            var source = Resources.Load<Sprite>("Creatures/" + species.ToString().ToLowerInvariant());
            Sprite sprite = source == null ? null : Resample(source);
            Cache[species] = sprite;
            return sprite;
        }

        private static Sprite Resample(Sprite source)
        {
            Texture2D src = source.texture;
            Color32[] pixels;
            try { pixels = src.GetPixels32(); }
            catch (System.Exception e) { Debug.LogWarning($"[CreatureSprites] {src.name} not readable ({e.GetType().Name}); using source sprite."); return source; }
            int srcW = src.width, srcH = src.height;
            int h = Mathf.Min(PixelHeight, srcH);
            int w = Mathf.Max(1, Mathf.RoundToInt(srcW * (h / (float)srcH)));
            var result = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int sy = Mathf.Min(srcH - 1, (int)((y + 0.5f) * srcH / h));
                for (int x = 0; x < w; x++)
                {
                    int sx = Mathf.Min(srcW - 1, (int)((x + 0.5f) * srcW / w));
                    result[y * w + x] = pixels[sy * srcW + sx];
                }
            }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(result);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
