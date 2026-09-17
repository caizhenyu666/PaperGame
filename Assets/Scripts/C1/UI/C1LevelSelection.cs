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
        private GameObject itemTemplate;
        private RawImage preview;
        private Text status;
        private Button create, play, regenerate, help, back;
        private readonly List<Texture2D> thumbnailTextures = new List<Texture2D>();
        private GameObject selectedRow;
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
            service = GetComponent<C1LevelService>() ?? gameObject.AddComponent<C1LevelService>();
            storage = GetComponent<C1LocalStorage>() ?? gameObject.AddComponent<C1LocalStorage>();
            list = Require("Level Book/Viewport/Content");
            itemTemplate = Require("Level Book/Viewport/Content/Level Item Template").gameObject;
            preview = Require("Preview Paper/Level Preview").GetComponent<RawImage>();
            status = Require("Action Bar/Status Paper/Status").GetComponent<Text>();
            create = Hook("Level Book/Create Level", RequestCreateLevel);
            regenerate = Hook("Action Bar/Regenerate", () => StartWork(true));
            play = Hook("Action Bar/Play", Play);
            help = Hook("Header/Drawing Help", () => ShowDrawingTutorial(false));
            back = Hook("Header/Back Home", () => { if (!busy) returnHome?.Invoke(); });
            capture = GetComponent<C1PhotoCapture>() ?? gameObject.AddComponent<C1PhotoCapture>();
            capture.Configure(null, status); capture.PhotoCaptured += OnPhoto;
            if (!Try(RefreshList)) return;
            BrowseBuiltIn();
            SetBusy(false);
            if (LevelCount <= 1)
                status.text = "请画好平台、一个起点圆圈和一个三角旗帜后拍照上传；标记可用黑笔空心绘制。";
            else status.text = "请选择本地关卡，或拍照上传创建新关卡。";
            if (library.Pending != null) status.text = "上次的照片已保留，可点击“重新生成”。";
            UpdateRegenerateState();
            if (createImmediately) RequestCreateLevel();
        }

        private Transform Require(string path)
        {
            var node = transform.Find(path);
            if (node == null) throw new MissingReferenceException("关卡选择预制体缺少节点：" + path);
            return node;
        }

        private Button Hook(string path, UnityEngine.Events.UnityAction action)
        {
            var button = Require(path).GetComponent<Button>();
            if (button == null) throw new MissingReferenceException("关卡选择预制体节点缺少 Button：" + path);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            return button;
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
                    ? "\n请调整纸面后重新拍照上传。" : "\n可点击“重新生成”重试。");
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
            for (var i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i);
                if (child.gameObject != itemTemplate) C1LevelLibrary.Release(child.gameObject);
            }
            ReleaseThumbnailTextures();
            var levels = library.Load();
            LevelCount = 1 + levels.Count;
            var builtInTexture = Resources.Load<Texture2D>("C1Levels/level1-background");
            var builtInRow = CreateItem("第1页", "第 1 页", "内置关卡", builtInTexture);
            builtInRow.GetComponent<Button>().onClick.AddListener(() =>
            {
                BrowseBuiltIn();
                SelectRow(builtInRow);
            });
            for (var i = 0; i < levels.Count; i++)
            {
                var item = levels[i];
                var page = i + 2;
                Texture2D thumbnail = null;
                Try(() =>
                {
                    library.Read(item.id, out thumbnail);
                    thumbnailTextures.Add(thumbnail);
                });
                var row = CreateItem(item.title, item.title, "我的关卡", thumbnail);
                row.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Browse(item, page);
                    SelectRow(row);
                });
            }
            SelectRow(builtInRow);
        }

        private GameObject CreateItem(string objectName, string title, string source, Texture texture)
        {
            var row = Instantiate(itemTemplate, list, false);
            row.name = objectName;
            row.transform.Find("Label").GetComponent<Text>().text = title;
            row.transform.Find("Source").GetComponent<Text>().text = source;
            var thumbnail = row.transform.Find("Thumbnail").GetComponent<RawImage>();
            thumbnail.texture = texture;
            thumbnail.color = texture == null ? new Color(1f, .98f, .90f, 1f) : Color.white;
            row.SetActive(true);
            row.GetComponent<Button>().interactable = !busy;
            return row;
        }

        private void SelectRow(GameObject row)
        {
            selectedRow = row;
            foreach (Transform child in list)
            {
                if (child.gameObject == itemTemplate) continue;
                var selectedState = child.gameObject == selectedRow;
                child.Find("Selection").gameObject.SetActive(selectedState);
                var star = child.Find("Star");
                star.Find("On").gameObject.SetActive(selectedState);
                star.Find("Off").gameObject.SetActive(!selectedState);
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
                status.text = "第 1 页 · 内置关卡，可以直接开始。";
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
            help.interactable = !value;
            back.interactable = !value;
            play.interactable = !value && (builtInSelected && selected == null || selected != null);
            UpdateRegenerateState();
            foreach (var button in list.GetComponentsInChildren<Button>()) button.interactable = !value;
        }

        private void UpdateRegenerateState()
        {
            var hasPending = false;
            Try(() => hasPending = library != null && library.Pending != null);
            regenerate.gameObject.SetActive(hasPending);
            regenerate.interactable = !busy && hasPending;
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
            ReleaseThumbnailTextures();
        }

        private void ReleaseSelectedTexture()
        {
            if (selectedTextureIsAsset) { selectedTexture = null; return; }
            C1LevelLibrary.Release(selectedTexture);
            selectedTexture = null;
        }

        private void ReleaseThumbnailTextures()
        {
            foreach (var texture in thumbnailTextures) C1LevelLibrary.Release(texture);
            thumbnailTextures.Clear();
        }

        internal static GameObject Panel(Transform parent, string title, float x, float y, float right, float top)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(.98f, .96f, .89f);
            return go;
        }

    }
}
