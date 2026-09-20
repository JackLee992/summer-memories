#!/usr/bin/env bash
# build_android.sh —— 一键出 Android APK（Development，debug 签名，ARM64）
#
# 前提：
#   1. 已安装 Unity 6000.0.83f1 + Android Build Support（含 SDK/NDK/OpenJDK）
#   2. 已在 Unity Hub 登录 Unity 账号激活 Personal 许可（一次即可）
#
# 用法：
#   bash tools/build_android.sh
set -euo pipefail

VERSION="6000.0.83f1"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# 常见安装位置（Hub 默认、用户目录手动解包、独立安装）
CANDIDATES=(
  "/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity"
  "$HOME/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity"
  "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
)

UNITY_BIN=""
for c in "${CANDIDATES[@]}"; do
  if [ -x "$c" ]; then UNITY_BIN="$c"; break; fi
done

if [ -z "$UNITY_BIN" ]; then
  echo "未找到 Unity $VERSION。请通过 Unity Hub 安装（勾选 Android Build Support），"
  echo "或见 docs/environment-setup.md §9 的手动安装方式。" >&2
  exit 2
fi

echo "使用 Unity：$UNITY_BIN"
cd "$PROJECT_DIR"

"$UNITY_BIN" \
  -batchmode -quit \
  -projectPath "$PROJECT_DIR" \
  -executeMethod SummerMemories.Editor.BuildScript.BuildAndroid \
  -logFile "$PROJECT_DIR/Builds/build-android.log"

RESULT=$?
if [ $RESULT -ne 0 ]; then
  echo "构建失败（exit $RESULT），日志末尾：" >&2
  tail -n 60 "$PROJECT_DIR/Builds/build-android.log" >&2 || true
  exit $RESULT
fi

APK="$PROJECT_DIR/Builds/summer-memories-v0.1-android.apk"
if [ -f "$APK" ]; then
  echo "✅ APK 已生成：$APK"
  ls -lh "$APK"
  echo "安装：adb install -r \"$APK\""
else
  echo "⚠️ Unity 退出码为 0 但未找到 APK，请查看 Builds/build-android.log" >&2
  exit 1
fi
