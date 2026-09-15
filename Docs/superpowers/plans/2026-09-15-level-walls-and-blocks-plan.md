# 纸上关卡墙体与实体图形实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 服务端从手绘关卡中输出平台、阻挡墙和矩形实体图形，排除起终点标记的重复几何，并由 Unity 生成对应静态碰撞体。

**架构：** 服务端先识别圆圈和旗帜，再从同一墨迹图中提取闭合图形与水平/竖直线段，按「标记、实体图形、线段」优先级消除重复。JSON 在 v1.0 内增加可选 `walls`、`blocks` 数组；Unity 对缺失字段按空数组处理，并分别创建固定厚度线碰撞体与实际尺寸矩形碰撞体。

**技术栈：** Python 3、OpenCV、Pydantic v2、pytest、Unity 2022、C#、NUnit、Unity Test Framework、BoxCollider2D。

## 执行结果（2026-09-15）

- [x] 任务 1～5：服务端协议、检测、发布及 Unity 数据、碰撞实现已直接接入原工程，完成规格与质量审查。
- [x] 任务 6：完成跨仓库验证及工作区归属核对；未覆盖其他未提交改动，未推送或部署。

服务端回归为 440 通过、3 跳过、1 既存失败；排除既存 WebGL 竖屏模板测试后为 440 通过、3 跳过。用户短线照片独立回归通过，输出 10 个平台，包含短平台，可玩性为 `not_checked`。

原工程已由用户打开 Unity Editor，测试使用 `/private/tmp/c1-data-tests.M4pepM` 的独立文件副本，生产代码仍修改于原工程。最终数据解析及新增碰撞测试为 25/25 通过，结果为 `/private/tmp/c1-final.xml`。C1 全量为 98/101：剩余失败涉及原有脚底坐标断言、服务地址配置和首页 Prefab，不属于本次实现，未扩展修改范围。

墙沿端点方向旋转，局部碰撞尺寸为「线段长度 × GroundThickness」；实体以 region 中心定位，按实际宽高生成轴对齐矩形。计划下方代码片段是初始测试示意，最终断言以实际测试代码为准。

上线需要部署服务端并重启 API 与关卡 Worker，重新构建 Unity 客户端（含 WebGL）。任务缓存盐已升级为 `explicit-markers-v3`；需重新上传生成新关卡，旧保存 JSON 不会自动补出新几何。

---

## 文件结构

### 服务端仓库 `PaperGmaeSever`

- 修改 `app/services/level_detect.py`：新增墙和实体图形候选，执行标记优先的几何去重。
- 修改 `app/services/level_parser.py`：校验并发布 `walls`、`blocks`，在叠加图中绘制新几何。
- 修改 `app/level_contracts.py`：定义 `Wall`、`Block` 及向后兼容数组，升级任务缓存盐。
- 修改 `tests/test_level_detect.py`：覆盖短平台、竖墙、空心/实心图形和标记排除。
- 修改 `tests/test_level_contracts.py`：覆盖新协议与缓存隔离。
- 修改 `tests/test_level_parser.py`：覆盖新几何发布和无新增几何兼容。
- 修改 `tests/test_level_markers.py`：加入实拍短平台回归。
- 新增 `testdata/levels/real/short-platform.jpg`：用户提供的实拍短平台回归图；墙和实体图形使用可精确断言坐标的合成图覆盖。

### Unity 仓库 `PaperGame`

- 修改 `Assets/Scripts/C1/C1LevelDefinition.cs`：新增墙线段与矩形实体定义及校验。
- 修改 `Assets/Scripts/C1/C1LevelLoader.cs`：解析 API 的 `walls`、`blocks`，兼容旧 JSON。
- 修改 `Assets/Scripts/C1/C1GameBootstrap.cs`：生成墙和实体图形的静态 `BoxCollider2D`。
- 修改 `Assets/Tests/EditMode/C1LevelDefinitionTests.cs`：覆盖新增几何校验。
- 修改 `Assets/Tests/EditMode/C1LevelLoaderTests.cs`：覆盖新旧协议解析。
- 修改 `Assets/Tests/EditMode/C1GameBootstrapTests.cs`：覆盖碰撞体尺寸和无伤害语义。

## 任务 1：扩展服务端几何协议

**文件：**

- 修改：`PaperGmaeSever/app/level_contracts.py`
- 测试：`PaperGmaeSever/tests/test_level_contracts.py`

- [ ] **步骤 1：编写失败的协议测试**

增加包含墙与实体图形的有效关卡，并验证旧关卡缺失字段时得到空数组：

```python
def test_level_accepts_walls_and_blocks_and_defaults_old_payloads():
    data = valid_level_data()
    old = Level.model_validate(data)
    assert old.walls == [] and old.blocks == []

    data['walls'] = [{'id': 'wall_001', 'start': {'x': 40, 'y': 20},
                      'end': {'x': 40, 'y': 100}, 'confidence': .9}]
    data['blocks'] = [{'id': 'block_001', 'region': {
        'x': 100, 'y': 80, 'width': 60, 'height': 40, 'confidence': .88}}]
    level = Level.model_validate(data)
    assert level.walls[0].id == 'wall_001'
    assert level.blocks[0].region.width == 60
```

缓存测试断言新任务 ID 使用 `explicit-markers-v3`，不等于 v2 摘要。

- [ ] **步骤 2：验证测试因字段缺失而失败**

运行：

```bash
cd PaperGmaeSever
/private/tmp/papergame-marker-tests/bin/python -m pytest tests/test_level_contracts.py -q
```

预期：新增测试因 `Level` 没有 `walls`、`blocks` 而失败。

- [ ] **步骤 3：实现最少协议扩展**

在 `level_contracts.py` 增加：

```python
class Wall(Platform):
    pass

class Block(ContractModel):
    id: str = Field(min_length=1)
    region: Region

class Level(ContractModel):
    # 保留现有字段
    walls: List[Wall] = Field(default_factory=list)
    blocks: List[Block] = Field(default_factory=list)
```

扩展唯一 ID、画布边界、墙方向和实体矩形校验，并把任务盐改为 `b':explicit-markers-v3'`。

- [ ] **步骤 4：运行协议测试确认通过**

运行步骤 2 的命令，预期全部通过。

- [ ] **步骤 5：提交服务端协议**

```bash
git add app/level_contracts.py tests/test_level_contracts.py
git commit -m "feat(关卡协议): 添加墙与实体图形定义"
```

## 任务 2：识别短平台、墙和实体图形并排除标记

**文件：**

- 修改：`PaperGmaeSever/app/services/level_detect.py`
- 测试：`PaperGmaeSever/tests/test_level_detect.py`

- [ ] **步骤 1：编写失败的检测测试**

用 900 × 560 合成图覆盖：70 px 短横线、80 px 短竖线、空心矩形、实心多边形、圆圈、旗帜及穿过旗杆底部的长平台。核心断言：

```python
result = detect(source, tmp_path)
flag_pole_x = 760
assert len(result.platform_candidates) == 2
assert any(55 <= item.length < 110 for item in result.platform_candidates)
assert len(result.wall_candidates) == 1
assert len(result.block_candidates) == 2
assert all(abs(item.start.x - flag_pole_x) > 8 for item in result.wall_candidates)
```

- [ ] **步骤 2：运行新增测试确认失败**

```bash
cd PaperGmaeSever
/private/tmp/papergame-marker-tests/bin/python -m pytest tests/test_level_detect.py -q
```

预期：因 `DetectionResult` 缺少墙、实体图形并且短横线被 110 px 门槛过滤而失败。

- [ ] **步骤 3：增加候选类型和方向检测**

新增：

```python
@dataclass(frozen=True)
class BlockCandidate:
    id: str
    region: RegionCandidate
    confidence: float

@dataclass(frozen=True)
class DetectionResult:
    platform_candidates: List[PlatformCandidate]
    goal_candidates: List[GoalCandidate]
    ink_mask_path: Path
    start_candidates: List[PointCandidate] = field(default_factory=list)
    wall_candidates: List[PlatformCandidate] = field(default_factory=list)
    block_candidates: List[BlockCandidate] = field(default_factory=list)
```

将横线和竖线共用的拟合逻辑提取为定向线段检测。短线最小长度取 `max(55, short * .10)`；55–109 px 候选额外检查长宽比不小于 4.5、方向误差不超过 6°、连续墨迹覆盖率不低于 70%。

- [ ] **步骤 4：增加空心与实心图形检测**

对墨迹轮廓使用 `cv2.RETR_TREE`，结合外轮廓面积、闭合度、轮廓层级及外接矩形识别空心/实心多边形，输出轴对齐 `BlockCandidate`。忽略纸边、标记区域、过小区域及覆盖大部分画布的区域。

- [ ] **步骤 5：实现优先级去重**

先调用 `detect_markers`，再按以下逻辑过滤：

```python
blocks = reject_marker_overlaps(blocks, marker_regions)
platforms = reject_contained_or_marker_lines(platforms, blocks, marker_regions)
walls = reject_contained_or_marker_lines(walls, blocks, marker_regions)
```

长平台仅小部分穿过旗帜区域时保留；旗杆主体位于旗帜区域时从墙候选删除。

- [ ] **步骤 6：运行检测测试确认通过**

运行步骤 2 的命令，预期全部通过。

- [ ] **步骤 7：提交服务端检测**

```bash
git add app/services/level_detect.py tests/test_level_detect.py
git commit -m "feat(关卡识别): 支持短平台墙体和实体图形"
```

## 任务 3：发布墙与实体图形并加入实拍回归

**文件：**

- 修改：`PaperGmaeSever/app/services/level_parser.py`
- 修改：`PaperGmaeSever/tests/test_level_parser.py`
- 修改：`PaperGmaeSever/tests/test_level_markers.py`
- 新增：`PaperGmaeSever/testdata/levels/real/short-platform.jpg`
- 修改：`PaperGmaeSever/testdata/levels/real/README.md`

- [ ] **步骤 1：编写失败的解析与实拍测试**

扩展 parser stub，断言发布结果包含稳定排序及重编号后的几何：

```python
assert payload['result']['level']['walls'][0]['id'] == 'wall_001'
assert payload['result']['level']['blocks'][0]['id'] == 'block_001'
assert payload['result']['analysis']['playability'] == 'not_checked'
```

将用户提供的短平台图片复制为固定实拍样本，断言短线附近存在长度 55–109 px 的平台，且平台总数为 10。

- [ ] **步骤 2：运行解析测试确认失败**

```bash
cd PaperGmaeSever
/private/tmp/papergame-marker-tests/bin/python -m pytest tests/test_level_parser.py tests/test_level_markers.py -q
```

预期：新字段尚未发布或实拍短平台仍缺失，测试失败。

- [ ] **步骤 3：发布并校验新增几何**

在 parser 中把 `wall_candidates`、`block_candidates` 转为协议字段，按 `(y, x)` 稳定排序后编号。扩展几何边界、重复项和墨迹覆盖检查；可疑新增几何被忽略，不改变起终点唯一性失败规则，也不恢复可玩性分析。

- [ ] **步骤 4：扩展叠加图**

平台、墙、实体图形使用不同颜色绘制，并标注 `platform_*`、`wall_*`、`block_*`，便于服务端查看页诊断。

- [ ] **步骤 5：运行解析与主回归**

```bash
/private/tmp/papergame-marker-tests/bin/python -m pytest tests/test_level_parser.py tests/test_level_markers.py tests/test_level_contracts.py tests/test_level_detect.py -q
/private/tmp/papergame-marker-tests/bin/python -m pytest -q --ignore=tests/test_motion_2d.py
```

预期：相关测试与后端主回归全部通过。

- [ ] **步骤 6：提交服务端发布链路**

```bash
git add app/services/level_parser.py tests/test_level_parser.py tests/test_level_markers.py testdata/levels/real
git commit -m "feat(关卡生成): 发布墙体与实体碰撞数据"
```

## 任务 4：扩展 Unity 关卡定义和加载器

**文件：**

- 修改：`PaperGame/Assets/Scripts/C1/C1LevelDefinition.cs`
- 修改：`PaperGame/Assets/Scripts/C1/C1LevelLoader.cs`
- 修改：`PaperGame/Assets/Tests/EditMode/C1LevelDefinitionTests.cs`
- 修改：`PaperGame/Assets/Tests/EditMode/C1LevelLoaderTests.cs`

- [ ] **步骤 1：编写失败的加载与兼容测试**

构造包含 `walls`、`blocks` 的 API 响应，断言：

```csharp
Assert.That(level.Walls.Length, Is.EqualTo(1));
Assert.That(level.Blocks.Length, Is.EqualTo(1));
Assert.That(level.Blocks[0].Region, Is.EqualTo(new Rect(500f, 280f, 100f, 70f)));
```

保留旧 JSON 测试并增加 `Walls`、`Blocks` 为空数组的断言。

- [ ] **步骤 2：运行 EditMode 测试确认失败**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" -runTests -testPlatform EditMode -testFilter "PaperGame.C1.Tests.C1LevelDefinitionTests;PaperGame.C1.Tests.C1LevelLoaderTests" -testResults /private/tmp/papergame-walls-loader.xml -quit
```

预期：编译或断言因新增类型和属性缺失而失败。

- [ ] **步骤 3：实现 Unity 数据定义**

增加 `C1WallDefinition` 线段类型和 `C1BlockDefinition` 矩形类型；`C1LevelDefinition` 的数组属性默认 `Array.Empty<T>()`。`TryValidate` 校验端点、矩形边界、正尺寸及主要方向。

- [ ] **步骤 4：实现新旧 JSON 解析**

在 `ApiLevelJson` 增加可空数组：

```csharp
public ApiPlatformJson[] walls;
public ApiBlockJson[] blocks;
```

数组为 `null` 时创建空数组；存在时映射到新定义。不得覆盖 `C1LevelLoader.cs` 中用户已有的无关修改。

- [ ] **步骤 5：运行加载测试确认通过**

运行步骤 2 的命令，读取 XML 并确认失败数为 0。

- [ ] **步骤 6：提交 Unity 数据链路**

```bash
git add Assets/Scripts/C1/C1LevelDefinition.cs Assets/Scripts/C1/C1LevelLoader.cs Assets/Tests/EditMode/C1LevelDefinitionTests.cs Assets/Tests/EditMode/C1LevelLoaderTests.cs
git commit -m "feat(关卡协议): 加载墙体与实体图形"
```

## 任务 5：生成 Unity 静态碰撞体

**文件：**

- 修改：`PaperGame/Assets/Scripts/C1/C1GameBootstrap.cs`
- 修改：`PaperGame/Assets/Tests/EditMode/C1GameBootstrapTests.cs`

- [ ] **步骤 1：编写失败的碰撞体测试**

构造带 1 个墙和 1 个实体图形的关卡，断言生成对象数量、旋转和尺寸：

```csharp
var wall = GameObject.Find("Wall").GetComponent<BoxCollider2D>();
Assert.That(wall.size.y, Is.GreaterThan(wall.size.x));

var block = GameObject.Find("Block").GetComponent<BoxCollider2D>();
Assert.That(block.size.x, Is.EqualTo(100f / C1LevelSpace.PixelsPerUnit).Within(.01f));
Assert.That(block.size.y, Is.EqualTo(70f / C1LevelSpace.PixelsPerUnit).Within(.01f));
Assert.That(block.isTrigger, Is.False);
```

- [ ] **步骤 2：运行 Bootstrap 测试确认失败**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" -runTests -testPlatform EditMode -testFilter PaperGame.C1.Tests.C1GameBootstrapTests -testResults /private/tmp/papergame-walls-bootstrap.xml -quit
```

预期：找不到 `Wall`、`Block` 对象，测试失败。

- [ ] **步骤 3：实现墙和实体图形生成**

复用线段坐标转换逻辑创建墙：固定厚度、按方向旋转的静态 `BoxCollider2D`。按矩形中心与实际宽高创建实体图形碰撞体。对象不添加触发器、伤害或失败组件。

- [ ] **步骤 4：运行 Bootstrap 测试确认通过**

运行步骤 2 的命令，读取 XML 并确认失败数为 0。

- [ ] **步骤 5：提交 Unity 碰撞生成**

```bash
git add Assets/Scripts/C1/C1GameBootstrap.cs Assets/Tests/EditMode/C1GameBootstrapTests.cs
git commit -m "feat(关卡玩法): 生成墙与实体碰撞体"
```

## 任务 6：跨仓库完整验证

**文件：**

- 检查：上述所有实现和测试文件。

- [ ] **步骤 1：运行服务端完整回归**

```bash
cd PaperGmaeSever
/private/tmp/papergame-marker-tests/bin/python -m pytest -q --ignore=tests/test_motion_2d.py
git diff --check
```

预期：pytest 无失败，Git 差异无空白错误。

- [ ] **步骤 2：运行 Unity C1 EditMode 回归**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame" -runTests -testPlatform EditMode -testFilter PaperGame.C1.Tests -testResults /private/tmp/papergame-walls-all.xml -quit
```

预期：XML 中失败数为 0。

- [ ] **步骤 3：核对工作区归属**

分别运行 `git status --short` 和 `git diff --check`，确认没有覆盖或提交任务开始前已存在的用户改动。

- [ ] **步骤 4：记录部署要求**

交付说明中明确：部署服务端并重启 API 与关卡 Worker；Unity 重新构建 WebGL；v3 任务 ID 自动绕过旧识别缓存。
