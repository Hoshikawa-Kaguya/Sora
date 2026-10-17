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
- `HoshikawaKaguya.Sora.Adapter.OneBot11` is supported only through Sora 2.2 and is excluded from subsequent builds and packaging. Its existing implementation and version files remain unchanged, but no feature, compatibility-fix, protocol-sync or validation work is planned for it. Shared framework and Milky changes do not require OB11 parity work. Deprecation is documented without changing NuGet deprecation status or adding compiler obsolescence attributes.
- `MessageBody` enforces segment direction for public mutation. Adapter input uses `FromIncoming()`; incoming-only fields use `internal init`.
- `MessageWaiter` is internal and is exposed through `MessageReceivedEvent` extension methods. Waiter-consumed events bypass pipeline filters.
- Event filters and command filters are opt-in and isolate ordinary callback exceptions; Sora-owned cancellation interrupts the pipeline. Event filter registration is startup-only; command filters are pinned at scan or dynamic registration time.
- `PipelineContext` is created by `SoraService` for normal event cycles and is shared by filters and handlers. It is not created for waiter-consumed events.

## Event and Command Pipeline

Normal event processing follows this order:

```text
adapter -> converter -> BotEvent -> actor policy -> MessageWaiter
  -> PipelineContext
  -> event pre-filters
  -> command matching/permissions/before-filters/handler/after-filters
  -> typed EventDispatcher
  -> event post-filters
```

The service identifies the triggering user from existing event fields before automatic reads, waiters and filters. Shared event identities are resolved internally by event type; OB11-specific identities are resolved inside its adapter through the internal event source. BlockUsers drops the entire user-triggered event; SuperUsers sets IsSuperUser. Unknown triggering users are not inferred from targets or peer IDs. Waiter matches then bypass all filters. Event pre-filters can short-circuit; post-filters run after normal completion or short-circuit and receive whether the earlier chain completed. Sora cancellation immediately terminates execution, including all remaining after/post filters. Event scope can restrict runtime event types, message source types, and a predicate; predicate failures are logged and treated as a scope mismatch. A filter implementing both event interfaces must be registered in both collections.

Command filters are attributes only, opt-in, sorted by ascending `Order`, and frozen when a command is scanned or dynamically registered. Class-level filters precede method-level filters on ties and are shared by every command in the group. Stateful filter attributes must be thread-safe. Ordinary filter exceptions are logged and treated as pass-through.

Filter and direct command/event handler boundaries classify an `OperationCanceledException` as Sora-owned cancellation only when its `CancellationToken` equals the token passed to the current operation and that token is canceled. Propagate that exception without error logging. Log and isolate other cancellation exceptions as ordinary callback failures; if the Sora token is also canceled, then issue a cancellation carrying the Sora token. Do not infer token ancestry or equate the service's linked token with the original `StartAsync` argument. Extensions that own a linked token source must translate Sora cancellation to the supplied token at their own boundary.

Keep execution orchestration as direct sequential calls. `ExecuteBeforeFiltersAsync`, `ExecuteAfterFiltersAsync`, and `ExecuteCommandHandlerAsync` handle callback exceptions internally. Event filter execution methods follow the same rule. Callers do not catch stage exceptions or retain pending failures. Sora cancellation propagates immediately; execution-chain completion is not guaranteed on cancellation or unexpected framework failures. Ordinary command failures are logged and returned to the caller only as metadata for after-filters.

Post-filters retain the original token and do not change the earlier chain's completion status. Command re-entry ownership is represented by a package-internal value-type scope used with `using`. Its `Dispose` only releases an acquired entry and never invokes filters or propagates cancellation. Keep the scope alive through context construction and command execution; do not copy or reuse it. Do not add outer exception guards, deferred exception propagation, or generic pipeline wrappers. Do not suppress CA2219.

`CommandManager` supports the built-in full, regex, and keyword matchers, static handlers, and singleton instances for instance handlers. Register external instances on the assembly registration before scanning with `AssemblyRegistration.RegisterCommandInstance<T>()`. Without a registered instance, a public/non-public parameterless constructor is used; otherwise GetUninitializedObject creates the supported singleton with zero-initialized fields and no constructor/field initializers. This is intentional support, not an error fallback to remove. State-dependent commands must use a constructor or preregister an initialized instance. Re-entry protection is keyed by method, connection, sender, group, and source type; it covers handlers waiting for follow-up messages.

Command registration and management follow [`docs/COMMANDS.md`](../docs/COMMANDS.md):

- All commands are registered through an `AssemblyRegistration` returned by `CommandManager.RegisterAssembly`; there is no manager-level scan or dynamic registration entry. Ownership is explicit: scanned commands belong to the scanned type (including inherited methods); dynamic commands require an `ownerType` whose `Assembly` is reference-equal to the registration's assembly. Do not infer ownership from delegate or method declaring types, and do not relax the check to referenced or base-class assemblies.
- `AssemblyRegistration.Registrar` exposes `ICommandRegistrar` through an internal implementation that is not the registration itself; keep management operations off that interface.
- Effective enablement is assembly local state AND owner type local state. Disabling never removes commands, instances, filter state or dynamic command IDs. Assembly and type state lives until the assembly is unregistered.
- Execution order is two-level: assemblies by descending priority then first-registration sequence; commands inside an assembly by descending priority then manager-wide registration sequence (scanned and dynamic mixed, no type priority). The immutable `ExecutionTable` of `AssemblySegment`s is rebuilt copy-on-write only when registrations, type states, priorities or enabled states change, under the registration lock. The message path reads it once with `Volatile.Read` and never sorts, copies or takes the registration lock. BlockAfterMatch remains a global stop.
- A scan call builds its batch first and publishes it in one step; failures publish nothing. Duplicate type scans and duplicate `RegisterAssembly` calls are skipped with a warning and never reset state.
- Matched commands enter an in-flight invocation on their registration before re-entry acquisition; a re-entry rejection exits it immediately. The same `CommandExecutionScope` releases both the re-entry key and the in-flight count with `using`; do not add try/catch or outer guards for it.
- `UnregisterAsync` removes the registration synchronously (later commands of in-flight messages are skipped by the liveness check), then awaits in-flight invocations; its token cancels only the wait. It does not dispose instances, track cross-assembly dependencies or wait for messages holding older tables.

`ICommandMatcher` implementations correspond one-to-one with the defined `MatchType` values and are stored in a fixed dictionary inside `CommandManager`. Adding a matching mode requires a framework change to both `MatchType` and its matcher; consumers cannot extend that enum. Do not expose matcher registration or require external custom-matcher registration tests.

CommandAttribute.SuperUserOnly and the dynamic superUserOnly option require IsSuperUser in addition to PermissionLevel; super users do not bypass group roles. BlockUsers takes precedence when a user appears in both lists.

Continuous commands use `MessageReceivedEvent.WaitForNextMessageAsync(...)` with a timeout, match patterns or predicates, and cancellation. Timeout is validated before source registration. A source has one atomic waiter; message, timeout, cancellation and disconnection compete for the same completion ownership. A null reply means timeout/cancellation; do not treat a protocol delivery failure as a successful event.

## Safety and Design

- Do not introduce an IOC/DI dependency or `dynamic`; use the existing factory and object pattern matching.
- Do not add adapter `InternalsVisibleTo` wildcards; register third-party adapters explicitly.
- Keep conversion and model ownership decisions explicit. If an autonomous change has multiple reasonable designs, leave a `TODO` describing the alternatives and use the conservative default.
- Summarize any genuinely necessary exception-throwing behavior after implementation.

## Adapter Lifetimes

Milky WS/SSE StartAsync schedules an owned connection task and returns without waiting for connection success. The task uses the linked lifetime token for connect/read/retry; Stop cancels and awaits it. ReconnectInterval=0 permits the initial connection only. Positive intervals retain automatic reconnect. OB11 uses ReconnectInterval for failed/lost-connection retry; heartbeat timeout remains separate. OB11 debug connection URLs intentionally include configured query authentication tokens; do not redact them as an unsolicited change.
