# 从中性骨架人升级到可辨认角色：实际读过的美术教程与论坛分享

查验日期：2026-09-21。本轮对象是 `tutorial-probe-2026-09-21/blender_lesson.py` 的挥锤练习：当前使用锥体四肢、球形关节、无五官的头部，工作台渲染。目标是自行建立带**长深色头发、眼镜、深色西装、长柄锤**识别点的日鹤练习角色，保留可编辑动作。它仍是原型美术练习，不等于成熟动画角色，也不将原作角色设计称为本项目原创。

下面四组资料均实际读取了公开正文；其中一节取得并读完官方英文字幕，两篇论坛分享还检查了图。**没有观看视频，不用“看完教程”描述只读字幕或简介的情况。** 没有下载、导入任何教程人物、付费素材或商业作品媒体。

## 1. Julien Kaspar：从独立体块建立身体，而后检查服装能否运动

来源：[Blender Studio — Stylized Character Workflow](https://studio.blender.org/training/stylized-character-workflow/)。这是 Blender Studio 角色艺术家 Julien Kaspar 的课程，原课程以 Blender 2.8 为基础；完整课程含订阅内容。以下选的是公开页面，不能说整套课程免费。

- [Creating a Primitive Body，免费课](https://studio.blender.org/training/stylized-character-workflow/5d7f7cf055ccaf1a4a78102d/)。已读页面和[官方英文字幕](https://studio.blender.org/api/videos/track/189/56/56f2a19d54b34893a6d7aec6e40a168b/56f2a19d54b34893a6d7aec6e40a168b.vtt)，字幕到 27:03；未观看视频。
- [Rain — Body Iterations](https://studio.blender.org/training/stylized-character-workflow/5d403efdbefa49b17d58df32/)。已读作者过程说明。
- [Rain — Outfit Variations](https://studio.blender.org/training/stylized-character-workflow/5d403f3dbefa49b17d58df37/)。已读作者服装选择说明。

真正可跟做的步骤，以下时间来自字幕，而不是凭课程目录猜测：

| 时间 | 作者步骤 | 这次对应的修改 |
|---|---|---|
| 03:02–03:46 | 以头高作比例单位，说明理想化八头身与约七点五头身；是便于记忆的基准 | 用头高检查躯干、腿长，先确定成人比例。不要把八头身当所有风格必须遵守的公式 |
| 03:47–06:18 | 从侧面用胸廓、骨盆两球体建立相向倾斜的结构，中间加腹部；保持体块分离方便改比例 | 替换现在竖直叠放的几节躯干，建立胸—腰—骨盆变化；先在正、侧、三分之四视角检查，再加衣领 |
| 12:02–14:34 | 手臂形体像链节，有宽面和窄面；前臂向手腕收窄，中间可增加截面控制宽度 | 衣袖用有肩部、肘部和袖口的截面，不把上下臂都做成相同圆管；减弱外露球形关节的玩偶感 |
| 15:02–17:40 | 腿保留一定方形截面，侧视与正视分别塑形，小腿后侧留出腿肚，向脚踝变窄 | 西裤以少量截面形成大腿、小腿、裤脚，鞋要有明确脚掌方向，避免圆柱直接插地 |
| 24:04–26:24 | 鞋底形体与脚背形体分开搭，再衔接脚踝 | 从当前短圆锥脚改成有鞋底、鞋头的简单鞋形，固定支撑脚的接地点 |

Body Iterations 的作者说明强调：先用独立球体调部位比例，满意后再合并；他为半写实人物选放松的 A pose。**本练习已有动画骨架，不需要为了照课立刻改静止姿态或重建骨架**，应在现有骨架静止空间中建立新外形。

Outfit Variations 特别有用：作者因卷起的袖子会与躯干相交，最后选择较便于动画的无袖服装；丝巾则被明确设计为跟随动作练习。本项目需要西装，不能照抄无袖结论，但可照做他的判断方法：在肩膀最高、双手握锤、胸前交叉和最大前倾四种姿势先查衣服，再雕褶皱。

**质量陷阱**：给粗骨架加领子和纽扣，不能自动修复肩宽、胸廓厚度、腿形和手脚方向。静止姿势好看也不能证明衣服在挥锤中可用。上述课程是建模基础，不提供自动修复这段动作的功能。

## 2. 美术论坛里的可操作节点图：Koyoinu 的风格化发束

- [BlenderArtists — Stylized Hair - Geometry Nodes](https://blenderartists.org/t/stylized-hair-geometry-nodes/1406121)
- 作者：Koyoinu；2022-09-12。
- [原帖节点图](https://blenderartists.org/uploads/default/original/4X/a/c/e/acef71e002fa49676569953941526c2028b6355d.png)
- 查验：通过论坛公开 JSON 读了两条帖文，并实际查看 1828×828 节点图。这是用户分享，不是 Blender 官方兼容性承诺。

图里的连接可以明确读出：

1. 输入曲线经过 `Deform Curves on Surface`，再进入 `Resample Curve`，示例采样数为 30。
2. `Spline Parameter` 的 Factor 经 `ColorRamp` 和 `Float Curve` 调整后，连接 `Set Curve Radius` 的 Radius。示例色阶由根部白色走向末端黑色，用来控制发束粗细变化。
3. `Object Info` 读取单独的 `BezierCircle`，其 Geometry 接入 `Curve to Mesh` 的 Profile Curve。
4. 已设置半径的曲线接入 `Curve to Mesh`，得到实际网格发束。

**落到当前练习**：先做一根可编辑的发束路径，截面压成扁椭圆或略带棱面的形状，使根部有宽度、尾端收尖；确认侧面厚度，再复制成左右侧发、后发和刘海几个大组。可以用普通曲线加截面或程序网格实现同样的形体原则，不必先搭完整的新毛发系统。

**不要遗漏原帖的限制**：第二条回复的作者 Ztitus 仍在寻找发束扭转、发际线调整、发束之间碰撞的方法。该节点图解决的是“曲线变成有粗细的发束”，**并不等于已经解决头发绑定、自动碰撞与次级动画**。本次不声称在当前 Blender 版本复现了原节点树。

## 3. 美术论坛里的多视角范例：Xeofrios 的几何发型分享

- [BlenderArtists — Geometry Nodes Stylized Hair](https://blenderartists.org/t/geometry-nodes-stylized-hair/1455508)
- 作者：Xeofrios；2023-03-17，作者回复为 2023-03-20。
- [长发四视角参考图](https://blenderartists.org/uploads/default/original/4X/e/2/1/e21eae43fb515c875ad529939c83d7362c0930fc.jpeg)
- [后续分享 — Stylized Hair with Node tools](https://blenderartists.org/t/stylized-hair-with-node-tools/1520904)，2024-03-17。
- 查验：读完两帖返回的全部帖文，查看了第一帖长发图的 425×500 预览；未播放帖子内的视频，未下载作者工程。

第一帖有多张多角度成品图。作者在回复中明确说，曲线之间补面的 `add surface` 操作来自当时预载的 **Bsurfaces** 插件。2024 年的后续帖则说明，旧方法反复应用 Geometry Nodes modifier 较繁琐，Blender 4.0 node tools 改善了操作。不能把旧版本“预载”描述为当前 Blender 一定已经安装。

**从实际看过的长发图可以借鉴的形体观察**：

- 刘海、耳前侧发、后脑发体和肩后长发形成不同层次，脸部仍有清楚开口。
- 发束有宽窄变化，末端高度错开；没有全部做成等长、等粗、平行的圆管。
- 从侧面和背面也能读出大体积，正面好看不是唯一验收角度。

这些是对图的观察，不冒充作者给出的逐步口述。**本次练习建议**：用约 8–12 个主要发束先形成背发体，再加少量刘海和侧发；数量是我们给原型的预算，不是论坛作者的规定。后发根部贴合头壳，发尾到肩背间留出运动余量；眼镜前方不要被两条对称粗刘海完全挡住。

**质量陷阱**：均匀复制会造成面条感；只有一整片后发会像披风；过细的尖端在 512 像素动作预览里会闪烁或消失。先修大轮廓，再考虑增加细沟和高光。

## 4. 官方卡通材质示例：先让明暗可控

- [Blender 4.5 Manual — Shader To RGB Node](https://docs.blender.org/manual/en/4.5/render/shader_nodes/converter/shader_to_rgb.html)
- 查验：HTTP 200，已读正文及 Examples 的说明；此页有 Toon shading 示例。

官方明确给出用法：**Diffuse BSDF 的输出经过 Shader to RGB，再用 Color Ramp 得到可调的 toon shader**。该节点仅用于 EEVEE。文档同时提醒它打破 PBR 流程，和部分效果、渲染通道结合时可能出现非预期结果。

**这次的最小材质练习**：改用 EEVEE，建立“Diffuse BSDF → Shader to RGB → Color Ramp → Emission → Material Output”；色阶使用 Constant 插值，先设两档或三档明暗。Emission 与 Constant 是本次为稳定明暗层次提出的实现选择，不是把整段节点连接归为官方原文。用一盏主光检查脸、头发和西装能否分开，然后再调轮廓线。

深色头发与深色西装不要都落在同一个纯黑值：可让头发偏冷黑，外套稍亮，白领形成清楚的浅色块。眼镜先用有厚度的镜框与鼻梁连接建立识别；本轮不必用高反射镜片遮住简化眼睛。眼镜、白领、配色方案属于本项目设计选择，没有找到并核实“日鹤眼镜建模”专门教程，因此不编造对应出处。

**当前实现的边界**：现有 `blender_lesson.py` 用的是 `BLENDER_WORKBENCH`。只换 `diffuse_color`、工作台灯光或轮廓开关，不能声称已经制作了上述 EEVEE 节点材质。应按实际使用的渲染器描述最终交付。

## 立即可落实的五项修改

以下是针对这段练习的设计与验收，不是教程作者的原话。

1. **先改身体大形**：成人头身比例、胸廓/腰/骨盆层次、有肩部的衣袖、收窄裤脚与鞋底。沿用原动作控制，避免在美术替换时同时改变挥击节奏。
2. **补齐角色识别点**：长深色头发、可见镜框、深西装和浅衬领。先在三分之四与侧视的整身图里确认可辨认，再增加面部细节。
3. **发束有主次与厚度**：少量宽发束形成后发体，刘海/侧发作为次级形体；根粗尾细、长短错开。头发即便暂时绑定头骨，也要明确标为尚无可信的长发次级动作。
4. **材质只做必要的层次**：有限配色与两三档明暗，保护眼镜和脸部的可读性。若使用工作台，只称风格化工作台预览；若使用 Shader to RGB，记录 EEVEE 与节点设置。
5. **检查挥锤极端姿势**：至少取准备、最大蓄力、下砸、卸力四帧；检查镜框是否离脸、肩袖是否裂开、头发是否穿过手臂/锤柄、裤脚是否吞进鞋面，并同时看正常速度预览。一个漂亮静帧不足以验收角色动画。

## 核实范围与本轮未采纳的入口

- Blender Studio 三个公开页及字幕、BlenderArtists 四个相关帖子 JSON、Blender 4.5 Shader to RGB 正文均实际读取。资料许可或页脚声明不自动成为任意嵌入模型的发布授权；本轮只学习方法并自行建几何形体。
- 还读了 BlenderArtists 的 [Help with stylized hair](https://blenderartists.org/t/help-with-stylized-hair/1330154)，但没有提升为核心教程：主要是少量发束排列/复制问题，不能从“最终用了 Array”推导出完整发型生产流程。
- Polycount 的本轮入口出现 HTTP 403 或 TLS 读取失败；若干 80.lv 试探地址返回 HTTP 200 的空壳，没有文章正文。它们没有作为已学习的教程证据。
- 没有取得可核实的 X/Twitter 原帖正文，因此没有补上来源不明的转述。论坛需求已由以上可阅读的 BlenderArtists 作者原帖与图片满足。
- 没有把本轮研究写成“教程步骤已全部在 Blender 实现”。最终实现、实际渲染方式和画面局限应由练习目录中的脚本、`.blend` 和预览记录另行证明。
