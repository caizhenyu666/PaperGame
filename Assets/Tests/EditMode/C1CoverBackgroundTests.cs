using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1CoverBackgroundTests
    {
        [TestCase(1672f, 941f, 1280f, 720f)]
        [TestCase(1672f, 941f, 2532f, 1170f)]
        [TestCase(1672f, 941f, 810f, 1440f)]
        public void CalculateSize_CoversParentWithoutChangingAspect(float iw, float ih, float pw, float ph)
        {
            var size = C1CoverBackground.CalculateSize(new Vector2(iw, ih), new Vector2(pw, ph));

            Assert.That(size.x, Is.GreaterThanOrEqualTo(pw));
            Assert.That(size.y, Is.GreaterThanOrEqualTo(ph));
            Assert.That(size.x / size.y, Is.EqualTo(iw / ih).Within(.0001f));
        }
    }
}
