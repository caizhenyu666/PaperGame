using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1LevelDefinitionTests
    {
        [Test]
        public void TryValidate_RejectsMissingPlatforms()
        {
            var level = new C1LevelDefinition();

            Assert.That(level.TryValidate(out var error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void TryValidate_RejectsZeroLengthPlatform()
        {
            var level = CreateValidLevel();
            level.Platforms[0].End = level.Platforms[0].Start;

            Assert.That(level.TryValidate(out var error), Is.False);
            Assert.That(error, Does.Contain("length"));
        }

        [Test]
        public void TryValidate_RejectsNonFiniteCoordinates()
        {
            var level = CreateValidLevel();
            level.PlayerStart = new Vector2(float.NaN, 0f);

            Assert.That(level.TryValidate(out var error), Is.False);
            Assert.That(error, Does.Contain("finite"));
        }

        private static C1LevelDefinition CreateValidLevel()
        {
            return new C1LevelDefinition
            {
                BackgroundResourcePath = "C1Levels/test-background",
                CanvasPixelSize = new Vector2Int(400, 300),
                GoalRegion = new Rect(330f, 30f, 50f, 80f),
                PlayerStart = new Vector2(40f, 260f),
                Platforms = new[]
                {
                    new C1PlatformDefinition(new Vector2(20f, 270f), new Vector2(180f, 270f))
                }
            };
        }
    }
}
