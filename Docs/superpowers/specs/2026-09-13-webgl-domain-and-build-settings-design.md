# WebGL 域名与一键构建设计

## 目标

将角色生成服务的基础地址从代码默认值中移出，改为 Unity 项目级配置；同时提供可点击的一键 WebGL 构建入口，构建结束后在 Unity Console 输出产物的绝对目录。

## 范围

- 默认服务地址为 `http://scjjysd.xyz`。
- 在 `Project Settings > Paper Game` 中提供可编辑的服务地址与 WebGL 输出目录。
- 保留 `PaperGame/C1/Build WebGL` 菜单入口，构建时读取配置的输出目录。
- 命令行构建仍可用 `C1_WEBGL_OUTPUT` 覆盖输出目录，以支持自动化。
- 构建成功时输出绝对目录；失败时抛出包含绝对目录和 Unity 构建错误数量的异常。

本次不包含域名的 DNS、服务器部署、HTTPS 证书或 CORS 配置。

## 设计

新增一个 `ScriptableSingleton` 项目设置对象，保存：

- `CharacterServiceBaseUrl`：角色服务基础地址，默认 `http://scjjysd.xyz`。
- `WebGlOutputPath`：项目相对或绝对的 WebGL 输出路径，默认 `Builds/C1WebGL`。

设置对象以 Unity 的 `ProjectSettings` 持久化，在 Project Settings 中通过自定义设置页编辑。地址由 `C1CharacterService` 在未通过 Inspector 显式赋值时读取；现有 Inspector 覆盖能力保留，便于临时调试。

`C1WebGLBuilder` 的菜单构建读取设置中的输出路径；命令行构建优先读取 `C1_WEBGL_OUTPUT`，未设置时读取项目设置。构建前统一将相对路径解析为项目根目录下的绝对路径，避免日志与产物位置含糊。WebGL 模板仍固定为 `PROJECT:PaperGameMobile`。

## 数据流

1. 开发者在 `Project Settings > Paper Game` 编辑服务地址与输出目录。
2. 游戏启动时，未显式配置的 `C1CharacterService` 获取项目设置中的服务地址。
3. 开发者点击 `PaperGame/C1/Build WebGL`。
4. 构建器确定输出目录、完成构建，并将绝对目录写入 Console。
5. 自动化命令若设置 `C1_WEBGL_OUTPUT`，仅覆盖本次构建输出位置，不修改项目设置。

## 错误处理与验证

- 空白或无效的输出目录在构建前报出清晰错误。
- 服务地址为空时回退到设置默认值；请求逻辑继续使用既有错误处理。
- EditMode 测试覆盖默认值、构建器引用设置、命令行覆盖优先级以及成功日志包含绝对路径的静态/可测行为。
- 运行相关 EditMode 测试，并执行一次实际 WebGL 构建，检查 Console/构建日志中的产物绝对路径。
