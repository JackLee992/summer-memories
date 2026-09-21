# 角色一致动画与动漫关键帧候选核查

核查日期：2026-09-21。仅在线读取官方 GitHub、HF 模型卡、许可证与推理说明；没有下载权重、安装环境、运行推理或恢复 Unity 开发。下列速度/显存均为作者报告，不是本机测量。

## 结论

**针对现有二维原画，最值得一次小试的是 ToonCrafter 的“两张相邻关键帧→短段中间帧”**，原因是任务直接匹配动漫关键帧补间，而不是把真人舞蹈迁移到角色。它并不新，也没有证据证明能解决挥锤几何；先用一次试验否证，不能承诺替代部件骨骼。

**若目标改为“已有完整动作视频→同一角色连续表演”，首选 Wan2.2-Animate**。它有更新的公开视频生成基础、角色图/姿态/脸部视频条件和完整公开流程，但仍不保证锤长、双手握点、脚锁定与准确回待机。两条路线都没有本项目实测，不可写成效果已改善。

**没有找到经过核实、可在当前 Mac 直接运行、同时硬保持角色结构/武器握持/首尾原画的全面更优替代。** 官方样片、参数量与 GitHub 热度都不能证明该条件成立。保持现有可控动画作为对照，不继续无差别安装旧模型。

## 四项筛选

| 候选 | 可用发布与源码快照 | 控制能力及关键缺口 | 硬件证据 | 本项目判断 |
|---|---|---|---|---|
| **ToonCrafter** | 2024-05-29 发布代码与权重；当前默认分支 `b0c47ff339c5e5ec45b84d0c6587850f242d41ef`，2025-03-19 | 两张起止图＋文字，最多 16 帧、512×320。原画是生成条件，不保证解码结果逐像素相同；HF 明说有损自编码导致闪烁。官方 README 草图控制仍在待办，展示不等于主仓可直接用；另有社区实现 | 官方 A100、50 步、约 24GB/24 秒；README 补充可能 24–27GB。社区封装约 10–12GB，不能当官方原版需求 | **相邻关键帧小试首选**。保留原始端点作为输出首尾，逐帧检查与相邻生成帧是否跳变；不要一次跨越蓄力→击中→回收 |
| **Wan2.2-Animate-14B** | 2025-09-19 发布；2025-11-13 宣布 Diffusers；代码 `42bf4cfaa384bc21833865abc2f9e6c0e67233dc`，2026-03-17；HF 权重 revision `cb93a225fbaf1ca100f54e79da8f994995b689b3` | 参考角色图＋驱动视频→姿态/脸部条件；animation 和 replacement 两种模式。无所查官方接口中“指定末帧原画”的硬约束；上一片段条件帧帮助长视频衔接，不等于首尾循环一致 | 官方 Animate 节有单 GPU 命令，但本次所查文字没有独立 Animate 最低显存数字。**不能套用 TI2V-5B 的 24GB 或 I2V-A14B 的 80GB 数字**。14B BF16 仅参数约 28GB 是理论估计，尚不含其他模型/激活；低显存需卸载/量化单独验证 | **动作视频迁移首选备选**；适合一次短视频探索软发、衣服与整体表演，不能保证武器拓扑 |
| **UniAnimate-DiT** | 2025-04-15 发布代码；`61d882c25385042f0cf5bcdaf6853238d9756d68`，2025-04-27；HF 发布 LoRA＋额外模块，需另配 Wan2.1-I2V-14B-720P | DWPose 体型对齐、角色参考图、连续姿态，长视频脚本。未发现官方任意首尾原画硬约束；姿态骨架没有武器几何 | 官方 480P/81 帧默认 23GB，降低常驻参数至 0 报告 14GB；720P 为 36GB/卸载 26GB。A800＋TeaCache 报告 5秒片段约 3分钟(480P)/13分钟(720P)，并提示缓存可能影响一致性 | 仅作为 **16/24GB CUDA 设备上的姿态迁移备选**；没有本项目证据说明优于 Wan Animate，暂不并行折腾 |
| **MimicMotion 1.1** | 2024-07-08 发布 1.1 权重；代码 `6907bdcc259a6a048d41a365e840d22274f9256c`，2025-11-18 | 置信度姿态引导、局部损失改善手部失真、长段融合；不提供握持刚性约束；参考图并非任意最后一帧约束 | 官方 72帧模型 16GB，35秒示例在 4090 约20分钟；旧16帧 UNet 可8GB，但 VAE 要16GB或CPU解码。不能混称整套只需8GB | **淘汰为生产方案**：权重许可限学术/研究/教育，明确禁商业或生产；仍可作为论文对照，不值得为此次目标装环境 |

Wan、UniAnimate-DiT、MimicMotion 的 GitHub releases API 本次均返回空列表；表中区分 README 宣布日期、权重版本与默认分支提交，不虚构正式 release tag。

## 为什么“姿态驱动”仍可能挥锤失败

- 人体关键点约束的是身体关节，通常不包含锤头、锤柄、接触点。手腕位置大致对，锤柄仍可能弯折、长度变化，双手仍可能脱离握点。手部关键点也不是物体抓握约束。
- 真人姿态估计器对动漫比例、侧身遮挡、发丝挡脸、两手重叠没有可靠性保证。Wan 官方预处理文档明确提醒体型不匹配可能变形，简化重定向不保证正确。
- Wan 基础重定向要求参考人物与驱动首帧均正面舒展；本项目侧面扛锤图不直接满足。增强路线借助 FLUX.1-Kontext-dev 改图，也明确不保证身份/姿态一致。因此要先检查姿态条件视频，不能直接拿现有线稿盲跑。
- ToonCrafter 会根据端点猜中间运动；从扛肩直接跳到锤落地的跨度，存在多种合理路径，不能把“符合两端”当成“动作意图正确”。应先补齐蓄力、击中、回收关键姿势，再只补邻接小段。
- 模型产生 RGB 视频，不自动给出合格透明精灵、碰撞时序或可复用角色骨骼。抠像、清边、稳定根节点和重排时序仍有成本。

## Mac 与许可边界

这些官方流程没有本次可核实的 Apple Metal/MPS 完整支持证明。Wan `wan/animate.py` 明确构造 `cuda:{device_id}`，其余安装/示例也按 NVIDIA CUDA 路线。24GB 统一内存不是24GB独立显存；“支持CPU卸载”更不表示可在Mac无修改推理。Mac只作为素材准备、关键帧整理和评审端；不要为验证几秒画面先移植 CUDA 环境。

| 候选 | 代码/权重许可核查 |
|---|---|
| ToonCrafter | GitHub LICENSE 为 Apache-2.0；HF 模型卡 Uses 明确 Apache-2.0。社区草图/ComfyUI 封装要另查各自许可，不能自动继承 |
| Wan Animate | GitHub LICENSE.txt 和 HF 模型卡为 Apache-2.0。可选 FLUX.1-Kontext-dev 的 HF 元数据标明 `flux-1-dev-non-commercial-license`，不能把整个含 FLUX 的工作流都称为 Apache-2.0；可不用该分支，或单独确认用途条件 |
| UniAnimate-DiT | HF 模型卡标 MIT；本次官方仓库根目录**没有独立 LICENSE 文件**，README又称学术研究用途。HF权重标签不自动解决代码与依赖许可；不据此承诺生产使用已清晰 |
| MimicMotion | GitHub代码 Apache-2.0（列有第三方例外）；HF权重 LICENSE 明确 “only for academic, research and education” 和 “refrain ... commercial or production ... under any circumstances”，还附 SVD条款。代码开放不等于权重可生产使用 |

上述只论模型/代码许可，不能授予原作人物与图像权利；沿用项目素材来源与原型记录要求。

## 一次小试的具体设计（尚未执行）

1. 选现有挥锤中**两张结构已修正、画风一致、跨度小**的侧面关键帧；构图、角色高度、落脚点固定。锤子独立保留一份刚性参考，不使用有明显骨长错误的输入去要求模型“补正确”。
2. 仅 ToonCrafter 一个 16帧短段、固定种子；保留输入原图与未经修饰的输出。若需对比，再允许第二种子；本轮不扩成批量挑片。
3. 输出端点用原图精确保留，但同时展示原始模型端点，检查插入原图后第1→2帧与倒数2→末帧是否跳变；**替换端点本身不算模型完成硬保持**。
4. 检查轮廓、握点、锤长、脚底、发型、击打轨迹，以及较现有骨骼小样增加的人工修补时间。出现明显锤柄弯曲/手部换位/路径反转即失败；不要用RIFE提高帧数掩盖。
5. 若输入已有合格驱动视频而非两张原画，改试 Wan Animate 一段2–3秒；先看pose/face预处理是否可用，再决定推理。GPU实际峰值、初始化下载、推理和修补耗时分别记录。没有可用CUDA环境时，停在可复查调研，不假装本机能跑。

## 官方证据链接

- ToonCrafter：[GitHub](https://github.com/ToonCrafter/ToonCrafter)、[HF模型卡](https://huggingface.co/Doubiiu/ToonCrafter)、[LICENSE](https://github.com/ToonCrafter/ToonCrafter/blob/main/LICENSE)。README 的草图控制待办与社区入口必须区分。
- Wan：[GitHub Animate说明](https://github.com/Wan-Video/Wan2.2#run-wan-animate)、[HF权重](https://huggingface.co/Wan-AI/Wan2.2-Animate-14B)、[预处理约束](https://github.com/Wan-Video/Wan2.2/blob/main/wan/modules/animate/preprocess/UserGuider.md)、[CUDA实现](https://github.com/Wan-Video/Wan2.2/blob/main/wan/animate.py)、[FLUX模型卡元数据](https://huggingface.co/api/models/black-forest-labs/FLUX.1-Kontext-dev)。HF旧README尚未勾选Diffusers，GitHub新README已宣布集成，采用日期较新的GitHub状态。
- UniAnimate-DiT：[GitHub显存与流程](https://github.com/ali-vilab/UniAnimate-DiT)、[HF模型卡](https://huggingface.co/ZheWang123/UniAnimate-DiT)。
- MimicMotion：[GitHub需求](https://github.com/Tencent/MimicMotion)、[HF权重](https://huggingface.co/tencent/MimicMotion)、[权重LICENSE](https://huggingface.co/tencent/MimicMotion/blob/main/LICENSE)、[NOTICE/SVD条款](https://huggingface.co/tencent/MimicMotion/blob/main/NOTICE)。
