# C1 草图关卡与 JSON 配置实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法跟踪进度。本轮按用户要求不执行 Git commit。
>
> **环境注意：** 用户的 Unity 编辑器正占用主工程，所有 Unity 批量命令必须在副本 `Temp/C1VerifyProject` 上运行（见“验证命令”）。

**目标：** 关卡改为 JSON 配置驱动，生成草图样式的细线平台关卡；新增左右屏幕边界与掉落失败面板；删除写死的旧默认关卡。

**架构：** `C1LevelLoader` 用 JsonUtility 解析 `Resources/C1Levels/level1.json`（`goalPosition` 独立字段生成终点触发器）；Bootstrap 依据固定镜头 framing 派生隐形边界墙与掉落 kill 线；`C1OutOfBoundsWatcher` 触发 `Fall()` 与失败面板。

**技术栈：** Unity 2022.3、C#、JsonUtility、Unity Test Framework、WebGL。

---

## 文件结构

- 创建：`Assets/Resources/C1Levels/level1.json`（+ `.meta`）：草图关卡数据。
- 创建：`Assets/Scripts/C1/C1LevelLoader.cs`：JSON 解析与默认关卡加载。
- 创建：`Assets/Scripts/C1/C1OutOfBoundsWatcher.cs`：掉落检测。
- 修改：`Assets/Scripts/C1/C1LevelDefinition.cs`：删除 `CreateDefault`。
- 修改：`Assets/Scripts/C1/C1PlayerController2D.cs`：新增 `IsFallen`/`Fall()`。
- 修改：`Assets/Scripts/C1/C1GoalFlag.cs`：拒绝已掉落玩家。
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`：JSON 构建、边界墙、watcher、失败面板。
- 修改：`Assets/Tests/EditMode/C1LevelDefinitionTests.cs`：移除 CreateDefault 依赖。
- 创建：`Assets/Tests/EditMode/C1LevelLoaderTests.cs`
- 修改：`Assets/Tests/EditMode/C1PlayerControllerTests.cs`、`C1GoalFlagTests.cs`、`C1GameBootstrapTests.cs`

## 验证命令（副本工程）

因主工程被编辑器锁定，统一用副本验证：

```bash
cd "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame"
rm -rf Temp/C1VerifyProject/Assets Temp/C1VerifyProject/Packages Temp/C1VerifyProject/ProjectSettings
cp -R Assets Packages ProjectSettings Temp/C1VerifyProject/
cp Temp/C1VerifyProject/Assets/Editor/C1VerifyAndBuild.cs Temp/C1VerifyProject/Assets/Editor/ 2>/dev/null
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics \
  -projectPath "$(pwd)/Temp/C1VerifyProject" -runTests -testPlatform EditMode \
  -testResults "$(pwd)/Temp/editmode-results.xml" -logFile "$(pwd)/Temp/C1VerifyProject/editmode.log"
```

预期：`Temp/editmode-results.xml` 中 `failed="0"`。

---

### 任务 1：关卡 JSON 与加载器（TDD）

**文件：**
- 创建：`Assets/Resources/C1Levels/level1.json` 与 `.meta`
- 创建：`Assets/Tests/EditMode/C1LevelLoaderTests.cs`
- 创建：`Assets/Scripts/C1/C1LevelLoader.cs`

- [ ] 创建 `level1.json`，内容如下（终点为独立字段 `goalPosition`）：

```json
{
  "playerStart": { "x": -12.5, "y": 0.8 },
  "goalPosition": { "x": 15.8, "y": 1.0 },
  "platforms": [
    { "x": -11.5, "y": -0.1, "width": 4.0, "height": 0.2 },
    { "x": -7.2,  "y": 0.05, "width": 3.5, "height": 0.2 },
    { "x": -3.4,  "y": 1.1,  "width": 2.5, "height": 0.2 },
    { "x": 0.4,   "y": 0.1,  "width": 2.0, "height": 0.2 },
    { "x": 4.1,   "y": 1.2,  "width": 2.5, "height": 0.2 },
    { "x": 8.1,   "y": 2.1,  "width": 2.5, "height": 0.2 },
    { "x": 13.5,  "y": 0.9,  "width": 6.0, "height": 0.2 }
  ]
}
```

- [ ] 为 `level1.json` 创建 `.meta`（TextScriptImporter，guid 用随机 32 位 hex）：

```yaml
fileFormatVersion: 2
guid: 4c1a5e6c7b8a49d0b2e3f6a7c8d90123
TextScriptImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
```

- [ ] 先创建失败测试 `C1LevelLoaderTests.cs`：

```csharp
using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1LevelLoaderTests
    {
        private const string ValidJson = @"{
            ""playerStart"": { ""x"": -12.5, ""y"": 0.8 },
            ""goalPosition"": { ""x"": 15.8, ""y"": 1.0 },
            ""platforms"": [
                { ""x"": -11.5, ""y"": -0.1, ""width"": 4.0, ""height"": 0.2 },
                { ""x"": 13.5, ""y"": 0.9, ""width"": 6.0, ""height"": 0.2 }
            ]
        }";

        [Test]
        public void Parse_ValidJsonBuildsLevelWithGoalAndPlatforms()
        {
            var level = C1LevelLoader.Parse(ValidJson, out var error);

            Assert.That(error, Is.Empty);
            Assert.That(level, Is.Not.Null);
            Assert.That(level.Platforms, Has.Length.EqualTo(2));
            Assert.That(level.PlayerStart, Is.EqualTo(new Vector2(-12.5f, 0.8f)));
            Assert.That(level.GoalPosition, Is.EqualTo(new Vector2(15.8f, 1f)));
            Assert.That(level.Platforms[0].Size, Is.EqualTo(new Vector2(4f, 0.2f)));
        }

        [Test]
        public void Parse_EmptyOrBrokenJsonReturnsError()
        {
            Assert.That(C1LevelLoader.Parse("", out var emptyError), Is.Null);
            Assert.That(emptyError, Is.Not.Empty);

            Assert.That(C1LevelLoader.Parse(@"{ ""platforms"": [] }", out var noPlatformError), Is.Null);
            Assert.That(noPlatformError, Is.Not.Empty);
        }

        [Test]
        public void Parse_InvalidPlatformSizeReturnsError()
        {
            var json = @"{
                ""playerStart"": { ""x"": 0, ""y"": 0 },
                ""goalPosition"": { ""x"": 1, ""y"": 0 },
                ""platforms"": [ { ""x"": 0, ""y"": 0, ""width"": -1, ""height"": 0.2 } ]
            }";

            Assert.That(C1LevelLoader.Parse(json, out var error), Is.Null);
            Assert.That(error, Does.Contain("positive"));
        }

        [Test]
        public void LoadDefault_ReturnsSevenPlatformSketchLevel()
        {
            var level = C1LevelLoader.LoadDefault();

            Assert.That(level, Is.Not.Null);
            Assert.That(level.Platforms, Has.Length.EqualTo(7));
            Assert.That(level.PlayerStart, Is.EqualTo(new Vector2(-12.5f, 0.8f)));
            Assert.That(level.GoalPosition, Is.EqualTo(new Vector2(15.8f, 1f)));
        }
    }
}
```

- [ ] 在副本工程运行测试，确认因 `C1LevelLoader` 缺失而编译失败（验证命令见上）。

- [ ] 实现 `C1LevelLoader.cs`：

```csharp
using System;
using UnityEngine;

namespace PaperGame.C1
{
    public static class C1LevelLoader
    {
        private const string DefaultResourcePath = "C1Levels/level1";

        public static C1LevelDefinition LoadDefault()
        {
            var asset = Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null)
            {
                Debug.LogError($"C1 level json is missing: Resources/{DefaultResourcePath}");
                return null;
            }

            var level = Parse(asset.text, out var error);
            if (level == null)
            {
                Debug.LogError($"C1 level json is invalid: {error}");
            }

            return level;
        }

        public static C1LevelDefinition Parse(string json, out string error)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Level json is empty.";
                return null;
            }

            LevelJson data;
            try
            {
                data = JsonUtility.FromJson<LevelJson>(json);
            }
            catch (ArgumentException)
            {
                error = "Level json is not valid json.";
                return null;
            }

            if (data == null || data.platforms == null || data.platforms.Length == 0)
            {
                error = "Level requires at least one platform.";
                return null;
            }

            var level = new C1LevelDefinition
            {
                PlayerStart = data.playerStart,
                GoalPosition = data.goalPosition,
                Platforms = new C1PlatformDefinition[data.platforms.Length]
            };

            for (var index = 0; index < data.platforms.Length; index++)
            {
                var platform = data.platforms[index];
                level.Platforms[index] = new C1PlatformDefinition(
                    new Vector2(platform.x, platform.y),
                    new Vector2(platform.width, platform.height));
            }

            return level.TryValidate(out error) ? level : null;
        }

        [Serializable]
        private sealed class LevelJson
        {
            public Vector2 playerStart;
            public Vector2 goalPosition;
            public PlatformJson[] platforms;
        }

        [Serializable]
        private sealed class PlatformJson
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }
    }
}
```

- [ ] 运行测试，确认任务 1 全部通过。

### 任务 2：玩家掉落状态与终点防护（TDD）

**文件：**
- 修改：`Assets/Scripts/C1/C1PlayerController2D.cs`
- 修改：`Assets/Scripts/C1/C1GoalFlag.cs`
- 修改：`Assets/Tests/EditMode/C1PlayerControllerTests.cs`、`Assets/Tests/EditMode/C1GoalFlagTests.cs`

- [ ] 先写失败测试。`C1PlayerControllerTests` 追加：

```csharp
[Test]
public void Fall_FreezesBodyAndRejectsInputAndJump()
{
    controller.Fall();

    Assert.That(controller.IsFallen, Is.True);
    controller.ApplyHorizontalInput(1f);
    Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
    Assert.That(controller.TryJump(), Is.False);
}
```

`C1GoalFlagTests` 追加：

```csharp
[Test]
public void TryReach_RejectsFallenPlayer()
{
    player.Fall();

    Assert.That(flag.TryReach(player), Is.False);
    Assert.That(flag.HasBeenReached, Is.False);
}
```

- [ ] 运行测试确认失败（`Fall`/`IsFallen` 缺失）。

- [ ] 实现：`C1PlayerController2D` 增加

```csharp
public bool IsFallen { get; private set; }

public void Fall()
{
    CacheComponents();
    if (IsCompleted || IsFallen)
    {
        return;
    }

    IsFallen = true;
    body.velocity = Vector2.zero;
    body.bodyType = RigidbodyType2D.Static;
}
```

并把 `ApplyHorizontalInput` 与 `TryJump` 的早退条件改为 `if (IsCompleted || IsFallen)`。

- [ ] `C1GoalFlag.TryReach` 早退条件改为 `if (player == null || HasBeenReached || player.IsFallen)`。

- [ ] 运行测试确认通过。

### 任务 3：越界监视器（TDD）

**文件：**
- 创建：`Assets/Scripts/C1/C1OutOfBoundsWatcher.cs`
- 创建：`Assets/Tests/EditMode/C1OutOfBoundsWatcherTests.cs`

- [ ] 先写失败测试：

```csharp
using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1OutOfBoundsWatcherTests
    {
        private GameObject playerObject;
        private C1PlayerController2D player;
        private GameObject watcherObject;
        private C1OutOfBoundsWatcher watcher;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("Player");
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<BoxCollider2D>();
            player = playerObject.AddComponent<C1PlayerController2D>();

            watcherObject = new GameObject("Watcher");
            watcher = watcherObject.AddComponent<C1OutOfBoundsWatcher>();
            watcher.Configure(player, -5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(watcherObject);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void Evaluate_BelowKillLineFallsPlayerAndNotifies()
        {
            var fell = 0;
            watcher.Fell += _ => fell++;
            playerObject.transform.position = new Vector3(0f, -6f, 0f);

            watcher.Evaluate();

            Assert.That(player.IsFallen, Is.True);
            Assert.That(fell, Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_AboveKillLineDoesNothing()
        {
            var fell = 0;
            watcher.Fell += _ => fell++;
            playerObject.transform.position = new Vector3(0f, 0f, 0f);

            watcher.Evaluate();

            Assert.That(player.IsFallen, Is.False);
            Assert.That(fell, Is.EqualTo(0));
        }
    }
}
```

- [ ] 运行测试确认失败（类型缺失）。

- [ ] 实现 `C1OutOfBoundsWatcher.cs`：

```csharp
using System;
using UnityEngine;

namespace PaperGame.C1
{
    public sealed class C1OutOfBoundsWatcher : MonoBehaviour
    {
        public event Action<C1PlayerController2D> Fell;

        private C1PlayerController2D player;
        private float killY = float.NegativeInfinity;

        public float KillY => killY;

        public void Configure(C1PlayerController2D target, float killYValue)
        {
            player = target;
            killY = killYValue;
        }

        private void LateUpdate()
        {
            Evaluate();
        }

        public void Evaluate()
        {
            if (player == null || player.IsCompleted || player.IsFallen)
            {
                return;
            }

            if (player.transform.position.y >= killY)
            {
                return;
            }

            player.Fall();
            Fell?.Invoke(player);
        }
    }
}
```

- [ ] 运行测试确认通过。

### 任务 4：Bootstrap 集成（JSON 构建、边界墙、失败面板）

**文件：**
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`
- 修改：`Assets/Scripts/C1/C1LevelDefinition.cs`（删除 `CreateDefault`）
- 修改：`Assets/Tests/EditMode/C1LevelDefinitionTests.cs`、`C1GameBootstrapTests.cs`

- [ ] 更新 `C1LevelDefinitionTests`：删除 `CreateDefault_ReturnsValidThreePlatformLevel`；新增私有辅助并替换其余用例中的 `CreateDefault()`：

```csharp
private static C1LevelDefinition CreateValidLevel()
{
    return new C1LevelDefinition
    {
        PlayerStart = new Vector2(-12.5f, 0.8f),
        GoalPosition = new Vector2(15.8f, 1f),
        Platforms = new[]
        {
            new C1PlatformDefinition(new Vector2(-11.5f, -0.1f), new Vector2(4f, 0.2f))
        }
    };
}
```

- [ ] 更新 `C1GameBootstrapTests`：所有 `C1LevelDefinition.CreateDefault()` 改为 `C1LevelLoader.LoadDefault()`；`GroundCount` 断言 3 改为 7；追加：

```csharp
[Test]
public void Build_CreatesScreenBoundsAlignedWithViewEdges()
{
    bootstrap.Build(C1LevelLoader.LoadDefault());

    var left = GameObject.Find("Left Bound");
    var right = GameObject.Find("Right Bound");
    Assert.That(left, Is.Not.Null);
    Assert.That(right, Is.Not.Null);

    var camera = Camera.main;
    var halfWidth = camera.orthographicSize * camera.aspect;
    var centerX = camera.transform.position.x;
    Assert.That(left.transform.position.x, Is.EqualTo(centerX - halfWidth - 0.5f).Within(0.01f));
    Assert.That(right.transform.position.x, Is.EqualTo(centerX + halfWidth + 0.5f).Within(0.01f));
}

[Test]
public void Build_FallingPlayerShowsFailurePanelAndFreezesPlayer()
{
    bootstrap.Build(C1LevelLoader.LoadDefault());

    bootstrap.Player.transform.position = new Vector3(0f, -100f, 0f);
    Object.FindObjectOfType<C1OutOfBoundsWatcher>().Evaluate();

    Assert.That(bootstrap.Player.IsFallen, Is.True);
    Assert.That(bootstrap.FailurePanel, Is.Not.Null);
    Assert.That(bootstrap.FailurePanel.activeSelf, Is.True);
}
```

- [ ] 修改 `C1LevelDefinition.cs`：删除 `CreateDefault` 方法。

- [ ] 修改 `C1GameBootstrap.cs`：
  - `Start` 与 Restart 按钮改为 `Build(C1LevelLoader.LoadDefault())`。
  - 新增 `public GameObject FailurePanel { get; private set; }` 与 `private C1OutOfBoundsWatcher fallWatcher;`。
  - `ConfigureCamera` 改为返回 `C1CameraFrame` 并 `out float aspect`；`Build` 中在其后调用：

```csharp
var framing = ConfigureCamera(level, out var aspect);
CreateScreenBounds(framing, aspect);
fallWatcher = generatedRoot.gameObject.AddComponent<C1OutOfBoundsWatcher>();
fallWatcher.Configure(Player, framing.Center.y - framing.OrthographicSize - 0.5f);
fallWatcher.Fell += HandlePlayerFell;
```

  - 新增：

```csharp
private void CreateScreenBounds(C1CameraFrame framing, float aspect)
{
    var halfWidth = framing.OrthographicSize * aspect;
    var height = framing.OrthographicSize * 4f;
    CreateBoundWall("Left Bound", framing.Center.x - halfWidth - 0.5f, framing.Center.y, height);
    CreateBoundWall("Right Bound", framing.Center.x + halfWidth + 0.5f, framing.Center.y, height);
}

private void CreateBoundWall(string name, float x, float y, float height)
{
    var wall = new GameObject(name);
    wall.transform.SetParent(generatedRoot, false);
    wall.transform.position = new Vector3(x, y, 0f);
    var collider = wall.AddComponent<BoxCollider2D>();
    collider.size = new Vector2(1f, height);
}

private void HandlePlayerFell(C1PlayerController2D player)
{
    if (FailurePanel != null)
    {
        FailurePanel.SetActive(true);
    }
}
```

  - HUD：把通关面板创建抽成 `CreateResultPanel(string title, Color titleColor)`，同时生成 `CompletionPanel`（"Goal Reached!"）与 `FailurePanel`（"You Fell!"，标题色 `new Color(0.55f, 0.1f, 0.1f)`），两者默认隐藏，Restart 按钮均调用 `Build(C1LevelLoader.LoadDefault())`。
  - `ClearGeneratedObjects`：`fallWatcher.Fell -= HandlePlayerFell; fallWatcher = null; FailurePanel = null;`。

- [ ] 同步副本工程并运行全部 EditMode 测试，预期全部通过（含新增用例）。

### 任务 5：WebGL 构建与浏览器验收

**文件：** 无新增；使用副本工程中的 `C1VerifyAndBuild.cs` 与 `serve_webgl.py`（已存在）。

- [ ] 同步副本工程（验证命令），并确认 `Temp/C1VerifyProject/Assets/Editor/C1VerifyAndBuild.cs` 存在（同步会删除它，需从主工程外备份恢复或重新创建）。

- [ ] 构建 WebGL 到 `Builds/C1WebGL`：

```bash
C1_WEBGL_OUTPUT="$(pwd)/Builds/C1WebGL" /Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath "$(pwd)/Temp/C1VerifyProject" \
  -executeMethod PaperGame.C1.Editor.C1VerifyAndBuild.Run \
  -logFile "$(pwd)/Temp/C1VerifyProject/build.log" -quit
```

预期：`build.log` 含 `C1VERIFY build finished`，`Builds/C1WebGL/index.html` 存在。

- [ ] 启动本地服务（若 8123 端口服务仍在运行则复用）：`python3 Temp/C1VerifyProject/serve_webgl.py 8123`。

- [ ] 浏览器验收（用 Browser 子代理，URL `http://127.0.0.1:8123/index.html`）：
  - 开场截图：7 根细线平台、玩家、终点旗同屏可见；镜头固定。
  - 按住 D 右行：角色 Run 动画朝右；走出平台掉入间隙后出现 “You Fell!” 面板；点击 Restart 重新开局。
  - 按住 A 左行到屏幕边缘：角色被边界墙挡住，无法出屏。
  - 静止时 Idle、跳跃时 Jump 动画正常。

- [ ] 更新 `Docs/superpowers/plans/2026-09-01-c1-sketch-level-json-plan.md` 与旧 C1 计划中对应复选框。
