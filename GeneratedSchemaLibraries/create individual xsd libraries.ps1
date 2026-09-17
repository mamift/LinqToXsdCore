<#
.SYNOPSIS
    Creates individual generated schema library csprojs from template.csproj_ and registers them.

.DESCRIPTION
    PowerShell port of 'create individual xsd libraries.linq', cross-platform (PowerShell 7+).

    Walks the sub-directories of this script's folder. For each one that has no csproj
    anywhere beneath it, it:
      - copies template.csproj_ to <dir>/<dir>.csproj, filling in <None Remove> and
        <EmbeddedResource Include> directives for every *.xsd* file directly in it,
      - adds a ProjectReference to LinqToXsd.Schemas.csproj (next to the XSD reference),
      - runs `dotnet sln add` against LinqToXsdCore.sln and LinqToXsd-TestingSuite.slnf.

.PARAMETER Directories
    Optional names of the sub-directories to process. When omitted, every sub-directory
    is processed.

.EXAMPLE
    pwsh -File 'create individual xsd libraries.ps1'

.EXAMPLE
    pwsh -File 'create individual xsd libraries.ps1' -Directories XHTML, DublinCore
#>
[CmdletBinding()]
param(
    [string[]]$Directories
)

$ErrorActionPreference = 'Stop'

$schemasRoot = $PSScriptRoot

# With `pwsh -File`, a comma-separated list arrives as a single string, so split it apart.
$Directories = @($Directories | ForEach-Object { $_.Split(',') } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$schemasCsProjPath = [System.IO.Path]::GetFullPath(
    (Join-Path $schemasRoot '../LinqToXsd.Schemas/LinqToXsd.Schemas.csproj'))

function Copy-AndFilloutTemplate([string]$TemplateDir, [string]$DestDir, [string]$Name) {
    $templateCsProjPath = Join-Path $TemplateDir 'template.csproj_'
    $destProjDir = Join-Path $DestDir $Name
    $destCsprojPath = Join-Path $destProjDir "$Name.csproj"

    $xdoc = [xml](Get-Content -LiteralPath $templateCsProjPath -Raw)
    $filesToEmbed = @(Get-ChildItem -LiteralPath $destProjDir -File -Filter '*.xsd*' | Sort-Object Name)

    $noneGroup = $xdoc.CreateElement('ItemGroup')
    foreach ($file in $filesToEmbed) {
        $element = $xdoc.CreateElement('None')
        $element.SetAttribute('Remove', $file.Name)
        [void]$noneGroup.AppendChild($element)
    }

    $embeddedGroup = $xdoc.CreateElement('ItemGroup')
    foreach ($file in $filesToEmbed) {
        $element = $xdoc.CreateElement('EmbeddedResource')
        $element.SetAttribute('Include', $file.Name)
        [void]$embeddedGroup.AppendChild($element)
    }

    $firstPropertyGroup = $xdoc.SelectSingleNode("//*[local-name()='PropertyGroup']")
    if (-not $firstPropertyGroup) {
        throw "No PropertyGroup found in '$templateCsProjPath'."
    }

    # Insert in the same order the original script produced: embedded group first, then none group.
    [void]$firstPropertyGroup.ParentNode.InsertAfter($noneGroup, $firstPropertyGroup)
    [void]$firstPropertyGroup.ParentNode.InsertAfter($embeddedGroup, $firstPropertyGroup)

    Write-Host "Copying to... $destCsprojPath"
    $xdoc.Save($destCsprojPath)
}

function Add-ProjectToSln([string]$ProjectPath, [string]$SlnFileName = 'LinqToXsdCore.sln') {
    # dotnet stores project paths relative to the solution's own location, so absolute
    # paths here produce the same entries as the original script's working-directory-
    # relative ones. Arguments are passed unquoted: PowerShell quotes them for the native
    # call itself when needed.
    $solutionFile = [System.IO.Path]::GetFullPath((Join-Path $schemasRoot "../$SlnFileName"))
    $arguments = @('sln', $solutionFile, 'add', $ProjectPath)
    Write-Host "dotnet $($arguments -join ' ')"
    $output = & dotnet @arguments 2>&1
    $output | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "dotnet exited with code $LASTEXITCODE"
    }
}

function Add-ProjectRefToSchemasProj([string]$CsProjLocation, [string]$ProjectRefStr) {
    $xdoc = [xml](Get-Content -LiteralPath $CsProjLocation -Raw)

    # The ProjectReference to the XSD library is used as a pointer to the right ItemGroup.
    $anchor = $null
    foreach ($projectRef in $xdoc.SelectNodes("//*[local-name()='ProjectReference']")) {
        if ($projectRef.GetAttribute('Include') -eq '..\GeneratedSchemaLibraries\XSD\XSD.csproj') {
            $anchor = $projectRef
            break
        }
    }
    if (-not $anchor) {
        throw "Could not find the XSD ProjectReference in '$CsProjLocation'."
    }
    $itemGroup = $anchor.ParentNode

    # Skip when a ProjectReference with the same Include already exists.
    foreach ($projectRef in $xdoc.SelectNodes("//*[local-name()='ProjectReference']")) {
        if ($projectRef.GetAttribute('Include') -eq $ProjectRefStr) { return }
    }

    $newProjectRef = $xdoc.CreateElement('ProjectReference')
    $newProjectRef.SetAttribute('Include', $ProjectRefStr)
    [void]$itemGroup.AppendChild($newProjectRef)

    $xdoc.Save($CsProjLocation)
    Write-Host "Was added to $CsProjLocation : $ProjectRefStr"
}

$schemaDirs = @(Get-ChildItem -LiteralPath $schemasRoot -Directory)
if ($Directories) {
    $schemaDirs = @($schemaDirs | Where-Object { $Directories -contains $_.Name })
}

$skipCount = 0
foreach ($dir in $schemaDirs) {
    $possibleCsprojs = @(Get-ChildItem -LiteralPath $dir.FullName -File -Filter '*.csproj' -Recurse)
    if ($possibleCsprojs.Count -gt 0) {
        $skipCount++
        continue
    }

    Write-Host "Found $($dir.FullName)"

    $lastFolderName = $dir.Name
    Copy-AndFilloutTemplate $schemasRoot $schemasRoot $lastFolderName

    # ProjectReference Include paths in the schemas csproj use backslashes, which MSBuild
    # accepts on every platform, so keep them exactly as the original script wrote them.
    $relativePath = "..\GeneratedSchemaLibraries\$lastFolderName\$lastFolderName.csproj"
    Add-ProjectRefToSchemasProj $schemasCsProjPath $relativePath

    $projectPath = Join-Path (Join-Path $schemasRoot $lastFolderName) "$lastFolderName.csproj"
    Add-ProjectToSln $projectPath
    Add-ProjectToSln $projectPath 'LinqToXsd-TestingSuite.slnf'
}

if ($skipCount -eq $schemaDirs.Count) {
    Write-Host 'None processed'
}
