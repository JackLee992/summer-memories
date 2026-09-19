# 环境搭建与构建指南

## 1. 版本要求

| 项 | 版本/要求 |
|---|---|
| Unity | **6000.0.83f1**（Unity 6 LTS；见 `ProjectSettings/ProjectVersion.txt`） |
| 渲染管线 | Built-in（MVP 不依赖 URP/HDRP） |
| 目标平台（MVP） | Android，minSdk 26（Android 8.0），ARM64，横屏 |
| 主机环境 | macOS（Android/iOS 可构建）；**Switch 商业包必须 Windows 10 Pro（英文/日文）构建机** |

## 2. 安装 Unity（macOS）

1. 下载安装 **Unity Hub**（unity.com 下载），用 Unity 账号登录（Personal 许可即可开发 Android/iOS）。
2. `Installs → Install Editor → 6000.0.83f1`，模块勾选：
   - **Android Build Support**（展开子项全选）：
     - OpenJDK（Unity 自带 JDK 17）
     - Android SDK & NDK Tools（NDK r23b）
   - **iOS Build Support**（如需出 iOS，另需 Xcode）
3. `Open → Add project from disk` 选择本仓库根目录（含 `Assets`、`ProjectSettings`），首次打开等待导入与 Library 生成。

> 若 Hub 列表没有 6000.0.83f1，可用 `Unity Download Archive` 链接定位该精确版本；大版本同为 6000.0 LTS 时一般可兼容，但以 ProjectVersion 为准。

## 3. 编辑器内运行与出包

- **直接 Play**：游戏通过 `GameBootstrap` 的 `[RuntimeInitializeOnLoadMethod]` 自举，无需打开任何场景，按 Play 即从标题页启动。
- **生成可提交的调试场景**（可选）：菜单 `夏日回忆 → Build → 在编辑器中生成 Boot 场景`，产物 `Assets/Scenes/Boot.unity`。
- **出 APK**：菜单 `夏日回忆 → Build → Android APK (ARM64)`，产物：
  `Builds/summer-memories-v0.1-android.apk`（Development 版，debug 签名）。
- PlayerSettings 已由 BuildScript 自动设置：包名 `com.summermemories.study`、横屏、minSdk 26、ARM64。

## 4. 命令行构建（CI / 自动化）

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -quit -projectPath "$PWD" \
  -executeMethod SummerMemories.Editor.BuildScript.BuildAndroid \
  -logFile -
```

成功标志：日志出现 `构建结果：Succeeded`，`Builds/` 下生成 APK。

安装到已连接设备：

```bash
adb install -r Builds/summer-memories-v0.1-android.apk
adb logcat -s Unity   # 看运行日志
```

## 5. 数据校验（无需 Unity）

```bash
# JSON 合法性
find Assets/Resources -name '*.json' -exec python3 -m json.tool {} \; >/dev/null
# 素材清单（生成/校验 SHA-256）
python3 tools/content_manifest.py --write
python3 tools/content_manifest.py --check
```

## 6. iOS（第二阶段，概要）

- 安装 Xcode；Unity Build Settings 切 iOS 出 Xcode 工程。
- 需要 Apple Developer Program（上架）；自用可用免费 Apple ID 七天签名（AltStore 类侧载不在正式路线内）。
- 横屏、ARM64；存档路径与 Android 一致走 `Application.persistentDataPath`。

## 7. Nintendo Switch（商业发行阶段，不在 MVP）

事实门槛（2026 年资料）：

1. 先成为**获批的任天堂开发者**（Nintendo Developer Portal，签 NDA 与发行协议）；
2. Unity 对 Switch/Switch 2 以**官方 add-on** 提供，仅对获批开发者开放；
3. 主机构建与许可需要 **Unity Pro** 订阅；
4. 构建机必须 **Windows 10 Pro（英文/日文版）**，macOS 无法出 Switch 商业包；
5. 通过 lotcheck 合规（性能、存档、手柄、UI 规范、分级）。

Cocos 等引擎同样需要任天堂授权，"免费"仅指 add-on 本身。结论：先用 Android/iOS 验证玩法与商业数据，再以公司主体申请主机资质。

## 8. 常见问题

- **按 Play 黑屏/无 UI**：确认 Console 无编译错误；本项目 UI 全部代码生成，不依赖场景中的 Canvas。
- **改了 JSON 不生效**：Resources 下数据在编辑器 Play 时即时读取；出包后才固化，重新构建即可。
- **Android 构建报 SDK/NDK 缺失**：Hub 中给该编辑器补装 Android 模块，或在 Preferences 指向本机 SDK/NDK。
- **图片显示为纯色块**：`ArtRegistry` 找不到资源 key 时的占位；检查 `Resources/Art/**` 路径与文件名（不含扩展名）。
