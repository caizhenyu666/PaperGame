# 关卡选择界面视觉完善实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 `executing-plans` 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法跟踪进度。

**目标：** 将关卡选择界面完善为清晰、统一、可发布的童画背景与纸艺控件混合界面。

**架构：** 新增关卡选择专用无文字素材；`C1LevelSelectionLayout` 构建 Header、固定创建入口、预览相框和 Action Bar，Editor 生成器负责 Sprite 导入、Prefab 生成与三分辨率截图。`C1LevelSelection` 只更新静态节点绑定，业务流程保持不变。

**技术栈：** Unity 2022.3、C#、UGUI、Resources、Unity Editor Prefab API、ImageGen、NUnit EditMode Tests。

---

## 文件结构

- 创建：`Assets/Art/UI/LevelSelection/`——无文字 PNG 素材。
- 修改：`Assets/Editor/AIUI/Schemas/C1LevelSelection.ui.json`——新版层级和尺寸。
- 修改：`Assets/Scripts/C1/UI/C1LevelSelectionLayout.cs`——静态视觉结构。
- 修改：`Assets/Scripts/C1/UI/C1LevelSelection.cs`——新版节点绑定和状态表现。
- 修改：`Assets/Editor/PaperGameLevelSelectionPrefabGenerator.cs`——Sprite 导入、Prefab 与预览生成。
- 生成：`Assets/Resources/C1UI/PaperGameLevelSelection.prefab`——正式 Prefab。
- 修改：`Assets/Tests/EditMode/PaperGameLevelSelectionPrefabTests.cs`——新版结构测试。
- 修改：`Assets/Tests/EditMode/C1LevelSelectionTests.cs`——新版节点业务测试。
- 更新：`Docs/ui/level-selection-preview*.png`——三种分辨率预览。

### 任务 1：建立新版结构红灯测试

**文件：**
- 修改：`Assets/Tests/EditMode/PaperGameLevelSelectionPrefabTests.cs`
- 修改：`Assets/Tests/EditMode/C1LevelSelectionTests.cs`

- [ ] **步骤 1：补充新版结构断言**

```csharp
foreach (var path in new[]
{
    "Header/Title/Label",
    "Header/Drawing Help/Icon",
    "Header/Back Home/Icon",
    "Level Book/Viewport/Content/Level Item Template/Star",
    "Level Book/Create Level/Icon",
    "Preview Paper/Level Preview",
    "Preview Paper/Frame",
    "Action Bar/Status Paper/Status",
    "Action Bar/Regenerate/Label",
    "Action Bar/Play/Label"
}) Assert.That(prefab.transform.Find(path), Is.Not.Null, path);
```

同时断言创建入口不属于滚动内容，预览包含 `AspectRatioFitter`，新版图标 Sprite 均不为空。

- [ ] **步骤 2：更新业务测试路径**

```csharp
selection.transform.Find("Action Bar/Regenerate");
selection.transform.Find("Header/Drawing Help");
selection.transform.Find("Level Book/Create Level");
```

- [ ] **步骤 3：运行红灯测试**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit \
  -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" \
  -runTests -testPlatform EditMode \
  -testFilter "PaperGame.C1.Tests.PaperGameLevelSelectionPrefabTests;PaperGame.C1.Tests.C1LevelSelectionTests" \
  -testResults /private/tmp/papergame-level-selection-polish-red.xml \
  -logFile /private/tmp/papergame-level-selection-polish-red.log
```

预期：FAIL，缺少 Header、Action Bar、新版图标或相框节点。

- [ ] **步骤 4：提交红灯测试**

```bash
git add Assets/Tests/EditMode/PaperGameLevelSelectionPrefabTests.cs Assets/Tests/EditMode/C1LevelSelectionTests.cs
git commit -m "test(关卡选择): 添加视觉完善结构测试"
```

### 任务 2：生成并导入专用素材

**文件：**
- 创建：`Assets/Art/UI/LevelSelection/*.png`
- 修改：`Assets/Editor/PaperGameLevelSelectionPrefabGenerator.cs`

- [ ] **步骤 1：使用 ImageGen 分别生成 10 个无文字素材**

统一约束：儿童绘本纸艺贴纸、粗蜡笔轮廓、清晰剪影；无文字、字母、数字、Emoji 和水印。分别生成 `title-paper.png`、`nav-paper.png`、`icon-help.png`、`icon-back.png`、`icon-camera.png`、`star-on.png`、`star-off.png`、`preview-frame.png`、`button-primary.png` 和 `button-secondary.png`。图标、星标和相框使用透明背景，相框中心透明。

- [ ] **步骤 2：保存最终素材**

将最终图片保存到 `Assets/Art/UI/LevelSelection/`，不得覆盖共享素材。

- [ ] **步骤 3：配置 Sprite 导入**

生成器新增 `ConfigureSprites()`：

```csharp
importer.textureType = TextureImporterType.Sprite;
importer.spriteImportMode = SpriteImportMode.Single;
importer.alphaIsTransparency = true;
importer.mipmapEnabled = false;
importer.filterMode = FilterMode.Bilinear;
importer.SaveAndReimport();
```

可拉伸底板设置 `spriteBorder`，布局使用 `Image.Type.Sliced`。

- [ ] **步骤 4：验证素材文件**

```bash
file Assets/Art/UI/LevelSelection/*.png
```

预期：10 个 PNG 均可识别，透明素材为 RGBA。

- [ ] **步骤 5：提交素材**

```bash
git add Assets/Art/UI/LevelSelection Assets/Editor/PaperGameLevelSelectionPrefabGenerator.cs
git commit -m "feat(关卡选择): 添加清晰纸艺控件素材"
```

### 任务 3：重构静态布局并生成 Prefab

**文件：**
- 修改：`Assets/Editor/AIUI/Schemas/C1LevelSelection.ui.json`
- 修改：`Assets/Scripts/C1/UI/C1LevelSelectionLayout.cs`
- 修改：`Assets/Editor/PaperGameLevelSelectionPrefabGenerator.cs`
- 生成：`Assets/Resources/C1UI/PaperGameLevelSelection.prefab`

- [ ] **步骤 1：更新 Schema**

增加 `Header`、`Action Bar`、`Preview Paper/Frame` 和各按钮 Icon/Label，确保节点名唯一、尺寸为正数。

- [ ] **步骤 2：加载专用素材**

由 Editor 构建器把 `Assets/Art/UI/LevelSelection/` 下的 Sprite 绑定到 Prefab；运行时不得依赖 `AssetDatabase`。

- [ ] **步骤 3：构建 Header 与关卡册**

建立独立标题、帮助和返回贴纸；提高条目可读性；创建入口固定在关卡册底部并使用 `icon-camera`。

- [ ] **步骤 4：构建预览与 Action Bar**

`Level Preview` 位于底层，透明中心 `Frame` 位于上层且不接收射线。Action Bar 包含状态纸、绿色重新生成按钮和黄色开始按钮。

- [ ] **步骤 5：生成 Prefab**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit \
  -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" \
  -executeMethod PaperGame.C1.Editor.PaperGameLevelSelectionPrefabGenerator.Generate \
  -logFile /private/tmp/papergame-level-selection-polish-generate.log
```

预期：日志包含 `Level selection prefab generated`，Prefab 无 Missing Script。

- [ ] **步骤 6：提交布局和 Prefab**

```bash
git add Assets/Editor/AIUI/Schemas/C1LevelSelection.ui.json Assets/Scripts/C1/UI/C1LevelSelectionLayout.cs Assets/Editor/PaperGameLevelSelectionPrefabGenerator.cs Assets/Resources/C1UI/PaperGameLevelSelection.prefab
git commit -m "feat(关卡选择): 完善纸艺双栏布局"
```

### 任务 4：适配业务绑定与状态表现

**文件：**
- 修改：`Assets/Scripts/C1/UI/C1LevelSelection.cs`
- 修改：`Assets/Tests/EditMode/C1LevelSelectionTests.cs`

- [ ] **步骤 1：更新绑定路径**

```csharp
status = transform.Find("Action Bar/Status Paper/Status").GetComponent<Text>();
regenerate = Hook("Action Bar/Regenerate", () => StartWork(true));
play = Hook("Action Bar/Play", Play);
Hook("Header/Drawing Help", () => ShowDrawingTutorial(false));
Hook("Header/Back Home", () => returnHome());
```

- [ ] **步骤 2：完善星标与缩略图空状态**

选中条目使用实心星标和完整黄色选中层，未选中使用空心星标。缩略图为空时显示中性纸色，不显示透明黑块。

- [ ] **步骤 3：运行目标测试**

重复任务 1 的测试命令。预期：全部通过，生成、重新生成和开始关卡行为不变。

- [ ] **步骤 4：提交业务适配**

```bash
git add Assets/Scripts/C1/UI/C1LevelSelection.cs Assets/Tests/EditMode/C1LevelSelectionTests.cs
git commit -m "refactor(关卡选择): 适配新版视觉节点"
```

### 任务 5：渲染和验证

**文件：**
- 更新：`Docs/ui/level-selection-preview.png`
- 更新：`Docs/ui/level-selection-preview-square.png`
- 更新：`Docs/ui/level-selection-preview-ultrawide.png`

- [ ] **步骤 1：生成三种尺寸预览**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit \
  -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" \
  -executeMethod PaperGame.C1.Editor.PaperGameLevelSelectionPrefabGenerator.GenerateAndRender \
  -logFile /private/tmp/papergame-level-selection-polish-render.log
```

- [ ] **步骤 2：视觉检查**

逐图检查标题、导航、相机、星标、相框、状态和操作按钮，确认没有模糊图标、低对比度灰字、图片拉伸、遮挡或安全区溢出。

- [ ] **步骤 3：运行目标测试和编译**

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

预期：0 个编译错误。随后运行任务 1 的目标测试和全量 EditMode，记录与基线相比是否新增失败。

- [ ] **步骤 4：检查差异并提交**

```bash
git diff --check
git status --short
git add Docs/ui/level-selection-preview.png Docs/ui/level-selection-preview-square.png Docs/ui/level-selection-preview-ultrawide.png
git commit -m "test(关卡选择): 更新视觉验收预览"
```

- [ ] **步骤 5：更新计划进度**

将全部复选框改为 `[x]`，提交：

```bash
git add Docs/superpowers/plans/2026-09-17-level-selection-visual-polish.md
git commit -m "docs(关卡选择): 更新视觉完善实施进度"
```
