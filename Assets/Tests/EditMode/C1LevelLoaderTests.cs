using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1LevelLoaderTests
    {
        private const string ValidJson = @"{
            ""playerStart"": { ""x"": -12.5, ""y"": 0.8 },
            ""goalPosition"": { ""x"": 15.8, ""y"": 1.0 },
            ""platforms"": [
                { ""x"": -11.5, ""y"": -0.1, ""width"": 4.0, ""height"": 0.2 },
                { ""x"": 13.5, ""y"": 0.9, ""width"": 6.0, ""height"": 0.2 }
            ]
        }";

        [Test]
        public void Parse_ValidJsonBuildsLevelWithGoalAndPlatforms()
        {
            var level = C1LevelLoader.Parse(ValidJson, out var error);

            Assert.That(error, Is.Empty);
            Assert.That(level, Is.Not.Null);
            Assert.That(level.Platforms, Has.Length.EqualTo(2));
            Assert.That(level.PlayerStart, Is.EqualTo(new Vector2(-12.5f, 0.8f)));
            Assert.That(level.GoalPosition, Is.EqualTo(new Vector2(15.8f, 1f)));
            Assert.That(level.Platforms[0].Size, Is.EqualTo(new Vector2(4f, 0.2f)));
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
        public void Parse_InvalidPlatformSizeReturnsError()
        {
            var json = @"{
                ""playerStart"": { ""x"": 0, ""y"": 0 },
                ""goalPosition"": { ""x"": 1, ""y"": 0 },
                ""platforms"": [ { ""x"": 0, ""y"": 0, ""width"": -1, ""height"": 0.2 } ]
            }";

            Assert.That(C1LevelLoader.Parse(json, out var error), Is.Null);
            Assert.That(error, Does.Contain("positive"));
        }

        [Test]
        public void LoadDefault_ReturnsFaithfulLandscapeSketch()
        {
            var level = C1LevelLoader.LoadDefault();

            Assert.That(level, Is.Not.Null);
            Assert.That(level.Platforms, Has.Length.EqualTo(7));
            Assert.That(level.PlayerStart, Is.EqualTo(new Vector2(4.70f, 5.09f)));
            Assert.That(level.GoalPosition, Is.EqualTo(new Vector2(28.27f, 14.96f)));
            Assert.That(level.Platforms[0].Size.y, Is.EqualTo(0.08f).Within(0.001f));
            Assert.That(level.Platforms[6].Size.x, Is.EqualTo(17.96f).Within(0.001f));
        }
    }
}
