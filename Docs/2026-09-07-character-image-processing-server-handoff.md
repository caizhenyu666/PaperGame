# 主角图片处理服务端化交接文档

**日期：** 2026-09-07  
**对接服务：** `http://scjjysd.xyz:8666/`  
**适用范围：** 纸上游戏机 Unity WebGL 客户端与角色渲染服务端

## 1. 目标

将主角照片从「拍照后由客户端处理」调整为「客户端只采集和展示，服务端统一完成图像处理与精灵生成」。服务端输出可直接在 Unity 中使用的透明 RGBA 精灵表（Sprite Sheet）及切帧元数据。

这样可以统一 OpenCV 与模型处理版本，避免 WebGL 端的性能、包体和浏览器兼容性问题，也让同一张画在不同设备上获得一致结果。

## 2. 当前客户端现状

当前 Unity 工程没有 OpenCV、OpenCvSharp 或其他本地图像算法依赖。现有职责如下：

| 模块 | 文件 | 当前行为 | 迁移后结论 |
|---|---|---|---|
| 手机拍照桥接 | `Assets/Plugins/WebGL/PaperGamePhotoCapture.jslib` | 调用浏览器文件选择/相机，接收 JPEG、PNG 并转为 Data URL | 保留；只做采集与前置格式、大小校验 |
| 拍照组件 | `Assets/Scripts/C1/C1PhotoCapture.cs` | 解码图片用于预览，保存原始字节 | 保留；后续将原始字节上传服务端 |
| 角色动画器 | `Assets/Scripts/C1/C1CharacterAnimator2D.cs` | 播放本地 `idle`、`run`、`jump` Sprite 数组 | 保留；新增运行时替换 `run`、`jump` 帧的入口 |
| 角色资源加载 | `Assets/Scripts/C1/C1GameBootstrap.cs` | 从 `Resources/C1Character/` 读取本地精灵资源 | 保留作默认兜底；服务端生成成功后替换为远端帧 |

**结论：** 当前没有需要从客户端“搬迁”的 OpenCV 源码。需要迁移的是能力边界：所有图像识别、图像规范化、抠图、骨架/关键点分析、动作合成、裁切和透明精灵表导出，均不得在 Unity/WebGL 中实现。

## 3. 目标职责边界

### 3.1 客户端职责

1. 调起相机或文件选择器，获取原始 JPEG/PNG 字节。
2. 在上传前校验格式和文件大小；大小上限统一为 **10 MiB**（服务端契约），替换当前客户端的 15 MB 限制。
3. 上传原始文件，不进行压缩、裁切、旋转校正、抠图、骨架推理或精灵切图。
4. 用不定态进度 UI 展示服务端排队和生成状态。
5. 在 `ready` 后下载 `run.png`、`jump.png`，按服务端返回的元数据切为 Unity `Sprite[]` 并播放。
6. 根据 `needs_correction`、`failed` 和网络错误引导用户重拍或重试。
7. 在服务端结果不可用时继续使用内置默认主角，保证游戏可玩。

### 3.2 服务端职责

1. 校验上传内容：文件头、格式、10 MiB 上限和异常图片。
2. 对原始照片统一执行图像预处理（包括必要的 EXIF 方向校正、颜色/尺寸规范化、去噪、前景分析等）。
3. 使用 OpenCV 和/或模型完成角色主体提取、连通域处理、轮廓与骨架/关节分析。
4. 对无法可靠生成角色的图片返回可解释的 `needs_correction` 结果，不生成不可信精灵图。
5. 根据通过校验的角色素材完成 run、jump 动作渲染、统一裁切、透明通道输出和精灵表拼接。
6. 生成并持久化切帧元数据，保证两个动作的帧尺寸一致。
7. 提供异步任务状态、重试语义、产物下载和可观测日志。

## 4. 服务端处理流水线

```text
原始 JPEG/PNG
  → 文件头与大小校验
  → EXIF 方向校正与像素规范化
  → OpenCV 图像预处理（按算法需要）
  → 主体/轮廓/骨架或关键点分析
  → 质量门禁
      ├─ 不通过：needs_correction + reason + 可用的 joints/mask
      └─ 通过：角色贴图与动作渲染
                 → run/jump 统一包围盒裁切
                 → 输出透明 RGBA 横向精灵表
                 → 写入动画元数据与 ready 状态
```

### 4.1 服务端输出约束

| 项目 | 必须满足的约束 |
|---|---|
| 图片格式 | `run.png`、`jump.png` 均为带 Alpha 的 RGBA PNG |
| 精灵表布局 | 单行横向拼接；总宽 = `frameWidth × frameCount`，总高 = `frameHeight` |
| 动作集合 | `ready.animations` 仅含 `run` 和 `jump` |
| 尺寸一致性 | run 与 jump 的 `frameWidth`、`frameHeight` 必须一致 |
| 帧参数 | 每个动作必须提供 `frameCount`、`fps`、`frameWidth`、`frameHeight`、`footAnchor` |
| 脚底锚点 | 坐标使用左上为原点、y 向下；当前约定为 `(frameWidth / 2, frameHeight)` |
| 失败可解释性 | 业务质量失败使用 `needs_correction`；基础设施失败使用 `failed`，不可混用 |

## 5. 接口契约与客户端调用顺序

### 5.1 上传

- **请求：** `POST /v1/characters`
- **Content-Type：** `multipart/form-data`
- **文件字段名：** `file`
- **文件类型：** PNG 或 JPEG
- **文件上限：** `10 * 1024 * 1024` 字节
- **成功响应：** `202 {"jobId":"char_..."}`

服务端以文件字节 SHA-256 的前 12 位生成 `jobId`。同一图片重复上传必须返回同一任务，不应重复渲染；只有显式传入 `force=true` 时才强制重跑。

### 5.2 轮询

- **请求：** `GET /v1/characters/{jobId}`
- **间隔：** 1 秒
- **客户端超时：** 至少 5 分钟
- **终态：** `ready`、`needs_correction`、`failed`

```text
POST → queued → processing → ready
                         ├→ needs_correction
                         └→ failed
```

### 5.3 成功结果

`ready` 时，服务端必须返回以下结构；`spriteSheetUrl` 为站内绝对路径，客户端以 Base URL 拼接：

```json
{
  "status": "ready",
  "characterId": "char_9c3ff81ce4ea",
  "animations": {
    "run": {
      "spriteSheetUrl": "/artifacts/char_9c3ff81ce4ea/run.png",
      "frameCount": 10,
      "fps": 15,
      "frameWidth": 241,
      "frameHeight": 275,
      "footAnchor": { "x": 120, "y": 275 }
    },
    "jump": {
      "spriteSheetUrl": "/artifacts/char_9c3ff81ce4ea/jump.png",
      "frameCount": 7,
      "fps": 15,
      "frameWidth": 241,
      "frameHeight": 275,
      "footAnchor": { "x": 120, "y": 275 }
    }
  }
}
```

客户端对每帧使用：`x = frameIndex * frameWidth`、`y = 0`、`width = frameWidth`、`height = frameHeight`。Unity 的 Sprite pivot 应由 `footAnchor` 换算为 `(x / frameWidth, 1 - y / frameHeight)`。

## 6. 结果与错误处理约定

| 服务端结果 | 典型原因 | 客户端动作 | 服务端要求 |
|---|---|---|---|
| `queued` / `processing` | 正在排队或生成 | 持续显示“正在生成主角” | 更新 `updatedAt`，不返回伪进度 |
| `ready` | 成功生成 | 下载精灵表并替换默认角色 | 所有元数据完整，静态文件可立即下载 |
| `needs_correction` | 人形或骨架质量不满足门禁 | 提示重拍/重画；可选展示 mask、joints | 返回稳定的 `reason`；早期失败允许 joints 为空 |
| `failed` | 渲染超时、崩溃、资产缺失 | 提示重试；保留默认角色 | 返回 `code`，记录可定位日志 |
| HTTP 400 | 格式或文件过大 | 本地提示后重拍 | 使用 `FILE_TOO_LARGE`、`NOT_AN_IMAGE`、`UNSUPPORTED_FORMAT` |
| HTTP 503 | 队列不可用 | 稍后重试 | 返回 `QUEUE_UNAVAILABLE` |

## 7. 服务端实现任务清单

### P0：上线前必须完成

- [ ] 将 OpenCV 处理链路放入异步 worker，不在 HTTP 请求线程执行长耗时推理。
- [ ] 将原始输入、处理后的中间文件、最终精灵表和 `result.json` 按 `jobId` 隔离存储。
- [ ] 实现上传幂等：同字节文件重复上传不产生重复任务。
- [ ] 实现 `queued`、`processing`、`ready`、`needs_correction`、`failed` 五态及终态不可覆盖规则。
- [ ] 输出符合第 4.1 节的 run/jump RGBA 精灵表和完整元数据。
- [ ] 为超时、子进程崩溃、损坏产物增加日志、错误码和一次自动重试。
- [ ] 配置 CORS，仅允许 WebGL 实际部署来源访问 API 与 `/artifacts/**`。
- [ ] 关闭公网匿名读取，或为任务查询与产物下载引入短期访问凭证。

### P1：体验与运维完善

- [ ] 为 `needs_correction` 输出实际存在的 mask、texture 和 joints，便于客户端引导重拍。
- [ ] 保存输入质量与各阶段耗时指标，监控排队时长、成功率、质量失败分布和渲染错误率。
- [ ] 为 worker 设定并发上限、队列积压告警和磁盘产物清理策略。
- [ ] 制定产物与原图保留时长；当前任务状态至少应能覆盖客户端 5 分钟轮询窗口。

## 8. 联调前置条件

1. `GET http://scjjysd.xyz:8666/healthz` 返回 `200 {"status":"ok"}`。
2. WebGL 所在域名已被服务端 CORS 白名单允许；若通过 HTTPS 提供游戏页面，API 也必须通过 HTTPS 提供，避免浏览器混合内容拦截。
3. 服务端提供至少 3 份固定测试图片及预期终态：成功、`needs_correction`、`failed` 或可模拟的队列不可用。
4. 上传文件大小限制在浏览器桥接、Unity 组件、服务端三处统一为 10 MiB。
5. 服务端确认 `ready` 返回的两个 PNG 在响应返回时已可下载，避免状态与产物可见性不一致。

## 9. 验收标准

### 功能验收

- 使用有效手绘角色 JPEG/PNG 上传后，客户端在 5 分钟内取得 `ready`，能正确播放 run 和 jump。
- 同一张图片连续上传 2 次，只产生 1 个 jobId 和 1 次有效渲染任务。
- 非人形或质量不足图片返回 `needs_correction`，不会返回空白、无 Alpha 或尺寸错误的精灵表。
- `run` 与 `jump` 帧尺寸一致，角色脚底在跨帧和跨动作时不发生明显跳动。
- 断网、503、超时、服务端崩溃时，客户端保留默认角色且可重新发起生成。

### 安全与部署验收

- 从实际 WebGL 页面发起上传、轮询、下载均不会产生 CORS 或混合内容错误。
- 未授权的第三方不能通过猜测 `jobId` 读取儿童原图、mask、贴图或精灵表。
- 服务端日志可通过 `jobId` 追踪完整处理链路和失败阶段。

## 10. 待服务端确认事项

1. 当前 `http://scjjysd.xyz:8666/` 是否仅用于联调；正式环境的 HTTPS 域名和 CORS 来源列表是什么？
2. 原图、中间产物和最终精灵图的存储位置、保留时长、访问鉴权策略分别是什么？
3. OpenCV 流水线的质量门禁阈值、失败 reason 映射和可观测指标由谁维护？
4. `idle` 动画是否继续使用客户端内置素材，还是服务端后续也需要生成？当前接口只约定 run 与 jump。
5. `needs_correction` 时是否计划增加关节回传/编辑接口？当前仅支持修改原图后重新上传。

## 11. 参考

- 服务端接口契约：`/Users/caizhenyu/Work/AI Work/API.md`
- 客户端拍照组件：`Assets/Scripts/C1/C1PhotoCapture.cs`
- WebGL 拍照桥接：`Assets/Plugins/WebGL/PaperGamePhotoCapture.jslib`
- 客户端角色动画器：`Assets/Scripts/C1/C1CharacterAnimator2D.cs`
- 客户端角色装配：`Assets/Scripts/C1/C1GameBootstrap.cs`
