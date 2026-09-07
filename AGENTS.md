# Sora Agent Guide

Sora is an asynchronous C#/.NET 10 multi-protocol IM bot framework. Milky is the primary protocol; OneBot v11 is retained as a deprecated compatibility adapter. NuGet package names use the `HoshikawaKaguya.` prefix while C# namespaces remain under `Sora.*`.

## Start Here

- Read this file for repository-wide rules and commands.
- For `src/**`, read [`src/AGENTS.md`](src/AGENTS.md).
- For `tests/**`, read [`tests/AGENTS.md`](tests/AGENTS.md).
- For protocol conformance work, use [`.agents/skills/sora-protocol-audit/SKILL.md`](.agents/skills/sora-protocol-audit/SKILL.md).
- For test coverage work, use [`.agents/skills/sora-test-audit/SKILL.md`](.agents/skills/sora-test-audit/SKILL.md).
- The audit CLI is documented in [`tools/agent-audit/README.md`](tools/agent-audit/README.md).

## Project Layout

```text
Sora.Core              IDs, enums, result types, shared primitives
Sora.Entities          events, segments, info models, IBotApi, dispatch and waiting
Sora.Command           command attributes, matchers, manager and command filters
Sora                   facade and service factory; no adapter references
Sora.Adapter.Milky     Milky HTTP API and SSE/WebSocket/WebHook adapter
Sora.Adapter.OneBot11  OneBot v11 adapter and adapter-specific models/events
```

Adapters depend on the facade and expose factory extensions such as `CreateMilkyService`. Third-party adapter registration is documented in [`docs/ADAPTER-DEVELOPMENT.md`](docs/ADAPTER-DEVELOPMENT.md).

`Sora.Command` is an independent command-extension package. Its package-local entities belong in `Sora.Command.InternalEntities`; command model ownership and internal matcher registration rules are defined in [`src/AGENTS.md`](src/AGENTS.md).

## Build and Test

```powershell
dotnet build Sora.slnx --configuration Release
dotnet test Sora.slnx --filter "Category=Unit" --no-build
pwsh tests/scripts/Run-Tests.ps1 -Category Unit
pwsh tests/scripts/Run-Tests.ps1 -Category All
```

Functional tests require `SORA_TEST_*` configuration and may skip only for missing environment preconditions. See [`docs/TESTING.md`](docs/TESTING.md) for the complete runner and environment reference.

## Repository Rules

- Preserve the dependency direction above and keep cross-protocol APIs protocol-agnostic.
- Milky-supported or shared models belong in the framework projects. OB11-only models belong under `Sora.Adapter.OneBot11`.
- Keep public APIs XML-documented and maintain the repository's zero-warning build policy.
- Use `ValueTask` for asynchronous framework/event paths, file-scoped namespaces, explicit types, and the existing Newtonsoft.Json/Mapster conventions.
- Do not add package `<Version>` values; versions are managed by Nerdbank.GitVersioning and each source project owns its `version.json`.
- Do not run Git write operations (`add`, `commit`, `push`, `stash`, reset, or checkout) as part of an implementation. Review and Git operations remain with the developer.
- Preserve protocol placeholder parameters and prefer graceful fallbacks over unnecessary exceptions.
- Do not copy credentials, local test settings, account IDs, or machine-specific absolute paths into tracked files.

## Documentation Routing

Read the closest relevant document instead of loading the entire repository. The main references are [`docs/TESTING.md`](docs/TESTING.md), [`docs/LOGGING.md`](docs/LOGGING.md), [`docs/ADAPTER-DEVELOPMENT.md`](docs/ADAPTER-DEVELOPMENT.md), and [`docs/FUNCTIONAL-TEST-CATALOG.md`](docs/FUNCTIONAL-TEST-CATALOG.md).
