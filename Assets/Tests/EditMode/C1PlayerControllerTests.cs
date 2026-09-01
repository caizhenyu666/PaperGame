using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1PlayerControllerTests
    {
        private GameObject playerObject;
        private GameObject groundObject;
        private Rigidbody2D body;
        private C1PlayerController2D controller;

        [SetUp]
        public void SetUp()
        {
            groundObject = new GameObject("Test Ground");
            groundObject.transform.position = new Vector2(0f, -1f);
            var groundCollider = groundObject.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(5f, 1f);

            playerObject = new GameObject("Test Player");
            playerObject.transform.position = Vector2.zero;
            body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            playerObject.AddComponent<BoxCollider2D>();
            controller = playerObject.AddComponent<C1PlayerController2D>();
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(groundObject);
        }

        [Test]
        public void ApplyHorizontalInput_SetsConfiguredHorizontalVelocity()
        {
            controller.ApplyHorizontalInput(1f);

            Assert.That(body.velocity.x, Is.EqualTo(controller.MoveSpeed).Within(0.001f));
        }

        [Test]
        public void TryJump_WhenGrounded_SetsUpwardVelocity()
        {
            var jumped = controller.TryJump();

            Assert.That(jumped, Is.True);
            Assert.That(body.velocity.y, Is.EqualTo(controller.JumpSpeed).Within(0.001f));
        }

        [Test]
        public void TryJump_WhenAirborne_IsRejected()
        {
            playerObject.transform.position = new Vector2(0f, 3f);
            Physics2D.SyncTransforms();

            var jumped = controller.TryJump();

            Assert.That(jumped, Is.False);
            Assert.That(body.velocity.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void TryJump_CalledTwiceBeforePhysicsStep_OnlyFirstJumpSucceeds()
        {
            var firstJump = controller.TryJump();
            var secondJump = controller.TryJump();

            Assert.That(firstJump, Is.True);
            Assert.That(secondJump, Is.False);
        }

        [Test]
        public void SetJumpHeight_ChangesOnlyRuntimeJumpSpeed()
        {
            body.gravityScale = 3f;
            controller.SetJumpHeight(4f);

            Assert.That(controller.JumpSpeed, Is.EqualTo(C1JumpPhysics.SpeedForHeight(4f, 29.43f)).Within(0.001f));
            Assert.That(controller.MoveSpeed, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void Complete_StopsBodyAndRejectsFurtherInput()
        {
            controller.ApplyHorizontalInput(1f);
            controller.Complete();
            controller.ApplyHorizontalInput(-1f);

            Assert.That(controller.IsCompleted, Is.True);
            Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
        }

        [Test]
        public void Fall_FreezesBodyAndRejectsInputAndJump()
        {
            controller.Fall();

            Assert.That(controller.IsFallen, Is.True);
            controller.ApplyHorizontalInput(1f);
            Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
            Assert.That(controller.TryJump(), Is.False);
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
        }
    }
}
