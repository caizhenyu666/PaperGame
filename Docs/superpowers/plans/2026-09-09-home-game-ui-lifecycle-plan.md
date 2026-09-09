# 首页与关卡 UI 生命周期实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 将首页与游戏态拆分为 `Home`、`Game` 两个场景，使角色创建页按需实例化，并让首页大底图以等比 Cover 方式适配任意分辨率。

**架构：** `HomeBootstrap` 只创建首页 Canvas 和首页预制体；`C1CharacterSelection` 在点击创建主角时才构造角色页。 `C1GameSession` 跨场景保存待运行关卡 ID，`Game` 内的 `C1GameBootstrap` 只据此生成 JSON 驱动的关卡。首页背景使用独立 Cover 适配组件，前景 UI 使用 CanvasScaler 和归一化锚点。

**技术栈：** Unity 2022.3、C#、uGUI、`UnityEngine.SceneManagement`、Unity Test Framework（NUnit EditMode）、Resources、WebGL。

---

## 文件结构

- 新建 `Assets/Scripts/C1/C1GameSession.cs`：跨场景的关卡 ID 与场景名常量。
- 新建 `Assets/Scripts/C1/HomeBootstrap.cs`：首页 Canvas、首页预制体和 EventSystem 的唯一装配点。
- 新建 `Assets/Scripts/C1/UI/C1CoverBackground.cs`：大底图 Cover 尺寸计算与响应父节点尺寸变化。
- 新建 `Assets/Editor/PaperGameSceneGenerator.cs`：可重复生成并注册 `Home.unity`、`Game.unity`。
- 修改 `Assets/Scripts/C1/UI/C1CharacterSelection.cs`：改成创建后才构造角色页；移除玩家暂停／启动职责。
- 修改 `Assets/Scripts/C1/C1GameBootstrap.cs`、`Assets/Scripts/C1/C1LevelLoader.cs`：限制为 `Game` 构建与回首页。
- 修改 `Assets/Editor/PaperGameHomePrefabGenerator.cs`：为背景挂载 Cover，前景限制中央安全区。
- 修改 `Assets/Scenes/Home.unity`、`Assets/Scenes/Game.unity`、`ProjectSettings/EditorBuildSettings.asset`、`ProjectSettings/ProjectSettings.asset`：注册两个场景并令 `Home` 成为默认。
- 新建或修改 `Assets/Tests/EditMode/*Tests.cs`：覆盖会话、首页懒创建、Cover 和场景资产；迁移旧 UI／Bootstrap 断言。

### 任务 1：关卡会话与可测试路由

**文件：**

- 创建：`Assets/Scripts/C1/C1GameSession.cs`
- 创建：`Assets/Tests/EditMode/C1GameSessionTests.cs`
- 修改：`Assets/Tests/EditMode/PaperGameUiFlowTests.cs`

- [ ] **步骤 1：编写失败测试。**

```csharp
[Test]
public void ConsumePendingLevel_UsesSelectedPathThenClearsIt()
{
    var session = C1GameSession.CreateForTests();
    session.SetPendingLevel("C1Levels/level2");
    Assert.That(session.ConsumePendingLevel(), Is.EqualTo("C1Levels/level2"));
    Assert.That(session.ConsumePendingLevel(), Is.EqualTo("C1Levels/level1"));
}

[Test]
public void StartPlay_RequestsGameSceneForSelectedLevel()
{
    flow.StartPlay("C1Levels/level2");
    Assert.That(flow.NextSceneName, Is.EqualTo(C1GameSession.GameSceneName));
    Assert.That(flow.PendingLevelResourcePath, Is.EqualTo("C1Levels/level2"));
}
```

- [ ] **步骤 2：运行并确认失败。**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$(pwd)" -runTests -testPlatform EditMode -testFilter "PaperGame.C1.Tests.C1GameSessionTests|PaperGame.C1.Tests.PaperGameUiFlowTests" -testResults /private/tmp/papergame-session-red.xml -logFile /private/tmp/papergame-session-red.log
```

预期：测试因 `C1GameSession`、`StartPlay(string)` 和路由属性不存在而失败。

- [ ] **步骤 3：实现最小会话和路由记录。**

```csharp
public sealed class C1GameSession : MonoBehaviour
{
    public const string HomeSceneName = "Home";
    public const string GameSceneName = "Game";
    public const string DefaultLevelResourcePath = "C1Levels/level1";
    public static C1GameSession Instance { get; private set; }
    private string pendingLevelResourcePath;

    public static C1GameSession CreateForTests()
    {
        return new GameObject("Game Session Test").AddComponent<C1GameSession>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPendingLevel(string path) => pendingLevelResourcePath =
        string.IsNullOrWhiteSpace(path) ? DefaultLevelResourcePath : path;

    public string ConsumePendingLevel()
    {
        var path = string.IsNullOrWhiteSpace(pendingLevelResourcePath)
            ? DefaultLevelResourcePath : pendingLevelResourcePath;
        pendingLevelResourcePath = null;
        return path;
    }
}
```

让 `PaperGameUiFlow.StartPlay(string)` 仅记录 `NextSceneName` 和路径；生产 Bootstrap 再调用 `SceneManager.LoadScene`，从而保持 EditMode 测试无场景依赖。

- [ ] **步骤 4：运行步骤 2 命令确认通过。**

预期：筛选测试全部通过。

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/C1GameSession.cs Assets/Scripts/C1/C1GameSession.cs.meta Assets/Tests/EditMode/C1GameSessionTests.cs Assets/Tests/EditMode/C1GameSessionTests.cs.meta Assets/Tests/EditMode/PaperGameUiFlowTests.cs
git commit -m "feat(场景): 添加关卡跨场景会话"
```

### 任务 2：首页与角色创建页的按需生命周期

**文件：**

- 创建：`Assets/Scripts/C1/HomeBootstrap.cs`
- 修改：`Assets/Scripts/C1/UI/C1CharacterSelection.cs`
- 创建：`Assets/Tests/EditMode/HomeBootstrapTests.cs`

- [ ] **步骤 1：编写失败测试。**

```csharp
[Test]
public void BuildHome_CreatesHomeWithoutCharacterScreen()
{
    bootstrap.BuildHome();
    Assert.That(bootstrap.HomeScreen, Is.Not.Null);
    Assert.That(bootstrap.CharacterScreen, Is.Null);
}

[Test]
public void ShowCharacterCreation_HidesHomeAndCreatesScreenOnDemand()
{
    bootstrap.BuildHome();
    bootstrap.ShowCharacterCreation();
    Assert.That(bootstrap.HomeScreen.activeSelf, Is.False);
    Assert.That(bootstrap.CharacterScreen, Is.Not.Null);
}

[Test]
public void ReturnHome_DestroysCharacterScreenAndRestoresHome()
{
    bootstrap.BuildHome(); bootstrap.ShowCharacterCreation(); bootstrap.ReturnHome();
    Assert.That(bootstrap.CharacterScreen, Is.Null);
    Assert.That(bootstrap.HomeScreen.activeSelf, Is.True);
}
```

- [ ] **步骤 2：运行并确认失败。**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$(pwd)" -runTests -testPlatform EditMode -testFilter PaperGame.C1.Tests.HomeBootstrapTests -testResults /private/tmp/papergame-home-red.xml -logFile /private/tmp/papergame-home-red.log
```

预期：`HomeBootstrap` 和其生命周期 API 尚不存在。

- [ ] **步骤 3：实现最小首页生命周期。**

```csharp
public void ShowCharacterCreation()
{
    if (characterScreen != null) return;
    homeScreen.SetActive(false);
    characterScreen = characterSelection.CreateScreen(canvas, ReturnHome);
}

public void StartGame(string resourcePath = C1GameSession.DefaultLevelResourcePath)
{
    C1GameSession.Instance.SetPendingLevel(resourcePath);
    SceneManager.LoadScene(C1GameSession.GameSceneName);
}

public void ReturnHome()
{
    if (characterScreen != null) Destroy(characterScreen);
    characterScreen = null;
    homeScreen.SetActive(true);
}
```

`C1CharacterSelection` 保留角色库、拍照、上传、预览和选择逻辑，但不再接收玩家、不再暂停玩家、不在 `Configure` 中创建 UI。返回按钮调用传入的 `ReturnHome` 回调；首页的「开始游戏」按钮绑定 `StartGame`。

- [ ] **步骤 4：运行步骤 2 命令确认通过。**

预期：首页只创建首页；角色页在首次点击后存在；返回后被销毁。

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/HomeBootstrap.cs Assets/Scripts/C1/HomeBootstrap.cs.meta Assets/Scripts/C1/UI/C1CharacterSelection.cs Assets/Tests/EditMode/HomeBootstrapTests.cs Assets/Tests/EditMode/HomeBootstrapTests.cs.meta
git commit -m "feat(UI): 按需创建角色界面"
```

### 任务 3：首页背景 Cover 适配

**文件：**

- 创建：`Assets/Scripts/C1/UI/C1CoverBackground.cs`
- 创建：`Assets/Tests/EditMode/C1CoverBackgroundTests.cs`
- 修改：`Assets/Editor/PaperGameHomePrefabGenerator.cs`
- 修改：`Assets/Tests/EditMode/PaperGameHomePrefabTests.cs`

- [ ] **步骤 1：编写失败测试。**

```csharp
[TestCase(1672f, 941f, 1280f, 720f, 1280f, 720f)]
[TestCase(1672f, 941f, 2532f, 1170f, 2532f, 1424.3f)]
[TestCase(1672f, 941f, 810f, 1440f, 2559.4f, 1440f)]
public void CalculateSize_CoversParentWithoutChangingAspect(
    float iw, float ih, float pw, float ph, float ew, float eh)
{
    var size = C1CoverBackground.CalculateSize(new Vector2(iw, ih), new Vector2(pw, ph));
    Assert.That(size.x, Is.EqualTo(ew).Within(0.2f));
    Assert.That(size.y, Is.EqualTo(eh).Within(0.2f));
}
```

- [ ] **步骤 2：运行并确认失败。**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$(pwd)" -runTests -testPlatform EditMode -testFilter "PaperGame.C1.Tests.C1CoverBackgroundTests|PaperGame.C1.Tests.PaperGameHomePrefabTests" -testResults /private/tmp/papergame-cover-red.xml -logFile /private/tmp/papergame-cover-red.log
```

预期：失败，`C1CoverBackground.CalculateSize` 不存在。

- [ ] **步骤 3：实现 Cover 并更新预制体生成器。**

```csharp
public static Vector2 CalculateSize(Vector2 image, Vector2 parent)
{
    if (image.x <= 0f || image.y <= 0f || parent.x <= 0f || parent.y <= 0f)
        return Vector2.zero;
    return image * Mathf.Max(parent.x / image.x, parent.y / image.y);
}
```

背景改为中心锚点、中心 pivot 和 `sizeDelta = CalculateSize(...)`，不再四角拉伸。生成器为 `Background` 挂载组件；标题、按钮、教程卡保持在横向 `0.20–0.80`、纵向 `0.16–0.90` 的中央安全区。预制体测试断言背景有 Cover 组件且所有交互按钮仍保留 Button。

- [ ] **步骤 4：运行步骤 2 命令确认通过。**

预期：16:9、超宽、竖屏均覆盖全屏并保持宽高比。

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/UI/C1CoverBackground.cs Assets/Scripts/C1/UI/C1CoverBackground.cs.meta Assets/Tests/EditMode/C1CoverBackgroundTests.cs Assets/Tests/EditMode/C1CoverBackgroundTests.cs.meta Assets/Editor/PaperGameHomePrefabGenerator.cs Assets/Tests/EditMode/PaperGameHomePrefabTests.cs Assets/Resources/C1UI/PaperGameHome.prefab
git commit -m "feat(UI): 首页底图使用等比 Cover 适配"
```

### 任务 4：将关卡 Bootstrap 限制在 `Game`

**文件：**

- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`
- 修改：`Assets/Scripts/C1/C1LevelLoader.cs`
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`

- [ ] **步骤 1：编写失败测试。**

```csharp
[Test]
public void BuildSelectedLevel_UsesSessionLevel()
{
    C1GameSession.Instance.SetPendingLevel("C1Levels/level1");
    Assert.That(bootstrap.BuildSelectedLevel(), Is.True);
    Assert.That(bootstrap.Player, Is.Not.Null);
}

[Test]
public void ReturnHome_ClearsGeneratedLevelBeforeRequestingHomeScene()
{
    bootstrap.Build(C1LevelLoader.LoadDefault());
    bootstrap.ReturnHome();
    Assert.That(bootstrap.Player, Is.Null);
    Assert.That(bootstrap.RequestedSceneName, Is.EqualTo(C1GameSession.HomeSceneName));
}
```

- [ ] **步骤 2：运行并确认失败。**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$(pwd)" -runTests -testPlatform EditMode -testFilter PaperGame.C1.Tests.C1GameBootstrapTests -testResults /private/tmp/papergame-game-red.xml -logFile /private/tmp/papergame-game-red.log
```

预期：失败，`BuildSelectedLevel`、`ReturnHome` 和路由状态尚不存在，旧首页耦合断言失效。

- [ ] **步骤 3：实现最小 Game 入口和统一返回。**

```csharp
private void Start() => BuildSelectedLevel();

public bool BuildSelectedLevel()
{
    return Build(C1LevelLoader.Load(C1GameSession.Instance.ConsumePendingLevel()));
}

public void ReturnHome()
{
    ClearGeneratedObjects();
    requestedSceneName = C1GameSession.HomeSceneName;
    SceneManager.LoadScene(requestedSceneName);
}
```

新增 `C1LevelLoader.Load(string)`，`LoadDefault()` 只包装默认路径。删除 `CreatePictureBookHome` 及其角色页装配；完成、失败和主动返回都调用 `ReturnHome`，不在当前场景重新 `Build`。移除会在 `Home` 自动创建关卡 Bootstrap 的 `RuntimeInitializeOnLoadMethod`。

- [ ] **步骤 4：运行步骤 2 命令确认通过。**

预期：关卡物理、相机和 HUD 断言仍通过；首页断言迁移到任务 2。

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/C1GameBootstrap.cs Assets/Scripts/C1/C1LevelLoader.cs Assets/Tests/EditMode/C1GameBootstrapTests.cs
git commit -m "refactor(关卡): 将运行态限定到 Game 场景"
```

### 任务 5：生成、注册并验证双场景

**文件：**

- 创建：`Assets/Editor/PaperGameSceneGenerator.cs`
- 创建：`Assets/Scenes/Home.unity`、`Assets/Scenes/Game.unity`
- 创建：`Assets/Tests/EditMode/SceneSetupTests.cs`
- 修改：`ProjectSettings/EditorBuildSettings.asset`、`ProjectSettings/ProjectSettings.asset`

- [ ] **步骤 1：编写失败的场景资产测试。**

```csharp
[Test]
public void BuildSettings_RegistersHomeThenGame()
{
    Assert.That(EditorBuildSettings.scenes.Select(x => x.path), Is.EqualTo(new[]
    {
        "Assets/Scenes/Home.unity", "Assets/Scenes/Game.unity"
    }));
}

[Test]
public void GeneratedScenes_HaveExpectedBootstrap()
{
    AssertSceneHasComponent("Assets/Scenes/Home.unity", typeof(HomeBootstrap));
    AssertSceneHasComponent("Assets/Scenes/Game.unity", typeof(C1GameBootstrap));
}
```

- [ ] **步骤 2：运行并确认失败。**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$(pwd)" -runTests -testPlatform EditMode -testFilter PaperGame.C1.Tests.SceneSetupTests -testResults /private/tmp/papergame-scenes-red.xml -logFile /private/tmp/papergame-scenes-red.log
```

预期：两个场景尚未注册或缺少预期 Bootstrap。

- [ ] **步骤 3：实现幂等生成器并生成场景。**

```csharp
[MenuItem("PaperGame/Scenes/Generate Home And Game")]
public static void Generate()
{
    CreateScene("Assets/Scenes/Home.unity", "Home Bootstrap", typeof(HomeBootstrap));
    CreateScene("Assets/Scenes/Game.unity", "Game Bootstrap", typeof(C1GameBootstrap));
    EditorBuildSettings.scenes = new[]
    {
        new EditorBuildSettingsScene("Assets/Scenes/Home.unity", true),
        new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
    };
}
```

生成前保存当前场景，每个目标场景仅创建一个根对象及对应 Bootstrap，生成后恢复先前场景。将 `templateDefaultScene` 设为 `Home.unity`。旧 `SampleScene.unity` 只停止注册，不删除。

- [ ] **步骤 4：运行步骤 2 命令确认通过。**

预期：Build Settings 顺序为 `Home`、`Game`，场景各自仅有指定 Bootstrap。

- [ ] **步骤 5：提交。**

```bash
git add Assets/Editor/PaperGameSceneGenerator.cs Assets/Editor/PaperGameSceneGenerator.cs.meta Assets/Scenes/Home.unity Assets/Scenes/Home.unity.meta Assets/Scenes/Game.unity Assets/Scenes/Game.unity.meta Assets/Tests/EditMode/SceneSetupTests.cs Assets/Tests/EditMode/SceneSetupTests.cs.meta ProjectSettings/EditorBuildSettings.asset ProjectSettings/ProjectSettings.asset
git commit -m "feat(场景): 拆分首页与通用关卡场景"
```

### 任务 6：完整回归与 WebGL 验证

**文件：**仅限任务 1–5 中测试确实暴露的问题文件。

- [ ] **步骤 1：运行全量 EditMode 测试。**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$(pwd)" -runTests -testPlatform EditMode -testResults /private/tmp/papergame-home-game-editmode.xml -logFile /private/tmp/papergame-home-game-editmode.log
```

预期：结果 XML 的 `<test-run>` 含 `result="Passed"`。

- [ ] **步骤 2：构建 WebGL。**

```bash
C1_WEBGL_OUTPUT="$(pwd)/Builds/C1WebGL-HomeGame" /Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$(pwd)" -executeMethod PaperGame.C1.Editor.C1WebGLBuilder.BuildFromCommandLine -logFile /private/tmp/papergame-home-game-webgl.log
```

预期：退出码为 `0`，日志不含 C# 编译错误，输出目录有构建产物。

- [ ] **步骤 3：在 Unity Editor 手动验收。**

1. 从 `Home` Play Mode 启动，首屏只有首页，不存在角色创建页、玩家、关卡背景或移动控制。
2. 点击「创建主角」后首页隐藏、角色页首次出现；返回后角色页销毁。
3. 在 `2532 × 1170`、`1280 × 720`、`810 × 1440` Game 视图下，底图无留边、无拉伸，按钮处于中央安全区。
4. 点击「开始游戏」进入 `Game` 并创建默认 JSON 关卡；完成、失败和主动返回均回到 `Home`。

- [ ] **步骤 4：只提交验证中必要的修复。**

```bash
git add <任务 1–5 中实际修改的文件>
git commit -m "fix(UI): 修正首页与关卡切换回归"
```

若不需要修复，不创建空提交。

## 计划自检

- 双场景、通用 Game、角色页延迟创建、回首页、Cover 底图与安全区均有独立任务。
- 每项生产变更均先定义失败测试，再给出最小实现、精确命令、通过预期和提交点。
- 后续任务一致使用 `C1GameSession`、`HomeBootstrap`、`C1CoverBackground`、`BuildSelectedLevel`、`ReturnHome` 这些名称。
- 不变更关卡 JSON、角色服务协议或首页美术资源；不删除旧 `SampleScene`。
