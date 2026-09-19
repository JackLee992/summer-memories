#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
content_manifest.py —— 仓库内 Resources 素材的 SHA-256 清单工具。

用途：
  --write   扫描 Assets/Resources，生成 tools/content-manifest.json
  --check   重新扫描并与清单比对（缺失/新增/哈希不一致则非零退出）

CI 在每次提交上运行 --check，防止素材被静默替换或损坏。
外置内容（persistentDataPath/ImportedContent）的校验在运行时由
ExternalContentLocator 用同样的 SHA-256 规则完成，不在本脚本范围。
"""
import argparse
import hashlib
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCAN_DIR = os.path.join(ROOT, "Assets", "Resources")
MANIFEST_PATH = os.path.join(ROOT, "tools", "content-manifest.json")


def sha256_of(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def scan():
    entries = {}
    for dirpath, _, filenames in os.walk(SCAN_DIR):
        for name in sorted(filenames):
            if name.endswith(".meta"):
                continue
            full = os.path.join(dirpath, name)
            rel = os.path.relpath(full, ROOT).replace(os.sep, "/")
            entries[rel] = {"sha256": sha256_of(full), "bytes": os.path.getsize(full)}
    return dict(sorted(entries.items()))


def write():
    entries = scan()
    manifest = {"algorithm": "sha256", "root": "Assets/Resources", "files": entries}
    with open(MANIFEST_PATH, "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2, sort_keys=True)
        f.write("\n")
    print(f"wrote {len(entries)} entries -> {os.path.relpath(MANIFEST_PATH, ROOT)}")


def check():
    if not os.path.exists(MANIFEST_PATH):
        print("manifest not found, run: python3 tools/content_manifest.py --write")
        return 2
    with open(MANIFEST_PATH, encoding="utf-8") as f:
        expected = json.load(f)["files"]
    actual = scan()
    problems = []
    for rel, meta in expected.items():
        if rel not in actual:
            problems.append(f"MISSING  {rel}")
        elif actual[rel]["sha256"] != meta["sha256"]:
            problems.append(f"CHANGED  {rel}")
    for rel in actual:
        if rel not in expected:
            problems.append(f"ADDED    {rel} (run --write)")
    if problems:
        print("\n".join(problems))
        print(f"\n{len(problems)} problem(s).")
        return 1
    print(f"OK: {len(actual)} files match manifest.")
    return 0


def main():
    ap = argparse.ArgumentParser()
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--write", action="store_true")
    g.add_argument("--check", action="store_true")
    args = ap.parse_args()
    if args.write:
        write()
        sys.exit(0)
    sys.exit(check())


if __name__ == "__main__":
    main()
