---
name: sora-protocol-audit
description: Audit Sora protocol conformance against the authoritative LuckyLilliaBot source. Use for protocol audit, protocol conformance, LLBot updates, protocol comparison, or synchronization checks.
---

# Sora Protocol Conformance Audit

Compare the current Sora implementation with the LuckyLilliaBot reference. Read [`references/protocol-context.md`](references/protocol-context.md) before classifying findings and [`references/known-gaps.md`](references/known-gaps.md) before reporting an intentional gap.

## Preconditions

The reference repository is supplied with `--llbot-path` or `SORA_LLBOT_PATH`, or is auto-detected beside this repository. Use the portable CLI from [`tools/agent-audit/README.md`](../../../tools/agent-audit/README.md); do not rely on Copilot SDK tools or a machine-specific absolute path.

## Workflow

1. Run `node tools/agent-audit/protocol-audit.mjs recent-changes --since "2 weeks ago"`.
2. Inspect relevant LLBot definitions with `definitions --area ...`.
3. Inspect Sora ownership, converters, interfaces, and models with `inventory --area ...`.
4. Use `compare` for a focused file-to-file check.
5. Apply the OB11 deprecation and known-gap rules before classifying a finding.
6. Report Critical, Important, and Moderate findings with file paths, evidence, ownership, and a minimal implementation plan. Do not implement or perform Git writes during an audit unless the user separately requests implementation.

## Commands

```text
node tools/agent-audit/protocol-audit.mjs recent-changes [--since <date-or-revision>] [--path-filter <path>] [--show-diff]
node tools/agent-audit/protocol-audit.mjs definitions --area <area> [--llbot-path <path>]
node tools/agent-audit/protocol-audit.mjs inventory --area <area> [--sora-path <path>]
node tools/agent-audit/protocol-audit.mjs compare --llbot-file <path> --sora-file <path> [--pattern <regex>]
```

Use `--format json` for automation. All paths are validated as repository-relative paths; missing files are reported as errors rather than silently ignored.

