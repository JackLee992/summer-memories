#!/usr/bin/env bash
# run_sim.sh —— headless 验证首战可通关（束搜索找胜利路径并复盘战报）
# 不启动 Unity、不需要 license；仅用编辑器自带 .NET 6 + Roslyn 编译纯逻辑层后运行。
set -euo pipefail

VERSION="${1:-}"
UNITY_ROOT="$HOME/Unity/Hub/Editor"
if [ -z "$VERSION" ]; then
  VERSION="$(ls -1 "$UNITY_ROOT" 2>/dev/null | sort -V | tail -1)"
fi
U="$UNITY_ROOT/$VERSION/Unity.app/Contents"
DOTNET="$U/NetCoreRuntime/dotnet"; CSC="$U/DotNetSdkRoslyn/csc.dll"
TPL="$(ls -d "$U/Resources/PackageManager/ProjectTemplates/libcache"/com.unity.template.2d-cross-platform-*/ScriptAssemblies 2>/dev/null | sort -V | tail -1)"

REPO="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${TMPDIR:-/tmp}/battle_sim"; rm -rf "$OUT"; mkdir -p "$OUT"
cd "$REPO"

REFS=()
for f in "$U/NetCoreRuntime/shared/Microsoft.NETCore.App/"*/*.dll; do REFS+=("-r:$f"); done
REFS+=("-r:$U/Managed/UnityEngine/UnityEngine.dll")
for f in "$U/Managed/UnityEngine/UnityEngine."*Module.dll; do REFS+=("-r:$f"); done
REFS+=("-r:$TPL/UnityEngine.UI.dll")

SRC="$(find Assets/Scripts/Core -name '*.cs') \
  Assets/Scripts/Battle/BattleModels.cs Assets/Scripts/Battle/Grid.cs \
  Assets/Scripts/Battle/RewindSystem.cs Assets/Scripts/Battle/BattleDirector.cs \
  tools/battle_sim/BattleSim.cs"

# shellcheck disable=SC2086
"$DOTNET" exec "$CSC" -nologo -target:exe -nostdlib -noconfig -langversion:9.0 \
  -nowarn:CS1701,CS1702 \
  "${REFS[@]}" $SRC -out:"$OUT/BattleSim.dll"

cd "$REPO"
cat > "$OUT/BattleSim.runtimeconfig.json" <<JSON
{
  "runtimeOptions": {
    "tfm": "net6.0",
    "framework": { "name": "Microsoft.NETCore.App", "version": "6.0.21" },
    "rollForwardOnNoCandidateFx": 2
  }
}
JSON
"$DOTNET" exec "$OUT/BattleSim.dll"
