using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class PaperGameUiFlowTests
    {
        private GameObject flowObject;
        private PaperGameUiFlow flow;

        [SetUp]
        public void SetUp()
        {
            flowObject = new GameObject("UI Flow Test");
            flow = flowObject.AddComponent<PaperGameUiFlow>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(flowObject);
        }

        [Test]
        public void StartPlay_WhenTutorialIsUnseen_ChangesToTutorial()
        {
            flow.ConfigureForTests(false);

            flow.StartPlay();

            Assert.That(flow.CurrentState, Is.EqualTo(PaperGameUiState.Tutorial));
        }

        [Test]
        public void CompleteTutorial_RecordsPreferenceAndChangesToPlaying()
        {
            flow.ConfigureForTests(false);

            flow.CompleteTutorial();

            Assert.That(flow.CurrentState, Is.EqualTo(PaperGameUiState.Playing));
            Assert.That(flow.HasSeenTutorial, Is.True);
        }

        [Test]
        public void ShowCompleted_ChangesToCompleted()
        {
            flow.ConfigureForTests(true);

            flow.ShowCompleted();

            Assert.That(flow.CurrentState, Is.EqualTo(PaperGameUiState.Completed));
        }
    }
}
