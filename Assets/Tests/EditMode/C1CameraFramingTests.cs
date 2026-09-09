using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1CameraFramingTests
    {
        [Test]
        public void Calculate_WideBoundsFitInsideOrthographicView()
        {
            var bounds = new Bounds(new Vector3(1f, -0.5f, 0f), new Vector3(18f, 6f, 0f));

            var framing = C1CameraFraming.Calculate(bounds, 16f / 9f, 0.5f);

            Assert.That(framing.Center, Is.EqualTo((Vector2)bounds.center));
            Assert.That(framing.OrthographicSize * 2f, Is.GreaterThanOrEqualTo(bounds.size.y + 1f));
            Assert.That(framing.OrthographicSize * 2f * (16f / 9f), Is.GreaterThanOrEqualTo(bounds.size.x + 1f));
        }

        [Test]
        public void Calculate_InvalidAspectUsesSafeFallback()
        {
            var framing = C1CameraFraming.Calculate(new Bounds(Vector3.zero, Vector3.one), 0f, 0.5f);

            Assert.That(float.IsNaN(framing.OrthographicSize), Is.False);
            Assert.That(framing.OrthographicSize, Is.GreaterThan(0f));
        }

        [Test]
        public void Calculate_ExplicitCanvasKeepsPaperCenter()
        {
            var canvas = new Bounds(new Vector3(16f, 10.41f, 0f), new Vector3(32f, 20.82f, 0f));

            var framing = C1CameraFraming.Calculate(canvas, 16f / 9f, 0.7f);

            Assert.That(framing.Center, Is.EqualTo(new Vector2(16f, 10.41f)));
        }
    }
}
