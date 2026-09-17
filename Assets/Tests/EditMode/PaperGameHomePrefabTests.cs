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
            Assert.That(prefab.transform.Find("Capture Level"), Is.Null);
            Assert.That(prefab.transform.Find("Tutorial Card").GetComponent<Image>().sprite, Is.Not.Null);
        }
    }
}
