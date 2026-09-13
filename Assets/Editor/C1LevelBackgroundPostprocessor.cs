using UnityEditor;
using UnityEngine;

namespace PaperGame.C1.Editor
{
    public sealed class C1LevelBackgroundPostprocessor : AssetPostprocessor
    {
        private const string LevelBackgroundDirectory = "Assets/Resources/C1Levels/";

        private void OnPreprocessTexture()
        {
            var normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith(LevelBackgroundDirectory) || !normalizedPath.EndsWith("-background.png"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
        }
    }
}
