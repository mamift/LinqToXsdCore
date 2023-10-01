# regenerate all CS code with this one command
Get-ChildItem -Attributes Directory | %{ LinqToXsd gen $_ -a }