using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Tests
{
    public sealed class C1LevelSelectionTests
    {
        private string tempRoot;
        private GameObject selectionObject;
        private C1LevelSelection selection;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.SeenPreferenceKey);
            PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.MutedPreferenceKey);
            tempRoot = Path.Combine(Path.GetTempPath(), "PaperGameC1LevelSelectionTests-" + Guid.NewGuid().ToString("N"));
            selectionObject = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("C1UI/PaperGameLevelSelection"));
            selectionObject.name = "Level Selection Test";
            selection = selectionObject.GetComponent<C1LevelSelection>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(selectionObject);
            PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.SeenPreferenceKey);
            PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.MutedPreferenceKey);
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }

        [Test]
        public void Configure_EmptyLibraryStillShowsAndSelectsBuiltInFirstPage()
        {
            selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot));

            Assert.That(selection.LevelCount, Is.EqualTo(1));
            Assert.That(selection.IsBuiltInSelected, Is.True);
            Assert.That(selection.SelectedPageNumber, Is.EqualTo(1));
            Assert.That(selection.transform.Find("Level Book/Viewport/Content/第1页"), Is.Not.Null);
        }

        [Test]
        public void SelectingUserLevel_ListsItAsSecondPageAfterBuiltIn()
        {
            var record = CreateUserLevel();

            selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot));

            Assert.That(selection.LevelCount, Is.EqualTo(2));
            Assert.That(selection.IsBuiltInSelected, Is.True);
            selection.transform.Find("Level Book/Viewport/Content/" + record.title).GetComponent<Button>().onClick.Invoke();
            Assert.That(selection.SelectedPageNumber, Is.EqualTo(2));
            Assert.That(selection.IsBuiltInSelected, Is.False);
        }

        [Test]
        public void CreateButton_FirstUseShowsTutorialBeforeCamera()
        {
            var cameraCalls = 0;
            selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot), () => cameraCalls++);

            selection.transform.Find("Level Book/Create Level").GetComponent<Button>().onClick.Invoke();

            Assert.That(selection.TutorialVisible, Is.True);
            Assert.That(cameraCalls, Is.Zero);
            selection.GetComponentInChildren<C1LevelDrawingTutorialController>(true).Skip();
            Assert.That(cameraCalls, Is.EqualTo(1));
        }

        [Test]
        public void HelpButton_AlwaysShowsTutorialWithoutOpeningCamera()
        {
            PlayerPrefs.SetInt(C1LevelDrawingTutorialController.SeenPreferenceKey, 1);
            var cameraCalls = 0;
            selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot), () => cameraCalls++);

            selection.transform.Find("Header/Drawing Help").GetComponent<Button>().onClick.Invoke();

            Assert.That(selection.TutorialVisible, Is.True);
            selection.GetComponentInChildren<C1LevelDrawingTutorialController>(true).Skip();
            Assert.That(cameraCalls, Is.Zero);
        }

        [Test]
        public void CreateButton_AfterTutorialOpensCameraDirectly()
        {
            PlayerPrefs.SetInt(C1LevelDrawingTutorialController.SeenPreferenceKey, 1);
            var cameraCalls = 0;
            selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot), () => cameraCalls++);

            selection.transform.Find("Level Book/Create Level").GetComponent<Button>().onClick.Invoke();

            Assert.That(selection.TutorialVisible, Is.False);
            Assert.That(cameraCalls, Is.EqualTo(1));
        }

        [Test]
        public void Configure_CreateImmediatelyUsesTheSameFirstTimeTutorialFlow()
        {
            var cameraCalls = 0;

            selection.Configure(() => { }, true, new C1LevelLibrary(tempRoot), () => cameraCalls++);

            Assert.That(selection.TutorialVisible, Is.True);
            Assert.That(cameraCalls, Is.Zero);
        }

        [Test]
        public void Configure_NormalStateHidesRegenerateAndHasNoContinueControl()
        {
            selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot));

            Assert.That(selection.transform.Find("Action Bar/Regenerate").gameObject.activeSelf, Is.False);
            Assert.That(selection.transform.Find("继续生成"), Is.Null);
        }

        [Test]
        public void Configure_PendingPhotoShowsRegenerate()
        {
            var library = new C1LevelLibrary(tempRoot);
            library.BeginUpload(new byte[] { 1, 2, 3 }, "image/png", "https://example.test");

            selection.Configure(() => { }, false, library);

            var regenerate = selection.transform.Find("Action Bar/Regenerate");
            Assert.That(regenerate.gameObject.activeSelf, Is.True);
            Assert.That(regenerate.GetComponent<Button>().interactable, Is.True);
            Assert.That(selection.StatusText, Does.Contain("重新生成"));
        }

        private C1SavedLevel CreateUserLevel()
        {
            var id = Guid.NewGuid().ToString("N");
            var folder = Path.Combine(tempRoot, id);
            Directory.CreateDirectory(folder);
            const string response = "{\"jobId\":\"job_level_selection_test\",\"status\":\"ready\",\"result\":{\"level\":{\"schemaVersion\":\"1.0\",\"canvas\":{\"width\":4,\"height\":3},\"background\":{\"imageUrl\":\"/artifacts/test/rectified.png\"},\"playerStart\":{\"x\":1,\"y\":1},\"platforms\":[{\"start\":{\"x\":0,\"y\":1},\"end\":{\"x\":3,\"y\":1}}],\"goalRegion\":{\"x\":2,\"y\":0,\"width\":1,\"height\":1}}}}";
            File.WriteAllText(Path.Combine(folder, "response.json"), response);
            var texture = new Texture2D(4, 3);
            File.WriteAllBytes(Path.Combine(folder, "background.png"), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            File.WriteAllBytes(Path.Combine(folder, "source"), new byte[] { 1 });
            var record = new C1SavedLevel
            {
                id = id, jobId = "job_level_selection_test", title = "测试关卡",
                status = "ready", createdAt = DateTime.UtcNow.ToString("O")
            };
            File.WriteAllText(Path.Combine(folder, "entry.json"), JsonUtility.ToJson(record));
            return record;
        }
    }
}
