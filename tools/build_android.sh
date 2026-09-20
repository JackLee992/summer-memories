#!/usr/bin/env bash
# build_android.sh —— 一键出 Android APK（Development，debug 签名，ARM64）
#
# 前提：
#   1. 已安装 Unity 6000.0.83f1 + Android Build Support（SDK/NDK/OpenJDK 可由
#      docs/environment-setup.md §9 的手动方式预置到 AndroidPlayer 目录）
#   2. 已在 Unity Hub 登录 Unity 账号激活 Personal 许可（一次即可，免费）
#
# 用法：
#   bash tools/build_android.sh            # 自动探测 Unity 版本
#   UNITY_VERSION=6000.0.83f1 bash tools/build_android.sh
set -o pipefail

VERSION="${UNITY_VERSION:-6000.0.83f1}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# ---------- Unity 可执行文件探测 ----------
CANDIDATES=(
  "$HOME/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity"
  "/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity"
  "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
)
UNITY_BIN=""
for c in "${CANDIDATES[@]}"; do
  if [ -x "$c" ]; then UNITY_BIN="$c"; break; fi
done
if [ -z "$UNITY_BIN" ]; then
  # 退而求其次：取 Hub 下最新版本
  LATEST="$(ls -1 "$HOME/Unity/Hub/Editor" 2>/dev/null | sort -V | tail -1)"
  if [ -n "$LATEST" ] && [ -x "$HOME/Unity/Hub/Editor/$LATEST/Unity.app/Contents/MacOS/Unity" ]; then
    UNITY_BIN="$HOME/Unity/Hub/Editor/$LATEST/Unity.app/Contents/MacOS/Unity"
    echo "未找到 $VERSION，改用已安装的最新版本：$LATEST"
  fi
fi
if [ -z "$UNITY_BIN" ]; then
  echo "未找到 Unity 编辑器。请通过 Unity Hub 安装，或见 docs/environment-setup.md §9。" >&2
  exit 2
fi

# ---------- License 预检 ----------
# Unity 6 起 Personal 许可可能只以 Hub 登录态/在线 entitlement 存在，不再落地传统 *.ulf。
# 因此 *.ulf 缺失只告警、不阻断，由编辑器启动时自行向 LicensingClient 校验；
# 若确无许可，Unity 会以非零退出并在日志中报 licensing 错误。
UNITY_CFG="$HOME/Library/Application Support/Unity"
if ! ls "$UNITY_CFG"/*.ulf >/dev/null 2>&1; then
  echo "提示：未发现传统 *.ulf 许可文件。Unity 6 可使用 Hub 登录态的 Personal 许可，继续尝试；"
  echo "      若日志报 No valid license / 0 entitlements，请在 Unity Hub 完成 Personal 许可激活。"
fi

# ---------- 代理（中国大陆访问 Gradle/Unity 服务用；无代理时自动跳过） ----------
if [ -z "${HTTPS_PROXY:-}" ] && nc -z 127.0.0.1 7897 >/dev/null 2>&1; then
  export HTTPS_PROXY=http://127.0.0.1:7897
  export HTTP_PROXY=http://127.0.0.1:7897
  echo "检测到本地代理 127.0.0.1:7897，已为本次构建启用。"
fi

mkdir -p "$PROJECT_DIR/Builds"
LOG="$PROJECT_DIR/Builds/build-android.log"
echo "使用 Unity：$UNITY_BIN"
echo "日志：$LOG"
cd "$PROJECT_DIR"

set +e
"$UNITY_BIN" \
  -batchmode -quit \
  -projectPath "$PROJECT_DIR" \
  -executeMethod SummerMemories.Editor.BuildScript.BuildAndroid \
  -logFile "$LOG"
RESULT=$?
set -e

if [ $RESULT -ne 0 ]; then
  echo "构建失败（exit $RESULT），日志末尾：" >&2
  tail -n 80 "$LOG" >&2 || true
  exit $RESULT
fi

APK="$PROJECT_DIR/Builds/summer-memories-v0.1-android.apk"
if [ -f "$APK" ]; then
  echo "APK 已生成：$APK"
  ls -lh "$APK"
  ADB="$(ls "$HOME/Library/Android/sdk/platform-tools/adb" 2>/dev/null || true)"
  echo "安装：${ADB:-adb} install -r \"$APK\""
else
  echo "Unity 退出码为 0 但未找到 APK，请查看 $LOG" >&2
  exit 1
fi
