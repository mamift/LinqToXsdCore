
# rebuild 

class ProcessResult {
    [System.Diagnostics.Process] $Process;
    [string] $Output;
    [string] $Errors;
    [int] $ReturnCode;
}

function build {
    [OutputType([ProcessResult])]
    param(
        [string]$path,
        [string]$args
    )
    $dnb = (Get-Command dotnet)

    if ([System.IO.File]::Exists($path) -eq $false) {
        throw new Exception("path does not exist!");
    }

    #write-host $dnb.Path

    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = $dnb.Path;
    $pinfo.RedirectStandardError = $true
    $pinfo.RedirectStandardOutput = $true
    $pinfo.UseShellExecute = $false
    $theArgs = "build $($path) -c DEBUG -v:normal";
    if ([string]::IsNullOrWhiteSpace($args) -eq $false) {
        $theArgs = "$($theArgs) $($args)";
    }

    #write-host $theArgs;
    $pinfo.Arguments = $theArgs
    
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo

    [string]$stdout = [string]::Empty;
    [string]$stderr = [string]::Empty;
    $outputCopy = {
        params([object]$sender, [System.Diagnostics.DataReceivedEventArgs]$eventArgs)

        write-host $eventArgs.Data;
        $stdout += $eventArgs.Data;
    };
    
    Register-ObjectEvent -InputObject $p -EventName "OutputDataReceived" -Action $outputCopy;

    $errorCopy = {
        params([object]$sender, [System.Diagnostics.DataReceivedEventArgs]$eventArgs)

        write-host $eventArgs.Data;
        $stderr += $eventArgs.Data;
    };
    
    Register-ObjectEvent -InputObject $p -EventName "ErrorDataReceived" -Action $errorCopy;

    $p.Start();
    $p.BeginErrorReadLine();
    $p.BeginOutputReadLine();

    $didExit = $p.WaitForExit();
    if ($didExit -eq $false) {
        write-error "error";
        return;
    }

    #write-host $stdout;
    
    return [ProcessResult]@{
        Process = $p
        Output = $stdout
        Errors = $stderr
        ReturnCode = $p.ExitCode
    };
}

class BuildResult {
    [string]$Name; 
    [string]$Output;
    [int]$ReturnCode;

    [string] ToString() {
        
        return "Name = $($this.Name), ReturnCode = $($this.ReturnCode)";
    }
}

$buildResults = New-Object System.Collections.Generic.List[BuildResult];
$dnb = Get-Command dotnet

Get-ChildItem -Attributes Directory | % {
	if ($_ -contains "Microsoft Project 2007") { return; }
	$projects = [System.IO.Directory]::GetFiles($_.FullName, "*.csproj");
    $first = $projects[0];

    write-host "Building $first...";

    if ($first -ne $null) {
        $rc = build -path $first

        $result = [BuildResult]::new();
        $result.ReturnCode = $rc.ReturnCode;
        $result.Name = $first;
        
        if ($rc.ReturnCode -eq 1) {
            write-host $rc.Errors;
            return;
        } else {
            write-host $result.ToString();
        }

        $result.Output = $rc.Output;

        $buildResults.Add($result);
    } else {
        Write-Error "Unable to find CSPROJ inside folder $($_.Name)";
    }
}

echo $buildResults | select Name,ReturnCode;
