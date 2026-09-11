# Protocol Audit Context

## Reference Ownership

LuckyLilliaBot is the authoritative implementation reference for Milky behavior. The maintained Sora framework and Milky adapter are the implementation under audit. Protocol documentation can explain design intent, but source code determines field and wire behavior.

## Sora Layers

```text
Sora.Core             IDs, enums, result types
Sora.Entities         events, segments, info models, IBotApi, dispatch/waiting
Sora.Command          command attributes, matchers, manager and filters
Sora                  service facade and factory
Sora.Adapter.Milky    HTTP API plus SSE/WebSocket/WebHook conversion
Sora.Adapter.OneBot11 Deprecated, unmaintained implementation retained in the tree
```

## Audit Mapping

- New events: check event definitions, the Milky event converter, dispatcher reachability, and event tests.
- New API actions: check `IBotApi` or the appropriate adapter extension, implementation, result mapping, and functional tests.
- New segments: check `SegmentType`, segment direction, the Milky message converter, validation, and message type tests.
- Changed fields: check wire DTOs, Mapster configuration, entity ownership, and converter tests.
- Behavior changes: check the adapter converter/API path and regression tests.

## Severity

- Critical: events are lost, APIs fail, or data is corrupted.
- Important: common functionality is missing or incomplete.
- Moderate: optional fields, edge cases, or cosmetic differences.
