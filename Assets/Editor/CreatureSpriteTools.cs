using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gotchi.EditorTools
{
    // Turns the validated reference art into game sprites: keys out the DaVinci cat's baked-in
    // checkerboard, crops the left seal from the ChatGPT pair, trims margins, writes to Resources/Creatures.
    public static class CreatureSpriteTools
    {
        private const string ReferenceDir = "references/creature-character/ai-creatures";
        private const string OutputDir = "Assets/Resources/Creatures";

        [MenuItem("Gotchi/Prepare Creature Sprites")]
        public static void PrepareCreatureSprites()
        {
            Directory.CreateDirectory(OutputDir);
            var cat = Load(Path.Combine(ReferenceDir, "davinci_a_single_cat_rendered_as_chunky_kawaii_pixel_art__.png"));
            KeyOutBackground(cat);
            Save(Trim(cat), "cat");

            var seals = Load(Path.Combine(ReferenceDir, "chatgpt seal.png"));
            var left = Crop(seals, 0, 0, seals.width / 2, seals.height);
            Save(Trim(left), "seal");
            AssetDatabase.Refresh();
            Debug.Log("[Gotchi] Creature sprites prepared.");
        }

        // Applies the import settings below to files that already existed before the postprocessor did.
        [MenuItem("Gotchi/Reimport Creature Sprites")]
        public static void ReimportCreatureSprites()
        {
            foreach (string file in Directory.GetFiles(OutputDir, "*.png"))
            {
                string asset = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(asset) as TextureImporter;
                if (importer == null) continue;
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
                Debug.Log("[Gotchi] Reimported " + asset);
            }
            AssetDatabase.Refresh();
        }

        private static Texture2D Load(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false)) throw new IOException("Could not load " + path);
            return texture;
        }

        private static void Save(Texture2D texture, string name)
        {
            File.WriteAllBytes(Path.Combine(OutputDir, name + ".png"), texture.EncodeToPNG());
        }

        // Flood-fills from the border through light, unsaturated (checkerboard) pixels and clears them.
        private static void KeyOutBackground(Texture2D texture)
        {
            int w = texture.width, h = texture.height;
            Color32[] pixels = texture.GetPixels32();
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();
            for (int x = 0; x < w; x++) { Push(queue, visited, pixels, x); Push(queue, visited, pixels, (h - 1) * w + x); }
            for (int y = 0; y < h; y++) { Push(queue, visited, pixels, y * w); Push(queue, visited, pixels, y * w + w - 1); }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                pixels[i] = new Color32(0, 0, 0, 0);
                int x = i % w, y = i / w;
                if (x > 0) Push(queue, visited, pixels, i - 1);
                if (x < w - 1) Push(queue, visited, pixels, i + 1);
                if (y > 0) Push(queue, visited, pixels, i - w);
                if (y < h - 1) Push(queue, visited, pixels, i + w);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private static void Push(Queue<int> queue, bool[] visited, Color32[] pixels, int index)
        {
            if (visited[index]) return;
            visited[index] = true;
            if (IsChecker(pixels[index])) queue.Enqueue(index);
        }

        private static bool IsChecker(Color32 c)
        {
            int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max - min < 14 && min > 170;
        }

        private static Texture2D Crop(Texture2D source, int x, int y, int w, int h)
        {
            var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
            result.SetPixels(source.GetPixels(x, y, w, h));
            result.Apply();
            return result;
        }

        private static Texture2D Trim(Texture2D source)
        {
            Color32[] pixels = source.GetPixels32();
            int w = source.width, h = source.height;
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (pixels[y * w + x].a > 8) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
            if (maxX < 0) return source;
            int pad = 4;
            minX = Mathf.Max(0, minX - pad); minY = Mathf.Max(0, minY - pad);
            maxX = Mathf.Min(w - 1, maxX + pad); maxY = Mathf.Min(h - 1, maxY + pad);
            return Crop(source, minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }

    public class CreatureSpritePostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Creatures/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.maxTextureSize = 1024;
        }
    }
}
