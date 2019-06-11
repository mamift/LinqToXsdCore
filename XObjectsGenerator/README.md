# XObjectsGenerator
This project contains the legacy command line based generator for the original LinqToXsd project. Nothing about it is changed, aside from the fact it now runs on .NET Core. Because it targets .NET Core instead of the .NET Framework, use the ```dotnet``` tool to invoke the dll instead of running an .exe.

## Running the command line generator
* To see the help screen and available arguments:
    ```
    dotnet XObjectsGenerator.dll
    ```
* To generate C# code for an XSD using default settings
    ```
    dotnet XObjectsGenerator.dll AnXmlSchema.xsd
    ```
    And it will produce 'AnXmlSchema.cs' file in the same directory of the schema file.