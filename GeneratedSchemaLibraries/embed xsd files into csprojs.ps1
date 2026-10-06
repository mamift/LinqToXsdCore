<#
.SYNOPSIS
    Embeds None/EmbeddedResource directives into existing generated schema library csprojs.

.DESCRIPTION
    PowerShell port of 'embed xsd files into csprojs.linq', cross-platform (PowerShell 7+).

    Walks the sub-directories of this script's folder. For each one that contains a csproj,
    it adds <None Remove="..."/> and <EmbeddedResource Include="..."/> directives for every
    *.xsd, *.xsd.cs, *.xsd.config, *.xml, *.txt or *.md file under it (excluding obj\Debug),
    inserting them right after the csproj's first PropertyGroup.

    Sub-directories whose csproj already contains both None and EmbeddedResource elements
    are left untouched.

.PARAMETER Directories
    Optional names of the sub-directories to process. When omitted, every sub-directory
    is processed.

.EXAMPLE
    pwsh -File 'embed xsd files into csprojs.ps1'

.EXAMPLE
    pwsh -File 'embed xsd files into csprojs.ps1' -Directories XHTML, DublinCore
#>
[CmdletBinding()]
param(
    [string[]]$Directories
)

$ErrorActionPreference = 'Stop'

$schemasRoot = $PSScriptRoot

# With `pwsh -File`, a comma-separated list arrives as a single string, so split it apart.
$Directories = @($Directories | ForEach-Object { $_.Split(',') } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

# True when the csproj already contains at least one None and one EmbeddedResource element.
function Test-HasEmbeddedAndNoneElements([string]$CsprojPath) {
    $xdoc = [xml](Get-Content -LiteralPath $CsprojPath -Raw)
    $noneCount = $xdoc.SelectNodes("//*[local-name()='None']").Count
    $embeddedCount = $xdoc.SelectNodes("//*[local-name()='EmbeddedResource']").Count
    return $noneCount -gt 0 -and $embeddedCount -gt 0
}

# Path of FileFullPath relative to DirPath, using the platform's directory separator.
function Get-PathRelativeTo([string]$FileFullPath, [string]$DirPath) {
    return [System.IO.Path]::GetRelativePath($DirPath, $FileFullPath)
}

function Embed-DirectivesIntoCsproj([string]$DestDir, [string]$Name) {
    $destProjDir = Join-Path $DestDir $Name
    $csprojPath = Join-Path $destProjDir "$Name.csproj"

    $xdoc = [xml](Get-Content -LiteralPath $csprojPath -Raw)

    $dirFiles = @(
        Get-ChildItem -LiteralPath $destProjDir -File -Recurse |
            Where-Object { $_.FullName -notmatch '[\\/]obj[\\/]Debug[\\/]' } |
            Where-Object { $_.Name -match '\.(xsd(\.cs|\.config)?|xml|txt|md)$' } |
            Sort-Object FullName
    )
    $filesToNoneRemove = @($dirFiles | Where-Object { -not $_.Name.EndsWith('.cs') })
    $filesToEmbed = $dirFiles

    # Same as the original: bail out when directives are already present.
    if ($xdoc.SelectNodes("//*[local-name()='None']").Count -gt 0) { return }
    if ($xdoc.SelectNodes("//*[local-name()='EmbeddedResource']").Count -gt 0) { return }

    $noneGroup = $xdoc.CreateElement('ItemGroup')
    foreach ($file in $filesToNoneRemove) {
        $element = $xdoc.CreateElement('None')
        $element.SetAttribute('Remove', (Get-PathRelativeTo $file.FullName $destProjDir))
        [void]$noneGroup.AppendChild($element)
    }

    $embeddedGroup = $xdoc.CreateElement('ItemGroup')
    foreach ($file in $filesToEmbed) {
        $element = $xdoc.CreateElement('EmbeddedResource')
        $element.SetAttribute('Include', (Get-PathRelativeTo $file.FullName $destProjDir))
        [void]$embeddedGroup.AppendChild($element)
    }

    $firstPropertyGroup = $xdoc.SelectSingleNode("//*[local-name()='PropertyGroup']")
    if (-not $firstPropertyGroup) {
        throw "No PropertyGroup found in '$csprojPath'."
    }

    # Insert in the same order the original script produced: embedded group first, then none group.
    [void]$firstPropertyGroup.ParentNode.InsertAfter($noneGroup, $firstPropertyGroup)
    [void]$firstPropertyGroup.ParentNode.InsertAfter($embeddedGroup, $firstPropertyGroup)

    Write-Host "Saving... $csprojPath"
    $xdoc.Save($csprojPath)
}

$schemaDirs = @(Get-ChildItem -LiteralPath $schemasRoot -Directory)
if ($Directories) {
    $schemaDirs = @($schemaDirs | Where-Object { $Directories -contains $_.Name })
}

$skipCount = 0
foreach ($dir in $schemaDirs) {
    $possibleCsprojs = @(Get-ChildItem -LiteralPath $dir.FullName -File -Filter '*.csproj' -Recurse)
    if ($possibleCsprojs.Count -eq 0) {
        $skipCount++
        continue
    }

    $csproj = $possibleCsprojs[0]
    if (Test-HasEmbeddedAndNoneElements $csproj.FullName) {
        $skipCount++
        Write-Host "$($csproj.Name): has none/embedded directives in csproj already"
        continue
    }

    Write-Host "Found $($dir.FullName)"
    Embed-DirectivesIntoCsproj $schemasRoot $dir.Name
}

if ($skipCount -eq $schemaDirs.Count) {
    Write-Host 'None processed'
}
