$linqtoxsd = "C:\Projects\GitHub\LinqToXsdCore\LinqToXsd\bin\Debug\net10.0\LinqToXsd.exe";
# regenerate all CS code with this one command
Get-ChildItem -Attributes Directory | %{ & $linqtoxsd gen $_ -a }