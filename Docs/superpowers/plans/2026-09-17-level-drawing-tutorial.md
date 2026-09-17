# 儿童关卡绘画新手引导实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在关卡创建入口加入 6 页、可跳过、可重看、带中文语音与轻量动画的儿童绘本式绘画引导，并在首次完成后直接进入现有相机。

**架构：** 新增独立的 `C1LevelDrawingTutorialController` 管理页面、首次展示偏好、语音和完成回调；教程 UI 由编辑器生成器产出独立 Resources 预制体。`C1LevelSelection` 只负责决定首次创建时是否打开教程，以及教程结束后调用现有 `OpenCamera()`，不接管教程内部状态。

**技术栈：** Unity 2022.3、C#、UGUI、PlayerPrefs、Resources、NUnit EditMode、ImageGen、macOS `say`/`afconvert`

---

## 文件结构

- 创建：`Assets/Scripts/C1/UI/C1LevelDrawingTutorialController.cs`——页面状态、首次展示偏好、语音、完成回调。
- 创建：`Assets/Scripts/C1/UI/C1LevelDrawingTutorialPageAnimator.cs`——基于 `Time.unscaledDeltaTime` 的轻量页面循环动画。
- 创建：`Assets/Editor/PaperGameLevelDrawingTutorialPrefabGenerator.cs`——导入插画并生成独立教程预制体。
- 创建：`Assets/Resources/C1UI/PaperGameLevelDrawingTutorial.prefab`——运行时加载的 6 页教程 UI。
- 创建：`Assets/Art/UI/LevelDrawingTutorial/tutorial-page-01.png` 至 `tutorial-page-06.png`——与首页一致的无文字蜡笔／铅笔场景插画。
- 创建：`Assets/Resources/C1TutorialAudio/page-01.aiff` 至 `page-06.aiff`——6 句中文语音。
- 创建：`Assets/Tests/EditMode/C1LevelDrawingTutorialTests.cs`——教程翻页、跳过、完成、重看和静音测试。
- 创建：`Assets/Tests/EditMode/PaperGameLevelDrawingTutorialPrefabTests.cs`——预制体结构、素材和安全区测试。
- 修改：`Assets/Scripts/C1/UI/C1LevelSelection.cs`——首次创建前置教程、主动重看入口和相机回调。
- 修改：`Assets/Tests/EditMode/C1LevelSelectionTests.cs`——首次入口、已读入口和主动重看测试。
- 创建：`Docs/ui/level-drawing-tutorial-preview.png`——最终预制体渲染预览。

### 任务 1：实现教程状态与偏好

**文件：**
- 创建：`Assets/Scripts/C1/UI/C1LevelDrawingTutorialController.cs`
- 创建：`Assets/Tests/EditMode/C1LevelDrawingTutorialTests.cs`

- [ ] **步骤 1：编写失败的状态测试**

测试创建一个最小层级：6 个 `Page N`、`Next`、`Skip`、`Sound` 和 `Page Dots`。覆盖：

```csharp
[Test]
public void Open_StartsAtFirstOfSixPages()
{
    controller.Open(null);
    Assert.That(controller.PageCount, Is.EqualTo(6));
    Assert.That(controller.CurrentPageIndex, Is.EqualTo(0));
    Assert.That(controller.IsOpen, Is.True);
}

[Test]
public void Next_OnLastPage_RecordsSeenAndInvokesCreateOnce()
{
    var calls = 0;
    controller.Open(() => calls++);
    for (var i = 0; i < 6; i++) controller.Next();
    Assert.That(C1LevelDrawingTutorialController.HasSeen, Is.True);
    Assert.That(calls, Is.EqualTo(1));
    Assert.That(controller.IsOpen, Is.False);
}

[Test]
public void Skip_RecordsSeenAndInvokesCreateOnce()
{
    var calls = 0;
    controller.Open(() => calls++);
    controller.Skip();
    controller.Skip();
    Assert.That(calls, Is.EqualTo(1));
    Assert.That(C1LevelDrawingTutorialController.HasSeen, Is.True);
}

[Test]
public void HelpMode_DismissesWithoutInvokingCreate()
{
    controller.Open(null);
    for (var i = 0; i < 6; i++) controller.Next();
    Assert.That(controller.IsOpen, Is.False);
}
```

测试 `SetUp` 和 `TearDown` 必须删除：

```csharp
PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.SeenPreferenceKey);
PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.MutedPreferenceKey);
```

- [ ] **步骤 2：运行测试验证失败**

运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics \
  -projectPath "$(pwd)" -runTests -testPlatform EditMode \
  -testFilter PaperGame.C1.Tests.C1LevelDrawingTutorialTests \
  -testResults /private/tmp/papergame-level-tutorial-state.xml \
  -logFile /private/tmp/papergame-level-tutorial-state.log
```

预期：FAIL，提示 `C1LevelDrawingTutorialController` 不存在。

- [ ] **步骤 3：实现最少控制器**

控制器公开以下稳定接口：

```csharp
public const string SeenPreferenceKey = "C1.LevelDrawingTutorial.Seen.v1";
public const string MutedPreferenceKey = "C1.LevelDrawingTutorial.Muted.v1";
public static bool HasSeen => PlayerPrefs.GetInt(SeenPreferenceKey, 0) == 1;
public int PageCount => pages.Length;
public int CurrentPageIndex { get; private set; }
public bool IsOpen => gameObject.activeSelf;
public bool IsMuted => PlayerPrefs.GetInt(MutedPreferenceKey, 0) == 1;

public void Open(Action afterDismiss)
{
    this.afterDismiss = afterDismiss;
    completed = false;
    CurrentPageIndex = 0;
    gameObject.SetActive(true);
    ShowPage();
}

public void Next()
{
    if (completed) return;
    if (CurrentPageIndex < pages.Length - 1)
    {
        CurrentPageIndex++;
        ShowPage();
        return;
    }
    Complete();
}

public void Skip() => Complete();

private void Complete()
{
    if (completed) return;
    completed = true;
    PlayerPrefs.SetInt(SeenPreferenceKey, 1);
    PlayerPrefs.Save();
    audioSource.Stop();
    gameObject.SetActive(false);
    var callback = afterDismiss;
    afterDismiss = null;
    callback?.Invoke();
}
```

`Awake()` 从固定名称读取 6 个页面和按钮，绑定监听；`ShowPage()` 只激活当前页、刷新 6 个页码点、切换末页按钮文案，并自动播放 `Resources/C1TutorialAudio/page-XX`。缺少 AudioClip 时直接跳过播放。

- [ ] **步骤 4：运行测试验证通过**

运行步骤 2 的命令。预期：全部 PASS。

- [ ] **步骤 5：提交状态控制器**

```bash
git add Assets/Scripts/C1/UI/C1LevelDrawingTutorialController.cs Assets/Scripts/C1/UI/C1LevelDrawingTutorialController.cs.meta Assets/Tests/EditMode/C1LevelDrawingTutorialTests.cs Assets/Tests/EditMode/C1LevelDrawingTutorialTests.cs.meta
git commit -m "feat(新手引导): 添加绘画教程状态控制器"
```

### 任务 2：制作 6 页手绘插画和教程预制体

**文件：**
- 创建：`Assets/Art/UI/LevelDrawingTutorial/tutorial-page-01.png` 至 `tutorial-page-06.png`
- 创建：`Assets/Scripts/C1/UI/C1LevelDrawingTutorialPageAnimator.cs`
- 创建：`Assets/Editor/PaperGameLevelDrawingTutorialPrefabGenerator.cs`
- 创建：`Assets/Resources/C1UI/PaperGameLevelDrawingTutorial.prefab`
- 创建：`Assets/Tests/EditMode/PaperGameLevelDrawingTutorialPrefabTests.cs`

- [ ] **步骤 1：编写失败的预制体测试**

```csharp
[Test]
public void TutorialPrefab_ContainsSixPagesAndControls()
{
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Resources/C1UI/PaperGameLevelDrawingTutorial.prefab");
    Assert.That(prefab, Is.Not.Null);
    Assert.That(prefab.GetComponent<C1LevelDrawingTutorialController>(), Is.Not.Null);
    Assert.That(prefab.transform.Find("Pages").childCount, Is.EqualTo(6));
    Assert.That(prefab.transform.Find("Next").GetComponent<Button>(), Is.Not.Null);
    Assert.That(prefab.transform.Find("Skip").GetComponent<Button>(), Is.Not.Null);
    Assert.That(prefab.transform.Find("Sound").GetComponent<Button>(), Is.Not.Null);
}

[TestCase(1)]
[TestCase(2)]
[TestCase(3)]
[TestCase(4)]
[TestCase(5)]
[TestCase(6)]
public void TutorialPrefab_PageUsesGeneratedArtwork(int page)
{
    var path = $"Assets/Art/UI/LevelDrawingTutorial/tutorial-page-{page:00}.png";
    Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null);
}
```

- [ ] **步骤 2：运行测试验证失败**

运行与任务 1 相同的 Unity 命令，`testFilter` 改为 `PaperGame.C1.Tests.PaperGameLevelDrawingTutorialPrefabTests`。预期：FAIL，提示预制体和插画不存在。

- [ ] **步骤 3：使用首页作为参考生成 6 张插画**

调用 ImageGen 时引用 `Docs/ui/review-assets/home-screen-v1.png`，每张图都要求：透明背景、无文字、无按钮、无 Emoji、黑色蜡笔轮廓、米白纸张、手绘不规则线条、与首页小黄鸡造型一致。

6 张内容分别为：横放白纸；空心起点圆与承载平台；多段水平平台；角色跨越合理间隙并对比过远间隙；无颜色限制的直旗杆与三角旗面；8:5 白色取景框完整包住纸面。

输出路径依次为：

```text
Assets/Art/UI/LevelDrawingTutorial/tutorial-page-01.png
Assets/Art/UI/LevelDrawingTutorial/tutorial-page-02.png
Assets/Art/UI/LevelDrawingTutorial/tutorial-page-03.png
Assets/Art/UI/LevelDrawingTutorial/tutorial-page-04.png
Assets/Art/UI/LevelDrawingTutorial/tutorial-page-05.png
Assets/Art/UI/LevelDrawingTutorial/tutorial-page-06.png
```

6 张图都使用 16:9 画布。生成器通过 `Image.preserveAspect = true` 适配实际像素尺寸，不拉伸插画。

- [ ] **步骤 4：实现页面轻量动画组件**

```csharp
public enum C1TutorialAnimationKind { PaperRise, Bob, Drift, Jump, Sway, Pulse }

public sealed class C1LevelDrawingTutorialPageAnimator : MonoBehaviour
{
    [SerializeField] private C1TutorialAnimationKind kind;
    private Vector3 originPosition;
    private Vector3 originScale;
    private Quaternion originRotation;
    private float elapsed;

    private void OnEnable()
    {
        elapsed = 0f;
        originPosition = transform.localPosition;
        originScale = transform.localScale;
        originRotation = transform.localRotation;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        var wave = Mathf.Sin(elapsed * Mathf.PI * 0.8f);
        transform.localPosition = originPosition;
        transform.localScale = originScale;
        transform.localRotation = originRotation;
        switch (kind)
        {
            case C1TutorialAnimationKind.PaperRise:
                transform.localPosition = originPosition + Vector3.up * Mathf.Lerp(-12f, 0f, Mathf.Clamp01(elapsed * 2f));
                break;
            case C1TutorialAnimationKind.Bob:
                transform.localPosition = originPosition + Vector3.up * wave * 6f;
                break;
            case C1TutorialAnimationKind.Drift:
                transform.localPosition = originPosition + Vector3.right * wave * 8f;
                break;
            case C1TutorialAnimationKind.Jump:
                transform.localPosition = originPosition + Vector3.up * Mathf.Max(0f, wave) * 12f;
                break;
            case C1TutorialAnimationKind.Sway:
                transform.localRotation = originRotation * Quaternion.Euler(0f, 0f, wave * 4f);
                break;
            case C1TutorialAnimationKind.Pulse:
                transform.localScale = originScale * (1f + wave * 0.02f);
                break;
        }
    }
}
```

实现 `OnDisable()` 恢复初始 Transform，避免反复翻页积累偏移。

- [ ] **步骤 5：实现预制体生成器**

生成器创建 1920×1080 根节点：

- 全屏 `home-background.png`；
- 半透明米白遮罩；
- 撕纸标题；
- `Pages` 下 6 个页面，每页包含场景插画、主文案和最多 3 条规则；
- `Next`、`Skip`、`Sound`、6 个页码点；
- 最后一页按钮文案为「我画好啦」，其余页为「下一步」；
- 所有文字使用 `C1UiFont.Load()`；
- 按钮和文字全部位于 5%～95% 横向、5%～95% 纵向安全区。

生成器先把 6 张 PNG 配置为单 Sprite、透明、无 Mipmap，再保存到 `Assets/Resources/C1UI/PaperGameLevelDrawingTutorial.prefab`。

- [ ] **步骤 6：批处理生成预制体并运行测试**

为生成器增加命令行入口：

```csharp
public static void GenerateFromCommandLine()
{
    GenerateTutorialPrefab();
    EditorApplication.Exit(0);
}
```

运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics \
  -projectPath "$(pwd)" \
  -executeMethod PaperGame.C1.Editor.PaperGameLevelDrawingTutorialPrefabGenerator.GenerateFromCommandLine \
  -logFile /private/tmp/papergame-level-tutorial-prefab.log
```

随后运行任务 2 的测试。预期：全部 PASS。

- [ ] **步骤 7：提交插画和预制体**

```bash
git add Assets/Art/UI/LevelDrawingTutorial Assets/Editor/PaperGameLevelDrawingTutorialPrefabGenerator.cs* Assets/Resources/C1UI/PaperGameLevelDrawingTutorial.prefab* Assets/Scripts/C1/UI/C1LevelDrawingTutorialPageAnimator.cs* Assets/Tests/EditMode/PaperGameLevelDrawingTutorialPrefabTests.cs*
git commit -m "feat(新手引导): 添加绘本式教程页面"
```

### 任务 3：生成中文语音并接入静音行为

**文件：**
- 创建：`Assets/Resources/C1TutorialAudio/page-01.aiff` 至 `page-06.aiff`
- 修改：`Assets/Tests/EditMode/C1LevelDrawingTutorialTests.cs`

- [ ] **步骤 1：补充失败的语音与静音测试**

```csharp
[Test]
public void ToggleMute_PersistsPreference()
{
    controller.Open(null);
    controller.ToggleMute();
    Assert.That(controller.IsMuted, Is.True);
    Assert.That(PlayerPrefs.GetInt(C1LevelDrawingTutorialController.MutedPreferenceKey), Is.EqualTo(1));
}

[TestCase(1)]
[TestCase(2)]
[TestCase(3)]
[TestCase(4)]
[TestCase(5)]
[TestCase(6)]
public void VoiceClip_ExistsForEveryPage(int page)
{
    Assert.That(Resources.Load<AudioClip>($"C1TutorialAudio/page-{page:00}"), Is.Not.Null);
}
```

- [ ] **步骤 2：运行测试验证失败**

预期：静音行为或语音资源断言失败。

- [ ] **步骤 3：生成 6 段中文语音**

使用系统普通话声音 `Tingting`，语速设为 170，分别朗读：

```text
准备一张横着放的白纸吧。
画一个空心圆圈，这里就是小英雄的家。
画几条平平的路，让小英雄向前跑。
留一点小空隙，让小英雄跳一跳。
画一面终点旗，涂什么颜色都可以。
把所有画画都放进白框里，就可以拍照啦。
```

输出为单声道 AIFF，文件名固定为 `page-01.aiff` 至 `page-06.aiff`。语速保持自然、温和，不添加背景音乐。

示例命令：

```bash
mkdir -p Assets/Resources/C1TutorialAudio
say -v Tingting -r 170 -o Assets/Resources/C1TutorialAudio/page-01.aiff --data-format=LEI16@22050 "准备一张横着放的白纸吧。"
```

其余 5 个文件使用相同参数和对应文案生成。

- [ ] **步骤 4：完成声音按钮行为**

`ToggleMute()` 在 `0/1` 间切换 `MutedPreferenceKey`，静音时立即停止播放；取消静音时重播当前页。`ReplayVoice()` 仅在未静音且当前页存在 AudioClip 时播放。

- [ ] **步骤 5：运行语音测试验证通过**

运行任务 1 的测试命令。预期：全部 PASS。

- [ ] **步骤 6：提交语音**

```bash
git add Assets/Resources/C1TutorialAudio Assets/Scripts/C1/UI/C1LevelDrawingTutorialController.cs Assets/Tests/EditMode/C1LevelDrawingTutorialTests.cs
git commit -m "feat(新手引导): 添加中文语音与静音控制"
```

### 任务 4：接入关卡创建和主动重看入口

**文件：**
- 修改：`Assets/Scripts/C1/UI/C1LevelSelection.cs`
- 修改：`Assets/Tests/EditMode/C1LevelSelectionTests.cs`

- [ ] **步骤 1：编写失败的入口测试**

新增测试：

```csharp
[Test]
public void CreateLevel_WhenTutorialIsUnseen_ShowsTutorialFirst()
{
    PlayerPrefs.DeleteKey(C1LevelDrawingTutorialController.SeenPreferenceKey);
    selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot));
    GameObject.Find("拍照上传新关卡").GetComponent<Button>().onClick.Invoke();
    Assert.That(selection.TutorialVisible, Is.True);
}

[Test]
public void HelpButton_AlwaysOpensTutorialWithoutStartingCamera()
{
    PlayerPrefs.SetInt(C1LevelDrawingTutorialController.SeenPreferenceKey, 1);
    selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot));
    GameObject.Find("怎么画？").GetComponent<Button>().onClick.Invoke();
    Assert.That(selection.TutorialVisible, Is.True);
}
```

在 `TearDown` 删除教程 Seen 和 Muted 键，避免污染其他测试。

- [ ] **步骤 2：运行测试验证失败**

运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics \
  -projectPath "$(pwd)" -runTests -testPlatform EditMode \
  -testFilter PaperGame.C1.Tests.C1LevelSelectionTests \
  -testResults /private/tmp/papergame-level-selection-tutorial.xml \
  -logFile /private/tmp/papergame-level-selection-tutorial.log
```

预期：FAIL，缺少 `TutorialVisible` 和「怎么画？」按钮。

- [ ] **步骤 3：接入教程**

把创建按钮回调从 `OpenCamera` 改为 `RequestCreateLevel`，并新增：

```csharp
public bool TutorialVisible => tutorial != null && tutorial.IsOpen;

private void RequestCreateLevel()
{
    if (busy) return;
    if (C1LevelDrawingTutorialController.HasSeen) OpenCamera();
    else ShowTutorial(true);
}

private void ShowTutorial(bool openCameraAfter)
{
    if (tutorial == null)
    {
        var prefab = Resources.Load<GameObject>("C1UI/PaperGameLevelDrawingTutorial");
        if (prefab == null)
        {
            Debug.LogWarning("关卡绘画引导预制体缺失。", this);
            if (openCameraAfter) OpenCamera();
            return;
        }
        var instance = Instantiate(prefab, transform);
        tutorial = instance.GetComponent<C1LevelDrawingTutorialController>();
    }
    tutorial.Open(openCameraAfter ? (Action)OpenCamera : null);
}
```

创建顶部「怎么画？」按钮，调用 `ShowTutorial(false)`。`createImmediately` 改为调用 `RequestCreateLevel()`，确保首页首次拍摄也进入教程。

- [ ] **步骤 4：运行入口测试和回归测试**

运行任务 4 的测试，再运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics \
  -projectPath "$(pwd)" -runTests -testPlatform EditMode \
  -testFilter PaperGame.C1.Tests.HomeBootstrapTests \
  -testResults /private/tmp/papergame-home-tutorial.xml \
  -logFile /private/tmp/papergame-home-tutorial.log
```

预期：全部 PASS。

- [ ] **步骤 5：提交入口接入**

```bash
git add Assets/Scripts/C1/UI/C1LevelSelection.cs Assets/Tests/EditMode/C1LevelSelectionTests.cs
git commit -m "feat(关卡创建): 首次拍照前展示绘画引导"
```

### 任务 5：全量验证与视觉验收

**文件：**
- 创建：`Docs/ui/level-drawing-tutorial-preview.png`
- 修改：仅修正本功能验证中发现的问题。

- [ ] **步骤 1：运行静态检查**

```bash
git diff --check
git status --short
```

预期：无空白错误；工作区中原有角色生成改动仍保持原样，教程改动范围清晰。

- [ ] **步骤 2：运行全量 EditMode 测试**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics \
  -projectPath "$(pwd)" -runTests -testPlatform EditMode \
  -testResults /private/tmp/papergame-level-tutorial-all.xml \
  -logFile /private/tmp/papergame-level-tutorial-all.log
```

预期：所有 EditMode 测试通过，XML 中 `failed="0"`。

- [ ] **步骤 3：渲染最终预览**

在编辑器生成器中提供预览渲染入口，把 1920×1080 的教程第 5 页渲染到 `Docs/ui/level-drawing-tutorial-preview.png`。检查：

- 首页冒险绘本风格一致；
- 旗帜不暗示固定颜色；
- 文字与按钮在安全区内；
- 页面只出现一个主场景和最多 3 条规则；
- 无 Emoji、系统图标或规则的扁平卡片。

- [ ] **步骤 4：检查 6 页资源与语音**

确认 6 张插画、6 段语音、6 个页码点和教程预制体均进入 Assets，且不存在空引用。

- [ ] **步骤 5：提交预览与最终修正**

```bash
git add Docs/ui/level-drawing-tutorial-preview.png Assets/Art/UI/LevelDrawingTutorial Assets/Resources/C1TutorialAudio Assets/Resources/C1UI/PaperGameLevelDrawingTutorial.prefab Assets/Resources/C1UI/PaperGameLevelDrawingTutorial.prefab.meta Assets/Editor/PaperGameLevelDrawingTutorialPrefabGenerator.cs Assets/Editor/PaperGameLevelDrawingTutorialPrefabGenerator.cs.meta Assets/Scripts/C1/UI/C1LevelDrawingTutorialController.cs Assets/Scripts/C1/UI/C1LevelDrawingTutorialPageAnimator.cs Assets/Tests/EditMode/C1LevelDrawingTutorialTests.cs Assets/Tests/EditMode/PaperGameLevelDrawingTutorialPrefabTests.cs Assets/Scripts/C1/UI/C1LevelSelection.cs Assets/Tests/EditMode/C1LevelSelectionTests.cs
git commit -m "test(新手引导): 完成绘画教程验收"
```
