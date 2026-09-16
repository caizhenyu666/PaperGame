# 固定横屏与拍照界面实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 消除移动端横竖屏切换时的 WebGL 闪烁，并让拍照取景框、关闭按钮和无文字双圆快门在任意物理方向下保持稳定横屏布局。

**架构：** Unity 页面只在启动时设置一次 Canvas 渲染缓冲，后续方向变化完全交给 CSS 横屏舞台变换，避免重建 WebGL 缓冲。拍照覆盖层建立同样的横屏舞台，并用局部布局坐标计算 `object-fit: cover` 裁剪区域，使裁剪不受父级旋转影响。

**技术栈：** Unity 2022.3、NUnit EditMode、Unity WebGL Template、HTML、CSS、JavaScript、WebRTC `getUserMedia`

---

## 文件结构

- 修改 `Assets/Tests/EditMode/C1WebGLTemplateTests.cs`：增加固定渲染缓冲、PWA 横屏声明和拍照覆盖层结构的回归测试。
- 修改 `Assets/WebGLTemplates/PaperGameMobile/index.html`：移除旋转期间的 Canvas 尺寸监听，只在启动时建立渲染分辨率。
- 修改 `Assets/WebGLTemplates/PaperGameMobile/TemplateData/style.css`：将竖屏布局改为单一合成层变换，避免位置与旋转分别更新。
- 修改 `Assets/Plugins/WebGL/PaperGamePhotoCapture.jslib`：实现固定横屏相机舞台、等比取景框、关闭按钮、无文字快门及幂等清理。

### 任务 1：锁定 WebGL 横屏舞台与渲染缓冲

**文件：**

- 修改：`Assets/Tests/EditMode/C1WebGLTemplateTests.cs`
- 修改：`Assets/WebGLTemplates/PaperGameMobile/index.html`
- 修改：`Assets/WebGLTemplates/PaperGameMobile/TemplateData/style.css`
- 验证：`Assets/WebGLTemplates/PaperGameMobile/manifest.json`

- [ ] **步骤 1：编写失败的模板测试**

在 `C1WebGLTemplateTests` 中增加以下测试，并保留既有测试：

```csharp
[Test]
public void MobileTemplate_KeepsWebGlBackingBufferStableAcrossOrientationChanges()
{
    var root = Path.Combine(Application.dataPath, "WebGLTemplates/PaperGameMobile");
    var html = File.ReadAllText(Path.Combine(root, "index.html"));
    var css = File.ReadAllText(Path.Combine(root, "TemplateData/style.css"));

    StringAssert.Contains("initializeUnityCanvas", html);
    StringAssert.DoesNotContain("addEventListener(\"resize\", resizeUnityCanvas)", html);
    StringAssert.DoesNotContain("new ResizeObserver(resizeUnityCanvas)", html);
    StringAssert.Contains("matchWebGLToCanvasSize: false", html);
    StringAssert.Contains("translate3d(100dvw, 0, 0) rotate(90deg)", css);
    StringAssert.Contains("backface-visibility: hidden", css);
}

[Test]
public void MobileManifest_PrefersStandaloneLandscape()
{
    var path = Path.Combine(Application.dataPath,
        "WebGLTemplates/PaperGameMobile/manifest.json");
    var manifest = File.ReadAllText(path);

    StringAssert.Contains("\"display\": \"standalone\"", manifest);
    StringAssert.Contains("\"orientation\": \"landscape\"", manifest);
}
```

- [ ] **步骤 2：运行测试并确认红灯**

运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$PWD" \
  -runTests -testPlatform EditMode \
  -testFilter PaperGame.C1.Tests.C1WebGLTemplateTests \
  -testResults /private/tmp/papergame-orientation-red.xml \
  -logFile /private/tmp/papergame-orientation-red.log
```

预期：新增的渲染缓冲测试失败，因为模板仍包含 `resize` 和 `ResizeObserver` 监听，CSS 也尚未使用原子 `translate3d + rotate` 变换；Manifest 测试通过，记录现有横屏偏好未回归。

- [ ] **步骤 3：只在启动时设置 Canvas 渲染分辨率**

将 `index.html` 中的 `resizeUnityCanvas` 及监听器替换为：

```javascript
// 渲染缓冲只在 Unity 启动前建立一次。旋转只改变 CSS 舞台，避免清空 WebGL 帧缓冲。
function initializeUnityCanvas() {
  var dpr = window.devicePixelRatio || 1;
  canvas.width = Math.max(1, Math.round(canvas.clientWidth * dpr));
  canvas.height = Math.max(1, Math.round(canvas.clientHeight * dpr));
}
initializeUnityCanvas();
```

保留 `matchWebGLToCanvasSize: false`，不得重新加入 `resize`、`orientationchange` 或 `ResizeObserver` 对 Canvas `width`、`height` 的写入。

- [ ] **步骤 4：将竖屏样式改为单一合成层变换**

把 `style.css` 中容器与竖屏规则调整为：

```css
#unity-container {
  position: fixed;
  inset: 0;
  width: 100vw;
  height: 100vh;
  width: 100dvw;
  height: 100dvh;
  transform: translate3d(0, 0, 0);
  transform-origin: top left;
  backface-visibility: hidden;
  will-change: transform;
  transition: none !important;
}

@media (orientation: portrait) {
  #unity-container {
    width: 100vh;
    height: 100vw;
    width: 100dvh;
    height: 100dvw;
    transform: translate3d(100vw, 0, 0) rotate(90deg);
    transform: translate3d(100dvw, 0, 0) rotate(90deg);
  }
}
```

删除竖屏规则中的 `position`、`top`、`left`，避免旋转时布局位置与合成变换分两次更新。

- [ ] **步骤 5：运行模板测试并确认绿灯**

重复步骤 2 的命令，输出改为 `/private/tmp/papergame-orientation-green.xml` 和 `.log`。

预期：`C1WebGLTemplateTests` 全部通过。

- [ ] **步骤 6：提交稳定横屏改动**

```bash
git add Assets/Tests/EditMode/C1WebGLTemplateTests.cs \
  Assets/WebGLTemplates/PaperGameMobile/index.html \
  Assets/WebGLTemplates/PaperGameMobile/TemplateData/style.css
git commit -m "fix(WebGL): 消除屏幕旋转时的画面闪烁"
```

### 任务 2：重构拍照覆盖层布局与生命周期

**文件：**

- 修改：`Assets/Tests/EditMode/C1WebGLTemplateTests.cs`
- 修改：`Assets/Plugins/WebGL/PaperGamePhotoCapture.jslib`

- [ ] **步骤 1：编写失败的拍照界面静态测试**

在 `C1WebGLTemplateTests` 中增加：

```csharp
[Test]
public void PhotoCaptureOverlay_UsesStableLandscapeControlsAndLocalCropCoordinates()
{
    var path = Path.Combine(Application.dataPath,
        "Plugins/WebGL/PaperGamePhotoCapture.jslib");
    var source = File.ReadAllText(path);

    StringAssert.Contains("id=\"pg-cam-close\"", source);
    StringAssert.Contains("aria-label=\"关闭相机\"", source);
    StringAssert.Contains("id=\"pg-cam-shutter-core\"", source);
    StringAssert.DoesNotContain(">拍 照<", source);
    StringAssert.Contains("aspect-ratio:8/5", source);
    StringAssert.Contains("translate3d(100dvw,0,0) rotate(90deg)", source);
    StringAssert.Contains("video.srcObject=null", source);
    StringAssert.Contains("close.addEventListener('click',cleanup)", source);
    StringAssert.Contains("frame.offsetLeft", source);
    StringAssert.Contains("video.clientWidth", source);
    StringAssert.DoesNotContain("frame.getBoundingClientRect()", source);
}
```

- [ ] **步骤 2：运行测试并确认红灯**

运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$PWD" \
  -runTests -testPlatform EditMode \
  -testFilter PaperGame.C1.Tests.C1WebGLTemplateTests.PhotoCaptureOverlay_UsesStableLandscapeControlsAndLocalCropCoordinates \
  -testResults /private/tmp/papergame-camera-red.xml \
  -logFile /private/tmp/papergame-camera-red.log
```

预期：FAIL，现有覆盖层没有关闭按钮和双圆快门，且裁剪使用变换后的 `getBoundingClientRect()`。

- [ ] **步骤 3：建立幂等清理与关闭行为**

在 `PaperGamePhotoCapture.jslib` 的局部状态中保存 `video`，并将清理函数改为：

```javascript
var stream = null;
var overlay = null;
var video = null;
var css = null;
var cleaned = false;

var cleanup = function () {
  if (cleaned) return;
  cleaned = true;
  if (stream) stream.getTracks().forEach(function (track) { track.stop(); });
  stream = null;
  if (video) video.srcObject=null;
  if (overlay && overlay.parentNode) overlay.parentNode.removeChild(overlay);
  if (css && css.parentNode) css.parentNode.removeChild(css);
  overlay = null;
  video = null;
  css = null;
};
```

所有成功和失败路径只调用 `cleanup()`，不得再次直接 `removeChild(css)`。用户主动关闭只调用 `cleanup()`，不调用 `fail()`。

- [ ] **步骤 4：创建固定横屏相机舞台和控件**

将覆盖层 DOM 改为：

```javascript
overlay.innerHTML = [
  '<video id="pg-cam-video" autoplay playsinline muted></video>',
  '<div id="pg-cam-frame" aria-hidden="true"></div>',
  '<button id="pg-cam-close" type="button" aria-label="关闭相机">×</button>',
  '<button id="pg-cam-btn" type="button" aria-label="拍照">',
  '<span id="pg-cam-shutter-core" aria-hidden="true"></span>',
  '</button>'
].join('');
```

注入样式应完整包含以下约束：

```css
#pg-camera-overlay{position:fixed;inset:0;width:100vw;height:100vh;width:100dvw;height:100dvh;z-index:2147483647;background:#000;overflow:hidden;transform:translate3d(0,0,0);transform-origin:top left;backface-visibility:hidden}
#pg-cam-video{position:absolute;inset:0;width:100%;height:100%;object-fit:cover}
#pg-cam-frame{position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);width:min(560px,calc(100% - 160px));aspect-ratio:8/5;border:2.5px solid rgba(255,255,255,.88);border-radius:14px;box-shadow:0 0 0 9999px rgba(0,0,0,.42);pointer-events:none}
#pg-cam-close{position:absolute;top:max(16px,env(safe-area-inset-top));left:max(16px,env(safe-area-inset-left));width:44px;height:44px;border:0;border-radius:50%;background:rgba(0,0,0,.45);color:#fff;font:300 34px/40px -apple-system,BlinkMacSystemFont,sans-serif;cursor:pointer;-webkit-tap-highlight-color:transparent}
#pg-cam-btn{position:absolute;top:50%;right:max(18px,env(safe-area-inset-right));transform:translateY(-50%);width:72px;height:72px;padding:5px;border:4px solid #fff;border-radius:50%;background:transparent;cursor:pointer;-webkit-tap-highlight-color:transparent}
#pg-cam-shutter-core{display:block;width:100%;height:100%;border-radius:50%;background:#fff;transition:transform .08s ease}
#pg-cam-btn:active #pg-cam-shutter-core{transform:scale(.9)}
#pg-cam-btn:disabled{opacity:.55}
@media (orientation:portrait){#pg-camera-overlay{width:100vh;height:100vw;width:100dvh;height:100dvw;transform:translate3d(100vw,0,0) rotate(90deg);transform:translate3d(100dvw,0,0) rotate(90deg)}}
```

CSS 作为 JavaScript 字符串写入时保持选择器和值一致。取得 `close` 后注册 `close.addEventListener('click',cleanup)`。

- [ ] **步骤 5：用局部布局坐标计算裁剪区域**

把拍照回调中的包围盒算法替换为：

```javascript
var frame = document.getElementById('pg-cam-frame');
var viewWidth = video.clientWidth;
var viewHeight = video.clientHeight;
var scale = Math.max(viewWidth / video.videoWidth,
  viewHeight / video.videoHeight);
var drawnWidth = video.videoWidth * scale;
var drawnHeight = video.videoHeight * scale;
var offsetX = (viewWidth - drawnWidth) / 2;
var offsetY = (viewHeight - drawnHeight) / 2;

var fx = frame.offsetLeft - frame.clientWidth / 2 - offsetX;
var fy = frame.offsetTop - frame.clientHeight / 2 - offsetY;
var sx = Math.max(0, Math.round(fx / scale));
var sy = Math.max(0, Math.round(fy / scale));
var sw = Math.min(video.videoWidth - sx,
  Math.round(frame.clientWidth / scale));
var sh = Math.min(video.videoHeight - sy,
  Math.round(frame.clientHeight / scale));
```

这里的 `offsetLeft`、`clientWidth` 和 `clientHeight` 均为横屏舞台内部坐标，不受父级 CSS 旋转后的包围盒影响。

拍照期间仅设置 `btn.disabled = true`；失败时恢复为 `false`，不得写入按钮文本。

- [ ] **步骤 6：运行拍照界面测试并确认绿灯**

重复步骤 2 的命令，输出改为 `/private/tmp/papergame-camera-green.xml` 和 `.log`。

预期：测试通过。

- [ ] **步骤 7：提交拍照界面改动**

```bash
git add Assets/Plugins/WebGL/PaperGamePhotoCapture.jslib \
  Assets/Tests/EditMode/C1WebGLTemplateTests.cs
git commit -m "feat(拍照): 固定取景布局并添加关闭与快门按钮"
```

### 任务 3：回归验证与真机验收交接

**文件：**

- 验证：`Assets/Tests/EditMode`
- 验证：`Assets/WebGLTemplates/PaperGameMobile`
- 验证：`Assets/Plugins/WebGL/PaperGamePhotoCapture.jslib`

- [ ] **步骤 1：运行全部 EditMode 测试**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$PWD" \
  -runTests -testPlatform EditMode \
  -testResults /private/tmp/papergame-landscape-all.xml \
  -logFile /private/tmp/papergame-landscape-all.log
```

预期：XML 中 `failed="0"`。若存在变更前已知失败，记录测试名称与错误，并确认新增的 `C1WebGLTemplateTests` 全部通过。

- [ ] **步骤 2：执行静态质量检查**

```bash
git diff --check
git status --short
```

预期：`git diff --check` 无输出；工作区只包含本计划涉及的预期文件，或已全部提交。

- [ ] **步骤 3：生成 WebGL 构建**

```bash
C1_WEBGL_OUTPUT=/private/tmp/papergame-stable-landscape-build \
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$PWD" \
  -executeMethod PaperGame.C1.Editor.C1WebGLBuilder.BuildFromCommandLine \
  -logFile /private/tmp/papergame-stable-landscape-build.log
```

预期：退出码为 `0`，日志包含 `WebGL build succeeded`，构建后的 `index.html` 不包含旋转期间 Canvas resize 监听。

- [ ] **步骤 4：按规格执行真机验收**

在 iPhone Safari 和添加到主屏幕的 Web App 中验证：游戏旋转无闪烁、触控命中正确、相机框布局不变、裁剪一致、关闭按钮释放摄像头、快门为无文字双圆。真机验收结果记录在任务交付说明中；自动化环境不得代替此步骤宣称真机效果已验证。

- [ ] **步骤 5：提交计划状态或必要的验证记录**

若实现过程中更新了本计划复选框或补充了验证结论：

```bash
git add Docs/superpowers/plans/2026-09-16-stable-landscape-camera-plan.md
git commit -m "docs(移动端): 更新固定横屏与拍照验证记录"
```
