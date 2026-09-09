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
        public string NextSceneName { get; private set; }
        public string PendingLevelResourcePath { get; private set; }

        public void ConfigureForTests(bool tutorialSeen)
        {
            HasSeenTutorial = tutorialSeen;
            CurrentState = PaperGameUiState.Home;
        }

        public void StartPlay()
        {
            CurrentState = HasSeenTutorial ? PaperGameUiState.Playing : PaperGameUiState.Tutorial;
        }

        public void StartPlay(string levelResourcePath)
        {
            StartPlay();
            NextSceneName = C1GameSession.GameSceneName;
            PendingLevelResourcePath = string.IsNullOrWhiteSpace(levelResourcePath)
                ? C1GameSession.DefaultLevelResourcePath
                : levelResourcePath;
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
