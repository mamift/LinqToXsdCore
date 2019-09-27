# LinqToXsdCore Release Notes

## XObjectsCore 3.0.0.4 and LinqToXsdCore 3.0.0.6
Nuget packages: 
* https://www.nuget.org/packages/XObjectsCore/3.0.0.4
* https://www.nuget.org/packages/LinqToXsdCore/3.0.0.6

Fixes a bug that caused XTypedElement.Clone() to fail when generated code had the `internal` visibility modifier. This manifested in the CLI tool, when attempting to use it to generate an example configuration file `linqtoxsd config 'file.xsd' -e`.