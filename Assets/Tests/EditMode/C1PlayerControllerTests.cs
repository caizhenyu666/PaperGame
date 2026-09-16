using System.Reflection;
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

        [TestCase(-1f)]
        [TestCase(1f)]
        public void HoldingInputAgainstWall_StillFalls(float direction)
        {
            var simulationMode = Physics2D.simulationMode;
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                groundObject.transform.position = new Vector2(direction, 3f);
                groundObject.GetComponent<BoxCollider2D>().size = new Vector2(1f, 12f);
                playerObject.transform.position = new Vector2(0f, 3f);
                body.gravityScale = 3f;
                body.velocity = new Vector2(0f, controller.JumpSpeed);
                Physics2D.SyncTransforms();

                for (var step = 0; step < 75; step++)
                {
                    SimulateFixedUpdate();
                    controller.ApplyHorizontalInput(direction);
                    Physics2D.Simulate(0.02f);
                }

                Assert.That(body.position.y, Is.LessThan(2f), "Holding forward must not suspend the player on a wall.");
                Assert.That(body.velocity.y, Is.LessThan(-1f));
            }
            finally
            {
                Physics2D.simulationMode = simulationMode;
            }
        }

        [TestCase(0.99f, 3f, 1f, 8f)]
        [TestCase(0f, 3.99f, 5f, 1f)]
        public void RefreshGroundedState_WhenTouchingWallOrCeiling_DoesNotAllowJump(
            float obstacleX, float obstacleY, float width, float height)
        {
            groundObject.transform.position = new Vector2(obstacleX, obstacleY);
            groundObject.GetComponent<BoxCollider2D>().size = new Vector2(width, height);
            playerObject.transform.position = new Vector2(0f, 3f);
            Physics2D.SyncTransforms();

            controller.RefreshGroundedState();

            Assert.That(controller.IsGrounded, Is.False);
            Assert.That(controller.TryJump(), Is.False);
        }

        [Test]
        public void TryJump_AfterLanding_AllowsAnotherJump()
        {
            var simulationMode = Physics2D.simulationMode;
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                body.gravityScale = 3f;
                Assert.That(controller.TryJump(), Is.True);

                for (var step = 0; step < 60; step++)
                {
                    SimulateFixedUpdate();
                    Physics2D.Simulate(0.02f);
                }

                controller.RefreshGroundedState();
                Assert.That(controller.IsGrounded, Is.True);
                Assert.That(controller.TryJump(), Is.True);
                Assert.That(body.velocity.y, Is.EqualTo(controller.JumpSpeed).Within(0.001f));
            }
            finally
            {
                Physics2D.simulationMode = simulationMode;
            }
        }

        [Test]
        public void ApplyHorizontalInput_SetsConfiguredHorizontalVelocity()
        {
            controller.ApplyHorizontalInput(1f);

            Assert.That(body.velocity.x, Is.EqualTo(controller.MoveSpeed).Within(0.001f));
        }

        [Test]
        public void ResolveHorizontalInput_UsesKeyboardFirstThenTouchFallback()
        {
            controller.SetTouchHorizontalInput(-1f);

            Assert.That(controller.ResolveHorizontalInput(1f), Is.EqualTo(1f));
            Assert.That(controller.ResolveHorizontalInput(0f), Is.EqualTo(-1f));
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
            controller.SetTouchHorizontalInput(1f);
            controller.ApplyHorizontalInput(1f);
            controller.Complete();
            controller.ApplyHorizontalInput(-1f);

            Assert.That(controller.IsCompleted, Is.True);
            Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
            Assert.That(controller.TouchHorizontalInput, Is.EqualTo(0f));
        }

        [Test]
        public void Fall_FreezesBodyAndRejectsInputAndJump()
        {
            controller.SetTouchHorizontalInput(-1f);
            controller.Fall();

            Assert.That(controller.IsFallen, Is.True);
            controller.ApplyHorizontalInput(1f);
            Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
            Assert.That(controller.TryJump(), Is.False);
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
            Assert.That(controller.TouchHorizontalInput, Is.EqualTo(0f));
        }

        private void SimulateFixedUpdate()
        {
            typeof(C1PlayerController2D).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
        }
    }
}
