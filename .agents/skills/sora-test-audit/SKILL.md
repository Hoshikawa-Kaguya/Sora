---
name: sora-test-audit
description: Audit Sora test coverage and functional-test design after source changes. Use for test audit, test coverage, checking tests, adding tests, or when source changes need verification.
---

# Sora Test Coverage Audit

Use the portable CLI to inspect changes, inventory tests, and identify coverage gaps. Read [`references/coverage-matrix.md`](references/coverage-matrix.md) and [`references/test-design-rules.md`](references/test-design-rules.md) when the change touches a mapped subsystem.

## Workflow

1. Run `node tools/agent-audit/test-audit.mjs changed-files --scope all_uncommitted`.
2. Run `inventory --category all --scan-antipatterns` or target the affected protocol/file.
3. Run `coverage-gaps` for the changed source files.
4. Check the coverage matrix and OB11 deprecation policy.
5. Add or update tests only when the user requests implementation. Build with zero warnings and run the unit suite.

## Commands

```text
node tools/agent-audit/test-audit.mjs changed-files [--scope staged|working|branch|commit|all_uncommitted] [--ref <name>] [--src-only]
node tools/agent-audit/test-audit.mjs inventory [--category unit|functional|all] [--protocol milky|ob11|both] [--file <name>] [--scan-antipatterns]
node tools/agent-audit/test-audit.mjs coverage-gaps [--files <path> ...] [--scope ...]
```

Use `--format json` for automation. The CLI is read-only and does not generate tests or perform Git writes.

