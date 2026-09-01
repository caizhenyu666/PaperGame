using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1LevelLoaderTests
    {
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
            Assert.That(level.CanvasPixelSize, Is.EqualTo(new Vector2Int(1245, 810)));
            Assert.That(level.Platforms, Has.Length.EqualTo(7));
            Assert.That(level.PlayerStart, Is.EqualTo(new Vector2(110f, 626f)));
            Assert.That(level.Platforms[0].Start, Is.EqualTo(new Vector2(69f, 640f)));
            Assert.That(level.Platforms[6].End, Is.EqualTo(new Vector2(1173f, 229f)));
            Assert.That(level.GoalRegion, Is.EqualTo(new Rect(1088f, 150f, 80f, 90f)));
        }
    }
}
