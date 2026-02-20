#!/usr/bin/env python3
"""
Fetch file-level coverage data from SonarQube, sorted by coverage ascending (lowest first).

Auto-loads SONARQUBE_TOKEN from the repo-root .env file.
No external dependencies (stdlib only).

Usage:
    python scripts/fetch-coverage-by-file.py [--min-lines N] [--top N]

Options:
    --min-lines N   Minimum lines to cover threshold (default: 10)
    --top N         Number of files to show (default: 30)
"""

import urllib.request
import urllib.parse
import json
import sys
import os
import argparse

# --- Config ---
PROJECT_KEY = "FoggyFreek_bosdat-v2"
BRANCH = "main"
SONARCLOUD_URL = "https://sonarcloud.io"

# Paths to skip (scripts, config, test infrastructure)
SKIP_PATHS = [
    ".claude/",
    "scripts/",
    "test/setup.ts",
    ".cjs",
    "Extensions/ServiceCollection",
]


def load_token():
    """Load SONARQUBE_TOKEN from .env file in repo root."""
    env_path = os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".env")
    env_path = os.path.normpath(env_path)
    if os.path.exists(env_path):
        with open(env_path) as f:
            for line in f:
                line = line.strip()
                if line.startswith("SONARQUBE_TOKEN="):
                    return line.split("=", 1)[1].strip()
    token = os.environ.get("SONARQUBE_TOKEN")
    if token:
        return token
    print("ERROR: SONARQUBE_TOKEN not found in .env or environment.", file=sys.stderr)
    sys.exit(1)


def fetch_component_tree(token, page_size=500):
    """Fetch all file-level coverage measures from SonarQube."""
    params = urllib.parse.urlencode({
        "component": PROJECT_KEY,
        "metricKeys": "coverage,uncovered_lines,lines_to_cover",
        "qualifiers": "FIL",
        "branch": BRANCH,
        "ps": page_size,
        "s": "metric",
        "asc": "true",
        "metricSort": "coverage",
    })
    url = f"{SONARCLOUD_URL}/api/measures/component_tree?{params}"

    import base64
    credentials = base64.b64encode(f"{token}:".encode()).decode()
    req = urllib.request.Request(url, headers={"Authorization": f"Basic {credentials}"})

    with urllib.request.urlopen(req) as resp:
        return json.load(resp)


def should_skip(path):
    return any(s in path for s in SKIP_PATHS)


def main():
    parser = argparse.ArgumentParser(description="Fetch file coverage from SonarQube")
    parser.add_argument("--min-lines", type=int, default=10, metavar="N",
                        help="Minimum lines to cover (default: 10)")
    parser.add_argument("--top", type=int, default=30, metavar="N",
                        help="Number of files to show (default: 30)")
    args = parser.parse_args()

    token = load_token()
    data = fetch_component_tree(token)

    components = data.get("components", [])
    results = []

    for c in components:
        path = c.get("path", "")
        if should_skip(path):
            continue
        measures = {m["metric"]: m.get("value") for m in c.get("measures", [])}
        lines = int(measures.get("lines_to_cover") or 0)
        if lines < args.min_lines:
            continue
        cov = float(measures.get("coverage") or 0)
        uncovered = int(measures.get("uncovered_lines") or 0)
        results.append((cov, uncovered, lines, path))

    results.sort(key=lambda x: x[0])

    # Summary header
    total_files = len(results)
    zero_cov = sum(1 for r in results if r[0] == 0.0)
    total_uncovered = sum(r[1] for r in results)

    print(f"Project: {PROJECT_KEY} (branch: {BRANCH})")
    print(f"Files with >={args.min_lines} coverable lines: {total_files}")
    print(f"Files with 0% coverage: {zero_cov}")
    print(f"Total uncovered lines (in scope): {total_uncovered}")
    print()
    print("%-8s  %-5s  %-5s  %s" % ("Coverage", "Uncov", "Total", "File"))
    print("-" * 100)

    for cov, unc, tot, path in results[:args.top]:
        print("%7.1f%%  %5d  %5d  %s" % (cov, unc, tot, path))

    if total_files > args.top:
        print(f"\n... and {total_files - args.top} more files. Use --top N to see more.")


if __name__ == "__main__":
    main()
