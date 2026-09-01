using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1GameBootstrapTests
    {
        private GameObject bootstrapObject;
        private C1GameBootstrap bootstrap;

        [SetUp]
        public void SetUp()
        {
            bootstrapObject = new GameObject("Test Bootstrap");
            bootstrap = bootstrapObject.AddComponent<C1GameBootstrap>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(bootstrapObject);
        }

        [Test]
        public void Build_ValidLevelCreatesPlayerGroundsAndSingleGoal()
        {
            var built = bootstrap.Build(C1LevelLoader.LoadDefault());

            Assert.That(built, Is.True);
            Assert.That(bootstrap.GroundCount, Is.EqualTo(7));
            Assert.That(bootstrap.Player, Is.Not.Null);
            Assert.That(bootstrap.Goal, Is.Not.Null);
        }

        [Test]
        public void Build_InvalidLevelCreatesNothing()
        {
            var built = bootstrap.Build(new C1LevelDefinition());

            Assert.That(built, Is.False);
            Assert.That(bootstrap.GroundCount, Is.EqualTo(0));
            Assert.That(bootstrap.Player, Is.Null);
            Assert.That(bootstrap.Goal, Is.Null);
        }

        [Test]
        public void Build_GroundVisualBoundsMatchColliderBounds()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());
            var colliders = bootstrap.GetComponentsInChildren<BoxCollider2D>();
            BoxCollider2D groundCollider = null;

            foreach (var candidate in colliders)
            {
                if (candidate.gameObject.name == "Ground")
                {
                    groundCollider = candidate;
                    break;
                }
            }

            Assert.That(groundCollider, Is.Not.Null);
            var spriteRenderer = groundCollider.GetComponent<SpriteRenderer>();
            Assert.That(spriteRenderer, Is.Not.Null);
            Assert.That(spriteRenderer.bounds.size.x, Is.EqualTo(groundCollider.bounds.size.x).Within(0.001f));
            Assert.That(spriteRenderer.bounds.size.y, Is.EqualTo(groundCollider.bounds.size.y).Within(0.001f));
        }

        [Test]
        public void Build_CreatesHudWithHiddenCompletionPanel()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());

            Assert.That(bootstrap.HudCanvas, Is.Not.Null);
            Assert.That(bootstrap.CompletionPanel, Is.Not.Null);
            Assert.That(bootstrap.CompletionPanel.activeSelf, Is.False);
        }

        [Test]
        public void Build_CreatesPlaytestTuningPanelWithDefaultHeight()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());

            var panel = bootstrap.HudCanvas.GetComponentInChildren<C1PlaytestTuningPanel>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.JumpHeight, Is.EqualTo(C1PlaytestTuningPanel.MinimumJumpHeight).Within(0.001f));
        }

        [Test]
        public void Build_UsesFixedCameraAndCreatesCharacterAnimator()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());

            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Camera.main.GetComponent<C1FollowCamera>(), Is.Null);
            Assert.That(bootstrap.Player.GetComponentInChildren<C1CharacterAnimator2D>(), Is.Not.Null);
        }

        [Test]
        public void Build_CharacterVisualLoadsSlicedAnimationFrames()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());

            var visual = bootstrap.Player.transform.Find("Character Visual");
            Assert.That(visual, Is.Not.Null);
            var animator = visual.GetComponent<C1CharacterAnimator2D>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.IdleFrameCount, Is.EqualTo(6));
            Assert.That(animator.RunFrameCount, Is.EqualTo(8));
            Assert.That(animator.JumpFrameCount, Is.EqualTo(8));
        }

        [Test]
        public void Build_CreatesScreenBoundsAlignedWithViewEdges()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());

            var left = GameObject.Find("Left Bound");
            var right = GameObject.Find("Right Bound");
            Assert.That(left, Is.Not.Null);
            Assert.That(right, Is.Not.Null);

            var camera = Camera.main;
            var halfWidth = camera.orthographicSize * camera.aspect;
            var centerX = camera.transform.position.x;
            Assert.That(left.transform.position.x, Is.EqualTo(centerX - halfWidth - 0.5f).Within(0.01f));
            Assert.That(right.transform.position.x, Is.EqualTo(centerX + halfWidth + 0.5f).Within(0.01f));
        }

        [Test]
        public void Build_FallingPlayerShowsFailurePanelAndFreezesPlayer()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());

            bootstrap.Player.transform.position = new Vector3(0f, -100f, 0f);
            Object.FindObjectOfType<C1OutOfBoundsWatcher>().Evaluate();

            Assert.That(bootstrap.Player.IsFallen, Is.True);
            Assert.That(bootstrap.FailurePanel, Is.Not.Null);
            Assert.That(bootstrap.FailurePanel.activeSelf, Is.True);
        }
    }
}
