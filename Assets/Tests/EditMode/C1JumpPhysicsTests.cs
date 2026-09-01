using NUnit.Framework;

namespace PaperGame.C1.Tests
{
    public sealed class C1JumpPhysicsTests
    {
        [Test]
        public void SpeedForHeight_UsesGravityAndReturnsUpwardSpeed()
        {
            Assert.That(C1JumpPhysics.SpeedForHeight(4f, 29.43f), Is.EqualTo(15.344f).Within(0.001f));
        }

        [Test]
        public void SpeedForHeight_NegativeInputsReturnZero()
        {
            Assert.That(C1JumpPhysics.SpeedForHeight(-1f, 29.43f), Is.EqualTo(0f));
            Assert.That(C1JumpPhysics.SpeedForHeight(4f, -1f), Is.EqualTo(0f));
        }
    }
}
