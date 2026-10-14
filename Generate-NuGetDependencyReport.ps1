<#
.SYNOPSIS
    Generates NuGetDependencies.csv, the master list of every NuGet dependency
    (direct and transitive) used by the LinqToXsdCore projects.

.DESCRIPTION
    Enumerates every .csproj in the repository except those under an excluded
    directory (GeneratedSchemaLibraries by default), runs
    'dotnet list package --include-transitive --format json' against each one,
    and unions the results into a single CSV with the columns:

        PackageName,IsTransitive,Version

    The file has one row per (package, version) pair. A pair is reported with
    IsTransitive=false when that exact version is a top-level package in at
    least one project/target framework, and true otherwise.

    Note that this means SDK-injected, auto-referenced packages
    (NETStandard.Library, Microsoft.NET.ILLink.*) are classified as
    non-transitive, because NuGet reports them as top-level. They are not
    declared in any csproj.

    Output is deterministic: sorted by package name, then by version in NuGet
    precedence order, using ordinal comparison so results do not vary with the
    machine's culture. Running the script twice produces byte-identical output.

.PARAMETER OutputPath
    Where to write the CSV. Defaults to NuGetDependencies.csv next to this script.

.PARAMETER ExcludeDirectory
    Directory names, relative to the repository root, whose projects are skipped.
    Defaults to 'GeneratedSchemaLibraries'.

.PARAMETER NoRestore
    Pass --no-restore to 'dotnet list package'. Use in a pipeline where a restore
    has already run; assumes project.assets.json files are present.

.PARAMETER Verify
    Do not write the CSV. Instead, fail with a non-zero exit code if the existing
    file differs from what would be generated. Intended for CI drift detection.

.EXAMPLE
    .\Generate-NuGetDependencyReport.ps1

.EXAMPLE
    # CI: fail the build if NuGetDependencies.csv is out of date
    .\Generate-NuGetDependencyReport.ps1 -Verify -NoRestore
#>
[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'NuGetDependencies.csv'),

    [string[]]$ExcludeDirectory = @('GeneratedSchemaLibraries'),

    [switch]$NoRestore,

    [switch]$Verify
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = $PSScriptRoot

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

<#
    Builds a string sort key that orders NuGet versions by precedence rather than
    lexically (so 9.0.2 < 10.0.0), with a release version sorting after any of
    its prereleases (1.0.0-beta < 1.0.0).
#>
function Get-NuGetVersionSortKey {
    param([Parameter(Mandatory)][string]$Version)

    $release = $Version.Trim()
    $pre = $null
    $dash = $release.IndexOf('-')
    if ($dash -ge 0) {
        $pre = $release.Substring($dash + 1)
        $release = $release.Substring(0, $dash)
    }

    $releaseParts = @()
    foreach ($part in $release.Split('.')) {
        $n = 0
        if (-not [int]::TryParse($part, [ref]$n)) { $n = 0 }
        $releaseParts += $n.ToString('D5')
    }
    $key = ($releaseParts -join '.')

    if ([string]::IsNullOrEmpty($pre)) {
        # '_' sorts after '-', so a release sorts after its own prereleases.
        return $key + '_'
    }

    $preParts = @()
    foreach ($identifier in $pre.Split('.')) {
        $n = 0
        if ([int]::TryParse($identifier, [ref]$n)) {
            $preParts += '0' + $n.ToString('D5')   # numeric identifiers sort first
        }
        else {
            $preParts += '1' + $identifier
        }
    }
    return $key + '-' + ($preParts -join '.')
}

<#
    Reads a property from a ConvertFrom-Json result without throwing when the
    property is absent. Tools often omit empty arrays entirely (a project with
    no transitive packages has no 'transitivePackages' member at all), which
    StrictMode would otherwise turn into a hard error.
#>
function Get-JsonMember {
    param($Object, [string]$Name)

    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

<#
    Quotes a CSV field only when it needs it, matching the output that
    Python's csv module and most CSV writers produce.
#>
function ConvertTo-CsvField {
    param([string]$Value)

    if ($Value -match '[",\r\n]') {
        return '"' + $Value.Replace('"', '""') + '"'
    }
    return $Value
}

<#
    Runs 'dotnet list package --include-transitive --format json' for one project
    and returns the parsed object.
#>
function Get-ProjectPackageJson {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [switch]$NoRestore
    )

    $listArgs = @('list', $ProjectPath, 'package', '--include-transitive', '--format', 'json')
    if ($NoRestore) { $listArgs += '--no-restore' }

    $output = & dotnet @listArgs 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet list package failed for '$ProjectPath' (exit code $LASTEXITCODE):`n$output"
    }

    # Restore warnings can be interleaved with the JSON payload; slice out the
    # JSON object itself.
    $start = $output.IndexOf('{')
    $end = $output.LastIndexOf('}')
    if ($start -lt 0 -or $end -lt $start) {
        throw "Could not find JSON in 'dotnet list package' output for '$ProjectPath':`n$output"
    }

    return ($output.Substring($start, $end - $start + 1) | ConvertFrom-Json)
}

# ---------------------------------------------------------------------------
# Discover projects
# ---------------------------------------------------------------------------

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The 'dotnet' CLI was not found on PATH."
}

$excludedFullPaths = $ExcludeDirectory | ForEach-Object {
    [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $_))
}

$projects = @(
    Get-ChildItem -Path $RepoRoot -Filter '*.csproj' -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
        Where-Object {
            $full = $_.FullName
            $isExcluded = $false
            foreach ($excluded in $excludedFullPaths) {
                if ($full.StartsWith($excluded, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $isExcluded = $true
                    break
                }
            }
            -not $isExcluded
        } |
        Sort-Object FullName
)

if ($projects.Count -eq 0) {
    throw "No .csproj files found under '$RepoRoot'."
}

Write-Host "Scanning $($projects.Count) project(s) for NuGet dependencies..."

# ---------------------------------------------------------------------------
# Build the package graph
# ---------------------------------------------------------------------------

# Keyed by "<name>`0<version>"; value is a flag for whether the pair was ever
# seen as a top-level (direct) package.
$directPairs = @{}
$allPairs = @{}

foreach ($project in $projects) {
    $relative = $project.FullName.Substring($RepoRoot.Length).TrimStart('\', '/')
    Write-Verbose "  $relative"

    $json = Get-ProjectPackageJson -ProjectPath $project.FullName -NoRestore:$NoRestore

    # Deliberately no @() around the Get-JsonMember calls: when a project has no
    # packages of a given kind, 'dotnet' omits the property entirely and the
    # helper returns $null. 'foreach' over $null iterates zero times, whereas
    # @($null) would produce a single bogus $null element.
    foreach ($proj in (Get-JsonMember $json 'projects')) {
        foreach ($framework in (Get-JsonMember $proj 'frameworks')) {
            foreach ($package in (Get-JsonMember $framework 'topLevelPackages')) {
                $version = Get-JsonMember $package 'resolvedVersion'
                if (-not $version) { $version = Get-JsonMember $package 'requestedVersion' }
                $key = (Get-JsonMember $package 'id') + [char]0 + $version
                $allPairs[$key] = $true
                $directPairs[$key] = $true
            }
            foreach ($package in (Get-JsonMember $framework 'transitivePackages')) {
                $version = Get-JsonMember $package 'resolvedVersion'
                if (-not $version) { $version = Get-JsonMember $package 'requestedVersion' }
                $key = (Get-JsonMember $package 'id') + [char]0 + $version
                $allPairs[$key] = $true
            }
        }
    }
}

if ($allPairs.Count -eq 0) {
    throw "No NuGet packages were found across the scanned projects."
}

# ---------------------------------------------------------------------------
# Sort (ordinal, deterministic) and render
# ---------------------------------------------------------------------------

$records = @()
$sortKeys = @()
foreach ($key in $allPairs.Keys) {
    $split = $key.IndexOf([char]0)
    $name = $key.Substring(0, $split)
    $version = $key.Substring($split + 1)

    # The NUL separator is lower than any printable character, so an ordinal
    # sort on this composite sorts by name first, then by version.
    $sortKeys += $name + [char]0 + (Get-NuGetVersionSortKey -Version $version)
    $records += [pscustomobject]@{
        Name         = $name
        Version      = $version
        IsTransitive = -not $directPairs.ContainsKey($key)
    }
}

[System.Array]::Sort(
    [string[]]$sortKeys,
    [object[]]$records,
    [System.StringComparer]::Ordinal
)

$builder = [System.Text.StringBuilder]::new()
[void]$builder.Append("PackageName,IsTransitive,Version`r`n")
foreach ($record in $records) {
    $transitive = if ($record.IsTransitive) { 'true' } else { 'false' }
    [void]$builder.Append(
        (ConvertTo-CsvField $record.Name) + ',' +
        $transitive + ',' +
        (ConvertTo-CsvField $record.Version) + "`r`n"
    )
}
$content = $builder.ToString()

$directRows = ($records | Where-Object { -not $_.IsTransitive }).Count
Write-Host ("Resolved {0} package/version rows ({1} distinct packages, {2} non-transitive)." -f `
    $records.Count, ($records.Name | Sort-Object -Unique).Count, $directRows)

# ---------------------------------------------------------------------------
# Emit
# ---------------------------------------------------------------------------

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

if ($Verify) {
    if (-not (Test-Path -LiteralPath $OutputPath)) {
        Write-Error "Expected '$OutputPath' to exist, but it does not. Run this script without -Verify to generate it."
        exit 1
    }

    $existing = [System.IO.File]::ReadAllText($OutputPath)
    if ($existing -ne $content) {
        Write-Error "'$OutputPath' is out of date. Re-run this script without -Verify and commit the result."
        exit 1
    }

    Write-Host "OK: $OutputPath is up to date."
    exit 0
}

[System.IO.File]::WriteAllText($OutputPath, $content, $utf8NoBom)
Write-Host "Wrote $OutputPath"
