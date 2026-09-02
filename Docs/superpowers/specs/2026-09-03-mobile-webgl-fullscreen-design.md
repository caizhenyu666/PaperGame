# PaperGame 移动端 WebGL 全屏设计

## 背景与根因

当前构建使用 Unity 2022 默认 WebGL 模板。模板只在 User-Agent 命中 `iPhone|iPad|iPod|Android` 时动态写入 viewport，并切换到全屏样式；未命中时固定使用 `960 × 600` 的桌面画布。

在 `844 × 390` 的横屏视口复现时，页面仍使用 `unity-desktop`，DOM 画布尺寸保持 `960 × 600`。手机浏览器会将这类桌面页面整体缩放，导致游戏、角色和触控按钮一起显得过小。问题位于 WebGL 页面模板层，不是 Unity 相机、照片像素坐标或 CanvasScaler 的归一化问题。

## 目标

- WebGL 页面不依赖设备型号识别，始终正确声明移动端 viewport。
- 游戏画布铺满当前浏览器可视区域，不保留 Unity 默认页脚和外围留白。
- 横竖屏切换、浏览器地址栏伸缩后，画布继续匹配可视区域。
- 保留 Unity 内部横屏提示、触控控制、拍照和照片预览功能。
- 模板纳入 Unity 项目，后续重新构建不会丢失修改。

## 非目标

- 本次不改变 Unity 相机的关卡取景算法。
- 本次不放大角色或改变照片的世界坐标比例。
- 本次不实现浏览器原生全屏 API，也不要求用户额外点击“进入全屏”。
- 本次不修改照片上传、OpenCV 处理、关卡分享等后续功能。

## 方案比较

### 方案 A：移动端优先的项目级 WebGL 模板（采用）

静态写入 viewport，并让页面、容器和画布始终占满可视区域。该方案不依赖 User-Agent，在手机、微信内置浏览器以及桌面浏览器中行为一致；模板属于项目资源，能随每次构建稳定产出。

### 方案 B：扩充 User-Agent 判断

继续保留 Unity 默认模板，只增加更多设备关键词。改动较小，但无法覆盖“请求桌面网站”、新设备标识和部分内置浏览器，仍然存在漏判风险。

### 方案 C：通过浏览器原生全屏 API 放大

让用户点击按钮进入全屏。该方案依赖用户手势，不同移动浏览器限制不同，且进入页面时仍会先显示过小画面，因此不作为基础布局方案。

## 设计

### 项目级模板

新增 `Assets/WebGLTemplates/PaperGameMobile`，包含：

- `index.html`：加载 Unity 构建、声明 viewport、展示加载进度和错误信息。
- `TemplateData/style.css`：定义全视口布局和安全区域适配。
- Unity 加载所需的图标及进度图片，沿用默认模板资源。

构建脚本在执行 WebGL 构建前将 `PlayerSettings.WebGL.template` 设置为 `PROJECT:PaperGameMobile`。模板选择保存在项目配置中，菜单构建和命令行构建使用相同页面。

### 页面布局

页面使用以下约束：

- `html`、`body`、`#unity-container` 和 `#unity-canvas` 占满宽高。
- 首选 `100dvw × 100dvh`，并用 `100vw × 100vh` 作为旧浏览器回退。
- `body` 使用 `overflow: hidden`，禁止页面因默认画布尺寸产生滚动或整体缩放。
- 画布使用 `display: block; width: 100%; height: 100%`。
- viewport 使用 `width=device-width, height=device-height, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover`。
- 不渲染 Unity 默认页脚；加载条和错误提示覆盖在画布上方。
- 刘海区域由 Unity HUD 自身布局负责，页面只使用 `viewport-fit=cover` 提供完整可视区域。

Unity 继续保持 `matchWebGLToCanvasSize` 默认行为，使渲染缓冲尺寸随 DOM 画布和设备像素比变化。

### 屏幕旋转

浏览器发生横竖屏切换或地址栏伸缩时，动态视口单位会更新容器尺寸。Unity 随画布尺寸变化更新 `Screen.width`、`Screen.height` 和相机 aspect；现有 `C1MobileOrientationHint` 继续在手机竖屏时遮罩并提示旋转。

## 错误处理

- Unity 加载失败时，在页面顶部展示永久错误横幅。
- 非关键警告展示 5 秒后自动消失。
- 不通过 JavaScript 强制旋转屏幕，避免浏览器权限和兼容性问题。
- 若浏览器不支持动态视口单位，使用传统视口单位回退。

## 验证

### 静态验证

- 模板必须静态包含 viewport，不允许放在 User-Agent 条件分支中。
- CSS 必须包含全视口尺寸、画布 `100%` 宽高和 `overflow: hidden`。
- 构建脚本必须选择 `PROJECT:PaperGameMobile`。

### 构建验证

- 运行全部 EditMode 测试，确认现有游戏逻辑无回归。
- 重新生成 WebGL 构建，确认构建成功。
- 检查构建后的 `index.html` 来自项目模板，并包含移动端 viewport。

### 浏览器验证

- 在 `844 × 390` 横屏视口下，容器和画布 CSS 尺寸必须等于视口尺寸。
- 页面不得产生水平或垂直滚动区域。
- 在竖屏视口下，Unity 的旋转提示必须继续覆盖游戏。
- 最终在真实手机上确认横屏画面铺满浏览器可视区域，并验证触控与拍照入口仍可用。
