# 日鹤：单手肩扛、接柄与人物精修

2026-09-21。根据用户“继续精修，有阻塞可用 Chrome 搜文章”的要求，延续独立美术/动作实验。未恢复 Unity 玩法开发，未更换游戏引擎，未导入新游戏资源。

## 交付

- [前后对照、逐帧、慢放与新版侧视](comparison.html)
- [新版 GIF](hammer_shoulder_polish.gif) · [三分之四 MP4](revised.mp4) · [横版侧视 MP4](side.mp4)
- [动作关键帧](keyposes.jpg) · [头部三视角前后对照](face_comparison.jpg)
- [可编辑 Blender 工程](st_hizuru_shoulder_polish.blend)

新版 72 帧、20 fps、3.6 秒；上一版为 60 帧、20 fps、3 秒。新增时间用于伸手接柄与松手。对照页各自按原速播放，旧版先结束并停在末帧；停帧按相同经过时间定位，**不是动作阶段严格对齐的性能/质量消融实验**。三分之四相机、正交比例、输出分辨率与 EEVEE 流程一致。另有新版正侧面动画。

## 本轮读了什么，用了什么

1. [aVersionOfReality — Custom Normals Workflow](https://www.aversionofreality.com/blog/2022/4/21/custom-normals-workflow)：作者区分轮廓形状和受光法线。已读全文。本轮先细化下颌/脸颊、平滑表面并给脸独立的柔和两档肤色，没有安装作者复杂动态法线系统，也没有声称复现代理法线工作流。
2. [AnimeCGLab — 四种二次元脸拓扑思路](https://animecglab.com/en/4-categories-of-face-topology-in-anime-3d-model/)：已读全文。采取简洁轮廓与独立五官的试作方式，并实际渲染正面、三分之四、侧面检查；不是仅增加细分面数就称完成脸部设计。
3. [BlenderArtists — 眼睛修改反馈 #18](https://blenderartists.org/t/critique-a-beginners-second-stylized-character/1576380/18)：读了讨论并查看作者前后图。调整虹膜纵向比例、上/下眼睑、小高光和镜框遮挡；随后按本轮近景进一步减小高光、拉开上镜框与上眼睑。
4. [Blender 官方 — Child Of](https://docs.blender.org/manual/en/latest/animation/constraints/relationship/child_of.html)：已读空间切换原则。当前实现采用 `Copy Transforms` 在世界自由手目标与武器握点之间切换，**不是宣称主工程用了 Child Of**；先匹配位置/朝向，再以阶梯关键帧切换影响。
5. [Blender 官方 — Armature](https://docs.blender.org/manual/en/latest/modeling/modifiers/deform/armature.html) 与 [Corrective Smooth](https://docs.blender.org/manual/en/latest/modeling/modifiers/deform/corrective_smooth.html)：连续袖/裤网格采用混合权重与 Preserve Volume，修正平滑限定在真实创建的局部关节顶点组；减少整体缩水。

完整正文要点、引用边界和本机 API 小探针见：[脸与长发](../face-polish-research-2026-09-21.md)、[握柄与形变](../grip-deformation-research-2026-09-21.md)。上一轮力量与惯性教程继续应用，未重复声称本轮看完其视频。

## Chrome 检索记录

本轮确实通过 Computer Use 使用用户 Chrome，新建研究标签，搜索 `Blender anime character face normals shoulder deformation breakdown`。读取到真实搜索结果，其中有上述 AnimeCGLab、aVersionOfReality 和一个 Polycount 专题链接。Google AI Overview 没有作为教程事实来源；正文由各原站进一步核验。

随后 Chrome 工具报告 Mac 已锁定且自动解锁失败，停止 UI 操作，没有处理密码或尝试绕过锁屏。已有资料足够，因此继续完成离线实验。Polycount 正文仍未取得，不编造已读结论。前轮本地 file URL 被浏览器工具安全策略拒绝，本轮没有通过 Chrome 绕过该拒绝去做同一本地预览的 UI 验证。

**对照页按钮尚未完成本轮实际 UI 测试**。JavaScript 仅做语法检查；可确认的是 Blender 工程、视频文件结构、关键帧与近景输出，不能以此代替原速播放的主观美术验收。

## 可见变化

- **单手扛锤**：近侧手持续承重，另一只手自然放下，胸前不再永远挤着两只手。进入挥击前空手抬起张开，接住锤柄；回肩后再放开。
- **接触连续**：空手轨迹先到达握点，13 帧开始跟随；61 帧解除跟随后从当前世界变换继续运动。近手在接柄前滑向靠近锤头的位置，远手抓住之后再沿柄调整间距；不让两手互相穿过来交换前后。
- **手部**：由长方块握手改为掌形和弯曲手指，远手具有 `Open_fingers` 开合形态键。它不是解剖级手部模型或完整手指骨骼。
- **脸部**：收拾下颌轮廓、虹膜/眼睑关系、微小高光、较细镜框、贴面的嘴线；脸使用独立两档肤色，减轻上一版下颌深阴影的胡须感。
- **头发**：在发束下新增连续后发体，发束截面由四点菱形改成八点扁截面，扩大主发束覆盖，去掉交替亮条和突兀亮色刘海。后发体和发梢共用相近的延迟节奏。路径截面仍是本实验简化构造，并未实现教程建议的通用曲线平行运输框架。
- **衣服**：袖/裤使用连续网格、保体积骨骼变形和局部平滑；肩侧补形采用混合权重并缩小鼓包。保留服装搭接，尚未完成全身统一拓扑。
- **承重轮廓**：独立审片发现原前后脚在主机位投影中交叉，最终调整前后站位，使命中时前腿弯曲、后腿支撑清楚可见。调整后重算全部脚踝与手腕约束。

仍保留中等锤头、长柄、明确靶面、快砸和较慢回收。未加震屏、打击闪光、音效或残影遮掩主动作。

## 最终验证

[verification.json](verification.json) 记录 72 帧约束与锤头最低点；[editability_check.json](editability_check.json) 在重新打开 `.blend` 后完成检查：

- 13、61 帧分别在同帧开/关手的跟随约束，位置误差与朝向误差均为 0。
- 13–60 帧远手目标相对武器 socket 的误差为 0。抓住过程中的滑动由 socket 的局部关键帧主动安排。
- 腕目标误差小于 0.001 个场景米；脚踝目标误差小于 0.001。它们不代表手指接触、脚底完整碰撞或商业动作质量。
- 33 帧锤头包围盒底部与安排的靶面高度差为 0。没有建立物理碰撞求解，不能据此声称所有网格都无穿插。
- 首尾骨骼头坐标一致；将近手控制器横移 0.015，腕部跟随约 0.015。试探编辑没有写回交付工程。
- 近手指开合键确认为 0，避免新建形态键默认值导致持柄手张开的试渲问题。
- 实际渲染近景三视角与动作关键帧，另有独立目视审查反馈；最终修正了反馈中的腿部交叉、肩鼓包及镜框/高光问题。

最终数值、视频尺寸帧率及 SHA-256 见相邻 JSON 文件。没有 Unity C# 改动，因此未运行 Unity 编译或构建。

## 尚未达到的部分

本版仍是简化的日鹤同人动画研究，脸与原作神态的接近程度有限；眼睛、鼻子、嘴仍为几何部件，缺乏表情变化。肩袖仍有搭接结构，长发/手/武器之间没有动态碰撞。换握靠人为设定关键帧，脚跟发力、肩胛运动和回收节奏还需要更多表演推敲。最终二维轮廓清稿、替换画、游戏接招窗口及真实玩家手感都未验收。

技术连续性通过，不等于商业品质通过。本轮没有把“下载某个新模型”作为继续制作前提，也没有购买/安装插件、重试 AI 生成或把角色归为本项目原创。

## 编辑与重现

文件保存在第 1 帧。骨架名 `ST_Hizuru_polished_rig`，动作名 `ST_single_hand_regrip_strike_release`。

- `CTRL_Hammer_rigid`：武器主控，承担主动作轨迹。
- `CTRL_Left_grip`：近手握点，武器子级，有沿柄滑动关键帧。
- `CTRL_Right_grip`：远手目标，自由运动段为世界关键帧，握持段由 `Grasp_weapon_after_contact` 约束跟随。
- `SOCKET_far_grip`：武器上的远手握点；其局部移动是有意滑握。
- `Open_fingers`：远手四根手指分别具有开合形态键。
- `Tip_lag`：后发体及发束的手工跟随。没有碰撞模拟。

修改握点/武器在13、61帧的位置后，要同步自由手的接触/释放矩阵；当前匹配是按本动作安排的，不是任何改动都能自动修复的通用切换插件。修改后重新运行检查。

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python docs/motion/shoulder-polish-2026-09-21/build_study.py -- all
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python docs/motion/shoulder-polish-2026-09-21/render_views.py -- side
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python docs/motion/shoulder-polish-2026-09-21/render_views.py -- baseline
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python docs/motion/shoulder-polish-2026-09-21/render_portraits.py
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python docs/motion/shoulder-polish-2026-09-21/verify_scene.py
/Users/jacklee/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3 docs/motion/shoulder-polish-2026-09-21/make_preview.py
```

本机 Blender 5.2.0 LTS；源骨架沿用上一轮已保留的 MoMask BVH，仅作比例支架，模型原动作已清除。所有本轮人物几何和动作由 Blender Python API 制作，不称作手绘/真人动捕/AI自动成片。重跑脚本只覆盖本实验输出，旧版独立保留。素材来源与方法边界承接上一轮 README。
