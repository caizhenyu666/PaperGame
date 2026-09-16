using UnityEngine;

namespace PaperGame.C1
{
    public sealed class C1GameSession : MonoBehaviour
    {
        public const string HomeSceneName = "Home";
        public const string GameSceneName = "Game";
        public const string DefaultLevelResourcePath = "C1Levels/level1";
        public const string MockLevelApiResourcePath = "C1Levels/level-api-mock";

        private static C1GameSession instance;
        private string pendingLevelResourcePath;
        private string pendingLocalLevelId;
        public int CurrentPageNumber { get; private set; } = 1;

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
            pendingLocalLevelId = null;
            pendingLevelResourcePath = string.IsNullOrWhiteSpace(resourcePath)
                ? DefaultLevelResourcePath
                : resourcePath;
            var match = System.Text.RegularExpressions.Regex.Match(pendingLevelResourcePath, @"(\d+)$");
            CurrentPageNumber = match.Success ? Mathf.Max(1, int.Parse(match.Groups[1].Value)) : 1;
        }

        public void SetPendingLocalLevel(string id, int pageNumber = 1)
        {
            if (!System.Guid.TryParseExact(id, "N", out _)) throw new System.ArgumentException("本地关卡编号无效");
            pendingLocalLevelId = id;
            pendingLevelResourcePath = null;
            CurrentPageNumber = Mathf.Max(1, pageNumber);
        }

        public string ConsumePendingLocalLevel()
        {
            var id = pendingLocalLevelId;
            pendingLocalLevelId = null;
            return id;
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
