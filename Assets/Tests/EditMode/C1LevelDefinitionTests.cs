using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1LevelDefinitionTests
    {
        [Test]
        public void TryValidate_AcceptsBlocksWithoutLinePlatforms()
        {
            var level = CreateValidLevel();
            level.Platforms = System.Array.Empty<C1PlatformDefinition>();
            level.Blocks = new[] { new C1BlockDefinition(new Rect(20f, 220f, 150f, 30f)) };
            Assert.That(level.TryValidate(out var error), Is.True, error);
            level.Blocks = System.Array.Empty<C1BlockDefinition>();
            Assert.That(level.TryValidate(out error), Is.False);
        }

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

        [Test]
        public void NewGeometry_DefaultsToEmpty()
        {
            var level = CreateValidLevel();
            foreach (var name in new[] { "Walls", "Blocks" })
            {
                var property = typeof(C1LevelDefinition).GetProperty(name);
                Assert.That(property, Is.Not.Null, name + " must be supported");
                Assert.That((System.Array)property.GetValue(level), Has.Length.Zero);
            }
        }

        [TestCase("Walls", "Start")]
        [TestCase("Walls", "End")]
        [TestCase("Blocks", "Region")]
        public void TryValidate_RejectsNonFiniteGeometry(string collection, string coordinate)
        {
            var level = CreateValidLevel();
            var property = typeof(C1LevelDefinition).GetProperty(collection);
            Assert.That(property, Is.Not.Null);
            var itemType = property.PropertyType.GetElementType();
            var item = collection == "Walls"
                ? System.Activator.CreateInstance(itemType, new Vector2(20f, 20f), new Vector2(20f, 200f))
                : System.Activator.CreateInstance(itemType, new Rect(20f, 20f, 50f, 50f));
            itemType.GetProperty(coordinate).SetValue(item, collection == "Walls"
                ? (object)new Vector2(float.NaN, 20f)
                : new Rect(20f, 20f, float.PositiveInfinity, 50f));
            var items = System.Array.CreateInstance(itemType, 1);
            items.SetValue(item, 0);
            property.SetValue(level, items);

            Assert.That(level.TryValidate(out var error), Is.False);
            Assert.That(error, Does.Contain("finite"));
        }

        [Test]
        public void TryValidate_AcceptsLegacyVerticalPlatform()
        {
            var level = CreateValidLevel();
            level.Platforms[0].End = new Vector2(20f, 50f);
            Assert.That(level.TryValidate(out var error), Is.True, error);
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
