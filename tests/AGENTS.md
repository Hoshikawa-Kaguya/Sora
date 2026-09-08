# Test Agent Guide

These rules apply to all files under `tests/` and extend the repository rules in the root `AGENTS.md`.

## Framework and Organization

- Use xUnit v3 with `[Trait("Category", "Unit")]` or `[Trait("Category", "Functional")]`.
- Keep unit tests grouped by module and use the existing collection fixtures. Use `#region` blocks for feature groups.
- Each test file should focus on one tested class or one protocol test surface. Add XML docs to test classes and methods using `<see cref>` or `<inheritdoc />`.
- Framework unit tests must remain protocol-agnostic. OB11 adapter tests may cover adapter-internal converters and DTO mapping.

## Reliability Rules

- Unit test failures are code defects and must not be masked.
- Functional tests may skip only for missing environment preconditions (`SORA_TEST_*`, optional media, or missing secondary bot). Protocol failures and event delivery timeouts must fail the test.
- Wait for event tasks with `Task.WhenAny`, assert `IsCompletedSuccessfully`, and only then access `.Result`. Never use `.Result` without a preceding completion guard.
- Every API test must validate both success status and response data. Prefer `AssertSuccess<T>()` for `ApiResult<T>` extraction; never use `result.Data!`.
- State-changing tests restore original state in `finally` blocks. Event tests validate meaningful event fields, not only receipt.
- Do not create fake pass-throughs or log-only timeout branches.

## Functional Test Architecture

Milky and OneBot11 functional suites mirror each other unless a capability is protocol-specific. The primary bot performs API actions and the secondary bot triggers or listens for events when the protocol does not echo events to the actor. When both protocols run in one group, guard waiters against cross-protocol messages with an explicit content/protocol check.

Use the existing `tests/scripts/Run-Tests.ps1` runner. Functional configuration is supplied through `SORA_TEST_*` variables. Media files are read locally and sent as `base64://` URIs; do not commit local paths or credentials.

## Coverage Expectations

Source changes should have corresponding unit coverage. Changes to `IBotApi`, events, segments, message converters, or event converters require checking both protocol functional suites where applicable. OB11-only gaps that would require framework changes are skipped under the source guide's deprecation policy.

