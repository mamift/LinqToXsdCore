# rebuild 

function start($path, $args) {
    if ([System.IO.File]::Exists($path) -eq $false) {
        throw new Exception("Erorr, path does not exist!");
    }

    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = $path;
    $pinfo.RedirectStandardError = $true
    $pinfo.RedirectStandardOutput = $true
    $pinfo.UseShellExecute = $false
    $pinfo.Arguments = "localhost"
    
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo
    $p.Start() | Out-Null
    $p.WaitForExit()
    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    Write-Host "stdout: $stdout"
    Write-Host "stderr: $stderr"
    Write-Host "exit code: " + $p.ExitCode

    return $p;
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
	if ($_ -eq "Microsoft Project 2007") { return; }
	$projects = [System.IO.Directory]::GetFiles($_.FullName, "*.csproj");
    $first = $projects[0];

    if ($first -ne $null) {
        $rc = start -path $dnb.Path -args "$($first) -c DEBUG -v:minimal";

        $result = [BuildResult]::new();
        $result.ReturnCode = $rc.ExitCode;
        $result.Name = $first;
        write-host $result.ToString();
        
        $result.Output = $rc.StandardOutput.ReadToEnd();
        $buildResults.Add($result);
    } else {
        Write-Error "Unable to find CSPROJ inside folder $($_.Name)";
    }
}


echo $buildResults;