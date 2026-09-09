using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1GoalFlagTests
    {
        private GameObject playerObject;
        private GameObject goalObject;
        private C1PlayerController2D player;
        private C1GoalFlag goal;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("Test Player");
            playerObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
            playerObject.AddComponent<BoxCollider2D>();
            player = playerObject.AddComponent<C1PlayerController2D>();

            goalObject = new GameObject("Test Goal");
            goal = goalObject.AddComponent<C1GoalFlag>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(goalObject);
        }

        [Test]
        public void TryReach_NotifiesExactlyOnceAndCompletesPlayer()
        {
            var callCount = 0;
            goal.Reached += _ => callCount++;

            Assert.That(goal.TryReach(player), Is.True);
            Assert.That(goal.TryReach(player), Is.False);
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(goal.HasBeenReached, Is.True);
            Assert.That(player.IsCompleted, Is.True);
        }

        [Test]
        public void TryReach_RejectsMissingPlayer()
        {
            Assert.That(goal.TryReach(null), Is.False);
            Assert.That(goal.HasBeenReached, Is.False);
        }

        [Test]
        public void TryReach_RejectsFallenPlayer()
        {
            player.Fall();

            Assert.That(goal.TryReach(player), Is.False);
            Assert.That(goal.HasBeenReached, Is.False);
        }
    }
}
