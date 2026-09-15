using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

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
        public void Build_BlockOnlyLevelPreservesEmptyConcaveCorner()
        {
            var level = C1LevelLoader.LoadDefault();
            level.Platforms = null;
            level.Blocks = new[] {
                new C1BlockDefinition(new Rect(200f, 180f, 30f, 120f)),
                new C1BlockDefinition(new Rect(230f, 270f, 150f, 30f))
            };
            Assert.That(bootstrap.Build(level), Is.True);
            var blocks = System.Array.FindAll(bootstrap.GetComponentsInChildren<BoxCollider2D>(),
                collider => collider.name == "Block");
            Assert.That(blocks, Has.Length.EqualTo(2));
            var empty = C1LevelSpace.PixelToWorld(new Vector2(300f, 220f), level.CanvasPixelSize);
            foreach (var block in blocks)
                Assert.That(block.OverlapPoint(empty), Is.False);
            Assert.That(bootstrap.GroundCount, Is.Zero);
        }

        [Test]
        public void Build_WallsFollowPixelEndpointsWithInvisibleSolidColliders()
        {
            var level = C1LevelLoader.LoadDefault();
            level.Walls = new[]
            {
                new C1WallDefinition(new Vector2(300f, 200f), new Vector2(300f, 450f)),
                new C1WallDefinition(new Vector2(400f, 200f), new Vector2(500f, 450f))
            };
            Assert.That(bootstrap.Build(level), Is.True);

            var walls = System.Array.FindAll(bootstrap.GetComponentsInChildren<BoxCollider2D>(),
                collider => collider.name == "Wall");
            Assert.That(walls.Length, Is.EqualTo(2));
            for (var i = 0; i < walls.Length; i++)
            {
                var start = C1LevelSpace.PixelToWorld(level.Walls[i].Start, level.CanvasPixelSize);
                var end = C1LevelSpace.PixelToWorld(level.Walls[i].End, level.CanvasPixelSize);
                var direction = end - start;
                Assert.That((Vector2)walls[i].transform.position, Is.EqualTo((start + end) * .5f));
                Assert.That(walls[i].size.x, Is.EqualTo(direction.magnitude).Within(.001f));
                Assert.That(walls[i].size.y, Is.EqualTo(C1LevelSpace.GroundThickness));
                Assert.That(Vector2.Distance(walls[i].transform.right, direction.normalized), Is.LessThan(.001f));
                Assert.That(walls[i].isTrigger, Is.False);
                Assert.That(walls[i].attachedRigidbody, Is.Null);
                Assert.That(walls[i].GetComponent<Renderer>(), Is.Null);
            }
            Assert.That(bootstrap.GroundCount, Is.EqualTo(7));
        }

        [Test]
        public void Build_BlocksUsePixelRectangleWithInvisibleSolidColliders()
        {
            var level = C1LevelLoader.LoadDefault();
            var region = new Rect(300f, 200f, 180f, 120f);
            level.Blocks = new[] { new C1BlockDefinition(region) };
            Assert.That(bootstrap.Build(level), Is.True);

            var blocks = System.Array.FindAll(bootstrap.GetComponentsInChildren<BoxCollider2D>(),
                collider => collider.name == "Block");
            Assert.That(blocks.Length, Is.EqualTo(1));
            Assert.That((Vector2)blocks[0].transform.position,
                Is.EqualTo(C1LevelSpace.PixelToWorld(region.center, level.CanvasPixelSize)));
            Assert.That(blocks[0].size, Is.EqualTo(region.size / C1LevelSpace.PixelsPerUnit));
            Assert.That(blocks[0].transform.rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(blocks[0].isTrigger, Is.False);
            Assert.That(blocks[0].attachedRigidbody, Is.Null);
            Assert.That(blocks[0].GetComponent<Renderer>(), Is.Null);
            Assert.That(bootstrap.FailurePanel.activeSelf, Is.False);
        }

        [Test]
        public void Build_RebuildAndReturnHomeClearWallsAndBlocks()
        {
            var level = C1LevelLoader.LoadDefault();
            level.Walls = new[] { new C1WallDefinition(new Vector2(300f, 200f), new Vector2(300f, 450f)) };
            level.Blocks = new[] { new C1BlockDefinition(new Rect(400f, 200f, 100f, 100f)) };
            bootstrap.Build(level);
            Assert.That(bootstrap.transform.Find("C1 Generated Level/Wall"), Is.Not.Null);
            Assert.That(bootstrap.transform.Find("C1 Generated Level/Block"), Is.Not.Null);

            bootstrap.Build(C1LevelLoader.LoadDefault());
            Assert.That(bootstrap.transform.Find("C1 Generated Level/Wall"), Is.Null);
            Assert.That(bootstrap.transform.Find("C1 Generated Level/Block"), Is.Null);
            Assert.That(bootstrap.GroundCount, Is.EqualTo(7));

            bootstrap.Build(level);
            bootstrap.ReturnHome();
            Assert.That(bootstrap.transform.Find("C1 Generated Level"), Is.Null);
            Assert.That(bootstrap.GroundCount, Is.Zero);
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
        public void Build_UsesPhotoBackgroundAndInvisiblePhysics()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());
            var background = GameObject.Find("Paper Background");
            Assert.That(background, Is.Not.Null);
            var backgroundRenderer = background.GetComponent<SpriteRenderer>();
            Assert.That(backgroundRenderer, Is.Not.Null);
            Assert.That(backgroundRenderer.sprite.texture.width, Is.EqualTo(1245));
            Assert.That(backgroundRenderer.sprite.texture.height, Is.EqualTo(810));

            var colliders = bootstrap.GetComponentsInChildren<BoxCollider2D>();
            foreach (var candidate in colliders)
            {
                if (candidate.gameObject.name == "Ground")
                {
                    Assert.That(candidate.GetComponent<SpriteRenderer>(), Is.Null);
                }
            }

            var goal = GameObject.Find("Goal Flag");
            Assert.That(goal, Is.Not.Null);
            Assert.That(goal.GetComponent<SpriteRenderer>(), Is.Null);
            Assert.That(goal.transform.Find("Flag Pole"), Is.Null);
            Assert.That(goal.transform.Find("Flag"), Is.Null);
        }

        [Test]
        public void Build_MapsPixelLineAndGoalToSameBackgroundSpace()
        {
            var level = C1LevelLoader.LoadDefault();
            bootstrap.Build(level);

            var grounds = bootstrap.GetComponentsInChildren<BoxCollider2D>();
            BoxCollider2D firstGround = null;
            foreach (var collider in grounds)
            {
                if (collider.gameObject.name == "Ground")
                {
                    firstGround = collider;
                    break;
                }
            }

            Assert.That(firstGround, Is.Not.Null);
            var expectedStart = C1LevelSpace.PixelToWorld(level.Platforms[0].Start, level.CanvasPixelSize);
            var expectedEnd = C1LevelSpace.PixelToWorld(level.Platforms[0].End, level.CanvasPixelSize);
            Assert.That((Vector2)firstGround.transform.position, Is.EqualTo((expectedStart + expectedEnd) * 0.5f));
            Assert.That(firstGround.size.x, Is.EqualTo(Vector2.Distance(expectedStart, expectedEnd)).Within(0.001f));

            var goalCollider = GameObject.Find("Goal Flag").GetComponent<BoxCollider2D>();
            var expectedGoalCenter = C1LevelSpace.PixelToWorld(level.GoalRegion.center, level.CanvasPixelSize);
            Assert.That((Vector2)goalCollider.transform.position, Is.EqualTo(expectedGoalCenter));
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
        public void BuildSelectedLevel_UsesPendingSessionLevel()
        {
            C1GameSession.Instance.SetPendingLevel(C1GameSession.DefaultLevelResourcePath);

            var built = bootstrap.BuildSelectedLevel();

            Assert.That(built, Is.True);
            Assert.That(bootstrap.Player, Is.Not.Null);
            Assert.That(GameObject.Find("PaperGameHome"), Is.Null);
        }

        [Test]
        public void Build_MockApiLevelCreatesGeometryAlignedToProvidedImage()
        {
            var built = bootstrap.Build(C1LevelLoader.Load(C1GameSession.MockLevelApiResourcePath));

            Assert.That(built, Is.True);
            Assert.That(bootstrap.GroundCount, Is.EqualTo(4));
            Assert.That((Vector2)bootstrap.Player.transform.position,
                Is.EqualTo(C1LevelSpace.PixelToWorld(new Vector2(89f, 472f), new Vector2Int(900, 560))));
            var background = GameObject.Find("Paper Background").GetComponent<SpriteRenderer>().sprite.texture;
            Assert.That(background.width, Is.EqualTo(900));
            Assert.That(background.height, Is.EqualTo(560));
        }

        [Test]
        public void TryGetSelectedCharacter_ReturnsSavedCustomRecord()
        {
            var library = new C1CharacterLibrary { selectedId = "char_selected" };
            var expected = new C1CharacterRecord { characterId = "char_selected" };
            library.characters.Add(expected);

            var found = C1GameBootstrap.TryGetSelectedCharacter(library, out var actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void ApplyRemoteCharacter_ReplacesDynamicAnimatorFrames()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());
            var texture = new Texture2D(100, 100);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 100f, 100f), new Vector2(.5f, 0f), 100f);
            var frames = new C1CharacterFrames
            {
                run = new[] { sprite },
                jump = new[] { sprite },
                runFps = 15f,
                jumpFps = 12f
            };

            var applied = C1GameBootstrap.ApplyRemoteCharacter(bootstrap.Player, frames);

            var animator = bootstrap.Player.GetComponentInChildren<C1CharacterAnimator2D>();
            Assert.That(applied, Is.True);
            Assert.That(animator.IdleFrameCount, Is.EqualTo(1));
            Assert.That(animator.RunFrameCount, Is.EqualTo(1));
            Assert.That(animator.JumpFrameCount, Is.EqualTo(1));
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void ReturnHome_ClearsGeneratedLevelAndRequestsHomeScene()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());

            bootstrap.ReturnHome();

            Assert.That(bootstrap.Player, Is.Null);
            Assert.That(bootstrap.RequestedSceneName, Is.EqualTo(C1GameSession.HomeSceneName));
        }

        [Test]
        public void Build_CreatesMobileControlsAndPhotoCaptureHud()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());

            var mobileControlsObject = GameObject.Find("Mobile Controls");
            Assert.That(mobileControlsObject, Is.Not.Null);
            Assert.That(mobileControlsObject.GetComponent<C1MobileControls>(), Is.Not.Null);
            Assert.That(GameObject.Find("Move Left").GetComponent<C1TouchDirectionButton>(), Is.Not.Null);
            Assert.That(GameObject.Find("Move Right").GetComponent<C1TouchDirectionButton>(), Is.Not.Null);
            Assert.That(GameObject.Find("Jump").GetComponent<C1TouchJumpButton>(), Is.Not.Null);
            Assert.That(GameObject.Find("Capture Photo").GetComponent<Button>(), Is.Not.Null);
            Assert.That(bootstrap.HudCanvas.GetComponent<C1PhotoCapture>(), Is.Not.Null);
        }


        [Test]
        public void Build_DoesNotCreateMobileOrientationHint()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());

            Assert.That(bootstrap.HudCanvas.GetComponent<C1MobileOrientationHint>(), Is.Null);
            Assert.That(GameObject.Find("Rotate Device Overlay"), Is.Null);
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
        public void Build_CharacterVisualLoadsOfficialBuiltInCharacter()
        {
            bootstrap.Build(C1LevelLoader.LoadDefault());

            var visual = bootstrap.Player.transform.Find("Character Visual");
            Assert.That(visual, Is.Not.Null);
            var animator = visual.GetComponent<C1CharacterAnimator2D>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.IdleFrameCount, Is.EqualTo(1));
            Assert.That(animator.RunFrameCount, Is.EqualTo(1));
            Assert.That(animator.JumpFrameCount, Is.EqualTo(1));
            Assert.That(visual.GetComponent<SpriteRenderer>().sprite, Is.EqualTo(C1BuiltInCharacters.Sprite(C1CharacterLibrary.Load().selectedId)));
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
