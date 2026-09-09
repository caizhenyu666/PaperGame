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
        private Sprite[] defaultIdle, defaultRun, defaultJump;

        public void Configure(Canvas canvas, GameObject homeScreen, C1PlayerController2D controller)
        {
            library = library ?? C1CharacterLibrary.Load();
            service = GetComponent<C1CharacterService>() ?? gameObject.AddComponent<C1CharacterService>();
            player = controller; home = homeScreen;
            var startObject = home.transform.Find("Start Play").gameObject;
            startPlay = startObject.GetComponent<Button>() ?? startObject.AddComponent<Button>();
            font = C1UiFont.Load();
            defaultIdle = Load("idle"); defaultRun = Load("run"); defaultJump = Load("jump");
            screen = Panel(canvas.transform, "Character Selection", new Color(0.98f, 0.95f, 0.86f), 0, 0, 1, 1);
            Label(screen.transform, "我的小主角", 42, .06f, .83f, .7f, .95f);
            ButtonAt(screen.transform, "返回首页（暂不进入关卡）", .74f, .85f, .95f, .94f, () => screen.SetActive(false));
            selectedLabel = Label(screen.transform, "", 20, .06f, .77f, .9f, .83f);
            var scrollRoot = Panel(screen.transform, "Characters", new Color(1, 1, 1, .7f), .05f, .2f, .4f, .75f);
            scrollRoot.AddComponent<RectMask2D>();
            var scroll = scrollRoot.AddComponent<ScrollRect>();
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(scrollRoot.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = Vector2.one; contentRect.pivot = new Vector2(.5f, 1);
            contentRect.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>(); layout.spacing = 12; layout.padding = new RectOffset(12,12,12,12);
            layout.childControlHeight = true; layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect; scroll.viewport = scrollRoot.GetComponent<RectTransform>(); scroll.horizontal = false;
            list = content.transform;
            var previewPanel = Panel(screen.transform, "Preview", Color.white, .44f, .29f, .94f, .75f);
            preview = Panel(previewPanel.transform, "Animated Character", Color.white, .25f, .12f, .75f, .92f).GetComponent<Image>();
            preview.preserveAspect = true; preview.raycastTarget = false;
            nameLabel = Label(screen.transform, "默认主角", 26, .44f, .22f, .94f, .29f);
            ButtonAt(screen.transform, "预览跑步", .46f, .13f, .64f, .21f, () => { previewJump = false; previewTime = 0; });
            ButtonAt(screen.transform, "预览跳跃", .66f, .13f, .84f, .21f, () => { previewJump = true; previewTime = 0; });
            create = ButtonAt(screen.transform, "拍照并创建新主角", .05f, .1f, .26f, .18f, () => capture.OpenCamera());
            use = ButtonAt(screen.transform, "选中并用于关卡", .74f, .02f, .96f, .1f, Select);
            retry = ButtonAt(screen.transform, "重试生成 / 查询进度", .29f, .1f, .45f, .18f, Retry);
            status = Label(screen.transform, "先选已有主角，或点击“拍照并创建新主角”拍下完整的小人", 19, .05f, .015f, .73f, .085f);
            var receiver = new GameObject("Character Photo Receiver " + GetInstanceID());
            receiver.transform.SetParent(screen.transform, false);
            capture = receiver.AddComponent<C1PhotoCapture>(); capture.Configure(null, status); capture.PhotoCaptured += OnPhoto;
            ButtonAt(home.transform, "创建主角", .04f, .06f, .25f, .16f, Show);
            screen.SetActive(false);
            RefreshList();
            if (cache.ContainsKey(library.selectedId)) Apply(library.selectedId);
            else if (library.selectedId != "default" && Application.isPlaying) StartCoroutine(Browse(library.selectedId, true));
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
            for (var i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i).gameObject; child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            AddRow("default", "默认主角");
            for (var i = 0; i < library.characters.Count; i++) AddRow(library.characters[i].characterId, "我的涂鸦 " + (i + 1));
            selectedLabel.text = "已选：" + DisplayName(library.selectedId) + "   ·   角色记录保存在当前浏览器";
        }
        private void AddRow(string id, string title)
        {
            var button = ButtonAt(list, title + (library.selectedId == id ? "  ✓" : ""), 0, 0, 1, 1,
                () => { if (!busy) StartCoroutine(Browse(id, false)); });
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var icon = Panel(button.transform, "Thumbnail", Color.white, .02f, .08f, .24f, .92f).GetComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;
            icon.sprite = id == "default" ? defaultIdle[0] : cache.ContainsKey(id) ? cache[id].run[0] : null;
            icon.enabled = icon.sprite != null;
        }
        private string DisplayName(string id) => id == "default" ? "默认主角" : "我的涂鸦 " + (library.characters.FindIndex(x => x.characterId == id) + 1);
        private void SetBusy(bool value)
        {
            busy = value; create.interactable = !value; use.interactable = !value; retry.interactable = !value;
            startPlay.interactable = !value;
        }
        private IEnumerator Browse(string id, bool apply)
        {
            requestedId = id;
            retryDownload = false;
            SetBusy(true);
            var downloadOk = true;
            if (id != "default" && !cache.ContainsKey(id))
            {
                status.text = "正在加载主角动画…";
                var record = library.characters.Find(x => x.characterId == id);
                yield return service.Download(record, frames => cache[id] = frames, message => status.text = message);
                if (!cache.ContainsKey(id)) { retryDownload = true; downloadOk = false; status.text += "\n\u70b9\u51fb\u201c\u91cd\u8bd5\u751f\u6210 / \u67e5\u8be2\u8fdb\u5ea6\u201d\u53ef\u4ee5\u91cd\u65b0\u4e0b\u8f7d"; }
            }
            if (downloadOk)
            {
                browsingId = id; previewTime = 0; previewJump = false;
                nameLabel.text = DisplayName(id); status.text = "\u53ef\u70b9\u51fb\u201c\u9884\u89c8\u8dd1\u6b65\u201d\u6216\u201c\u9884\u89c8\u8df3\u8dc3\u201d\uff0c\u518d\u70b9\u51fb\u201c\u9009\u4e2d\u5e76\u7528\u4e8e\u5173\u5361\u201d\u5b8c\u6210\u9009\u62e9";
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
                    status.text = "\u2713 \u751f\u6210\u6210\u529f\uff01\u5df2\u6dfb\u52a0\u5230\u89d2\u8272\u5217\u8868\uff0c\u70b9\u51fb\u201c\u9009\u4e2d\u5e76\u7528\u4e8e\u5173\u5361\u201d\u5373\u53ef\u4f7f\u7528";
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
        }
        private void Apply(string id)
        {
            var visual = player.transform.Find("Character Visual");
            var animator = visual.GetComponent<C1CharacterAnimator2D>();
            if (id == "default")
            {
                // Restore authored default animation speeds as well as its frames.
                animator.ConfigureRemote(defaultRun, defaultJump, 10, 10);
                animator.Configure(defaultIdle, defaultRun, defaultJump);
                var scale = 2.2f / defaultIdle[0].bounds.size.y;
                visual.localScale = Vector3.one * scale; visual.localPosition = Vector3.zero;
            }
            else
            {
                var frames = cache[id]; animator.ConfigureRemote(frames.run, frames.jump, frames.runFps, frames.jumpFps);
                var scale = 2.2f / frames.run[0].bounds.size.y;
                visual.localScale = Vector3.one * scale;
                visual.localPosition = new Vector3(0, -player.GetComponent<BoxCollider2D>().size.y / 2, 0);
            }
            visual.GetComponent<SpriteRenderer>().color = Color.white;
        }
        private void Update()
        {
            if (screen == null || !screen.activeSelf) return;
            var frames = previewJump ? defaultJump : defaultRun;
            var fps = 10f;
            if (browsingId != "default" && cache.TryGetValue(browsingId, out var remote))
            { frames = previewJump ? remote.jump : remote.run; fps = previewJump ? remote.jumpFps : remote.runFps; }
            if (frames.Length == 0) return;
            previewTime += Time.unscaledDeltaTime;
            preview.sprite = frames[(int)(previewTime * fps) % frames.Length];
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
