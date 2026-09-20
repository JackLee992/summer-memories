#!/usr/bin/env bash
set -euo pipefail
SQUAD_REPO="$(cd "$(dirname "$0")/.." && pwd)"
SQUAD_APP="$SQUAD_REPO/Builds/SummerTimeSquad.app/Contents/MacOS/SummerMemories"
if [[ ! -x "$SQUAD_APP" ]]; then
  echo 'Run bash tools/build_squad_mac.sh first.' >&2
  exit 2
fi
"$SQUAD_APP" -batchmode -nographics --squad-smoke \
  --squad-report "$SQUAD_REPO/Builds/squad-smoke.json" \
  -logFile "$SQUAD_REPO/Builds/squad-smoke.log"
python3 - "$SQUAD_REPO/Builds/squad-smoke.json" <<'PY'
import json, sys
report = json.load(open(sys.argv[1]))
print(f"Squad smoke: {len(report['passed'])} passed; success={report['success']}")
if not report['success']:
    raise SystemExit(report['error'])
PY
