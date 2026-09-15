# 主界面背景拆分 v1

本批仅包含纸张底图与四角装饰，不包含按钮、标题和教程说明卡。
采用内置 imagegen 基于审核过的 home-screen-v1.png 提取并补绘，并非原始分层文件的无损导出；角色轮廓与蜡笔纹理有细微变化，需视觉审核。

| 文件 | 内容 | 建议锚点 / Pivot |
| --- | --- | --- |
| background-paper.png | 纯纸张底图 | 全屏 Stretch |
| corner-01-top-left.png | 太阳与云 | 左上 / (0,1) |
| corner-02-top-right.png | 蓝云 | 右上 / (1,1) |
| corner-03-bottom-left.png | 小鸡、台阶、花草；保留平台内部纸面填充 | 左下 / (0,0) |
| corner-04-bottom-right.png | 台阶、红旗、树、底部花草 | 右下 / (1,0) |

四角均为带真实 Alpha 的 PNG。底图允许拉伸，装饰请等比缩放，不要随宽高分别拉伸。透明边距需通过位置偏移补偿。四角独立定位不保证底部地形在任意比例下无缝相接；极宽屏允许中间留纸面。尚未修改 Unity 场景，也未上传 Figma。

## 最终提示词记录

模式：内置 imagegen 编辑；参考：Docs/ui/review-assets/home-screen-v1.png。

1. Make a transparent PNG cutout of just the smiling sun and its neighboring blue cloud in the top left. Remove everything else. Preserve the reference artwork. Transparent background.
2. Extract ONLY the large blue scribbled crayon cloud near the upper right. No flag, no terrain, no other objects. Tight landscape crop with small transparent margin. Preserve crayon grain and colors, genuine transparent alpha, no annotations.
3. Extract just the bottom-left decoration as a transparent PNG: yellow red-caped chick standing on the left pencil platform, its downward step at right, orange flower and green grasses below. Remove 创建主角 button and fill grass behind it. Remove paper background and all other objects. Keep chick identity and flat original illustration unchanged. Transparent background.
4. Extract the right-side pencil staircase, red finish flag, green tree, sloping bottom ground line and bottom grasses and yellow flower as one transparent PNG cutout. Remove the paper background, tutorial card, all buttons, title, clouds and left-side character and platform. Repair the pencil staircase hidden behind the tutorial card. Preserve flat crayon illustration, no glow, no shadows. Transparent background.

底图复用已审核的 paper-background-stretch-v1.png。
