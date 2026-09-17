using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaperGame.C1
{
    public sealed class C1LevelSelection : MonoBehaviour
    {
        private C1LevelLibrary library;
        private C1LevelService service;
        private C1PhotoCapture capture;
        private C1LocalStorage storage;
        private C1LevelDrawingTutorialController tutorial;
        private Action openCameraOverride;
        private Transform list;
        private RawImage preview;
        private Text status;
        private Button create, play, resume, regenerate;
        private C1SavedLevel selected;
        private Texture2D selectedTexture;
        private bool selectedTextureIsAsset;
        private bool busy;
        private int selectedPage = 1;
        private bool builtInSelected = true;
        public int LevelCount { get; private set; }
        public bool IsBuiltInSelected => builtInSelected && selected == null;
        public int SelectedPageNumber => builtInSelected && selected == null ? 1 : selectedPage;
        public string StatusText => status == null ? string.Empty : status.text;
        public bool TutorialVisible => tutorial != null && tutorial.IsOpen;

        public void Configure(Action returnHome, bool createImmediately = false, C1LevelLibrary localLibrary = null,
            Action cameraOpener = null)
        {
            library = localLibrary ?? new C1LevelLibrary();
            openCameraOverride = cameraOpener;
            service = gameObject.AddComponent<C1LevelService>();
            storage = gameObject.AddComponent<C1LocalStorage>();
            Label(transform, "我的纸上关卡", 38, .05f, .86f, .64f, .96f);
            ButtonAt(transform, "怎么画？", .65f, .87f, .76f, .96f, () => ShowDrawingTutorial(false));
            ButtonAt(transform, "返回首页", .78f, .87f, .95f, .96f, () => returnHome());
            var scrollRoot = Panel(transform, "Saved Levels", .05f, .27f, .39f, .83f);
            scrollRoot.AddComponent<RectMask2D>();
            var scroll = scrollRoot.AddComponent<ScrollRect>();
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(scrollRoot.transform, false);
            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10); layout.spacing = 10;
            layout.childControlHeight = true; layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = rect; scroll.viewport = scrollRoot.GetComponent<RectTransform>(); scroll.horizontal = false;
            list = content.transform;
            var picture = new GameObject("Level Preview", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            picture.transform.SetParent(Panel(transform, "Preview Frame", .42f, .27f, .95f, .83f).transform, false);
            preview = picture.GetComponent<RawImage>(); preview.raycastTarget = false;
            picture.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            preview.color = Color.clear;
            status = Label(transform, "", 20, .05f, .13f, .95f, .26f);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            status.resizeTextForBestFit = true; status.resizeTextMinSize = 13; status.resizeTextMaxSize = 20;
            create = ButtonAt(transform, "拍照上传新关卡", .05f, .035f, .28f, .115f, RequestCreateLevel);
            resume = ButtonAt(transform, "继续生成", .30f, .035f, .48f, .115f, () => StartWork(false));
            regenerate = ButtonAt(transform, "重新上传", .50f, .035f, .68f, .115f, () => StartWork(true));
            play = ButtonAt(transform, "开始关卡", .71f, .035f, .95f, .115f, Play);
            capture = gameObject.AddComponent<C1PhotoCapture>();
            capture.Configure(null, status); capture.PhotoCaptured += OnPhoto;
            if (!Try(RefreshList)) return;
            BrowseBuiltIn();
            SetBusy(false);
            if (LevelCount <= 1)
                status.text = "请画好平台、一个起点圆圈和一个三角旗帜后拍照上传；标记可用黑笔空心绘制。";
            else status.text = "请选择本地关卡，或拍照上传创建新关卡。";
            if (library.Pending != null) status.text = "上次的照片和生成任务已保留，可点击“继续生成”。";
            if (createImmediately) RequestCreateLevel();
        }

        private void RequestCreateLevel()
        {
            if (C1LevelDrawingTutorialController.HasSeen)
            {
                OpenCamera();
                return;
            }

            ShowDrawingTutorial(true);
        }

        private void ShowDrawingTutorial(bool openCameraAfter)
        {
            if (tutorial == null)
            {
                var prefab = Resources.Load<GameObject>("C1UI/PaperGameLevelDrawingTutorial");
                if (prefab == null)
                {
                    if (openCameraAfter) OpenCamera();
                    else if (status != null) status.text = "绘画引导暂时不可用，请稍后再试。";
                    return;
                }

                var tutorialObject = Instantiate(prefab, transform, false);
                tutorialObject.name = "绘画新手引导";
                tutorialObject.transform.SetAsLastSibling();
                tutorial = tutorialObject.GetComponent<C1LevelDrawingTutorialController>();
            }

            tutorial.Open(openCameraAfter ? OpenCamera : (Action)null);
        }

        public void OpenCamera()
        {
            if (busy) return;
            if (openCameraOverride != null) openCameraOverride();
            else capture.OpenCamera();
        }

        private void OnPhoto(byte[] bytes, string mime)
        {
            if (busy) return;
            if (!Try(() => library.BeginUpload(bytes, mime, service.BaseUrl))) return;
            selected = null;
            ReleaseSelectedTexture();
            ShowPreview(capture.PreviewTexture);
            StartWork(false);
        }

        private void StartWork(bool force)
        {
            if (busy) return;
            StartCoroutine(RunSafely(Generate(force)));
        }

        private IEnumerator Generate(bool force)
        {
            var pending = library.Pending;
            if (pending == null) { status.text = "请先拍照上传关卡图。"; yield break; }
            SetBusy(true);
            service.BaseUrl = pending.serviceUrl;
            status.text = "正在保存照片…";
            yield return storage.Flush();
            if (storage.Error != null) { status.text = storage.Error; yield break; }
            string error = null;
            if (force || string.IsNullOrEmpty(pending.jobId))
            {
                status.text = "正在上传关卡照片…";
                yield return service.Upload(library.ReadPendingPhoto(), pending.mime, force,
                    id => pending.jobId = id, message => error = message);
                if (error != null) { status.text = error; yield break; }
                library.SetPending(pending);
                yield return storage.Flush();
                if (storage.Error != null) { status.text = storage.Error; yield break; }
            }
            C1LevelResponse response = null;
            yield return service.Poll(pending.jobId, message => status.text = message,
                result => response = result, message => error = message);
            if (error != null) { status.text = error; yield break; }
            if (response == null) { status.text = "没有收到关卡结果，请重试。"; yield break; }
            if (response.status == "needs_review" || response.status == "failed")
            {
                status.text = response.UserMessage + (response.status == "needs_review" || response.error?.retryable == false
                    ? "\n请调整纸面后重新拍照上传。" : "\n可点击“重新上传”重试。");
                yield break;
            }
            status.text = "正在下载并保存关卡…";
            byte[] background = null;
            yield return service.Download(response, bytes => background = bytes, message => error = message);
            if (error != null) { status.text = error; yield break; }
            var saved = library.Save(response.json, background, library.ReadPendingPhoto());
            yield return storage.Flush();
            if (storage.Error != null) { status.text = storage.Error; yield break; }
            library.ClearPending();
            yield return storage.Flush();
            RefreshList();
            Browse(saved);
            if (storage.Error != null) status.text = storage.Error;
        }

        private IEnumerator RunSafely(IEnumerator operation)
        {
            // Flatten nested coroutines so IO/decoding failures cannot leave the UI permanently busy.
            var stack = new Stack<IEnumerator>();
            stack.Push(operation);
            try
            {
                while (stack.Count > 0)
                {
                    object next = null;
                    var moved = false;
                    string error = null;
                    try { moved = stack.Peek().MoveNext(); if (moved) next = stack.Peek().Current; }
                    catch (Exception e) { error = "关卡处理失败：" + e.Message; }
                    if (error != null) { status.text = error; yield break; }
                    if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (next is IEnumerator nested) stack.Push(nested);
                    else yield return next;
                }
            }
            finally
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
                if (this != null) SetBusy(false);
            }
        }

        private void RefreshList()
        {
            foreach (Transform child in list)
            {
                child.gameObject.SetActive(false);
                C1LevelLibrary.Release(child.gameObject);
            }
            var levels = library.Load();
            LevelCount = 1 + levels.Count;
            var builtIn = ButtonAt(list, "第1页", 0, 0, 1, 1, BrowseBuiltIn);
            builtIn.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
            builtIn.interactable = !busy;
            foreach (var level in levels)
            {
                var item = level;
                var page = levels.IndexOf(level) + 2;
                var button = ButtonAt(list, level.title, 0, 0, 1, 1, () => Browse(item, page));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
                button.interactable = !busy;
            }
        }

        private void BrowseBuiltIn()
        {
            if (!Try(() =>
            {
                var level = C1LevelLoader.LoadDefault();
                if (level == null) throw new InvalidOperationException("内置关卡数据缺失或损坏");
                ReleaseSelectedTexture();
                // The bundled asset must not be destroyed like runtime textures; just drop the reference.
                selectedTexture = Resources.Load<Texture2D>("C1Levels/level1-background");
                selectedTextureIsAsset = true;
                selected = null; selectedPage = 1; builtInSelected = true;
                ShowPreview(selectedTexture);
                status.text = "第1页 · 内置关卡，可直接开始。";
                play.GetComponentInChildren<Text>().text = "开始关卡";
            })) { builtInSelected = false; }
            play.interactable = !busy && (builtInSelected && selected == null || selected != null);
        }

        private void Browse(C1SavedLevel record, int pageNumber = 1)
        {
            if (!Try(() =>
            {
                library.Read(record.id, out var texture);
                ReleaseSelectedTexture();
                selectedTexture = texture; selectedTextureIsAsset = false;
                selected = record; selectedPage = Mathf.Max(1, pageNumber); builtInSelected = false;
                ShowPreview(texture);
                status.text = record.title + " · 已保存在本地，可直接开始。";
                play.GetComponentInChildren<Text>().text = "开始关卡";
            })) selected = null;
            play.interactable = !busy && selected != null;
        }

        private void Play()
        {
            if (busy) return;
            if (builtInSelected && selected == null)
            {
                C1GameSession.Instance.SetPendingLevel(C1GameSession.DefaultLevelResourcePath);
                SceneManager.LoadScene(C1GameSession.GameSceneName);
                return;
            }
            if (selected == null) return;
            // Decode before leaving the selection screen so corrupt files show a recoverable error here.
            if (!Try(() =>
            {
                library.Read(selected.id, out var texture);
                C1LevelLibrary.Release(texture);
                C1GameSession.Instance.SetPendingLocalLevel(selected.id, selectedPage);
                SceneManager.LoadScene(C1GameSession.GameSceneName);
            })) return;
        }

        private bool Try(Action operation)
        {
            try { operation(); return true; }
            catch (Exception e) { if (status != null) status.text = "无法读取或保存本地关卡：" + e.Message; return false; }
        }

        private void SetBusy(bool value)
        {
            busy = value;
            if (create == null) return;
            create.interactable = !value;
            play.interactable = !value && selected != null;
            var hasPending = false;
            Try(() => hasPending = library.Pending != null);
            resume.interactable = regenerate.interactable = !value && hasPending;
            foreach (var button in list.GetComponentsInChildren<Button>()) button.interactable = !value;
        }

        private void ShowPreview(Texture2D texture)
        {
            preview.texture = texture; preview.color = texture == null ? Color.clear : Color.white;
            if (texture != null) preview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            if (capture != null) capture.PhotoCaptured -= OnPhoto;
            ReleaseSelectedTexture();
        }

        private void ReleaseSelectedTexture()
        {
            if (selectedTextureIsAsset) { selectedTexture = null; return; }
            C1LevelLibrary.Release(selectedTexture);
            selectedTexture = null;
        }

        internal static GameObject Panel(Transform parent, string title, float x, float y, float right, float top)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(.98f, .96f, .89f);
            return go;
        }

        private static Text Label(Transform parent, string title, int size, float x, float y, float right, float top)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>(); text.text = title; text.font = C1UiFont.Load(); text.fontSize = size;
            text.color = new Color(.2f, .24f, .2f); text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            return text;
        }

        private static Button ButtonAt(Transform parent, string title, float x, float y, float right, float top, UnityEngine.Events.UnityAction action)
        {
            var go = Panel(parent, title, x, y, right, top);
            go.GetComponent<Image>().color = new Color(.81f, .89f, .76f);
            var button = go.AddComponent<Button>(); button.onClick.AddListener(action);
            var label = Label(go.transform, title, 22, .02f, 0, .98f, 1);
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 14; label.resizeTextMaxSize = 22;
            return button;
        }
    }
}
