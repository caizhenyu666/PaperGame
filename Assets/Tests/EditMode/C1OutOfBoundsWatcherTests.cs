using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1OutOfBoundsWatcherTests
    {
        private GameObject playerObject;
        private C1PlayerController2D player;
        private GameObject watcherObject;
        private C1OutOfBoundsWatcher watcher;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("Player");
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<BoxCollider2D>();
            player = playerObject.AddComponent<C1PlayerController2D>();

            watcherObject = new GameObject("Watcher");
            watcher = watcherObject.AddComponent<C1OutOfBoundsWatcher>();
            watcher.Configure(player, -5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(watcherObject);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void Evaluate_BelowKillLineFallsPlayerAndNotifies()
        {
            var fell = 0;
            watcher.Fell += _ => fell++;
            playerObject.transform.position = new Vector3(0f, -6f, 0f);

            watcher.Evaluate();

            Assert.That(player.IsFallen, Is.True);
            Assert.That(fell, Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_AboveKillLineDoesNothing()
        {
            var fell = 0;
            watcher.Fell += _ => fell++;
            playerObject.transform.position = new Vector3(0f, 0f, 0f);

            watcher.Evaluate();

            Assert.That(player.IsFallen, Is.False);
            Assert.That(fell, Is.EqualTo(0));
        }
    }
}
