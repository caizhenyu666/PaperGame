# WebGL 域名与一键构建实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 提供可在 Project Settings 修改的角色服务域名与 WebGL 输出目录，并使一键构建输出产物绝对路径。

**架构：** `C1WebGLBuildSettings` 是驻留在 `Resources` 的运行时配置资源，令 WebGL 包可读取角色服务地址。`C1WebGLBuildSettingsProvider` 在 Project Settings 显示和保存该资源；构建器从其读取输出目录，命令行环境变量只覆盖单次输出位置。

**技术栈：** Unity 2022.3、C#、UnityEditor、Unity Test Framework（NUnit）、WebGL。

---

## 文件结构

- 创建：`Assets/Scripts/C1/C1WebGLBuildSettings.cs`——运行时域名和默认输出目录配置，以及安全的路径规范化。
- 创建：`Assets/Editor/C1WebGLBuildSettingsProvider.cs`——Project Settings 设置页与配置资产的创建/保存。
- 修改：`Assets/Scripts/C1/C1CharacterService.cs`——默认从配置资源获取服务域名，同时保留 Inspector 覆盖。
- 修改：`Assets/Editor/C1WebGLBuilder.cs`——菜单构建读取配置、命令行覆盖优先、日志输出绝对目录。
- 创建：`Assets/Resources/C1WebGLBuildSettings.asset`——随 WebGL 构建发布的默认配置。
- 修改：`Assets/Tests/EditMode/C1CharacterServiceTests.cs`——锁定默认服务地址来源。
- 修改：`Assets/Tests/EditMode/C1WebGLTemplateTests.cs`——锁定构建器的配置读取与成功日志。

### 任务 1：运行时配置资源

**文件：**
- 创建：`Assets/Scripts/C1/C1WebGLBuildSettings.cs`
- 创建：`Assets/Resources/C1WebGLBuildSettings.asset`
- 测试：`Assets/Tests/EditMode/C1CharacterServiceTests.cs`

- [ ] **步骤 1：编写失败的测试**

```csharp
[Test]
public void DefaultBaseUrl_UsesProjectWebGlSettings()
{
    var root = new GameObject("Character Service Default Url Test");
    var service = root.AddComponent<C1CharacterService>();

    Assert.That(service.BaseUrl, Is.EqualTo("http://scjjysd.xyz"));

    UnityEngine.Object.DestroyImmediate(root);
}
```

- [ ] **步骤 2：运行测试验证失败**

运行：
```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter PaperGame.C1.Tests.C1CharacterServiceTests.DefaultBaseUrl_UsesProjectWebGlSettings -testResults /private/tmp/c1-settings-red.xml -logFile /private/tmp/c1-settings-red.log
```

预期：FAIL，断言实际内网地址不等于 `http://scjjysd.xyz`。

- [ ] **步骤 3：编写最少实现代码**

```csharp
public sealed class C1WebGLBuildSettings : ScriptableObject
{
    public const string ResourcePath = "C1WebGLBuildSettings";
    public const string DefaultCharacterServiceBaseUrl = "http://scjjysd.xyz";
    public const string DefaultWebGlOutputPath = "Builds/C1WebGL";

    [SerializeField] private string characterServiceBaseUrl = DefaultCharacterServiceBaseUrl;
    [SerializeField] private string webGlOutputPath = DefaultWebGlOutputPath;
    public string CharacterServiceBaseUrl => string.IsNullOrWhiteSpace(characterServiceBaseUrl) ? DefaultCharacterServiceBaseUrl : characterServiceBaseUrl.TrimEnd('/');
    public string WebGlOutputPath => string.IsNullOrWhiteSpace(webGlOutputPath) ? DefaultWebGlOutputPath : webGlOutputPath;
    public static C1WebGLBuildSettings Load() => Resources.Load<C1WebGLBuildSettings>(ResourcePath);
}
```

创建资源并将 `C1CharacterService` 的序列化字段初值替换为 `C1WebGLBuildSettings.Load()?.CharacterServiceBaseUrl ?? C1WebGLBuildSettings.DefaultCharacterServiceBaseUrl`。

- [ ] **步骤 4：运行测试验证通过**

重复步骤 2 命令。

预期：PASS，NUnit 结果中 1/1 通过。

- [ ] **步骤 5：提交**

```bash
git add Assets/Scripts/C1/C1WebGLBuildSettings.cs Assets/Scripts/C1/C1WebGLBuildSettings.cs.meta Assets/Resources/C1WebGLBuildSettings.asset Assets/Resources/C1WebGLBuildSettings.asset.meta Assets/Scripts/C1/C1CharacterService.cs Assets/Tests/EditMode/C1CharacterServiceTests.cs
git commit -m "feat(WebGL): 添加运行时域名配置"
```

### 任务 2：Project Settings 编辑入口

**文件：**
- 创建：`Assets/Editor/C1WebGLBuildSettingsProvider.cs`
- 修改：`Assets/Tests/EditMode/C1WebGLTemplateTests.cs`

- [ ] **步骤 1：编写失败的测试**

```csharp
[Test]
public void WebGlSettingsProvider_RegistersProjectSettingsPage()
{
    var source = File.ReadAllText(Path.Combine(Application.dataPath, "Editor/C1WebGLBuildSettingsProvider.cs"));

    StringAssert.Contains("Project/Paper Game", source);
    StringAssert.Contains("SettingsProvider", source);
    StringAssert.Contains("C1WebGLBuildSettings", source);
}
```

- [ ] **步骤 2：运行测试验证失败**

运行：
```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter PaperGame.C1.Tests.C1WebGLTemplateTests.WebGlSettingsProvider_RegistersProjectSettingsPage -testResults /private/tmp/c1-provider-red.xml -logFile /private/tmp/c1-provider-red.log
```

预期：FAIL，找不到设置页源文件。

- [ ] **步骤 3：编写最少实现代码**

```csharp
[SettingsProvider]
private static SettingsProvider CreateProvider()
{
    return new SettingsProvider("Project/Paper Game", SettingsScope.Project)
    {
        guiHandler = _ =>
        {
            var settings = LoadOrCreate();
            var editor = UnityEditor.Editor.CreateEditor(settings);
            editor.OnInspectorGUI();
        }
    };
}
```

`LoadOrCreate` 通过 `AssetDatabase.LoadAssetAtPath` 读取 `Assets/Resources/C1WebGLBuildSettings.asset`；缺失时创建目录、创建默认资产并保存。设置页展示资产的 Inspector，字段修改由 Unity 序列化保存。

- [ ] **步骤 4：运行测试验证通过**

重复步骤 2 命令。

预期：PASS，NUnit 结果中 1/1 通过。

- [ ] **步骤 5：提交**

```bash
git add Assets/Editor/C1WebGLBuildSettingsProvider.cs Assets/Editor/C1WebGLBuildSettingsProvider.cs.meta Assets/Tests/EditMode/C1WebGLTemplateTests.cs
git commit -m "feat(WebGL): 添加项目设置入口"
```

### 任务 3：一键 WebGL 构建与绝对路径日志

**文件：**
- 修改：`Assets/Editor/C1WebGLBuilder.cs`
- 修改：`Assets/Tests/EditMode/C1WebGLTemplateTests.cs`

- [ ] **步骤 1：编写失败的测试**

```csharp
[Test]
public void WebGLBuilder_UsesConfiguredOutputAndLogsAbsolutePath()
{
    var source = File.ReadAllText(Path.Combine(Application.dataPath, "Editor/C1WebGLBuilder.cs"));

    StringAssert.Contains("C1WebGLBuildSettings", source);
    StringAssert.Contains("Path.GetFullPath", source);
    StringAssert.Contains("WebGL build succeeded:", source);
    StringAssert.Contains("C1_WEBGL_OUTPUT", source);
}
```

- [ ] **步骤 2：运行测试验证失败**

运行：
```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter PaperGame.C1.Tests.C1WebGLTemplateTests.WebGLBuilder_UsesConfiguredOutputAndLogsAbsolutePath -testResults /private/tmp/c1-builder-red.xml -logFile /private/tmp/c1-builder-red.log
```

预期：FAIL，构建器尚未引用配置资源和成功日志。

- [ ] **步骤 3：编写最少实现代码**

```csharp
private static string ResolveOutputPath(string configuredPath)
{
    if (string.IsNullOrWhiteSpace(configuredPath))
        throw new InvalidOperationException("WebGL 输出目录不能为空。");
    return Path.GetFullPath(Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.Combine(Directory.GetCurrentDirectory(), configuredPath));
}

private static void Build(string outputPath)
{
    var resolvedOutputPath = ResolveOutputPath(outputPath);
    // BuildPipeline.BuildPlayer 使用 resolvedOutputPath。
    Debug.Log("[PaperGame] WebGL build succeeded: " + resolvedOutputPath);
}
```

菜单入口传入 `C1WebGLBuildSettings.Load()?.WebGlOutputPath ?? C1WebGLBuildSettings.DefaultWebGlOutputPath`；命令行入口优先传入非空 `C1_WEBGL_OUTPUT`，否则使用同一配置值。失败异常包含 `resolvedOutputPath`。

- [ ] **步骤 4：运行测试验证通过**

重复步骤 2 命令。

预期：PASS，NUnit 结果中 1/1 通过。

- [ ] **步骤 5：实际构建验收**

运行：
```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$PWD" -executeMethod PaperGame.C1.Editor.C1WebGLBuilder.BuildFromCommandLine -logFile /private/tmp/c1-webgl-build.log
```

预期：退出码 0，`/private/tmp/c1-webgl-build.log` 包含 `[PaperGame] WebGL build succeeded:`，其后的路径是绝对路径，且该目录存在 `index.html`。

- [ ] **步骤 6：提交**

```bash
git add Assets/Editor/C1WebGLBuilder.cs Assets/Tests/EditMode/C1WebGLTemplateTests.cs
git commit -m "feat(WebGL): 支持一键构建和产物日志"
```

### 任务 4：完整回归

**文件：** 无新增修改。

- [ ] **步骤 1：运行全部 EditMode 测试**

运行：
```bash
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /private/tmp/c1-webgl-settings-tests.xml -logFile /private/tmp/c1-webgl-settings-tests.log
```

预期：退出码 0，测试结果 XML 中没有失败项。

- [ ] **步骤 2：提交计划文档**

```bash
git add Docs/superpowers/specs/2026-09-13-webgl-domain-and-build-settings-design.md Docs/superpowers/plans/2026-09-13-webgl-domain-and-build-settings-plan.md
git commit -m "docs(WebGL): 补充域名构建实施计划"
```
