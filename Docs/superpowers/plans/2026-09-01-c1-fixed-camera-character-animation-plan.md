# C1 固定镜头与角色动画实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法跟踪进度。本轮按用户要求不执行 Git commit。

**目标：** 固定单镜头展示完整 C1 关卡，并让工程中的 Idle、Run、Jump 精灵随角色物理状态播放。

**架构：** 角色根对象继续承载物理，子对象上的 `C1CharacterAnimator2D` 只处理画面。三张精灵集放入 Resources 并由导入器切帧；Bootstrap 一次性根据关卡边界配置相机，运行中不跟随。

**技术栈：** Unity 2022.3、C#、SpriteRenderer、TextureImporter、Unity Test Framework、WebGL。

---

## 文件结构

- 移动并修改：`Assets/Image/{idle,run,jump}.png` → `Assets/Resources/C1Character/`
- 创建：`Assets/Editor/C1CharacterSpritePostprocessor.cs`：透明精灵集的多帧导入配置。
- 创建：`Assets/Scripts/C1/C1CharacterAnimator2D.cs`：动画状态、帧推进与朝向。
- 创建：`Assets/Scripts/C1/C1CameraFraming.cs`：固定镜头边界计算。
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`：使用角色动画并配置固定镜头。
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`
- 创建：`Assets/Tests/EditMode/C1CharacterAnimatorTests.cs`
- 创建：`Assets/Tests/EditMode/C1CameraFramingTests.cs`

### 任务 1：透明资源与切帧

- [x] 使用 imagegen 将 `idle.png`、`jump.png` 棋盘格改为透明通道，保持角色、排列和尺寸不变。
- [x] 将三张图及 `.meta` 移入 `Assets/Resources/C1Character/`。
- [x] 创建 `C1CharacterSpritePostprocessor`：Idle 切 6 帧，Run/Jump 各切 8 帧；帧名使用 `idle_00` 等稳定顺序，pivot 为 `(0.5, 0.5)`，PPU 为 100。
- [x] Unity 重新导入后用 `Resources.LoadAll<Sprite>` 验证帧数分别为 6、8、8，且 idle/jump 纹理带 Alpha。

### 任务 2：角色动画状态（TDD）

- [x] 先创建失败测试，断言 `ResolveState(false, 0)` 为 Jump、`ResolveState(true, 1)` 为 Run、`ResolveState(true, 0)` 为 Idle，并验证负速度翻转。
- [x] 运行测试，确认因 `C1CharacterAnimator2D` 缺失而失败。
- [x] 实现 `C1CharacterAnimator2D`：公开 `Configure`、`ResolveState`、`Tick` 测试入口，Jump 优先，Run 阈值为 `0.05f`，Idle/Run 循环，Jump 停留在最后一帧。
- [x] 运行测试，确认动画状态、帧推进、空帧降级和左右翻转通过。

### 任务 3：固定全景镜头（TDD）

- [x] 先创建失败测试：给定关卡 bounds 与 16:9 aspect，计算结果必须完整包含 bounds；Bootstrap 构建后相机不得包含 `C1FollowCamera`。
- [x] 运行测试，确认固定镜头 API 缺失或跟随组件仍存在导致失败。
- [x] 实现 `C1CameraFraming.Calculate(bounds, aspect, margin)`，正交尺寸取纵向半高与横向半宽/aspect 的较大值。
- [x] 修改 Bootstrap：移除跟随组件，构建时把相机放在 bounds 中心并仅配置一次。
- [x] 运行测试，确认全景镜头测试通过。

### 任务 4：Bootstrap 集成与验证

- [x] 先扩展 Bootstrap 失败测试，断言玩家拥有视觉子对象、`C1CharacterAnimator2D` 和非空 Idle/Run/Jump 帧。
- [x] 修改 `CreatePlayer`，根对象仅保留刚体与碰撞体；视觉子对象加载 `Resources/C1Character` 三组帧并按角色高度缩放。
- [x] 资源缺失时使用纯色占位 Sprite 并记录错误，保证游戏仍可运行。
- [x] 运行全部 EditMode 测试，预期全部通过且无编译错误（24/24 通过）。
- [ ] 构建 WebGL 并在浏览器验证：首次画面显示完整关卡和终点；镜头不移动；空格跳跃切 Jump；移动切 Run；静止切 Idle。

