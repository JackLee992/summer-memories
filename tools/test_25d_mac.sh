#!/usr/bin/env bash
set -euo pipefail
SQUAD_REPO="$(cd "$(dirname "$0")/.." && pwd)"
"$SQUAD_REPO/Builds/SummerTime25D.app/Contents/MacOS/SummerMemories" -batchmode -nographics --squad25d --squad-smoke \
  --squad-report "$SQUAD_REPO/Builds/25d-smoke.json" -logFile "$SQUAD_REPO/Builds/25d-smoke.log"
python3 - "$SQUAD_REPO/Builds/25d-smoke.json" <<'PY'
import json,sys
report=json.load(open(sys.argv[1]))
print(f"2.5D smoke: {len(report['passed'])} passed; success={report['success']}")
if not report['success']:raise SystemExit(report['error'])
PY
