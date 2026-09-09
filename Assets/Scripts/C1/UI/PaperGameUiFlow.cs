using UnityEngine;

namespace PaperGame.C1
{
    public enum PaperGameUiState
    {
        Home,
        Tutorial,
        Playing,
        Completed,
        Retry
    }

    public sealed class PaperGameUiFlow : MonoBehaviour
    {
        public PaperGameUiState CurrentState { get; private set; } = PaperGameUiState.Home;
        public bool HasSeenTutorial { get; private set; }

        public void ConfigureForTests(bool tutorialSeen)
        {
            HasSeenTutorial = tutorialSeen;
            CurrentState = PaperGameUiState.Home;
        }

        public void StartPlay()
        {
            CurrentState = HasSeenTutorial ? PaperGameUiState.Playing : PaperGameUiState.Tutorial;
        }

        public void CompleteTutorial()
        {
            HasSeenTutorial = true;
            CurrentState = PaperGameUiState.Playing;
        }

        public void ShowCompleted()
        {
            CurrentState = PaperGameUiState.Completed;
        }

        public void ShowRetry()
        {
            CurrentState = PaperGameUiState.Retry;
        }
    }
}
