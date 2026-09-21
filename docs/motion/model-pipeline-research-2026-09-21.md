# 图片转模型与绑定流程调研（2026-09-21）

## 结论与本次范围

**image-to-3dlab 有帮助，但适合做“统一模型→固定侧视动画渲染”的辅助工具，不是把现有立绘直接变成可战斗人物的一键方案。** 我们现在主要缺的是连续运动设计、稳定的握持/脚底接触、可变形的人体；单张图转网格只解决其中的形体起点。

本次直接读取作者 GitHub README、实现边界文档、上游项目与 Hugging Face 模型卡；未安装、未下载权重、未上传现有素材、未运行生成。作者公布的速度和内存数字均非本机测量。集成仓读取时最新 commit 为 `2da62010437708db526ba1b18a08228c2b23881b`。

本机只记录与选型相关的硬件：Apple M5 Pro，24 GB 统一内存。**不是 32 GB 机器**。统一内存不能按 NVIDIA 显存规格直接换算。

## image-to-3dlab 的实际能力

来源：[README](https://github.com/Bingeljell/image-to-3dlab/blob/main/README.md)、[工作室边界及当前状态](https://github.com/Bingeljell/image-to-3dlab/blob/main/docs/browser-workshop.md)、[后端来源](https://github.com/Bingeljell/image-to-3dlab/blob/main/docs/info_and_credits.md)。

- 面向 Apple Silicon，整合 Pixal3D C++/GGML/Metal、Hunyuan3D-MLX、TRELLIS.2 Metal 移植、Stable Fast 3D。它本身不是新的生成模型。
- 输入透明背景图，输出贴图 GLB 和 provenance sidecar，记录参数、哈希、组件许可。我们已有 BiRefNet-lite 抠图可作为前处理。
- 网格整理包括体素重建、减面、可选重绘贴图、压缩。文档给出原始网格常见约 90 万面；这个数量是作者观察，非我们资产测量。
- **体素重建＋减面不等于适合肩肘膝弯曲的动画拓扑。** 减少三角形数不会自动产生合理关节环线，也不会把粘连的头发、手指、裙摆、锤柄分开。
- 绑定审查/修改会交给 Blender worker 重新绑定和转移权重。当前第一个内置适配器是 **Blender 5.2 Rigify Basic Quadruped 四足骨架**；不能从网页“Rig / Animate”入口推断已经支持我们的人形角色。
- 文档明确按模型定制的生物骨架与动作并未随仓库发布。通用工作室路线图与已验证功能要分开。

## 后端选择与本机可行性

| 候选 | 直接核实的要求/限制 | 对本项目判断 |
|---|---|---|
| [Pixal3D C++/Metal](https://github.com/raven38/pixal3d.cpp) | 上游记录 M5/24GB、512 分辨率约 9分21秒、峰值 RSS 5.6GB；集成仓 Q8 单视图权重约8.1GB，要求 Xcode Metal 编译器 | 最值得做一次低分辨率本地小样；相似硬件案例增加可行性，但不是本机承诺，不能用权重大小代替总内存需求 |
| Hunyuan3D-MLX | 集成仓推荐32GB；默认2.0形体＋贴图约13GB下载；作者约9分钟 | 可作备用形体路线，不同时安装全部后端；先确认24GB实际峰值与许可用途 |
| [官方 TRELLIS.2](https://github.com/microsoft/TRELLIS.2) | 官方测试 Linux、NVIDIA≥24GB、CUDA；Mac是社区移植；集成仓权重约14GB，32GB作者简单物体约14分钟、复杂约78分钟 | 不作为低成本第一步，尤其我们当前连接和成本已经是痛点 |
| [Stable Fast 3D](https://github.com/Stability-AI/stable-fast-3d) | 官方 MPS 实验支持；测试机 M1 Max/64GB，明确建议低于32GB使用CPU；默认约6GB是GPU路径指标 | 不因名字“Fast”就当作本机最快。24GB Mac不优先 |
| [Hunyuan3D-2mini](https://huggingface.co/tencent/Hunyuan3D-2mini) | 官方0.6B形体模型；官方仓有低显存模式，不能据此推定集成仓MLX已支持mini | 小模型候选；暂不额外拓展实现 |

Pixal3D 集成仓报告默认引导下细剑曾缺失，提高参数才改善。因此**日鹤的长柄锤不应和人物一起依靠生成**。锤子用单独简单几何建模，锁定长短和锤头比例，成本更可控。侧视展示也仍要检查手臂、躯干、武器交叠处，不能仅看漂亮正面截图。

## 自动绑定候选

| 候选 | 已核实功能与要求 | 使用边界 |
|---|---|---|
| [UniRig](https://github.com/VAST-AI-Research/UniRig) / [HF](https://huggingface.co/VAST-AI/UniRig) | 自动骨架和蒙皮；GitHub要求 CUDA≥8GB；MIT代码，HF模型卡标MIT。官方提醒骨架不准会显著降低蒙皮效果 | 不能原生当作本机Apple Silicon流程。GitHub与HF卡发布状态文字存在不一致，试用前要固定实际检查点，不能把论文全部效果当现成模型能力 |
| [SkinTokens](https://github.com/VAST-AI-Research/SkinTokens) / [HF](https://huggingface.co/VAST-AI/SkinTokens) | UniRig后续项目，联合预测骨架与蒙皮；CUDA≥12.1、NVIDIA≥14GB；代码/LICENSE与HF卡均MIT | 可在其[官方Space](https://huggingface.co/spaces/VAST-AI/SkinTokens)做一次绑定对比，服务是否空闲、速度和额度未测；无需先在Mac上移植CUDA依赖 |
| Blender Rigify / 手动权重修正 | image-to-3dlab 已将重绑定、权重转移、动画烘焙留给 Blender | 对单个人形小样，标准人形骨架＋少量人工修权重往往比折腾另一个研究环境更可控；这是制作判断，尚无本项目工时对照 |

自动绑定输出不是动作：还要重定向/制作动画，修脚滑、手部接触、穿模和根运动。潮的攻击长发不应期望通用人形自动绑定完成：应作为独立曲线/骨链或2D发束特效。裙摆与衣袖也应先分离，再决定少量骨骼还是手修关键帧，不在首样引入布料模拟。

## 许可核实（与资源选择直接相关）

- **集成仓顶层未发现 LICENSE**：根目录文件列表无许可证文件，`/license` GitHub API 返回404。README列出的后端许可不能自动覆盖它自写的整合代码。可以继续评估其工作流；若复制其代码进本项目或分发工具，需要先明确该部分授权。
- Pixal3D 集成说明为代码/flow权重MIT，但内含DINOv3编码器，后者是[独立DINOv3许可](https://github.com/facebookresearch/dinov3/blob/main/LICENSE.md)。不要简称整个依赖链“全MIT”。此次未独立核对每个量化权重文件的声明。
- TRELLIS.2官方声明模型与代码MIT，依赖另有条款；社区移植与编码器同样需记录。
- [Hunyuan3D-2许可](https://github.com/Tencent-Hunyuan/Hunyuan3D-2/blob/main/LICENSE)明确排除欧盟、英国、韩国，并在5(c)写明Works、Output、results不得在区域外使用/分发/展示，不能简单以“只分发生成模型”忽略。它也写明腾讯不主张输出所有权；两点并存。2.1等版本要各自核查，不能外推。
- [Stable Fast 3D当前许可](https://github.com/Stability-AI/stable-fast-3d/blob/main/LICENSE.md)为Stability Community，商业注册、年营收门槛和署名等条件；不是无条件MIT。
- UniRig / SkinTokens 的代码和HF模型卡是MIT，但不会替我们取得输入角色的商业授权。当前项目仍依约限原型生成资产。

## 最适合我们的小样：3D只作动画支架，游戏仍横版2D

建议对同一段日鹤动作比较，不恢复完整3D游戏：

1. 先用现在线稿定下扛肩、蓄力、挥击、落点、收回的重心和节奏；这一部分与生成模型独立。
2. 单个人体用统一中性姿势建一次模型。手与躯干留间隙，长发和锤子单独做；不将已扛肩的复杂遮挡姿势直接当绑定模型。
3. 标准人形骨架，锤子固定手部握点，地面脚底有约束；只做这一段约一至两秒的动作。
4. 固定正交侧视相机、同一光照和材质、同一画幅/脚底锚点，渲染透明PNG序列，必要时在线稿上描修。首轮8–12个有意义姿势即可测试，避免先追求满帧精绘。
5. 循环检查轮廓、锤长、握点、脚滑、发束和裙摆穿插；再检查待机→攻击→待机接续。

收益来自同一网格/骨架在所有帧里保持比例和运动轨迹，不是“把每帧再交给AI重画”。逐帧无约束重绘仍可能重新引入漂移。预渲染2D降低实时模型预算，却不会消灭最初绑定、遮挡和关键姿势修正成本。

### 通过/停止条件

下一次真正试验时记录：安装/下载时间、首次生成时间、峰值内存、失败次数、人工修模和绑定时间、最终侧视可用帧数。先只装一个后端或复用现有基础模型。

- 通过：比例、握点、脚底稳定，能够连回待机，修改锤长只需改一个资产；人工清理明显少于当前逐帧修图。
- 停止扩展：24GB频繁换页/崩溃；一张人物反复生成仍粘连；修拓扑/权重花费大于做简单2D骨骼。回到部件动画＋少量手绘特殊姿势，不继续堆安装环境。

本报告只完成调研；这些通过条件尚未实测。
