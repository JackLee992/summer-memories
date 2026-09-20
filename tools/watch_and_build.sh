#!/bin/bash
# 轮询 Unity Personal license（*.ulf）。
# 用户在 Unity Hub 完成「登录 + 获取免费 Personal 许可」后，
# ~/Library/Application Support/Unity/ 下会出现 .ulf；
# 本脚本检测到后自动执行 Android 一键构建，无需人工再干预。
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$ROOT/Builds"
LOG="$ROOT/Builds/watch-build.log"
LICENSE_DIR="$HOME/Library/Application Support/Unity"

echo "[watch] $(date '+%F %T') 开始轮询 Unity license（最长 6 小时）..." | tee -a "$LOG"

for i in $(seq 1 360); do
  if compgen -G "$LICENSE_DIR"/*.ulf > /dev/null 2>&1; then
    echo "[watch] $(date '+%F %T') 检测到 license：$(ls "$LICENSE_DIR"/*.ulf)" | tee -a "$LOG"
    bash "$ROOT/tools/build_android.sh" >> "$LOG" 2>&1
    code=$?
    echo "[watch] $(date '+%F %T') build_android.sh 退出码=$code" | tee -a "$LOG"
    if [ "$code" -eq 0 ] && ls "$ROOT/Builds"/*.apk > /dev/null 2>&1; then
      echo "[watch] APK 已生成：$(ls -lh "$ROOT/Builds"/*.apk)" | tee -a "$LOG"
      touch "$ROOT/Builds/AUTO_BUILD_DONE"
    else
      touch "$ROOT/Builds/AUTO_BUILD_FAILED"
    fi
    exit "$code"
  fi
  sleep 60
done

echo "[watch] $(date '+%F %T') 超时仍未检测到 license，退出。" | tee -a "$LOG"
touch "$ROOT/Builds/AUTO_WATCH_TIMEOUT"
exit 1
