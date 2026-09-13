# 记事本主题正式 UI 美术产出计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在 Figma 中交付可编辑的关卡 HUD 和角色创建页，并将无文字的记事本主题切图导入 Unity 工程。

**架构：** Figma 是布局、尺寸和导出命名的唯一设计稿源；AI 图像生成只负责透明背景的纸张与手绘装饰。所有文案、角色预览和状态文案均由 Unity 渲染，切图仅提供无字底图、图标和装饰。

**技术栈：** Figma、Codex 图像生成、Unity UI Image/Sprite Editor、PNG（RGBA）。

---

## 文件结构

- 创建：`Figma 文件「纸上游戏 · 正式 UI」`：包含 Foundations、关卡 HUD、创建主角、Export 4 个页面。
- 创建：`Assets/Art/UI/Notebook/paper-background-tile.png`：可平铺暖白纸纹。
- 创建：`Assets/Art/UI/Notebook/panel-torn-paper.png`：九宫格撕纸容器。
- 创建：`Assets/Art/UI/Notebook/notebook-list-panel.png`：带活页孔的角色列表容器。
- 创建：`Assets/Art/UI/Notebook/button-primary-yellow.png`：无字橙黄色主按钮底图。
- 创建：`Assets/Art/UI/Notebook/button-secondary-green.png`：无字草绿色次按钮底图。
- 创建：`Assets/Art/UI/Notebook/button-direction-paper.png`：方向键白纸底图。
- 创建：`Assets/Art/UI/Notebook/button-jump-yellow.png`：跳跃键黄纸底图。
- 创建：`Assets/Art/UI/Notebook/button-pause-paper.png`：暂停键白纸底图。
- 创建：`Assets/Art/UI/Notebook/icon-arrow-left-crayon.png`、`icon-arrow-right-crayon.png`、`icon-jump-scribble.png`、`icon-pause-crayon.png`：无文字蜡笔图标。
- 创建：`Assets/Art/UI/Notebook/sticker-selected-star.png`：已选角色星星贴纸。
- 创建：`Assets/Art/UI/Notebook/decoration-clouds.png`、`decoration-grass-flowers.png`、`decoration-flag-confetti.png`：透明背景边缘装饰。
- 创建：`Docs/ui/notebook-ui-export-manifest.md`：资源尺寸、用途、九宫格边界与 Unity 导入说明。

### 任务 1：建立 Figma 基础与页面结构

**文件：**
- 创建：Figma 文件「纸上游戏 · 正式 UI」

- [ ] **步骤 1：创建 1920 × 1080 横屏设计文件**

在 Figma 创建设计文件，新增 `00 · Foundations`、`01 · 关卡 HUD`、`02 · 创建主角`、`03 · Export` 页面；所有页面基准 Frame 为 1920 × 1080。

- [ ] **步骤 2：建立视觉 token**

在 `00 · Foundations` 建立下列颜色样式：`Paper #F7F0DE`、`Pencil #2D2926`、`Crayon Yellow #F7B718`、`Crayon Green #73B72B`、`Crayon Blue #3899D8`、`Crayon Red #EC5454`。描边统一为 5 px 深灰，圆角保持轻微不规则，阴影为 `0 8 0 #2D292633`。

- [ ] **步骤 3：核对母版一致性**

将现有首页截图作为视觉参考置于 Foundations 页面旁，只作比对、不导出。确认主按钮为黄色、次按钮为绿色、文字不烘焙进切图。

- [ ] **步骤 4：提交设计稿基础版本**

在 Figma 命名为「纸上游戏 · 正式 UI · v1」，保留可编辑层级。

### 任务 2：设计关卡 HUD

**文件：**
- 修改：Figma 文件 `01 · 关卡 HUD`

- [ ] **步骤 1：制作默认 HUD Frame**

创建默认 Frame：左上放「第 1 页」撕纸角标与 3 颗星；右上放 96 × 96 暂停键；左下并列放 160 × 160 左右键；右下放 220 × 220 跳跃键。中央 70% 区域完全透明，作为关卡画面安全区。

- [ ] **步骤 2：制作交互与结果状态**

复制默认 Frame，分别制作方向/跳跃按钮按下态、暂停态、成功结果与重试结果。成功卡使用红旗和彩纸；重试卡使用铅笔箭头。结果卡的按钮底图不含文字。

- [ ] **步骤 3：进行视觉验收**

用 1920 × 1080 和 1600 × 900 预览：左右控制键之间间距不少于 24 px；暂停、HUD、控制键均不进入中央玩法安全区。

### 任务 3：设计创建主角页

**文件：**
- 修改：Figma 文件 `02 · 创建主角`

- [ ] **步骤 1：制作默认 Frame**

创建「我的小主角」撕纸标题、右上返回入口、左侧活页角色列表、中央白纸预览台、右侧跑步/跳跃便签、左下绿色拍照按钮、右下黄色确认按钮。角色、全部中文文案和动画预览均为可编辑占位层，不导出为图片。

- [ ] **步骤 2：制作生成中与失败状态**

复制默认 Frame，分别放置铅笔旋转圈 + 状态便签，以及带回形针的失败提示纸 + 重试按钮。按钮状态均使用与默认页相同的底图组件。

- [ ] **步骤 3：进行视觉验收**

与首页截图并排检查：纸色、铅笔轮廓、云朵涂鸦与黄绿按钮一致；复杂装饰只停留在页面边缘。

### 任务 4：生成、导出与验收切图

**文件：**
- 创建：`Assets/Art/UI/Notebook/*.png`
- 创建：`Docs/ui/notebook-ui-export-manifest.md`

- [ ] **步骤 1：生成无文字 PNG 资产**

基于首页参考图，分别生成纸纹、撕纸容器、活页容器、5 类按钮、4 个图标、星星贴纸和 3 类装饰。所有输出均为 RGBA PNG；容器、按钮和图标单独生成，禁止把整页 UI 导出为一张图。

- [ ] **步骤 2：检查透明度与边缘**

逐张检查：纸张和笔触边缘没有白底方框；按钮不存在 AI 文字；图标轮廓在浅纸背景上保持足够对比度。

- [ ] **步骤 3：写出资源清单**

在 `Docs/ui/notebook-ui-export-manifest.md` 记录每张资源的文件名、尺寸、用途、Sprite Mode、九宫格边界与是否允许 Repeat。

- [ ] **步骤 4：导入 Unity 并做拉伸检查**

将 PNG 放入 `Assets/Art/UI/Notebook/`，对 `panel-*` 和 `button-primary-*`、`button-secondary-*` 设置九宫格；在 1×、1.5×、2× 宽度下预览，确认边角和活页孔不被拉伸。

- [ ] **步骤 5：提交美术资源**

```bash
git add Assets/Art/UI/Notebook Docs/ui/notebook-ui-export-manifest.md
git commit -m "feat(UI): 添加记事本主题界面切图"
```

### 任务 5：最终交付检查

**文件：**
- 验证：Figma 文件「纸上游戏 · 正式 UI」
- 验证：`Assets/Art/UI/Notebook/`
- 验证：`Docs/ui/notebook-ui-export-manifest.md`

- [ ] **步骤 1：执行资源完整性检查**

运行：

```bash
rg --files Assets/Art/UI/Notebook | sort
```

预期：清单中的 15 个 PNG 文件全部存在，且没有整页 UI 位图。

- [ ] **步骤 2：执行 Git 工作区检查**

运行：

```bash
git diff --check
git status --short
```

预期：无空白错误；仅显示用户原有改动及本次切图提交后的干净状态。
