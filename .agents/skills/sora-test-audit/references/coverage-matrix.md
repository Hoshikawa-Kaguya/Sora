# Sora Test Coverage Matrix

## Source to Unit Tests

| Source area | Expected tests |
|---|---|
| `Sora.Core` types and enums | `tests/Sora.Tests/Unit/Core/` |
| `Sora.Entities` events, message body, waiter, segments, info | `tests/Sora.Tests/Unit/Entities/` or the established matching file |
| `Sora.Command` manager, scanning, matching, re-entry | `tests/Sora.Tests/Unit/Command/` |
| Milky converters and mapping | `tests/Sora.Tests/Unit/Milky/` |

## Functional Coverage

The maintained functional suite covers Milky:

```text
tests/Sora.Tests/Functional/Milky/{ApiTests,EventTests,MessageTypeTests,CommandTests}.cs
```

Every supported `IBotApi` method, event, and sendable segment should have appropriate framework and Milky coverage. OneBot11 is deprecated and unmaintained; retained tests are historical, with no parity or coverage-expansion requirement.

## Required Change Checks

- `IBotApi` or Milky adapter API changes: verify Milky API tests.
- Event or event converter changes: verify converter unit tests and functional event tests.
- Segment, message body, or message converter changes: verify converter unit tests and message type tests.
- Command changes: follow the package ownership and matcher contract in [`src/AGENTS.md`](../../../../src/AGENTS.md). Cover `Sora.Command.InternalEntities` through command behavior tests; matchers are fixed internally, so external custom-matcher registration is not a required coverage scenario.
- New source behavior: cover meaningful core/boundary behavior in the appropriate suite; do not require a dedicated test file for every data type.
- User policies: verify triggering-user-versus-target identity from existing event fields, blocked events before automatic reads/waiters, and SuperUserOnly combined with member permissions. Unknown triggering users remain unknown; protocol-specific identities are resolved internally by the adapter.
- Connection lifetimes: use isolated loopback endpoints to verify nonblocking startup, owned cancellation, positive-interval retries and zero-interval single attempts. Parser tests alone do not prove network lifecycle behavior.
- Waiters: invalid timeout must not reserve a source; same-source registration and all completion paths have one winner. Preserve connection/source isolation and prefer controlled interleavings over timing guesses.

## Functional API Groups

The functional API matrix is grouped as follows: identity (`GetSelfInfoAsync`, `GetImplInfoAsync`, `GetCookiesAsync`, `GetCsrfTokenAsync`); messaging (`SendGroupMessageAsync`, `SendPrivateMessageAsync`, recall, get message/history/forward, mark read); friend (`GetUserInfoAsync`, profile, friend list/info/requests, request handling, delete friend, friend nudge); group (`GetGroupInfoAsync`, list, members, notifications, name/card/title/admin/kick/leave/mute, group request handling); extended group (`invitation`, avatar, nudge, announcements, essence); file (`group/private listing, download, folders, rename/move, upload); profile (`avatar`, bio, nickname, resource URL, custom face URLs); reactions (`profile like`, group message reaction); and Milky peer pin extensions.

The maintained Milky section of [`docs/FUNCTIONAL-TEST-CATALOG.md`](../../../../docs/FUNCTIONAL-TEST-CATALOG.md) is the scenario-level source of truth when this reference becomes stale.

## Event and Segment Groups

Automatable event coverage includes message receive/delete, group and friend nudge where supported, file upload, group reaction, group admin/essence/mute/name changes, and Milky peer pin changes. Destructive friend/member/request setup scenarios remain converter/unit coverage when they cannot be safely automated.

Message type coverage includes text and special characters, face, mention, mention-all, reply, image, audio/video where supported, forward, light-app, and multi-segment messages. Round-trip tests should validate meaningful received fields, not only a send status.
