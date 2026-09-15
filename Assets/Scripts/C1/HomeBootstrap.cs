using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaperGame.C1
{
    public sealed class HomeBootstrap : MonoBehaviour
    {
        private Canvas canvas;
        private C1CharacterSelection characterSelection;

        public GameObject HomeScreen { get; private set; }
        public GameObject CharacterScreen { get; private set; }
        public GameObject LevelScreen { get; private set; }

        private void Start()
        {
            BuildHome();
        }

        public void BuildHome()
        {
            if (HomeScreen != null) return;

            var canvasObject = new GameObject("Home Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var prefab = Resources.Load<GameObject>("C1UI/PaperGameHome");
            if (prefab == null)
            {
                Debug.LogWarning("Picture book home prefab is missing.", this);
                return;
            }

            HomeScreen = Instantiate(prefab, canvas.transform);
            HomeScreen.name = "PaperGameHome";
            var homeRect = HomeScreen.GetComponent<RectTransform>();
            homeRect.anchorMin = Vector2.zero;
            homeRect.anchorMax = Vector2.one;
            homeRect.offsetMin = Vector2.zero;
            homeRect.offsetMax = Vector2.zero;

            characterSelection = GetComponent<C1CharacterSelection>() ?? gameObject.AddComponent<C1CharacterSelection>();
            FindButton("Start Play").onClick.AddListener(StartGame);
            FindButton("Create Role").onClick.AddListener(ShowCharacterCreation);
            FindButton("Capture Level").onClick.AddListener(() => ShowLevels(true));

            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("Home EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            }
        }

        public void ShowCharacterCreation()
        {
            if (CharacterScreen != null) return;
            HomeScreen.SetActive(false);
            CharacterScreen = characterSelection.CreateScreen(canvas, ReturnHome);
        }

        public void ReturnHome()
        {
            if (LevelScreen != null)
            {
                LevelScreen.SetActive(false);
                if (Application.isPlaying) Destroy(LevelScreen);
                else DestroyImmediate(LevelScreen);
            }
            LevelScreen = null;
            if (CharacterScreen != null)
            {
                if (Application.isPlaying) Destroy(CharacterScreen);
                else DestroyImmediate(CharacterScreen);
            }

            CharacterScreen = null;
            HomeScreen.SetActive(true);
        }

        private void StartGame()
        {
            ShowLevels(false);
        }

        public void ShowLevels(bool createImmediately = false)
        {
            if (LevelScreen != null) return;
            HomeScreen.SetActive(false);
            LevelScreen = C1LevelSelection.Panel(canvas.transform, "Level Selection " + GetInstanceID(), 0, 0, 1, 1);
            LevelScreen.AddComponent<C1LevelSelection>().Configure(ReturnHome, createImmediately);
        }

        private Button FindButton(string name)
        {
            foreach (var candidate in HomeScreen.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.gameObject.name == name)
                {
                    return candidate.GetComponent<Button>() ?? candidate.gameObject.AddComponent<Button>();
                }
            }

            throw new MissingReferenceException("Home button is missing: " + name);
        }
    }
}
