using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Tests
{
    public sealed class C1CharacterSelectionPrefabTests
    {
        [Test]
        public void CharacterScreen_HasSeparateArtAndInteractiveControls()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/C1UI/PaperGameCharacterSelection.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(1920, 1080)));
            foreach (var name in new[] { "Back", "Create", "Use", "Run", "Jump", "Add Photo" })
                Assert.That(prefab.transform.Find(name).GetComponent<Button>(), Is.Not.Null, name);
            Assert.That(prefab.transform.Find("Preview/Animated Character").GetComponent<Image>().preserveAspect, Is.True);
        }

        [TestCase("default")]
        [TestCase("default-chick")]
        public void Library_PreservesBothBuiltInCharacters(string id)
        {
            var previous = PlayerPrefs.GetString(C1CharacterLibrary.StorageKey, "");
            try
            {
                new C1CharacterLibrary { selectedId = id }.Save();
                Assert.That(C1CharacterLibrary.Load().selectedId, Is.EqualTo(id));
            }
            finally
            {
                if (previous == "") PlayerPrefs.DeleteKey(C1CharacterLibrary.StorageKey);
                else PlayerPrefs.SetString(C1CharacterLibrary.StorageKey, previous);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Selection_UpdatesPreviewButOnlyConfirmPersists(bool confirm)
        {
            var previous = PlayerPrefs.GetString(C1CharacterLibrary.StorageKey, "");
            var root = new GameObject("Character Screen Test", typeof(RectTransform), typeof(Canvas));
            try
            {
                new C1CharacterLibrary().Save();
                var returned = false;
                var controller = root.AddComponent<C1CharacterSelection>();
                var screen = controller.CreateScreen(root.GetComponent<Canvas>(), () => returned = true);
                var list = screen.transform.Find("Characters/Content");
                Assert.That(list.childCount, Is.EqualTo(2));
                list.GetChild(1).GetComponent<Button>().onClick.Invoke();
                Assert.That(screen.transform.Find("Preview/Animated Character").GetComponent<Image>().sprite,
                    Is.EqualTo(C1BuiltInCharacters.Sprite(C1BuiltInCharacters.Chick)));
                Assert.That(C1CharacterLibrary.Load().selectedId, Is.EqualTo("default"));
                screen.transform.Find(confirm ? "Use" : "Back").GetComponent<Button>().onClick.Invoke();
                Assert.That(returned, Is.True);
                Assert.That(C1CharacterLibrary.Load().selectedId, Is.EqualTo(confirm ? "default-chick" : "default"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (previous == "") PlayerPrefs.DeleteKey(C1CharacterLibrary.StorageKey);
                else PlayerPrefs.SetString(C1CharacterLibrary.StorageKey, previous);
            }
        }
    }
}
