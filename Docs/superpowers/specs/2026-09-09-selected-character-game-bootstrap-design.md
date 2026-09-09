# 关卡应用已选自定义角色设计

## 目标

动态创建关卡 Player 后，读取浏览器中保存的已选角色；若选择的是自定义角色，下载其动画精灵帧并替换 Player 的默认动画。

## 数据流

1. 首页角色选择继续通过 `C1CharacterLibrary.Save()` 保存 `selectedId` 与角色记录。
2. `C1GameBootstrap` 创建 Player 时，先同步配置 Resources 中的默认 idle/run/jump 帧，确保关卡可立即游玩。
3. 若 `C1CharacterLibrary.Load().selectedId` 不是 `default`，Bootstrap 在运行时创建或复用 `C1CharacterService`，下载该角色记录的 run/jump 帧。
4. 下载成功后，对新建 Player 的 `C1CharacterAnimator2D` 调用 `ConfigureRemote`，同步更新视觉缩放、脚底偏移与白色渲染色。
5. 找不到角色记录或下载失败时，记录警告并保留默认角色，不阻塞关卡。

## 生命周期与资源

下载帧仅在 Game 场景中持有；由 Bootstrap 在生成对象清理或销毁时释放。异步回调在 Player 或 Bootstrap 已销毁时不再应用帧，避免跨场景访问失效对象。

## 测试

新增 EditMode 测试，验证 Bootstrap 可以解析当前已选的自定义角色记录，并在没有有效记录时维持默认角色。网络下载由现有 `C1CharacterServiceTests` 覆盖；Bootstrap 的帧应用逻辑应暴露为不依赖网络的可测试方法。
