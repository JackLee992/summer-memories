#!/usr/bin/env bash
set -euo pipefail
SQUAD_REPO="$(cd "$(dirname "$0")/.." && pwd)"
SQUAD_UNITY="$HOME/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity"
mkdir -p "$SQUAD_REPO/Builds"
"$SQUAD_UNITY" -batchmode -quit -projectPath "$SQUAD_REPO" -buildTarget OSXUniversal \
  -executeMethod SummerMemories.Editor.BuildScript.BuildMac25D -logFile "$SQUAD_REPO/Builds/build-25d-mac.log"
