using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1MobileControlsTests
    {
        private GameObject playerObject;
        private GameObject controlsObject;
        private C1PlayerController2D controller;
        private C1MobileControls controls;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("Test Player");
            playerObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
            playerObject.AddComponent<BoxCollider2D>();
            controller = playerObject.AddComponent<C1PlayerController2D>();

            controlsObject = new GameObject("Test Mobile Controls");
            controls = controlsObject.AddComponent<C1MobileControls>();
            controls.Configure(controller);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(controlsObject);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void DirectionButtons_CombineAndReleaseWithoutLeavingStuckInput()
        {
            controls.PressLeft();
            Assert.That(controller.TouchHorizontalInput, Is.EqualTo(-1f));

            controls.PressRight();
            Assert.That(controller.TouchHorizontalInput, Is.EqualTo(0f));

            controls.ReleaseLeft();
            Assert.That(controller.TouchHorizontalInput, Is.EqualTo(1f));

            controls.ReleaseRight();
            Assert.That(controller.TouchHorizontalInput, Is.EqualTo(0f));
        }

        [Test]
        public void DisablingControls_ClearsHeldDirection()
        {
            controls.PressLeft();

            typeof(C1MobileControls)
                .GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controls, null);

            Assert.That(controller.TouchHorizontalInput, Is.EqualTo(0f));
        }
    }
}
