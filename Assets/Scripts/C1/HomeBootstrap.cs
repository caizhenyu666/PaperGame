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
            scaler.referenceResolution = new Vector2(1280f, 720f);
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
            CreateHomeButton("创建主角", new Vector2(.04f, .06f), new Vector2(.25f, .16f), ShowCharacterCreation);
            FindButton("Start Play").onClick.AddListener(StartGame);

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
            C1GameSession.Instance.SetPendingLevel(C1GameSession.DefaultLevelResourcePath);
            SceneManager.LoadScene(C1GameSession.GameSceneName);
        }

        private void CreateHomeButton(string title, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(HomeScreen.transform, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            buttonObject.GetComponent<Image>().color = new Color(.82f, .9f, .77f);
            buttonObject.GetComponent<Button>().onClick.AddListener(action);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<Text>();
            label.font = C1UiFont.Load();
            label.text = title;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(.22f, .26f, .24f);
            label.raycastTarget = false;
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
