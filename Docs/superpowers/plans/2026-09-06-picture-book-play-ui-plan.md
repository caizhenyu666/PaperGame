# 儿童绘本风试玩 UI 实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 `superpowers:subagent-driven-development`（推荐）或 `superpowers:executing-plans` 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 为手机横屏试玩流程提供儿童绘本风的首页、引导、游戏 HUD 与成功/重试预制体，并与现有 C1 游戏事件连通。

**架构：** 新增独立的 UI 主题、流程控制器和页面组件；编辑器生成器将导入的 PNG 元素配置为 Sprite 并创建可复用预制体。`C1GameBootstrap` 只在构建成功后创建和绑定 `PaperGameUiFlow`，继续保留角色、物理、旗帜和掉落逻辑的所有权。

**技术栈：** Unity 2022.3 LTS、C#、uGUI（`UnityEngine.UI`）、NUnit EditMode、PNG Sprite、Unity `AssetDatabase`/`PrefabUtility`。

---

## 文件结构

- 创建：`Assets/Art/UI/PictureBook/` — 由图像生成后导入的纸张、插画、按钮和图标 PNG。
- 创建：`Assets/UI/PaperGameUiTheme.cs` — 主题 token 的 ScriptableObject 类型。
- 创建：`Assets/UI/PaperGameUiTheme.asset` — 默认绘本主题实例。
- 创建：`Assets/Scripts/C1/UI/PaperGameUiFlow.cs` — 页面状态、首次引导偏好及游戏事件绑定。
- 创建：`Assets/Scripts/C1/UI/PaperGameHome.cs` — 首页按钮与“即将开放”提示。
- 创建：`Assets/Scripts/C1/UI/PaperGameTutorial.cs` — 三页引导的切换逻辑。
- 创建：`Assets/Scripts/C1/UI/PaperGameHud.cs` — 移动、跳跃、暂停 UI 的装配边界。
- 创建：`Assets/Scripts/C1/UI/PaperGameResultPanel.cs` — 成功/重试文案和重新开始回调。
- 创建：`Assets/Editor/PaperGameUiPrefabGenerator.cs` — 主题资产、Canvas 页面和预制体自动生成菜单。
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs` — 替换临时 HUD 创建并绑定 UI Flow。
- 创建：`Assets/Tests/EditMode/PaperGameUiFlowTests.cs` — UI 状态与首次引导测试。
- 创建：`Assets/Tests/EditMode/PaperGameUiPrefabGeneratorTests.cs` — 主题和预制体结构测试。

### 任务 1：生成并导入绘本 UI 元素

**文件：**
- 创建：`Assets/Art/UI/PictureBook/background-paper.png`
- 创建：`Assets/Art/UI/PictureBook/illustration-clouds.png`
- 创建：`Assets/Art/UI/PictureBook/illustration-grass.png`
- 创建：`Assets/Art/UI/PictureBook/illustration-flag.png`
- 创建：`Assets/Art/UI/PictureBook/decoration-confetti.png`
- 创建：`Assets/Art/UI/PictureBook/button-primary.png`
- 创建：`Assets/Art/UI/PictureBook/button-secondary.png`
- 创建：`Assets/Art/UI/PictureBook/button-direction.png`
- 创建：`Assets/Art/UI/PictureBook/button-jump.png`
- 创建：`Assets/Art/UI/PictureBook/button-pause.png`
- 创建：`Assets/Art/UI/PictureBook/icon-left.png`
- 创建：`Assets/Art/UI/PictureBook/icon-right.png`
- 创建：`Assets/Art/UI/PictureBook/icon-jump.png`
- 创建：`Assets/Art/UI/PictureBook/icon-pause.png`

- [ ] **步骤 1：生成不可替代的绘本元素。**

使用图像生成工具分别生成纸张背景、云/草地、红旗、彩纸、五种无文字按钮底图和四个无文字图标。所有元素不得包含文字、水印、设备外框或人物肖像；按钮和图标必须单独输出为透明 PNG。

- [ ] **步骤 2：将最终资源导入到固定目录。**

将选定 PNG 复制为上述精确文件名。背景与场景插画保留不透明底；按钮、图标和装饰保留 alpha 通道。

- [ ] **步骤 3：在 Unity 中配置导入器。**

在 `PaperGameUiPrefabGenerator.ConfigureTextureImporter` 中，确保所有资源使用 Sprite (2D and UI)；为 `background-paper` 使用单 Sprite，其他资源同样使用单 Sprite。为可拉伸的五个按钮设置 `spriteBorder = new Vector4(24f, 24f, 24f, 24f)`，并将 `mipmapEnabled` 设为 `false`。

```csharp
private static void ConfigureTextureImporter(string assetPath, bool isNineSliced)
{
    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
    importer.textureType = TextureImporterType.Sprite;
    importer.spriteImportMode = SpriteImportMode.Single;
    importer.mipmapEnabled = false;
    if (isNineSliced)
    {
        importer.spriteBorder = new Vector4(24f, 24f, 24f, 24f);
    }
    importer.SaveAndReimport();
}
```

- [ ] **步骤 4：人工检查透明边缘与九宫格。**

在 Sprite Editor 预览每个按钮；拉伸到 `420×124` 时，边角必须不变形，文字区域保持干净。

- [ ] **步骤 5：提交资源。**

```bash
git add Assets/Art/UI/PictureBook
git commit -m "feat(UI): 添加儿童绘本风界面元素"
```

### 任务 2：建立主题与无场景依赖的页面状态

**文件：**
- 创建：`Assets/UI/PaperGameUiTheme.cs`
- 创建：`Assets/UI/PaperGameUiTheme.asset`
- 创建：`Assets/Scripts/C1/UI/PaperGameUiFlow.cs`
- 测试：`Assets/Tests/EditMode/PaperGameUiFlowTests.cs`

- [ ] **步骤 1：编写失败的状态流测试。**

```csharp
[Test]
public void StartPlay_WhenTutorialIsUnseen_ChangesToTutorial()
{
    var flow = new GameObject().AddComponent<PaperGameUiFlow>();
    flow.ConfigureForTests(tutorialSeen: false);

    flow.StartPlay();

    Assert.That(flow.CurrentState, Is.EqualTo(PaperGameUiState.Tutorial));
}

[Test]
public void CompleteTutorial_RecordsPreferenceAndChangesToPlaying()
{
    var flow = new GameObject().AddComponent<PaperGameUiFlow>();
    flow.ConfigureForTests(tutorialSeen: false);

    flow.CompleteTutorial();

    Assert.That(flow.CurrentState, Is.EqualTo(PaperGameUiState.Playing));
    Assert.That(flow.HasSeenTutorial, Is.True);
}
```

- [ ] **步骤 2：运行测试，确认失败。**

运行：`/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath "$(pwd)" -runTests -testPlatform EditMode -testFilter PaperGameUiFlowTests -testResults "$(pwd)/Temp/ui-flow-results.xml" -logFile "$(pwd)/Temp/ui-flow.log"`

预期：编译失败，因为 `PaperGameUiFlow` 和 `PaperGameUiState` 尚不存在。

- [ ] **步骤 3：实现主题类型和状态流。**

```csharp
public enum PaperGameUiState { Home, Tutorial, Playing, Completed, Retry }

public sealed class PaperGameUiFlow : MonoBehaviour
{
    public PaperGameUiState CurrentState { get; private set; }
    public bool HasSeenTutorial { get; private set; }

    public void StartPlay() => SetState(HasSeenTutorial ? PaperGameUiState.Playing : PaperGameUiState.Tutorial);
    public void CompleteTutorial() { HasSeenTutorial = true; SetState(PaperGameUiState.Playing); }
    public void ShowCompleted() => SetState(PaperGameUiState.Completed);
    public void ShowRetry() => SetState(PaperGameUiState.Retry);
    private void SetState(PaperGameUiState state) { CurrentState = state; }
}
```

`PaperGameUiTheme` 至少包含 `PrimaryColor`、`SecondaryColor`、`JumpColor`、`TextColor`、`ReferenceResolution`、`MinimumTouchSize` 与资源 Sprite 引用。生成器创建的默认资产分别设置为 `#E85A3F`、`#F4BD37`、`#F1C744`、`#242A2E`、`1280×720`、`96`。

- [ ] **步骤 4：重跑状态测试。**

运行相同命令。

预期：`PaperGameUiFlowTests` 全部通过。

- [ ] **步骤 5：提交。**

```bash
git add Assets/UI Assets/Scripts/C1/UI/PaperGameUiFlow.cs Assets/Tests/EditMode/PaperGameUiFlowTests.cs
git commit -m "feat(UI): 添加试玩页面状态流"
```

### 任务 3：生成四类页面预制体

**文件：**
- 创建：`Assets/Editor/PaperGameUiPrefabGenerator.cs`
- 创建：`Assets/Resources/C1UI/PaperGameHome.prefab`
- 创建：`Assets/Resources/C1UI/PaperGameTutorial.prefab`
- 创建：`Assets/Resources/C1UI/PaperGameHud.prefab`
- 创建：`Assets/Resources/C1UI/PaperGameResultPanel.prefab`
- 创建：`Assets/Scripts/C1/UI/PaperGameHome.cs`
- 创建：`Assets/Scripts/C1/UI/PaperGameTutorial.cs`
- 创建：`Assets/Scripts/C1/UI/PaperGameHud.cs`
- 创建：`Assets/Scripts/C1/UI/PaperGameResultPanel.cs`
- 测试：`Assets/Tests/EditMode/PaperGameUiPrefabGeneratorTests.cs`

- [ ] **步骤 1：编写失败的预制体结构测试。**

```csharp
[TestCase("Assets/Resources/C1UI/PaperGameHome.prefab", "Start Play")]
[TestCase("Assets/Resources/C1UI/PaperGameTutorial.prefab", "Tutorial Card 1")]
[TestCase("Assets/Resources/C1UI/PaperGameHud.prefab", "Jump")]
[TestCase("Assets/Resources/C1UI/PaperGameResultPanel.prefab", "Restart")]
public void GeneratedPrefab_ContainsRequiredChild(string path, string childName)
{
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    Assert.That(prefab, Is.Not.Null);
    Assert.That(prefab.transform.Find(childName), Is.Not.Null);
}
```

- [ ] **步骤 2：运行测试，确认失败。**

运行：`/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath "$(pwd)" -runTests -testPlatform EditMode -testFilter PaperGameUiPrefabGeneratorTests -testResults "$(pwd)/Temp/ui-prefab-results.xml" -logFile "$(pwd)/Temp/ui-prefab.log"`

预期：失败，因为四个预制体尚未生成。

- [ ] **步骤 3：实现生成器和页面最小行为。**

生成器提供 `PaperGame/UI/Generate Picture Book Prefabs` 菜单，创建固定层级：

```text
PaperGameHome
├── Background
├── Start Play
├── Capture Coming Soon
└── Settings

PaperGameTutorial
├── Tutorial Card 1
├── Tutorial Card 2
├── Tutorial Card 3
└── Start Game

PaperGameHud
├── Move Left
├── Move Right
├── Jump
└── Pause

PaperGameResultPanel
├── Decoration
├── Title
├── Hint
└── Restart
```

所有生成的根节点都包含 `RectTransform` 和 `CanvasGroup`；Interactive `Image` 的 `type` 为 `Image.Type.Sliced`，并用主题的 `MinimumTouchSize` 保证触控尺寸。`PaperGameTutorial` 仅显示当前卡，`PaperGameResultPanel.ShowCompleted()` 设置“你到终点啦！”/“再玩一次”，`ShowRetry()` 设置“再试一次吧”/“跳高一点，越过障碍”。

- [ ] **步骤 4：从 Unity 菜单或批处理方法生成预制体。**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit \
  -projectPath "$(pwd)" \
  -executeMethod PaperGame.C1.Editor.PaperGameUiPrefabGenerator.GenerateAll \
  -logFile "$(pwd)/Temp/ui-prefab-generate.log"
```

预期：退出码为 `0`，四个 `.prefab` 与 `PaperGameUiTheme.asset` 存在。

- [ ] **步骤 5：重跑预制体测试。**

运行步骤 2 的命令。

预期：`PaperGameUiPrefabGeneratorTests` 全部通过。

- [ ] **步骤 6：提交。**

```bash
git add Assets/Editor/PaperGameUiPrefabGenerator.cs Assets/Resources/C1UI Assets/Scripts/C1/UI Assets/Tests/EditMode/PaperGameUiPrefabGeneratorTests.cs
git commit -m "feat(UI): 生成绘本试玩界面预制体"
```

### 任务 4：替换临时 HUD 并接入游戏事件

**文件：**
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`
- 修改：`Assets/Scripts/C1/C1MobileOrientationHint.cs`
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`

- [ ] **步骤 1：为 Bootstrap 编写失败的集成测试。**

```csharp
[Test]
public void Build_CreatesPictureBookUiAndHidesDebugTuningPanel()
{
    bootstrap.Build(C1LevelLoader.LoadDefault());

    Assert.That(bootstrap.HudCanvas.GetComponentInChildren<PaperGameUiFlow>(), Is.Not.Null);
    Assert.That(bootstrap.HudCanvas.GetComponentInChildren<C1PlaytestTuningPanel>(true).gameObject.activeSelf, Is.False);
    Assert.That(GameObject.Find("Capture Photo"), Is.Null);
}

[Test]
public void GoalReached_ChangesUiToCompleted()
{
    bootstrap.Build(C1LevelLoader.LoadDefault());
    bootstrap.Goal.GetComponent<C1GoalFlag>().SendMessage("OnTriggerEnter2D", bootstrap.Player.GetComponent<Collider2D>());

    Assert.That(bootstrap.HudCanvas.GetComponentInChildren<PaperGameUiFlow>().CurrentState, Is.EqualTo(PaperGameUiState.Completed));
}
```

- [ ] **步骤 2：运行 Bootstrap 测试，确认失败。**

运行：`/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath "$(pwd)" -runTests -testPlatform EditMode -testFilter C1GameBootstrapTests -testResults "$(pwd)/Temp/bootstrap-ui-results.xml" -logFile "$(pwd)/Temp/bootstrap-ui.log"`

预期：失败，因为 Bootstrap 尚未实例化 `PaperGameUiFlow`，且旧拍照 HUD 仍存在。

- [ ] **步骤 3：以预制体实例化替换临时 UI。**

在 `CreateHud` 后加载 `Resources` 或序列化引用的四个预制体，在固定 Canvas 内创建 `PaperGameUiFlow`；把原先 `CreateMobileHud` 中的移动和跳跃 `C1Touch*Button` 绑定到 `PaperGameHud` 的对应按钮。移除正式流程中的拍照按钮、照片预览、照片状态文字和调试高度面板；保留竖屏遮罩，并使遮罩出现时 CanvasGroup 禁止 HUD 点击。

```csharp
private void HandleGoalReached(C1PlayerController2D player)
{
    uiFlow.ShowCompleted();
}

private void HandlePlayerFell(C1PlayerController2D player)
{
    uiFlow.ShowRetry();
}
```

结果页的重新开始回调继续调用 `Build(C1LevelLoader.LoadDefault())`，并先解绑旧事件和销毁旧 Canvas，确保只存在一个 `EventSystem` 与一个 HUD Canvas。

- [ ] **步骤 4：重跑 Bootstrap 测试。**

运行步骤 2 的命令。

预期：`C1GameBootstrapTests` 全部通过，且没有重复 Canvas 警告。

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/C1GameBootstrap.cs Assets/Scripts/C1/C1MobileOrientationHint.cs Assets/Tests/EditMode/C1GameBootstrapTests.cs
git commit -m "feat(UI): 接入绘本试玩流程"
```

### 任务 5：全量回归与 WebGL 验收

**文件：**
- 修改：`Docs/superpowers/specs/2026-09-06-picture-book-play-ui-design.md`（仅在验收发现规格需要澄清时修改）

- [ ] **步骤 1：运行完整 EditMode 套件。**

运行：`/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath "$(pwd)" -runTests -testPlatform EditMode -testResults "$(pwd)/Temp/editmode-results.xml" -logFile "$(pwd)/Temp/editmode-ui.log"`

预期：命令退出码为 `0`，`Temp/editmode-results.xml` 包含 `failed="0"`。

- [ ] **步骤 2：构建 WebGL。**

运行：`C1_WEBGL_OUTPUT="$(pwd)/Builds/C1WebGL" /Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$(pwd)" -executeMethod PaperGame.C1.Editor.C1WebGLBuilder.BuildFromCommandLine -logFile "$(pwd)/Temp/c1-webgl-ui-build.log"`

预期：命令退出码为 `0`，`Builds/C1WebGL/Build/` 的构建产物更新时间更新。

- [ ] **步骤 3：在手机横屏进行人工验收。**

- [ ] 首次进入可看到首页；点击“开始试玩”后依次完成三页引导。
- [ ] 左/右/跳按钮在边缘安全区内，按钮最小可触面积不小于 `96×96` 参考像素。
- [ ] 碰到红旗显示“你到终点啦！”，掉出关卡显示“再试一次吧”，两个页面都能重新开始。
- [ ] 将手机转为竖屏时，只有“请将手机旋转为横屏”遮罩可见，游戏操作不生效。

- [ ] **步骤 4：提交验收结果。**

```bash
git add Docs/superpowers/specs/2026-09-06-picture-book-play-ui-design.md
git commit -m "test(UI): 验收绘本试玩界面"
```

## 自检

- 规格中的首页、三步引导、精简 HUD、成功/重试页、拍摄占位、横屏遮罩、最小触控尺寸、首次引导偏好和回退要求，分别由任务 2、3、4 和 5 覆盖。
- 所有新类型都在任务 2 或任务 3 定义，再由任务 4 使用；UI 不拥有角色物理或关卡解析逻辑。
- 计划没有未定义的“待定”实现项；图像生成步骤限定了具体元素、导入路径和透明要求。
