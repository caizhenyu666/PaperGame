# 内置默认关卡实现计划

> **面向 AI 代理的工作者：** 使用 `executing-plans` 在当前共享工作区执行。本仓库当前包含尚未提交且与关卡页码相关的改动，不创建基于旧 `HEAD` 的工作树，也不自动提交用户现有改动。

**目标：** 将指定的 900×560 本地关卡固化为始终存在的内置第 1 页，并让用户关卡从第 2 页开始。

**架构：** 完整服务器响应和背景图作为 `Resources/C1Levels/level1` 的配套资源随安装包发布；`C1LevelLoader` 在加载打包关卡时优先绑定同名本地背景。`C1LevelSelection` 将内置关卡作为独立固定列表项，不把它伪装成可变的本地 GUID 记录。

**技术栈：** Unity 2022、C#、UGUI、Unity Resources、NUnit EditMode Tests。

---

## 文件结构

- 修改：`Assets/Resources/C1Levels/level1.json`——保存指定服务器响应。
- 修改：`Assets/Resources/C1Levels/level1-background.png`——保存指定 900×560 矫正背景。
- 修改：`Assets/Scripts/C1/C1LevelLoader.cs`——为打包关卡绑定相邻本地背景资源。
- 修改：`Assets/Scripts/C1/UI/C1LevelSelection.cs`——固定创建、预览和启动内置第 1 页。
- 修改：`Assets/Tests/EditMode/C1LevelLoaderTests.cs`——验证内置资源内容与尺寸。
- 创建：`Assets/Tests/EditMode/C1LevelSelectionTests.cs`——验证固定第 1 页和用户页码偏移。

### 任务 1：固化默认关卡资源

- [ ] **步骤 1：先修改默认关卡测试，表达新数据要求**

将 `LoadDefault_ReturnsPhotoBackgroundPixelLevel` 的断言改为：

```csharp
Assert.That(level.CanvasPixelSize, Is.EqualTo(new Vector2Int(900, 560)));
Assert.That(level.PlayerStartIsFeet, Is.True);
Assert.That(level.PlayerStart, Is.EqualTo(new Vector2(150f, 184f)));
Assert.That(level.Platforms, Has.Length.Zero);
Assert.That(level.Walls, Has.Length.Zero);
Assert.That(level.Blocks, Has.Length.EqualTo(13));
Assert.That(level.Blocks[0].Region, Is.EqualTo(new Rect(18f, 315f, 229f, 30f)));
Assert.That(level.Blocks[12].Region, Is.EqualTo(new Rect(816f, 111f, 48f, 12f)));
Assert.That(level.GoalRegion, Is.EqualTo(new Rect(812f, 65f, 42f, 63f)));
```

另加背景资源测试：

```csharp
[Test]
public void LoadDefault_UsesBundledBackgroundAtCanvasResolution()
{
    var level = C1LevelLoader.LoadDefault();
    var texture = Resources.Load<Texture2D>(level.BackgroundResourcePath);

    Assert.That(texture, Is.Not.Null);
    Assert.That(new Vector2Int(texture.width, texture.height), Is.EqualTo(level.CanvasPixelSize));
}
```

- [ ] **步骤 2：运行目标测试并确认红灯**

运行 Unity EditMode 测试筛选器 `PaperGame.C1.Tests.C1LevelLoaderTests`。预期：旧默认关卡仍为 1245×810、7 个平台，新增断言失败。

- [ ] **步骤 3：复制指定本地资源**

将：

```text
/Users/caizhenyu/Library/Application Support/DefaultCompany/PaperGame/Levels/v1/4549cce997554b30855d81382360d709/response.json
/Users/caizhenyu/Library/Application Support/DefaultCompany/PaperGame/Levels/v1/4549cce997554b30855d81382360d709/background.png
```

分别覆盖：

```text
Assets/Resources/C1Levels/level1.json
Assets/Resources/C1Levels/level1-background.png
```

- [ ] **步骤 4：最小修改打包背景解析**

在 `C1LevelLoader.Load` 中，解析成功后检查 `Resources.Load<Texture2D>(safePath + "-background")`。如果相邻背景存在，则把 `BackgroundResourcePath` 设置为该本地路径；不存在时保持当前解析结果，避免影响纯 JSON 解析和用户本地关卡。

- [ ] **步骤 5：运行目标测试并确认绿灯**

再次运行 `PaperGame.C1.Tests.C1LevelLoaderTests`。预期：默认关卡、API Mock 和解析测试全部通过。

### 任务 2：固定内置第 1 页

- [ ] **步骤 1：创建失败的关卡选择测试**

测试文件信息：

- 文件路径：`Assets/Tests/EditMode/C1LevelSelectionTests.cs`
- 命名空间：`PaperGame.C1.Tests`
- 测试类：`C1LevelSelectionTests`
- 测试层级：EditMode，L3（轻量 UGUI 装配与本地文件夹替身）
- 测试价值：高价值长期测试，不使用异常吞噬或 `Assert.Pass()`。

至少覆盖：

```csharp
[Test]
public void Configure_EmptyLibraryStillShowsAndSelectsBuiltInFirstPage()
{
    selection.Configure(() => { }, false, new C1LevelLibrary(tempRoot));

    Assert.That(selection.LevelCount, Is.EqualTo(1));
    Assert.That(selection.IsBuiltInSelected, Is.True);
    Assert.That(selection.SelectedPageNumber, Is.EqualTo(1));
    Assert.That(GameObject.Find("第1页"), Is.Not.Null);
}
```

再构造 1 个有效的本地关卡记录，点击其列表按钮并断言 `SelectedPageNumber == 2`。

- [ ] **步骤 2：运行目标测试并确认红灯**

运行 Unity EditMode 测试筛选器 `PaperGame.C1.Tests.C1LevelSelectionTests`。预期：`IsBuiltInSelected`、`SelectedPageNumber` 和固定列表项尚不存在，测试编译或断言失败。

- [ ] **步骤 3：实现固定内置项与选择状态**

在 `C1LevelSelection` 中：

- 添加只读属性 `IsBuiltInSelected` 与 `SelectedPageNumber`。
- `RefreshList()` 先创建标题为「第1页」的固定按钮，再追加本地关卡。
- `LevelCount` 等于 `1 + levels.Count`。
- 本地关卡页码由原来的 `index + 1` 改为 `index + 2`。
- `BrowseBuiltIn()` 使用 `C1LevelLoader.LoadDefault()` 校验配置，并从 `C1Levels/level1-background` 加载预览。
- 初次配置时自动调用 `BrowseBuiltIn()`。
- `Play()` 在内置项被选中时调用 `C1GameSession.Instance.SetPendingLevel(C1GameSession.DefaultLevelResourcePath)`；选择用户关卡时保留现有本地 GUID 流程。
- 拍照或选择用户关卡时清除内置选择标记；忙碌状态根据两种选择共同决定播放按钮是否可用。

- [ ] **步骤 4：运行目标测试并确认绿灯**

再次运行 `PaperGame.C1.Tests.C1LevelSelectionTests`。预期：固定第 1 页与用户第 2 页测试全部通过。

### 任务 3：回归与资源验证

- [ ] **步骤 1：运行相关测试集合**

运行以下测试：

```text
PaperGame.C1.Tests.C1LevelLoaderTests
PaperGame.C1.Tests.C1LevelSelectionTests
PaperGame.C1.Tests.C1GameSessionTests
PaperGame.C1.Tests.C1GameBootstrapTests
```

预期：本次相关测试全部通过。

- [ ] **步骤 2：运行完整 EditMode 测试**

运行项目完整 EditMode 测试并保存 XML 结果。将失败与变更前已知失败对比，不把无关历史失败误报为本次回归。

- [ ] **步骤 3：静态资源检查**

验证：

```text
level1-background.png = 900×560
level1.json 可解析且包含 13 个 blocks
git diff --check 无格式错误
```

- [ ] **步骤 4：检查最终差异**

只审查本计划涉及的文件，确认未覆盖用户现有角色、HUD 和首页改动；不自动执行 `git commit`。
