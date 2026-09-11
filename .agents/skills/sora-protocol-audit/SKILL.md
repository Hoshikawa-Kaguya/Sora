---
name: sora-protocol-audit
description: Audit Sora Milky protocol conformance against the authoritative LuckyLilliaBot source. Use for protocol audit, protocol conformance, LLBot updates, protocol comparison, or synchronization checks.
---

# Sora Protocol Conformance Audit

Compare the maintained Sora Milky implementation with the LuckyLilliaBot reference. Read [`references/protocol-context.md`](references/protocol-context.md) before classifying findings and [`references/known-gaps.md`](references/known-gaps.md) for the maintenance boundary.

## Preconditions

The reference repository is supplied with `--llbot-path` or `SORA_LLBOT_PATH`, or is auto-detected beside this repository. Use the portable CLI from [`tools/agent-audit/README.md`](../../../tools/agent-audit/README.md); do not rely on Copilot SDK tools or a machine-specific absolute path. Local `inventory` needs no reference checkout and supports both file and directory areas; it is not evidence of external protocol conformance.

## Workflow

1. Run `node tools/agent-audit/protocol-audit.mjs recent-changes --since "2 weeks ago" --path-filter src/milky`.
2. Inspect relevant LLBot definitions with `definitions --area ...`.
3. Inspect Sora ownership, converters, interfaces, and models with `inventory --area ...`.
4. Use `compare` for a focused file-to-file check.
5. Exclude OB11 from findings and implementation plans: it is deprecated and unmaintained, including adapter-only fixes, protocol synchronization and validation.
6. Report Critical, Important, and Moderate findings with file paths, evidence, ownership, and a minimal implementation plan. Do not implement or perform Git writes during an audit unless the user separately requests implementation.

## Commands

```text
node tools/agent-audit/protocol-audit.mjs recent-changes [--since <date-or-revision>] [--path-filter <path>] [--show-diff]
node tools/agent-audit/protocol-audit.mjs definitions --area <area> [--llbot-path <path>]
node tools/agent-audit/protocol-audit.mjs inventory --area <area> [--sora-path <path>]
node tools/agent-audit/protocol-audit.mjs compare --llbot-file <path> --sora-file <path> [--pattern <regex>]
```

Use `--format json` for automation. Paths are validated as repository-relative paths. Required reference files fail when absent; inventory represents absent optional paths explicitly. An unavailable reference limits the conclusion to local source evidence.

Apply current framework contracts from `src/AGENTS.md`: actor-based user policies, nonblocking Milky connection lifetimes, zero-interval reconnect semantics and preserved error messages. OB11 debug URLs intentionally retain query authentication tokens; their presence alone is not a requested defect. Do not expand an implementation task to unrelated wire changes.
