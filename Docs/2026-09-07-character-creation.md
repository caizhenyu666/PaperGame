# 主角创建与选择

## 使用流程

首页点击「创建主角」，进入角色列表。默认角色使用 `Resources/C1Character` 中现有的 idle、run、jump 图集。点击「拍照创建」后获取 JPEG/PNG 原图并自动上传，等待生成完成后可以预览跑步和跳跃，点击「使用这个主角」再返回首页开始游戏。

生成角色站立时使用 run 第一帧。切换动作使用服务端各动作的 fps；精灵切帧 pivot 按左上原点的 footAnchor 换算，并将角色视觉脚底对齐现有碰撞体底部，保持游戏物理参数不变。

## 服务端配置

`C1CharacterService.BaseUrl` 默认为 `http://10.131.58.235:8000`，可在组件 Inspector 或代码中调整。接口使用 `POST /v1/characters`（multipart 字段 file）、`GET /v1/characters/{jobId}` 和 ready 返回的两张产物图片。

允许 JPEG/PNG，浏览器和 Unity 上传上限均为 10 MiB。轮询间隔 1 秒，最长等待 6 分钟；任务生成失败后允许使用 force=true 重试，同图普通重复上传使用服务端幂等语义。接口只有 run/jump 两个键，客户端使用具名字段解析 animations，没有使用 JsonUtility 不支持的 Dictionary。

项目的 HTTP 下载许可已开启以适配提供的 HTTP 服务。WebGL 页面仍须满足浏览器同源/CORS 和混合内容限制；HTTPS 页面应对接 HTTPS API 或同源代理。

## 保存与恢复

角色编号、动画元数据、所选角色和未完成的任务编号通过 PlayerPrefs 保存。WebGL 保存范围是同一浏览器、同一站点；清除站点数据会清除列表，未实现账号同步。

图集在当前运行期间缓存于内存，重新打开游戏时会从服务端下载已选角色的图集，因此自定义主角需要服务器产物仍可访问。默认角色始终在包内。网络失败时保留默认角色，进入角色页可重试下载；未完成任务可点击「重试 / 继续查询」恢复轮询。

## 代码入口

- `Assets/Scripts/C1/UI/C1CharacterSelection.cs`：角色界面、列表、预览、创建、选择和关卡视觉替换。
- `Assets/Scripts/C1/C1CharacterService.cs`：上传、轮询、下载和响应解析。
- `Assets/Scripts/C1/C1CharacterLibrary.cs`：本地角色记录、契约类型、精灵切帧与资源释放。
- `Assets/Scripts/C1/C1PhotoCapture.cs`：拍照结果事件与照片校验。
- `Assets/Tests/EditMode/C1CharacterServiceTests.cs`：切帧、契约、序列化和本地 HTTP 全链路测试。

## 联调验收

1. 默认角色预览与进入关卡一致，首页停留时角色不移动或下落。
2. 拍照后显示排队/生成提示，下载完成后可以预览 run/jump。
3. 点击使用后进入关卡、重开关卡均使用选中的角色。
4. 重新打开同站点游戏后角色列表和选择恢复。
5. 网络断开、质量不合格、生成失败、图集尺寸错误均提示失败且允许恢复操作。
6. 从实际手机 WebGL 页面验证相机权限、上传、CORS 和透明图集显示。

当前开发环境应使用 `10.131.58.235` 访问服务；本地 HTTP 测试不能替代真实服务器端到端验收。
