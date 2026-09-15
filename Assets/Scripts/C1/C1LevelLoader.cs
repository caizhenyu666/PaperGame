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
            if (level != null && !string.IsNullOrWhiteSpace(level.BackgroundResourcePath) &&
                level.BackgroundResourcePath.StartsWith("/", StringComparison.Ordinal))
            {
                level.BackgroundResourcePath = safePath + "-background";
            }
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

            if (data != null && !string.IsNullOrWhiteSpace(data.backgroundImage))
            {
                return CreateLocalLevel(data, out error);
            }

            ApiResponseJson apiResponse;
            try
            {
                apiResponse = JsonUtility.FromJson<ApiResponseJson>(json);
            }
            catch (ArgumentException)
            {
                error = "Level json is not valid json.";
                return null;
            }

            if (apiResponse?.result?.level == null)
            {
                error = "Level response is missing its level data.";
                return null;
            }

            return CreateApiLevel(apiResponse.result.level, out error);
        }

        private static C1LevelDefinition CreateLocalLevel(LevelJson data, out string error)
        {
            data.platforms = data.platforms ?? Array.Empty<PlatformJson>();
            var level = new C1LevelDefinition
            {
                BackgroundResourcePath = data.backgroundImage,
                CanvasPixelSize = new Vector2Int(data.canvas.width, data.canvas.height),
                PlayerStart = data.playerStart,
                GoalRegion = new Rect(data.goalRegion.x, data.goalRegion.y, data.goalRegion.width, data.goalRegion.height),
                Platforms = new C1PlatformDefinition[data.platforms.Length],
                Walls = CreateWalls(data.walls),
                Blocks = CreateBlocks(data.blocks)
            };

            for (var index = 0; index < data.platforms.Length; index++)
            {
                var platform = data.platforms[index];
                level.Platforms[index] = new C1PlatformDefinition(
                    new Vector2(platform.x1, platform.y1), new Vector2(platform.x2, platform.y2));
            }

            return level.TryValidate(out error) ? level : null;
        }

        private static C1LevelDefinition CreateApiLevel(ApiLevelJson data, out string error)
        {
            data.platforms = data.platforms ?? Array.Empty<ApiPlatformJson>();
            var level = new C1LevelDefinition
            {
                PlayerStartIsFeet = true,
                BackgroundResourcePath = data.background.imageUrl,
                CanvasPixelSize = new Vector2Int(data.canvas.width, data.canvas.height),
                PlayerStart = data.playerStart.ToVector2(),
                GoalRegion = new Rect(data.goalRegion.x, data.goalRegion.y, data.goalRegion.width, data.goalRegion.height),
                Platforms = new C1PlatformDefinition[data.platforms.Length],
                Walls = CreateWalls(data.walls),
                Blocks = CreateBlocks(data.blocks)
            };

            for (var index = 0; index < data.platforms.Length; index++)
            {
                var platform = data.platforms[index];
                level.Platforms[index] = new C1PlatformDefinition(platform.start.ToVector2(), platform.end.ToVector2());
            }

            return level.TryValidate(out error) ? level : null;
        }

        private static C1WallDefinition[] CreateWalls(ApiPlatformJson[] data)
        {
            if (data == null) return Array.Empty<C1WallDefinition>();
            var walls = new C1WallDefinition[data.Length];
            for (var index = 0; index < data.Length; index++)
                walls[index] = new C1WallDefinition(data[index].start.ToVector2(), data[index].end.ToVector2());
            return walls;
        }

        private static C1BlockDefinition[] CreateBlocks(BlockJson[] data)
        {
            if (data == null) return Array.Empty<C1BlockDefinition>();
            var blocks = new C1BlockDefinition[data.Length];
            for (var index = 0; index < data.Length; index++)
            {
                var region = data[index].region;
                blocks[index] = new C1BlockDefinition(new Rect(region.x, region.y, region.width, region.height));
            }
            return blocks;
        }

        [Serializable]
        private struct BlockJson
        {
            public RegionJson region;
        }

        [Serializable]
        private sealed class LevelJson
        {
            public string backgroundImage;
            public CanvasJson canvas;
            public Vector2 playerStart;
            public PlatformJson[] platforms;
            public ApiPlatformJson[] walls;
            public BlockJson[] blocks;
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

        [Serializable]
        private sealed class ApiResponseJson
        {
            public ApiResultJson result;
        }

        [Serializable]
        private sealed class ApiResultJson
        {
            public ApiLevelJson level;
        }

        [Serializable]
        private sealed class ApiLevelJson
        {
            public CanvasJson canvas;
            public ApiBackgroundJson background;
            public ApiPointJson playerStart;
            public ApiPlatformJson[] platforms;
            public ApiPlatformJson[] walls;
            public BlockJson[] blocks;
            public RegionJson goalRegion;
        }

        [Serializable]
        private struct ApiBackgroundJson
        {
            public string imageUrl;
        }

        [Serializable]
        private struct ApiPointJson
        {
            public float x;
            public float y;

            public Vector2 ToVector2()
            {
                return new Vector2(x, y);
            }
        }

        [Serializable]
        private struct ApiPlatformJson
        {
            public ApiPointJson start;
            public ApiPointJson end;
        }
    }
}
