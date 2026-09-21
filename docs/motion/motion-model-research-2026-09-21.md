# 动作生成与视频动捕调研（2026-09-21）

范围：只读检查官方 GitHub、官方文档与 Hugging Face 模型卡。未安装依赖、下载权重、生成动作或实测耗时。项目仍以横版 2D 为目标：3D 骨架可以作为连续线稿参考，或固定镜头渲染成 2D 序列；不因此恢复 3D 游戏开发。

## 结论与优先级

1. **先完成当前手绘动作的连续性验证。** 人物比例、锤子长度、固定握点、支撑脚、蓄力/命中/收招节奏，是所有自动方案都绕不过去的验收标准。
2. **最值得验证的动作生成：Kimodo-SOMA-RP-v1.1。** 已有姿势/手脚/路径约束、接续生成和官方 BVH 导出，适合先做一个 2–4 秒完整挥锤动作；前提是可用 NVIDIA GPU 和 gated Llama 文本编码器访问权限。本机 Mac 不应盲装 CUDA 环境。
3. **最值得本机验证的视频动捕：GEM-X 的 Apple Silicon ONNX 路线。** 官方现有 macOS 文档，能从清楚、固定镜头、单人全身参考视频估计连续人体动作。但锤子、头发、衣服不在人体骨架中，需要另做；输出不是直接可用的 Unity 人物动画包。
4. **MDM/DiP、GVHMR 作为研究比较，ARDY/MotionBricks 后置。** 前两者存在老依赖/许可或转换成本；后两者偏实时生成、机器人演示，当前离线小动画没有必要承担其集成成本。

## Kimodo：对我们的帮助最大在哪里

官方仓库：[nv-tlabs/kimodo](https://github.com/nv-tlabs/kimodo)。本次读到 main 提交 `1aece8c124d73d255ceff5086d983b844c9f4e94`。

| 核查项 | 官方信息及本项目含义 |
|---|---|
| 输入 | 文本、时长、全身关键姿势、手脚位置/旋转、平面路径/路点。可先规定扛肩、蓄力、命中、收回姿势，再生成中间运动。 |
| 连续性 | 多段提示逐段生成，后段以前段末尾若干帧为条件。过渡消耗后段开头时长，不是每段独立生成后硬拼。 |
| 限制 | 每个提示最长 10 秒；除密集根路径外，每类约束建议少于 20 个关键帧；模型本身可能滑步、不能精确命中约束，需要开启后处理。 |
| 输出 | 默认 NPZ；**SOMA 现已支持 `--bvh` 导出**。输出为 77 关节，模型内部为 30 关节。不要依据较旧模型卡中的 30 关节说明误写导出映射。 |
| BVH | 存 root translation 和 local rotation；米转换为厘米；默认 rest pose 与 BONES-SEED 一致，可用 `--bvh_standard_tpose`。并未核实存在官方 Unity FBX 一键导出。 |
| 帧率 | 模型卡：30 FPS，单提示最多 300 帧。最终 2D 可重新编排采样和停顿，不应机械等间隔删帧。 |
| 硬件 | 官方 README：全 GPU 约 17 GB 显存，`TEXT_ENCODER_DEVICE=cpu` 可降至小于 3 GB 显存，但文本编码更慢且系统内存需求没有给出。不能把“小于 3 GB”理解成无需显卡或总内存小于 3 GB。 |
| 平台 | Linux 为主，Windows/Docker 路线；测试 3090/4090/A100 等。没有查到官方 Mac/MPS 支持承诺。 |
| 隐性准备成本 | 文本编码器依赖 gated `Meta-Llama-3-8B-Instruct`，需要 HF 账户获准访问与 token；首次使用自动下载模型。 |
| 许可 | 代码 Apache-2.0；SOMA/G1 权重 NVIDIA Open Model License；**SMPLX 权重 NVIDIA R&D Model License**，不能混为一谈。正文模型卡称 SOMA-RP-v1.1 ready for commercial use，仍需保留模型、第三方依赖和输入资产许可记录。 |
| 不解决的问题 | 不制作人物网格、绑骨、衣服/头发动画，不理解场景中的锤子碰撞与双手握持约束整体，也不能保证动漫夸张动作或特定影视武术。 |

官方自述覆盖 locomotion、gestures、everyday activities、videogame combat 等，不能因此保证“扛肩长柄锤挥击”一遍成功。模型卡明确指出不感知周边场景对象、以真实人体动作为目标。长柄锤应作为固定尺寸刚体，额外使用握持点与 IK 校正。

### 可行的制作链（建议，尚未实施）

1. 先保留手绘动作设计：扛肩待机 → 压重心蓄力 → 挥锤 → 命中停顿 → 跟随 → 收回。用同一人物骨长和锤子模型贯穿。
2. 在 Kimodo 中给少量全身/手部关键约束，生成一个完整短片段；对比少量候选，不无限重试提示词。
3. 导出 SOMA BVH，在 Blender 中导入；核对厘米到米、坐标朝向、T-pose/rest pose、根骨位置。
4. 将 SOMA 动作重定向到**已有且正确绑定**的角色骨架；校正脚底接触、膝肘、双手握点。给锤子单独父子关系/约束，烘焙动画。
5. 固定正交相机，固定画布、脚底锚点、灯光和线稿材质；输出透明 PNG 序列。人物与道具一致性来自同一模型，避免逐帧重新生成形象。
6. 为本 2D 游戏采用统一 pivot 的 sprite sequence；若需要未来 3D 复用，再导出角色/动画 FBX，通过 Unity Humanoid Avatar 配置和 AnimationClip 验证。BVH 本身不是可承诺直接拖进 Unity 就正确运行的交付格式。

验收先看：连播五次无跳姿；肩扛锤与手腕不穿插；握点不滑；支撑脚不漂；打击轮廓横版镜头可读；回到待机不卡顿。记录人工修正分钟数，再决定是否比手绘省钱。

来源：
- [README / 环境 / 模型许可](https://github.com/nv-tlabs/kimodo)
- [官方输出格式（BVH 与 NPZ）](https://github.com/nv-tlabs/kimodo/blob/main/docs/source/user_guide/output_formats.md)
- [官方限制与多段过渡](https://research.nvidia.com/labs/sil/projects/kimodo/docs/key_concepts/limitations.html)
- [官方安装与 gated 模型依赖](https://research.nvidia.com/labs/sil/projects/kimodo/docs/getting_started/installation.html)
- [SOMA-RP-v1.1 模型卡](https://huggingface.co/nvidia/Kimodo-SOMA-RP-v1.1)

## GEM-X：视频参考转连续人体骨架

官方：[NVlabs/GEM-X](https://github.com/NVlabs/GEM-X)；本次 main 提交 `32992550dba114c62243fb55e361311972dce8f9`。

- 输入单目视频，输出 SOMA 77 关节的人体姿态与全局运动；含手部，支持动态相机估计。适合从自摄或可使用的动作参考中获得节奏、重心和人体关节运动。
- **官方 macOS 文档现有 Apple Silicon 流程**：macOS 13+、M1/M2/M3/M4、Python 3.12+、约 5 GB 模型与资产磁盘空间，ONNX Runtime + CoreML；完整离线 PyTorch 管线仍以 NVIDIA GPU 为优选。这里是文档声明，不是本机测得性能。
- 输出含关键点预览视频、人体网格预览、`preprocess/hpe_results.pt` 等。`--retarget` 导出的 BVH/CSV 是 **Unitree G1 机器人**版本，不应当成游戏人物的标准 Humanoid 导出。
- 要用于本游戏仍需 SOMA 人体到角色骨架的转换与清理。摄像机运动、遮挡、快速模糊、武器遮挡手腕、宽袖都会提高误差；从 Seedance 生成视频动捕会把生成视频中的身体错误带入骨架，并不会自动修好。
- 代码 Apache-2.0，模型 NVIDIA Open Model License。README 的“Apache licensed / commercially usable”应和最后 GOVERNING TERMS 及 HF 模型卡一起读，不把代码许可等同权重许可。
- HF 卡仍写 Linux/CUDA 为支持平台，而仓库已有单独 Mac ONNX 文档；应使用后者的小样验证路线，不能宣称所有训练/推理功能在 Mac 都等价。

来源：[Mac 安装](https://github.com/NVlabs/GEM-X/blob/main/docs/INSTALL_MACOS.md)、[Demo 输出](https://github.com/NVlabs/GEM-X/blob/main/docs/DEMO.md)、[HF 模型卡](https://huggingface.co/nvidia/GEM-X)。

## 其他路线筛选

| 项目 | 能帮什么 | 本次确认的成本/限制 | 优先级 |
|---|---|---|---|
| [MDM / DiP](https://github.com/GuyTevet/motion-diffusion-model) | 文本生动作、中间段补全、固定下身改上身 | README 测试 Ubuntu/CUDA；输出 `results.npy` 关节与 stick figure MP4，可另经 SMPLify 转 SMPL 参数/逐帧 OBJ。代码 MIT，SMPL/SMPL-X/数据另有许可。官方速度不是我们机器性能。 | 研究备选；不优先于有 BVH/约束界面的 Kimodo |
| [GVHMR](https://github.com/zju3dv/GVHMR) | 视频恢复人体、重力对齐的世界运动，静态镜头可跳过 VO | 依赖 SMPL/SMPL-X 及若干检测权重；**仓库 LICENSE 仅教育/研究/非营利用途，商用需联系作者**。本次未确认 Mac 支持或最低推理显存。 | 动捕质量比较可用，产品制作管线暂不选 |
| [ARDY](https://github.com/nv-tlabs/ardy) | Kimodo 同系实时自回归动作，连续文本/路径/关键姿势控制 | Linux/4090 为主要测试平台；gated Llama；输出 NPZ，G1 另 CSV；代码 Apache-2.0、权重 NVIDIA Open Model。当前 README 已发布 Core/G1，SOMA 仍列 coming soon。 | 当前离线短动作收益小，后置 |
| [MotionBricks](https://github.com/NVlabs/GR00T-WholeBodyControl/tree/main/motionbricks) | 研究实时动作过渡、smart primitives、动作组合思路 | 已公开代码/权重，不只是论文。CUDA；约 2.2 GB checkpoint（**不是显存要求**）；当前 quickstart 是 G1/MuJoCo，完整训练发布仍列 roadmap。代码 Apache-2.0、权重 NVIDIA Open Model。README 15,000 FPS 不是本机/Unity整条管线性能。 | 学习过渡思路；不为一个挥锤动作引入机器人栈 |

GVHMR 许可原文来源：[LICENSE](https://github.com/zju3dv/GVHMR/blob/main/LICENSE)。MDM 的 MIT 仅代码，不自动授权所有依赖/训练集。各项目官方没有给出的最低显存/本机速度，不在此补猜数字。

## 推荐最小试验，而非继续扩建管线

第一轮只比较同一个动作：**2–4 秒“扛肩 → 双手挥击 → 收回”**。使用统一相机与相同身体/道具比例，比较手绘关键帧、Kimodo 约束生成、GEM-X 清楚视频动捕三条路线。记录：准备时间、下载/运行成本、返修时间、脚滑/握点错误、循环或回待机衔接质量。只有“修正后的可用一分钟动画成本”下降，才扩展素材库。

无需为了验证先生成日鹤完整精细 3D 模型：简单人偶与长柄锤就能发现重心、弧线、接触与过渡是否成立。潮的头发能力属于附加骨链/曲线动画，以上人体模型不能直接解决。
