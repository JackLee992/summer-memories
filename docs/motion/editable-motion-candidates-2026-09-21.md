# 可编辑动作候选：MoMask / CondMDI / AnyTop

日期：2026-09-21。范围仅为动画调研，未恢复游戏开发。本轮访问官方 GitHub 源码、README、许可与 Hugging Face Space API；**没有下载权重、安装依赖、运行推理或操作 UI**。以下质量与耗时均待实测，不把论文演示当作本机结果。

## 结论

**新增候选优先试 MoMask，目的明确为“便宜地取得可编辑人体动作底稿”。** 官方链接的 HF 演示支持 CPU 并提供 BVH 下载；本次 API 返回 `RUNNING`、`cpu-basic`，但没有实际提交生成请求，不能保证排队时间或动作成功率。它比 Kimodo 更容易开展零本地环境试验，不是约束能力或动作质量已经胜过 Kimodo。

CondMDI 更贴近“给稀疏关节/关键姿势补动作”的问题，但输出转换和旧 CUDA 环境令它暂不适合一段挥锤。AnyTop 的优势是不同骨架拓扑与动物动作，当前人形挥锤没有必要承担其预处理和数据许可成本。三者都不直接生成可编辑的 2D 轮廓/绘画关键帧；需骨架重定向、正交投影或描修。它们输出的密集逐帧曲线也不等同动画师整理过的少量控制器关键帧。

现有本地 2D IK 小样已经解决部分几何一致性，仍显僵硬；再换插值算法不会自动产生重心转移、肩胯先后、锤头滞后和卸力。新试验应该比较“人体动作底稿是否减少人工设计/返修”，而不是比较谁帧数更多。

## 三项新增候选与现有基线

| 路线 | 发布与输出证据 | 约束/接触能力 | 实际准备成本与判断 |
|---|---|---|---|
| **MoMask：本轮首选小试** | 官方已发布训练、生成、模型下载与 temporal inpainting。`gen_t2m.py` 输出 22 关节 XYZ、预览 MP4、BVH；官方提供 Blender / KeeMap 重定向流程。HF 演示有 BVH 下载。 | 可文本生动作和按时间段重生成；编辑源要求 HumanML3D 263 维特征，不能直接拿几张 PNG 当姿势输入。其 residual 阶段重生成全序列，不能把 mask 外关键帧理解成严格锁定。足部 IK 官方明确有时失败。**没有查到武器刚体、双手握点、命中物体约束接口。** | HF CPU 路线最省本机准备。CLI 明确 `--gpu_id -1` 走 CPU；不等于 Mac 原生依赖已验证。20 fps，指定帧数按 4 帧粒度处理。代码 MIT；SMPL/SMPL-X、PyTorch3D、HumanML3D 等依赖/数据各有许可；代码许可不能自动证明所有权重和产物的商业使用边界。 |
| **CondMDI：有条件的研究备选** | 官方代码与三类 checkpoint 链接已发布，包括随机帧与关节条件模型。输出 `results.npy` XYZ 与 MP4；SMPLify 可产生 SMPL 参数与逐帧 OBJ。**README 未提供一键 BVH/FBX。** | 稀疏关键帧/关节条件、imputation、reconstruction guidance 比纯文本更贴近补中间段。README 的自选关键帧交互仍标 `In development`。**人体位置条件不等于锤子碰撞、握持与动力学约束。** | 官方开发环境 Ubuntu 20.04、Python 3.7、CUDA 11.7、PyTorch 1.13.1；SMPLify 也需要 GPU。未找到本机 Mac 支持或可靠最低显存数值。MIT 代码；CLIP、SMPL、SMPL-X、PyTorch3D、数据另许可。要接 Blender/Unity，需 SMPL 骨架参数导入/重定向/烘焙，OBJ 序列不能当骨骼 AnimationClip。 |
| **AnyTop：当前不选，异形/动物再考虑** | 官方代码、预训练模型、补间与上半身编辑已发布；直接输出 XYZ、MP4、BVH；提供 `visualization/bvh2skeleton.py` 生成有骨架动画的 `.blend`。 | 适配未见骨架，支持固定前后段补中间、固定下身改上身。骨架处理含脚接触启发式；**不证明能维持武器双手闭链或环境碰撞**。主要收益是拓扑泛化，并非精确人形挥锤。 | 官方 Ubuntu 18.04.5、Python 3.8、CUDA GPU；最低显存未披露。新骨架需 BVH、朝向关节名、rest pose 与 `cond.npy`；可能需调整脚接触阈值。代码 MIT，但处理后的 Truebones 数据因许可澄清暂停发布；官方完整流程指向另取 Truebones 数据，预处理可能数小时。不能从 MIT 推断 Truebones 或权重商业授权。 |
| **Kimodo（已有基线，不重新推荐一遍）** | 现有报告核实 SOMA BVH、手脚/姿势/路径约束。 | 比 MoMask 更直接的动作控制；仍需握锤和接触后处理。 | NVIDIA/GPU、gated Llama 带来准备成本。若 GPU 条件已有，应保留为约束能力对照；详见 `motion-model-research-2026-09-21.md`。 |
| **本地 2D IK（已有基线）** | 已有程序、GIF 与几何验证，不需要模型。 | 骨长、握点可确定控制；动作风格、重量感仍靠作者。 | 最低运行成本，短板在表演设计。AI 骨架可供重心/节奏参考；不必为了换模型重做角色美术。 |

### MoMask 的硬件与可执行性补证

本次读取 `gen_t2m.py`，设备选择为 `cpu`（`gpu_id == -1`）或 CUDA，没有原生 MPS 分支。README 旧 conda 环境为 Python 3.7.13 / PyTorch 1.7.1，另列 Python 3.10 pip 路径；当前 requirements 固定 torch 1.12.0、numpy 1.21.5 等旧版本及 cu113 index。**CPU 支持有证据，当前 Apple Silicon 一键安装没有证据。** HF Space 当前使用 Python 3.10.12，API 状态只是访问时的瞬时状态。官方未给出这里可引用的最低 RAM、显存和 3 秒片段本机用时，因此不填猜测值。

无需下载完整 HumanML3D 就可从自定义文本生成，是它相较 CondMDI/AnyTop 低成本试验的实际优点。BVH 由关节位置经转换/IK得到，应检查足滑、旋转扭转和肘翻转；不是所有姿势都自动变成好用的角色动画。

## 动作库核查：LAFAN1 不作为游戏素材池

[Ubisoft LAFAN1](https://github.com/ubisoft/ubisoft-laforge-animation-dataset) 官方提供 77 条、30 fps、约 4.6 小时 BVH，含 fight、fight and sports 等类别；代码基线只需 Python/NumPy，能低成本拿真人运动作补间研究。没有核实其中存在双手长柄锤，不能凭 fight 标签承诺可用。

但 [license.txt](https://github.com/ubisoft/ubisoft-laforge-animation-dataset/blob/master/license.txt) 为 **CC BY-NC-ND 4.0**，允许非商业条件下制作和复制改编，但不允许分享改编材料；它不是允许任意修改再放进游戏发布的免费商用动作库。因此只列作研究资料，**本轮排除出可交付素材管线**。GitHub 可下载、格式是 BVH，都不能代替内容许可。

## 最小试验：MoMask 一段动作 + 现有 IK 对照

这是待执行方案，本轮没有运行。推荐总预算 **60–90 分钟人工工作、最多 2 个生成结果**；预算是管理上限，不是性能承诺。

1. **先选 HF 官方链接演示试 1 次**，避免在 Mac 安装旧依赖。输入通用动作文本：`A person grips a long-handled hammer with both hands, raises it over one shoulder, makes one forceful downward strike, follows through, and returns to a balanced ready stance.` 不要求模型认识日鹤，不生成新人物图。若演示长度不可控，则从结果中取完整一次击打；不反复改提示赌命中。
2. 下载生成 **BVH 和预览**，不下载权重。离线备选命令仅在以后环境已确认时使用：`python gen_t2m.py --gpu_id -1 --ext hammer_probe --motion_length 60 --repeat_times 1 --text_prompt "..."`。60 帧在 20 fps 下为 3 秒，且是 4 的倍数；本轮未执行该命令。
3. 在已有可用 Blender 中先看原始骨架，**不绑定精细角色**。固定正交侧视，按 5 个事件检查：蓄力、骨盆/躯干启动、手臂带锤加速、接触、卸力回收。原始结果连单次挥击语义都不对，最多换一个 seed，随后停止这一候选。
4. 添加一个简单刚性长柄锤和两个握持标记。主手驱动锤子；副手 IK 跟随锤柄第二标记，修正肘弯方向；脚在支撑区间锁位置。此步骤是建议的人工约束方案，不是 MoMask 功能。若姿势无法同时满足两手与骨长，需要改肩/躯干，不能靠拉长胳膊掩盖。烘焙前避免“锤受手驱动、同一手又受锤驱动”的循环依赖。
5. 保留原始与修正后的动作各一份。与现有 2D IK 使用相同时长、侧视与比例对照，先画棒人即可。只有重量感和衔接改善才考虑投影为 2D、关键姿势描修或最终 FBX。Unity 需映射 Humanoid/Generic、坐标/比例与 root motion 配置；BVH 不承诺直接拖入 Unity 即用。
6. 记录准备分钟、生成等待、修正分钟、握点最大误差、支撑脚漂移与回待机突跳。512 px 预览建议握点误差 ≤2 px、固定骨长变化 ≤1%；这只是试验阈值。请人对原始、修正、现有 IK 盲看“重心先动、锤头滞后、命中停顿、卸力”是否清楚。**若仍需逐帧重摆大多数姿势，停止模型迁移，转向参考动作/人工关键姿势，不能用几何误差小宣告表演合格。**

本轮没有发现一个已核验的“输入 2D 草图几帧 → 自动输出完整 2D 可编辑控制器动画并保证双手握锤”的即用开源方案。OpenToonz/Synfig 的已核查功能继续见 `2d-rig-options-2026-09-21.md`，本报告不把这些旧候选重新包装成新发现。

## 官方来源与查验记录

全部于 2026-09-21 读取文本；未点击推理或下载模型。

- [MoMask 官方 README](https://github.com/EricGuo5513/momask-codes)：CPU demo 公告、BVH、足 IK 限制、编辑输入格式、Blender 重定向与许可声明。
- [MoMask 生成源码](https://github.com/EricGuo5513/momask-codes/blob/main/gen_t2m.py)：CPU 分支、BVH 转换、100 次 IK 迭代和 20 fps；[依赖](https://github.com/EricGuo5513/momask-codes/blob/main/requirements.txt)。
- [MoMask HF Space](https://huggingface.co/spaces/MeYourHint/MoMask)、[Space API](https://huggingface.co/api/spaces/MeYourHint/MoMask)、[app.py](https://huggingface.co/spaces/MeYourHint/MoMask/blob/main/app.py)：访问时 sha `4b0323058afeb8d4443d5b370a9e35fd9537222b`，runtime `RUNNING/cpu-basic`；源码确认 BVH download。Space 是官方仓库链接的演示；运行状态不等于本次完成推理。
- [CondMDI 官方实现](https://github.com/setarehc/diffusion-motion-inbetweening)：环境、预训练模型链接、条件模式、输出、SMPLify、许可。
- [AnyTop 官方实现](https://github.com/Anytop2025/Anytop)：已发布阶段、BVH/Blender 脚本、未见骨架预处理、Truebones 许可澄清；[代码许可](https://github.com/Anytop2025/Anytop/blob/main/LICENSE)。
- [LAFAN1 官方 README](https://github.com/ubisoft/ubisoft-laforge-animation-dataset/blob/master/README.md)、[数据许可](https://github.com/ubisoft/ubisoft-laforge-animation-dataset/blob/master/license.txt)：数据格式/规模与 NC-ND 限制。
