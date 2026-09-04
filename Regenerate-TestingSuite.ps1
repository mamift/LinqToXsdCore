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
$timestamp = '{0:yyyyMMdd_HHmmss}' -f (Get-Date)
$logPath = Join-Path $repoRoot "RegenerateLog_$timestamp.log"
$assertLogPath = Join-Path $repoRoot "RegenerateAsserts_$timestamp.log"

#region Debug.Assert dialog watcher
# Debug builds pop up non-modal "Assertion Failed" dialogs from DefaultTraceListener.
# They block the child process until dismissed, so a background runspace scrapes their
# text into a log and clicks Ignore.

$assertDialogSource = @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class AssertDialogSweeper
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder buf, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassNameW(IntPtr hWnd, StringBuilder buf, int max);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_COMMAND = 0x0111;
    private const int IDIGNORE = 5;

    private static readonly HashSet<IntPtr> Seen = new HashSet<IntPtr>();

    private static string TextOf(IntPtr hWnd)
    {
        var sb = new StringBuilder(8192);
        GetWindowTextW(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string ClassOf(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassNameW(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    /// <summary>Returns the text of every newly-seen assertion dialog and dismisses each one.</summary>
    public static string[] SweepOnce()
    {
        var captured = new List<string>();

        EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
        {
            if (!IsWindowVisible(hWnd) || Seen.Contains(hWnd)) return true;
            if (ClassOf(hWnd) != "#32770") return true;

            string title = TextOf(hWnd);
            if (title.IndexOf("Assertion Failed", StringComparison.OrdinalIgnoreCase) < 0) return true;

            Seen.Add(hWnd);

            var body = new StringBuilder();
            body.AppendLine(title);
            EnumChildWindows(hWnd, delegate(IntPtr child, IntPtr l)
            {
                if (ClassOf(child) == "Static")
                {
                    string t = TextOf(child);
                    if (!string.IsNullOrWhiteSpace(t)) body.AppendLine(t);
                }
                return true;
            }, IntPtr.Zero);

            captured.Add(body.ToString());
            PostMessage(hWnd, WM_COMMAND, (IntPtr)IDIGNORE, IntPtr.Zero);
            return true;
        }, IntPtr.Zero);

        return captured.ToArray();
    }
}
'@

function Start-AssertDialogWatcher {
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][string]$Source,
        [int]$PollMilliseconds = 400
    )

    $state = [hashtable]::Synchronized(@{ Stop = $false; Count = 0 })Login failed
  PKIX path building failed: sun.security.provider.certpath.SunCertPathBuilderException: unable to find valid             certification path to requested target

    $runspace = [runspacefactory]::CreateRunspace()
    $runspace.Open()
    $runspace.SessionStateProxy.SetVariable('state', $state)
    $runspace.SessionStateProxy.SetVariable('logPath', $LogPath)
    $runspace.SessionStateProxy.SetVariable('source', $Source)
    $runspace.SessionStateProxy.SetVariable('pollMs', $PollMilliseconds)

    $ps = [powershell]::Create()
    $ps.Runspace = $runspace
    $null = $ps.AddScript({
        if (-not ('AssertDialogSweeper' -as [type])) {
            Add-Type -TypeDefinition $source -Language CSharp
        }
        while (-not $state.Stop) {
            foreach ($text in [AssertDialogSweeper]::SweepOnce()) {
                $state.Count++
                $entry = "=== Assertion dialog #$($state.Count) — $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===`r`n$text"
                Add-Content -Path $logPath -Value $entry -Encoding UTF8
            }
            Start-Sleep -Milliseconds $pollMs
        }
    })

    [PSCustomObject]@{
        PowerShell = $ps
        Runspace   = $runspace
        Handle     = $ps.BeginInvoke()
        State      = $state
    }
}

function Stop-AssertDialogWatcher {
    param($Watcher)

    if (-not $Watcher) { return 0 }

    $Watcher.State.Stop = $true
    $null = $Watcher.Handle.AsyncWaitHandle.WaitOne(5000)
    try { $Watcher.PowerShell.EndInvoke($Watcher.Handle) } catch { }
    $Watcher.PowerShell.Dispose()
    $Watcher.Runspace.Dispose()
    return $Watcher.State.Count
}
#endregion

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
}
finally {
    $assertCount = Stop-AssertDialogWatcher $assertWatcher
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Done. Succeeded: $succeeded, Failed: $($failed.Count)" -ForegroundColor Cyan

if ($assertCount -gt 0) {
    Write-Host "Dismissed $assertCount assertion dialog(s); details in: $assertLogPath" -ForegroundColor Yellow
}

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
