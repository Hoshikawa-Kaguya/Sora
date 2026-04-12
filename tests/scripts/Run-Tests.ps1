<#
.SYNOPSIS
    Sora Framework Test Runner (Dual-Bot Architecture)

.DESCRIPTION
    Runs unit and/or functional tests with configurable parameters.
    Supports dual-bot architecture with primary and secondary bot hosts.
    Generates TRX result files and optionally uploads them to a QQ group via Milky.

.PARAMETER Category
    Test category to run: "Unit", "Functional", or "All". Default: "All".

.PARAMETER Ob11PrimaryHost
    OneBot11 primary bot host. Required for OB11 functional tests.

.PARAMETER Ob11SecondaryHost
    OneBot11 secondary bot host for dual-bot tests.

.PARAMETER Ob11Port
    OneBot11 server port. Default: 3001.

.PARAMETER Ob11Token
    OneBot11 access token.

.PARAMETER MilkyPrimaryHost
    Milky primary bot host. Required for Milky functional tests.

.PARAMETER MilkySecondaryHost
    Milky secondary bot host for dual-bot tests.

.PARAMETER MilkyPort
    Milky server port. Default: 3010.

.PARAMETER MilkyToken
    Milky access token.

.PARAMETER MilkyPrefix
    Milky URL prefix (e.g. "milky").

.PARAMETER GroupId
    Test group ID for functional tests. Required for functional tests.

.PARAMETER PrimaryBotAvatar
    Path to the primary bot's avatar image file.

.PARAMETER SecondaryBotAvatar
    Path to the secondary bot's avatar image file.

.PARAMETER GroupAvatarPath
    Path to the test group's avatar image file.

.PARAMETER AudioFilePath
    Path to an audio file for SendReceive_Audio test (read locally, sent as base64).

.PARAMETER VideoFilePath
    Path to a video file for SendReceive_Video test (read locally, sent as base64).

.PARAMETER Configuration
    Build configuration. Default: "Release".

.PARAMETER LogLevel
    Sora framework log level override: "Trace", "Debug", "Info", "Warn", "Error", "Fatal", "None".
    Default: "Debug". Sets the SORA_LOG_LEVEL_OVERRIDE environment variable (case-insensitive).

.PARAMETER WaitDebugger
    Wait for a debugger to attach before running tests (sets VSTEST_HOST_DEBUG=1).

.PARAMETER NoBuild
    Skip building before testing.

.EXAMPLE
    # Run all unit tests only
    .\Run-Tests.ps1 -Category Unit

.EXAMPLE
    # Run functional tests against dual-bot setup
    .\Run-Tests.ps1 -Category Functional -Ob11PrimaryHost <primary-host> -Ob11SecondaryHost <secondary-host> -MilkyPrimaryHost <primary-host> -MilkySecondaryHost <secondary-host> -MilkyToken <token> -MilkyPrefix <prefix> -GroupId <group-id>

.EXAMPLE
    # Run all tests with report upload via Milky
    .\Run-Tests.ps1 -Ob11PrimaryHost <primary-host> -MilkyPrimaryHost <primary-host> -MilkyToken <token> -MilkyPrefix <prefix> -GroupId <group-id> -EnableReport

.EXAMPLE
    # Run tests with Debug log level
    .\Run-Tests.ps1 -Category Unit -LogLevel Debug

.EXAMPLE
    # Run tests in debug mode (waits for debugger to attach)
    .\Run-Tests.ps1 -Category Unit -WaitDebugger
#>
[CmdletBinding()]
param(
    [ValidateSet("Unit", "Functional", "All")]
    [string]$Category = "All",

    [string]$Ob11PrimaryHost,
    [string]$Ob11SecondaryHost,
    [int]$Ob11Port = 3001,
    [string]$Ob11Token,

    [string]$MilkyPrimaryHost,
    [string]$MilkySecondaryHost,
    [int]$MilkyPort = 3010,
    [string]$MilkyToken,
    [string]$MilkyPrefix,

    [long]$GroupId,

    [string]$PrimaryBotAvatar,
    [string]$SecondaryBotAvatar,
    [string]$GroupAvatarPath,

    [string]$AudioFilePath,
    [string]$VideoFilePath,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("Trace", "Debug", "Info", "Warn", "Error", "Fatal", "None")]
    [string]$LogLevel = "Debug",

    [string]$ResultsDir,

    ## Additional dotnet test filter expression. Appended to the category filter.
    ## Examples: "FullyQualifiedName~ApiTests", "FullyQualifiedName~GetForwardMessages"
    [string]$Filter,

    ## Enable/disable the TestReporter (send report to group + upload TRX).
    ## Default: disabled. Use -EnableReport to enable.
    [switch]$EnableReport,

    ## Wait for debugger to attach before running tests (sets VSTEST_HOST_DEBUG=1).
    [switch]$WaitDebugger,

    ## Collect code coverage using coverlet and generate an HTML report with ReportGenerator.
    [switch]$Coverage,

    [switch]$NoBuild
)

$ErrorActionPreference = "Continue"

# Fix console encoding for UTF-8 output (Chinese characters from dotnet test)
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$SolutionRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$SolutionFile = Join-Path $SolutionRoot "Sora.slnx"

# Default results directory
if (-not $ResultsDir) {
    $ResultsDir = Join-Path $SolutionRoot "TestResults"
}

# Ensure results directory exists
if (-not (Test-Path $ResultsDir)) {
    New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null
    Write-Host "[Setup] Created results directory: $ResultsDir" -ForegroundColor DarkGray
}

# Clean existing TRX files before test run
$existingTrx = Get-ChildItem -LiteralPath $ResultsDir -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue
if ($existingTrx) {
    $existingTrx | Remove-Item -Force
    Write-Host "[Setup] Cleaned $($existingTrx.Count) existing TRX file(s)" -ForegroundColor DarkGray
}

# ---- Banner ----
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Sora Framework Test Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ---- Set environment variables ----
if ($Ob11PrimaryHost)   { $env:SORA_TEST_OB11_PRIMARY_HOST = $Ob11PrimaryHost }
if ($Ob11SecondaryHost) { $env:SORA_TEST_OB11_SECONDARY_HOST = $Ob11SecondaryHost }
if ($Ob11Port)          { $env:SORA_TEST_OB11_PORT = $Ob11Port.ToString() }
if ($Ob11Token)         { $env:SORA_TEST_OB11_TOKEN = $Ob11Token }
if ($MilkyPrimaryHost)   { $env:SORA_TEST_MILKY_PRIMARY_HOST = $MilkyPrimaryHost }
if ($MilkySecondaryHost) { $env:SORA_TEST_MILKY_SECONDARY_HOST = $MilkySecondaryHost }
if ($MilkyPort)          { $env:SORA_TEST_MILKY_PORT = $MilkyPort.ToString() }
if ($MilkyToken)         { $env:SORA_TEST_MILKY_TOKEN = $MilkyToken }
if ($MilkyPrefix)        { $env:SORA_TEST_MILKY_PREFIX = $MilkyPrefix }
if ($GroupId -gt 0)      { $env:SORA_TEST_GROUP_ID = $GroupId.ToString() }
if ($PrimaryBotAvatar)   { $env:SORA_TEST_PRIMARY_BOT_AVATAR = $PrimaryBotAvatar }
if ($SecondaryBotAvatar) { $env:SORA_TEST_SECONDARY_BOT_AVATAR = $SecondaryBotAvatar }
if ($GroupAvatarPath)    { $env:SORA_TEST_GROUP_AVATAR = $GroupAvatarPath }
if ($AudioFilePath)     { $env:SORA_TEST_AUDIO_FILE = $AudioFilePath }
if ($VideoFilePath)     { $env:SORA_TEST_VIDEO_FILE = $VideoFilePath }

# Always set results directory so test reporter can find TRX files
$env:SORA_TEST_RESULTS_DIR = $ResultsDir

# Enable functional tests if any host is configured and group is set
if (($Ob11PrimaryHost -or $MilkyPrimaryHost) -and $GroupId -gt 0) {
    $env:SORA_TEST_FUNCTIONAL = "true"
}

# Log level override for Sora framework
$env:SORA_LOG_LEVEL_OVERRIDE = $LogLevel

# Debug mode — test host waits for debugger to attach
if ($WaitDebugger) {
    $env:VSTEST_HOST_DEBUG = "1"
}

# ---- Display configuration ----
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Category:        $Category"
Write-Host "  Build Config:    $Configuration"
Write-Host "  Results Dir:     $ResultsDir"
if ($Ob11PrimaryHost) {
    Write-Host "  OB11 Primary:    ws://${Ob11PrimaryHost}:${Ob11Port}" -ForegroundColor Green
} else {
    Write-Host "  OB11 Primary:    (not configured)" -ForegroundColor DarkGray
}
if ($Ob11SecondaryHost) {
    Write-Host "  OB11 Secondary:  ws://${Ob11SecondaryHost}:${Ob11Port}" -ForegroundColor Green
} else {
    Write-Host "  OB11 Secondary:  (not configured)" -ForegroundColor DarkGray
}
if ($MilkyPrimaryHost) {
    $prefix = if ($MilkyPrefix) { "/$MilkyPrefix" } else { "" }
    Write-Host "  Milky Primary:   http://${MilkyPrimaryHost}:${MilkyPort}${prefix}" -ForegroundColor Green
} else {
    Write-Host "  Milky Primary:   (not configured)" -ForegroundColor DarkGray
}
if ($MilkySecondaryHost) {
    $prefix = if ($MilkyPrefix) { "/$MilkyPrefix" } else { "" }
    Write-Host "  Milky Secondary: http://${MilkySecondaryHost}:${MilkyPort}${prefix}" -ForegroundColor Green
} else {
    Write-Host "  Milky Secondary: (not configured)" -ForegroundColor DarkGray
}
if ($GroupId -gt 0) {
    Write-Host "  Test Group:      $GroupId" -ForegroundColor Green
} else {
    Write-Host "  Test Group:      (not set — functional tests will skip)" -ForegroundColor DarkGray
}
if ($Filter) {
    Write-Host "  Filter:          $Filter" -ForegroundColor Cyan
}
if ($EnableReport) {
    Write-Host "  Report:          enabled" -ForegroundColor Green
} else {
    Write-Host "  Report:          disabled (use -EnableReport to enable)" -ForegroundColor DarkGray
}
if ($LogLevel -ne "Debug") {
    Write-Host "  Log Level:       $LogLevel" -ForegroundColor Cyan
} else {
    Write-Host "  Log Level:       $LogLevel" -ForegroundColor DarkGray
}
if ($WaitDebugger) {
    Write-Host "  Debug Mode:      enabled (test host will wait for debugger)" -ForegroundColor Magenta
}
if ($Coverage) {
    Write-Host "  Coverage:        enabled" -ForegroundColor Green
} else {
    Write-Host "  Coverage:        disabled (use -Coverage to enable)" -ForegroundColor DarkGray
}
Write-Host ""

# ---- Build ----
if (-not $NoBuild) {
    Write-Host "[Build] Building solution..." -ForegroundColor Yellow
    dotnet build $SolutionFile --configuration $Configuration --verbosity quiet 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[Build] FAILED" -ForegroundColor Red
        exit 1
    }
    Write-Host "[Build] Succeeded" -ForegroundColor Green
    Write-Host ""
}

# ---- Helper function for running tests ----
$testRunResults = @()

function Invoke-TestRun {
    param(
        [string]$Filter,
        [string]$Label,
        [string]$TrxFileName
    )

    $dateStr = Get-Date -Format "yyyyMMdd_HHmmss"
    $trxPath = Join-Path $ResultsDir "${TrxFileName}_${dateStr}.trx"
    Write-Host "[$Label] Running..." -ForegroundColor Yellow

    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    $args = @(
        "test", $SolutionFile,
        "--configuration", $Configuration,
        "--no-build",
        "--logger", "console;verbosity=normal",
        "--logger", "trx;LogFileName=$trxPath",
        "--results-directory", $ResultsDir
    )

    if ($Filter) {
        $args += "--filter"
        $args += $Filter
    }

    if ($Coverage) {
        $args += "--collect:XPlat Code Coverage"
    }

    # Separator between dotnet test args and xUnit runner args
    $args += "--"
    $args += "--diagnostic"
    $args += "--diagnostic-output-directory"
    $args += $ResultsDir
    $args += "--diagnostic-output-fileprefix"
    $args += $Label

    # Run and stream output to console
    & dotnet @args 2>&1 | ForEach-Object { Write-Host $_ }

    $sw.Stop()
    $exitCode = $LASTEXITCODE
    $duration = $sw.Elapsed

    if ($exitCode -eq 0) {
        Write-Host ("[$Label] PASSED ({0:F3} s)" -f $duration.TotalSeconds) -ForegroundColor Green
    } else {
        Write-Host ("[$Label] FAILED ({0:F3} s, exit code: $exitCode)" -f $duration.TotalSeconds) -ForegroundColor Red
    }

    # Check for TRX file
    if (Test-Path $trxPath) {
        Write-Host "[$Label] TRX: $trxPath" -ForegroundColor DarkGray
    }

    Write-Host ""

    # Record result for summary
    $script:testRunResults += [PSCustomObject]@{
        Label    = $Label
        ExitCode = $exitCode
        Duration = $duration
        TrxPath  = $trxPath
    }

    return $exitCode
}

# ---- Run tests ----
$exitCodes = @()

# Helper: combine base filter with user's -Filter param
function Get-CombinedFilter([string]$BaseFilter) {
    if ($Filter) { return "$BaseFilter&$Filter" }
    return $BaseFilter
}

if ($Category -eq "Unit" -or $Category -eq "All") {
    $unitFilter = Get-CombinedFilter "Category=Unit"
    $exitCodes += Invoke-TestRun -Filter $unitFilter -Label "Unit Tests" -TrxFileName "[Unit][All][All]"
}

if ($Category -eq "Functional" -or $Category -eq "All") {
    if ($env:SORA_TEST_FUNCTIONAL -eq "true") {
        if ($Ob11PrimaryHost) {
            $ob11Filter = Get-CombinedFilter "Category=Functional&FullyQualifiedName~OneBot11"
            $exitCodes += Invoke-TestRun -Filter $ob11Filter -Label "Functional OB11" -TrxFileName "[Func][OneBot11][ApiTests]"
        } else {
            Write-Host "[Functional OB11] Skipped — SORA_TEST_OB11_PRIMARY_HOST not set" -ForegroundColor DarkGray
            Write-Host ""
        }

        if ($MilkyPrimaryHost) {
            $milkyFilter = Get-CombinedFilter "Category=Functional&FullyQualifiedName~Milky"
            $exitCodes += Invoke-TestRun -Filter $milkyFilter -Label "Functional Milky" -TrxFileName "[Func][Milky][ApiTests]"
        } else {
            Write-Host "[Functional Milky] Skipped — SORA_TEST_MILKY_PRIMARY_HOST not set" -ForegroundColor DarkGray
            Write-Host ""
        }
    } else {
        Write-Host "[Functional] Skipped — no hosts configured or GroupId not set" -ForegroundColor DarkGray
        Write-Host ""
    }
}

# ---- Summary ----
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test Run Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Duration breakdown
$unitRuns = $testRunResults | Where-Object { $_.Label -like "Unit*" }
$funcRuns = $testRunResults | Where-Object { $_.Label -like "Functional*" }

if ($unitRuns) {
    $unitTotal = [TimeSpan]::Zero
    foreach ($r in $unitRuns) { $unitTotal += $r.Duration }
    Write-Host ("  Unit Tests:       {0:F3} s" -f $unitTotal.TotalSeconds) -ForegroundColor $(if (($unitRuns | Where-Object { $_.ExitCode -ne 0 })) { "Red" } else { "Green" })
}
if ($funcRuns) {
    $funcTotal = [TimeSpan]::Zero
    foreach ($r in $funcRuns) { $funcTotal += $r.Duration }
    Write-Host ("  Functional Tests: {0:F3} s" -f $funcTotal.TotalSeconds) -ForegroundColor $(if (($funcRuns | Where-Object { $_.ExitCode -ne 0 })) { "Red" } else { "Green" })
}

$totalDuration = [TimeSpan]::Zero
foreach ($r in $testRunResults) { $totalDuration += $r.Duration }
Write-Host ("  Total Duration:   {0:F3} s" -f $totalDuration.TotalSeconds) -ForegroundColor White
Write-Host ""

# Per-run details
foreach ($r in $testRunResults) {
    $status = if ($r.ExitCode -eq 0) { "PASS" } else { "FAIL" }
    $color = if ($r.ExitCode -eq 0) { "Green" } else { "Red" }
    Write-Host ("  [{0}] {1} — {2:F3} s" -f $status, $r.Label, $r.Duration.TotalSeconds) -ForegroundColor $color
}
Write-Host ""

# TRX files
$trxFiles = Get-ChildItem $ResultsDir -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue
if ($trxFiles) {
    Write-Host "  Generated TRX files:" -ForegroundColor Yellow
    foreach ($f in $trxFiles) {
        Write-Host "    $($f.FullName)" -ForegroundColor DarkGray
    }
} else {
    Write-Host "  No TRX files generated." -ForegroundColor DarkGray
}

$failed = $exitCodes | Where-Object { $_ -ne 0 }

# ---- Generate coverage report ----
if ($Coverage) {
    $coberturaFiles = Get-ChildItem $ResultsDir -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
    if ($coberturaFiles) {
        $coverageDir = Join-Path $ResultsDir "CoverageReport"
        $reportSources = ($coberturaFiles | ForEach-Object { $_.FullName }) -join ";"
        Write-Host ""
        Write-Host "[Coverage] Generating HTML report..." -ForegroundColor Yellow
        & reportgenerator `
            "-reports:$reportSources" `
            "-targetdir:$coverageDir" `
            "-reporttypes:Html;TextSummary" `
            "-verbosity:Warning" 2>&1 | ForEach-Object { Write-Host $_ }
        if (Test-Path (Join-Path $coverageDir "Summary.txt")) {
            Write-Host ""
            Write-Host "[Coverage] Summary:" -ForegroundColor Cyan
            Get-Content (Join-Path $coverageDir "Summary.txt") | ForEach-Object { Write-Host "  $_" }
        }
        Write-Host "[Coverage] HTML report: $coverageDir\index.html" -ForegroundColor Green
    } else {
        Write-Host "[Coverage] No coverage files generated." -ForegroundColor DarkGray
    }
    Write-Host ""
}

# ---- Send report to group via TestReporter tool ----
if ($EnableReport -and $MilkyPrimaryHost -and $GroupId -gt 0) {
    Write-Host ""
    Write-Host "[Report] Sending test report to group $GroupId..." -ForegroundColor Yellow

    # Compute totals for reporter args
    $unitDurSec = 0.0
    $funcDurSec = 0.0

    if ($unitRuns) { foreach ($r in $unitRuns) { $unitDurSec += $r.Duration.TotalSeconds } }
    if ($funcRuns) { foreach ($r in $funcRuns) { $funcDurSec += $r.Duration.TotalSeconds } }

    # Test counts are parsed from TRX files by the reporter tool itself
    $reporterProject = Join-Path $SolutionRoot "tools\TestReporter\TestReporter.csproj"
    $reportArgs = @(
        "run", "--project", $reporterProject, "--configuration", $Configuration, "--no-build", "--",
        $ResultsDir, $GroupId.ToString(),
        $unitDurSec.ToString("F3"), $funcDurSec.ToString("F3")
    )

    & dotnet @reportArgs 2>&1 | ForEach-Object { Write-Host $_ }
    Write-Host "[Report] Done" -ForegroundColor Green
    Write-Host ""
}

if ($failed) {
    Write-Host ""
    Write-Host "  Result: SOME TESTS FAILED" -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "  Result: ALL TESTS PASSED" -ForegroundColor Green
    exit 0
}
