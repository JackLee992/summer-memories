#!/usr/bin/env bash
set -euo pipefail
SIDE_REPO="$(cd "$(dirname "$0")/.." && pwd)"
"$HOME/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity" -batchmode -quit -projectPath "$SIDE_REPO" -buildTarget OSXUniversal -executeMethod SummerMemories.Editor.BuildScript.BuildMacSide2D -logFile "$SIDE_REPO/Builds/build-side-mac.log"
