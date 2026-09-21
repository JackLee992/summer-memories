# MoMask 在线实验：接口与 Blender 预检

核对日期：2026-09-21。本文只记录公开源码、运行中配置和本机安装文件的只读检查；未提交生成任务、未下载模型权重、未启动 Blender 或 Unity。实际动作样本与质量判断由实验记录另行提供。

## 在线服务与调用

- Space：[MeYourHint/MoMask](https://huggingface.co/spaces/MeYourHint/MoMask)。[HF 元数据](https://huggingface.co/api/spaces/MeYourHint/MoMask)检查时为 `RUNNING`、`cpu-basic`，源码版本 `4b0323058afeb8d4443d5b370a9e35fd9537222b`。
- 服务根地址：`https://meyourhint-momask.hf.space`。[实时 config](https://meyourhint-momask.hf.space/config)为 Gradio `3.24.1`，`enable_queue: true`。
- Generate 按钮：`fn_index = 3`；文本框回车：`fn_index = 4`。两者输入为组件 `[4,7,10]`，输出为 HTML 组件 `[19]`；`api_name` 均为 `null`。
- 三个输入依次是文本字符串、秒数字符串、`"IK"` 或 `"Raw"`。例如数据结构 `{"fn_index":3,"data":["A person walks forward.","4","IK"]}`。本预检没有提交该请求。
- [Gradio 3.24.1 路由源码](https://github.com/gradio-app/gradio/blob/v3.24.1/gradio/routes.py)支持 `/api/{api_name}` 或 `/run/{api_name}` 搭配显式 `fn_index`；直接 HTTP 能否绕过排队受服务 `api_open` 控制，尚未实测。该版本排队入口是 WebSocket `/queue/join`，不是新版 Gradio 的 `/call/...`。
- WebSocket 交互：服务要求 `send_hash` 时回传 `session_hash` 和 `fn_index`；要求 `send_data` 时发送输入数据及同一会话信息；读取 `process_completed` 的 `output.data`。可从 [queue/status](https://meyourhint-momask.hf.space/queue/status)只读查看队列。检查时队列为零，均值约 9.88 秒；这不是本次样本耗时保证。
- `/info` 本次读取失败，不能声称其接口说明可用。以上参数由实时 config 与 app.py 交叉确认。

## 输入限制与复现

依据 [app.py](https://huggingface.co/spaces/MeYourHint/MoMask/blob/4b0323058afeb8d4443d5b370a9e35fd9537222b/app.py)：

- 秒数先转换为 `int(float(seconds) * 20)`，夹到 0–196，再向下取 4 的倍数，因此最终以 0.2 秒步长量化，最长 9.8 秒。`4` 对应 80 帧。
- 输入 `0` 或无法解析的时长会使用长度预测器。避免非零但小于 0.2 秒的输入，以免量化后为零帧。
- 只公开文本、时长、后处理三项；不公开种子、采样次数、采样温度或模型参数。
- 内部 `generate` 虽有 `seed=10107` 参数，但每次 `fixseed(seed)` 被注释；进程启动时才调用固定种子。在线同样输入不能承诺逐次复现。
- 内部默认 `repeat_times=1`，每次生成一条。
- `IK` 额外处理足部接触。`Raw` 关闭足部锁定；两者仍经关节位置到 BVH 旋转的拟合。官方 README 明示简单 foot IK 有时有效、有时失败。

## 输出和并发注意事项

返回值不是文件对象列表，而是一个 HTML 字符串，里面包含下载链接和 MP4 `<source>`。

| 文件 | 服务端路径 | 含义 |
|---|---|---|
| BVH | `./cached/12138/sample_repeat0.bvh` | 可编辑的骨架运动 |
| MP4 | `./cached/12138/sample_repeat0_<随机整数>.mp4` | 20 fps 骨架预览 |
| NPY | `./cached/12138/sample_repeat0.npy` | 转换后全局关节坐标，形状 `(frames,22,3)`；写入源码可见，但 HTML 未提供下载链接 |

HTML 使用 `file/` 加相对路径（例如 `file/./cached/12138/sample_repeat0.bvh`）。下载 URL 应从该次响应中提取并相对服务根地址解析。

`uid` 在源码固定为 `12138`，BVH/NPY 文件名固定，**其他任务完成后可能覆盖它们**。实验应在当前请求成功后立即保存 BVH 和对应 MP4，记录请求、响应、时间与文件校验值。随机 MP4 名字不等同于独立 BVH 存储。不要把实验之前读取的已有缓存当成本次输出。

## BVH 结构

依据 [joints2bvh.py](https://huggingface.co/spaces/MeYourHint/MoMask/blob/4b0323058afeb8d4443d5b370a9e35fd9537222b/visualization/joints2bvh.py)、[模板](https://huggingface.co/spaces/MeYourHint/MoMask/blob/4b0323058afeb8d4443d5b370a9e35fd9537222b/visualization/data/template.bvh)和 [BVH_mod.py](https://huggingface.co/spaces/MeYourHint/MoMask/blob/4b0323058afeb8d4443d5b370a9e35fd9537222b/visualization/BVH_mod.py)：

- 输出为 22 关节、69 通道；根 `Hips` 为 6 通道 `Xposition Yposition Zposition Zrotation Yrotation Xrotation`，其他 21 关节各 3 通道 `Zrotation Yrotation Xrotation`。
- 生成导出固定 `Frame Time: 0.050000`，即 20 fps。模板原文件的 60 fps 不代表生成结果帧率。
- 层级：`Hips → LeftUpLeg → LeftLeg → LeftFoot → LeftToe`，右腿同构；`Hips → Spine → Spine1 → Spine2`；Spine2 向上接 `Neck → Head`，向左右接 `LeftShoulder → LeftArm → LeftForeArm → LeftHand` 及右臂同构。
- Head、双手、双脚趾各有一个零偏移 End Site；无手指、面部、服饰骨骼，也无网格/蒙皮。
- 转换器把 HumanML3D 关节顺序重排为 BVH 层级，使用模板骨长，保留根位置，执行 100 轮基本 IK 拟合。模板纵轴为 Y；Blender 导入应保留相应轴转换。

## 本机 Blender

读取 `/Applications/Blender.app/Contents/Info.plist` 得到版本 **5.2.0**。

已有官方 BVH 插件目录：`/Applications/Blender.app/Contents/Resources/5.2/scripts/addons_core/io_anim_bvh`。源码中的导入 operator 为 `bpy.ops.import_anim.bvh`；没有缺插件的证据，无需先下载插件。

根据该安装版本源码，导入参数可使用：

```python
# 仅列出建议调用，预检没有执行。
bpy.ops.preferences.addon_enable(module="io_anim_bvh")
bpy.ops.import_anim.bvh(
    filepath=bvh_path,
    target="ARMATURE",
    global_scale=1.0,
    frame_start=1,
    update_scene_fps=True,
    update_scene_duration=True,
    use_fps_scale=False,
    rotate_mode="NATIVE",
    axis_forward="-Z",
    axis_up="Y",
)
```

`update_scene_duration` 只延长，不缩短默认时间线；实际预览仍应根据 BVH 帧数明确设置 `frame_end`。以上只验证安装文件和参数定义，是否成功导入及动画显示需由实际实验确认。

## 官方重定向教程

[官方 README Visualization / Retargeting](https://github.com/EricGuo5513/momask-codes#dancers-visualization)流程：

1. 准备带骨架的 Mixamo T-Pose 角色 FBX。
2. 安装 [KeeMap Rig Transfer](https://github.com/nkeeline/Keemap-Blender-Rig-ReTargeting-Addon/releases)。官方提供[视频教程](https://www.youtube.com/watch?v=EG-VCMkVpxg)。
3. 在 Blender 中导入 BVH 和角色 FBX；Shift 选中源、目标骨架，进入 Pose Mode，打开 KeeMapRig。
4. 加载 [mapping.json](https://github.com/EricGuo5513/momask-codes/blob/main/assets/mapping.json)，不适用时尝试 [mapping6.json](https://github.com/EricGuo5513/momask-codes/blob/main/assets/mapping6.json)，点击 Read In Bone Mapping File。
5. 调整 Number of Samples、Source Rig、Destination Rig Name，再执行 Transfer Animation from Source Destination。自定义角色可手改映射和旋转修正。

两个映射文件均为 18 对骨骼，目标分别使用 `mixamorig:` 与 `mixamorig6:` 前缀；不含双手和脚趾映射。文件内样本数、源骨架名和 Windows 路径是作者环境值，不能直接当作本次设置。KeeMap 在本机 Blender 5.2 的兼容性未验证。

官方还链接了[第三方 Blender 集成教程](https://medium.com/@makeinufilm/notes-on-how-to-set-up-the-momask-environment-and-how-to-use-blenderaddon-6563f1abdbfa)与 [bvh2vrma 在线可视化](https://vrm-c.github.io/bvh2vrma/)；这两者不是本次预检已实测的管线。
