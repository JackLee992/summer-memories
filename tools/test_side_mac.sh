#!/usr/bin/env bash
set -euo pipefail
SIDE_REPO="$(cd "$(dirname "$0")/.." && pwd)"
"$SIDE_REPO/Builds/SummerTimeSide2D.app/Contents/MacOS/SummerMemories" -batchmode -nographics --side-smoke --side-report "$SIDE_REPO/Builds/side-smoke.json" -logFile "$SIDE_REPO/Builds/side-smoke.log" > "$SIDE_REPO/Builds/side-launch.log" 2>&1
python3 - "$SIDE_REPO/Builds/side-smoke.json" <<'PY'
import json,sys
r=json.load(open(sys.argv[1]));print('Side smoke:',len(r['passed']),'passed;',r['success'])
if not r['success']:raise SystemExit(r['error'])
PY
