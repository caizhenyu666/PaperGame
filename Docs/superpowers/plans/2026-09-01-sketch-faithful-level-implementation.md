# 草图保真关卡与跳跃调节实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 以当前横屏草图的等比例 JSON 替换预置关卡，平台厚度降至 `0.08`，并提供不改动关卡几何的测试跳跃高度调节和不可达诊断。

**架构：** `level1.json` 只保存草图的运行时几何；纯 C# 的跳跃计算与可达性分析独立于 Unity UI；`C1PlaytestTuningPanel` 用滑块改变当前玩家的跳跃初速度，并显示分析结果。照片识别由外部多模态服务完成，调用提示词和输出契约以已提交设计文档为准。

**技术栈：** Unity 2022.3、C#、Unity UI（Legacy）、NUnit EditMode Test Framework。

---

## 文件结构

- 修改：`Assets/Resources/C1Levels/level1.json`——本图的 7 段等比例保真平台与终点。
- 创建：`Assets/Scripts/C1/C1JumpPhysics.cs`——高度、重力和跳跃初速度的纯计算。
- 创建：`Assets/Scripts/C1/C1Reachability.cs`——平台图的纯可达性分析与失败跳跃记录。
- 创建：`Assets/Scripts/C1/C1PlaytestTuningPanel.cs`——运行时跳跃高度滑块和状态文本。
- 修改：`Assets/Scripts/C1/C1PlayerController2D.cs`——应用运行时跳跃高度。
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`——创建、配置调节面板，并以 `0.08` 平台重建关卡。
- 创建：`Assets/Tests/EditMode/C1JumpPhysicsTests.cs`——跳跃物理单元测试。
- 创建：`Assets/Tests/EditMode/C1ReachabilityTests.cs`——不可达/提高跳跃高度后的可达性测试。
- 修改：`Assets/Tests/EditMode/C1LevelLoaderTests.cs`、`Assets/Tests/EditMode/C1GameBootstrapTests.cs`、`Assets/Tests/EditMode/C1PlayerControllerTests.cs`——草图坐标、厚度和试玩调节回归测试。

### 任务 1：替换为保真草图 JSON

**文件：**
- 修改：`Assets/Resources/C1Levels/level1.json`
- 修改：`Assets/Tests/EditMode/C1LevelLoaderTests.cs`

- [ ] **步骤 1：先更新失败断言，锁定草图数据。**

```csharp
[Test]
public void LoadDefault_ReturnsFaithfulLandscapeSketch()
{
    var level = C1LevelLoader.LoadDefault();

    Assert.That(level.Platforms, Has.Length.EqualTo(7));
    Assert.That(level.PlayerStart, Is.EqualTo(new Vector2(4.70f, 5.09f)));
    Assert.That(level.GoalPosition, Is.EqualTo(new Vector2(28.27f, 14.96f)));
    Assert.That(level.Platforms[0].Size.y, Is.EqualTo(0.08f).Within(0.001f));
    Assert.That(level.Platforms[6].Size.x, Is.EqualTo(17.96f).Within(0.001f));
}
```

- [ ] **步骤 2：运行 EditMode 测试，确认旧坐标导致失败。**

运行：`/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath "$(pwd)/Temp/C1VerifyProject" -runTests -testPlatform EditMode -testResults "$(pwd)/Temp/editmode-results.xml" -logFile "$(pwd)/Temp/C1VerifyProject/editmode.log"`

预期：`C1LevelLoaderTests.LoadDefault_ReturnsFaithfulLandscapeSketch` 失败，旧出生点为 `(-12.5, 0.8)`。

- [ ] **步骤 3：用以下内容替换 `level1.json`。**

```json
{
  "playerStart": { "x": 4.70, "y": 5.09 },
  "goalPosition": { "x": 28.27, "y": 14.96 },
  "platforms": [
    { "x": 4.70, "y": 4.40, "width": 5.79, "height": 0.08 },
    { "x": 13.05, "y": 4.40, "width": 8.87, "height": 0.08 },
    { "x": 16.64, "y": 6.69, "width": 8.41, "height": 0.08 },
    { "x": 22.82, "y": 8.70, "width": 4.63, "height": 0.08 },
    { "x": 17.09, "y": 10.70, "width": 6.66, "height": 0.08 },
    { "x": 9.06, "y": 12.70, "width": 8.40, "height": 0.08 },
    { "x": 21.18, "y": 14.96, "width": 17.96, "height": 0.08 }
  ]
}
```

- [ ] **步骤 4：重跑同一测试，确认通过。**

- [ ] **步骤 5：提交。**

```bash
git add Assets/Resources/C1Levels/level1.json Assets/Tests/EditMode/C1LevelLoaderTests.cs
git commit -m "feat(关卡): 使用保真草图坐标"
```

### 任务 2：实现跳跃高度换算

**文件：**
- 创建：`Assets/Scripts/C1/C1JumpPhysics.cs`
- 创建：`Assets/Tests/EditMode/C1JumpPhysicsTests.cs`
- 修改：`Assets/Scripts/C1/C1PlayerController2D.cs`
- 修改：`Assets/Tests/EditMode/C1PlayerControllerTests.cs`

- [ ] **步骤 1：编写失败测试。**

```csharp
[Test]
public void SpeedForHeight_UsesGravityAndReturnsUpwardSpeed()
{
    Assert.That(C1JumpPhysics.SpeedForHeight(4f, 29.43f), Is.EqualTo(15.345f).Within(0.001f));
}

[Test]
public void SetJumpHeight_ChangesOnlyRuntimeJumpSpeed()
{
    controller.SetJumpHeight(4f);
    Assert.That(controller.JumpSpeed, Is.EqualTo(C1JumpPhysics.SpeedForHeight(4f, 29.43f)).Within(0.001f));
}
```

- [ ] **步骤 2：运行上述两项，确认因类型或方法缺失而失败。**

- [ ] **步骤 3：创建纯计算类，并为玩家增加公开设置方法。**

```csharp
public static class C1JumpPhysics
{
    public static float SpeedForHeight(float height, float downwardGravity)
    {
        return Mathf.Sqrt(2f * Mathf.Max(0f, downwardGravity) * Mathf.Max(0f, height));
    }
}
```

```csharp
public void SetJumpHeight(float height)
{
    CacheComponents();
    var downwardGravity = Mathf.Abs(Physics2D.gravity.y) * body.gravityScale;
    jumpSpeed = C1JumpPhysics.SpeedForHeight(height, downwardGravity);
}
```

- [ ] **步骤 4：重跑测试；预期全部通过。**

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/C1JumpPhysics.cs Assets/Scripts/C1/C1PlayerController2D.cs Assets/Tests/EditMode/C1JumpPhysicsTests.cs Assets/Tests/EditMode/C1PlayerControllerTests.cs
git commit -m "feat(试玩): 支持按高度调节跳跃"
```

### 任务 3：实现不改图的可达性分析

**文件：**
- 创建：`Assets/Scripts/C1/C1Reachability.cs`
- 创建：`Assets/Tests/EditMode/C1ReachabilityTests.cs`

- [ ] **步骤 1：定义失败测试：默认高度不可达，增高后可达。**

```csharp
[Test]
public void Analyze_FaithfulSketchNeedsHigherJumpHeight()
{
    var result = C1Reachability.Analyze(C1LevelLoader.LoadDefault(), 1.4f);
    Assert.That(result.CanReachGoal, Is.False);
    Assert.That(result.BlockedJumps, Is.Not.Empty);
}

[Test]
public void Analyze_FaithfulSketchBecomesReachableAtFourPointOne()
{
    var result = C1Reachability.Analyze(C1LevelLoader.LoadDefault(), 4.1f);
    Assert.That(result.CanReachGoal, Is.True);
}
```

- [ ] **步骤 2：运行测试，确认编译失败。**

- [ ] **步骤 3：实现图搜索。**

```csharp
public static C1ReachabilityResult Analyze(C1LevelDefinition level, float jumpHeight)
{
    // 每个平台是一个节点；平台水平投影相交或间隙不大于
    // sqrt(2 * jumpHeight / gravity) * moveSpeed 时可以尝试跳跃。
    // 目标节点为旗帜所在平台。只依据原始坐标建边，不改变 level。
    // 对每条未满足 height 或 gap 的候选边记录 C1BlockedJump。
}
```

`C1BlockedJump` 至少包含 `FromPlatformIndex`、`ToPlatformIndex`、`RequiredHeight` 与 `RequiredHorizontalGap`。通过 BFS 从包含 `PlayerStart.x` 的最低可站立平台搜索到包含 `GoalPosition.x` 的平台。

- [ ] **步骤 4：重跑测试，确认 1.4 不可达、4.1 可达。**

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/C1Reachability.cs Assets/Tests/EditMode/C1ReachabilityTests.cs
git commit -m "feat(关卡): 增加保真几何可达性诊断"
```

### 任务 4：生成测试跳跃高度面板

**文件：**
- 创建：`Assets/Scripts/C1/C1PlaytestTuningPanel.cs`
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`

- [ ] **步骤 1：先补失败测试。**

```csharp
[Test]
public void Build_CreatesPlaytestTuningPanelWithDefaultHeight()
{
    bootstrap.Build(C1LevelLoader.LoadDefault());
    var panel = Object.FindObjectOfType<C1PlaytestTuningPanel>();

    Assert.That(panel, Is.Not.Null);
    Assert.That(panel.JumpHeight, Is.EqualTo(1.4f).Within(0.001f));
}
```

- [ ] **步骤 2：运行测试，确认面板类型缺失。**

- [ ] **步骤 3：创建面板并接入 Bootstrap。**

```csharp
public sealed class C1PlaytestTuningPanel : MonoBehaviour
{
    public const float MinimumJumpHeight = 1.4f;
    public const float MaximumJumpHeight = 18f;
    public const float JumpHeightStep = 0.1f;
    public float JumpHeight { get; private set; } = MinimumJumpHeight;

    public void Configure(C1PlayerController2D player, C1LevelDefinition level)
    {
        // 创建右侧 Slider、数值 Text 和状态 Text；监听 valueChanged。
        // 通过 Mathf.Round(value / JumpHeightStep) * JumpHeightStep 对齐步长。
        // 每次更新调用 player.SetJumpHeight(JumpHeight) 与 C1Reachability.Analyze(level, JumpHeight)。
    }
}
```

在 `C1GameBootstrap.Build` 的 `CreateHud()` 后调用 `CreatePlaytestTuningPanel(level)`；面板是 HUD Canvas 的子对象，锚定为右上，不能遮挡终点。

- [ ] **步骤 4：重跑 Bootstrap 测试，确认默认高度、UI 创建和玩家速度同步。**

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/C1PlaytestTuningPanel.cs Assets/Scripts/C1/C1GameBootstrap.cs Assets/Tests/EditMode/C1GameBootstrapTests.cs
git commit -m "feat(试玩): 添加跳跃高度调节面板"
```

### 任务 5：回归验证与 WebGL 验收

**文件：** 无新增。

- [ ] **步骤 1：同步验证副本，保留其 `C1VerifyAndBuild.cs`，运行全量 EditMode 测试。**

```bash
cp -R Assets Packages ProjectSettings Temp/C1VerifyProject/
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath "$(pwd)/Temp/C1VerifyProject" -runTests -testPlatform EditMode -testResults "$(pwd)/Temp/editmode-results.xml" -logFile "$(pwd)/Temp/C1VerifyProject/editmode.log"
```

预期：`Temp/editmode-results.xml` 包含 `failed="0"`。

- [ ] **步骤 2：构建 WebGL。**

```bash
C1_WEBGL_OUTPUT="$(pwd)/Builds/C1WebGL" /Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$(pwd)" -executeMethod PaperGame.C1.Editor.C1WebGLBuilder.BuildFromCommandLine -logFile "$(pwd)/Temp/c1-webgl-build.log"
```

预期：命令退出码为 `0`，`Builds/C1WebGL/Build/C1WebGL.wasm.gz` 的修改时间更新。

- [ ] **步骤 3：浏览器验收。**

- [ ] 确认 7 条平台与草图的左下起点、右上旗帜相符，平台是细线而非粗条。
- [ ] 确认滑块默认显示 `1.4` 且提示不可达；调至 `4.1` 时提示可达。
- [ ] 确认滑动只影响角色跳跃，不改变重启后的关卡平台坐标。

- [ ] **步骤 4：提交最终验证相关变更（如有）。**

```bash
git add Assets Builds/C1WebGL
git commit -m "test(关卡): 验证保真草图试玩流程"
```

## 自检

- 规格中的保真坐标、`0.08` 厚度、不可玩诊断和跳跃滑块均有对应任务。
- 跳跃高度通过统一的 `C1JumpPhysics` 换算，UI 和控制器不各自维护公式。
- `C1Reachability` 只读取 `C1LevelDefinition`，不会改写平台或旗帜坐标。
- 计划中没有未定义类型：`C1ReachabilityResult` 和 `C1BlockedJump` 在任务 3 中实现，且只由任务 3 与任务 4 使用。

### 任务 6：固定纸张画布与旗帜比例

**文件：**
- 修改：`Assets/Scripts/C1/C1LevelDefinition.cs`
- 修改：`Assets/Scripts/C1/C1LevelLoader.cs`
- 修改：`Assets/Resources/C1Levels/level1.json`
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`
- 修改：`Assets/Tests/EditMode/C1CameraFramingTests.cs`
- 修改：`Assets/Tests/EditMode/C1LevelLoaderTests.cs`

- [ ] **步骤 1：先写失败测试，锁定画布而不是内容包围盒。**

```csharp
[Test]
public void Calculate_UsesExplicitCanvasBounds()
{
    var frame = C1CameraFraming.Calculate(new Bounds(new Vector3(16f, 10.41f), new Vector3(32f, 20.82f)), 16f / 9f, 0.7f);
    Assert.That(frame.Center, Is.EqualTo(new Vector2(16f, 10.41f)));
}
```

- [ ] **步骤 2：运行测试，确认当前 `C1LevelDefinition` 没有 `CanvasSize` 且 Bootstrap 仍以内容计算相机。**

- [ ] **步骤 3：为关卡定义增加 `CanvasSize`，JSON 解析 `canvas.width`、`canvas.height`；将 `ConfigureCamera` 改为 `new Bounds(level.CanvasSize * 0.5f, level.CanvasSize)`。**

- [ ] **步骤 4：将 JSON 的旗帜尺寸写入 `goal`，并让 `CreateGoal` 使用该尺寸创建旗杆、旗面与触发器。**

- [ ] **步骤 5：重跑测试，确认相机中心固定为纸张中心、旗帜不再使用固定大尺寸。**

- [ ] **步骤 6：提交。**

```bash
git add Assets/Scripts/C1 Assets/Resources/C1Levels/level1.json Assets/Tests/EditMode
git commit -m "fix(关卡): 固定纸张画布取景与旗帜比例"
```
