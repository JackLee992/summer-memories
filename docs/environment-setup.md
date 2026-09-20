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
# C# 编译检查（无需 Unity license / 不打开编辑器，用编辑器自带 Roslyn）
tools/offline_compile_check.sh
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

## 9. 中国大陆手动安装（Hub 下载失败时的实测路径）

Unity 6 的中国镜像（阿里云 OSS、unitychina CDN）对 6000.0.x 的 pkg 普遍 404，Hub CLI 对手动注册的编辑器装子模块会报 `TypeError: ... reading 'find'`。以下流程在 macOS arm64 + 本机 HTTP 代理（示例端口 7897）实测通过。

### 9.1 下载并解包编辑器（无需 sudo）

Unity 6 macOS arm64 的 pkg 文件名**没有 `-arm64` 后缀**；changeset 为 `dacc44548933`：

```bash
PX=http://127.0.0.1:7897   # 换成你的代理；无代理可直连 download.unity3d.com
mkdir -p /tmp/unity-pkgs && cd /tmp/unity-pkgs
curl -L -x $PX -O https://download.unity3d.com/download_unity/dacc44548933/MacEditorInstallerArm64/Unity-6000.0.83f1.pkg
curl -L -x $PX -O https://download.unity3d.com/download_unity/dacc44548933/MacEditorTargetInstaller/UnitySetup-Android-Support-for-Editor-6000.0.83f1.pkg

# 展开 pkg（不跑 installer，不需要 sudo）
pkgutil --expand Unity-6000.0.83f1.pkg editor_expanded
mkdir -p editor_payload && (cd editor_payload && cat ../editor_expanded/Payload | gunzip -dc | cpio -i)
pkgutil --expand UnitySetup-Android-Support-for-Editor-6000.0.83f1.pkg android_expanded
mkdir -p android_payload && (cd android_payload && cat ../android_expanded/Payload | gunzip -dc | cpio -i)

# 按 Hub 布局安放
mkdir -p "$HOME/Unity/Hub/Editor/6000.0.83f1"
mv editor_payload/Unity/Unity.app "$HOME/Unity/Hub/Editor/6000.0.83f1/Unity.app"
mv android_payload/AndroidPlayer "$HOME/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/PlaybackEngines/AndroidPlayer"
```

注册到 Hub：

```bash
arch -x86_64 "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless \
  install-path -s "$HOME/Unity/Hub/Editor"
arch -x86_64 "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless \
  editors --add "$HOME/Unity/Hub/Editor/6000.0.83f1"
```

> Android Support 的 pkg **不含** SDK/NDK/JDK（macOS ini 只有这一个条目），需按 9.2 预置，或在 license 激活后让编辑器首次构建时自动下载。

### 9.2 手动预置 Android SDK / NDK r23b / JDK 17

Unity 6000.0 要求：NDK **r23b（23.1.7779620）**、JDK **17**、SDK Platform **android-35**、Build-Tools **35.0.0**。归档均来自 Google 官方源与 Adoptium（走代理）：

```bash
D=~/android-deps; mkdir -p $D && cd $D
curl -L -x $PX -O https://dl.google.com/android/repository/android-ndk-r23b-darwin.zip
curl -L -x $PX -O https://dl.google.com/android/repository/commandlinetools-mac_arm64-16111833_latest.zip
curl -L -x $PX -O https://dl.google.com/android/repository/build-tools_r35_macosx.zip
curl -L -x $PX -O https://dl.google.com/android/repository/platform-35_r02.zip
curl -L -x $PX -O https://dl.google.com/android/repository/platform-tools_r37.0.1-darwin.zip
curl -L -x $PX -o jdk17.tar.gz "https://api.adoptium.net/v3/binary/latest/17/ga/mac/aarch64/jdk/hotspot/normal/eclipse"
```

按 Hub 布局解包到 `PlaybackEngines/AndroidPlayer/`：

| 目标目录 | 来源归档内容 |
|---|---|
| `AndroidPlayer/SDK/cmdline-tools/latest/` | cmdline-tools 包内的 `cmdline-tools/` |
| `AndroidPlayer/SDK/platform-tools/` | platform-tools 包内的 `platform-tools/` |
| `AndroidPlayer/SDK/platforms/android-35/` | platform 包内的 `android-35/` |
| `AndroidPlayer/SDK/build-tools/35.0.0/` | build-tools 包内的 `android-15/` |
| `AndroidPlayer/NDK/`（直接是 ndk-build 所在层） | ndk 包内的 `android-ndk-r23b/` 内容 |
| `AndroidPlayer/OpenJDK/Contents/Home/` | JDK tar 包内 `jdk-17*/Contents/` |

并在 `AndroidPlayer/SDK/licenses/` 写入 license hash（`android-sdk-license` 内容 `24333f8a63b6825ea9c5514f83c2829b004d1fee`）。
解包后执行一次 `xattr -dr com.apple.quarantine AndroidPlayer/{NDK,SDK,OpenJDK}` 避免 Gatekeeper 拦截。

`BuildScript.ConfigureAndroidTools()` 会自动探测上述 Hub 布局目录（也支持 `ANDROID_SDK_ROOT` / `ANDROID_NDK_ROOT` / `JAVA_HOME` 覆盖）。

### 9.3 激活 license（唯一必须人工的一步）

Personal 许可免费，但 **Hub 3.3.6 headless 没有 login 命令**，必须在 Hub GUI 浏览器 OAuth 登录一次：

1. 打开 Unity Hub → 头像 → Sign in，浏览器登录 Unity 账号（没有就注册，Personal 免费）；
2. 登录后 Hub 自动为本机编辑器激活许可，`~/Library/Application Support/Unity/` 下出现 `*.ulf`；
3. 之后 batchmode 出包不再需要任何交互。

### 9.4 一键出包

```bash
bash tools/build_android.sh        # 自动探测 Unity、预检 license、自动挂 7897 代理、出 APK
```

## 10. 免 license 的 CI 编译校验

`tools/offline_compile_check.sh` 用编辑器自带的 .NET 6 Roslyn（`NetCoreRuntime/dotnet exec DotNetSdkRoslyn/csc.dll`）按 asmdef 依赖顺序编译全部六个程序集，不需要 license、不启动编辑器。引用集要点（复现时注意）：

- BCL 用 `NetCoreRuntime/shared/Microsoft.NETCore.App/6.0.21/*.dll`（netstandard2.1 超集，仅编译期检查）；
- 引擎引用 `Managed/UnityEngine/UnityEngine.dll`（164KB 转发 facade）+ `Managed/UnityEngine/UnityEngine.*Module.dll`；
- **不要**引用 `Managed/UnityEngine.dll`（6.6MB 聚合实现，与模块类型重复，CS0433）；
- uGUI 引用模板缓存里同版本预编译的 `UnityEngine.UI.dll`（`Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.2d-cross-platform-*/ScriptAssemblies/`）；
- Editor 层引用聚合 `Managed/UnityEditor.dll`（不要同时引 `UnityEditor.*Module.dll`）。
