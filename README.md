# 夏日回忆 · 归墟来潮（Summer Memories）

> 时间回溯 × 网格策略战棋 × 电影式 ADV 的中国神话叙事游戏。
> 一个渔村少年在东海孤岛的七月半死去无数次——每死一次，潮水把他送回登岛那天。
> **题材原创**：取材《山海经》《西游记》《列子》《庄子》与东海民俗，讲我们自己的、关于生死与爱情的故事。

- **引擎**：Unity 6 LTS（`6000.0.83f1`）/ C# / uGUI
- **平台路线**：Android（MVP，进行中）→ iOS → Nintendo Switch（商业发行阶段）
- **当前版本**：v0.1 MVP（序章完整可玩：标题 → ADV 序章 → 首次死亡/回潮教学 → 海堤战 → 周目时间线）

## 玩法特色

| 系统 | 说明 |
|---|---|
| **外层 · 回潮** | 死亡后时间退回七月初一码头；《山海异闻》情报跨周目继承，记忆与情感则随回潮磨损 |
| **内层 · 救命毫毛** | 每场战斗 3 次快照回溯，为自己的失误反悔 |
| **网格战棋** | 曼哈顿网格、BFS 寻路、明确的敌方意图预告；赢在信息差与走位，而非数值 |
| **火眼金睛** | 每回合一次，提前照破敌人的移动格与袭杀目标；探索阶段用于识破假扮熟人的"罔两" |
| **电影式 ADV** | 数据驱动的对白/旁白/选项/分支标志/回潮指令，打字机、历史、跳过、存档 |

## 快速开始

1. 安装 **Unity Hub** 与 **Unity 6000.0.83f1**（勾选 Android Build Support，含 OpenJDK / Android SDK / NDK）。详见 [docs/environment-setup.md](docs/environment-setup.md)。
2. Hub 中 `Open → Add project from disk` 选择本仓库根目录。
3. 编辑器内：
   - 直接按 **Play** 即可运行（游戏通过 `[RuntimeInitializeOnLoadMethod]` 自举，无需打开场景）；
   - 或菜单 **夏日回忆 → Build → Android APK (ARM64)** 出包；
   - 菜单 **夏日回忆 → Build → 生成 Boot 场景** 可在 `Assets/Scenes/Boot.unity` 落一个可提交的调试场景。
4. 命令行出包：

```bash
/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath . \
  -executeMethod SummerMemories.Editor.BuildScript.BuildAndroid -logFile -
# 产物：Builds/summer-memories-v0.1-android.apk
```

## 工程结构

```
Assets/
  Scripts/
    Core/      事件总线、日志、程序化 UI、存档、本地化、外置内容（无依赖）
    ADV/       ADV 数据模型、导演器、视图（打字机/选项/历史/回潮指令）
    Battle/    网格、战斗导演器、敌方 AI 与意图、救命毫毛快照、战斗视图
    Loop/      《山海异闻》跨周目系统
    App/       自举启动、全局流程、标题页、周目时间线、教学对话框
  Editor/      BuildScript（一键/命令行构建）
  Resources/
    Story/     ADV 脚本 JSON（prologue、loop_teaching）
    Battles/   战棋关卡 JSON（battle_01）
    Tips/      《山海异闻》条目 JSON
    Localization/ 文案 JSON
    Art/       Bg（1920×1080）、Portraits（立绘）、Icons
ProjectSettings/  Packages/
docs/          GDD、设定集、架构、版权、环境、决策记录、路线图
tools/         content_manifest.py（素材 SHA-256 清单）
```

## 文档

- [游戏设计文档 GDD](docs/GDD.md) —— 玩法、剧情四支柱（动人/惊奇/探险/猎奇）、章节大纲、多结局
- [美术与剧情设定集](docs/art-and-story-bible.md)
- [架构说明](docs/architecture.md)
- [版权与素材合规](docs/licensing-and-copyright.md)（**重要**：公版素材边界与第三方独创内容红线）
- [环境搭建](docs/environment-setup.md)
- [路线图](docs/roadmap.md)
- [决策记录](docs/decisions/0001-engine-unity.md)

## 素材与版权声明

代码以 MIT 许可发布；剧情文本与美术资源的版权见 [版权文档](docs/licensing-and-copyright.md)。
当前美术为 AI 生成的**原创原型素材**（不含任何受版权保护角色的直接描摹），**仅用于玩法验证，商业化前将全部替换为签约美术资产**。本项目不使用、不包含任何同人 ROM、提取素材或未授权第三方资源。

## 协作（含 AI 协作者）

开始改动前请先阅读 [AGENTS.md](AGENTS.md) 中的工程契约。
