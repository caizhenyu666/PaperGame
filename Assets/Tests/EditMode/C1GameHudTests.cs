using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Tests
{
    public sealed class C1GameHudTests
    {
        private float originalTimeScale;

        [SetUp] public void SetUp() { originalTimeScale = Time.timeScale; Time.timeScale = 1f; }
        [TearDown] public void TearDown() { Time.timeScale = originalTimeScale; }

        [Test]
        public void Prefab_ContainsFormalCornerControlsAndHiddenPauseModal()
        {
            var prefab = Resources.Load<GameObject>("C1UI/PaperGameGameHUD");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.Find("Page Label/Page Text").GetComponent<Text>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Pause").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Move Left").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Move Right").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Jump").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Pause Modal/Return Home").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Pause Modal/Continue").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Pause Modal").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Configure_ShowsPageNumberAndPauseActionsRestoreTime()
        {
            var prefab = Resources.Load<GameObject>("C1UI/PaperGameGameHUD");
            var instance = UnityEngine.Object.Instantiate(prefab);
            var player = new GameObject("Player", typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(C1PlayerController2D));
            try
            {
                var returned = false;
                var hud = instance.GetComponent<C1GameHudController>();
                hud.Configure(player.GetComponent<C1PlayerController2D>(), 3, () => returned = true);
                Assert.That(hud.PageText, Is.EqualTo("第3页"));

                hud.PauseGame();
                Assert.That(hud.IsPaused, Is.True);
                Assert.That(Time.timeScale, Is.Zero);

                hud.ContinueGame();
                Assert.That(hud.IsPaused, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1f));

                hud.PauseGame();
                hud.ReturnHome();
                Assert.That(returned, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void Session_PreservesSelectedPageUntilGameBuilds()
        {
            var session = C1GameSession.Instance;
            session.SetPendingLocalLevel(Guid.NewGuid().ToString("N"), 4);
            Assert.That(session.CurrentPageNumber, Is.EqualTo(4));
            session.SetPendingLevel("C1Levels/level2");
            Assert.That(session.CurrentPageNumber, Is.EqualTo(2));
        }
    }
}
