# 11 帧长柄锤：补间与姿势约束的小试验选择

调研日期：2026-09-21。范围：只读现有接触表与 README、查询上游文档，新增本说明；未安装工具、下载模型、运行推理或修改原 GIF、Piskel 工程及游戏代码。此工作不恢复暂停的游戏开发。

## 判断与最小顺序

**先用原画改时间，再修三个关键姿势；自动补间只值得做两对图的探针。** RIFE/FILM/AnimeInterp 的任务是估计两张输入之间的图像，而不是判断正确人体结构。保留原始关键帧的补间流程会保留已经画错的手肘、握柄与体积；平滑了错误也不等于修好了动作。

检查 `line-test-2026-09-20/st_hizuru_hammer_contact_sheet.jpg`：1–4 是从肩后抬起，5–7 向右下挥出，8 接近脚边最低点，9–11 从左下回肩。当前看起来更像绕轴转锤。第 8 帧没有被画出的目标，因此下面的“命中停顿”只是节奏假设，不是已经存在的接触动作。第 11→1 帧双臂仍跳变，不能靠播放延时修正。

优先级：**A 原图 timing 对照 → B 三姿势约束重画 → C 单次 2× RIFE 探针（可选）**。FILM 与 AnimeInterp 本轮不安装，不同时铺开多条模型管线。

## 四种方案与边界

| 方案 | 上游依据与 Mac 可行性 | 许可证证据 | 对这份粗线稿的实际意义 |
|---|---|---|---|
| RIFE，优先考察 ncnn 移植版 | 原作者实现支持任意中间时刻，README 提到后续动漫优化模型。nihui 的移植版提供 macOS 可执行包、CPU `-g -1`，不要求 CUDA/PyTorch；源码构建有 MoltenVK 选项。这是文档支持，**未在本机验证架构和速度**。移植版 README 模型表最高列到 4.6，不能把上游 4.7–4.10 的动漫优化直接算到该二进制上。[1][2] | RIFE 与移植实现分别为 MIT；实际采用的模型包仍应记录版本、来源和随包条款，不能以代码许可替代未核验的权重许可。[3] | 最便宜的局部平滑探针。细线、交叉手臂、前后遮挡和大角度锤柄运动可能变成双线、断线、弯柄或局部融化；这是根据输入和图像补间机制作出的风险判断，并非本次跑出的结果。不能锁定骨长、握点或命中位置。 |
| FILM | 官方 TensorFlow 2 实现面向大运动和近似照片；安装路径仍列 Python 3.9、CUDA 11.2.1、cuDNN 8.1。没有由这些资料确认的 Apple Silicon/MPS 一键方案，CPU/Metal 兼容需要单独验证，不值得本轮为两个样本搭旧环境。官方 README 链接 HF Space，但本次 HF API 返回 401，**没有确认其在线可用**。[4][5] | 仓库 Apache-2.0；Google Drive 权重没有在本轮单独核验许可。[6] | 可作为未来大位移对照，不是解剖修复器。自然视频训练/演示与粗线稿有域差异；“Large Motion”不能证明其会恢复正确手肘或长柄刚性。 |
| AnimeInterp | 作者明确针对动画的纹理缺失、大幅非线性运动，用色块分割匹配与循环流精炼。README 要求 PyTorch 0.4–1.1，推理与模型源码有直接 `.cuda()`，因此原样不适合这台 Mac；移植代价高。[7][8] | README 声明代码 MIT；内含 RFR 模块另有 BSD-3-Clause，需保留其条款。外链权重许可本轮未单独核验。[7][9] | 比通用补帧更贴近动画，但这份未封闭细线稿缺少稳定色块，不能假设它的优势成立；仍然不提供人体、武器握持或固定骨长约束。本轮排除。 |
| Blender 的固定骨长人偶 + 双手 IK/肘部 pole target + 刚性锤柄，作为重画参考 | 官方源码提供 IK 的 Pole Target、Chain Length、Stretch 开关；Apple 构建配置包含 arm64/x86_64。是本机可行的常规创作路径，**不代表已验证本项目装有可用版本**。[10][11] | Blender 程序 GPL；本轮不引入第三方人物或动作资产，无模型权重。GPL 是软件许可，不能用它代替素材来源记录。[12] | 唯一直接针对当前结构错误的候选：关闭拉伸，设置两节手臂链，肘部 pole 固定弯曲方向；双手目标挂到刚性锤柄两个握点，脚固定时允许骨盆/胸腔转动。无法自动修好原有位图，需要按参考修改关键帧；也不能仅凭 IK 自动得到重量、漂亮剪影或正确身体力学。不可达握点必须移动身体/调整姿势，不能打开拉伸糊过去。 |

## A：可立即执行的原图 X-sheet 对照

编号以接触表 1–11 为准。以下仅改变帧显示时长，不插值、不生成画面，不改变像素。所有时长都是 GIF 10 ms 的整数倍，避免量化混淆。

| 帧 | 原版 A（ms） | 节奏版 B（ms） | 用途 |
|---|---:|---:|---|
| 1 | 80 | 100 | 扛肩起始，读清剪影 |
| 2 | 80 | 100 | 蓄力抬锤 |
| 3 | 80 | 120 | 接近顶点，减速 |
| 4 | 80 | 160 | 顶点蓄力停留；画面无新增后压姿势 |
| 5 | 80 | 40 | 开始快速击出 |
| 6 | 80 | 30 | 快速穿过水平位置 |
| 7 | 80 | 30 | 快速下落 |
| 8 | 80 | 100 | 假设的命中/最低点停顿；等待后续补接触姿势 |
| 9 | 80 | 70 | 跟随与收回 |
| 10 | 80 | 100 | 放慢回肩 |
| 11 | 80 | 130 | 收势 |
| 合计 | 880 | 980 | B 比 A 长 100 ms，节奏差异与总长差异同时存在 |

为排除“仅仅整体变慢”的影响，可增加均匀 C 对照：前 10 帧各 90 ms，第 11 帧 80 ms，合计同为 980 ms。B、C 同长比较更有解释力；C 只有最后一帧不同 10 ms，是 GIF 时间精度下的近均匀基线。

先看单次播放；循环对照保留并明确标注 11→1 跳姿，不反转收回、不用交叉淡化掩盖。若浏览器夹紧 30 ms 显示时间，记录播放器并用支持可变帧时长的视频对照，不能仅凭肉眼认定导出的 timing 被准确执行。可交付新命名的 B/C GIF 与独立 CSV，不覆盖原 GIF。

**它能回答**：现有画面是否因为等时播放而失去蓄力/速度差。**它不能回答或修好**：肘部解剖、手掌握柄、肢体长短、重心转移、目标接触、收回路径以及首尾连续。延长错误姿势甚至可能更显眼；若 B 更有力但仍变形，应进入 B 阶段重画，不能继续堆插帧。

## B/C：限定投入的下一步

1. 姿势约束只做 3 个关键点：第 1 帧起始、第 4 帧蓄力、第 8 帧接触假设。先在固定视角下锁定头身比例、上/下臂骨长、锤柄长度和双手握点；重新选身体转向与目标位置。第 11 帧收势最终必须与第 1 帧一致或设计明确过渡。此阶段是重新制作少量姿势，不是自动修复 11 张旧位图。
2. 如仍需了解自动补帧，RIFE 仅试 3→4（抬锤）和 6→7（快速挥落），各插一个中点，原始输入原样保留。不要插 11→1，也不要先批量 4×/8×。
3. 输出与原画同尺寸并看 100% 局部：手肘、双手、锤柄、锤头四处。任何新增手臂、双线锤柄、握点脱离或线条断裂都判失败，放弃在这组原画上批量补间。一次失败不证明整个模型没用，只表明它不能低成本解决这组输入。
4. 加入一个中点后必须分割原段时长，不能把 80 ms 的原段直接变成两个 80 ms；命中停顿保留静帧，不在 hold 内插出抖动。自动补间的评价和 timing 的评价分开。

## 已访问的一手来源

以下链接在 2026-09-21 用 HTTP 获取 README/源码/许可证核验；HF 特例明确标记失败。未下载任何模型或视频。

1. [RIFE 作者仓库 README](https://github.com/hzwer/ECCV2022-RIFE)：任意时刻补间、后续动漫模型与 macOS PR 指引。
2. [RIFE ncnn Vulkan README](https://github.com/nihui/rife-ncnn-vulkan)：macOS 包、CPU 参数、MoltenVK、模型版本表；这是移植作者仓库，不是原作者的 PyTorch 实现。
3. [RIFE MIT](https://github.com/hzwer/ECCV2022-RIFE/blob/main/LICENSE)；[ncnn 移植 MIT](https://github.com/nihui/rife-ncnn-vulkan/blob/master/LICENSE)。
4. [Google FILM 官方 README](https://github.com/google-research/frame-interpolation)：安装、数据集与逐对补间接口。
5. [官方 README 指向的 HF Space](https://huggingface.co/spaces/johngoad/frame-interpolation)：HF API 本次 401，不作为可立即使用的服务承诺。
6. [FILM Apache-2.0](https://github.com/google-research/frame-interpolation/blob/main/LICENSE)。
7. [AnimeInterp 作者 README](https://github.com/lisiyao21/AnimeInterp)：算法、环境要求及 MIT 声明。
8. [AnimeInterp 推理脚本](https://github.com/lisiyao21/AnimeInterp/blob/main/test_anime_sequence_one_by_one.py)；[模型源码](https://github.com/lisiyao21/AnimeInterp/blob/main/models/AnimeInterp.py)：直接 CUDA 调用。
9. [AnimeInterp 内 RFR 的 BSD-3-Clause](https://github.com/lisiyao21/AnimeInterp/blob/main/models/rfr_model/LICENSE)。
10. [Blender IK 属性源码](https://github.com/blender/blender/blob/main/source/blender/makesrna/intern/rna_constraint.cc)：Pole Target、Chain Length、Enable IK Stretching。
11. [Blender Apple 构建配置](https://github.com/blender/blender/blob/main/build_files/cmake/platform/platform_apple.cmake)：arm64/x86_64 分支。
12. [Blender COPYING](https://github.com/blender/blender/blob/main/COPYING)：GPL。官方网站需求/许可证页面本次 403，手册请求连接失败，因此可行性依据使用源码，不冒称已读到手册具体结论。
