using System.IO;
using UnityEditor;
using UnityEngine;

namespace PaperGame.C1.Editor
{
    /// <summary>
    /// 为 Resources/C1Character 下的角色精灵集配置统一切帧：
    /// Idle 切 6 帧，Run/Jump 各切 8 帧，pivot 居中，PPU 100。
    /// </summary>
    public sealed class C1CharacterSpritePostprocessor : AssetPostprocessor
    {
        private const string CharacterFolder = "Resources/C1Character/";
        private const int PixelsPerUnit = 100;

        private void OnPreprocessTexture()
        {
            var path = assetPath.Replace('\\', '/');
            if (!path.Contains(CharacterFolder))
            {
                return;
            }

            var frameCount = ResolveFrameCount(Path.GetFileNameWithoutExtension(path));
            if (frameCount == 0)
            {
                return;
            }

            var size = ReadPngSize(assetPath);
            if (size.x <= 0 || size.y <= 0)
            {
                Debug.LogWarning($"C1 character sprite has invalid size: {assetPath}");
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritesheet = BuildFrames(Path.GetFileNameWithoutExtension(path).ToLowerInvariant(), size, frameCount);
        }

        private static int ResolveFrameCount(string fileName)
        {
            switch (fileName.ToLowerInvariant())
            {
                case "idle":
                    return 6;
                case "run":
                case "jump":
                    return 8;
                default:
                    return 0;
            }
        }

        private static SpriteMetaData[] BuildFrames(string namePrefix, Vector2Int size, int frameCount)
        {
            var frames = new SpriteMetaData[frameCount];
            var frameWidth = size.x / (float)frameCount;
            for (var index = 0; index < frameCount; index++)
            {
                frames[index] = new SpriteMetaData
                {
                    name = $"{namePrefix}_{index:D2}",
                    rect = new Rect(index * frameWidth, 0f, frameWidth, size.y),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }

            return frames;
        }

        private static Vector2Int ReadPngSize(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var header = new byte[24];
                    if (stream.Read(header, 0, header.Length) != header.Length)
                    {
                        return Vector2Int.zero;
                    }

                    var width = ReadBigEndian(header, 16);
                    var height = ReadBigEndian(header, 20);
                    return new Vector2Int(width, height);
                }
            }
            catch (IOException error)
            {
                Debug.LogWarning($"C1 character sprite size read failed: {path} ({error.Message})");
                return Vector2Int.zero;
            }
        }

        private static int ReadBigEndian(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }
    }
}
