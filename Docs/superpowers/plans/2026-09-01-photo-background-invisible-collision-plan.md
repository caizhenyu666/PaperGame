# 照片背景与透明碰撞关卡实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 将用户照片拉正为横屏背景，Unity 只在照片横线和旗帜位置创建不可见碰撞体与触发器。

**架构：** 本地 Python/OpenCV 工具负责纸张四角检测和透视校正，输出背景 PNG；关卡 JSON 使用校正图像的像素坐标保存平台端点和终点区域。Unity 使用一个背景 Sprite 显示照片，将像素坐标统一映射为世界坐标，并创建不含 Renderer 的物理对象。

**技术栈：** Python 3、OpenCV、NumPy、Unity 2022.3、C#、Unity Test Framework。

---

## 文件结构

- 创建：`Tools/PaperLevel/requirements.txt`——固定 OpenCV 与 NumPy 工具依赖。
- 创建：`Tools/PaperLevel/rectify_paper.py`——自动四角检测、手动四角参数与透视校正 CLI。
- 创建：`Tools/PaperLevel/tests/test_rectify_paper.py`——合成四边形的校正测试。
- 创建：`Assets/Resources/C1Levels/level1-background.png`——当前样本的拉正背景。
- 修改：`Assets/Resources/C1Levels/level1.json`——背景路径、像素画布、平台端点与终点区域。
- 修改：`Assets/Scripts/C1/C1LevelDefinition.cs`——像素画布、背景、线段与终点区域模型。
- 修改：`Assets/Scripts/C1/C1LevelLoader.cs`——解析新版 JSON。
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`——背景 Sprite、透明平台和透明终点。
- 修改：`Assets/Tests/EditMode/C1LevelLoaderTests.cs`——新版 JSON 契约测试。
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`——背景存在且物理对象不可见。

### 任务 1：建立照片拉正工具

**文件：**
- 创建：`Tools/PaperLevel/requirements.txt`
- 创建：`Tools/PaperLevel/rectify_paper.py`
- 创建：`Tools/PaperLevel/tests/test_rectify_paper.py`

- [ ] **步骤 1：声明依赖。**

```text
opencv-python-headless==4.10.0.84
numpy==2.1.0
```

- [ ] **步骤 2：先写失败测试。**

```python
def test_rectify_maps_detected_paper_to_output_corners(tmp_path):
    source = make_synthetic_paper((40, 30), (360, 50), (380, 270), (20, 250))
    result, corners = rectify(source)
    assert result.shape[1] > result.shape[0]
    assert_corner_order(corners)
    assert result[5, 5].mean() > 240
```

- [ ] **步骤 3：运行测试验证失败。**

运行：`python3 -m unittest Tools/PaperLevel/tests/test_rectify_paper.py -v`

预期：因 `rectify_paper` 模块不存在而失败。

- [ ] **步骤 4：实现 `order_corners`、`detect_paper_corners`、`warp_paper` 和 CLI。**

```bash
python3 Tools/PaperLevel/rectify_paper.py INPUT OUTPUT
python3 Tools/PaperLevel/rectify_paper.py INPUT OUTPUT --corners "206,401;1451,401;1451,1211;206,1211"
```

自动模式按灰度、Gaussian Blur、Canny、外轮廓面积排序和四边形逼近检测纸张；无法获得四边形时退出码为 `2`，并输出 `NEEDS_CORNER_CONFIRMATION`。

- [ ] **步骤 5：重跑测试，确认通过。**

- [ ] **步骤 6：提交。**

```bash
git add Tools/PaperLevel
git commit -m "feat(图像): 添加纸张透视校正工具"
```

### 任务 2：生成当前样本背景和像素 JSON

**文件：**
- 创建：`Assets/Resources/C1Levels/level1-background.png`
- 修改：`Assets/Resources/C1Levels/level1.json`

- [ ] **步骤 1：先运行自动检测。**

```bash
python3 Tools/PaperLevel/rectify_paper.py "/var/folders/r5/pp1vpvrj5y11d7qln1bvm3k80000gn/T/codex-clipboard-89e267f7-3133-4fc6-a616-819e75e0cf4d.png" "Assets/Resources/C1Levels/level1-background.png"
```

- [ ] **步骤 2：若返回 `NEEDS_CORNER_CONFIRMATION`，使用当前样本人工核对的四角重新执行。**

```bash
python3 Tools/PaperLevel/rectify_paper.py INPUT "Assets/Resources/C1Levels/level1-background.png" --corners "206,401;1451,401;1451,1211;206,1211"
```

- [ ] **步骤 3：使用校正图像像素坐标写入 JSON。**

```json
{
  "backgroundImage": "C1Levels/level1-background",
  "canvas": { "width": 1245, "height": 810 },
  "playerStart": { "x": 110, "y": 626 },
  "platforms": [
    { "x1": 69, "y1": 640, "x2": 294, "y2": 640 },
    { "x1": 334, "y1": 640, "x2": 678, "y2": 640 },
    { "x1": 482, "y1": 551, "x2": 809, "y2": 551 },
    { "x1": 796, "y1": 473, "x2": 976, "y2": 473 },
    { "x1": 534, "y1": 394, "x2": 793, "y2": 394 },
    { "x1": 188, "y1": 317, "x2": 514, "y2": 317 },
    { "x1": 474, "y1": 229, "x2": 1173, "y2": 229 }
  ],
  "goalRegion": { "x": 1088, "y": 150, "width": 80, "height": 90 }
}
```

- [ ] **步骤 4：使用图片查看工具核对背景横屏、纸张边缘贴合和旗帜未被裁掉。**

- [ ] **步骤 5：提交。**

```bash
git add Assets/Resources/C1Levels/level1-background.png Assets/Resources/C1Levels/level1.json
git commit -m "feat(关卡): 添加拉正照片背景与像素坐标"
```

### 任务 3：解析像素坐标关卡契约

**文件：**
- 修改：`Assets/Scripts/C1/C1LevelDefinition.cs`
- 修改：`Assets/Scripts/C1/C1LevelLoader.cs`
- 修改：`Assets/Tests/EditMode/C1LevelLoaderTests.cs`

- [ ] **步骤 1：先写失败测试。**

```csharp
[Test]
public void LoadDefault_ReturnsPhotoBackgroundPixelLevel()
{
    var level = C1LevelLoader.LoadDefault();
    Assert.That(level.BackgroundResourcePath, Is.EqualTo("C1Levels/level1-background"));
    Assert.That(level.CanvasPixelSize, Is.EqualTo(new Vector2Int(1245, 810)));
    Assert.That(level.Platforms, Has.Length.EqualTo(7));
    Assert.That(level.Platforms[0].Start, Is.EqualTo(new Vector2(69f, 640f)));
}
```

- [ ] **步骤 2：运行测试，确认旧模型缺少背景和端点字段。**

- [ ] **步骤 3：实现 `C1PlatformDefinition.Start/End`、`BackgroundResourcePath`、`CanvasPixelSize` 和 `GoalRegion`；校验所有像素坐标位于画布范围内。**

- [ ] **步骤 4：重跑 Loader 测试，确认通过。**

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/C1LevelDefinition.cs Assets/Scripts/C1/C1LevelLoader.cs Assets/Tests/EditMode/C1LevelLoaderTests.cs
git commit -m "refactor(关卡): 使用照片像素坐标契约"
```

### 任务 4：显示背景并创建透明物理对象

**文件：**
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`

- [ ] **步骤 1：先写失败测试。**

```csharp
[Test]
public void Build_UsesPhotoBackgroundAndInvisiblePhysics()
{
    bootstrap.Build(C1LevelLoader.LoadDefault());
    var background = GameObject.Find("Paper Background");
    Assert.That(background.GetComponent<SpriteRenderer>(), Is.Not.Null);
    foreach (var collider in bootstrap.GetComponentsInChildren<BoxCollider2D>())
        if (collider.gameObject.name == "Ground")
            Assert.That(collider.GetComponent<SpriteRenderer>(), Is.Null);
    Assert.That(GameObject.Find("Goal Flag").GetComponent<SpriteRenderer>(), Is.Null);
}
```

- [ ] **步骤 2：运行测试，确认当前 Bootstrap 仍生成黑色平台和红色旗帜。**

- [ ] **步骤 3：创建背景 Sprite，按 `canvas.width / pixelsPerUnit` 和 `canvas.height / pixelsPerUnit` 建立世界画布；相机固定取景整个背景。**

- [ ] **步骤 4：平台端点经统一函数完成像素到世界坐标转换；创建旋转 `BoxCollider2D`，不添加 Renderer。**

- [ ] **步骤 5：`goalRegion` 创建透明 Trigger；删除旗杆和旗面生成代码。**

- [ ] **步骤 6：重跑 Bootstrap 与全量 EditMode 测试，确认通过。**

- [ ] **步骤 7：提交。**

```bash
git add Assets/Scripts/C1/C1GameBootstrap.cs Assets/Tests/EditMode/C1GameBootstrapTests.cs
git commit -m "feat(关卡): 使用照片背景和透明碰撞体"
```

### 任务 5：WebGL 与视觉验收

- [ ] **步骤 1：在验证副本运行全量 EditMode 测试，预期 `failed="0"`。**
- [ ] **步骤 2：从验证副本构建 WebGL，避免与已打开的主 Unity 冲突。**
- [ ] **步骤 3：确认游戏只显示拉正照片、角色与 HUD；没有额外黑线或红旗。**
- [ ] **步骤 4：确认角色脚底与照片横线重合，触碰照片旗帜区域时通关。**
- [ ] **步骤 5：确认改变 Game View 宽高比后背景和碰撞体不发生相对漂移。**

## 自检

- 规格中的拉正照片、透明地面、透明终点、统一像素坐标和手动四角兜底均有实现任务。
- Python 校正和 Unity 运行时通过 PNG + JSON 边界解耦。
- 所有新增行为都先有失败测试，再实现最少代码。
- 本阶段没有引入拍照 UI、上传接口或批量识别服务。
