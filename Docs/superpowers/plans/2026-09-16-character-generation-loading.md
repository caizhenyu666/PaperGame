# 角色生成 Loading 实现计划

> **面向 AI 代理的工作者：** 使用 `executing-plans` 在当前共享工作区执行。本项目存在用户尚未提交的角色界面改动，只修改本计划列出的文件，不自动提交。

**目标：** 为角色上传、生成和动画下载流程增加正式的全屏 Loading、动态阶段文字和可彻底丢弃当前任务的取消按钮。

**架构：** 由 `C1CharacterScreenLayout` 和现有编辑器生成器统一生成 Loading 节点与预制体；`C1CharacterSelection` 管理单一活动协程、遮罩状态和丢弃语义；`C1CharacterService` 暴露当前网络请求取消入口。正式插画与按钮使用透明 PNG Sprite。

**技术栈：** Unity 2022、C#、UGUI、UnityWebRequest、NUnit EditMode Tests、内置图像生成工具。

---

## 文件结构

- 创建：`Assets/Resources/C1CharacterUI/loading-character.png`——Loading 插画。
- 创建：`Assets/Resources/C1CharacterUI/cancel-generation.png`——取消按钮切图。
- 创建：`Assets/Editor/AIUI/Schemas/C1CharacterGenerationLoading.ui.json`——受约束 UI 结构说明。
- 修改：`Assets/Scripts/C1/UI/C1CharacterScreenLayout.cs`——生成 Loading 节点。
- 修改：`Assets/Editor/PaperGameCharacterPrefabGenerator.cs`——导入资源、生成预制体和 Loading 预览图。
- 修改：`Assets/Scripts/C1/C1CharacterService.cs`——跟踪并终止活动 `UnityWebRequest`。
- 修改：`Assets/Scripts/C1/UI/C1CharacterSelection.cs`——显示进度、动画、取消和清理状态。
- 修改：`Assets/Tests/EditMode/C1CharacterSelectionPrefabTests.cs`——验证预制体结构与默认状态。
- 修改：`Assets/Tests/EditMode/C1CharacterServiceTests.cs`——验证取消入口状态。
- 创建：`Docs/ui/character-generation-loading-preview.png`——1920×1080 实际 Unity 渲染预览。

### 任务 1：生成正式切图

- [ ] 使用项目中的 `green.png`、`chick.png` 作为角色参考，生成透明背景 Loading 插画。不得改变角色核心外形，不含文字、边框或整屏背景。
- [ ] 使用现有纸张按钮作为风格参考，生成透明背景红色撕纸按钮，准确包含「取消生成」。
- [ ] 将最终 PNG 保存到 `Assets/Resources/C1CharacterUI`，检查 Alpha 通道并记录最终生成提示词。

### 任务 2：先写预制体红灯测试

- [ ] 在 `C1CharacterSelectionPrefabTests` 添加测试，要求预制体包含 `Generation Loading/Blocker`、`Loading Art`、`Loading Status`、`Loading Tip`、`Cancel Generation`。
- [ ] 断言 Loading 根节点默认隐藏、Blocker 可接收 Raycast、取消节点具有 `Button`、两张 Sprite 均非空。
- [ ] 运行目标 EditMode 测试，确认因为节点尚不存在而失败。

### 任务 3：生成 Loading 预制体

- [ ] 创建 1920×1080 Schema，所有节点名称唯一且宽高为正数。
- [ ] 在 `C1CharacterScreenLayout.Build` 中增加全屏遮罩、中央插画、动态状态文本、随机趣味文本和取消按钮，最后把 Loading 根节点设为隐藏。
- [ ] 更新生成器并通过 `PaperGame/UI/Generate Character Selection` 重新生成独立业务预制体，不手工编辑 YAML。
- [ ] 运行预制体测试，确认结构测试通过。

### 任务 4：先写取消行为红灯测试

- [ ] 在 `C1CharacterServiceTests` 添加测试，要求 `CancelActiveRequests()` 在无活动请求时安全执行，并暴露 `HasActiveRequest == false`。
- [ ] 在角色选择测试中构造界面、预置 `pendingJobId`，调用公开取消入口，断言任务编号、照片状态和 Loading 状态均被清理，状态文字为「已取消生成，请重新拍照创建角色」。
- [ ] 运行目标测试，确认缺少取消 API 和 Loading 状态时失败。

### 任务 5：实现网络与 UI 取消

- [ ] `C1CharacterService` 在 Upload、Poll、Download 的每个请求期间登记活动请求，结束时清理引用；`CancelActiveRequests()` 调用 `Abort()`。
- [ ] `C1CharacterSelection` 只保留一个活动操作句柄，所有生成、继续查询和远程下载从统一入口启动。
- [ ] 开始操作时显示 Loading 并锁定底层控件；阶段回调同步更新原状态区和 `Loading Status`。
- [ ] 点击取消时停止协程、终止请求、清空并保存 `pendingJobId`、清空照片与重试标记、关闭 Loading 并恢复交互。
- [ ] `Update()` 使用 `Time.unscaledDeltaTime` 为插画提供不超过 12 px 的浮动、0.97–1.03 呼吸缩放和 ±2° 摇摆；趣味文案每约 2.5 秒随机无重复轮播。
- [ ] 成功、失败、对象销毁时统一关闭 Loading，避免残留遮罩。
- [ ] 运行取消与角色选择目标测试，确认通过。

### 任务 6：渲染和完整验证

- [ ] 使用生成器渲染 1920×1080 Loading 实际预览到 `Docs/ui/character-generation-loading-preview.png`。
- [ ] 运行角色预制体、角色服务和完整 EditMode 测试，记录新增失败与已知历史失败。
- [ ] 检查两张图片尺寸、Alpha、Prefab 无丢失脚本、`git diff --check` 无格式错误。
- [ ] 只审查本计划涉及文件，不覆盖用户当前的角色、HUD、首页和默认关卡改动，不自动提交。
