#!/usr/bin/env bash
# Mac (Apple Silicon) 3D 动作切片一键构建。
# 产物：Builds/SummerMemories3D.app（构建期临时定义 SM_ACTION3D，双击即进入 3D 海堤夜战）。
set -euo pipefail

VERSION="${1:-6000.0.83f1}"
UNITY="$HOME/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity"
REPO="$(cd "$(dirname "$0")/.." && pwd)"
LOG="$REPO/Builds/build-mac.log"
mkdir -p "$REPO/Builds"
cd "$REPO"

echo "[build-mac] Unity=$UNITY"
"$UNITY" -batchmode -quit -projectPath "$REPO" \
  -buildTarget OSXUniversal \
  -executeMethod SummerMemories.Editor.BuildScript.BuildMac3D \
  -logFile "$LOG"
echo "[build-mac] 完成，见 $LOG"
