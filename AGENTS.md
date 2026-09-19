# AGENTS.md —— 协作契约

本文件约束所有人类协作者与 AI 代理在本仓库中的行为。改动代码或剧情前请通读。

## 1. 项目是什么

《夏日回忆 · 归墟来潮》：Unity 6 LTS / C# 的时间回溯 ADV + 网格战棋叙事游戏。
**叙事优先**：任何系统改动都要回答"它如何服务于动人/惊奇/探险/猎奇四种体验之一"。

## 2. 技术边界（硬约束）

- 引擎锁定 **Unity 6 LTS（6000.0.83f1）**，不得擅自更换引擎或引入需要替换引擎的方案。
- 程序集依赖方向只能向内：
  `Core ← ADV / Battle / Loop ← App ← Editor`。Core 不得引用任何上层程序集。
- 玩法内容**数据驱动**：剧情写 `Resources/Story/*.json`，关卡写 `Resources/Battles/*.json`，
  异闻写 `Resources/Tips/*.json`，文案写 `Resources/Localization/*.json`；不要把剧情文本硬编码进 C#。
- UI 当前全部由代码生成（uGUI + `UIFactory`），参考分辨率 1920×1080 横屏；在引入 UI 框架/Prefab 前需在 ADR 中说明理由。
- 运行时通过 `GameBootstrap`（`[RuntimeInitializeOnLoadMethod]`）自举，不依赖手工维护的场景；
  出包由 `BuildScript` 动态生成临时 Boot 场景。
- 存档路径固定为 `Application.persistentDataPath/saves/{slot}.json`，损坏存档必须自动隔离（`.broken-<ticks>`），不得覆盖。

## 3. 命名与内容约定

- 主角叫**石头（大名石磊）**，不是孙悟空。孙悟空是千年前的传说人物；石头的神话根器（六耳猕猴/"二心"）是终章反转，
  在序章至第三章只能留白暗示，**不许提前说破**。
- 女主现代小名**阿娃**，神话层为女娃/精卫。其他神话人物（龙女、七爷/菩提、夜叉、六耳）的身份揭示节奏以 GDD 为准。
- 新增角色/术语时，同步更新：对应 JSON、`docs/GDD.md`、`docs/art-and-story-bible.md`、异闻录条目。
- 资源 key：背景 `Art/Bg/{key}`、立绘 `Art/Portraits/{key}`、图标 `Art/Icons/{key}`；新增图片放入对应 `Resources/Art/` 目录。

## 4. 版权红线（不可违反）

- 只使用：公版文本（《西游记》《山海经》《列子》《庄子》等）、本项目原创内容、许可明确的第三方资产。
- **禁止**使用以下作品的独创内容（即使"致敬"也不行）：
  - 电影《大话西游》系列：月光宝盒道具与造型、至尊宝/紫霞/白晶晶等角色、"般若波罗蜜"回潮口诀、片中台词；
  - 时间循环类动画的独创设定（特定岛名、"影子病"等命名、角色名与人设、原画与音乐）；
  - 《黑神话：悟空》的具体美术、台词、关卡表达；
  - 任何商业游戏 ROM 提取资源、字体、音乐、配音。
- 借鉴只能停留在**范式层**（回溯救人、喜剧外壳悲剧内核、二心隐喻、信息差战棋），表达必须原创。
- AI 生成素材仅限原型阶段；生成记录要可追溯，商业化前由签约美术全部重绘（见 licensing 文档）。

## 5. Git 与提交

- 不提交 `Library/ Temp/ obj/ Builds/ Logs/ .vs/` 及外置内容目录（`.gitignore` 已配置）。
- 提交信息建议格式：`type(scope): 简述`，type ∈ feat/fix/data/art/docs/chore/build。
- 数据类改动（JSON）提交前必须通过 JSON 解析校验；C# 改动必须在 Unity 中编译通过（或说明为何无法本地验证）。
- 不要把 `.meta` 文件遗漏：在 Unity 编辑器导入过的资源，其 `.meta` 需一并提交，避免 GUID 漂移。

## 6. 完成定义（DoD）

- 走通"标题 → 序章 → 回潮 → 战斗 → 胜利/失败两条路径"无空指针、无死路；
- 新增 JSON 可解析、id 引用存在（立绘/背景/tip/jumpIndex）；
- 真机（或说明中指定的最低环境）可安装运行；
- 文档随实现更新；已知限制在 PR/提交说明中显式列出。

## 7. 验证手段

- JSON：`python3 -m json.tool <file>`；
- 素材清单：`python3 tools/content_manifest.py --check`；
- 编译/出包：Unity batchmode 跑 `SummerMemories.Editor.BuildScript.BuildAndroid`；
- 玩法回归：编辑器 Play 模式走通主路径，观察日志无异常。
