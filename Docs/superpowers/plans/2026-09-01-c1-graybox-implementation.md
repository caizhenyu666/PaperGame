# C1 Unity 游戏灰盒实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在空 Unity 2022.3 场景中运行时生成一个离线灰盒关卡，让玩家使用 A/D、方向键和空格完成跑、跳、落地及碰旗帜结算闭环。

**架构：** `C1GameBootstrap` 从内置 `C1LevelDefinition` 构建平台、角色、目标和镜头，不依赖手工 Prefab 或服务端。`C1PlayerController2D` 负责物理与输入，`C1GoalFlag` 负责一次性完成事件，`C1FollowCamera` 只跟随玩家 X 轴；组件均暴露最小测试入口。

**技术栈：** Unity 2022.3.62f1、C#、Rigidbody2D、Collider2D、Unity Test Framework 1.1.33、WebGL。

---

## 文件结构

- 创建：`Assets/Scripts/C1/C1LevelDefinition.cs`：C1 mock 平台与目标的数据结构。
- 创建：`Assets/Scripts/C1/C1PlayerController2D.cs`：键盘输入、水平移动、接地检测、跳跃和完成锁定。
- 创建：`Assets/Scripts/C1/C1GoalFlag.cs`：只触发一次的终点检测。
- 创建：`Assets/Scripts/C1/C1FollowCamera.cs`：固定 Y、平滑跟随 X。
- 创建：`Assets/Scripts/C1/C1GameBootstrap.cs`：运行时生成灰盒和完成提示。
- 创建：`Assets/Scripts/C1/PaperGame.C1.asmdef`：运行时代码程序集。
- 创建：`Assets/Tests/EditMode/C1PlayerControllerTests.cs`：移动、跳跃、完成状态测试。
- 创建：`Assets/Tests/EditMode/C1GoalFlagTests.cs`：旗帜幂等测试。
- 创建：`Assets/Tests/EditMode/C1BootstrapTests.cs`：关卡构建与非法定义测试。
- 创建：`Assets/Tests/EditMode/PaperGame.C1.EditModeTests.asmdef`：EditMode 测试程序集。

### 任务 1：建立 C1 数据契约与关卡验证

**文件：**
- 创建：`Assets/Scripts/C1/C1LevelDefinition.cs`
- 创建：`Assets/Scripts/C1/PaperGame.C1.asmdef`
- 创建：`Assets/Tests/EditMode/C1BootstrapTests.cs`
- 创建：`Assets/Tests/EditMode/PaperGame.C1.EditModeTests.asmdef`

- [ ] **步骤 1：编写失败测试**

测试构造含两个平台和一个目标的定义并断言有效；空平台、零尺寸平台和非有限坐标必须无效。

```csharp
[Test]
public void Validate_RejectsMissingPlatformsAndInvalidCoordinates()
{
    Assert.That(new C1LevelDefinition().TryValidate(out _), Is.False);
    var invalid = C1LevelDefinition.CreateDefault();
    invalid.platforms[0].size.x = 0f;
    Assert.That(invalid.TryValidate(out _), Is.False);
}
```

- [ ] **步骤 2：运行测试并确认失败**

运行：Unity batchmode EditMode 测试，筛选 `C1BootstrapTests`。

预期：编译失败，提示 `C1LevelDefinition` 不存在。

- [ ] **步骤 3：实现最小数据结构**

```csharp
[Serializable]
public sealed class C1PlatformDefinition
{
    public Vector2 position;
    public Vector2 size;
}

[Serializable]
public sealed class C1LevelDefinition
{
    public C1PlatformDefinition[] platforms = Array.Empty<C1PlatformDefinition>();
    public Vector2 playerStart;
    public Vector2 goalPosition;

    public bool TryValidate(out string error)
    {
        if (platforms == null || platforms.Length == 0)
        {
            error = "Level requires at least one platform.";
            return false;
        }

        if (!IsFinite(playerStart) || !IsFinite(goalPosition))
        {
            error = "Player and goal positions must be finite.";
            return false;
        }

        foreach (var platform in platforms)
        {
            if (platform == null || !IsFinite(platform.position) ||
                !IsFinite(platform.size) || platform.size.x <= 0f || platform.size.y <= 0f)
            {
                error = "Every platform requires finite coordinates and a positive size.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    public static C1LevelDefinition CreateDefault()
    {
        return new C1LevelDefinition
        {
            playerStart = new Vector2(-7f, -2.25f),
            goalPosition = new Vector2(8f, 0.4f),
            platforms = new[]
            {
                new C1PlatformDefinition { position = new Vector2(-5f, -3f), size = new Vector2(7f, 1f) },
                new C1PlatformDefinition { position = new Vector2(0.5f, -1.6f), size = new Vector2(3f, 0.6f) },
                new C1PlatformDefinition { position = new Vector2(6f, -0.5f), size = new Vector2(6f, 0.8f) }
            }
        };
    }

    private static bool IsFinite(Vector2 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) && !float.IsInfinity(value.y);
    }
}
```

- [ ] **步骤 4：运行测试并确认通过**

预期：`C1BootstrapTests.Validate_*` 全部通过。

### 任务 2：实现可测试的跑跳控制器

**文件：**
- 创建：`Assets/Scripts/C1/C1PlayerController2D.cs`
- 创建：`Assets/Tests/EditMode/C1PlayerControllerTests.cs`

- [ ] **步骤 1：编写失败测试**

覆盖水平速度、完成后冻结和仅落地可跳三项行为。测试创建带 `Rigidbody2D`、`BoxCollider2D` 的玩家与静态地面，调用 `Physics2D.SyncTransforms()` 后执行控制器公开方法。

```csharp
[Test]
public void ApplyHorizontalInput_SetsExpectedVelocity()
{
    controller.ApplyHorizontalInput(1f);
    Assert.That(body.velocity.x, Is.EqualTo(controller.MoveSpeed).Within(0.001f));
}

[Test]
public void Complete_StopsMovementAndRejectsInput()
{
    controller.ApplyHorizontalInput(1f);
    controller.Complete();
    controller.ApplyHorizontalInput(-1f);
    Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
}
```

- [ ] **步骤 2：运行测试并确认失败**

预期：编译失败，提示 `C1PlayerController2D` 不存在。

- [ ] **步骤 3：实现最小控制器**

```csharp
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class C1PlayerController2D : MonoBehaviour
{
    public float MoveSpeed => moveSpeed;
    public bool IsCompleted { get; private set; }
    public bool IsGrounded { get; private set; }

    public void ApplyHorizontalInput(float input)
    {
        if (IsCompleted) return;
        body.velocity = new Vector2(Mathf.Clamp(input, -1f, 1f) * moveSpeed, body.velocity.y);
    }

    public bool TryJump()
    {
        RefreshGrounded();
        if (IsCompleted || !IsGrounded) return false;
        body.velocity = new Vector2(body.velocity.x, jumpSpeed);
        IsGrounded = false;
        return true;
    }

    public void Complete()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        body.velocity = Vector2.zero;
        body.bodyType = RigidbodyType2D.Static;
    }
}
```

`Update` 读取 `Input.GetAxisRaw("Horizontal")` 和 `Input.GetKeyDown(KeyCode.Space)`；`FixedUpdate` 刷新接地状态。刚体锁定 Z 旋转，重力和速度使用序列化常量。

- [ ] **步骤 4：运行测试并确认通过**

预期：水平速度、落地跳跃、空中拒绝跳跃和完成冻结测试全部通过。

### 任务 3：实现一次性旗帜与完成闭环

**文件：**
- 创建：`Assets/Scripts/C1/C1GoalFlag.cs`
- 创建：`Assets/Tests/EditMode/C1GoalFlagTests.cs`

- [ ] **步骤 1：编写失败测试**

```csharp
[Test]
public void Reach_NotifiesExactlyOnce()
{
    var calls = 0;
    goal.Reached += _ => calls++;
    Assert.That(goal.TryReach(player), Is.True);
    Assert.That(goal.TryReach(player), Is.False);
    Assert.That(calls, Is.EqualTo(1));
    Assert.That(player.IsCompleted, Is.True);
}
```

- [ ] **步骤 2：运行测试并确认失败**

预期：编译失败，提示 `C1GoalFlag` 不存在。

- [ ] **步骤 3：实现最小旗帜组件**

```csharp
public sealed class C1GoalFlag : MonoBehaviour
{
    public event Action<C1PlayerController2D> Reached;
    public bool HasBeenReached { get; private set; }

    public bool TryReach(C1PlayerController2D player)
    {
        if (player == null || HasBeenReached) return false;
        HasBeenReached = true;
        player.Complete();
        Reached?.Invoke(player);
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other) { TryReach(other.GetComponent<C1PlayerController2D>()); }
}
```

- [ ] **步骤 4：运行测试并确认通过**

预期：空玩家被拒绝，首次到达成功，后续到达不重复通知。

### 任务 4：运行时生成关卡、镜头与提示

**文件：**
- 创建：`Assets/Scripts/C1/C1FollowCamera.cs`
- 创建：`Assets/Scripts/C1/C1GameBootstrap.cs`
- 修改：`Assets/Tests/EditMode/C1BootstrapTests.cs`

- [ ] **步骤 1：扩展失败测试**

```csharp
[Test]
public void Build_CreatesOneGroundPerPlatformAndSingleGoal()
{
    Assert.That(bootstrap.Build(C1LevelDefinition.CreateDefault()), Is.True);
    Assert.That(bootstrap.GroundCount, Is.EqualTo(3));
    Assert.That(bootstrap.Player, Is.Not.Null);
    Assert.That(bootstrap.Goal, Is.Not.Null);
}
```

另测非法定义返回 `false` 且不创建部分对象。

- [ ] **步骤 2：运行测试并确认失败**

预期：编译失败，提示 `C1GameBootstrap` 不存在。

- [ ] **步骤 3：实现关卡构建**

`C1GameBootstrap.Build` 先验证定义，再用 `SpriteRenderer`、`BoxCollider2D` 和纯色 `Texture2D.whiteTexture` Sprite 创建：

- 深灰平台：静态碰撞体；
- 蓝色玩家：动态刚体与控制器；
- 红色旗杆和旗面：同一根对象上的 trigger collider 与 `C1GoalFlag`；
- 正交相机：挂载 `C1FollowCamera`，固定 Y 与 Z。

组件用 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` 确保空场景自动出现一个 Bootstrap。`OnGUI` 显示操作提示；完成后显示“到达终点！”和重新开始按钮。

- [ ] **步骤 4：实现镜头最小行为**

```csharp
public sealed class C1FollowCamera : MonoBehaviour
{
    public void SetTarget(Transform value) { target = value; }

    private void LateUpdate()
    {
        if (target == null) return;
        var position = transform.position;
        position.x = Mathf.SmoothDamp(position.x, target.position.x, ref velocityX, smoothTime);
        position.y = fixedY;
        position.z = -10f;
        transform.position = position;
    }
}
```

- [ ] **步骤 5：运行全部 EditMode 测试**

预期：C1 三个测试类全部通过，无编译错误和异常日志。

### 任务 5：编辑器与 WebGL 验收

**文件：**
- 修改：`ProjectSettings/EditorBuildSettings.asset`（若 SampleScene 尚未加入构建）

- [ ] **步骤 1：运行 Unity 脚本编译**

以 Unity 2022.3.62f1 batchmode 打开项目并退出。

预期：进程退出码为 0，日志无 C# 编译错误。

- [ ] **步骤 2：运行全量 EditMode 测试**

预期：所有 C1 测试通过。

- [ ] **步骤 3：执行 WebGL 构建**

构建 `Assets/Scenes/SampleScene.unity` 到临时输出目录。

预期：生成 `index.html`、`Build/` 和 `TemplateData/`，构建日志无失败。

- [ ] **步骤 4：人工灰盒验收**

在编辑器 Play Mode 或本地 WebGL 页面确认：

- A/D 与左右方向键可移动；
- 空格只能在落地时跳跃；
- 可跨越默认平台并落地；
- 镜头水平跟随；
- 碰红旗后只结算一次并冻结控制。

本轮按用户要求不处理 GitHub 远程与推送；代码验证完成后再单独决定是否提交。
