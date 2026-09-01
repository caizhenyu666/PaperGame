using NUnit.Framework;

namespace PaperGame.C1.Tests
{
    public sealed class C1ReachabilityTests
    {
        [Test]
        public void Analyze_FaithfulSketchNeedsHigherJumpHeight()
        {
            var result = C1Reachability.Analyze(C1LevelLoader.LoadDefault(), 1.4f);

            Assert.That(result.CanReachGoal, Is.False);
            Assert.That(result.BlockedJumps, Is.Not.Empty);
        }

        [Test]
        public void Analyze_FaithfulSketchBecomesReachableAtFourPointOne()
        {
            var result = C1Reachability.Analyze(C1LevelLoader.LoadDefault(), 4.1f);

            Assert.That(result.CanReachGoal, Is.True);
        }
    }
}
