using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Tests
{
    public sealed class PaperGameHomePrefabTests
    {
        [Test]
        public void GenerateHomePrefab_CreatesIndependentOverlayImages()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/C1UI/PaperGameHome.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.Find("Background").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(prefab.transform.Find("Title").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(prefab.transform.Find("Start Play").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(prefab.transform.Find("Capture Level"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Image").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(prefab.transform.Find("Image (1)").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(prefab.transform.Find("Image (2)").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(prefab.transform.Find("Image (3)").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(prefab.transform.Find("Tutorial Card").GetComponent<Image>().sprite, Is.Not.Null);
        }
    }
}
