# 关卡选择界面预制体重设计实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 `executing-plans` 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法跟踪进度。

**目标：** 用正式的手账双栏 Prefab 替换关卡选择页测试样式，收敛生成恢复入口，并把首页统一为单一「开始游戏」入口。

**架构：** `C1LevelSelectionLayout` 描述 `1920×1080` 静态节点和已有 Sprite 绑定，Editor 生成器产出独立业务 Prefab；`C1LevelSelection` 只绑定节点、复制条目模板并管理业务状态。`HomeBootstrap` 只实例化 Prefab，首页生成器移除重复的拍照入口。

**技术栈：** Unity 2022.3、C#、UGUI、Resources、Unity Editor Prefab API、NUnit EditMode Tests。

---

## 文件结构

- 创建：`Assets/Editor/AIUI/Schemas/C1LevelSelection.ui.json`——受约束的页面结构描述。
- 创建：`Assets/Scripts/C1/UI/C1LevelSelectionLayout.cs`——构建静态布局并绑定已有 Sprite。
- 创建：`Assets/Scripts/C1/UI/C1LevelSelectionScreenFit.cs`——按统一比例适配近方形与超宽屏。
- 创建：`Assets/Editor/PaperGameLevelSelectionPrefabGenerator.cs`——生成 Prefab 并渲染预览。
- 创建：`Assets/Resources/C1UI/PaperGameLevelSelection.prefab`——Unity 生成的正式页面。
- 创建：`Assets/Tests/EditMode/PaperGameLevelSelectionPrefabTests.cs`——验证结构、资源、默认状态和适配。
- 创建：`Docs/ui/level-selection-preview.png`——Unity 实际渲染截图。
- 修改：`Assets/Scripts/C1/UI/C1LevelSelection.cs`——绑定 Prefab、刷新模板条目并收敛重新生成状态。
- 修改：`Assets/Scripts/C1/HomeBootstrap.cs`——实例化关卡选择 Prefab。
- 修改：`Assets/Editor/PaperGameHomePrefabGenerator.cs`——移除首页重复拍照按钮并调整主按钮布局。
- 修改：`Assets/Tests/EditMode/C1LevelSelectionTests.cs`——改用正式 Prefab 并覆盖按钮显隐。
- 修改：`Assets/Tests/EditMode/HomeBootstrapTests.cs`——覆盖单入口和 Prefab 实例化。
- 修改：`Assets/Tests/EditMode/PaperGameHomePrefabTests.cs`——断言首页不再包含拍照入口。

### 任务 1：建立预制体结构红灯测试

**文件：**
- 创建：`Assets/Tests/EditMode/PaperGameLevelSelectionPrefabTests.cs`
- 修改：`Assets/Tests/EditMode/C1LevelSelectionTests.cs`

- [ ] **步骤 1：编写 Prefab 结构失败测试**

新增测试并要求正式 Prefab 包含所有业务绑定节点：

```csharp
[Test]
public void LevelSelectionPrefab_HasRequiredPaperLayoutAndControls()
{
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Resources/C1UI/PaperGameLevelSelection.prefab");
    Assert.That(prefab, Is.Not.Null);
    Assert.That(prefab.GetComponent<C1LevelSelection>(), Is.Not.Null);
    Assert.That(prefab.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(1920, 1080)));
    foreach (var path in new[]
    {
        "Background", "Title", "Drawing Help", "Back Home", "Level Book/Viewport/Content/Level Item Template",
        "Level Book/Create Level", "Preview Paper/Level Preview", "Status", "Regenerate", "Play"
    }) Assert.That(prefab.transform.Find(path), Is.Not.Null, path);
    Assert.That(prefab.transform.Find("Regenerate").gameObject.activeSelf, Is.False);
}
```

- [ ] **步骤 2：让业务测试从正式 Prefab 创建控制器**

将测试初始化改为：

```csharp
var prefab = Resources.Load<GameObject>("C1UI/PaperGameLevelSelection");
selectionObject = UnityEngine.Object.Instantiate(prefab);
selection = selectionObject.GetComponent<C1LevelSelection>();
```

新增断言：正常状态隐藏 `Regenerate`，设置 `library.Pending` 后重新配置时显示 `Regenerate`，且界面不存在「继续生成」按钮。

- [ ] **步骤 3：运行目标测试并确认红灯**

运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit \
  -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" \
  -runTests -testPlatform EditMode \
  -testFilter "PaperGame.C1.Tests.PaperGameLevelSelectionPrefabTests;PaperGame.C1.Tests.C1LevelSelectionTests" \
  -testResults /private/tmp/papergame-level-selection-red.xml \
  -logFile /private/tmp/papergame-level-selection-red.log
```

预期：FAIL，提示 `PaperGameLevelSelection.prefab` 或所需节点不存在。

- [ ] **步骤 4：提交测试**

```bash
git add Assets/Tests/EditMode/PaperGameLevelSelectionPrefabTests.cs Assets/Tests/EditMode/C1LevelSelectionTests.cs
git commit -m "test(关卡选择): 添加正式预制体红灯测试"
```

### 任务 2：创建 Schema、布局和正式 Prefab

**文件：**
- 创建：`Assets/Editor/AIUI/Schemas/C1LevelSelection.ui.json`
- 创建：`Assets/Scripts/C1/UI/C1LevelSelectionLayout.cs`
- 创建：`Assets/Scripts/C1/UI/C1LevelSelectionScreenFit.cs`
- 创建：`Assets/Editor/PaperGameLevelSelectionPrefabGenerator.cs`
- 生成：`Assets/Resources/C1UI/PaperGameLevelSelection.prefab`

- [ ] **步骤 1：写入受约束 Schema**

Schema 使用唯一节点名、正数尺寸和现有组件名：

```json
{
  "name": "PaperGameLevelSelection",
  "canvasWidth": 1920,
  "canvasHeight": 1080,
  "nodes": [
    { "name": "Background", "component": "PaperImage", "width": 1920, "height": 1080 },
    { "name": "Title", "component": "PaperLabel", "width": 620, "height": 150 },
    { "name": "Drawing Help", "component": "PaperTextButton", "width": 250, "height": 100 },
    { "name": "Back Home", "component": "PaperImageButton", "width": 370, "height": 114 },
    { "name": "Level Book", "component": "PaperPanel", "width": 560, "height": 650 },
    { "name": "Preview Paper", "component": "PaperPanel", "width": 980, "height": 650 },
    { "name": "Status", "component": "PaperText", "width": 760, "height": 80 },
    { "name": "Regenerate", "component": "PaperTextButton", "width": 320, "height": 110 },
    { "name": "Play", "component": "PaperTextButton", "width": 420, "height": 120 }
  ]
}
```

- [ ] **步骤 2：实现布局构建器**

`C1LevelSelectionLayout.Build` 创建完整静态层级，复用 `C1CharacterUI/background`、`notebook`、`preview-paper`、`paper`、`star-on`、`star-off`、`cloud`、`sun`、`tree-left`、`tree-right`、`bottom-grass`、`back`、`add` 和 `C1GameUI/paper-label`。条目模板包含：

```text
Level Item Template
├── Selection
├── Thumbnail
├── Label
├── Source
└── Star
```

模板根节点带 `Button` 和 `LayoutElement`，默认隐藏；`Thumbnail` 使用 `RawImage`，预览使用 `RawImage + AspectRatioFitter`。

- [ ] **步骤 3：实现分辨率适配**

`C1LevelSelectionScreenFit.RefreshLayout()` 使用：

```csharp
var scale = Mathf.Min(viewport.rect.width / 1920f, viewport.rect.height / 1080f);
rect.sizeDelta = new Vector2(1920, 1080);
rect.localScale = new Vector3(scale, scale, 1);
```

背景扩展覆盖额外区域，顶部导航、底部操作和四角装饰按额外边距外移，中心书本与预览保持固定设计坐标。

- [ ] **步骤 4：实现 Editor 生成器**

生成器调用布局构建器后执行：

```csharp
var root = C1LevelSelectionLayout.Build(null);
PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
Object.DestroyImmediate(root);
AssetDatabase.SaveAssets();
```

同时提供 `GenerateAndRender`，用于渲染 `1920×1080` 预览。

- [ ] **步骤 5：通过 Unity 生成 Prefab**

运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit \
  -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" \
  -executeMethod PaperGame.C1.Editor.PaperGameLevelSelectionPrefabGenerator.Generate \
  -logFile /private/tmp/papergame-level-selection-generate.log
```

预期：日志包含 `Level selection prefab generated`，Prefab 无丢失脚本。

- [ ] **步骤 6：运行结构测试确认通过**

重复任务 1 的目标测试命令。预期：Prefab 结构测试通过；业务测试仍因旧绑定方式失败。

- [ ] **步骤 7：提交静态 UI**

```bash
git add Assets/Editor/AIUI/Schemas/C1LevelSelection.ui.json Assets/Scripts/C1/UI/C1LevelSelectionLayout.cs Assets/Scripts/C1/UI/C1LevelSelectionScreenFit.cs Assets/Editor/PaperGameLevelSelectionPrefabGenerator.cs Assets/Resources/C1UI/PaperGameLevelSelection.prefab Assets/Tests/EditMode/PaperGameLevelSelectionPrefabTests.cs
git commit -m "feat(关卡选择): 添加手账双栏预制体"
```

### 任务 3：接入关卡选择业务与重新生成状态

**文件：**
- 修改：`Assets/Scripts/C1/UI/C1LevelSelection.cs`
- 修改：`Assets/Tests/EditMode/C1LevelSelectionTests.cs`

- [ ] **步骤 1：补充重新生成行为红灯测试**

新增测试，预置暂存照片与旧任务编号，点击 `Regenerate` 后断言新流程采用强制上传语义；通过注入或公开只读状态验证不再存在继续旧任务入口。至少包含以下 UI 断言：

```csharp
Assert.That(selection.transform.Find("Regenerate").gameObject.activeSelf, Is.True);
Assert.That(GameObject.Find("继续生成"), Is.Null);
Assert.That(selection.transform.Find("Regenerate").GetComponent<Button>().interactable, Is.True);
```

- [ ] **步骤 2：绑定 Prefab 静态节点**

`Configure` 在开始时取得：

```csharp
list = transform.Find("Level Book/Viewport/Content");
itemTemplate = list.Find("Level Item Template").gameObject;
preview = transform.Find("Preview Paper/Level Preview").GetComponent<RawImage>();
status = transform.Find("Status").GetComponent<Text>();
create = Hook("Level Book/Create Level", RequestCreateLevel);
regenerate = Hook("Regenerate", () => StartWork(true));
play = Hook("Play", Play);
Hook("Drawing Help", () => ShowDrawingTutorial(false));
Hook("Back Home", () => returnHome());
```

- [ ] **步骤 3：用模板刷新列表**

对每个关卡复制模板并配置：

```csharp
var row = Instantiate(itemTemplate, list, false);
row.name = displayTitle;
row.SetActive(true);
row.transform.Find("Label").GetComponent<Text>().text = displayTitle;
row.transform.Find("Source").GetComponent<Text>().text = builtIn ? "内置关卡" : "我的关卡";
row.GetComponent<Button>().onClick.AddListener(onClick);
```

选中时切换 `Selection`、`star-on` 与 `star-off`，并更新缩略图与大预览；刷新前只销毁模板之外的运行时条目。

- [ ] **步骤 4：收敛恢复流程**

删除 `resume` 字段和 `StartWork(false)` 的公开按钮入口。`Regenerate` 仅在 `library.Pending != null` 时显示，点击固定执行 `StartWork(true)`。生成失败保留暂存照片；生成成功清除 Pending 并隐藏按钮。

- [ ] **步骤 5：修正繁忙与异常状态**

`SetBusy` 同时锁定列表、创建、帮助、返回、重新生成和开始按钮；`finally` 中始终恢复交互。内置关卡和本地关卡都能正确启用 `Play`，避免原实现只按 `selected != null` 判断导致内置关卡在繁忙状态后不可用。

- [ ] **步骤 6：运行目标测试确认通过**

运行任务 1 的目标测试命令。预期：全部通过。

- [ ] **步骤 7：提交业务接入**

```bash
git add Assets/Scripts/C1/UI/C1LevelSelection.cs Assets/Tests/EditMode/C1LevelSelectionTests.cs
git commit -m "refactor(关卡选择): 接入正式预制体与重新生成状态"
```

### 任务 4：首页改为单入口并接入正式页面

**文件：**
- 修改：`Assets/Editor/PaperGameHomePrefabGenerator.cs`
- 生成：`Assets/Resources/C1UI/PaperGameHome.prefab`
- 修改：`Assets/Scripts/C1/HomeBootstrap.cs`
- 修改：`Assets/Tests/EditMode/PaperGameHomePrefabTests.cs`
- 修改：`Assets/Tests/EditMode/HomeBootstrapTests.cs`

- [ ] **步骤 1：编写首页单入口红灯测试**

Prefab 测试改为：

```csharp
Assert.That(prefab.transform.Find("Start Play").GetComponent<Button>(), Is.Not.Null);
Assert.That(prefab.transform.Find("Capture Level"), Is.Null);
```

Bootstrap 测试调用 `ShowLevels(false)` 后断言实例名称为 `PaperGameLevelSelection`，根节点带 `C1LevelSelection`，首页隐藏；返回后首页恢复。

- [ ] **步骤 2：运行首页测试确认失败**

运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit \
  -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" \
  -runTests -testPlatform EditMode \
  -testFilter "PaperGame.C1.Tests.PaperGameHomePrefabTests;PaperGame.C1.Tests.HomeBootstrapTests" \
  -testResults /private/tmp/papergame-home-single-entry-red.xml \
  -logFile /private/tmp/papergame-home-single-entry-red.log
```

预期：FAIL，首页仍存在 `Capture Level`，关卡页仍由代码创建。

- [ ] **步骤 3：移除首页重复入口**

删除首页生成器中的 `Capture Level` 节点，把 `Start Play` 调整到页面主操作中心；`HomeBootstrap.BuildHome` 不再查找或绑定 `Capture Level`。

- [ ] **步骤 4：从 Resources 实例化关卡页面**

`ShowLevels` 使用：

```csharp
var prefab = Resources.Load<GameObject>("C1UI/PaperGameLevelSelection");
LevelScreen = Instantiate(prefab, canvas.transform, false);
LevelScreen.name = "PaperGameLevelSelection";
LevelScreen.GetComponent<C1LevelSelection>().Configure(ReturnHome, createImmediately);
```

若 Prefab 缺失，记录错误并恢复首页，不创建测试样式兜底页面。

- [ ] **步骤 5：重新生成首页并运行测试**

运行首页生成器，再运行步骤 2 的测试命令。预期：全部通过。

- [ ] **步骤 6：提交首页改动**

```bash
git add Assets/Editor/PaperGameHomePrefabGenerator.cs Assets/Resources/C1UI/PaperGameHome.prefab Assets/Scripts/C1/HomeBootstrap.cs Assets/Tests/EditMode/PaperGameHomePrefabTests.cs Assets/Tests/EditMode/HomeBootstrapTests.cs
git commit -m "feat(首页): 统一关卡选择入口"
```

### 任务 5：视觉渲染与完整验证

**文件：**
- 创建：`Docs/ui/level-selection-preview.png`
- 修改：`Assets/Editor/PaperGameLevelSelectionPrefabGenerator.cs`（仅在渲染发现布局问题时）
- 修改：`Assets/Scripts/C1/UI/C1LevelSelectionLayout.cs`（仅在渲染发现布局问题时）

- [ ] **步骤 1：生成正式预览**

运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit \
  -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" \
  -executeMethod PaperGame.C1.Editor.PaperGameLevelSelectionPrefabGenerator.GenerateAndRender \
  -logFile /private/tmp/papergame-level-selection-render.log
```

检查 `Docs/ui/level-selection-preview.png`：标题、列表、预览、状态和主按钮无遮挡，正式界面不出现默认测试样式。

- [ ] **步骤 2：验证三种宽高比**

运行 Prefab 测试中的 `1920×1080`、`1094×1016` 和 `2560×1080` 参数用例。预期：设计坐标保持 `1920×1080`，统一缩放，背景铺满，关键按钮位于安全区。

- [ ] **步骤 3：运行完整 EditMode 测试**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit \
  -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" \
  -runTests -testPlatform EditMode \
  -testResults /private/tmp/papergame-level-selection-all.xml \
  -logFile /private/tmp/papergame-level-selection-all.log
```

预期：新增与修改测试全部通过；若存在历史失败，记录测试名并确认与本次变更无关。

- [ ] **步骤 4：执行静态检查**

运行 `git diff --check`，检查所有新增 `.meta`、Schema JSON、Prefab 无丢失脚本，确认未修改服务端协议、相机逻辑或共享 Sprite。

- [ ] **步骤 5：提交验证产物**

```bash
git add Docs/ui/level-selection-preview.png Assets/Editor/PaperGameLevelSelectionPrefabGenerator.cs Assets/Scripts/C1/UI/C1LevelSelectionLayout.cs
git commit -m "test(关卡选择): 添加正式界面视觉验证"
```
