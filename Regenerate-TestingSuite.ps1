<#
.SYNOPSIS
    Regenerates code for all GeneratedSchemaLibraries projects listed in
    the LinqToXsd-TestingSuite.slnf solution filter.
.DESCRIPTION
    Parses the solution filter JSON, finds every project under the
    GeneratedSchemaLibraries folder, and invokes the LinqToXsd CLI tool
    with 'gen -a .' in each project directory.
#>

param(
    [string]$Configuration = 'release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$slnfPath = Join-Path $repoRoot 'LinqToXsd-TestingSuite.slnf'
$linqToXsdProject = Join-Path $repoRoot 'LinqToXsd' 'LinqToXsd.csproj'
$logPath = Join-Path $repoRoot ('RegenerateLog_{0:yyyyMMdd_HHmmss}.log' -f (Get-Date))

if (-not (Test-Path $slnfPath)) {
    Write-Error "Solution filter not found: $slnfPath"
    exit 1
}

if (-not (Test-Path $linqToXsdProject)) {
    Write-Error "LinqToXsd project not found: $linqToXsdProject"
    exit 1
}

Write-Host "Building LinqToXsd CLI..." -ForegroundColor Cyan
dotnet build $linqToXsdProject
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit $LASTEXITCODE
}

$slnf = Get-Content $slnfPath -Raw | ConvertFrom-Json
$projects = $slnf.solution.projects |
    Where-Object { $_.StartsWith('GeneratedSchemaLibraries\') }

Write-Host "Found $($projects.Count) projects in GeneratedSchemaLibraries." -ForegroundColor Cyan

$failed = @()
$succeeded = 0

foreach ($proj in $projects) {
    # Path looks like: GeneratedSchemaLibraries\ProjectName\ProjectName.csproj
    $projDir = Split-Path $proj -Parent           # e.g. GeneratedSchemaLibraries\ProjectName
    $absProjDir = Join-Path $repoRoot $projDir    # full absolute path

    Write-Host "`n[$($succeeded + $failed.Count + 1)/$($projects.Count)] $projDir" -ForegroundColor Yellow
    Write-Host "  Running: dotnet run -c $Configuration --project LinqToXsd -- gen -a ."

    Push-Location $absProjDir
    try {
        $output = & dotnet run -c $Configuration -v q --framework netframework472 --project $linqToXsdProject -- gen -a . 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  OK" -ForegroundColor Green
            $succeeded++
        }
        else {
            Write-Host "  FAILED (exit code: $LASTEXITCODE)" -ForegroundColor Red
            $failed += [PSCustomObject]@{
                Project  = $projDir
                ExitCode = $LASTEXITCODE
                Output   = $output | Out-String
            }
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Done. Succeeded: $succeeded, Failed: $($failed.Count)" -ForegroundColor Cyan

if ($failed.Count -gt 0) {
    Write-Host "`nFailed projects:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  $($_.Project) (exit code: $($_.ExitCode))" -ForegroundColor Red }

    # Write detailed log file
    $logLines = @()
    $logLines += "LinqToXsd regeneration log — $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $logLines += "============================================================"
    $logLines += "Succeeded: $succeeded"
    $logLines += "Failed:    $($failed.Count)"
    $logLines += ""
    foreach ($f in $failed) {
        $logLines += "--- FAILED: $($f.Project) (exit code: $($f.ExitCode)) ---"
        $logLines += $f.Output
        $logLines += ""
    }
    $logLines | Out-File -FilePath $logPath -Encoding UTF8
    Write-Host "`nFull output written to: $logPath" -ForegroundColor Yellow
    exit 1
}
else {
    # Write a success-only log as well
    "LinqToXsd regeneration log — $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`nAll $succeeded projects succeeded." |
        Out-File -FilePath $logPath -Encoding UTF8
}
