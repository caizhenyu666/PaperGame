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
            tempRoot = Path.Combine(Path.GetTempPath(), "PaperGameC1LevelSelectionTests-" + Guid.NewGuid().ToString("N"));
            selectionObject = new GameObject("Level Selection Test");
            selection = selectionObject.AddComponent<C1LevelSelection>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(selectionObject);
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }

        [Test]
        public void Configure_EmptyLibraryStillShowsAndSelectsBuiltInFirstPage()
        {
            selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot));

            Assert.That(selection.LevelCount, Is.EqualTo(1));
            Assert.That(selection.IsBuiltInSelected, Is.True);
            Assert.That(selection.SelectedPageNumber, Is.EqualTo(1));
            Assert.That(GameObject.Find("第1页"), Is.Not.Null);
        }

        [Test]
        public void SelectingUserLevel_ListsItAsSecondPageAfterBuiltIn()
        {
            var record = CreateUserLevel();

            selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot));

            Assert.That(selection.LevelCount, Is.EqualTo(2));
            Assert.That(selection.IsBuiltInSelected, Is.True);
            GameObject.Find(record.title).GetComponent<Button>().onClick.Invoke();
            Assert.That(selection.SelectedPageNumber, Is.EqualTo(2));
            Assert.That(selection.IsBuiltInSelected, Is.False);
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
