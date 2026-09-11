# Agent Audit CLI

They use only Node built-in modules and work from any Agent or terminal that can run Node 20+ and Git.

## Protocol Audit

Configure the LuckyLilliaBot reference repository with either:

```powershell
$env:SORA_LLBOT_PATH = "C:\path\to\LuckyLilliaBot"
```

or pass `--llbot-path` to a command. If neither is set, the tool checks common sibling repository layouts.

```powershell
node tools/agent-audit/protocol-audit.mjs recent-changes --since "2 weeks ago" --path-filter src/milky
node tools/agent-audit/protocol-audit.mjs definitions --area milky_apis
node tools/agent-audit/protocol-audit.mjs inventory --area api_methods
node tools/agent-audit/protocol-audit.mjs compare --llbot-file src/milky/common/event.ts --sora-file src/Sora.Adapter.Milky/Converter/EventConverter.cs --pattern group_mute
```

Run `node tools/agent-audit/protocol-audit.mjs --help` for supported areas and options.

`inventory` reads the local Sora checkout without requiring LuckyLilliaBot. It lists directory contents and reads individual files according to their filesystem type, including empty directories. Use its output to locate the current source before comparing protocol contracts; it does not determine conformance by itself.

## Test Audit

```powershell
node tools/agent-audit/test-audit.mjs changed-files --scope all_uncommitted
node tools/agent-audit/test-audit.mjs inventory --category functional --protocol milky
node tools/agent-audit/test-audit.mjs coverage-gaps --scope all_uncommitted
```

Run `node tools/agent-audit/test-audit.mjs --help` for all options. Add `--format json` to any command for machine-readable output.

The reports are heuristics for review, not proof of semantic coverage. A reported missing unit-test filename may be covered by an established aggregate test; verify the referenced source and tests before editing.

Maintenance and audit plans cover the framework and Milky. `HoshikawaKaguya.Sora.Adapter.OneBot11` is deprecated and unmaintained; retained OB11 CLI areas and historical tests do not create protocol-sync, compatibility-fix or coverage tasks. NuGet deprecation status is unchanged.

## Exit Codes

- `0`: the command ran successfully, even if the audit found gaps.
- `1`: invalid arguments, missing paths, unsafe relative paths, or an underlying Git/read failure.
