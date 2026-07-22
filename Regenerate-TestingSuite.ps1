<#
.SYNOPSIS
    Regenerates code for all GeneratedSchemaLibraries projects listed in
    the LinqToXsd-TestingSuite.slnf solution filter.
.DESCRIPTION
    Parses the solution filter JSON, finds every project under the
    GeneratedSchemaLibraries folder, and invokes the LinqToXsd CLI tool
    with 'gen -a .' in each project directory.
#>

$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$slnfPath = Join-Path $repoRoot 'LinqToXsd-TestingSuite.slnf'
$linqToXsdProject = Join-Path $repoRoot 'LinqToXsd' 'LinqToXsd.csproj'

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
    Write-Host "  Running: dotnet run --project LinqToXsd -- gen -a ."

    Push-Location $absProjDir
    try {
        dotnet run -v q --framework netframework472 --project $linqToXsdProject -- gen -a .
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  OK" -ForegroundColor Green
            $succeeded++
        }
        else {
            Write-Host "  FAILED (exit code: $LASTEXITCODE)" -ForegroundColor Red
            $failed += $projDir
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
    $failed | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}
