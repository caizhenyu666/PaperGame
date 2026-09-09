using NUnit.Framework;
using UnityEngine;

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
    }
}
