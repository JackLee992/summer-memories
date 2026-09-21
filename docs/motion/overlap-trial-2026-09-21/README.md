# 第三轮：GitHub / Hugging Face 筛选与跟随动画

2026-09-21。继续研究动画生产流程；没有恢复 Unity 玩法开发。本轮并行核查图像动画、可编辑动作两路官方资料，并继续本地二维小样。没有下载模型权重或执行下列项目的推理。

## 更值得试的开源候选

| 候选 | 当前可用性与优势 | 对我们仍缺什么 |
|---|---|---|
| [MoMask](https://github.com/EricGuo5513/momask-codes) / [官方所链 HF 演示](https://huggingface.co/spaces/MeYourHint/MoMask) | 文本生可编辑 BVH；查询时 Space 为 RUNNING / cpu-basic，源代码确认 BVH 下载 | 没有握锤刚体约束；服务状态不保证生成成功。适合下一次最低准备成本的动作底稿试验 |
| [ToonCrafter](https://github.com/ToonCrafter/ToonCrafter) / [HF](https://huggingface.co/Doubiiu/ToonCrafter) | 两张相邻动漫关键帧生成短段，任务比真人舞蹈迁移更贴近二维补间 | 无硬骨长/握点保证；官方草图控制仍待办。原版约24–27GB GPU是作者报告，不是本机实测 |
| [Wan2.2-Animate](https://github.com/Wan-Video/Wan2.2) / [HF](https://huggingface.co/Wan-AI/Wan2.2-Animate-14B) | 用角色参考图与动作视频生成连续外观，代码权重公开 | 需要合格驱动视频与CUDA资源；无锤柄和首尾像素硬保持，不能套用Wan 5B显存需求 |

没有核实到一个在本机 Mac 即用、同时自动保证动漫角色外观、武器握持与可编辑骨架的全面替代方案。MoMask 优势是试验准备便宜，不是已证明质量强于 Kimodo。ToonCrafter 是关键帧补间候选，不是错误原画修复器。

详细证据、发布状态、依赖与许可见 [角色动画候选](../character-animation-candidates-2026-09-21.md)、[可编辑动作候选](../editable-motion-candidates-2026-09-21.md)。MimicMotion 权重生产限制、UniAnimate-DiT 代码许可待明确、AnyTop 数据许可边界均在报告中列明。

## 另外核查的低成本路线

- [Meta Animated Drawings](https://github.com/facebookresearch/AnimatedDrawings)：同一张图进行 ARAP 变形，接受 BVH 和自定义重定向，支持透明 GIF。README 记录 macOS 测试，提供手工标注绕过检测的路径；代码与模型为 MIT。但官方仓库已于2025-09-03归档，旧Python/TorchServe依赖需额外处理，儿童画演示不能证明能处理双手武器。值得借鉴“一张图＋骨架”的思路，不据此新增整套环境。
- [Inochi Creator](https://github.com/Inochi2D/inochi-creator)：分层图的网格变形与参数控制，release API 的 latest 为 v0.8.6，附 osx.zip / dmg；没有实测当前 Apple Silicon 兼容性。[Unity binding](https://github.com/Inochi2D/com.inochi2d.inochi2d-unity) README 明确 beta、加载0.8模型。适合分层人物表现研究，不是自动挥锤动作生成器，也未核实Unity 6整合。
- [JigglePhysics](https://github.com/naelstrof/JigglePhysics)：官方说明有 Verlet 跟随、骨长/角度控制、空气阻力和碰撞，适合头发/衣摆二级动作。性能数字是作者演示。本轮仅阅读其设计思路，**没有引入代码/依赖**；未完成其集成与许可核查，不作为已批准的游戏依赖。

这些来源均于本轮直接读取官方 README；归档和发布状态通过 GitHub API核查。没有把 Hugging Face 搜索中仅名字含“2D animation”的图像 LoRA 当作可用角色动画模型。

## 实际完成的小样

[同步交互对照](comparison.html) · [新版 GIF](overlap_hair.gif) · [姿势总览](contact_sheet.jpg) · [验证记录](verification.json) · [重现脚本](make_trial.py)

全部是 Pillow 程序结构稿，非 Computer Use 手绘、非上述模型输出。保留上一轮2.6秒时序、相机、锤子轨迹和脚底，做三个版本：

1. A：上一轮完整画面。
2. B：腰胸分段旋转，头部减少随躯干倾斜；头发刚性。
3. C：与B相同身体姿势，头发改四段固定长度链，使用带阻尼的角度跟随与逐段延迟。

B/C 可以观察头发跟随的增量；A/B还改变了头部/发束画法，因此不是严格的躯干单变量消融。没有为画面更动感增加震屏、拖影或闪光。

首次逐帧检查发现发束进入躯干，增加保守的侧视端点避让：保持段长，偏转端点到身体后缘以外。65个采样中61帧触发修正，这说明该几何构型对避让依赖很高，**不是只靠阻尼就自然解决穿模**。该办法不检查连续网格、武器和每一条线段，尚不能称完整碰撞。

## 验证结果与限制

- 三版各65个25fps采样，总时长2600ms；GIF可能合并重复静帧。
- 骨长、握点、锤柄和发链长度误差均小于1e-12模型单位。
- 弹簧先运行8周期进入周期状态；首尾采样发尾距离约0.037模型单位，不再宣称首尾每个像素都相同。
- 发链最大偏角47度。几何约束通过不等于重量感或解剖表演通过。
- Chrome Computer Use实际打开对照页，确认三栏加载、暂停、滑条Home/Right逐帧到0.04秒。页面新增禁止自动翻译标记，避免中文被浏览器扩展自动翻译影响评审。

新版头发可以相对身体滞后和回落，头部倾斜更少；但身体仍偏剪纸、肩胯缺少三维扭转、没有接触对象、衣摆或脚步。头发避让也会改变形状，不应把这版当成角色美术完成。

## 建议下一次只做哪一个

优先一次 **MoMask HF → 原始BVH预览 → 简单人偶侧视** 的动作底稿探针，最多两个结果，先看动作是否真像挥重物；若语义和重心不对，不进入精细人物绑定。ToonCrafter 放到三张关键姿势精修后，只补相邻短段。Wan Animate 等有可用驱动视频与GPU条件时再做，避免同时铺三套环境。

研究报告中的模型试验仍未执行。本轮实际优化只涉及本目录程序小样。运行脚本需现有 Pillow，并读取上一轮 `constraint-trial-2026-09-21/make_trial.py` 作为基线，不修改它。
