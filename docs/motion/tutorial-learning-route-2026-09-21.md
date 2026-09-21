# 能直接跟做的动画教程与挥锤练习

查验日期：2026-09-21。目标是由我们自己照教程做一个可修改的挥锤动作，先得到动作练习结果。下面筛选了 4 组公开教程，每组都有具体练习；不要求先学完整角色建模或制作整套游戏动画。

**本轮优先顺序：动作节奏小练习 → BVH 导入并看原始动作 → 修改少数关键姿势。** 如果原始 BVH 有可用动作，再做角色重定向。2D 换画作为另一条练习路线，不与 3D 环境同时铺开。

本文中的帧数、限时和挥锤作业是针对本项目制定的练习，不冒充教程作者的原话。网页和代码说明已读；两段 YouTube 只核实到标题、作者和来源链接，**没有观看视频或取得字幕**。

## 1. 先学会让动作有快慢：Alan Becker《12 Principles of Animation》

- 视频：[12 Principles of Animation (Official Full Series)](https://www.youtube.com/watch?v=uDqjIdI4bF4)
- 作者：AlanBeckerTutorials；英文。未验证中文字幕可用性。
- 查验：[YouTube oEmbed](https://www.youtube.com/oembed?url=https%3A%2F%2Fwww.youtube.com%2Fwatch%3Fv%3DuDqjIdI4bF4&format=json) 返回 HTTP 200，标题和作者与上面一致。只验证身份和可访问元数据，不据此声称看完课程。

看课时优先找这几个主题：**anticipation（预备）、pose to pose（关键姿势）、slow in / slow out（缓入缓出）、timing（时序）、follow through / overlapping action（跟随与动作交叠）**。这些是要学习的主题；本次没有核实视频时间码，因此不提供未经核实的跳转点。

### 跟做作业：同一套姿势做两版节奏

1. 用圆形头、线段躯干、简单锤子即可，画出 5 个姿势：准备、向后蓄力、开始下砸、触地、卸力。
2. A 版把 5 个姿势平均分配时间。B 版保留较长预备，缩短下砸，接触后稍停，再慢慢收回。先完全不加中间帧，观察能否仅靠停留时间看出重量变化。
3. 在 B 版中加入一张下砸中间姿势，把锤头在相邻画面之间的距离拉开；靠近蓄力极限时位置变化较小。不要把所有中间姿势等间距摆放。
4. 将骨盆/胸部启动与锤头启动错开，再查看是否出现“人带动锤”的感觉。这个安排需要按具体握持姿势修改，不能机械地每个部位都延迟固定帧数。

**交付**：两个同长度的粗线拍，关键姿势完全相同，只有时序和中间位置不同。**验收**：正常速度能分辨哪版更重；关闭音效、震屏仍然成立。计划练习约 20–30 分钟，不是课程时长。

**素材许可**：本练习自己画几何形状，不提取视频画面/角色作为项目素材。公开视频的观看权限不自动成为画面再利用许可。

## 2. 实际操作关键姿势和洋葱皮：Krita 官方步态循环教程

- 中文：[使用 Krita 制作动画](https://docs.krita.org/zh_CN/user_manual/animation.html)
- 英文：[Animation with Krita](https://docs.krita.org/en/user_manual/animation.html)
- 直接跟做章节：`Introduction to animation: How to make a walk cycle`，含 Setup、Animating、Expanding upon your rough walk cycle、Exporting。
- 查验：中英文正文均 HTTP 200；已读取完整操作步骤。中文版页面修订号为 `c7c02a5`，英文页面显示 Krita Manual 5.3.0。

这不是只介绍原则的文章。教程从新建文件开始，要求切到 Animation 工作区，创建环境层与动作层，画地面，制作两个极端姿势，再复制为四张绘画、区分前后腿、播放，并插入停留格和中间画。

### 跟做作业：先完成原教程的四帧，再换成挥锤

1. 照原教程做四张步态绘画，按教程用 4 fps 播放，确认自己能创建、复制、移动、延长一张画。
2. 注意原教程强调的区别：已有绘画要用 **Create Duplicate Frame** 保留；Create Blank Frame 会产生空白帧。绘画层背景要透明，白色不是透明，洋葱皮才能按预期显示。
3. 新建挥锤练习，画固定地面，把上一组的 5 个粗姿势放到时间轴；改变各姿势的停留长度，而不是先画很多中间张。
4. 开洋葱皮，只显示相邻前后帧；先标出支撑脚的落点、两手握点、锤头路径，再补 2–3 张最需要的过渡。
5. 分别导出 PNG 序列和预览。保留 `.kra`，使以后可以直接改画和改时间。

**交付**：一个教程步态小样、一个挥锤 `.kra`。**验收**：能在时间轴上单独延长蓄力、提前命中；支撑脚不会因每张画位置不同而左右漂移。完成前 4 帧不代表学会成熟的中间画，原文也明确说中间画本身需要额外学习。

**素材/样例**：页面提供逐步图示，这次未查到页面链接的可下载 `.kra` 工程；不把图示说成工程包。按步骤从空白文件自画。文档页脚许可为 **GNU Free Documentation License 1.3+，除另行说明外**；该文档许可与用户自己画出的练习文件不是一回事。

## 3. 把 MoMask 的 BVH 变成可以改的骨骼动作：官方步骤 + KeeMap 作者教程

- [MoMask 官方 README：Visualization / Retargeting](https://github.com/EricGuo5513/momask-codes#dancers-visualization)
- [KeeMap 官方 README：WorkFlow / Quick Start](https://github.com/nkeeline/Keemap-Blender-Rig-ReTargeting-Addon#workflowquick-start)
- [作者视频：Blender Retarget Addon and Tutorial](https://www.youtube.com/watch?v=EG-VCMkVpxg)，频道 Checkered Bug。MoMask 与 KeeMap 两个仓库都指向这段视频。
- [插件 releases](https://github.com/nkeeline/Keemap-Blender-Rig-ReTargeting-Addon/releases)

查验：两个 README 的原始文件与 GitHub API 均 HTTP 200；已读文字步骤。视频 oEmbed HTTP 200，已核实标题/作者，未观看视频。KeeMap README 明确写 **tested and working on Blender 3.3.1**，并提醒新版 Blender 可能破坏兼容性。因此下述是已公开的教程路径，不是已经在当前本机 Blender 验证过的操作结果。

### 第一段作业：只有 BVH，也要先拿到可编辑结果

1. 先取得一段自己的 MoMask 输出，保留 `.bvh` 与预览。MoMask 官方说明输出位于 `generation/<ext>/animation/`，包含 BVH；它用 20 fps，带 `_ik` 的结果应用了简单足部 IK，但作者提醒有时会失败。
2. 在 Blender 中导入 BVH，先只看原始骨架。核对时长、朝向、地面与帧率，保持固定侧视。不要此时就叠加新人物美术来判断质量。
3. 标记“蓄力、发力、接触、卸力”实际发生的帧号。如果动作没有单次挥击语义，记录不合格，不把错误动作的顺滑播放当作成功。
4. 保存 `.blend`，确认能够选择骨骼、查看关键帧，并在副本上修改一个姿势后播放。**这是 BVH 可编辑性的最小验证**，无需先安装重定向插件。

原始骨架通常有密集关键帧；“导入后可编辑”只表示能改骨骼数据，不表示已经得到易操作的 Rigify 控制器或整理好的少量关键姿势。

### 第二段作业：原动作可用后，才跟做重定向

下面顺序来自 MoMask 与 KeeMap 的文字教程：

1. 安装 KeeMap **releases 中的插件 ZIP**。作者特别提醒，不要把整个源码仓库 ZIP 当插件安装，也不要装“ZIP 里面还有 ZIP”的包装文件。
2. 将源 BVH 骨架和目标角色骨架放在同一 `.blend`；同时选中两个骨架，切到 **Pose Mode** 才能看到 KeeMap 界面。
3. 设置 Source Rig 与 Destination Rig。若目标采用教程中的 Mixamo 骨名，可试 MoMask 的 `assets/mapping.json`，不适配再看 `mapping6.json`。这是作者给多数 Mixamo 角色制作的映射，**不声称自动适用 Rigify 或本项目任何自定义骨架**。
4. 对自定义目标骨架，按 KeeMap Quick Start 从 root 开始逐个配置，按父到子的顺序排列。先使用 **Test / Test All** 在几个关键姿势上检查，再批量 Transfer。
5. 如需少量关键姿势，KeeMap 文档提供 **KeyFrame Test**：到目标时间点，勾选关键帧选项并 Test All，可以只在选定时间设关键帧。另有 KeyFrame Number 控制批量采样间隔。先检查姿势，不能把稀疏采样本身当作动画优化。
6. 确认映射后设置 Starting Frame、Number of Samples，执行 **Transfer Animation from Source to Destination Character**。保存映射文件和 `.blend`。

KeeMap 还说明可由 FK 源定位目标的 IK 控制骨、配置 pole 方向与位置偏移，对之后锁脚/握持有用；但本轮未验证某一 Rigify 模板映射。**它不会自动建立锤子刚体、两手握持、命中目标这几个约束。** 手与锤的关系仍需在可编辑工程中单独检查和修正。

**交付**：原始 `.bvh`、原始导入 `.blend`、如兼容则另存重定向 `.blend` 与 mapping JSON；各有固定侧视预览。**验收**：改变一个关键姿势后，重新播放能看到修改；脚与握点是否正确另行记录。插件安装失败应保留错误与软件版本，不把实验全耗在换环境。

### 随教程样例与许可

| 示例 | 已验证内容 | 当前用途与许可边界 |
|---|---|---|
| [MoMask mapping.json](https://github.com/EricGuo5513/momask-codes/blob/main/assets/mapping.json) / [mapping6.json](https://github.com/EricGuo5513/momask-codes/blob/main/assets/mapping6.json) | 官方 assets 目录中分别为 16,244 / 16,468 字节；README 明确给出使用方法 | 仓库代码 MIT，适合学习映射结构并按实际骨名调整；此许可不能替代角色模型、训练数据、依赖库各自许可 |
| [KeeMap example/Retarget Addon.blend](https://github.com/nkeeline/Keemap-Blender-Rig-ReTargeting-Addon/blob/main/example/Retarget%20Addon.blend) | GitHub 目录 API 核实文件存在，57,192,780 字节；本次未下载或打开 | 仓库有 GPL-3.0 LICENSE；未核实其中角色/动作的单独素材许可，不能据此保证内嵌资产可用于本项目发布。作为公开示例入口列出，不复制其中人物进入交付 |
| [KeeMap example 目录](https://github.com/nkeeline/Keemap-Blender-Rig-ReTargeting-Addon/tree/main/example) | 还包含 `Radical2Daz.json`、`Radical2MakeHuman.json`、`Snow Bone Map.txt` 和 FBX | 适合查看映射命名与配置方式；不是已验证可直接套到 MoMask 的完整工程包 |

实际动作练习优先使用本轮生成的 BVH 和自行建立的简单骨架/几何形状，避免为了跟教程额外引入角色资产。MoMask README 的许可附注仍需保留：SMPL、SMPL-X、PyTorch3D 与所用数据集各自有许可，MIT 代码不能自动证明全部生成资产的商业授权。

## 4. 让 2D 角色能换姿势又不断关节：OpenToonz 官方剪纸与 hooks 教程

- [Creating Cutout Animation（排版文档）](https://opentoonz.readthedocs.io/en/latest/creating_cutout_animation.html)
- [同页官方文档源码](https://github.com/opentoonz/opentoonz_docs/blob/master/source/creating_cutout_animation.rst)
- 已读取源码的章节：Using the Skeleton Tool、Creating Basic Models、Creating Models with Hooks、Animating Models、Inverse Kinematics。
- 查验：本次实际读取的 raw GitHub 正文 HTTP 200；排版页只作为阅读入口，不声称本轮已读取其网页。源码有明确编号步骤和图示引用。

### 跟做作业：只做“肩 → 上臂 → 前臂 → 手”与两张躯干

1. 自己画躯干、上臂、前臂、手，每个部件放不同 column/layer。先做普通站姿，不必清稿上色。
2. Skeleton 工具切到 **Build Skeleton**：拖黄色圆点设 pivot，拖手柄方块连到父部件。从躯干向手依次连接。
3. 切 **Animate**，在两个时间点摆出抬臂与放下，确认带动关系。此时基础 pivot 是固定的，不能靠转动层解决所有转面。
4. 给躯干画第二张“向前俯身”的替换画。用 **Hook** 工具在两张躯干分别标出肩关节；把上臂连接到对应 hook。逐帧切换躯干画，检查肩膀是否仍连接。
5. 手部也可用两张绘画表达握锤方向变化；这一练习只验证换画与连接，先不加入复杂手指。需要改前后遮挡时明确调整层级，不靠拉伸原来的手掌制造转面。
6. 再试 Skeleton 的 **Inverse Kinematics**。教程区别了持续保留的 pinned center 与仅当前帧使用的 temporarily pinned center；后者不能当作跨帧自动锁脚。

**交付**：一个 OpenToonz 场景，包含原有部件层、两张躯干绘画与 hooks。**验收**：切换两张躯干时手臂不脱落；能单独修改一张绘画，且不必重做整个动画。这个练习直接对应挥锤极端俯身、肩膀转面时“骨骼摆得动但画面仍僵”的问题。

**样例许可**：本文只依据官方文档步骤，未下载第三方角色/工程。官方 docs 仓库的 GitHub license API 本次返回 404，不能从 OpenToonz 软件的许可反推文档图例可随意变成游戏资产；练习用自画部件。

## 今天实际该做哪一件

用 **一段自己的 BVH + 简单骨架** 完成教程 3 第一段，取得能播放、能改姿势的 `.blend`；同时用教程 1 的节奏练习，判断生成动作中哪些时间点值得保留和改动。下一步的判断只看这一个小样：

- 有挥击动作、修改后更清楚：再尝试重定向，并加锤子握点。
- 能导入，但语义或受力不对：保留为失败对照，用教程 2 的关键姿势重新设计；不要继续叠加精细美术。
- 决定使用纯 2D：只做教程 4 的一条手臂和一次躯干换画，先证明换画连接可维护。

不要求所有教程做完才动手。本文没有声称这些练习已经完成；实际生成、导入、改动和画面结果以本轮试验输出为准。

## 查验记录与没有采用的入口

2026-09-21 读取的仓库 HEAD：

- MoMask：`94a6636c9c463b7a9414c3401a6f1b67e6c51824`。
- KeeMap：`c34d19e18d221121ef0b8ad2421c6b1097040b31`。
- OpenToonz 文档：`d0cb6a49f3b489c455bc6cf7674ff98f5682dccf`。

本次 Blender Studio 的 Animation Fundamentals、Blender 官方 demo files、Blender manual 的 BVH/Rigify 页面均返回 HTTP 403。因此没有把这些课程中的教学细节、示例许可或软件操作当作本次已核实证据。BVH/重定向部分采用实际读取到的 MoMask 与 KeeMap 官方文字步骤。
