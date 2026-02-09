
$cwd = [Environment]::CurrentDirectory;

$linqToXsdProject = [System.IO.Path]::Combine($cwd, "..\LinqToXsd\LinqToXsd.csproj")

# regenerate all CS code with this one command
Get-ChildItem -Attributes Directory | % {    
    dotnet run --no-launch-profile --framework net8.0 --project $linqToXsdProject -- gen $_ -a
    #LinqToXsd gen $_ -a 
}