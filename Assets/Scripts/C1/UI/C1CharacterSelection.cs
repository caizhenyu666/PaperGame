using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1
{
    public sealed class C1CharacterSelection : MonoBehaviour
    {
        private readonly Dictionary<string, C1CharacterFrames> cache = new Dictionary<string, C1CharacterFrames>();
        private C1CharacterLibrary library;
        private C1CharacterService service;
        private C1PhotoCapture capture;
        private C1PlayerController2D player;
        private GameObject screen, home;
        private Transform list;
        private Image preview;
        private Text status, nameLabel, selectedLabel;
        private Button create, use, retry, startPlay;
        private Font font;
        private string browsingId = "default", requestedId = "default", photoMime;
        private byte[] photoBytes;
        private bool busy, previewJump, forceRetry, retryDownload;
        private float previewTime;

        private Action returnHomeAction;
        private Image runPreview, jumpPreview;
        private bool previewRunning;
        private Vector2 previewOrigin;

        public GameObject CreateScreen(Canvas canvas, Action returnHome)
        {
            library = C1CharacterLibrary.Load();
            service = GetComponent<C1CharacterService>() ?? gameObject.AddComponent<C1CharacterService>();
            font = C1UiFont.Load();
            returnHomeAction = returnHome;
            var prefab = Resources.Load<GameObject>("C1UI/PaperGameCharacterSelection");
            screen = prefab != null ? Instantiate(prefab, canvas.transform, false) : C1CharacterScreenLayout.Build(canvas.transform);
            var rootRect = screen.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            var fit = screen.GetComponent<C1CharacterScreenFit>() ?? screen.AddComponent<C1CharacterScreenFit>();
            fit.RefreshLayout();
            list = screen.transform.Find("Characters/Content");
            preview = screen.transform.Find("Preview/Animated Character").GetComponent<Image>();
            previewOrigin = preview.rectTransform.anchoredPosition;
            runPreview = screen.transform.Find("Run/Character").GetComponent<Image>();
            jumpPreview = screen.transform.Find("Jump/Character").GetComponent<Image>();
            status = screen.transform.Find("Status").GetComponent<Text>();
            nameLabel = screen.transform.Find("Name").GetComponent<Text>();
            selectedLabel = screen.transform.Find("Selected").GetComponent<Text>();
            Hook("Back", () => { if (!busy) returnHomeAction?.Invoke(); });
            Hook("Run", () => { previewRunning = true; previewJump = false; previewTime = 0; });
            Hook("Jump", () => { previewRunning = false; previewJump = true; previewTime = 0; });
            create = Hook("Create", () => capture.OpenCamera());
            Hook("Add Photo", () => { if (!busy) capture.OpenCamera(); });
            use = Hook("Use", Select);
            retry = Hook("Retry", Retry);
            var receiver = new GameObject("Character Photo Receiver " + GetInstanceID());
            receiver.transform.SetParent(screen.transform, false);
            capture = receiver.AddComponent<C1PhotoCapture>();
            capture.Configure(null, status); capture.PhotoCaptured += OnPhoto;
            browsingId = requestedId = library.selectedId;
            previewJump = previewRunning = false; previewTime = 0;
            RefreshList();
            UpdatePreview();
            if (!C1BuiltInCharacters.IsBuiltIn(browsingId) && Application.isPlaying)
                StartCoroutine(Browse(browsingId, false));
            return screen;
        }

        private Button Hook(string name, UnityEngine.Events.UnityAction action)
        {
            var button = screen.transform.Find(name).GetComponent<Button>();
            var graphic = button.GetComponent<Graphic>();
            if (graphic != null) { graphic.raycastTarget = true; button.targetGraphic = graphic; }
            button.onClick.AddListener(action);
            return button;
        }

        public void Configure(Canvas canvas, GameObject homeScreen, C1PlayerController2D controller)
        {
            player = controller; home = homeScreen;
            startPlay = home.transform.Find("Start Play").GetComponent<Button>();
            CreateScreen(canvas, () => { screen.SetActive(false); home.SetActive(true); });
            var createObject = home.transform.Find("Create Role");
            if (createObject != null) createObject.GetComponent<Button>().onClick.AddListener(Show);
            screen.SetActive(false);
            if (C1BuiltInCharacters.IsBuiltIn(library.selectedId)) Apply(library.selectedId);
            else if (Application.isPlaying) StartCoroutine(Browse(library.selectedId, true));
            PauseAtHome();
        }
        public void StartPlaying()
        {
            if (busy) return;
            home.SetActive(false); screen.SetActive(false);
            player.enabled = true; player.GetComponent<Rigidbody2D>().simulated = true;
        }
        private void PauseAtHome()
        {
            player.enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
        }
        private void Show()
        {
            screen.SetActive(true); screen.transform.SetAsLastSibling();
            if (!busy) StartCoroutine(Browse(library.selectedId, false));
        }
        private void RefreshList()
        {
            Debug.Log("[RefreshList] Starting, list=" + (list != null ? "valid" : "NULL") + ", browsingId=" + browsingId + ", cache entries=" + cache.Count);
            for (var i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i).gameObject; child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            var priorityId = library.selectedId;
            if (!C1BuiltInCharacters.IsBuiltIn(priorityId) && !library.characters.Exists(x => x.characterId == priorityId))
                priorityId = C1BuiltInCharacters.Green;
            Debug.Log("[RefreshList] priorityId=" + priorityId + ", total characters=" + (2 + library.characters.Count));
            var ordered = new List<KeyValuePair<string, string>>();
            ordered.Add(new KeyValuePair<string, string>(C1BuiltInCharacters.Green, C1BuiltInCharacters.Name(C1BuiltInCharacters.Green)));
            ordered.Add(new KeyValuePair<string, string>(C1BuiltInCharacters.Chick, C1BuiltInCharacters.Name(C1BuiltInCharacters.Chick)));
            for (var i = 0; i < library.characters.Count; i++)
                ordered.Add(new KeyValuePair<string, string>(library.characters[i].characterId, "我的涂鸦 " + (i + 1)));
            ordered.RemoveAll(x => x.Key == priorityId);
            ordered.Insert(0, new KeyValuePair<string, string>(priorityId, DisplayName(priorityId)));
            foreach (var entry in ordered) AddRow(entry.Key, entry.Value);
            selectedLabel.text = "";
            Debug.Log("[RefreshList] Done, created " + ordered.Count + " rows");
        }
        private void AddRow(string id, string title)
        {
            var button = ButtonAt(list, title, 0, 0, 1, 1,
                () => BrowseCharacter(id));
            button.GetComponent<Image>().sprite = C1CharacterScreenLayout.Art("paper");
            button.GetComponent<Image>().color = Color.white;
            button.transform.GetChild(0).gameObject.SetActive(false);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 170;
            var icon = Panel(button.transform, "Thumbnail", Color.white, .12f, .02f, .57f, .92f).GetComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;
            if (C1BuiltInCharacters.IsBuiltIn(id))
            {
                icon.sprite = C1CharacterScreenLayout.Art(id == C1BuiltInCharacters.Chick ? "chick-thumbnail" : "green-thumbnail");
            }
            else if (cache.ContainsKey(id) && cache[id].run != null && cache[id].run.Length > 0)
            {
                icon.sprite = cache[id].run[0];
            }
            else
            {
                var record = library.characters.Find(x => x.characterId == id);
                icon.sprite = service.LoadThumbnail(id, record);
            }
            icon.enabled = icon.sprite != null;
            Debug.Log("[AddRow] id=" + id + ", title=" + title + ", sprite=" + (icon.sprite != null ? icon.sprite.name : "NULL") + ", enabled=" + icon.enabled);
            var selected = Panel(button.transform, "Selection", Color.white, 0, 0, 1, 1).GetComponent<Image>();
            selected.sprite = C1CharacterScreenLayout.Art("selected"); selected.raycastTarget = false;
            selected.enabled = browsingId == id;
            var star = Panel(button.transform, "Star", Color.white, .77f, .35f, .93f, .8f).GetComponent<Image>();
            star.sprite = C1CharacterScreenLayout.Art(library.selectedId == id ? "star-on" : "star-off");
            star.preserveAspect = true; star.raycastTarget = false;
            if (!C1BuiltInCharacters.IsBuiltIn(id)) Label(button.transform, title, 20, .52f, .1f, .76f, .9f);
        }
        private string DisplayName(string id) => C1BuiltInCharacters.IsBuiltIn(id) ? C1BuiltInCharacters.Name(id) : "我的涂鸦 " + (library.characters.FindIndex(x => x.characterId == id) + 1);
        private void BrowseCharacter(string id)
        {
            if (busy) return;
            if (!C1BuiltInCharacters.IsBuiltIn(id)) { StartCoroutine(Browse(id, false)); return; }
            browsingId = requestedId = id;
            previewTime = 0; previewRunning = previewJump = retryDownload = false;
            status.text = "";
            UpdatePreview(); RefreshList();
        }
        private void SetBusy(bool value)
        {
            busy = value; create.interactable = !value; use.interactable = !value; retry.interactable = !value;
            foreach (var name in new[] { "Back", "Add Photo", "Run", "Jump" }) screen.transform.Find(name).GetComponent<Button>().interactable = !value;
            if (startPlay != null) startPlay.interactable = !value;
        }
        private IEnumerator Browse(string id, bool apply)
        {
            requestedId = id;
            retryDownload = false;
            SetBusy(true);
            var downloadOk = true;
            if (!C1BuiltInCharacters.IsBuiltIn(id) && !cache.ContainsKey(id))
            {
                Debug.Log("[Browse] Loading character: " + id + ", cache has " + cache.Count + " entries");
                var record = library.characters.Find(x => x.characterId == id);
                if (record == null) { status.text = "找不到该角色，请重新选择"; SetBusy(false); yield break; }
                Debug.Log("[Browse] Found record, jobId=" + record.jobId + ", trying local cache...");
                if (service.TryLoadFrames(id, record, out var cached))
                {
                    Debug.Log("[Browse] Local cache HIT for " + id + ", run frames=" + cached.run.Length + ", jump frames=" + cached.jump.Length);
                    cache[id] = cached;
                    status.text = "";
                    browsingId = id; previewTime = 0; previewJump = previewRunning = false;
                    nameLabel.text = "";
                    UpdatePreview();
                    if (apply) Apply(id);
                    RefreshList();
                    SetBusy(false);
                    yield break;
                }
                else
                {
                    Debug.Log("[Browse] Local cache MISS for " + id + ", downloading from server...");
                    status.text = "正在加载主角动画…";
                    yield return service.Download(record, frames => {
                        Debug.Log("[Browse] Download complete for " + id + ", run=" + frames.run.Length + " frames, jump=" + frames.jump.Length + " frames");
                        cache[id] = frames;
                        service.SaveFrames(id, frames);
                    }, message => {
                        Debug.LogWarning("[Browse] Download progress/error: " + message);
                        status.text = message;
                    });
                }
                if (!cache.ContainsKey(id)) {
                    Debug.LogWarning("[Browse] Cache still empty after download attempt for " + id);
                    retryDownload = true; downloadOk = false; status.text += "\n\u70b9\u51fb\u201c\u91cd\u8bd5\u751f\u6210 / \u67e5\u8be2\u8fdb\u5ea6\u201d\u53ef\u4ee5\u91cd\u65b0\u4e0b\u8f7d";
                }
            }
            else
            {
                Debug.Log("[Browse] Character " + id + " already in cache or is built-in");
            }
            if (downloadOk)
            {
                Debug.Log("[Browse] Updating UI for " + id + ", cache now has " + cache.Count + " entries");
                browsingId = id; previewTime = 0; previewJump = previewRunning = false;
                nameLabel.text = ""; status.text = "";
                UpdatePreview();
                if (apply) Apply(id);
                RefreshList();
            }
            SetBusy(false);
        }
        private void OnPhoto(byte[] bytes, string mime)
        {
            if (busy) return;
            retryDownload = false;
            photoBytes = bytes; photoMime = mime; forceRetry = false;
            status.text = "\u7167\u7247\u5df2\u63a5\u6536\uff0c\u6b63\u5728\u4e0a\u4f20\u2026";
            StartCoroutine(Generate(false));
        }
        private void Retry()
        {
            if (busy) return;
            if (retryDownload) { status.text = "\u6b63\u5728\u91cd\u65b0\u4e0b\u8f7d\u89d2\u8272\u52a8\u753b\u2026"; StartCoroutine(Browse(requestedId, requestedId == library.selectedId)); }
            else if (forceRetry && photoBytes != null) { status.text = "\u6b63\u5728\u91cd\u65b0\u4e0a\u4f20\u4f60\u7684\u6d82\u9e26\u2026"; StartCoroutine(Generate(true)); }
            else if (!string.IsNullOrEmpty(library.pendingJobId)) { status.text = "\u6b63\u5728\u67e5\u8be2\u751f\u6210\u8fdb\u5ea6\u2026"; StartCoroutine(ContinueJob()); }
            else if (photoBytes != null) { status.text = "\u6b63\u5728\u91cd\u65b0\u4e0a\u4f20\u4f60\u7684\u6d82\u9e26\u2026"; StartCoroutine(Generate(false)); }
            else { status.text = "\u6b63\u5728\u52a0\u8f7d\u89d2\u8272\u2026"; StartCoroutine(Browse(requestedId, requestedId == library.selectedId)); }
        }
        private IEnumerator Generate(bool force)
        {
            SetBusy(true);
            string id = null;
            yield return service.Upload(photoBytes, photoMime, force, accepted => id = accepted, message => status.text = message);
            if (id == null) { SetBusy(false); yield break; }
            library.pendingJobId = id; library.Save();
            yield return ContinueJob();
        }
        private IEnumerator ContinueJob()
        {
            SetBusy(true);
            C1CharacterRecord result = null;
            yield return service.Poll(library.pendingJobId, message => status.text = message, ready => result = ready,
                message => { status.text = message; forceRetry = message.StartsWith("\u751f\u6210\u5931\u8d25"); });
            if (result != null)
            {
                if (string.IsNullOrEmpty(result.characterId)) { status.text = "\u670d\u52a1\u5668\u7f3a\u5c11\u89d2\u8272\u7f16\u53f7\uff0c\u8bf7\u70b9\u51fb\u201c\u91cd\u8bd5\u751f\u6210\u201d"; SetBusy(false); yield break; }
                library.Add(result);
                yield return Browse(result.characterId, false);
                if (cache.ContainsKey(result.characterId))
                {
                    photoBytes = null; forceRetry = false;
                    status.text = "\u2713 \u751f\u6210\u6210\u529f\uff01\u5df2\u6dfb\u52a0\u5230\u89d2\u8272\u5217\u8868\uff0c\u70b9\u51fb\u201c\u9009\u4e2d\u5e76\u7528\u4e8e\u5173\u5361\u201d\u5373\u53ef\u4f7f\u7528";
                }
            }
            else
            {
                status.text += "\n\u70b9\u51fb\u201c\u91cd\u8bd5\u751f\u6210 / \u67e5\u8be2\u8fdb\u5ea6\u201d\u53ef\u4ee5\u7ee7\u7eed";
            }
            SetBusy(false);
        }
        private void Select()
        {
            if (busy) return;
            library.selectedId = browsingId; library.Save(); Apply(browsingId); RefreshList();
            status.text = "已选择“" + DisplayName(browsingId) + "”，返回首页后点击“开始游戏”即可使用它";
            returnHomeAction?.Invoke();
        }
        private void Apply(string id)
        {
            if (player == null) return;
            if (C1BuiltInCharacters.IsBuiltIn(id)) { C1BuiltInCharacters.Apply(player, id); return; }
            if (cache.TryGetValue(id, out var frames)) C1GameBootstrap.ApplyRemoteCharacter(player, frames);
        }
        private void Update()
        {
            if (screen == null || !screen.activeSelf) return;
            previewTime += Time.unscaledDeltaTime;
            UpdatePreview();
        }
        private void UpdatePreview()
        {
            if (preview == null) return;
            retry.gameObject.SetActive(retryDownload || forceRetry || !string.IsNullOrEmpty(library.pendingJobId) || photoBytes != null);
            var sprite = C1BuiltInCharacters.Sprite(browsingId);
            var run = sprite; var jump = sprite;
            if (C1BuiltInCharacters.IsBuiltIn(browsingId))
            {
                run = C1BuiltInCharacters.PreviewFrame(browsingId, "run", previewTime);
                jump = C1BuiltInCharacters.PreviewFrame(browsingId, "jump", previewTime);
                sprite = C1BuiltInCharacters.PreviewFrame(browsingId, "idle", previewTime);
                if (previewJump) sprite = C1BuiltInCharacters.PreviewFrame(browsingId, "jump", previewTime, false);
                else if (previewRunning) sprite = run;
            }
            if (!C1BuiltInCharacters.IsBuiltIn(browsingId))
            {
                sprite = run = jump = null;
                if (cache.TryGetValue(browsingId, out var remote))
                {
                    run = remote.run[(int)(previewTime * remote.runFps) % remote.run.Length];
                    jump = remote.jump[Mathf.Min((int)(previewTime * remote.jumpFps), remote.jump.Length - 1)];
                    sprite = previewJump ? jump : run;
                }
            }
            preview.sprite = sprite; preview.enabled = sprite != null;
            var scale = C1BuiltInCharacters.IsBuiltIn(browsingId) ? 256f / 190f : 1f;
            preview.rectTransform.localScale = Vector3.one * scale;
            runPreview.rectTransform.localScale = jumpPreview.rectTransform.localScale = Vector3.one * scale;
            runPreview.sprite = run; jumpPreview.sprite = jump;
            runPreview.enabled = run != null; jumpPreview.enabled = jump != null;
            var jumpOffset = previewJump ? Mathf.Sin(Mathf.Clamp01(previewTime / .8f) * Mathf.PI) * 95f : 0;
            var runOffset = previewRunning ? Mathf.Sin(previewTime * 3f) * 75f : 0;
            preview.rectTransform.anchoredPosition = previewOrigin + new Vector2(runOffset, jumpOffset);
            preview.rectTransform.localEulerAngles = new Vector3(0, 0, previewRunning ? Mathf.Sin(previewTime * 14) * 4 : 0);
            if (previewJump && previewTime > .8f) previewJump = false;
        }
        private void OnDestroy() { foreach (var frames in cache.Values) frames.Dispose(); }
        private static Sprite[] Load(string action)
        {
            var frames = Resources.LoadAll<Sprite>("C1Character/" + action);
            Array.Sort(frames, (a,b) => string.CompareOrdinal(a.name,b.name)); return frames;
        }
        private GameObject Panel(Transform parent, string title, Color color, float x, float y, float right, float top)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(x,y); rect.anchorMax = new Vector2(right,top);
            rect.offsetMin = rect.offsetMax = Vector2.zero; go.GetComponent<Image>().color = color; return go;
        }
        private Text Label(Transform parent, string title, int size, float x, float y, float right, float top)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(x,y); rect.anchorMax = new Vector2(right,top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>(); text.font = font; text.text = title; text.fontSize = size;
            text.color = new Color(.22f,.26f,.24f); text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 12; text.resizeTextMaxSize = size; return text;
        }
        private Button ButtonAt(Transform parent, string title, float x, float y, float right, float top, UnityEngine.Events.UnityAction action)
        {
            var go = Panel(parent, title, new Color(.82f,.9f,.77f), x,y,right,top);
            var button = go.AddComponent<Button>(); button.onClick.AddListener(action);
            Label(go.transform, title, 24, .05f,0,.95f,1); return button;
        }
    }
}
