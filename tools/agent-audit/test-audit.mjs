#!/usr/bin/env node

import { existsSync, readFileSync, readdirSync } from "node:fs";
import { basename, dirname, isAbsolute, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const defaultSoraPath = resolve(scriptDirectory, "..", "..");

const projectLayers = [
    { pattern: "src/Sora.Core/", layer: "Core", unitTestDir: "Core" },
    { pattern: "src/Sora.Entities/", layer: "Entities", unitTestDir: "Entities" },
    { pattern: "src/Sora.Command/", layer: "Command", unitTestDir: "Command" },
    { pattern: "src/Sora.Adapter.Milky/", layer: "Milky Adapter", unitTestDir: "Milky" },
    { pattern: "src/Sora.Adapter.OneBot11/", layer: "OneBot11 Adapter", unitTestDir: "OneBot11" },
    { pattern: "src/Sora/", layer: "Facade", unitTestDir: null },
];

function parseArguments(argv) {
    const args = { _: [] };
    const booleanOptions = new Set(["src-only", "scan-antipatterns", "no-scan-antipatterns"]);
    let index = 0;
    while (index < argv.length) {
        const token = argv[index];
        if (!token.startsWith("--")) {
            args._.push(token);
            index += 1;
            continue;
        }
        const name = token.slice(2);
        if (booleanOptions.has(name)) {
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
  node tools/agent-audit/test-audit.mjs changed-files [--scope staged|working|branch|commit|all_uncommitted] [--ref <name>] [--src-only]
  node tools/agent-audit/test-audit.mjs inventory [--category unit|functional|all] [--protocol milky|ob11|both] [--file <name>] [--scan-antipatterns]
  node tools/agent-audit/test-audit.mjs coverage-gaps [--files <path> ...] [--scope staged|working|all_uncommitted]

Global options: --sora-path <path> --format markdown|json`;
}

function fail(message) {
    throw new Error(message);
}

function normalizePath(value) {
    return value.replaceAll("\\", "/");
}

function pathInside(basePath, candidatePath, label) {
    const base = resolve(basePath);
    if (isAbsolute(candidatePath))
        fail(`${label} must be repository-relative: ${candidatePath}`);
    const candidate = resolve(base, candidatePath);
    const rel = relative(base, candidate);
    if (rel.startsWith("..") || isAbsolute(rel))
        fail(`${label} escapes the repository: ${candidatePath}`);
    return candidate;
}

function git(args, cwd) {
    const result = spawnSync("git", ["--no-pager", ...args], {
        cwd,
        encoding: "utf8",
        maxBuffer: 4 * 1024 * 1024,
    });
    if (result.error)
        fail(`Unable to run git: ${result.error.message}`);
    if (result.status !== 0)
        fail(`git ${args.join(" ")} failed: ${(result.stderr || result.stdout || "unknown error").trim()}`);
    return (result.stdout || "").trim();
}

function classifyFile(filePath) {
    const normalized = normalizePath(filePath);
    return projectLayers.find((item) => normalized.includes(item.pattern)) || { pattern: "unknown", layer: "Unknown", unitTestDir: null };
}

function listCsFiles(rootPath, relativePath) {
    const fullPath = pathInside(rootPath, relativePath, "test directory");
    if (!existsSync(fullPath))
        return [];
    const result = [];
    for (const entry of readdirSync(fullPath, { withFileTypes: true })) {
        const child = join(relativePath, entry.name);
        if (entry.isDirectory())
            result.push(...listCsFiles(rootPath, child));
        else if (entry.name.endsWith(".cs"))
            result.push(normalizePath(child));
    }
    return result.sort();
}

function parseNameStatus(output) {
    if (!output)
        return [];
    return output.split(/\r?\n/).filter(Boolean).map((line) => {
        const parts = line.split("\t");
        const rawStatus = parts[0].trim();
        const filePath = normalizePath(parts.at(-1).trim());
        const type = rawStatus.startsWith("M") ? "Modified"
            : rawStatus.startsWith("A") ? "Added"
                : rawStatus.startsWith("D") ? "Deleted"
                    : rawStatus.startsWith("R") ? "Renamed"
                        : rawStatus;
        return { path: filePath, status: type, layer: classifyFile(filePath).layer };
    });
}

function changedFiles(args, soraPath) {
    const scope = args.scope || "all_uncommitted";
    let diffArgs;
    if (scope === "staged")
        diffArgs = ["diff", "--cached", "--name-status"];
    else if (scope === "working")
        diffArgs = ["diff", "--name-status"];
    else if (scope === "branch")
        diffArgs = ["diff", "--name-status", args.ref || "master", "HEAD"];
    else if (scope === "commit")
        diffArgs = ["diff", "--name-status", args.ref || "HEAD~1", "HEAD"];
    else if (scope === "all_uncommitted")
        diffArgs = ["diff", "--name-status", "HEAD"];
    else
        fail(`Unknown scope '${scope}'`);

    const entries = parseNameStatus(git(diffArgs, soraPath));
    if (scope === "all_uncommitted" || scope === "working") {
        const untracked = git(["ls-files", "--others", "--exclude-standard"], soraPath).split(/\r?\n/).filter(Boolean);
        for (const filePath of untracked)
            entries.push({ path: normalizePath(filePath), status: "Added", layer: classifyFile(filePath).layer });
    }

    const unique = [...new Map(entries.map((entry) => [entry.path, entry])).values()];
    const filtered = args["src-only"] ? unique.filter((entry) => entry.path.startsWith("src/")) : unique;
    return {
        scope,
        files: filtered,
        sourceFiles: filtered.filter((entry) => entry.path.startsWith("src/")),
        testFiles: filtered.filter((entry) => entry.path.startsWith("tests/")),
    };
}

function extractTestMethods(content) {
    const methods = [];
    const lines = content.split(/\r?\n/);
    for (let index = 0; index < lines.length; index += 1) {
        if (!/\[(?:Fact|Theory)(?:\(|\])/.test(lines[index]))
            continue;
        for (let cursor = index + 1; cursor < Math.min(index + 9, lines.length); cursor += 1) {
            const match = lines[cursor].match(/(?:public\s+)?(?:async\s+)?(?:Task|ValueTask|void)\s+(\w+)\s*\(/);
            if (!match)
                continue;
            let cref = null;
            for (let doc = Math.max(0, index - 5); doc < index; doc += 1) {
                const crefMatch = lines[doc].match(/see\s+cref="([^"]+)"/);
                if (crefMatch)
                    cref = crefMatch[1];
            }
            methods.push({ name: match[1], line: cursor + 1, cref });
            break;
        }
    }
    return methods;
}

function extractRegions(content) {
    return content.split(/\r?\n/).map((line, index) => {
        const match = line.match(/#region\s+(.+)/);
        return match ? { name: match[1].trim(), line: index + 1 } : null;
    }).filter(Boolean);
}

function scanAntiPatterns(content, filePath) {
    const issues = [];
    const lines = content.split(/\r?\n/);
    for (let index = 0; index < lines.length; index += 1) {
        const line = lines[index].trim();
        if (/Assert\.SkipWhen\(\s*!\w*\.?(?:Task\.)?IsCompletedSuccessfully/.test(line))
            issues.push({ file: filePath, line: index + 1, severity: "critical", pattern: "Skip on timeout", message: "Protocol/event timeouts must fail instead of skip." });
        if (/if\s*\(\s*\w*\.?(?:Task\.)?IsCompletedSuccessfully\s*\)/.test(line))
            issues.push({ file: filePath, line: index + 1, severity: "critical", pattern: "Conditional timeout assertion", message: "A conditional completion branch can silently pass after timeout." });
        if (/\.Task\.Result|\btcs\.Task\.Result/.test(line)) {
            const guarded = lines.slice(Math.max(0, index - 4), index).some((previous) => /Assert\.True.*IsCompletedSuccessfully|Assert\.NotNull.*Result/.test(previous));
            if (!guarded)
                issues.push({ file: filePath, line: index + 1, severity: "critical", pattern: "Unguarded Result", message: "Task result is read without a nearby successful-completion assertion." });
        }
        if (/Assert\.True\(\s*\w+\.IsSuccess\b/.test(line)) {
            const following = lines.slice(index + 1, index + 9);
            const hasDataAssert = following.some((next) => /Assert\.(?:Equal|NotNull|NotEmpty|Contains|False|InRange)/.test(next));
            const continuesScenario = following.some((next) => /Task\.WhenAny|Task\.Delay|await\s+.*Async\(/.test(next));
            if (!hasDataAssert && !continuesScenario)
                issues.push({ file: filePath, line: index + 1, severity: "warning", pattern: "Weak API assertion", message: "Success is asserted without validating response data." });
        }
    }
    return issues;
}

function inventory(args, soraPath) {
    const category = args.category || "all";
    const protocol = args.protocol || "both";
    if (!new Set(["unit", "functional", "all"]).has(category))
        fail(`Unknown category '${category}'`);
    if (!new Set(["milky", "ob11", "both"]).has(protocol))
        fail(`Unknown protocol '${protocol}'`);
    const directories = [];
    if (category === "unit" || category === "all") {
        for (const name of ["Adapters", "Command", "Core", "Entities", "Milky", "OneBot11"])
            directories.push({ path: `tests/Sora.Tests/Unit/${name}`, category: "Unit" });
    }
    if (category === "functional" || category === "all") {
        if (protocol === "milky" || protocol === "both")
            directories.push({ path: "tests/Sora.Tests/Functional/Milky", category: "Functional" });
        if (protocol === "ob11" || protocol === "both")
            directories.push({ path: "tests/Sora.Tests/Functional/OneBot11", category: "Functional" });
    }

    const scan = args["no-scan-antipatterns"] !== true;
    const files = [];
    const issues = [];
    for (const directory of directories) {
        for (const filePath of listCsFiles(soraPath, directory.path)) {
            if (args.file && basename(filePath) !== args.file)
                continue;
            if (/Fixture\.cs$|UnitTestFixtures\.cs$/.test(filePath))
                continue;
            const content = readFileSync(pathInside(soraPath, filePath, "test file"), "utf8");
            const methods = extractTestMethods(content);
            if (methods.length === 0)
                continue;
            const entry = { path: filePath, category: directory.category, methods, regions: extractRegions(content) };
            files.push(entry);
            if (scan && directory.category === "Functional")
                issues.push(...scanAntiPatterns(content, filePath));
        }
    }
    return { category, protocol, files, issues, testCount: files.reduce((sum, file) => sum + file.methods.length, 0) };
}

function hasNamedTest(soraPath, directory, testName) {
    return listCsFiles(soraPath, directory).some((file) => basename(file) === testName);
}

function coverageGaps(args, soraPath) {
    let sourcePaths;
    if (args.files) {
        const raw = Array.isArray(args.files) ? args.files : [args.files];
        sourcePaths = raw.flatMap((item) => item.split(",")).map(normalizePath).filter((file) => file.startsWith("src/") && file.endsWith(".cs"));
    } else {
        sourcePaths = changedFiles({ scope: args.scope || "all_uncommitted", ref: args.ref, "src-only": true }, soraPath).sourceFiles
            .map((entry) => entry.path).filter((file) => file.endsWith(".cs"));
    }

    const gaps = [];
    for (const filePath of sourcePaths) {
        const fileName = basename(filePath, ".cs");
        const layer = classifyFile(filePath);
        if (fileName !== "GlobalUsings" && !fileName.endsWith("Config") && !filePath.includes("/Models/") && layer.unitTestDir) {
            const unitDirectory = `tests/Sora.Tests/Unit/${layer.unitTestDir}`;
            if (!hasNamedTest(soraPath, unitDirectory, `${fileName}Tests.cs`))
                gaps.push({ file: filePath, type: "missing_unit_test", severity: "important", message: `No ${fileName}Tests.cs was found under ${unitDirectory}. Verify coverage or document the existing aggregate test.` });
        }
        if (filePath.includes("IBotApi.cs") || filePath.endsWith("BotApi.cs"))
            gaps.push({ file: filePath, type: "api_change", severity: "critical", message: "Verify modified APIs in both Milky and OneBot11 functional ApiTests where supported." });
        if (filePath.includes("/Events/") || filePath.includes("EventConverter") || filePath.includes("EventDispatcher"))
            gaps.push({ file: filePath, type: "event_change", severity: "critical", message: "Verify converter unit tests and functional EventTests for the changed event behavior." });
        if (filePath.includes("/Segments/") || filePath.includes("MessageConverter") || filePath.includes("MessageBody"))
            gaps.push({ file: filePath, type: "segment_change", severity: "critical", message: "Verify message converter unit tests and functional MessageTypeTests." });
        if (filePath.includes("/Converter/") && !filePath.includes("Mapster"))
            gaps.push({ file: filePath, type: "converter_change", severity: "important", message: "Verify the corresponding converter unit tests cover the changed mappings." });
    }

    const functional = inventory({ category: "functional", protocol: "both" }, soraPath);
    return { sourceFiles: sourcePaths, gaps, issues: functional.issues };
}

function renderChanged(result) {
    const groups = new Map();
    for (const file of result.files) {
        if (!groups.has(file.layer))
            groups.set(file.layer, []);
        groups.get(file.layer).push(file);
    }
    let output = `# Changed Files (${result.scope})\n\n`;
    if (result.files.length === 0)
        return `${output}No changes detected.`;
    for (const [layer, files] of groups) {
        output += `## ${layer}\n\n`;
        output += files.map((file) => `- [${file.status}] ${file.path}`).join("\n");
        output += "\n\n";
    }
    return output.trimEnd();
}

function renderInventory(result) {
    let output = `# Test Inventory: ${result.testCount} tests, ${result.issues.length} issues\n\n`;
    for (const file of result.files) {
        output += `## ${file.path} (${file.methods.length})\n\n`;
        if (file.regions.length > 0)
            output += `Regions: ${file.regions.map((region) => region.name).join(", ")}\n\n`;
        output += "| Test | Line | Covers |\n|---|---:|---|\n";
        output += file.methods.map((method) => `| ${method.name} | ${method.line} | ${method.cref || "-"} |`).join("\n");
        output += "\n\n";
    }
    if (result.issues.length > 0) {
        output += "## Potential Anti-patterns\n\n";
        output += result.issues.map((issue) => `- ${issue.file}:${issue.line} [${issue.severity}] ${issue.pattern}: ${issue.message}`).join("\n");
    }
    return output.trimEnd();
}

function renderCoverage(result) {
    let output = `# Coverage Gap Analysis\n\nChanged source files: ${result.sourceFiles.length}\n\nGaps: ${result.gaps.length}\n\nAnti-patterns: ${result.issues.length}\n\n`;
    if (result.gaps.length > 0) {
        output += "## Coverage Checks\n\n";
        output += result.gaps.map((gap) => `- ${gap.file} [${gap.severity}/${gap.type}]: ${gap.message}`).join("\n");
        output += "\n\n";
    }
    if (result.issues.length > 0) {
        output += "## Functional Test Anti-patterns\n\n";
        output += result.issues.map((issue) => `- ${issue.file}:${issue.line} [${issue.severity}] ${issue.pattern}: ${issue.message}`).join("\n");
    }
    if (result.gaps.length === 0 && result.issues.length === 0)
        output += "No coverage gaps or anti-patterns detected.";
    return output.trimEnd();
}

function main() {
    const args = parseArguments(process.argv.slice(2));
    const command = args._[0];
    if (!command || command === "help" || args.help) {
        console.log(usage());
        return;
    }
    const soraPath = resolve(args["sora-path"] || defaultSoraPath);
    let result;
    let markdown;
    if (command === "changed-files") {
        result = changedFiles(args, soraPath);
        markdown = renderChanged(result);
    } else if (command === "inventory") {
        result = inventory(args, soraPath);
        markdown = renderInventory(result);
    } else if (command === "coverage-gaps") {
        result = coverageGaps(args, soraPath);
        markdown = renderCoverage(result);
    } else {
        fail(`Unknown command '${command}'.\n\n${usage()}`);
    }
    console.log(args.format === "json" ? JSON.stringify(result, null, 2) : markdown);
}

try {
    main();
} catch (error) {
    console.error(`test-audit: ${error.message}`);
    process.exitCode = 1;
}

