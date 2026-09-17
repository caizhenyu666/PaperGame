using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Tests
{
    public sealed class PaperGameLevelSelectionPrefabTests
    {
        private const string PrefabPath = "Assets/Resources/C1UI/PaperGameLevelSelection.prefab";

        [Test]
        public void LevelSelectionPrefab_HasRequiredPaperLayoutAndControls()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<C1LevelSelection>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(1920, 1080)));
            foreach (var path in new[]
            {
                "Background",
                "Title",
                "Drawing Help",
                "Back Home",
                "Level Book/Viewport/Content/Level Item Template",
                "Level Book/Create Level",
                "Preview Paper/Level Preview",
                "Status",
                "Regenerate",
                "Play"
            })
            {
                Assert.That(prefab.transform.Find(path), Is.Not.Null, path);
            }

            Assert.That(prefab.transform.Find("Regenerate").gameObject.activeSelf, Is.False);
            Assert.That(prefab.transform.Find("Level Book/Viewport/Content/Level Item Template").gameObject.activeSelf,
                Is.False);
        }

        [Test]
        public void LevelSelectionPrefab_UsesPaperArtAndAspectPreservingPreview()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.Find("Background").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(prefab.transform.Find("Title").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(prefab.transform.Find("Level Book").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(prefab.transform.Find("Preview Paper").GetComponent<Image>().sprite, Is.Not.Null);
            var preview = prefab.transform.Find("Preview Paper/Level Preview");
            Assert.That(preview.GetComponent<RawImage>(), Is.Not.Null);
            Assert.That(preview.GetComponent<AspectRatioFitter>().aspectMode,
                Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
        }

        [TestCase(1920, 1080)]
        [TestCase(1094, 1016)]
        [TestCase(2560, 1080)]
        public void LevelSelectionPrefab_DifferentAspectRatiosKeepOneDesignScale(int width, int height)
        {
            var canvasObject = new GameObject("Responsive Level Selection Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(width, height);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                var screen = Object.Instantiate(prefab, canvasObject.transform, false);

                screen.GetComponent<C1LevelSelectionScreenFit>().RefreshLayout();
                Canvas.ForceUpdateCanvases();

                var rect = screen.GetComponent<RectTransform>();
                var expectedScale = Mathf.Min(width / 1920f, height / 1080f);
                Assert.That(rect.rect.size, Is.EqualTo(new Vector2(1920, 1080)));
                Assert.That(rect.localScale.x, Is.EqualTo(expectedScale).Within(.001f));
                Assert.That(rect.localScale.y, Is.EqualTo(expectedScale).Within(.001f));
                var background = screen.transform.Find("Background").GetComponent<RectTransform>();
                Assert.That(background.rect.width * expectedScale, Is.EqualTo(width).Within(.01f));
                Assert.That(background.rect.height * expectedScale, Is.EqualTo(height).Within(.01f));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
