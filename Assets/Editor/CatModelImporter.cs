using System;
using UnityEditor;
using UnityEngine;

namespace Gotchi.EditorTools
{
    // Import settings for the 3D cat (Assets/Resources/Creatures/Cat3D): legacy animation so the clips can be
    // driven from code without an AnimatorController asset; the Blender textures stay crisp and clamped.
    public class CatModelImporter : AssetPostprocessor
    {
        private static readonly string[] Loops = { "Idle", "Happy", "Sad", "Sleep", "Alert", "Walk", "Fainted" };

        private bool IsCat => assetPath.Replace('\\', '/').Contains("/Creatures/Cat3D/");

        private void OnPreprocessModel()
        {
            if (!IsCat) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.isReadable = false;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsCat) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            foreach (var clip in clips)
            {
                string name = clip.name;
                int bar = name.LastIndexOf('|');
                if (bar >= 0) name = name.Substring(bar + 1);
                clip.name = name;
                bool loop = Array.IndexOf(Loops, name) >= 0;
                clip.loopTime = loop;
                clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            }
            importer.clipAnimations = clips;
        }

        private void OnPreprocessTexture()
        {
            if (!IsCat) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 1024;
            importer.alphaSource = TextureImporterAlphaSource.None;
        }
    }
}
