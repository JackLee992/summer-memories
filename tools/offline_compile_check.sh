#!/usr/bin/env bash
# 离线编译检查（无需 Unity license / 无需打开编辑器）
#
# 用编辑器自带的 .NET 6 Roslyn (csc.dll) 直接编译全部六个 asmdef：
#   SummerMemories.Core / ADV / Battle / Loop / App / Editor
# 适合在 CI 或尚未激活 license 的机器上做静态编译验证。
#
# 用法: tools/offline_compile_check.sh [Unity版本号]
#   例: tools/offline_compile_check.sh 6000.0.83f1
# 不传版本号时自动探测 ~/Unity/Hub/Editor 下最新的编辑器。
set -euo pipefail

VERSION="${1:-}"
UNITY_ROOT="$HOME/Unity/Hub/Editor"
if [ -z "$VERSION" ]; then
  VERSION="$(ls -1 "$UNITY_ROOT" 2>/dev/null | sort -V | tail -1)"
fi
U="$UNITY_ROOT/$VERSION/Unity.app/Contents"
if [ ! -d "$U" ]; then
  echo "[offline-compile] 找不到 Unity 编辑器: $U" >&2
  exit 2
fi

DOTNET="$U/NetCoreRuntime/dotnet"
CSC="$U/DotNetSdkRoslyn/csc.dll"
TPL_ROOT="$U/Resources/PackageManager/ProjectTemplates/libcache"
# uGUI 在 Unity 6 是内置源码包，模板缓存里有同版本预编译 UnityEngine.UI.dll 可直接引用
TPL="$(ls -d "$TPL_ROOT"/com.unity.template.2d-cross-platform-*/ScriptAssemblies 2>/dev/null | sort -V | tail -1)"
if [ -z "$TPL" ] || [ ! -f "$TPL/UnityEngine.UI.dll" ]; then
  echo "[offline-compile] 找不到模板缓存中的 UnityEngine.UI.dll" >&2
  exit 2
fi

REPO="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${TMPDIR:-/tmp}/sm_compile"
rm -rf "$OUT"; mkdir -p "$OUT"

REFS=()
# .NET 6 BCL（netstandard2.1 超集，仅用于编译期检查；游戏目标仍是 netstandard2.1）
for f in "$U/NetCoreRuntime/shared/Microsoft.NETCore.App/"*/netstandard.dll \
         "$U/NetCoreRuntime/shared/Microsoft.NETCore.App/"*/System.Private.CoreLib.dll \
         "$U/NetCoreRuntime/shared/Microsoft.NETCore.App/"*/System.Runtime.dll; do
  [ -f "$f" ] && REFS+=("-r:$f")
done
for f in "$U/NetCoreRuntime/shared/Microsoft.NETCore.App/"*/System.*.dll; do
  [ -f "$f" ] && REFS+=("-r:$f")
done
# Unity 引擎：小体积 facade（程序集名 UnityEngine）+ 各模块实现 dll；
# 切勿引用 Managed/UnityEngine.dll（6.6MB 聚合实现，与模块类型重复，CS0433）
REFS+=("-r:$U/Managed/UnityEngine/UnityEngine.dll")
for f in "$U/Managed/UnityEngine/UnityEngine."*Module.dll; do REFS+=("-r:$f"); done
REFS+=("-r:$TPL/UnityEngine.UI.dll")

ERRORS=0
compile() {
  local name="$1"; shift
  echo "[offline-compile] $name <- $*"
  if ! "$DOTNET" exec "$CSC" -nologo -target:library -nostdlib -noconfig \
        -langversion:9.0 -nowarn:CS1701,CS1702 \
        "${REFS[@]}" "$@" -out:"$OUT/$name.dll" 2> "$OUT/$name.err"; then
    cat "$OUT/$name.err" >&2
    ERRORS=$((ERRORS + 1))
  fi
  # 警告也打印（CS1701/1702 程序集版本重定向噪音已屏蔽）
  grep -E "warning" "$OUT/$name.err" | grep -vE "CS1701|CS1702" || true
}

cd "$REPO"
compile SummerMemories.Core $(find Assets/Scripts/Core -name '*.cs')
REFS+=("-r:$OUT/SummerMemories.Core.dll")
compile SummerMemories.ADV  $(find Assets/Scripts/ADV -name '*.cs')
compile SummerMemories.Battle $(find Assets/Scripts/Battle -name '*.cs')
compile SummerMemories.Loop  $(find Assets/Scripts/Loop -name '*.cs')
REFS+=("-r:$OUT/SummerMemories.ADV.dll" "-r:$OUT/SummerMemories.Battle.dll" "-r:$OUT/SummerMemories.Loop.dll")
compile SummerMemories.App   $(find Assets/Scripts/App -name '*.cs')
# Editor 层：聚合 UnityEditor.dll（含全部 Editor 模块类型）
REFS+=("-r:$U/Managed/UnityEditor.dll" "-r:$TPL/UnityEditor.UI.dll" "-r:$OUT/SummerMemories.App.dll")
compile SummerMemories.Editor $(find Assets/Editor -name '*.cs')

if [ "$ERRORS" -ne 0 ]; then
  echo "[offline-compile] 失败：$ERRORS 个程序集编译错误" >&2
  exit 1
fi
echo "[offline-compile] 全部程序集编译通过 ($VERSION)"
ls -la "$OUT"/*.dll
