#!/usr/bin/env python3
"""Fetch open SonarQube issues for bosdat-v2 and output compact JSON.

Reads SONARQUBE_TOKEN from the repo-root .env file automatically.

Usage:
    python .claude/skills/sonarqube/scripts/fetch-open-issues.py

Optional env vars (override .env values):
    SONARQUBE_TOKEN  SonarCloud token (required, auto-loaded from .env)
    SEVERITIES       Comma-separated severities (default: MAJOR,CRITICAL,BLOCKER)
                     Valid: INFO, MINOR, MAJOR, CRITICAL, BLOCKER
    PAGE_SIZE        Results per page, max 500 (default: 500)

Output: JSON array of { key, rule, severity, message, component, line }
"""

import json
import os
import sys
import urllib.request
import urllib.parse
from base64 import b64encode
from pathlib import Path

SONAR_HOST = "https://sonarcloud.io"
PROJECT = "FoggyFreek_bosdat-v2"


def load_env(env_path: Path) -> None:
    """Load key=value pairs from a .env file into os.environ (no overwrite)."""
    if not env_path.is_file():
        return
    with open(env_path) as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            key, _, value = line.partition("=")
            os.environ.setdefault(key.strip(), value.strip())


def main() -> None:
    # Auto-load .env from repo root (4 levels up from this script)
    repo_root = Path(__file__).resolve().parents[4]
    load_env(repo_root / ".env")

    token = os.environ.get("SONARQUBE_TOKEN")
    if not token:
        print("Error: SONARQUBE_TOKEN not set (checked .env and environment)", file=sys.stderr)
        sys.exit(1)

    severities = os.environ.get("SEVERITIES", "LOW,MEDIUM,HIGH,BLOCKER")
    page_size = os.environ.get("PAGE_SIZE", "500")

    params = urllib.parse.urlencode({
        "componentKeys": PROJECT,
        "s": "SEVERITY",
        "issueStatuses": "OPEN",
        "impactSeverities": severities,
        "ps": page_size,
        "p":1
    })

    url = f"{SONAR_HOST}/api/issues/search?{params}"
    credentials = b64encode(f"{token}:".encode()).decode()

    req = urllib.request.Request(url, headers={
        "Authorization": f"Basic {credentials}",
    })

    try:
        with urllib.request.urlopen(req) as resp:
            data = json.loads(resp.read())
    except urllib.error.HTTPError as e:
        body = e.read().decode(errors="replace")
        print(f"Error: HTTP {e.code} - {e.reason}\n{body}", file=sys.stderr)
        sys.exit(1)

    issues = [
        {
            "rule": issue["rule"],
            "severity": issue["severity"],
            "impactSeverity": issue["impacts"],
            "message": issue["message"],
            "component": issue["component"],
            "line": issue.get("line"),
        }
        for issue in data.get("issues", [])
    ]

    print(json.dumps(issues, indent=2))


if __name__ == "__main__":
    main()
