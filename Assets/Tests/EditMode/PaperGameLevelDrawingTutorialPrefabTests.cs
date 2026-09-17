using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Tests
{
    public sealed class PaperGameLevelDrawingTutorialPrefabTests
    {
        private const string PrefabPath = "Assets/Resources/C1UI/PaperGameLevelDrawingTutorial.prefab";

        [Test]
        public void TutorialPrefab_ContainsSixPagesAndControls()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<C1LevelDrawingTutorialController>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Pages").childCount, Is.EqualTo(6));
            Assert.That(prefab.transform.Find("Next").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Skip").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Sound").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Replay").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Page Dots").childCount, Is.EqualTo(6));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void TutorialPrefab_PageUsesGeneratedArtwork(int page)
        {
            var path = $"Assets/Art/UI/LevelDrawingTutorial/tutorial-page-{page:00}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            Assert.That(sprite, Is.Not.Null, path);
        }

        [Test]
        public void TutorialPrefab_UsesSafeAreaForInteractiveControls()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var names = new[] { "Next", "Skip", "Sound", "Replay" };

            foreach (var name in names)
            {
                var rect = prefab.transform.Find(name).GetComponent<RectTransform>();
                Assert.That(rect.anchorMin.x, Is.GreaterThanOrEqualTo(.05f), name);
                Assert.That(rect.anchorMin.y, Is.GreaterThanOrEqualTo(.05f), name);
                Assert.That(rect.anchorMax.x, Is.LessThanOrEqualTo(.95f), name);
                Assert.That(rect.anchorMax.y, Is.LessThanOrEqualTo(.95f), name);
            }
        }

        [Test]
        public void TutorialPrefab_CoversOnlyBackgroundAndKeepsContentRootStretched()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var root = prefab.GetComponent<RectTransform>();

            Assert.That(root.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(root.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(prefab.GetComponent<C1CoverBackground>(), Is.Null);
            Assert.That(prefab.transform.Find("Background").GetComponent<C1CoverBackground>(), Is.Not.Null);
        }
    }
}
