# Source Agent Guide

These rules apply to all files under `src/` and extend the repository rules in the root `AGENTS.md`.

## C# Style

- Nullable is enabled. Use explicit types; do not use `var`.
- Use file-scoped namespaces, target-typed `new()` where the type is visible, collection expressions, LINQ method syntax, expression-bodied members for simple expressions, and `System.Threading.Lock` instead of `lock (object)`.
- Parameterized primary constructors are not used. Declare properties/fields in the body and initialize through constructors or object initializers; fixed-value base calls are the only primary-constructor exception.
- Use `ValueTask` for asynchronous framework and event methods. Accept and propagate `CancellationToken` through asynchronous pipeline methods.
- Add XML documentation to public and internal members. Use `<inheritdoc />` for interface implementations. Use `#region` blocks for established API/adapter groupings.
- Use Newtonsoft.Json attributes for wire models and Mapster `.Adapt<T>()` for DTO-to-entity mapping. Configure unmapped destination properties with `.Ignore()` and provide context-dependent values at the call site.

## Nullability and Conversion

`ApiResult<T>.Data` is nullable. Never use the null-forgiving operator on it. Extract successful reference data with a property pattern:

```csharp
if (response is not { IsSuccess: true, Data: { } data })
    return ApiResult<Entity>.Fail(response.Code, response.Message);
```

Conversion methods must attempt actual per-type conversion rather than silently filtering convertible values. `Segment.ToOutgoing()` is virtual; derived segments override it only when custom conversion is required and call the base implementation for the direction change.

## Framework Boundaries

- `IBotApi` and `BotEvent` expose unified typed parameters. Adapters translate protocol wire formats in converters.
- `Sora.Entities` models must be shared by both protocols or Milky-specific. OB11-only events, models, segments, and response fields stay in `Sora.Adapter.OneBot11`.
- `Sora.Command` owns its command-extension entities under `Sora.Command.InternalEntities`. Keep command-specific metadata and execution state in this package, not in `Sora.Entities`; directory placement and namespace declarations must agree. Namespace ownership does not determine C# accessibility: types exposed by public filter contracts still require public accessibility.
- OB11 is deprecated. An OB11-only feature that would require changes to `Sora.Core`, `Sora.Entities`, `Sora.Command`, or the facade is skipped unless Milky has equivalent support. Adapter-only compatibility fixes remain allowed.
- `MessageBody` enforces segment direction for public mutation. Adapter input uses `FromIncoming()`; incoming-only fields use `internal init`.
- `MessageWaiter` is internal and is exposed through `MessageReceivedEvent` extension methods. Waiter-consumed events bypass pipeline filters.
- Event filters and command filters are opt-in and must tolerate exceptions without crashing the event pipeline. Event filter registration is startup-only; command filters are pinned at scan or dynamic registration time.
- `PipelineContext` is created by `SoraService` for normal event cycles and is shared by filters and handlers. It is not created for waiter-consumed events.

## Event and Command Pipeline

Normal event processing follows this order:

```text
adapter -> converter -> BotEvent -> MessageWaiter
  -> PipelineContext
  -> event pre-filters
  -> command matching/permissions/before-filters/handler/after-filters
  -> typed EventDispatcher
  -> event post-filters in finally
```

Waiter matches have highest priority and bypass all filters. Event pre-filters can short-circuit; post-filters run unconditionally and receive whether the earlier chain completed. Event scope can restrict runtime event types, message source types, and a predicate; predicate failures are logged and treated as a scope mismatch. A filter implementing both event interfaces must be registered in both collections.

Command filters are attributes only, opt-in, sorted by ascending `Order`, and frozen when a command is scanned or dynamically registered. Class-level filters precede method-level filters on ties and are shared by every command in the group. Stateful filter attributes must be thread-safe. Filter exceptions are logged and treated as pass-through so user code is not taken down by a filter.

`CommandManager` supports the built-in full, regex, and keyword matchers, static handlers, and singleton instances for instance handlers. Register external instances before scanning with `RegisterCommandInstance<T>()`. Re-entry protection is keyed by method, connection, sender, group, and source type; it covers handlers waiting for follow-up messages.

`ICommandMatcher` implementations correspond one-to-one with the defined `MatchType` values. `RegisterMatcher` is a private implementation method of `CommandManager`, not a consumer extension point. Adding a matching mode requires a framework change to both `MatchType` and its matcher; consumers cannot extend that enum. Do not expose matcher registration or require external custom-matcher registration tests.

Continuous commands use `MessageReceivedEvent.WaitForNextMessageAsync(...)` with a timeout, match patterns or predicates, and cancellation. A null reply means timeout/cancellation; do not treat a protocol delivery failure as a successful event.

## Safety and Design

- Do not introduce an IOC/DI dependency or `dynamic`; use the existing factory and object pattern matching.
- Do not add adapter `InternalsVisibleTo` wildcards; register third-party adapters explicitly.
- Keep conversion and model ownership decisions explicit. If an autonomous change has multiple reasonable designs, leave a `TODO` describing the alternatives and use the conservative default.
- Summarize any genuinely necessary exception-throwing behavior after implementation.
