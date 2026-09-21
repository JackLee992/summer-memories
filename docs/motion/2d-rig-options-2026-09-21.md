# 2D 挥锤线稿：本地骨骼与补间方案复核

日期：2026-09-21。范围仅为动画实验，不恢复游戏开发。本轮只读访问官方资料，没有操作桌面、安装软件、下载模型或改游戏源码；未实机运行候选软件。

## 结论

**最值得小试的是 OpenToonz Plastic；Synfig 可作简单单手 FK 对照。** OpenToonz 官方明确提供关节间距保持、关节角范围、挂接点和逐参数速度曲线，这些直接对应现有 11 帧的手肘变形、握点漂移与均速挥锤。它们都不会自动创造重量感：仍需先安排预备、加速、接触、停顿、回收。

本轮不推荐为这段动作引入 enve 的 Mac 源码构建。Blender 并非判定不可用，而是本次官方手册请求被拒，未形成同等级核验证据；不把记忆中的功能冒充此次验证结果。四项均不需要大模型权重。

## 筛选（已证实与待验证分开）

| 方案 | 本次官方证据 | 骨长、握点、时间控制 | 小试判断与限制 |
|---|---|---|---|
| **OpenToonz / Plastic** | 官方仓库有 macOS 构建与安装入口；官方手册源码完整可读。[1–3] | **已支持** Keep Distance 保持关节间距离；Angle Bounds 限制角度；普通绘图层的 Hook 可挂到 Plastic 骨架顶点；顶点 Angle / Distance / SO 可进入 Function Editor，Speed In/Out 手柄可调速度。 | 首选。先只做一只主手与刚性锤子。官方未在上述资料中承诺双手闭链 IK 自动满足；不能声称挂住一只手就解决双手握锤。Keep Distance 是编辑选项，仍应核对所有 Distance 曲线恒定，避免已有关键帧带入伸缩。 |
| **Synfig / Skeleton Layer** | 官方仓库明确 Windows、Linux、macOS；Wiki 有父子骨骼、Link to Bone、长度缩放参数与 Waypoint 插值。[4–7] | **已支持** 单骨对顶点 100% 影响、子骨跟随父骨；固定 Length Setup，Local/Recursive Length Scale 保持 1，可用只转角的 FK 保持长度（这是配置方案，不是独立“锁骨长”按钮的证实）。Constant、Linear、Clamped、TCB、Ease In/Out 可分别设入/出。 | 可做低成本单手剪纸/FK 实验。锤子顶点与手绑定同一骨是握点方案；**未证实原生双手 IK**。不要给锤子使用多骨权重，否则容易弯柄。旧 Wiki 有草稿与年份较早的内容，版本 UI 需实测。 |
| **Blender / Grease Pencil + Armature** | 此次官方手册返回 HTTP 403，官方源码站返回 TLS EOF；记录入口以便后续核对。[8] | 本次**未核实** GP 绑定、IK 禁伸缩、平面关节限制及 F-curve 的当前版本操作，不能将其标为已验证方案。 | 若本机已装且操作者熟悉，可后续验证；本轮不因“功能应当存在”启动新环境。成本主要是绑定和约束学习，不能承诺自动修正原线稿。 |
| **enve** | 官方 README 明确下载为 Linux / Windows；macOS 只提源码构建说明。[9] | 本轮没有取得足够骨长、握点约束证据。矢量动画能力不等于角色骨骼或 IK 能力。 | 当前排除：不符合本轮 Mac 即用、不开环境的目标。 |

## 最小可实施试验：先验证约束，再验证表演

以下是**待执行试验设计**，不是已完成效果。优先使用已安装工具；若没有，不在本轮擅自安装。总时限建议 45–60 分钟，超时保留记录而不继续铺设环境。

1. **只做一条手臂与一把锤，先不画完整人物。** 画肩—肘—腕两个固定长度段，以圆点标关节；锤为独立刚性层。给肘设置可接受的弯曲区间，保持相同弯曲方向。用侧面单手方案先排除双手闭链难题。
2. **30 帧、24 fps、一次挥击。** 暂定 f1 准备、f8 最大预备、f12 加速中、f14 接触、f16 接触停顿结束、f22 跟随/卸力、f30 回收。这是 1.25 秒的测试节奏，不是普适动作模板。锤头路径先画弧线；肩部和身体略领先，锤头在预备末端滞后。
3. **OpenToonz 设置。** 开 Keep Distance；给肘设 Angle Bounds；锤柄握持位置建 Hook，挂到腕顶点。检查 Distance 通道全段恒定，仅调 Angle 通道及必要的整体位移。锤子保持独立刚性层，不依赖网格 Rigidity 模拟刚体。
4. **时间曲线。** 预备慢、出锤快；f14–16 值保持以形成接触停顿，随后小幅反弹/卸力，不把所有段统一 Ease In/Out。Speed In/Out 可调斜率，但不保证自然，必须逐帧看锤头间距。
5. **回到同一准备姿而非直接接上最后一帧。** 本试验先按非循环动作验收。只有 f30 已恢复准备姿、根位置一致，且末端速度衔接可接受时才做循环。复制首帧仅解决位置，不保证速度连续；若循环播放，避免把等同的首尾端点重复停留。
6. 输出低分辨率预览及带关节点版本，与旧 11 帧以相同时长对照。只通过后才加第二只手、完整轮廓和头发。第二只手一旦需要不断追着锤柄修位置，立即将“双手约束”列为下一项独立验证，不扩美术。

### 可量化失败条件

下列数值是本实验建议阈值，不是官方承诺；统一在 512 px 宽预览上检查。

- 肩肘或肘腕距离逐帧变化超过 1%，或肘翻到相反弯曲方向：骨架测试失败；先检查 Distance、缩放、关键帧，不再补轮廓。
- 主握点与锤柄标记距离大于 2 px，或柄长度/直线性发生变化：挂接失败。
- 预备、加速、接触后仍呈近似等距移动，且无明确停顿/卸力：表演失败，增加帧数不能算修复。
- 最后一帧到第一帧任一关节跳超过 2 px，或出现明显方向突变：不能作为无缝循环交付；保留一次性动作版本。
- 工具启动、绑定或 Mac 兼容排障超过 30 分钟仍无法产生三姿态预览：停止工具迁移，报告兼容阻碍；不临时转源码编译。
- 经过一次曲线调整，主观对照仍不能清楚感到“先蓄力、再击中、后卸力”：本试验不通过。约束正确只是必要条件，不能代替动作设计。

## 官方来源与访问记录

均于 2026-09-21 使用 Python urllib 访问；ReadTheDocs 展示页被拒时改读项目自己的文档仓库，未使用非官方教程代替证据。

1. [OpenToonz 官方仓库 README](https://github.com/opentoonz/opentoonz/blob/master/README.md)：macOS 构建、安装入口、许可证与文档仓库链接。本轮成功读取其 raw 内容。未验证本机/芯片兼容。
2. [Plastic 官方文档源码](https://github.com/opentoonz/opentoonz_docs/blob/master/source/create_animations_using_plastic_tool.rst)：Keep Distance、Angle Bounds、Rigidity、Parenting Plastic levels using vertices and hooks、Function Editor representation of Plastic data。本轮成功读取。
3. [曲线编辑官方文档源码](https://github.com/opentoonz/opentoonz_docs/blob/master/source/editing_curves_and_numerical_columns.rst)：Speed In/Out 手柄方向和长度控制速度，Ease In/Out 可设置加减速段时长，Plastic 顶点表达式。本轮成功读取。
4. [Synfig 官方仓库](https://github.com/synfig/synfig/blob/master/README.md)：免费开源、macOS 支持。本轮成功读取。
5. [Synfig Skeleton Layer](https://wiki.synfig.org/Skeleton_Layer)：父子骨、Link to Bone、长度参数。本轮成功读取；页面标记 Draft，最后修改时间显示 2017。
6. [Synfig Skeleton Deformation Layer](https://wiki.synfig.org/Skeleton_Deformation_Layer)：栅格变形、网格范围及影响区域问题。本轮成功读取；不建议刚性锤子走此变形。
7. [Synfig Waypoint](https://wiki.synfig.org/Waypoint)：分别控制入/出插值，Clamped 防过冲。本轮成功读取。
8. Blender 官方待核验入口：[GP Armature](https://docs.blender.org/manual/en/latest/grease_pencil/modifiers/deform/armature.html)、[IK](https://docs.blender.org/manual/en/latest/animation/constraints/kinematic/ik_solver.html)、[F-curves](https://docs.blender.org/manual/en/latest/editors/graph_editor/fcurves/introduction.html)。本轮均 403；另试 projects.blender.org 官方文档源码，TLS EOF。故未据此确认功能。
9. [enve 官方 README](https://github.com/MaurycyLiebner/enve/blob/master/README.md)：Linux/Windows 下载与 macOS 构建说明入口。本轮成功读取，不把“可构建”解释为 Mac 安装即用。
