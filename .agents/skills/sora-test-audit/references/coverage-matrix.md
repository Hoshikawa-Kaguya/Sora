# Sora Test Coverage Matrix

## Source to Unit Tests

| Source area | Expected tests |
|---|---|
| `Sora.Core` types and enums | `tests/Sora.Tests/Unit/Core/` |
| `Sora.Entities` events, message body, waiter, segments, info | `tests/Sora.Tests/Unit/Entities/` or the established matching file |
| `Sora.Command` manager, scanning, matching, re-entry | `tests/Sora.Tests/Unit/Command/` |
| Milky converters and mapping | `tests/Sora.Tests/Unit/Milky/` |
| OneBot11 converters and mapping | `tests/Sora.Tests/Unit/OneBot11/` |

## Functional Mirrors

The functional layout mirrors protocol surfaces:

```text
tests/Sora.Tests/Functional/Milky/{ApiTests,EventTests,MessageTypeTests,CommandTests}.cs
tests/Sora.Tests/Functional/OneBot11/{ApiTests,EventTests,MessageTypeTests,CommandTests}.cs
```

Every cross-protocol `IBotApi` method, event, and sendable segment should have both protocol tests where supported. OB11 `NotSupported` behavior is still tested as graceful failure. Protocol-specific capabilities are the exception.

## Required Change Checks

- `IBotApi` or adapter API changes: verify API tests in both protocols.
- Event or event converter changes: verify converter unit tests and functional event tests.
- Segment, message body, or message converter changes: verify converter unit tests and message type tests.
- Command changes: follow the package ownership and matcher contract in [`src/AGENTS.md`](../../../../src/AGENTS.md). Cover `Sora.Command.InternalEntities` through command behavior tests; `RegisterMatcher` is private, so external custom-matcher registration is not a required coverage scenario.
- New source classes: verify a matching unit test file or document why coverage is not applicable.

## Functional API Groups

The functional API matrix is grouped as follows: identity (`GetSelfInfoAsync`, `GetImplInfoAsync`, `GetCookiesAsync`, `GetCsrfTokenAsync`); messaging (`SendGroupMessageAsync`, `SendPrivateMessageAsync`, recall, get message/history/forward, mark read); friend (`GetUserInfoAsync`, profile, friend list/info/requests, request handling, delete friend, friend nudge); group (`GetGroupInfoAsync`, list, members, notifications, name/card/title/admin/kick/leave/mute, group request handling); extended group (`invitation`, avatar, nudge, announcements, essence); file (`group/private listing, download, folders, rename/move, upload); profile (`avatar`, bio, nickname, resource URL, custom face URLs); reactions (`profile like`, group message reaction); and Milky peer pin extensions.

OB11 `NotSupported` cases remain testable behavior: the test should verify a clear failure result rather than omit the case. The functional test catalog in [`docs/FUNCTIONAL-TEST-CATALOG.md`](../../../../docs/FUNCTIONAL-TEST-CATALOG.md) is the current scenario-level source of truth when this reference becomes stale.

## Event and Segment Groups

Automatable event coverage includes message receive/delete, group and friend nudge where supported, file upload, group reaction, group admin/essence/mute/name changes, and Milky peer pin changes. Destructive friend/member/request setup scenarios remain converter/unit coverage when they cannot be safely automated.

Message type coverage includes text and special characters, face, mention, mention-all, reply, image, audio/video where supported, forward, light-app, and multi-segment messages. Round-trip tests should validate meaningful received fields, not only a send status.
