---
name: sora-test-audit
description: Audit maintained Sora framework and Milky test coverage and functional-test design after source changes. Use for test audit, test coverage, checking tests, adding tests, or when source changes need verification.
---

# Sora Test Coverage Audit

Use the portable CLI to inspect changes, inventory tests, and identify coverage gaps. Read [`references/coverage-matrix.md`](references/coverage-matrix.md) and [`references/test-design-rules.md`](references/test-design-rules.md) when the change touches a mapped subsystem.

## Workflow

1. Establish the requested scope. For change reviews run `node tools/agent-audit/test-audit.mjs changed-files --scope all_uncommitted`; for a full engineering review inventory the tracked tree and keep a per-file ledger.
2. Run `inventory --category all --scan-antipatterns` or target the affected protocol/file.
3. Run `coverage-gaps` for the changed source files.
4. Check the framework and Milky coverage matrix. OB11 is deprecated and unmaintained; its retained tests and coverage gaps do not create maintenance or validation tasks.
5. Add or update tests only when the user requests implementation. Build with zero warnings and run the framework/Milky unit suite using `Category=Unit&FullyQualifiedName!~OneBot11`.

## Commands

```text
node tools/agent-audit/test-audit.mjs changed-files [--scope staged|working|branch|commit|all_uncommitted] [--ref <name>] [--src-only]
node tools/agent-audit/test-audit.mjs inventory [--category unit|functional|all] --protocol milky [--file <name>] [--scan-antipatterns]
node tools/agent-audit/test-audit.mjs coverage-gaps [--files <path> ...] [--scope ...]
```

Use `--format json` for automation. The CLI is read-only and does not generate tests or perform Git writes.

Read the current `src/AGENTS.md` and `tests/AGENTS.md` contracts before classifying a design as redundant. Preserve callback isolation, waiter completion ownership, actor policies and supported command-instance construction. Prefer behavioral regressions over automatic-property tests or implementation-mirroring matrices. A heuristic match is not a confirmed defect.

Verify runner selection offline when filters include OR. Zero selected tests return normally; report executed/skip counts separately. Reporter success concerns local report generation, not confirmed message delivery. Real account tests follow the authorized scope and pacing, without repeated retries to obtain a passing result.
