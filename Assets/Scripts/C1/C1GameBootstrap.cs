using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
        private C1CharacterFrames selectedCharacterFrames;
        private Texture2D localBackground;
        private Sprite backgroundSprite;
        private Sprite paperBackdropSprite;

        public int GroundCount { get; private set; }
        public C1PlayerController2D Player { get; private set; }
        public C1GoalFlag Goal { get; private set; }
        public Canvas HudCanvas { get; private set; }
        public GameObject CompletionPanel { get; private set; }
        public GameObject FailurePanel { get; private set; }
        public string RequestedSceneName { get; private set; }

        private void Start()
        {
            if (!hasBuilt)
            {
                BuildSelectedLevel();
            }
        }

        public bool BuildSelectedLevel()
        {
            var localId = C1GameSession.Instance.ConsumePendingLocalLevel();
            if (!string.IsNullOrEmpty(localId))
            {
                Texture2D texture = null;
                try
                {
                    var level = new C1LevelLibrary().Read(localId, out texture);
                    if (Build(level, texture))
                    {
                        localBackground = texture;
                        return true;
                    }
                }
                catch (Exception e) { Debug.LogWarning("无法打开本地关卡：" + e.Message, this); }
                C1LevelLibrary.Release(texture);
                ReturnHome();
                return false;
            }
            return Build(C1LevelLoader.Load(C1GameSession.Instance.ConsumePendingLevel()));
        }

        public static bool TryGetSelectedCharacter(C1CharacterLibrary library, out C1CharacterRecord record)
        {
            record = null;
            if (library == null || library.characters == null || library.selectedId == "default") return false;
            record = library.characters.Find(item => item != null && item.characterId == library.selectedId);
            return record != null;
        }

        public static bool ApplyRemoteCharacter(C1PlayerController2D player, C1CharacterFrames frames)
        {
            if (player == null || frames == null || frames.run == null || frames.jump == null ||
                frames.run.Length == 0 || frames.jump.Length == 0) return false;

            var visual = player.transform.Find("Character Visual");
            var animator = visual == null ? null : visual.GetComponent<C1CharacterAnimator2D>();
            var renderer = visual == null ? null : visual.GetComponent<SpriteRenderer>();
            var collider = player.GetComponent<BoxCollider2D>();
            if (animator == null || renderer == null || collider == null) return false;

            animator.ConfigureRemote(frames.run, frames.jump, frames.runFps, frames.jumpFps);
            C1CharacterSizing.Apply(visual, collider, frames.run[0].bounds.size.y);
            renderer.color = Color.white;
            return true;
        }

        public void ReturnHome()
        {
            ClearGeneratedObjects();
            RequestedSceneName = C1GameSession.HomeSceneName;
            if (Application.isPlaying) SceneManager.LoadScene(RequestedSceneName);
        }

        public bool Build(C1LevelDefinition level)
        {
            return Build(level, null);
        }

        public bool Build(C1LevelDefinition level, Texture2D backgroundOverride)
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

            var backgroundTexture = backgroundOverride != null ? backgroundOverride : Resources.Load<Texture2D>(level.BackgroundResourcePath);
            if (backgroundTexture == null || backgroundTexture.width != level.CanvasPixelSize.x || backgroundTexture.height != level.CanvasPixelSize.y)
            {
                Debug.LogWarning($"C1 level background is missing or does not match the canvas: {level.BackgroundResourcePath}", this);
                return false;
            }

            ClearGeneratedObjects();
            generatedRoot = new GameObject("C1 Generated Level").transform;
            generatedRoot.SetParent(transform, false);
            CreateBackground(backgroundTexture, level.CanvasPixelSize);

            foreach (var platform in level.Platforms ?? Array.Empty<C1PlatformDefinition>())
            {
                CreatePlatform(platform, level.CanvasPixelSize);
            }

            foreach (var wall in level.Walls ?? Array.Empty<C1WallDefinition>())
            {
                CreateWall(wall, level.CanvasPixelSize);
            }

            foreach (var block in level.Blocks ?? Array.Empty<C1BlockDefinition>())
            {
                CreateBlock(block, level.CanvasPixelSize);
            }

            var playerStart = C1LevelSpace.PixelToWorld(level.PlayerStart, level.CanvasPixelSize);
            if (level.PlayerStartIsFeet) playerStart.y += C1CharacterSizing.Height * .5f + C1LevelSpace.GroundThickness * .5f;
            Player = CreatePlayer(playerStart);
            LoadSelectedCharacter(Player);
            Goal = CreateGoal(level);
            Goal.Reached += HandleGoalReached;
            var framing = ConfigureCamera(level, out var aspect);
            CreateScreenBounds(framing, aspect);
            fallWatcher = generatedRoot.gameObject.AddComponent<C1OutOfBoundsWatcher>();
            fallWatcher.Configure(Player, framing.Center.y - framing.OrthographicSize - 0.5f);
            fallWatcher.Fell += HandlePlayerFell;
            CreateHud();
            hasBuilt = true;
            return true;
        }

        private void CreateBackground(Texture2D texture, Vector2Int canvasPixelSize)
        {
            var paperTexture = Resources.Load<Texture2D>("C1GameUI/paper-background");
            if (paperTexture != null)
            {
                var backdrop = new GameObject("Paper Backdrop");
                backdrop.transform.SetParent(generatedRoot, false);
                backdrop.transform.position = C1LevelSpace.CanvasWorldSize(canvasPixelSize) * .5f;
                var backdropRenderer = backdrop.AddComponent<SpriteRenderer>();
                paperBackdropSprite = Sprite.Create(paperTexture, new Rect(0, 0, paperTexture.width, paperTexture.height),
                    new Vector2(.5f, .5f), C1LevelSpace.PixelsPerUnit);
                backdropRenderer.sprite = paperBackdropSprite;
                backdropRenderer.sortingOrder = -110;
                var canvasWorldSize = C1LevelSpace.CanvasWorldSize(canvasPixelSize);
                var cover = Mathf.Max(canvasWorldSize.x / paperBackdropSprite.bounds.size.x,
                    canvasWorldSize.y / paperBackdropSprite.bounds.size.y) * 1.35f;
                backdrop.transform.localScale = Vector3.one * cover;
            }
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
            backgroundSprite = renderer.sprite;
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

        private void CreateWall(C1WallDefinition definition, Vector2Int canvasPixelSize)
        {
            var start = C1LevelSpace.PixelToWorld(definition.Start, canvasPixelSize);
            var end = C1LevelSpace.PixelToWorld(definition.End, canvasPixelSize);
            var direction = end - start;
            var wall = new GameObject("Wall");
            wall.transform.SetParent(generatedRoot, false);
            wall.transform.position = (start + end) * 0.5f;
            wall.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(direction.magnitude, C1LevelSpace.GroundThickness);
        }

        private void CreateBlock(C1BlockDefinition definition, Vector2Int canvasPixelSize)
        {
            var block = new GameObject("Block");
            block.transform.SetParent(generatedRoot, false);
            block.transform.position = C1LevelSpace.PixelToWorld(definition.Region.center, canvasPixelSize);
            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = definition.Region.size / C1LevelSpace.PixelsPerUnit;
        }

        private C1PlayerController2D CreatePlayer(Vector2 position)
        {
            var playerObject = new GameObject("Player");
            playerObject.transform.position = position;
            playerObject.transform.SetParent(generatedRoot, true);
            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(C1CharacterSizing.Width, C1CharacterSizing.Height);
            var body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var controller = playerObject.AddComponent<C1PlayerController2D>();

            var visual = new GameObject("Character Visual");
            visual.transform.SetParent(playerObject.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            var characterAnimator = visual.AddComponent<C1CharacterAnimator2D>();
            // 自定义角色下载失败时也保留正式默认角色，而不是旧的临时像素人物。
            C1BuiltInCharacters.Apply(controller, C1BuiltInCharacters.Green);
            if (renderer.sprite == null)
            {
                renderer.sprite = GetWhiteSprite();
                renderer.color = PlayerColor;
                visual.transform.localScale = new Vector3(C1CharacterSizing.Width, C1CharacterSizing.Height, 1f);
            }
            return controller;
        }

        private void LoadSelectedCharacter(C1PlayerController2D targetPlayer)
        {
            var library = C1CharacterLibrary.Load();
            if (C1BuiltInCharacters.IsBuiltIn(library.selectedId))
            {
                C1BuiltInCharacters.Apply(targetPlayer, library.selectedId);
                return;
            }
            if (!Application.isPlaying || !TryGetSelectedCharacter(library, out var record)) return;
            var service = GetComponent<C1CharacterService>() ?? gameObject.AddComponent<C1CharacterService>();
            StartCoroutine(DownloadAndApplySelectedCharacter(service, record, targetPlayer));
        }

        private IEnumerator DownloadAndApplySelectedCharacter(
            C1CharacterService service,
            C1CharacterRecord record,
            C1PlayerController2D targetPlayer)
        {
            C1CharacterFrames downloadedFrames = null;
            string error = null;
            yield return service.Download(record, frames => downloadedFrames = frames, message => error = message);

            if (downloadedFrames == null)
            {
                Debug.LogWarning("Unable to load selected character: " + error, this);
                yield break;
            }

            if (Player != targetPlayer || !ApplyRemoteCharacter(targetPlayer, downloadedFrames))
            {
                downloadedFrames.Dispose();
                yield break;
            }

            selectedCharacterFrames?.Dispose();
            selectedCharacterFrames = downloadedFrames;
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
            sceneCamera.backgroundColor = new Color(0.96f, 0.93f, 0.82f);
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
            var prefab = Resources.Load<GameObject>("C1UI/PaperGameGameHUD");
            if (prefab == null) throw new InvalidOperationException("正式关卡 HUD 预制体缺失");
            var canvasObject = Instantiate(prefab, generatedRoot, false);
            canvasObject.name = "C1 HUD";
            HudCanvas = canvasObject.GetComponent<Canvas>();
            canvasObject.GetComponent<C1GameHudController>().Configure(Player, C1GameSession.Instance.CurrentPageNumber, ReturnHome);

            CompletionPanel = CreateResultPanel(canvasObject.transform, "Completion Panel", "Goal Reached!", new Color(0.1f, 0.38f, 0.16f));
            FailurePanel = CreateResultPanel(canvasObject.transform, "Failure Panel", "You Fell!", new Color(0.55f, 0.1f, 0.1f));

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("C1 EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(generatedRoot, false);
            }
        }

        private void CreateMobileHud(GameObject canvasObject)
        {
            var mobileRoot = new GameObject("Mobile Controls", typeof(RectTransform));
            mobileRoot.transform.SetParent(canvasObject.transform, false);
            var mobileRect = mobileRoot.GetComponent<RectTransform>();
            mobileRect.anchorMin = Vector2.zero;
            mobileRect.anchorMax = Vector2.one;
            mobileRect.offsetMin = Vector2.zero;
            mobileRect.offsetMax = Vector2.zero;

            var controls = mobileRoot.AddComponent<C1MobileControls>();
            controls.Configure(Player);

            var leftButton = CreateHudButton(
                mobileRoot.transform,
                "Move Left",
                "◀",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(82f, 82f),
                new Vector2(104f, 104f),
                new Color(0.04f, 0.08f, 0.14f, 0.42f));
            leftButton.gameObject.AddComponent<C1TouchDirectionButton>().Configure(controls, true);

            var rightButton = CreateHudButton(
                mobileRoot.transform,
                "Move Right",
                "▶",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(202f, 82f),
                new Vector2(104f, 104f),
                new Color(0.04f, 0.08f, 0.14f, 0.42f));
            rightButton.gameObject.AddComponent<C1TouchDirectionButton>().Configure(controls, false);

            var jumpButton = CreateHudButton(
                mobileRoot.transform,
                "Jump",
                "跳",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-82f, 82f),
                new Vector2(112f, 112f),
                new Color(0.1f, 0.36f, 0.78f, 0.48f));
            jumpButton.gameObject.AddComponent<C1TouchJumpButton>().Configure(controls);

            var captureButton = CreateHudButton(
                canvasObject.transform,
                "Capture Photo",
                "拍照",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -42f),
                new Vector2(150f, 52f),
                new Color(0.08f, 0.42f, 0.75f, 0.82f));

            var status = CreateText(canvasObject.transform, "Photo Status", "", 18, TextAnchor.UpperCenter);
            status.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            status.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            status.rectTransform.pivot = new Vector2(0.5f, 1f);
            status.rectTransform.anchoredPosition = new Vector2(0f, -76f);
            status.rectTransform.sizeDelta = new Vector2(440f, 42f);
            status.color = new Color(0.08f, 0.1f, 0.14f);

            var previewObject = new GameObject("Photo Preview", typeof(RectTransform), typeof(RawImage));
            previewObject.transform.SetParent(canvasObject.transform, false);
            var previewRect = previewObject.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.5f, 1f);
            previewRect.anchorMax = new Vector2(0.5f, 1f);
            previewRect.pivot = new Vector2(0.5f, 1f);
            previewRect.anchoredPosition = new Vector2(0f, -118f);
            previewRect.sizeDelta = new Vector2(260f, 170f);
            var preview = previewObject.GetComponent<RawImage>();
            preview.color = Color.white;
            preview.raycastTarget = false;
            previewObject.SetActive(false);

            var photoCapture = canvasObject.AddComponent<C1PhotoCapture>();
            photoCapture.Configure(preview, status);
            captureButton.onClick.AddListener(photoCapture.OpenCamera);

        }

        private static Button CreateHudButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            buttonObject.GetComponent<Image>().color = color;

            var buttonLabel = CreateText(buttonObject.transform, "Label", label, 34, TextAnchor.MiddleCenter);
            buttonLabel.color = Color.white;
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = Vector2.zero;
            buttonLabel.rectTransform.offsetMax = Vector2.zero;
            return buttonObject.GetComponent<Button>();
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
            buttonObject.GetComponent<Button>().onClick.AddListener(ReturnHome);

            panel.SetActive(false);
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment)
        {
            if (hudFont == null)
            {
                hudFont = C1UiFont.Load();
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
            C1LevelLibrary.Release(backgroundSprite);
            backgroundSprite = null;
            C1LevelLibrary.Release(paperBackdropSprite);
            paperBackdropSprite = null;
            C1LevelLibrary.Release(localBackground);
            localBackground = null;
            selectedCharacterFrames?.Dispose();
            selectedCharacterFrames = null;

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

        private void OnDestroy()
        {
            ClearGeneratedObjects();
        }
    }
}
