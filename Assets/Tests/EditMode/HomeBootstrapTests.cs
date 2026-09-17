using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Tests
{
    public sealed class HomeBootstrapTests
    {
        private GameObject bootstrapObject;
        private HomeBootstrap bootstrap;

        [SetUp]
        public void SetUp()
        {
            bootstrapObject = new GameObject("Home Bootstrap Test");
            bootstrap = bootstrapObject.AddComponent<HomeBootstrap>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(bootstrapObject);
        }

        [Test]
        public void BuildHome_CreatesHomeWithoutCharacterScreen()
        {
            bootstrap.BuildHome();

            Assert.That(bootstrap.HomeScreen, Is.Not.Null);
            Assert.That(bootstrap.CharacterScreen, Is.Null);
            var legacyCaptureButton = bootstrap.HomeScreen.transform.Find("Capture Level");
            Assert.That(legacyCaptureButton == null || !legacyCaptureButton.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void BuildHome_Uses1920By1080AsTheReferenceResolution()
        {
            bootstrap.BuildHome();

            var scaler = bootstrapObject.GetComponentInChildren<CanvasScaler>();

            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
        }

        [Test]
        public void ShowCharacterCreation_HidesHomeAndCreatesScreenOnDemand()
        {
            bootstrap.BuildHome();

            bootstrap.ShowCharacterCreation();

            Assert.That(bootstrap.HomeScreen.activeSelf, Is.False);
            Assert.That(bootstrap.CharacterScreen, Is.Not.Null);
            Assert.That(bootstrap.CharacterScreen.activeSelf, Is.True);
        }

        [Test]
        public void ReturnHome_DestroysCharacterScreenAndRestoresHome()
        {
            bootstrap.BuildHome();
            bootstrap.ShowCharacterCreation();

            bootstrap.ReturnHome();

            Assert.That(bootstrap.CharacterScreen, Is.Null);
            Assert.That(bootstrap.HomeScreen.activeSelf, Is.True);
        }

        [Test]
        public void ShowLevels_InstantiatesFormalLevelSelectionPrefab()
        {
            bootstrap.BuildHome();

            bootstrap.ShowLevels();

            Assert.That(bootstrap.HomeScreen.activeSelf, Is.False);
            Assert.That(bootstrap.LevelScreen, Is.Not.Null);
            Assert.That(bootstrap.LevelScreen.name, Is.EqualTo("PaperGameLevelSelection"));
            Assert.That(bootstrap.LevelScreen.GetComponent<C1LevelSelection>(), Is.Not.Null);
            Assert.That(bootstrap.LevelScreen.transform.Find("Level Book/Viewport/Content/第1页"), Is.Not.Null);
        }

        [Test]
        public void ReturnHome_DestroysLevelSelectionAndRestoresHome()
        {
            bootstrap.BuildHome();
            bootstrap.ShowLevels();

            bootstrap.ReturnHome();

            Assert.That(bootstrap.LevelScreen, Is.Null);
            Assert.That(bootstrap.HomeScreen.activeSelf, Is.True);
        }
    }
}
