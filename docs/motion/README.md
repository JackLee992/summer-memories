# 动画研究与试验索引

本目录保存方法研究及离线试作；**不是已接入游戏的正式动作库**。当前阶段归档见 [2026-09-21进度](../progress-checkpoint-2026-09-21.md)，后续工作由所有者重新发起。

## 实际做过的实验

| 顺序 | 内容 | 可复查成果与主要结论 |
|---|---|---|
| 1 | [Piskel线稿](line-test-2026-09-20/README.md) | 11帧粗稿、原生文件、GIF；未完成商业清稿 |
| 2 | [节奏与约束](constraint-trial-2026-09-21/README.md) | 等时长/节奏/约束对照；几何连续不等于角色动画 |
| 3 | [身体与头发延迟](overlap-trial-2026-09-21/README.md) | 发尾/主体相位对照；仍为程序几何试验 |
| 4 | [MoMask → Blender关键姿势](tutorial-probe-2026-09-21/README.md) | 两次真实模型结果失败，保留BVH/MP4；用同骨架重新编排，非模型自动修复 |
| 5 | [人物与力量感](art-weight-trial-2026-09-21/README.md) | 长发眼镜西装、靶面接触、EEVEE材质、原生工程 |
| 6 | [单手肩扛精修](shoulder-polish-2026-09-21/README.md) | 72帧接柄/松手、近景、握点/脚位验证；最新可编辑工程 |
| 7 | [Seedance视频参考](seedance-v2v-2026-09-21/README.md) | 1条4秒，264积分；细节提升但比例/裁切/时序改变，只得到视频 |

## 教程和作者分享

- [传统动画方法](2d-animation-workflow-research-2026-09-20.md) · [可执行教程路线](tutorial-learning-route-2026-09-21.md)
- [人物美术与发束](character-art-tutorials-2026-09-21.md) · [力量感与反作用](weight-community-tutorials-2026-09-21.md)
- [论坛、博客与X入口](artist-social-links-2026-09-21.md) · [脸部精修](face-polish-research-2026-09-21.md) · [接柄与衣袖形变](grip-deformation-research-2026-09-21.md)
- [商业游戏动画研究](commercial-game-animation-research-2026-09-21.md) · [商业动画流程](commercial-animation-quality-research-2026-09-21.md) · [阶段质量方案](commercial-quality-plan-2026-09-21.md)

文档分别标注已读正文、已看图片、仅读字幕和未看视频，不能把链接收集量当作制作完成度。

## GitHub / Hugging Face候选

- [建模管线与image-to-3dlab](model-pipeline-research-2026-09-21.md)
- [Kimodo等动作模型](motion-model-research-2026-09-21.md)
- [角色动画候选](character-animation-candidates-2026-09-21.md) · [可编辑动作候选](editable-motion-candidates-2026-09-21.md)
- [二维绑定](2d-rig-options-2026-09-21.md) · [补间候选](inbetween-options-2026-09-21.md) · [组合建议](pipeline-recommendation-2026-09-21.md)
- [MoMask API实际探针](momask-probe-api-notes-2026-09-21.md)

大部分是资料调研，不声称已下载/部署/实际跑通。候选模型代码许可不自动覆盖权重、训练数据和生成物商业权限。

## 重新渲染

每个实验README列出脚本与依赖。Blender工程和最终视频已保留；逐帧PNG、渲染日志、`.blend1`和Python缓存仅留本机且被忽略，需要时从源工程重渲。对照HTML嵌入主要媒体，直接下载原始HTML后可离线查看；GitHub代码页不会运行交互页面。
