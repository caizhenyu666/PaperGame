# 关卡应用已选自定义角色实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 `superpowers:subagent-driven-development`（推荐）或 `superpowers:executing-plans` 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 关卡动态创建 Player 后，自动读取并应用当前保存的自定义角色动画。

**架构：** `C1GameBootstrap` 保持默认动画作为同步回退，再从 `C1CharacterLibrary` 读取选中记录。仅当选中自定义角色时启动下载，下载成功后将帧应用到新建 Player 的 Animator；无记录、下载失败或对象销毁时保留默认外观并释放未使用帧。

**技术栈：** Unity 2022、C#、Unity Test Framework（NUnit）、`UnityWebRequest`。

---

## 文件结构

- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs` — 创建角色后读取已选配置、下载并应用远程帧，以及清理下载资源。
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs` — 覆盖已选角色记录的解析与默认回退行为。

### 任务 1：定义可测试的已选角色解析

**文件：**
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`

- [ ] **步骤 1：编写失败的测试**

```csharp
[Test]
public void TryGetSelectedCharacter_ReturnsSavedCustomRecord()
{
    var library = new C1CharacterLibrary { selectedId = "char_selected" };
    var expected = new C1CharacterRecord { characterId = "char_selected" };
    library.characters.Add(expected);

    Assert.That(C1GameBootstrap.TryGetSelectedCharacter(library, out var actual), Is.True);
    Assert.That(actual, Is.SameAs(expected));
}
```

- [ ] **步骤 2：运行测试验证失败**

运行：Unity EditMode 测试 `C1GameBootstrapTests.TryGetSelectedCharacter_ReturnsSavedCustomRecord`。

预期：编译失败，提示 `C1GameBootstrap` 尚未定义 `TryGetSelectedCharacter`。

- [ ] **步骤 3：编写最少实现代码**

```csharp
public static bool TryGetSelectedCharacter(C1CharacterLibrary library, out C1CharacterRecord record)
{
    record = library?.characters?.Find(item => item != null && item.characterId == library.selectedId);
    return record != null && library.selectedId != "default";
}
```

- [ ] **步骤 4：运行测试验证通过**

运行：同一 Unity EditMode 测试。

预期：PASS。

### 任务 2：启动下载并应用远程动画

**文件：**
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`
- 测试：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`

- [ ] **步骤 1：编写失败的测试**

```csharp
[Test]
public void TryGetSelectedCharacter_RejectsDefaultAndMissingRecords()
{
    Assert.That(C1GameBootstrap.TryGetSelectedCharacter(new C1CharacterLibrary(), out _), Is.False);
    Assert.That(C1GameBootstrap.TryGetSelectedCharacter(
        new C1CharacterLibrary { selectedId = "missing" }, out _), Is.False);
}
```

- [ ] **步骤 2：运行测试验证失败**

运行：Unity EditMode 测试 `C1GameBootstrapTests.TryGetSelectedCharacter_RejectsDefaultAndMissingRecords`。

预期：在任务 1 实现前失败；实现后该测试作为回归检查通过。

- [ ] **步骤 3：编写最少实现代码**

```csharp
private void LoadSelectedCharacter()
{
    if (!TryGetSelectedCharacter(C1CharacterLibrary.Load(), out var record)) return;
    StartCoroutine(DownloadAndApplySelectedCharacter(record));
}
```

协程使用 `C1CharacterService.Download` 下载帧；仅在 `Player` 和 `Character Visual` 仍存在时调用 `ConfigureRemote`，更新缩放与脚底偏移。失败时保留默认角色，未应用的帧立即 `Dispose()`。

- [ ] **步骤 4：运行测试验证通过**

运行：全部 `C1GameBootstrapTests`。

预期：PASS。

### 任务 3：完整回归验证

**文件：**
- 修改：`Assets/Scripts/C1/C1GameBootstrap.cs`
- 修改：`Assets/Tests/EditMode/C1GameBootstrapTests.cs`

- [ ] **步骤 1：运行完整 EditMode 测试集**

运行：Unity EditMode 测试程序集。

预期：所有测试通过，且没有新的编译错误。

- [ ] **步骤 2：检查改动范围**

运行：`git diff --check` 与 `git diff -- Assets/Scripts/C1/C1GameBootstrap.cs Assets/Tests/EditMode/C1GameBootstrapTests.cs`。

预期：只包含关卡角色加载和对应测试，不影响现有未提交的场景与项目设置改动。
