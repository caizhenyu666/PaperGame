using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1CharacterAnimatorTests
    {
        private GameObject characterObject;
        private SpriteRenderer spriteRenderer;
        private C1CharacterAnimator2D animator;

        [SetUp]
        public void SetUp()
        {
            characterObject = new GameObject("Character Visual");
            spriteRenderer = characterObject.AddComponent<SpriteRenderer>();
            animator = characterObject.AddComponent<C1CharacterAnimator2D>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(characterObject);
        }

        [TestCase(false, 0f, C1CharacterAnimationState.Jump)]
        [TestCase(true, 1f, C1CharacterAnimationState.Run)]
        [TestCase(true, 0f, C1CharacterAnimationState.Idle)]
        public void ResolveState_UsesGroundedAndHorizontalSpeed(
            bool grounded,
            float horizontalSpeed,
            C1CharacterAnimationState expected)
        {
            Assert.That(animator.ResolveState(grounded, horizontalSpeed, false), Is.EqualTo(expected));
        }

        [Test]
        public void Tick_UsesVelocityToFlipCharacter()
        {
            animator.Tick(0f, true, -1f, false);
            Assert.That(spriteRenderer.flipX, Is.True);

            animator.Tick(0f, true, 1f, false);
            Assert.That(spriteRenderer.flipX, Is.False);
        }

        [Test]
        public void Tick_WhenCompleted_ReturnsToIdle()
        {
            animator.Tick(0f, false, 0f, true);

            Assert.That(animator.CurrentState, Is.EqualTo(C1CharacterAnimationState.Idle));
        }
    }
}
