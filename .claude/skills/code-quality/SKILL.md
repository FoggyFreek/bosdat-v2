---
name: code-quality
description: "SonarQube code quality workflows: finding and triaging issues, checking quality gates, analyzing code snippets, reviewing metrics. Use when investigating code quality, and fixing SonarQube issues."
---

# SonarQube API and MCP

The SonarQube MCP server is already connected. All tools are available as `mcp__sonarqube__*`.
The SonarQube API is used to fetch issues

## Project context

- **Project key:** `FoggyFreek_bosdat-v2`
- **Current branch:** !`git branch --show-current`
- **Open PR (if any):** use `gh pr view --json number --jq .number` to find the PR number if needed
- **File key prefix:** `FoggyFreek_bosdat-v2:` + path from repo root

Use this context directly — do not call `search_my_sonarqube_projects` to discover the key.

## Supporting files

- [`scripts/fetch-open-issues.py`](scripts/fetch-open-issues.py) — Python script to fetch OPEN issues as compact JSON. Auto-loads `SONARQUBE_TOKEN` from the repo-root `.env` file. No external dependencies (stdlib only). Run this outside Claude and paste the output into the conversation to avoid filling context with resolved/historical issues.

---

## Tool Reference

### Finding Issues

**`search_sonar_issues_in_projects`** — primary tool for finding issues
- `projects`: filter to specific project keys (omit for all)
- `severities`: `INFO`, `LOW`, `MEDIUM`, `HIGH`, `BLOCKER`
- `branch`: branch name
- `pullRequestId`: PR number (string)
- `p` / `ps`: pagination (default page size 100, max 500)

> **Important:** The API returns all statuses (OPEN, CLOSED, RESOLVED, etc.). Always filter results to `status === "OPEN"` before presenting or acting on issues. Closed/resolved issues are historical — only OPEN issues require attention.

**`search_my_sonarqube_projects`** — discover available project keys
- Use when you don't know the project key yet

### Quality Gates

**`get_project_quality_gate_status`** — pass/fail status for a project
- Use `projectKey` (most common), or `analysisId`, `projectId`
- Add `branch` or `pullRequest` for non-main branches
- Returns: `OK`, `WARN`, or `ERROR` per condition

**`list_quality_gates`** — see all defined quality gate definitions

### Metrics

**`get_component_measures`** — retrieve specific metrics for a project
- `projectKey`: the project
- `metricKeys`: array e.g. `["coverage", "duplicated_lines_density", "violations", "complexity", "ncloc"]`
- `branch` / `pullRequest`: optional

**`search_metrics`** — discover all available metric keys

### Rules & Issues Detail

**`show_rule`** — get full explanation of a rule
- `key`: rule key from an issue (e.g. `java:S1135`, `typescript:S6544`)
- Returns: description, rationale, examples, remediation guidance

**`list_rule_repositories`** — list rule sets by language
- `language`: e.g. `java`, `ts`, `cs`, `py`

### Code Analysis

**`analyze_code_snippet`** — analyze code without a full project scan
- `codeSnippet`: the code to analyze
- `language`: optional, improves accuracy
- `projectKey`: optional, applies project-specific rules

---

## Common Workflows

### Investigate and triage issues

```
1. fetch issues with support script: python scripts/fetch-open-issues.py
2. show_rule(key) — understand what the rule is about
3. Fix most common issues 

```

## Key Metric Keys

| Metric | Key |
|--------|-----|
| Test coverage | `coverage` |
| Duplicated lines % | `duplicated_lines_density` |
| Total violations | `violations` |
| Cyclomatic complexity | `complexity` |
| Lines of code | `ncloc` |
| Technical debt ratio | `sqale_debt_ratio` |
| Reliability rating | `reliability_rating` |
| Security rating | `security_rating` |
| Maintainability rating | `sqale_rating` |
| New issues | `new_violations` |
| New coverage | `new_coverage` |

---

## Tips

- **File keys** use format `FoggyFreek_bosdat-v2:path/from/repo/root` — e.g. `FoggyFreek_bosdat-v2:src/BosDAT.API/Controllers/StudentsController.cs`
- **PR IDs are strings** in the API even though they look like numbers
- **`change_sonar_issue_status`** requires explicit user confirmation before calling — it modifies data in SonarQube
- **Severity `INFO`** is usually noise — start with `MEDIUM` and above when triaging
- **Quality gate status `WARN`** is not a failure — only `ERROR` blocks the gate
