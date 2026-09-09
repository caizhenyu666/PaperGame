using System;
using UnityEngine;

namespace PaperGame.C1
{
    public static class C1LevelLoader
    {
        private const string DefaultResourcePath = "C1Levels/level1";

        public static C1LevelDefinition LoadDefault()
        {
            return Load(DefaultResourcePath);
        }

        public static C1LevelDefinition Load(string resourcePath)
        {
            var safePath = string.IsNullOrWhiteSpace(resourcePath) ? DefaultResourcePath : resourcePath;
            var asset = Resources.Load<TextAsset>(safePath);
            if (asset == null)
            {
                Debug.LogError($"C1 level json is missing: Resources/{safePath}");
                return null;
            }

            var level = Parse(asset.text, out var error);
            if (level == null)
            {
                Debug.LogError($"C1 level json is invalid: {error}");
            }

            return level;
        }

        public static C1LevelDefinition Parse(string json, out string error)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Level json is empty.";
                return null;
            }

            LevelJson data;
            try
            {
                data = JsonUtility.FromJson<LevelJson>(json);
            }
            catch (ArgumentException)
            {
                error = "Level json is not valid json.";
                return null;
            }

            if (data == null || data.platforms == null || data.platforms.Length == 0)
            {
                error = "Level requires at least one platform.";
                return null;
            }

            var level = new C1LevelDefinition
            {
                BackgroundResourcePath = data.backgroundImage,
                CanvasPixelSize = new Vector2Int(data.canvas.width, data.canvas.height),
                PlayerStart = data.playerStart,
                GoalRegion = new Rect(data.goalRegion.x, data.goalRegion.y, data.goalRegion.width, data.goalRegion.height),
                Platforms = new C1PlatformDefinition[data.platforms.Length]
            };

            for (var index = 0; index < data.platforms.Length; index++)
            {
                var platform = data.platforms[index];
                level.Platforms[index] = new C1PlatformDefinition(
                    new Vector2(platform.x1, platform.y1),
                    new Vector2(platform.x2, platform.y2));
            }

            return level.TryValidate(out error) ? level : null;
        }

        [Serializable]
        private sealed class LevelJson
        {
            public string backgroundImage;
            public CanvasJson canvas;
            public Vector2 playerStart;
            public PlatformJson[] platforms;
            public RegionJson goalRegion;
        }

        [Serializable]
        private struct CanvasJson
        {
            public int width;
            public int height;
        }

        [Serializable]
        private struct PlatformJson
        {
            public float x1;
            public float y1;
            public float x2;
            public float y2;
        }

        [Serializable]
        private struct RegionJson
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }
    }
}
