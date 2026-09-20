# 架构说明

## 当前动作版增量方向（2026-09-20）

当前已实现《夏日重现》小队 v03，具体范围见 [实施说明](st-squad-demo.md)。依赖方向 `Core ← ADV / Battle / Loop / Action3D ← App ← Editor`。

- `Action3D/Squad`：角色/意识/武器/变形、队友命令、敌人、配置场景、快照与亲历时间线。角色控制器禁用后再传送，避免回溯后的物理内部位置仍留在原点。
- `App/SquadDemoDirector`：唯一模式入口、ADV、存档、音频、暂停。HUD、形态目录、俯瞰、情报为 uGUI。
- `SquadBodyV02`：导入 Blender FBX，按命名关节驱动程序动作，材质统一使用小队表面着色器；可独立替换动画层。
- `OverlookArchive`：按分支持久化事件、地图路径、队员快照；未知未来不预填。
- Core 的 SaveSlot 保持通用，动作快照/档案序列化到命名 flag，`st_demo` 与 `auto` 分离。

旧单人白盒继续由 `--action3d` 访问，旧 ADV/战棋由 `--legacy` 访问，均由 App 在创建对象之前决定。旧 Action3D Bootstrap 不再事后销毁 App。新包通过 BuildPlayerOptions.extraScriptingDefines 选择 `SM_SQUAD`，不改全局定义。

## 1. 总览

Unity 6 LTS / C#，全部 UI 由代码生成（uGUI），剧情与关卡数据驱动（JSON + `Resources.Load`）。
游戏无需手工场景即可运行/打包：`GameBootstrap` 通过 `[RuntimeInitializeOnLoadMethod]` 自举；
`BuildScript` 出包时动态生成一个空 Boot 场景。

## 2. 程序集依赖

```
                ┌──────────┐
                │  Editor  │  BuildScript（仅编辑器）
                └────┬─────┘
                     ▼
┌──────┐      ┌──────────┐
│ Core │◄─────│   App    │ 自举/全局流程/标题/时间线/教学
└──┬───┘      └─┬──┬──┬──┘
   ▲            │  │  │
   ├────────────┘  │  └────────────┐
   ▼               ▼               ▼
┌──────┐      ┌──────────┐    ┌──────────┐
│ ADV  │      │  Battle  │    │   Loop   │
└──────┘      └──────────┘    └──────────┘
```

| 程序集 | 职责 | 依赖 |
|---|---|---|
| SummerMemories.Core | EventBus、Log、Palette/UIFactory/ArtRegistry、SaveSystem、LocalizationService、ExternalContentLocator | 无（仅 UnityEngine） |
| SummerMemories.ADV | AdvModels（指令 schema）、AdvDirector（推进/打字机/选项/标志位/回调）、AdvView（背景/立绘/对话框） | Core |
| SummerMemories.Battle | BattleModels、Grid（曼哈顿/BFS）、RewindSystem、BattleDirector（回合/AI/意图/桃橛/火眼金睛/胜负）、BattleView | Core |
| SummerMemories.Loop | TipsSystem（《山海异闻》，跨回潮继承） | Core |
| SummerMemories.App | GameBootstrap、GameDirector（状态流）、TitleScreen、LoopTimelineScreen、TeachingDialog | Core/ADV/Battle/Loop/Action3D |
| SummerMemories.Editor | BuildScript | 以上全部，Editor only |

## 3. 启动与全局流程

```
GameBootstrap(RuntimeInitializeOnLoadMethod, BeforeSceneLoad)
  └─ 创建常驻 Canvas/EventCamera → GameDirector
       TitleScreen
         └─ prologue.json（ADV）
              └─ loopreset 指令 → 回潮（loopCount++、解锁 tip、存档）
                   └─ TeachingDialog（归墟说明）
                        └─ loop_teaching.json（ADV 教学）
                             └─ battle_01（BattleView）
                                  ├─ 胜利 → 存档 → LoopTimelineScreen（周目时间线/异闻录/重战）
                                  └─ 失败 → loopCount++、tip_death_returns → 回潮重战
```

- ADV 与战斗都是"屏幕级" GameObject，由 `GameDirector.ClearScreens()` 统一清理。
- 跨场景/跨流程状态保存在 `SaveSlot`（单槽 auto，可扩展多槽）。

## 4. ADV 指令 Schema（`Resources/Story/*.json`）

```jsonc
{
  "id": "prologue",
  "title": "章节名",
  "commands": [
    { "type": "bg",      "art": "bg_dock", "color": "#2a6f8f", "note": "地点/时间字幕" },
    { "type": "portrait","portrait": "jingwei", "side": "right" },   // "" 清除立绘
    { "type": "line",    "speaker": "精卫", "text": "……" },
    { "type": "narration","text": "……" },
    { "type": "choice",  "text": "（提示）", "options": [
        { "text": "选项", "setFlag": "greet=tease", "jumpIndex": 6 } ] },
    { "type": "tip",     "tip": "tip_guixu" },       // 解锁异闻
    { "type": "loopreset","anchor": "jul01_dock", "tip": "tip_guixu" },
    { "type": "end" }
  ]
}
```

- `jumpIndex` 为 commands 数组下标；选项设置的标志位写入 AdvDirector 运行时状态（后续扩展条件指令）。
- `loopreset` 由 App 层接管：存档计数、教学对话框、进入下一章节。

## 5. 战斗系统

### 5.1 数据（`Resources/Battles/*.json`）

关卡：宽高、`rewindCharges`（毫毛数）、`nails`（桃橛数）、介绍/胜利/失败文案、单位数组。
单位：`id/displayName/team(0己方1敌方)/hp/atk/moveRange/attackRange/skill/gx/gy`。
`skill`：0 无、1 桃橛定身（近战专属）、2 衔石投石（远程）。

### 5.2 回合状态机

```
Player（己方单位逐个：移动→攻击/技能/待机）
  → EndTurn：ComputeIntents 生成敌方意图（移动格+攻击目标）→ IntentPreview（约 2 秒预告）
  → Enemy 执行（被桃橛眩晕者跳过）
  → 胜负判定 → 新回合（清眩晕、清意图、火眼金睛次数重置）
```

- **火眼金睛**：玩家阶段每回合可主动 `RevealIntents()` 一次，提前暴露意图；任何落子（PushSnapshot）都会使预判失效。
- 敌方 AI：BFS 逼近最近己方单位；射程内即攻击；意图在行动前完全公开，保证"公平可解"。

### 5.3 双层回溯

| 层 | 实现 | 状态 |
|---|---|---|
| 内层（救命毫毛） | `RewindSystem` 快照栈：每次玩家落子 `PushSnapshot()`（回合数/桃橛数/全体单位克隆），回溯 `RestoreSnapshot()` | 每场 3 次，JSON 配置 |
| 外层（回潮） | App 层 `SaveSlot.loopCount/anchorId/tipsUnlocked`；战斗失败或 ADV `loopreset` 触发 | 无限次，异闻继承 |

## 6. 存档

- 路径：`Application.persistentDataPath/saves/{slot}.json`（默认 slot=auto）。
- 内容：回潮次数、锚点、章节进度、battle01Cleared、已解锁异闻 id 等。
- 反序列化失败时把坏文件改名为 `.broken-<ticks>` 并新开存档，绝不吞掉用户数据。

## 7. 美术加载与外置内容

- `ArtRegistry` 按 key 从 `Resources/Art/...` 加载 Sprite；缺失时回退程序化纯色占位（开发期不阻断流程）。
- `ExternalContentLocator` 预留：`persistentDataPath/ImportedContent/manifest.json`（SHA-256 校验）。
  原创路线下作为未来 Mod/章节扩展机制保留，MVP 不启用。

## 8. 构建

- `SummerMemories.Editor.BuildScript.BuildAndroid`：
  设置 PlayerSettings（横屏、包名 `com.summermemories.study`、minSdk 26、ARM64、debug 签名）
  → 生成 `Temp/Boot_build.unity` → `BuildPipeline.BuildPlayer` 输出
  `Builds/summer-memories-v0.1-android.apk` → 清理临时场景。
- 菜单等价入口：**夏日回忆 → Build → Android APK (ARM64)**。

## 9. 扩展指引

- 新增关卡：写 `Resources/Battles/battle_02.json`，在 GameDirector 增加入口；后续改为章节表驱动。
- 新增神通：在 BattleDirector 增加能力状态与按钮（参考 RevealIntents 的"每回合一次 + 落子失效"模式）。
- 新增敌人行为：扩展 EnemyIntent 与 ComputeIntents/ExecuteEnemyTurns，意图必须对玩家可见。
- 新增剧情：按 schema 写 JSON；所有新人物先在设定集与异闻录登记。
