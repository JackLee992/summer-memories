# 人物美术与重锤力量感：教程应用试作

2026-09-21。用户要求继续搜索人物美术、力量感教程，也搜索美术论坛和 X/Twitter 的制作分享，再优化一版。本目录是独立动画资产研究，**没有恢复 Unity 玩法开发，也没有改回 3D 游戏**。

## 看结果

- [可播放、半速、逐帧、切换横版侧视的对照页](comparison.html)
- [新版 GIF](hammer_art_weight.gif) · [新版 MP4](revised.mp4) · [横版侧视 MP4](side.mp4)
- [八张关键姿势](keyposes.jpg)
- [可编辑 Blender 工程](st_hizuru_weight_study.blend)

A 是上一版自己编排的骨架人偶动作，重新用相同相机位置、正交比例与 720×720 分辨率渲染；保留原有 Workbench 材质。B 同时改了人物、动作、材质与靶面，使用 EEVEE；因此是整体迭代对照，不能据此孤立判断某个因素的贡献。两版均 60 帧、20 fps、3 秒。侧视是对 B 的额外检查，A 的机位不会跟随切换。

## 实际学到并用在哪里

| 可阅读的教程/分享 | 已读到的方法 | 本轮具体应用 |
|---|---|---|
| [Blender Studio：Creating a Primitive Body](https://studio.blender.org/training/stylized-character-workflow/5d7f7cf055ccaf1a4a78102d/) | 已读取官方完整英文字幕；独立体块调比例，四肢有宽窄，鞋底和鞋身分开 | 在上次骨架比例上重新建立躯干、连贯的衣袖/裤腿、鞋与鞋底，未导入课程模型 |
| [aVersionOfReality：Reimu Workflow Test](https://www.aversionofreality.com/blog/2021/1/22/reimu-hakurei-workflow-test) | 已读作者全文；长发分束控制、材质分层、为镜头修形 | 刘海/侧发/后发分组，限制配色，检查肩扛和接触时手、脸、柄的重叠关系；没有复刻其复杂绑定系统 |
| [BlenderArtists：Stylized Hair - Geometry Nodes](https://blenderartists.org/t/stylized-hair-geometry-nodes/1406121) | 已读原帖并查看节点图；路径、半径曲线和截面形成发束 | 自建宽窄变化的几何发束并平滑；没有宣称搭建了原节点树，发梢延迟用形态键安排 |
| [Blender 官方：Shader to RGB](https://docs.blender.org/manual/en/4.5/render/shader_nodes/converter/shader_to_rgb.html) | 已读正文；光照转色阶用于卡通着色 | 实际使用 EEVEE，Diffuse → Shader to RGB → Constant ColorRamp → Emission，角色三档明暗；地面用连续明暗，减少背景干扰 |
| [AnimSchool：Blocking Plus / Timing](https://blog.animschool.edu/2025/08/08/blocking-plus-workflow-timing/) | 已读课堂文字摘要；关键姿态先行、蓄力/爆发节奏变化、吸收余势 | 身体在武器主挥动前启动，出击集中在 23–29 帧；接触后下沉、反弹、慢回收；不是视频逐帧临摹 |
| [Animator Island：Action-Reaction](https://www.animatorisland.com/physics-in-animation-action-reaction/) | 已读完整文字；打击者本身也受接触反力影响 | 明确第 29 帧锤头碰靶面，第 30 帧武器停留而身体继续下降；32 帧轻微反弹，35 帧落稳 |
| [BlenderArtists：动作姿势批改](https://blenderartists.org/t/rusty-animator-28-day-challenge-animation-practice/1638849/8) | 已读轮廓、负空间、引导线批改文字 | 独立审查肩扛、蓄力、接触、卸力四帧，随后检查正常速度；移动武器及握点整组，避免只改道具外观 |

来源细节、作者、观看/阅读范围与检索失败记录另见：[人物教程](../character-art-tutorials-2026-09-21.md)、[力量感教程](../weight-community-tutorials-2026-09-21.md)、[论坛与 X](../artist-social-links-2026-09-21.md)。

找到了作者自己链接的 [@AversionReality](https://x.com/AversionReality)；本轮未读到推文正文。Polycount 返回 403。没有将不能访问的帖子、未观看的视频或标题写成已学完的教程。

## 人物和动作改了什么

- 人物从无脸中性人偶改为长发、眼镜、白领、深西装的日鹤风格练习模型。几何脸、镜框、领片、发束和鞋均由本目录脚本制作，未抽取官方媒体。
- 发束有前后层次和宽窄变化，发尾通过形态键迟于头部收束。它是人为编排的次级动作，没有头发碰撞求解。
- 腿、袖改成跨关节的连续网格，修正了首次试渲的断口及沿轴扭转。仍不是生产级拓扑、权重或褶皱。
- 锤柄约 1.25 个场景米；锤头主体 0.29×0.17×0.18，带小端盖。这个尺度是练习约定，保留适中锤头、长柄的方向。
- 两手目标跟随同一武器控制器，握点相隔 0.24；握持位置和武器通道随阶段变化，手臂 IK 禁止拉伸。
- 前后脚形成支撑，髋部后撤/下降蓄力，再先于锤头移动。第 29 帧按锤头包围盒最低点设置靶面高度；武器接触时停止，身体继续吸收余势。
- 复位速度低于主下击，末尾稳定回肩。没有使用震屏、闪白、残影、粒子或音效包装力量感。

主要关键时点：1、6、12、18、21、23、25、27、29、30、32、35、39、44、50、56、60。重复停留姿态不等于独立原画数。3 秒是展示练习，未作为正式游戏攻击时长。

## 检查与发现

第一轮检查发现：肩扛/蓄力时有手部超出可达范围，脚位也有部分超限；独立目视审查还指出肩侧柄的重叠、腰膝接缝。修订了握点、武器侧向路径、髋高、脚位，并重做连续袖/裤网格后再导出。

最终保存工程经重新打开验证，见 [editability_check.json](editability_check.json) 和逐帧 [verification.json](verification.json)：

- 60 帧腕目标最大误差约 0.000079，脚踝目标最大误差约 0.000049；都小于 0.001 个场景米。
- 第 29 帧锤头包围盒与设定靶面高度差为 0；全段锤头未低于地面。这里只验证锤头与目标的安排，不是全模型碰撞证明。
- 首尾骨骼头位置相同；把已保存工程的左握点横移 0.015，左腕实际移动约 0.0150001。测试后复原，没有把试探位移保存进工程。
- 静帧已经检查肩扛、最大蓄力、接触、卸力；同时提供侧视，以免单机位掩盖动作关系。三份导出均经 ffprobe 验证为 720×720、20 fps、60 帧、3 秒。网页的加载、播放、切视角与拖动按钮本轮尚未完成 UI 实测（见下方访问限制）。

这些指标只证明约束连续、可编辑，不能证明好看、有力量或达到商业水准。

## 还差什么

人物仍是风格化练习模型：脸与原作神态不够贴近，肩肘和衣服形变比较简化；双手扛肩的姿态也还缺少原作单手肩扛的松弛与张力。抬锤有时遮挡脸，换握、眼神和脚跟发力未精做。

手掌/简化手指是随武器的可见握持组件，腕部通过 IK 追随；没有独立手指骨骼。锤头接触由关键姿势安排，没有物理碰撞或受击目标变形。头发是分束和形态键跟随，不是自动碰撞模拟。没有最终二维轮廓清稿、帧替换画或游戏内手感验证。

达到商业级仍需要在同一个短片里持续做角色修形、姿态批改和最终二维整理；这轮的收获是获得可反复编辑的角色与动作，不是证明“脚本建模即可自动达到成片水准”。

## 在 Blender 中编辑

1. 打开 `st_hizuru_weight_study.blend`，文件保存于第 1 帧；数字键盘 0 进入相机，空格播放。
2. `CTRL_Hammer_rigid` 是武器主控；左右握点为其子级。改主控的位置/旋转并插入关键帧，双手跟随；移动过远时需检查腕部是否还够得到。
3. `CTRL_Left_elbow` / `CTRL_Right_elbow` 是肘部方向；脚与膝的控制器同样以 `CTRL_` 开头。
4. 角色骨架为 `ST_Hizuru_study_rig`，主动作 `ST_weight_shoulder_load_drive_contact_settle`。改变躯干关键帧后要检查头、肩和手的关系。
5. `Hair_back_lock_*` 和侧发具有 `Tip_lag` 形态键；可在 Graph Editor 调整跟随强度和时刻。
6. 角色实际使用 EEVEE 节点材质。另存工程再修改；本脚本重跑会覆盖本实验目录内的工程/预览，不操作正在打开的 GUI 未保存内容。

## 重现与来源

本机 Blender 5.2.0 LTS，使用其内置 BVH 导入器。输入沿用上一轮保留的 `../tutorial-probe-2026-09-21/momask_raw.bvh` 的骨架比例，**清除了模型原动作，当前动作由本轮编排**。没有再次访问在线模型，没有新下载权重、插件或第三方人物。

人物识别点参考现有 `Assets/Resources/Art/Portraits/25D/st_hizuru.png`；没有将该图片投影成网格。原作角色不是本项目原创。教程只用于方法学习，未复制其中角色资产。保存与渲染都由 Blender Python API 完成，不称作 Computer Use 手绘；Computer Use 尝试打开本地对照页时被 Browser URL policy 拒绝，未通过其他浏览器绕过。因此这轮没有实际验证网页按钮；已验证的是 Blender 工程、导出视频结构和静帧。

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python docs/motion/art-weight-trial-2026-09-21/build_study.py -- all
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python docs/motion/art-weight-trial-2026-09-21/render_views.py -- side
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python docs/motion/art-weight-trial-2026-09-21/render_views.py -- baseline
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python docs/motion/art-weight-trial-2026-09-21/verify_scene.py
```

`make_preview.py` 需要 Pillow，并调用 `/opt/homebrew/bin/ffmpeg` 生成三份视频、GIF、关键帧拼图和内嵌媒体的离线网页。本机可使用 Codex bundled Python：`/Users/jacklee/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3`。未运行 Unity 编译，因为没有修改或导入游戏内容。
