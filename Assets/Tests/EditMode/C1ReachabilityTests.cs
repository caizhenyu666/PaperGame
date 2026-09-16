using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1ReachabilityTests
    {
        [Test]
        public void Analyze_FaithfulSketchNeedsHigherJumpHeight()
        {
            // The bundled default level is blocks-only (no platform graph), so the jump
            // feasibility analysis is exercised on a synthetic platform level instead.
            // Goal x sits above the second platform; the player starts on the first.
            var level = C1LevelLoader.LoadDefault();
            level.Platforms = new[]
            {
                new C1PlatformDefinition(new Vector2(40f, 505f), new Vector2(240f, 505f)),
                new C1PlatformDefinition(new Vector2(300f, 385f), new Vector2(500f, 385f))
            };
            level.GoalRegion = new Rect(380f, 330f, 20f, 20f);

            var result = C1Reachability.Analyze(level, 1.4f);

            Assert.That(result.CanReachGoal, Is.False);
            Assert.That(result.BlockedJumps, Is.Not.Empty);
        }

        [Test]
        public void Analyze_FaithfulSketchBecomesReachableAtFourPointOne()
        {
            var level = C1LevelLoader.LoadDefault();
            level.Platforms = new[]
            {
                new C1PlatformDefinition(new Vector2(40f, 505f), new Vector2(240f, 505f)),
                new C1PlatformDefinition(new Vector2(300f, 385f), new Vector2(500f, 385f))
            };
            level.GoalRegion = new Rect(380f, 330f, 20f, 20f);

            var result = C1Reachability.Analyze(level, 4.1f);

            Assert.That(result.CanReachGoal, Is.True);
        }
    }
}
