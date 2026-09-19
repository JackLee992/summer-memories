# ADR 0001：引擎选择 —— Unity 6 LTS

- 状态：**已采纳（锁定）**
- 日期：2026-09-20
- 决策者：项目所有者（经多轮确认：SDL3 自研 → Cocos Creator → Unity）

## 背景

项目目标为跨 Android / iOS / Nintendo 平台的叙事向游戏（ADV + 网格战棋 + 时间回溯），
并明确"为后期真正转型商业产品做技术积累"。评估过三条路线：

| 路线 | 优点 | 缺点 |
|---|---|---|
| C/C++ + SDL3 自研内核（参考 pal-mobile 多端壳） | 极致可控、包体小、已有 homebrew 经验 | 一切系统（UI、动画、资源、音频、工具链）从零造；战棋/ADV 内容生产效率极低；无商业主机通路 |
| Cocos Creator | 轻量、2D 友好、免费；任天堂门户提供 Switch 专属版 | 团队/行业人才与中间件生态弱于 Unity；Switch add-on 同样需任天堂授权；商业大型项目案例少 |
| **Unity 6 LTS** | 移动端成熟、2D/UI 与数据驱动工作流完善；**Switch/Switch 2 官方 add-on**；行业人才与教程多；CI/构建/热更方案齐备 | 主机需 Pro 订阅；引擎体积大；需注意 LTS 版本锁定 |

## 决策

采用 **Unity 6 LTS（6000.0.83f1）+ C#**，MVP 先交付 Android（ARM64、minSdk 26、横屏），
iOS 第二，Nintendo Switch 留待商业发行阶段。

## 理由

1. **Switch 商业发行路径最稳**：Unity 对 Switch/Switch 2 提供官方 add-on（需获批任天堂开发者 + Unity Pro），
   这是"未来真正转型商业产品"确定性最高的通路；SDL3 的 homebrew 路径不能用于商业上架。
2. **内容生产效率**：ADV 与战棋是内容密集型，Unity 的 uGUI/Resources/编辑器生态让 JSON 数据驱动、
   立绘背景替换、后续 Live2D/动画/音频中间件接入成本最低。
3. **技术积累可迁移**：Unity 在手游行业的人才与资料密度最高，团队积累的 C#、构建、CI 经验对商业化最有价值。
4. **macOS 可完成 MVP**：Android/iOS 构建在 macOS 无阻碍；Switch 构建机（Windows 10 Pro）问题可推迟到立项商业化时解决。

## 已知后果与约束

- Switch：必须成为获批任天堂开发者、订阅 Unity Pro（约 2200 美元/座/年，以官网现价为准）、
  准备 Windows 10 Pro（英/日文）构建机并通过 lotcheck。
- Unity 个人版在 CI 中的批量激活受限：仓库 CI 默认只做数据校验，Unity 构建走手动触发或自建授权 runner。
- 锁定 LTS 精确版本，不随意升级大版本；升级需另开 ADR 并回归全流程。
- 既有 GitHub 项目（pal-mobile / switch-toolchain 等）仅作**架构参考**，其 GPL/原生代码不得拷贝进本 MIT 工程。

## 参考

- 早期三路线对比详表：`../game-engine-eval/index.html`（结论已被本 ADR 取代，保留作背景资料）。
