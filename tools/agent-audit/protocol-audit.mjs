#!/usr/bin/env node

import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, isAbsolute, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const defaultSoraPath = resolve(scriptDirectory, "..", "..");

const definitionAreas = {
    ob11_segments: ["src/onebot11/types.ts"],
    ob11_actions: ["src/onebot11/action/types.ts"],
    ob11_events: ["src/onebot11/event/index.ts"],
    ob11_types: ["src/onebot11/types.ts", "src/onebot11/entities.ts"],
    milky_events: ["src/milky/common/event.ts"],
    milky_apis: ["src/milky/api/system.ts", "src/milky/api/message.ts", "src/milky/api/friend.ts", "src/milky/api/group.ts", "src/milky/api/file.ts"],
    milky_segments: ["src/milky/transform/message/incoming.ts", "src/milky/transform/message/outgoing.ts"],
    ntqqapi_types: ["src/ntqqapi/types/user.ts", "src/ntqqapi/types/group.ts", "src/ntqqapi/types/msg.ts", "src/ntqqapi/types/notify.ts", "src/ntqqapi/types/flashfile.ts"],
};

const inventoryAreas = {
    events: ["src/Sora.Entities/Events", "src/Sora.Adapter.OneBot11/Events"],
    segments: ["src/Sora.Entities/Segments", "src/Sora.Core/Enums/SegmentType.cs", "src/Sora.Core/Enums/FaceSubType.cs"],
    api_methods: ["src/Sora.Entities/Interfaces/IBotApi.cs", "src/Sora.Adapter.OneBot11/IOneBot11ExtApi.cs", "src/Sora.Adapter.Milky/IMilkyExtApi.cs"],
    ob11_converter_events: ["src/Sora.Adapter.OneBot11/Converter/EventConverter.cs"],
    ob11_converter_segments: ["src/Sora.Adapter.OneBot11/Converter/MessageConverter.cs"],
    milky_converter_events: ["src/Sora.Adapter.Milky/Converter/EventConverter.cs"],
    milky_converter_segments: ["src/Sora.Adapter.Milky/Converter/MessageConverter.cs"],
    ob11_models: ["src/Sora.Adapter.OneBot11/Models"],
    milky_models: ["src/Sora.Adapter.Milky/Models"],
    info_models: ["src/Sora.Entities/Info"],
    enums: ["src/Sora.Core/Enums"],
};

function normalizePath(value) {
    return value.replaceAll("\\", "/");
}

function parseArguments(argv) {
    const args = { _: [] };
    let index = 0;
    while (index < argv.length) {
        const token = argv[index];
        if (!token.startsWith("--")) {
            args._.push(token);
            index += 1;
            continue;
        }

        const name = token.slice(2);
        if (name === "show-diff" || name === "help") {
            args[name] = true;
            index += 1;
            continue;
        }

        const values = [];
        let cursor = index + 1;
        while (cursor < argv.length && !argv[cursor].startsWith("--")) {
            values.push(argv[cursor]);
            cursor += 1;
        }
        args[name] = values.length <= 1 ? values[0] : values;
        index = cursor;
    }
    return args;
}

function usage() {
    return `Usage:
  node tools/agent-audit/protocol-audit.mjs recent-changes [--since <date-or-revision>] [--path-filter <path>] [--show-diff]
  node tools/agent-audit/protocol-audit.mjs definitions --area <area> [--llbot-path <path>]
  node tools/agent-audit/protocol-audit.mjs inventory --area <area> [--sora-path <path>]
  node tools/agent-audit/protocol-audit.mjs compare --llbot-file <path> --sora-file <path> [--pattern <regex>]

Definitions areas: ${Object.keys(definitionAreas).join(", ")}
Inventory areas: ${Object.keys(inventoryAreas).join(", ")}
Global options: --format markdown|json`;
}

function fail(message) {
    throw new Error(message);
}

function pathInside(basePath, candidatePath, label) {
    const base = resolve(basePath);
    if (isAbsolute(candidatePath))
        fail(`${label} must be repository-relative: ${candidatePath}`);
    const candidate = resolve(base, candidatePath);
    const rel = relative(base, candidate);
    if (rel.startsWith("..") || isAbsolute(rel))
        fail(`${label} escapes its repository: ${candidatePath}`);
    return candidate;
}

function readRequired(basePath, relativePath, label) {
    const fullPath = pathInside(basePath, relativePath, label);
    if (!existsSync(fullPath))
        fail(`${label} does not exist: ${relativePath}`);
    return readFileSync(fullPath, "utf8");
}

function listFiles(basePath, relativePath) {
    const fullPath = pathInside(basePath, relativePath, "directory");
    if (!existsSync(fullPath))
        return [];
    const entries = readdirSync(fullPath, { withFileTypes: true });
    const result = [];
    for (const entry of entries) {
        const child = join(relativePath, entry.name);
        if (entry.isDirectory())
            result.push(...listFiles(basePath, child));
        else
            result.push(child);
    }
    return result.map(normalizePath).sort();
}

function git(args, cwd) {
    const result = spawnSync("git", ["--no-pager", ...args], {
        cwd,
        encoding: "utf8",
        maxBuffer: 8 * 1024 * 1024,
    });
    if (result.error)
        fail(`Unable to run git: ${result.error.message}`);
    if (result.status !== 0)
        fail(`git ${args.join(" ")} failed: ${(result.stderr || result.stdout || "unknown error").trim()}`);
    return (result.stdout || "").trim();
}

function resolveLlbotPath(args, soraPath) {
    const configured = args["llbot-path"] || process.env.SORA_LLBOT_PATH;
    const candidates = configured
        ? [configured]
        : [
            join(dirname(resolve(soraPath)), "LuckyLilliaBot"),
            join(resolve(soraPath), "..", "LuckyLilliaBot"),
            join(resolve(soraPath), "..", "..", "github", "LuckyLilliaBot"),
        ];
    for (const candidate of candidates) {
        const fullPath = resolve(candidate);
        if (existsSync(join(fullPath, ".git")) && existsSync(join(fullPath, "src")))
            return fullPath;
    }
    fail("LuckyLilliaBot repository was not found. Set SORA_LLBOT_PATH or pass --llbot-path <path>.");
}

function markdownSections(title, sections) {
    const body = sections.map((section) => `## ${section.title}\n\n${section.content || "(empty)"}`).join("\n\n---\n\n");
    return `# ${title}\n\n${body}`;
}

function recentChanges(args, soraPath) {
    const since = args.since || "2 weeks ago";
    const pathFilter = args["path-filter"];
    const llbotPath = resolveLlbotPath(args, soraPath);
    const isRevision = /^[0-9a-f]{7,40}$/i.test(since);
    const range = isRevision ? `${since}..HEAD` : `--since=${since}`;
    const suffix = pathFilter ? ["--", pathFilter] : [];
    const log = git(["log", "--oneline", "--stat", range, ...suffix], llbotPath);
    const sections = [{ title: `Git log (${since})${pathFilter ? ` [${pathFilter}]` : ""}`, content: log || "(no commits found)" }];
    if (args["show-diff"]) {
        const commits = git(["log", "--reverse", "--format=%H", range, ...suffix], llbotPath).split(/\r?\n/).filter(Boolean);
        if (commits.length > 0) {
            const diffRange = isRevision ? `${since}..HEAD` : `${commits[0]}^..HEAD`;
            const diff = git(["diff", diffRange, ...suffix], llbotPath);
            sections.push({ title: "Diff", content: diff || "(no diff)" });
        }
    }
    return { title: "Protocol Audit: Recent Changes", sections };
}

function definitions(args, soraPath) {
    const area = args.area;
    if (!definitionAreas[area])
        fail(`Unknown definitions area '${area}'. Valid values: ${Object.keys(definitionAreas).join(", ")}`);
    const llbotPath = resolveLlbotPath(args, soraPath);
    const sections = definitionAreas[area].map((file) => ({ title: file, content: readRequired(llbotPath, file, "LLBot file") }));
    return { title: `Protocol Audit: LLBot Definitions (${area})`, sections };
}

function inventory(args, soraPath) {
    const area = args.area;
    if (!inventoryAreas[area])
        fail(`Unknown inventory area '${area}'. Valid values: ${Object.keys(inventoryAreas).join(", ")}`);
    const sections = [];
    for (const item of inventoryAreas[area]) {
        const fullPath = pathInside(soraPath, item, "Sora path");
        if (!existsSync(fullPath)) {
            sections.push({ title: item, content: "(not found)" });
            continue;
        }
        const files = listFiles(soraPath, item);
        if (files.length > 0 && !item.endsWith(".cs")) {
            let content = files.join("\n");
            if (area === "info_models" || area === "enums") {
                const sourceFiles = files.filter((file) => file.endsWith(".cs"));
                const sourceContent = sourceFiles.map((file) => `--- ${file} ---\n${readFileSync(pathInside(soraPath, file, "Sora file"), "utf8")}`).join("\n\n");
                content = `${content}\n\n${sourceContent}`;
            }
            sections.push({ title: item, content });
        } else
            sections.push({ title: item, content: readFileSync(fullPath, "utf8") });
    }
    return { title: `Protocol Audit: Sora Inventory (${area})`, sections };
}

function findMatches(content, pattern) {
    let expression;
    try {
        expression = new RegExp(pattern);
    } catch (error) {
        fail(`Invalid search pattern '${pattern}': ${error.message}`);
    }
    return content.split(/\r?\n/).map((line, index) => ({ line: index + 1, text: line })).filter((entry) => expression.test(entry.text));
}

function compare(args, soraPath) {
    if (!args["llbot-file"] || !args["sora-file"])
        fail("compare requires --llbot-file <path> and --sora-file <path>");
    const llbotPath = resolveLlbotPath(args, soraPath);
    const llbotContent = readRequired(llbotPath, args["llbot-file"], "LLBot file");
    const soraContent = readRequired(soraPath, args["sora-file"], "Sora file");
    const sections = [
        { title: `LLBot: ${args["llbot-file"]}`, content: llbotContent },
        { title: `Sora: ${args["sora-file"]}`, content: soraContent },
    ];
    if (args.pattern) {
        const llMatches = findMatches(llbotContent, args.pattern).map((entry) => `${entry.line}: ${entry.text.trim()}`).join("\n");
        const soraMatches = findMatches(soraContent, args.pattern).map((entry) => `${entry.line}: ${entry.text.trim()}`).join("\n");
        sections.push({ title: `Matches for /${args.pattern}/`, content: `### LLBot\n${llMatches || "(none)"}\n\n### Sora\n${soraMatches || "(none)"}` });
    }
    return { title: "Protocol Audit: Cross Reference", sections };
}

function render(result, format) {
    if (format === "json")
        return JSON.stringify(result, null, 2);
    return markdownSections(result.title, result.sections);
}

function main() {
    const args = parseArguments(process.argv.slice(2));
    const command = args._[0];
    if (!command || command === "help" || args.help) {
        console.log(usage());
        return;
    }
    const soraPath = resolve(args["sora-path"] || defaultSoraPath);
    const handlers = { "recent-changes": recentChanges, definitions, inventory, compare };
    if (!handlers[command])
        fail(`Unknown command '${command}'.\n\n${usage()}`);
    const result = handlers[command](args, soraPath);
    console.log(render(result, args.format || "markdown"));
}

try {
    main();
} catch (error) {
    console.error(`protocol-audit: ${error.message}`);
    process.exitCode = 1;
}
