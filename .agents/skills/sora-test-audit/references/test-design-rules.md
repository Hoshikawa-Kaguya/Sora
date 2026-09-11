# Functional Test Design Rules

- Unit failures are defects. Functional failures must identify protocol or environment causes.
- `Assert.SkipWhen` is for environment preconditions only, except the documented cross-protocol contamination guard. Never skip a protocol timeout.
- Event waits use `Task.WhenAny`, assert completion, then read the result. Timeout is failure.
- API tests verify status and data using `AssertSuccess<T>()` or safe property patterns.
- State-changing tests restore original state in `finally` blocks and use unique markers when events may be stale.
- Media is read locally and sent as `base64://`; do not use machine-specific `file://` paths for remote protocol endpoints.
- Primary/secondary bot roles are explicit. The secondary bot triggers external-user events, listens when the actor is not notified, supplies private-chat targets, validates messages, and continues dialog flows.
- Correlate received messages with the expected content and source rather than treating unrelated messages as successful replies.
- Preserve collection isolation and required mapping initialization when simplifying fixtures. Only remove statistics without consumers; TRX and runner process durations are actual report inputs.
- Runner success concerns selected tests; empty selection is normal. Group delivery from TestReporter is intentionally best effort after the local report is saved.
