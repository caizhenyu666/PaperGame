using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Tests
{
    public sealed class C1LevelDrawingTutorialTests
    {
        private GameObject root;
        private C1LevelDrawingTutorialController controller;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.SeenPreferenceKey);
            PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.MutedPreferenceKey);
            root = BuildTutorialHierarchy();
            controller = root.AddComponent<C1LevelDrawingTutorialController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.SeenPreferenceKey);
            PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.MutedPreferenceKey);
        }

        [Test]
        public void Open_StartsAtFirstOfSixPages()
        {
            controller.Open(null);

            Assert.That(controller.PageCount, Is.EqualTo(6));
            Assert.That(controller.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(controller.IsOpen, Is.True);
            Assert.That(root.transform.Find("Pages/Page 1").gameObject.activeSelf, Is.True);
            Assert.That(root.transform.Find("Pages/Page 2").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Next_OnLastPage_RecordsSeenAndInvokesCreateOnce()
        {
            var calls = 0;
            controller.Open(() => calls++);

            for (var i = 0; i < 6; i++) controller.Next();
            controller.Next();

            Assert.That(C1LevelDrawingTutorialController.HasSeen, Is.True);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(controller.IsOpen, Is.False);
        }

        [Test]
        public void Skip_RecordsSeenAndInvokesCreateOnce()
        {
            var calls = 0;
            controller.Open(() => calls++);

            controller.Skip();
            controller.Skip();

            Assert.That(C1LevelDrawingTutorialController.HasSeen, Is.True);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(controller.IsOpen, Is.False);
        }

        [Test]
        public void HelpMode_DismissesWithoutInvokingCreate()
        {
            controller.Open(null);

            for (var i = 0; i < 6; i++) controller.Next();

            Assert.That(controller.IsOpen, Is.False);
            Assert.That(C1LevelDrawingTutorialController.HasSeen, Is.True);
        }

        [Test]
        public void Open_UpdatesPageDotsAndFinalButtonLabel()
        {
            controller.Open(null);

            for (var i = 0; i < 5; i++) controller.Next();

            Assert.That(root.transform.Find("Next/Label").GetComponent<Text>().text, Is.EqualTo("我画好啦"));
            Assert.That(root.transform.Find("Page Dots/Dot 6").GetComponent<Image>().color.a, Is.EqualTo(1f));
            Assert.That(root.transform.Find("Page Dots/Dot 1").GetComponent<Image>().color.a, Is.LessThan(1f));
        }

        [Test]
        public void ToggleMute_PersistsPreferenceAndUpdatesLabel()
        {
            controller.Open(null);

            controller.ToggleMute();

            Assert.That(controller.IsMuted, Is.True);
            Assert.That(PlayerPrefs.GetInt(C1LevelDrawingTutorialController.MutedPreferenceKey), Is.EqualTo(1));
            Assert.That(root.transform.Find("Sound/Label").GetComponent<Text>().text, Is.EqualTo("开启声音"));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void VoiceClip_ExistsForEveryPage(int page)
        {
            var clip = Resources.Load<AudioClip>($"C1TutorialAudio/page-{page:00}");

            Assert.That(clip, Is.Not.Null, $"Missing voice clip for page {page}");
        }

        [Test]
        public void PageAnimator_BobMovesAndResetRestoresPose()
        {
            var animated = new GameObject("Animated Art", typeof(RectTransform));
            animated.transform.localPosition = new Vector3(12f, 34f, 0f);
            var animator = animated.AddComponent<C1LevelDrawingTutorialPageAnimator>();
            animator.Configure(C1TutorialAnimationKind.Bob);

            animator.Tick(.5f);
            Assert.That(animated.transform.localPosition, Is.Not.EqualTo(new Vector3(12f, 34f, 0f)));

            animator.ResetPose();
            Assert.That(animated.transform.localPosition, Is.EqualTo(new Vector3(12f, 34f, 0f)));
            Object.DestroyImmediate(animated);
        }

        private static GameObject BuildTutorialHierarchy()
        {
            var tutorial = new GameObject("Level Drawing Tutorial", typeof(RectTransform), typeof(AudioSource));
            var pages = Child(tutorial.transform, "Pages");
            for (var i = 1; i <= 6; i++) Child(pages.transform, "Page " + i);

            var dots = Child(tutorial.transform, "Page Dots");
            for (var i = 1; i <= 6; i++)
            {
                Child(dots.transform, "Dot " + i, typeof(Image));
            }

            Button(tutorial.transform, "Next", "下一步");
            Button(tutorial.transform, "Skip", "先跳过");
            Button(tutorial.transform, "Sound", "声音");
            return tutorial;
        }

        private static GameObject Child(Transform parent, string name, params System.Type[] components)
        {
            var child = new GameObject(name, components);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Button Button(Transform parent, string name, string label)
        {
            var buttonObject = Child(parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
            var labelObject = Child(buttonObject.transform, "Label", typeof(RectTransform), typeof(Text));
            labelObject.GetComponent<Text>().text = label;
            return buttonObject.GetComponent<Button>();
        }
    }
}
