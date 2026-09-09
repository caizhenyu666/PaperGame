# C1 草图关卡与 JSON 配置设计

## 目标

将 C1 关卡改为 JSON 配置驱动，生成对应手绘草图的新关卡：细横线平台、高低错落的间隙、右侧长平台上的终点旗；同时新增左右屏幕边界与掉落失败结算，并删除代码中写死的旧三平台关卡。

## 范围

- 新增关卡 JSON 文件与加载器。
- 删除 `C1LevelDefinition.CreateDefault`。
- 左右隐形边界墙，与固定镜头视野边缘对齐，玩家无法走出屏幕。
- 掉落检测与失败面板（Restart 重新挑战）。
- 终点位置由 JSON 独立字段 `goalPosition` 描述，构建时在该位置生成结束触发器（终点旗）。
- 更新与新增测试；WebGL 浏览器验收。

不在本次范围内：多关卡切换 UI、关卡编辑器、背景美术、音效、移动端输入和 GitHub 推送。

## 关卡 JSON

路径：`Assets/Resources/C1Levels/level1.json`。`goalPosition` 为独立顶层字段；平台为细横线（厚度 0.2）。

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

物理可行性（moveSpeed 6、jumpSpeed 9、gravityScale 3：最高跳约 1.4、带高度差跳跃水平距离约 2.6）：相邻平台高度差 ≤1.1、水平间隙 ≤1.6，全部可稳定通过。

## 架构

### 加载

新增 `C1LevelLoader`：

- `Parse(string json, out string error)`：纯解析，不依赖 Resources，便于测试；JsonUtility DTO 转换为 `C1LevelDefinition`。
- `LoadDefault()`：`Resources.Load<TextAsset>("C1Levels/level1")` 后调用 `Parse`。
- 解析结果复用 `C1LevelDefinition.TryValidate` 校验。

### 构建

- `C1GameBootstrap` 的首次构建与 Restart 均改为 `C1LevelLoader.LoadDefault()`；加载或校验失败时记录错误且不构建。
- 终点旗沿用现有 `C1GoalFlag`（自带触发器碰撞体），生成在 `goalPosition`。

### 左右边界

固定镜头 framing 计算完成后，取视野半宽 `halfWidth = orthographicSize * aspect`，在 `center.x ± halfWidth` 各生成一个隐形静态 `BoxCollider2D`（厚度 1、高度 4 倍视野高），内侧面与视野边缘对齐。

### 掉落失败

- 新增 `C1OutOfBoundsWatcher`：`Configure(player, killY)`，killY = 视野下缘 − 0.5；每帧检查，玩家 y 低于 killY 且未通关/未掉落时调用 `player.Fall()` 并触发 `Fell` 事件。
- `C1PlayerController2D` 新增 `IsFallen` 与 `Fall()`：速度归零、刚体转 Static、拒绝输入与跳跃，模式与 `Complete()` 一致。
- 终点触发器拒绝已掉落玩家，避免双重结算。
- HUD 新增失败面板，样式与通关面板一致：标题 “You Fell!” + Restart 按钮，默认隐藏。

### 镜头

沿用现有 `C1CameraFraming` 固定全景镜头；边界墙与 kill 线均由 framing 结果派生。

## 异常与降级

- JSON 缺失或解析失败：记录明确错误，不生成关卡，避免静默失败。
- 关卡数据非法：复用现有 TryValidate 警告路径。
- 掉落后忽略输入与终点触发，防止重复结算。

## 测试与验收

自动测试覆盖：

- Loader：`Parse` 成功得到 7 个平台、出生点与终点正确；非法 JSON（缺字段、负尺寸、非有限坐标）返回错误。
- Bootstrap：默认构建生成 7 个平台；存在左右边界墙且位于视野边缘；模拟掉落后失败面板显示且玩家冻结；通关流程不受影响。
- Controller：`Fall()` 后拒绝输入与跳跃。

WebGL 验收覆盖：

- 开场同时看到全部平台、玩家与终点旗；镜头固定。
- 左右移动无法走出屏幕。
- 掉入间隙后显示失败面板，Restart 可重新挑战。
- Idle/Run/Jump 动画与朝向继续符合此前验收。
