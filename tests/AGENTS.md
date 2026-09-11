# Test Agent Guide

These rules apply to all files under `tests/` and extend the repository rules in the root `AGENTS.md`.

## Framework and Organization

- Use xUnit v3 with `[Trait("Category", "Unit")]` or `[Trait("Category", "Functional")]`.
- Keep unit tests grouped by module and use the existing collection fixtures. Use `#region` blocks for feature groups.
- Each test file should focus on one tested class or one protocol test surface. Add XML docs to test classes and methods using `<see cref>` or `<inheritdoc />`.
- Framework unit tests must remain protocol-agnostic. Maintained adapter coverage targets Milky; retained OB11 tests are historical and are not a maintenance or validation target.

## Reliability Rules

- Unit test failures are code defects and must not be masked.
- Functional tests may skip only for missing environment preconditions (`SORA_TEST_*`, optional media, or missing secondary bot). Protocol failures and event delivery timeouts must fail the test.
- Wait for event tasks with `Task.WhenAny`, assert `IsCompletedSuccessfully`, and only then access `.Result`. Never use `.Result` without a preceding completion guard.
- Every API test must validate both success status and response data. Prefer `AssertSuccess<T>()` for `ApiResult<T>` extraction; never use `result.Data!`.
- State-changing tests restore original state in `finally` blocks. Event tests validate meaningful event fields, not only receipt.
- Do not create fake pass-throughs or log-only timeout branches.

## Functional Test Architecture

The maintained functional suite covers Milky. The primary bot performs API actions and the secondary bot triggers or listens for events when the protocol does not echo events to the actor. Correlate waiters with the expected content and source so unrelated messages cannot satisfy assertions.

Use the existing `tests/scripts/Run-Tests.ps1` runner. Functional configuration is supplied through `SORA_TEST_*` variables. Media files are read locally and sent as `base64://` URIs; do not commit local paths or credentials.

## Coverage Expectations

Source changes should have corresponding framework or Milky unit coverage. Changes to `IBotApi`, events, segments, message converters, or event converters require checking the Milky functional suite where applicable. Do not add OB11 parity, coverage-gap, compatibility or real-account validation tasks.


## Runner and Report Contracts

- Milky primary and secondary ports are independent: SORA_TEST_MILKY_PRIMARY_PORT and SORA_TEST_MILKY_SECONDARY_PORT, both defaulting to 3010. Use the corresponding runner parameters; there is no shared-port fallback.
- Category/protocol and caller filters are grouped before combining with AND. Verify selection offline before account tests, especially when the caller filter contains OR.
- ALL TESTS PASSED means all selected tests passed; zero selection returns success. Report actual execution/skip counts without interpreting this as whole-protocol coverage.
- TestReporter primarily saves local reports. Optional group delivery is best effort and its API result is intentionally ignored.
- Lifecycle tests use isolated ephemeral loopback endpoints; real account tests follow the user-approved scope and frequency, without automatic repeated triggering.
