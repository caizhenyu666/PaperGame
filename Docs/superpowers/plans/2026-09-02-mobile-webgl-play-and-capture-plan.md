# 手机 WebGL 游玩与拍照实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在当前照片关卡 WebGL 中同时加入手机触控游玩、横屏提示和系统相机拍照预览。

**架构：** Unity 角色控制器合并键盘与触控方向，独立移动控件组件处理 Pointer 生命周期，Bootstrap 只负责装配 HUD。拍照由 WebGL `.jslib` 创建原生文件拍摄入口，以原始 Data URL 回传 `C1PhotoCapture`，Unity 校验并生成内存预览；不包含上传或图像处理。

**技术栈：** Unity 2022.3、C#、Unity UI/EventSystem、Unity Test Framework、WebGL JavaScript 插件、HTML File API。

---

## 文件结构

- 修改：`Assets/Scripts/C1/C1PlayerController2D.cs`——合并键盘与触控方向输入。
- 创建：`Assets/Scripts/C1/C1MobileControls.cs`——左右按压状态、跳跃转发和 Pointer 按钮组件。
- 创建：`Assets/Tests/EditMode/C1MobileControlsTests.cs`——触控状态与跳跃测试。
- 创建：`Assets/Scripts/C1/C1MobileOrientationHint.cs`——竖屏提示判定。
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`——创建移动按钮、拍照入口、预览和旋转提示。
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`——移动 HUD 装配测试。
- 创建：`Assets/Scripts/C1/C1PhotoCapture.cs`——Data URL 解码、原始字节、预览和错误状态。
- 创建：`Assets/Tests/EditMode/C1PhotoCaptureTests.cs`——有效照片、错误与替换测试。
- 创建：`Assets/Plugins/WebGL/PaperGamePhotoCapture.jslib`——浏览器相机/文件选择桥接。
- 修改：`Assets/Editor/C1WebGLBuilder.cs`——保持移动模板并输出可验收构建。

### 任务 1：角色触控输入与移动控件

**文件：**
- 修改：`Assets/Scripts/C1/C1PlayerController2D.cs`
- 创建：`Assets/Scripts/C1/C1MobileControls.cs`
- 创建：`Assets/Tests/EditMode/C1MobileControlsTests.cs`
- 修改：`Assets/Tests/EditMode/C1PlayerControllerTests.cs`

- [ ] **步骤 1：先写失败测试。**

```csharp
[Test]
public void DirectionButtons_CombineAndReleaseTouchInput()
{
    controls.Configure(controller);
    controls.PressLeft();
    Assert.That(controller.TouchHorizontalInput, Is.EqualTo(-1f));
    controls.PressRight();
    Assert.That(controller.TouchHorizontalInput, Is.Zero);
    controls.ReleaseLeft();
    Assert.That(controller.TouchHorizontalInput, Is.EqualTo(1f));
    controls.ReleaseRight();
    Assert.That(controller.TouchHorizontalInput, Is.Zero);
}

[Test]
public void ResolveHorizontalInput_KeyboardOverridesTouch()
{
    controller.SetTouchHorizontalInput(-1f);
    Assert.That(controller.ResolveHorizontalInput(1f), Is.EqualTo(1f));
    Assert.That(controller.ResolveHorizontalInput(0f), Is.EqualTo(-1f));
}
```

- [ ] **步骤 2：同步验证副本并运行筛选测试。**

运行 Unity EditMode，筛选 `C1MobileControlsTests;C1PlayerControllerTests`。

预期：编译失败，缺少 `C1MobileControls`、`TouchHorizontalInput`、`SetTouchHorizontalInput` 和 `ResolveHorizontalInput`。

- [ ] **步骤 3：实现最小触控输入。**

`C1PlayerController2D.Update()` 改为：

```csharp
ApplyHorizontalInput(ResolveHorizontalInput(Input.GetAxisRaw("Horizontal")));
```

新增：

```csharp
public float TouchHorizontalInput { get; private set; }
public void SetTouchHorizontalInput(float input) => TouchHorizontalInput = Mathf.Clamp(input, -1f, 1f);
public float ResolveHorizontalInput(float keyboardInput) =>
    Mathf.Abs(keyboardInput) > 0.01f ? keyboardInput : TouchHorizontalInput;
```

`C1MobileControls` 保存 `leftPressed/rightPressed`，每次变化后设置 `(-1 或 0) + (1 或 0)`；`OnDisable` 清零。`C1TouchDirectionButton` 实现 `IPointerDownHandler/IPointerUpHandler/IPointerExitHandler`，`C1TouchJumpButton` 在 PointerDown 调用 `PressJump()`。

- [ ] **步骤 4：重跑筛选测试，确认通过。**

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/C1PlayerController2D.cs Assets/Scripts/C1/C1MobileControls.cs Assets/Tests/EditMode/C1MobileControlsTests.cs Assets/Tests/EditMode/C1PlayerControllerTests.cs
git commit -m "feat(移动端): 添加触控移动与跳跃输入"
```

### 任务 2：照片接收、校验与预览模型

**文件：**
- 创建：`Assets/Scripts/C1/C1PhotoCapture.cs`
- 创建：`Assets/Tests/EditMode/C1PhotoCaptureTests.cs`

- [ ] **步骤 1：先写失败测试。**

```csharp
[Test]
public void ReceivePhotoDataUrl_ValidPngPreservesBytesAndCreatesPreview()
{
    capture.Configure(preview, status);
    capture.ReceivePhotoDataUrl("data:image/png;base64," + Convert.ToBase64String(onePixelPng));
    CollectionAssert.AreEqual(onePixelPng, capture.CapturedBytes);
    Assert.That(capture.PreviewTexture.width, Is.EqualTo(1));
    Assert.That(status.text, Is.EqualTo("照片已获取"));
}

[Test]
public void ApplyPhoto_InvalidTypeDoesNotReplaceExistingPhoto()
{
    capture.ApplyPhoto(onePixelPng, "image/png");
    var previous = capture.PreviewTexture;
    Assert.That(capture.ApplyPhoto(onePixelPng, "image/heic"), Is.False);
    Assert.That(capture.PreviewTexture, Is.SameAs(previous));
}
```

另写超过 `15 * 1024 * 1024` 字节和第二次成功替换纹理的测试。

- [ ] **步骤 2：运行 `C1PhotoCaptureTests`，确认缺少组件导致编译失败。**

- [ ] **步骤 3：实现最小照片模型。**

公开契约：

```csharp
public const int MaximumPhotoBytes = 15 * 1024 * 1024;
public byte[] CapturedBytes { get; private set; }
public Texture2D PreviewTexture { get; private set; }
public void Configure(RawImage preview, Text status);
public void OpenCamera();
public void ReceivePhotoDataUrl(string dataUrl);
public void ReceivePhotoError(string code);
public bool ApplyPhoto(byte[] bytes, string mimeType);
```

`ApplyPhoto` 先完成类型、大小与 `Texture2D.LoadImage` 校验；仅全部成功后替换字段、RawImage 与状态，并销毁旧纹理。`OpenCamera` 在 `UNITY_WEBGL && !UNITY_EDITOR` 调用 `PaperGame_OpenPhotoCapture(gameObject.name)`，其他平台显示明确提示。

- [ ] **步骤 4：重跑照片测试，确认通过且无意外日志。**

- [ ] **步骤 5：提交。**

```bash
git add Assets/Scripts/C1/C1PhotoCapture.cs Assets/Tests/EditMode/C1PhotoCaptureTests.cs
git commit -m "feat(拍照): 添加照片接收与内存预览"
```

### 任务 3：移动 HUD、旋转提示与浏览器桥接

**文件：**
- 创建：`Assets/Scripts/C1/C1MobileOrientationHint.cs`
- 创建：`Assets/Plugins/WebGL/PaperGamePhotoCapture.jslib`
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`

- [ ] **步骤 1：先写失败测试。**

```csharp
[Test]
public void Build_CreatesMobileControlsAndPhotoCaptureHud()
{
    bootstrap.Build(C1LevelLoader.LoadDefault());
    Assert.That(GameObject.Find("Mobile Controls").GetComponent<C1MobileControls>(), Is.Not.Null);
    Assert.That(GameObject.Find("Move Left").GetComponent<C1TouchDirectionButton>(), Is.Not.Null);
    Assert.That(GameObject.Find("Move Right").GetComponent<C1TouchDirectionButton>(), Is.Not.Null);
    Assert.That(GameObject.Find("Jump").GetComponent<C1TouchJumpButton>(), Is.Not.Null);
    Assert.That(GameObject.Find("Capture Photo").GetComponent<Button>(), Is.Not.Null);
    Assert.That(bootstrap.HudCanvas.GetComponentInChildren<C1PhotoCapture>(true), Is.Not.Null);
}

[TestCase(720, 1280, true)]
[TestCase(1280, 720, false)]
public void OrientationHint_ShowsOnlyInPortrait(int width, int height, bool expected)
{
    hint.Evaluate(width, height, true);
    Assert.That(overlay.activeSelf, Is.EqualTo(expected));
}
```

- [ ] **步骤 2：运行 Bootstrap 与 Orientation 筛选测试，确认组件/UI 缺失。**

- [ ] **步骤 3：实现 `C1MobileOrientationHint`。**

`Evaluate(width,height,isTouchDevice)` 仅在触屏且 `height > width` 时激活遮罩；`Update()` 使用 `Screen.width/height` 和 `Application.isMobilePlatform || Input.touchSupported`。

- [ ] **步骤 4：Bootstrap 创建移动 HUD。**

创建锚定底部的左右/跳跃触控区、上方拍摄按钮、状态 Text、默认隐藏的 RawImage 预览和竖屏遮罩；使用现有 HUD Font、CanvasScaler 与 EventSystem。按钮 Image 使用低透明度，Pointer 组件绑定 `C1MobileControls`，拍摄 Button 绑定 `C1PhotoCapture.OpenCamera`。

- [ ] **步骤 5：添加 WebGL `.jslib`。**

```javascript
mergeInto(LibraryManager.library, {
  PaperGame_OpenPhotoCapture: function(receiverPtr) {
    var receiver = UTF8ToString(receiverPtr);
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/jpeg,image/png';
    input.capture = 'environment';
    input.style.display = 'none';
    document.body.appendChild(input);
    input.onchange = function() {
      var file = input.files && input.files[0];
      if (!file) { input.remove(); return; }
      if (file.size > 15 * 1024 * 1024) {
        SendMessage(receiver, 'ReceivePhotoError', 'size');
        input.remove();
        return;
      }
      var reader = new FileReader();
      reader.onload = function() {
        SendMessage(receiver, 'ReceivePhotoDataUrl', reader.result);
        input.remove();
      };
      reader.onerror = function() {
        SendMessage(receiver, 'ReceivePhotoError', 'read');
        input.remove();
      };
      reader.readAsDataURL(file);
    };
    input.click();
  }
});
```

- [ ] **步骤 6：重跑任务 3 筛选测试，确认通过。**

- [ ] **步骤 7：提交。**

```bash
git add Assets/Scripts/C1/C1MobileOrientationHint.cs Assets/Plugins/WebGL/PaperGamePhotoCapture.jslib Assets/Scripts/C1/C1GameBootstrap.cs Assets/Tests/EditMode/C1GameBootstrapTests.cs
git commit -m "feat(移动端): 装配手机HUD与拍照桥接"
```

### 任务 4：全量验证与 WebGL 手机视口验收

- [ ] **步骤 1：同步验证副本并运行全部 EditMode 测试。**

预期：结果 XML `failed="0"`，原有照片背景、透明碰撞、动画、跳跃调节测试全部继续通过。

- [ ] **步骤 2：从验证副本构建新的 WebGL 输出。**

```bash
C1_WEBGL_OUTPUT="$(pwd)/Builds/C1WebGL-Mobile" /Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath "$(pwd)/Temp/C1VerifyProject" \
  -executeMethod PaperGame.C1.Editor.C1VerifyAndBuild.Run \
  -logFile "$(pwd)/Temp/c1-mobile-webgl-build.log"
```

预期：进程退出码 `0`，日志包含 `C1VERIFY build finished`。

- [ ] **步骤 3：用带 gzip 响应头的本地服务器加载 WebGL。**

检查桌面视口仍可键盘游玩；移动横屏视口显示触控按钮与拍摄入口；移动竖屏视口显示旋转提示。

- [ ] **步骤 4：检查构建产物。**

确认 `.data.gz` 内包含 `PaperGamePhotoCapture`/拍照状态文本，浏览器控制台无新增脚本错误。真实相机权限与系统相机 UI 必须在实际手机上最终确认，桌面自动化只验证文件选择入口和回传组件。

- [ ] **步骤 5：若验证阶段产生必要修复，单独提交。**

```bash
git add <仅验证修复涉及的文件>
git commit -m "fix(移动端): 修复WebGL手机验收问题"
```

## 自检

- 触控游玩、键盘兼容、横屏提示、拍照、无损字节、预览、大小/格式错误均有明确任务与测试。
- 分享、上传、OpenCV 和动态关卡没有被提前引入。
- `.jslib` 只在用户点击时打开相机，符合移动浏览器手势要求。
- 照片组件保存原始字节，为后续服务端接口提供稳定边界。
