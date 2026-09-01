using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PaperGame.C1
{
    public sealed class C1GameBootstrap : MonoBehaviour
    {
        private static readonly Color PlayerColor = new Color(0.13f, 0.48f, 0.95f);
        private static Sprite whiteSprite;
        private static Font hudFont;

        private Transform generatedRoot;
        private bool hasBuilt;
        private C1OutOfBoundsWatcher fallWatcher;

        public int GroundCount { get; private set; }
        public C1PlayerController2D Player { get; private set; }
        public C1GoalFlag Goal { get; private set; }
        public Canvas HudCanvas { get; private set; }
        public GameObject CompletionPanel { get; private set; }
        public GameObject FailurePanel { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrapExists()
        {
            if (FindObjectOfType<C1GameBootstrap>() == null)
            {
                new GameObject("C1 Game Bootstrap").AddComponent<C1GameBootstrap>();
            }
        }

        private void Start()
        {
            if (!hasBuilt)
            {
                Build(C1LevelLoader.LoadDefault());
            }
        }

        public bool Build(C1LevelDefinition level)
        {
            if (level == null)
            {
                Debug.LogWarning("C1 level is invalid: level is missing.", this);
                return false;
            }

            if (!level.TryValidate(out var error))
            {
                Debug.LogWarning($"C1 level is invalid: {error}", this);
                return false;
            }

            var backgroundTexture = Resources.Load<Texture2D>(level.BackgroundResourcePath);
            if (backgroundTexture == null || backgroundTexture.width != level.CanvasPixelSize.x || backgroundTexture.height != level.CanvasPixelSize.y)
            {
                Debug.LogWarning($"C1 level background is missing or does not match the canvas: {level.BackgroundResourcePath}", this);
                return false;
            }

            ClearGeneratedObjects();
            generatedRoot = new GameObject("C1 Generated Level").transform;
            generatedRoot.SetParent(transform, false);
            CreateBackground(backgroundTexture, level.CanvasPixelSize);

            foreach (var platform in level.Platforms)
            {
                CreatePlatform(platform, level.CanvasPixelSize);
            }

            Player = CreatePlayer(C1LevelSpace.PixelToWorld(level.PlayerStart, level.CanvasPixelSize));
            Goal = CreateGoal(level);
            Goal.Reached += HandleGoalReached;
            var framing = ConfigureCamera(level, out var aspect);
            CreateScreenBounds(framing, aspect);
            fallWatcher = generatedRoot.gameObject.AddComponent<C1OutOfBoundsWatcher>();
            fallWatcher.Configure(Player, framing.Center.y - framing.OrthographicSize - 0.5f);
            fallWatcher.Fell += HandlePlayerFell;
            CreateHud();
            HudCanvas.gameObject.AddComponent<C1PlaytestTuningPanel>().Configure(Player, level);

            hasBuilt = true;
            return true;
        }

        private void CreateBackground(Texture2D texture, Vector2Int canvasPixelSize)
        {
            var background = new GameObject("Paper Background");
            background.transform.SetParent(generatedRoot, false);
            background.transform.position = C1LevelSpace.CanvasWorldSize(canvasPixelSize) * 0.5f;
            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                C1LevelSpace.PixelsPerUnit);
            renderer.sprite.name = "Paper Background Sprite";
            renderer.sortingOrder = -100;
        }

        private void CreatePlatform(C1PlatformDefinition definition, Vector2Int canvasPixelSize)
        {
            var start = C1LevelSpace.PixelToWorld(definition.Start, canvasPixelSize);
            var end = C1LevelSpace.PixelToWorld(definition.End, canvasPixelSize);
            var direction = end - start;
            var platform = new GameObject("Ground");
            platform.transform.SetParent(generatedRoot, false);
            platform.transform.position = (start + end) * 0.5f;
            platform.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            var collider = platform.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(direction.magnitude, C1LevelSpace.GroundThickness);
            GroundCount++;
        }

        private C1PlayerController2D CreatePlayer(Vector2 position)
        {
            var playerObject = new GameObject("Player");
            playerObject.transform.position = position;
            playerObject.transform.SetParent(generatedRoot, true);
            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.3f);
            var body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var controller = playerObject.AddComponent<C1PlayerController2D>();

            var visual = new GameObject("Character Visual");
            visual.transform.SetParent(playerObject.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            var characterAnimator = visual.AddComponent<C1CharacterAnimator2D>();
            var idleFrames = LoadCharacterFrames("idle");
            var runFrames = LoadCharacterFrames("run");
            var jumpFrames = LoadCharacterFrames("jump");
            characterAnimator.Configure(idleFrames, runFrames, jumpFrames);

            if (idleFrames.Length == 0)
            {
                renderer.sprite = GetWhiteSprite();
                renderer.color = PlayerColor;
                visual.transform.localScale = new Vector3(0.8f, 1.3f, 1f);
            }
            else
            {
                renderer.color = Color.white;
                var spriteHeight = Mathf.Max(0.01f, idleFrames[0].bounds.size.y);
                var scale = 2.2f / spriteHeight;
                visual.transform.localScale = new Vector3(scale, scale, 1f);
            }

            return controller;
        }

        private C1GoalFlag CreateGoal(C1LevelDefinition level)
        {
            var goalObject = new GameObject("Goal Flag");
            goalObject.transform.SetParent(generatedRoot, false);
            goalObject.transform.position = C1LevelSpace.PixelToWorld(level.GoalRegion.center, level.CanvasPixelSize);

            var trigger = goalObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = level.GoalRegion.size / C1LevelSpace.PixelsPerUnit;
            return goalObject.AddComponent<C1GoalFlag>();
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite == null)
            {
                whiteSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                    new Vector2(0.5f, 0.5f),
                    1f);
                whiteSprite.name = "C1 White Sprite";
            }

            return whiteSprite;
        }

        private static Sprite[] LoadCharacterFrames(string stateName)
        {
            var frames = Resources.LoadAll<Sprite>($"C1Character/{stateName}");
            Array.Sort(frames, (left, right) => string.CompareOrdinal(left.name, right.name));
            return frames;
        }

        private static C1CameraFrame ConfigureCamera(C1LevelDefinition level, out float aspect)
        {
            var sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                sceneCamera = cameraObject.AddComponent<Camera>();
            }

            sceneCamera.orthographic = true;
            sceneCamera.backgroundColor = new Color(0.88f, 0.94f, 1f);
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;

            var oldFollow = sceneCamera.GetComponent<C1FollowCamera>();
            if (oldFollow != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(oldFollow);
                }
                else
                {
                    DestroyImmediate(oldFollow);
                }
            }

            var canvasWorldSize = C1LevelSpace.CanvasWorldSize(level.CanvasPixelSize);
            var bounds = new Bounds(canvasWorldSize * 0.5f, canvasWorldSize);
            aspect = sceneCamera.aspect;
            var framing = C1CameraFraming.Calculate(bounds, aspect, 0.7f);
            sceneCamera.transform.position = new Vector3(framing.Center.x, framing.Center.y, -10f);
            sceneCamera.orthographicSize = framing.OrthographicSize;
            return framing;
        }

        private void CreateScreenBounds(C1CameraFrame framing, float aspect)
        {
            var halfWidth = framing.OrthographicSize * aspect;
            var height = framing.OrthographicSize * 4f;
            CreateBoundWall("Left Bound", framing.Center.x - halfWidth - 0.5f, framing.Center.y, height);
            CreateBoundWall("Right Bound", framing.Center.x + halfWidth + 0.5f, framing.Center.y, height);
        }

        private void CreateBoundWall(string name, float x, float y, float height)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(generatedRoot, false);
            wall.transform.position = new Vector3(x, y, 0f);
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, height);
        }

        private void CreateHud()
        {
            var canvasObject = new GameObject("C1 HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(generatedRoot, false);
            HudCanvas = canvasObject.GetComponent<Canvas>();
            HudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var instruction = CreateText(canvasObject.transform, "Instruction", "Move: A/D or Arrow Keys | Jump: Space", 24, TextAnchor.UpperLeft);
            var instructionRect = instruction.rectTransform;
            instructionRect.anchorMin = new Vector2(0f, 1f);
            instructionRect.anchorMax = new Vector2(0f, 1f);
            instructionRect.pivot = new Vector2(0f, 1f);
            instructionRect.anchoredPosition = new Vector2(24f, -20f);
            instructionRect.sizeDelta = new Vector2(620f, 50f);
            instruction.color = new Color(0.08f, 0.1f, 0.14f);

            CompletionPanel = CreateResultPanel(canvasObject.transform, "Completion Panel", "Goal Reached!", new Color(0.1f, 0.38f, 0.16f));
            FailurePanel = CreateResultPanel(canvasObject.transform, "Failure Panel", "You Fell!", new Color(0.55f, 0.1f, 0.1f));

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("C1 EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(generatedRoot, false);
            }
        }

        private GameObject CreateResultPanel(Transform canvasTransform, string name, string title, Color titleColor)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasTransform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(320f, 170f);
            panel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.94f);

            var titleText = CreateText(panel.transform, "Title", title, 34, TextAnchor.MiddleCenter);
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.48f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(12f, 0f);
            titleRect.offsetMax = new Vector2(-12f, -8f);
            titleText.color = titleColor;

            var buttonObject = new GameObject("Restart Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 22f);
            buttonRect.sizeDelta = new Vector2(150f, 48f);
            buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.46f, 0.9f);

            var buttonLabel = CreateText(buttonObject.transform, "Label", "Restart", 24, TextAnchor.MiddleCenter);
            buttonLabel.color = Color.white;
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = Vector2.zero;
            buttonLabel.rectTransform.offsetMax = Vector2.zero;
            buttonObject.GetComponent<Button>().onClick.AddListener(() => Build(C1LevelLoader.LoadDefault()));

            panel.SetActive(false);
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment)
        {
            if (hudFont == null)
            {
                hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = hudFont;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private void HandleGoalReached(C1PlayerController2D player)
        {
            if (CompletionPanel != null)
            {
                CompletionPanel.SetActive(true);
            }
        }

        private void HandlePlayerFell(C1PlayerController2D player)
        {
            if (FailurePanel != null)
            {
                FailurePanel.SetActive(true);
            }
        }

        private void ClearGeneratedObjects()
        {
            if (Goal != null)
            {
                Goal.Reached -= HandleGoalReached;
            }

            if (fallWatcher != null)
            {
                fallWatcher.Fell -= HandlePlayerFell;
                fallWatcher = null;
            }

            if (generatedRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(generatedRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(generatedRoot.gameObject);
                }
            }

            generatedRoot = null;
            Player = null;
            Goal = null;
            HudCanvas = null;
            CompletionPanel = null;
            FailurePanel = null;
            GroundCount = 0;
            hasBuilt = false;
        }
    }
}
