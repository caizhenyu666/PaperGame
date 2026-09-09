using UnityEngine;

namespace PaperGame.C1
{
    public sealed class C1GameSession : MonoBehaviour
    {
        public const string HomeSceneName = "Home";
        public const string GameSceneName = "Game";
        public const string DefaultLevelResourcePath = "C1Levels/level1";

        private static C1GameSession instance;
        private string pendingLevelResourcePath;

        public static C1GameSession Instance
        {
            get
            {
                if (instance == null)
                {
                    var sessionObject = new GameObject("C1 Game Session");
                    instance = sessionObject.AddComponent<C1GameSession>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void SetPendingLevel(string resourcePath)
        {
            pendingLevelResourcePath = string.IsNullOrWhiteSpace(resourcePath)
                ? DefaultLevelResourcePath
                : resourcePath;
        }

        public string ConsumePendingLevel()
        {
            var resourcePath = string.IsNullOrWhiteSpace(pendingLevelResourcePath)
                ? DefaultLevelResourcePath
                : pendingLevelResourcePath;
            pendingLevelResourcePath = null;
            return resourcePath;
        }
    }
}
