# PaperGame 移动端 WebGL 全屏实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 使用项目级 WebGL 模板替换依赖 User-Agent 的 Unity 默认页面，使游戏画布在手机与桌面浏览器中始终铺满可视区域。

**架构：** `PaperGameMobile` 模板静态声明 viewport，并以 `100dvw × 100dvh` 驱动 DOM 画布。`C1WebGLBuilder` 在构建前选择该模板，静态 EditMode 测试锁定模板和构建配置，最终通过真实 WebGL 构建与移动视口检查验证产物。

**技术栈：** Unity 2022.3、C#、NUnit EditMode、Unity WebGL Template、HTML、CSS、JavaScript

---

## 文件结构

- 创建 `Assets/WebGLTemplates/PaperGameMobile/index.html`：Unity WebGL 加载入口与固定移动端 viewport。
- 创建 `Assets/WebGLTemplates/PaperGameMobile/TemplateData/style.css`：全视口画布、加载条和错误横幅样式。
- 创建 `Assets/Tests/EditMode/C1WebGLTemplateTests.cs`：验证模板不再依赖 User-Agent，且构建器明确选择项目模板。
- 修改 `Assets/Editor/C1WebGLBuilder.cs`：构建前选择 `PROJECT:PaperGameMobile`。
- 生成并提交以上 Unity 资源的 `.meta` 文件，保证模板 GUID 稳定。

### 任务 1：项目级全屏模板与构建选择

**文件：**

- 创建：`Assets/WebGLTemplates/PaperGameMobile/index.html`
- 创建：`Assets/WebGLTemplates/PaperGameMobile/TemplateData/style.css`
- 创建：`Assets/Tests/EditMode/C1WebGLTemplateTests.cs`
- 修改：`Assets/Editor/C1WebGLBuilder.cs`

- [ ] **步骤 1：编写失败的模板配置测试**

在 `C1WebGLTemplateTests.cs` 中读取项目文件并断言：

```csharp
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1WebGLTemplateTests
    {
        [Test]
        public void MobileTemplate_UsesStaticViewportAndFullViewportCanvas()
        {
            var templateRoot = Path.Combine(Application.dataPath, "WebGLTemplates/PaperGameMobile");
            var html = File.ReadAllText(Path.Combine(templateRoot, "index.html"));
            var css = File.ReadAllText(Path.Combine(templateRoot, "TemplateData/style.css"));

            StringAssert.Contains("width=device-width, height=device-height", html);
            StringAssert.DoesNotContain("iPhone|iPad|iPod|Android", html);
            StringAssert.Contains("width: 100dvw", css);
            StringAssert.Contains("height: 100dvh", css);
            StringAssert.Contains("overflow: hidden", css);
            StringAssert.Contains("#unity-canvas", css);
        }

        [Test]
        public void WebGLBuilder_SelectsPaperGameMobileTemplate()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath, "Editor/C1WebGLBuilder.cs"));
            StringAssert.Contains("PROJECT:PaperGameMobile", source);
            StringAssert.Contains("PlayerSettings.WebGL.template", source);
        }
    }
}
```

- [ ] **步骤 2：运行测试并确认红灯**

将 `Assets`、`Packages` 和 `ProjectSettings` 同步到 `Temp/C1VerifyProject`，运行：

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath Temp/C1VerifyProject \
  -runTests -testPlatform EditMode \
  -testFilter PaperGame.C1.Tests.C1WebGLTemplateTests \
  -testResults /private/tmp/papergame-webgl-template-red.xml \
  -logFile /private/tmp/papergame-webgl-template-red.log
```

预期：测试失败，原因是 `PaperGameMobile/index.html` 不存在或构建器未选择模板。

- [ ] **步骤 3：创建最小全屏模板**

`index.html` 静态写入 viewport，不包含设备判断。核心结构和 Unity 配置如下：

```html
<!DOCTYPE html>
<html lang="zh-CN">
  <head>
    <meta charset="utf-8">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <meta name="viewport" content="width=device-width, height=device-height, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover">
    <title>{{{ PRODUCT_NAME }}}</title>
    <link rel="stylesheet" href="TemplateData/style.css">
  </head>
  <body>
    <div id="unity-container">
      <canvas id="unity-canvas" tabindex="-1"></canvas>
      <div id="unity-loading-bar"><div id="unity-progress-bar-full"></div></div>
      <div id="unity-warning"></div>
    </div>
    <script>
      var canvas = document.querySelector("#unity-canvas");
      var loadingBar = document.querySelector("#unity-loading-bar");
      var progressBarFull = document.querySelector("#unity-progress-bar-full");
      var warningBanner = document.querySelector("#unity-warning");
      function unityShowBanner(message, type) {
        var item = document.createElement("div");
        item.textContent = message;
        item.className = type === "error" ? "error" : "warning";
        warningBanner.appendChild(item);
        if (type !== "error") setTimeout(function () { item.remove(); }, 5000);
      }
      var buildUrl = "Build";
      var loaderUrl = buildUrl + "/{{{ LOADER_FILENAME }}}";
      var config = {
        dataUrl: buildUrl + "/{{{ DATA_FILENAME }}}",
        frameworkUrl: buildUrl + "/{{{ FRAMEWORK_FILENAME }}}",
#if USE_THREADS
        workerUrl: buildUrl + "/{{{ WORKER_FILENAME }}}",
#endif
#if USE_WASM
        codeUrl: buildUrl + "/{{{ CODE_FILENAME }}}",
#endif
#if MEMORY_FILENAME
        memoryUrl: buildUrl + "/{{{ MEMORY_FILENAME }}}",
#endif
#if SYMBOLS_FILENAME
        symbolsUrl: buildUrl + "/{{{ SYMBOLS_FILENAME }}}",
#endif
        streamingAssetsUrl: "StreamingAssets",
        companyName: {{{ JSON.stringify(COMPANY_NAME) }}},
        productName: {{{ JSON.stringify(PRODUCT_NAME) }}},
        productVersion: {{{ JSON.stringify(PRODUCT_VERSION) }}},
        showBanner: unityShowBanner
      };
      loadingBar.style.display = "block";
      var script = document.createElement("script");
      script.src = loaderUrl;
      script.onload = function () {
        createUnityInstance(canvas, config, function (progress) {
          progressBarFull.style.width = (progress * 100) + "%";
        }).then(function () {
          loadingBar.style.display = "none";
        }).catch(function (message) {
          unityShowBanner(message, "error");
        });
      };
      document.body.appendChild(script);
    </script>
  </body>
</html>
```

`style.css` 使用旧视口单位回退，再以动态视口单位覆盖：

```css
html, body {
  width: 100%;
  height: 100%;
  margin: 0;
  padding: 0;
  overflow: hidden;
  background: #000;
}

#unity-container {
  position: fixed;
  inset: 0;
  width: 100vw;
  height: 100vh;
  width: 100dvw;
  height: 100dvh;
}

#unity-canvas {
  display: block;
  width: 100%;
  height: 100%;
  background: {{{ BACKGROUND_COLOR }}};
  touch-action: none;
}

#unity-loading-bar {
  display: none;
  position: absolute;
  left: 20%;
  top: 50%;
  width: 60%;
  height: 12px;
  transform: translateY(-50%);
  overflow: hidden;
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.25);
}

#unity-progress-bar-full {
  width: 0;
  height: 100%;
  background: #2f80ed;
}

#unity-warning {
  position: absolute;
  z-index: 2;
  top: env(safe-area-inset-top, 0);
  left: 50%;
  width: min(90%, 720px);
  transform: translateX(-50%);
  font: 16px sans-serif;
}

#unity-warning .warning,
#unity-warning .error { padding: 10px; }
#unity-warning .warning { background: #fff3a3; }
#unity-warning .error { color: #fff; background: #c62828; }
```

- [ ] **步骤 4：让构建器选择项目模板**

在 `C1WebGLBuilder.Build` 创建 `BuildPlayerOptions` 前加入：

```csharp
PlayerSettings.WebGL.template = "PROJECT:PaperGameMobile";
```

模板选择是项目构建配置，不在构建结束后恢复，确保菜单和命令行后续构建保持一致。

- [ ] **步骤 5：运行定向测试并确认绿灯**

重复步骤 2 命令，将结果写入 `/private/tmp/papergame-webgl-template-green.xml`。

预期：`2/2` 通过，`0` 失败。

- [ ] **步骤 6：提交模板实现**

```bash
git add Assets/WebGLTemplates Assets/Editor/C1WebGLBuilder.cs Assets/Editor/C1WebGLBuilder.cs.meta Assets/Tests/EditMode/C1WebGLTemplateTests.cs Assets/Tests/EditMode/C1WebGLTemplateTests.cs.meta
git commit -m "fix(WebGL): 让游戏画布铺满手机视口"
```

### 任务 2：全量测试、构建与移动视口验证

**文件：**

- 验证：`Assets/Tests/EditMode`
- 构建：`Builds/C1WebGL-Mobile`
- 检查：`Builds/C1WebGL-Mobile/index.html`

- [ ] **步骤 1：运行全部 EditMode 测试**

```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath Temp/C1VerifyProject \
  -runTests -testPlatform EditMode \
  -testResults /private/tmp/papergame-all-editmode-fullscreen.xml \
  -logFile /private/tmp/papergame-all-editmode-fullscreen.log
```

预期：全部测试通过，失败数为 `0`。

- [ ] **步骤 2：重新构建移动端 WebGL**

```bash
env C1_WEBGL_OUTPUT="/Users/caizhenyu/Work/AI Work/PaperGame/PaperGame/Builds/C1WebGL-Mobile" \
  /Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath Temp/C1VerifyProject \
  -executeMethod PaperGame.C1.Editor.C1WebGLBuilder.BuildFromCommandLine \
  -logFile /private/tmp/papergame-webgl-fullscreen-build.log \
  -quit
```

预期：进程退出码为 `0`，生成新的 `index.html`、`.data.gz`、`.framework.js.gz` 和 `.wasm.gz`。

- [ ] **步骤 3：检查构建产物和 HTTP 响应**

```bash
rg -n "viewport-fit=cover|unity-container|100dvw" Builds/C1WebGL-Mobile/index.html Builds/C1WebGL-Mobile/TemplateData/style.css
curl -sS -I http://127.0.0.1:8125/index.html
curl -sS -I http://127.0.0.1:8125/Build/C1WebGL-Mobile.wasm.gz
```

预期：产物包含静态 viewport 与动态视口样式；入口返回 `200`；WASM 返回 `Content-Type: application/wasm` 和 `Content-Encoding: gzip`。

- [ ] **步骤 4：以手机横屏尺寸验证 DOM 布局**

通过浏览器视口能力设置 `844 × 390`，打开本地构建并读取 DOM 矩形。

预期：

```text
viewport: 844 × 390
container: 844 × 390
canvas: 844 × 390
body scroll: 844 × 390 或更小
```

同时确认页面正常加载游戏，触控按钮仍在画布内；恢复浏览器默认视口。

- [ ] **步骤 5：更新局域网访问结果**

运行 `ipconfig getifaddr en0` 读取当前 Wi-Fi 地址，确认服务继续监听 `0.0.0.0:8125`，再组合为 `http://局域网地址:8125` 提供给用户。说明真实手机上的相机调起和刘海区域仍需用户最终确认。
