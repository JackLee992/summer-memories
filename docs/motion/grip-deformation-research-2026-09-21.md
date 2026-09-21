# 单手肩扛、接柄和衣袖连续形变：教程与本机 API 核验

日期：2026-09-21。此轮只研究与验证独立动画方法，未改主实验脚本，未恢复 Unity 开发。

## 已实际读取的资料

| 来源 | 已读内容 | 对当前动作的用法 |
|---|---|---|
| [Blender 官方：Child Of Constraint](https://docs.blender.org/manual/en/latest/animation/constraints/relationship/child_of.html) | `Influence` 可动画，因而可以随时间切换父级；`Set Inverse` 用于修正启用约束后不正确的变换。多个约束可以混合，但官方提醒骨骼目标有时更适合 Armature Constraint | 处理空手→握柄、握柄→松手的控制空间切换；不能只在接触帧把 Influence 从 0 改成 1 |
| [Blender 官方：Armature Modifier](https://docs.blender.org/manual/en/latest/modeling/modifiers/deform/armature.html) | 顶点组名称与骨名对应，组权重决定骨骼影响；Preserve Volume 采用四元数保体积，减少关节旋转造成的塌缩，但接近 180° 时仍有不连续限制 | 衣袖改为真正连续网格，在肩/肘设置连续权重过渡，测试 Preserve Volume，而非将多个硬块仅仅贴在一起 |
| [Blender 官方：Smooth Corrective Modifier](https://docs.blender.org/manual/en/latest/modeling/modifiers/deform/corrective_smooth.html) | 常用在 Armature 之后减少关节形变扭曲；支持顶点组局部限制；过强会引入体积损失；如果之前有改变拓扑的修改器，需注意 Bind Coordinates | 修正肩窝或肘部局部扭结，不能用全身强平滑掩盖坏拓扑和坏权重 |
| [Blender 官方：Weight Paint Options](https://docs.blender.org/manual/en/latest/sculpt_paint/weight_paint/tool_settings/options.html) | Auto Normalize 保持变形组权重和为 1；Multi-Paint 保持已选组之间相对影响；非零权重才能有有意义的比例，Smooth Weight 可从邻点传播初始分布 | 肩部同时受胸、锁骨、上臂影响；先有合理分布，再平滑局部，不要让所有衣袖顶点 100% 绑定一根骨 |
| [Blender 官方 Python API：ChildOfConstraint](https://docs.blender.org/api/current/bpy.types.ChildOfConstraint.html) | `inverse_matrix`、`set_inverse_pending`、`target`、`subtarget`、各轴开关与继承的 `influence` | 脚本建立和调整接触空间，可做最小可复现探针验证 |

上述均读取了网页正文，未把视频标题作为教程内容。`latest` / `current` 文档可能随网站更新；是否可用于本机另见下方实际测试。

## 本轮推荐的控制结构

继续以锤为主控制器，不把锤的运动反过来依赖同一条 IK 手臂，否则容易形成“锤跟手、手跟锤”的循环。

- 右手持续握住主握点，单手肩扛时保留肩接触与适量肘弯曲。
- 左手单独有一个 IK 目标。空手阶段走明确的世界空间轨迹，从身体侧方抬起、接近柄。
- 接触帧前，让这个目标的位置与朝向先匹配左握点。随后启用跟随；跟随段让握点随锤运动。
- 松手时先记录当前可见的世界变换，在这一变换上解除跟随，再从这里开始空手轨迹。不能直接回到它接柄前的旧本地坐标。
- 手指接近时张开，接触时合拢，松手时再打开。腕 IK 位置正确并不代表手掌/手指接触正确。

这是一套项目实施建议。官方 Child Of 页面定义工具行为，没有替我们规定锤子的具体表演。

### 两种可落地方式

**方式 A：Child Of 空间切换。** 为手目标添加跟随握点的约束，匹配切换前后矩阵，并给 `influence` 插关键帧。明确的抓住/放开用阶梯（CONSTANT）变化；不要让中间影响值随自动曲线形成悬浮半跟随。伸手动作由目标位置关键帧完成，而不是用缓慢提高约束影响值来代替。

**方式 B：世界空间烘焙手目标。** 逐帧计算握持段目标的 `hammer.matrix_world @ grip_local_matrix`，空手段使用自己的世界轨迹，在接触点对齐两段。对于当前短实验通常更容易检查，也不会受到约束叠层和已有父级影响。仍然保留原始锤/握点控制器，方便重烘焙；只是烘焙后的目标不能宣称会自动追随之后修改的锤动画。

方式 B 可以是快速验证分镜的工具，但正式可编辑控制可以继续采用方式 A。不能把对象无父级时的逆矩阵公式原封不动用于任意姿态骨骼：PoseBone 的 `matrix`、骨架对象变换、父级和约束空间都要统一。

## 避免切换跳帧的步骤

1. 在切换帧更新依赖图，读出切换前控制器的实际可见世界矩阵。
2. 将准备启用的空间对齐，设好逆矩阵/偏移，使启用前后位置和朝向相同。
3. 插入前一帧与切换帧的控制状态关键帧，并把开关曲线设为 CONSTANT。握点的持续运动本身保持平滑。
4. 在 `f-1、f、f+1` 和必要的半帧重算矩阵；分别检查位置跳差、角度跳差、锤杆局部接触坐标变化。
5. 松手重复相同流程，保留当前世界变换后再开始自由手动作。
6. 当存在非均匀缩放或复杂父级时，不直接假定 `target.matrix_world.inverted()` 足以解决；先用隔离探针验证实际层级。

“切换没有跳变”与“接触期间没有滑动”应分开验收。反例是抓住瞬间无跳变，但左手后续仍在世界空间插值，导致武器移动时手沿柄漂移。

## 肩肘衣袖：需要改什么

先做连续网格与权重，再做修形。当前每段肢体独立且 100% 归属单骨的做法，只能得到刚性拼装效果；启用 Preserve Volume 不会把不同网格自动缝合。

建议衣袖在肘处至少有可支撑弯折的连续环线，肩袖与躯干连接处有连续顶点或明确设计的衣料搭接。肩口的影响由 `Spine2 / Shoulder / Arm` 平滑变化；肘弯区域由 `Arm / ForeArm` 平滑变化。权重范围需要根据实际骨骼、衣袖轮廓和举臂动作观察，不能用一个固定的“50%/50%”公式当作所有角色的正确答案。

- 先逐个看肩扛、抬臂抓柄、举锤、接触下压四个极限姿态。
- 保持腋下折叠空间，避免关节附近完全塌成零厚度。
- 比较 Preserve Volume 开/关，确认没有肩部鼓包；它并非无条件更好。
- 最后仅在肩肘顶点组上加较轻的 Corrective Smooth，减少扭结。它可能缩小体积，因此要对照剪影和袖口厚度。
- 衣服褶皱/贴图留到连续形变通过后；高频纹理不能解决断开的肩袖。

## 本机 Blender 5.2 实际小探针

执行程序：`/Applications/Blender.app/Contents/MacOS/Blender`，`--background --factory-startup`，未读写当前主 `.blend`。

返回版本：`5.2.0 LTS`，hash `fbe6228777e7`。

测试 1：两个无父级 Empty，一个作为手目标，一个作为握点；后者只含平移。设 `ChildOfConstraint.inverse_matrix = target.matrix_world.inverted()` 后，把影响从 0 变为 1。

```json
{
  "blender": "5.2.0 LTS",
  "childof_inverse_matrix": true,
  "set_inverse_pending": true,
  "switch_position_error": 0.0,
  "target_quarter_meter_hand_movement": 0.25,
  "preserve_volume_set": true,
  "corrective_smooth_factor": 0.20000000298023224,
  "corrective_smooth_iterations": 3
}
```

测试 2：创建真实的 `shoulder_blend` 顶点组后，`CorrectiveSmoothModifier.vertex_group` 能设置为该组；`use_pin_boundary`、`rest_source`、`smooth_type` 属性均存在。

小陷阱：如果还没有创建该顶点组，给 `vertex_group` 直接赋一个不存在的名字，本机读回的是空字符串。应先 `obj.vertex_groups.new(name='shoulder_blend')` 再指定修改器组名。

**测试边界**：上面验证了本机属性可用，以及最简单对象空间下启用 Child Of 没有位置跳变；没有测试任意父骨、多个叠加约束、非均匀缩放、180° 旋转，也没有证明主工程肩袖形变已通过。完整角色仍必须目视与接触轨迹验收。

API 适用片段（需接入实际对象；不是已运行的主工程补丁）：

```python
arm_mod = sleeve.modifiers.new('SleeveArmature', 'ARMATURE')
arm_mod.object = rig
arm_mod.use_deform_preserve_volume = True

# shoulder_blend 须包含需要修正的真实顶点权重。
if not sleeve.vertex_groups.get('shoulder_blend'):
    sleeve.vertex_groups.new(name='shoulder_blend')
smooth = sleeve.modifiers.new('ShoulderCorrection', 'CORRECTIVE_SMOOTH')
smooth.factor = 0.2
smooth.iterations = 3
smooth.vertex_group = 'shoulder_blend'
```

0.2 / 3 只是保守试验起点，不是官方推荐值；空组没有形变修正效果。新增修改器顺序也不能破坏现有细分/绑定拓扑。
