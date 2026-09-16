using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1LevelLoaderTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Parse_LoadsBlocksOnlyLevel(bool api)
        {
            const string local = "{\"backgroundImage\":\"test\",\"canvas\":{\"width\":400,\"height\":300},\"playerStart\":{\"x\":40,\"y\":220},\"platforms\":[],\"blocks\":[{\"region\":{\"x\":20,\"y\":250,\"width\":200,\"height\":30}}],\"goalRegion\":{\"x\":300,\"y\":50,\"width\":30,\"height\":50}}";
            var json = api ? "{\"result\":{\"level\":" + local.Replace("\"backgroundImage\":\"test\"", "\"background\":{\"imageUrl\":\"test\"}") + "}}" : local;
            var level = C1LevelLoader.Parse(json, out var error);
            Assert.That(level, Is.Not.Null, error);
            Assert.That(level.Platforms, Has.Length.Zero);
            Assert.That(level.Blocks, Has.Length.EqualTo(1));
        }

        private const string ApiNeedsFixResponse = @"{
            ""jobId"": ""level_6782be3607df"",
            ""status"": ""needs_fix"",
            ""result"": {
                ""level"": {
                    ""schemaVersion"": ""1.0"",
                    ""canvas"": { ""width"": 900, ""height"": 560 },
                    ""background"": { ""imageUrl"": ""/artifacts/level_6782be3607df/rectified.png"" },
                    ""playerStart"": { ""x"": 89, ""y"": 472 },
                    ""platforms"": [
                        { ""id"": ""platform_001"", ""start"": { ""x"": 73, ""y"": 472 }, ""end"": { ""x"": 293, ""y"": 472 } },
                        { ""id"": ""platform_002"", ""start"": { ""x"": 114, ""y"": 126 }, ""end"": { ""x"": 493, ""y"": 126 } }
                    ],
                    ""goalRegion"": { ""x"": 756, ""y"": 0, ""width"": 65, ""height"": 110 }
                },
                ""analysis"": { ""playability"": ""unreachable"" }
            }
        }";

        private const string ValidJson = @"{
            ""backgroundImage"": ""C1Levels/test-background"",
            ""canvas"": { ""width"": 400, ""height"": 300 },
            ""playerStart"": { ""x"": 40, ""y"": 260 },
            ""platforms"": [
                { ""x1"": 20, ""y1"": 270, ""x2"": 180, ""y2"": 270 },
                { ""x1"": 220, ""y1"": 190, ""x2"": 360, ""y2"": 180 }
            ],
            ""goalRegion"": { ""x"": 330, ""y"": 30, ""width"": 50, ""height"": 80 }
        }";

        [Test]
        public void Parse_OptionalGeometryDefaultsToEmpty()
        {
            foreach (var json in new[] { ValidJson, ApiNeedsFixResponse,
                ValidJson.Replace("\"goalRegion\"", "\"walls\": null, \"blocks\": null, \"goalRegion\"") })
            {
                var level = C1LevelLoader.Parse(json, out var error);
                Assert.That(error, Is.Empty);
                foreach (var name in new[] { "Walls", "Blocks" })
                {
                    var property = typeof(C1LevelDefinition).GetProperty(name);
                    Assert.That(property, Is.Not.Null, name + " must be supported");
                    Assert.That((System.Array)property.GetValue(level), Has.Length.Zero);
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Parse_LoadsWallsAndBlocks(bool api)
        {
            var geometry = "\"walls\":[{\"id\":\"wall_001\",\"start\":{\"x\":200,\"y\":20},\"end\":{\"x\":210,\"y\":200},\"confidence\":0.8}],\"blocks\":[{\"id\":\"block_001\",\"region\":{\"x\":230,\"y\":100,\"width\":50,\"height\":60,\"confidence\":0.9}}],";
            var json = (api ? ApiNeedsFixResponse : ValidJson).Replace("\"goalRegion\"", geometry + "\"goalRegion\"");
            var level = C1LevelLoader.Parse(json, out var error);
            Assert.That(error, Is.Empty);
            Assert.That(level, Is.Not.Null);
            var wallsProperty = typeof(C1LevelDefinition).GetProperty("Walls");
            var blocksProperty = typeof(C1LevelDefinition).GetProperty("Blocks");
            Assert.That(wallsProperty, Is.Not.Null);
            Assert.That(blocksProperty, Is.Not.Null);
            var walls = (System.Array)wallsProperty.GetValue(level);
            var blocks = (System.Array)blocksProperty.GetValue(level);
            Assert.That(walls, Has.Length.EqualTo(1));
            Assert.That(blocks, Has.Length.EqualTo(1));
            Assert.That(walls.GetValue(0).GetType().GetProperty("End").GetValue(walls.GetValue(0)), Is.EqualTo(new Vector2(210f, 200f)));
            Assert.That(blocks.GetValue(0).GetType().GetProperty("Region").GetValue(blocks.GetValue(0)), Is.EqualTo(new Rect(230f, 100f, 50f, 60f)));
        }

        [TestCase("\"walls\":[{\"start\":{\"x\":20,\"y\":20},\"end\":{\"x\":200,\"y\":30}}]", "vertical")]
        [TestCase("\"walls\":[{\"start\":{\"x\":20,\"y\":20},\"end\":{\"x\":20,\"y\":20}}]", "length")]
        [TestCase("\"walls\":[{\"start\":{\"x\":20,\"y\":20},\"end\":{\"x\":20,\"y\":301}}]", "canvas")]
        [TestCase("\"blocks\":[{\"region\":{\"x\":380,\"y\":20,\"width\":30,\"height\":50}}]", "canvas")]
        [TestCase("\"blocks\":[{\"region\":{\"x\":20,\"y\":20,\"width\":0,\"height\":50}}]", "positive")]
        public void Parse_RejectsInvalidGeometry(string geometry, string expectedError)
        {
            var json = ValidJson.Replace("\"goalRegion\"", geometry + ",\"goalRegion\"");
            Assert.That(C1LevelLoader.Parse(json, out var error), Is.Null);
            Assert.That(error, Does.Contain(expectedError));
        }

        [Test]
        public void Parse_ValidJsonBuildsPhotoPixelLevel()
        {
            var level = C1LevelLoader.Parse(ValidJson, out var error);

            Assert.That(error, Is.Empty);
            Assert.That(level, Is.Not.Null);
            Assert.That(level.Platforms, Has.Length.EqualTo(2));
            Assert.That(level.BackgroundResourcePath, Is.EqualTo("C1Levels/test-background"));
            Assert.That(level.CanvasPixelSize, Is.EqualTo(new Vector2Int(400, 300)));
            Assert.That(level.PlayerStart, Is.EqualTo(new Vector2(40f, 260f)));
            Assert.That(level.Platforms[0].Start, Is.EqualTo(new Vector2(20f, 270f)));
            Assert.That(level.Platforms[1].End, Is.EqualTo(new Vector2(360f, 180f)));
            Assert.That(level.GoalRegion, Is.EqualTo(new Rect(330f, 30f, 50f, 80f)));
        }

        [Test]
        public void Parse_ApiResponseBuildsLevelWithoutConsultingStatusOrAnalysis()
        {
            var level = C1LevelLoader.Parse(ApiNeedsFixResponse, out var error);

            Assert.That(error, Is.Empty);
            Assert.That(level, Is.Not.Null);
            Assert.That(level.CanvasPixelSize, Is.EqualTo(new Vector2Int(900, 560)));
            Assert.That(level.BackgroundResourcePath, Is.EqualTo("/artifacts/level_6782be3607df/rectified.png"));
            Assert.That(level.PlayerStart, Is.EqualTo(new Vector2(89f, 472f)));
            Assert.That(level.Platforms, Has.Length.EqualTo(2));
            Assert.That(level.Platforms[0].Start, Is.EqualTo(new Vector2(73f, 472f)));
            Assert.That(level.GoalRegion, Is.EqualTo(new Rect(756f, 0f, 65f, 110f)));
        }

        [Test]
        public void Parse_EmptyOrBrokenJsonReturnsError()
        {
            Assert.That(C1LevelLoader.Parse("", out var emptyError), Is.Null);
            Assert.That(emptyError, Is.Not.Empty);

            Assert.That(C1LevelLoader.Parse(@"{ ""platforms"": [] }", out var noPlatformError), Is.Null);
            Assert.That(noPlatformError, Is.Not.Empty);
        }

        [Test]
        public void Parse_OutOfCanvasPlatformReturnsError()
        {
            var json = @"{
                ""backgroundImage"": ""C1Levels/test-background"",
                ""canvas"": { ""width"": 400, ""height"": 300 },
                ""playerStart"": { ""x"": 40, ""y"": 260 },
                ""platforms"": [ { ""x1"": 20, ""y1"": 270, ""x2"": 401, ""y2"": 270 } ],
                ""goalRegion"": { ""x"": 330, ""y"": 30, ""width"": 50, ""height"": 80 }
            }";

            Assert.That(C1LevelLoader.Parse(json, out var error), Is.Null);
            Assert.That(error, Does.Contain("canvas"));
        }

        [Test]
        public void LoadDefault_ReturnsPhotoBackgroundPixelLevel()
        {
            var level = C1LevelLoader.LoadDefault();

            Assert.That(level, Is.Not.Null);
            Assert.That(level.BackgroundResourcePath, Is.EqualTo("C1Levels/level1-background"));
            Assert.That(level.CanvasPixelSize, Is.EqualTo(new Vector2Int(900, 560)));
            Assert.That(level.PlayerStartIsFeet, Is.True);
            Assert.That(level.PlayerStart, Is.EqualTo(new Vector2(150f, 184f)));
            Assert.That(level.Platforms, Has.Length.Zero);
            Assert.That(level.Walls, Has.Length.Zero);
            Assert.That(level.Blocks, Has.Length.EqualTo(13));
            Assert.That(level.Blocks[0].Region, Is.EqualTo(new Rect(18f, 315f, 229f, 30f)));
            Assert.That(level.Blocks[12].Region, Is.EqualTo(new Rect(816f, 111f, 48f, 12f)));
            Assert.That(level.GoalRegion, Is.EqualTo(new Rect(812f, 65f, 42f, 63f)));
        }

        [Test]
        public void LoadDefault_UsesBundledBackgroundAtCanvasResolution()
        {
            var level = C1LevelLoader.LoadDefault();
            var texture = Resources.Load<Texture2D>(level.BackgroundResourcePath);

            Assert.That(texture, Is.Not.Null);
            Assert.That(new Vector2Int(texture.width, texture.height), Is.EqualTo(level.CanvasPixelSize));
        }

        [Test]
        public void Load_MockApiResponseUsesItsPairedLocalBackground()
        {
            var level = C1LevelLoader.Load(C1GameSession.MockLevelApiResourcePath);

            Assert.That(level, Is.Not.Null);
            Assert.That(level.BackgroundResourcePath, Is.EqualTo("C1Levels/level-api-mock-background"));
            Assert.That(level.CanvasPixelSize, Is.EqualTo(new Vector2Int(900, 560)));
            Assert.That(level.Platforms, Has.Length.EqualTo(4));
        }
    }
}
