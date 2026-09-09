using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1GameSessionTests
    {
        private GameObject sessionObject;
        private C1GameSession session;

        [SetUp]
        public void SetUp()
        {
            sessionObject = new GameObject("Game Session Test");
            session = sessionObject.AddComponent<C1GameSession>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(sessionObject);
        }

        [Test]
        public void ConsumePendingLevel_UsesSelectedPathThenClearsIt()
        {
            session.SetPendingLevel("C1Levels/level2");

            Assert.That(session.ConsumePendingLevel(), Is.EqualTo("C1Levels/level2"));
            Assert.That(session.ConsumePendingLevel(), Is.EqualTo(C1GameSession.DefaultLevelResourcePath));
        }

        [Test]
        public void SetPendingLevel_UsesDefaultForEmptyValue()
        {
            session.SetPendingLevel(string.Empty);

            Assert.That(session.ConsumePendingLevel(), Is.EqualTo(C1GameSession.DefaultLevelResourcePath));
        }
    }
}
