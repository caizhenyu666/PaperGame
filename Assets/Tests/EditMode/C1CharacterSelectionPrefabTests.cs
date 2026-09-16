using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Tests
{
    public sealed class C1CharacterSelectionPrefabTests
    {
        [TestCase(1920, 1080)]
        [TestCase(1094, 1016)]
        [TestCase(1280, 1024)]
        [TestCase(2560, 1080)]
        public void CharacterScreen_DifferentAspectRatiosKeepOneDesignScale(int width, int height)
        {
            var canvasObject = new GameObject("Responsive Character Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(width, height);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/C1UI/PaperGameCharacterSelection.prefab");
                var screen = Object.Instantiate(prefab, canvasObject.transform, false);
                Canvas.ForceUpdateCanvases();
                var rect = screen.GetComponent<RectTransform>();
                Assert.That(rect.rect.size, Is.EqualTo(new Vector2(1920, 1080)), "设计坐标不能随窗口宽高分别变形");
                var expectedScale = Mathf.Min(width / 1920f, height / 1080f);
                Assert.That(rect.localScale.x, Is.EqualTo(expectedScale).Within(.001f));
                Assert.That(rect.localScale.y, Is.EqualTo(expectedScale).Within(.001f));
                var background = screen.transform.Find("Background").GetComponent<RectTransform>();
                Assert.That(background.rect.width * expectedScale, Is.EqualTo(width).Within(.01f));
                Assert.That(background.rect.height * expectedScale, Is.EqualTo(height).Within(.01f));
            }
            finally { Object.DestroyImmediate(canvasObject); }
        }

        [Test]
        public void CharacterScreen_ResizingKeepsListInsideNotebook()
        {
            var canvasObject = new GameObject("Resize Character Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(1920, 1080);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/C1UI/PaperGameCharacterSelection.prefab");
                var screen = Object.Instantiate(prefab, canvasObject.transform, false);
                foreach (var size in new[] { new Vector2(1094, 1016), new Vector2(2560, 1080), new Vector2(1920, 1080) })
                {
                    canvasRect.sizeDelta = size;
                    screen.GetComponent<C1CharacterScreenFit>().RefreshLayout();
                    Canvas.ForceUpdateCanvases();
                    var notebook = screen.transform.Find("Notebook").GetComponent<RectTransform>();
                    var viewport = screen.transform.Find("Characters").GetComponent<RectTransform>();
                    var corners = new Vector3[4]; viewport.GetWorldCorners(corners);
                    foreach (var corner in corners)
                        Assert.That(notebook.rect.Contains(notebook.InverseTransformPoint(corner)), Is.True, "列表不能跑出纸张");
                    Assert.That(screen.transform.Find("Characters/Content").GetComponent<RectTransform>().rect.width,
                        Is.EqualTo(423).Within(.01f));
                    Assert.That(screen.transform.Find("Preview/Animated Character").GetComponent<RectTransform>().rect.height,
                        Is.EqualTo(360).Within(.01f));
                }
            }
            finally { Object.DestroyImmediate(canvasObject); }
        }

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
