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
    }
}
