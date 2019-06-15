# LinqToXsd

This project forms the command-line interface utility used to generate C# code that represents the structure of an XML document that conforms to an [W3C XML Schema (XSD)](https://www.w3.org/standards/xml/schema). 

Most of the logic for reading an XSD is coded in the `XObjectsCore` project, but logic that is specific to an individual XSD (like custom types and elements), is generated as C# code using the CodeDOM provider (also inside `XObjectsCore`) that is invoked via this CLI tool.

The code this tool generates has a dependency on namespaces defined in the `XObjectsCore` library. You can add a reference to that library via nuget here: https://www.nuget.org/packages/XObjectsCore 

**Don't add this project as a dependency to your shipping libraries or app - depend on the code it generates, and the XObjectsCore library instead.**

This project adds additional features from the old CLI tool (the [XObjectsGenerator](https://github.com/mamift/LinqToXsdCore/tree/master/XObjectsGenerator) project is the legacy CLI), like generating `.config` files with default values from an existing XSD and generating code from multiple XSD's and multiple `.config` files.

## Installation

This tool is published as a global .NET tool here: https://www.nuget.org/packages/LinqToXsdCore. Install it using 
```
dotnet tool install LinqToXsdCore -g
```

After that, you can use the tool globally via the command 'LinqToXsd' at a console (`cmd` or `powershell`).

## Using LinqToXsd to generate code

Before generating code, create an example configuration file from your XSD (this will allow you to customize how XML namespaces map to .NET CLR namepsaces).

```
linqtoxsd config -e wss.xsd
```

Currently, as this is a port of the LinqToXsd project, some configuration elements are not documented but for some reason or another are processed and interpreted in source code. But the gist of what you need as a developer is to pay attention to the ``<Namespaces />`` element. 

Under that element, it will create default namepsace mappings similar to the following example: 

```XML
<Namespaces>
  <Namespace Schema="http://schemas.microsoft.com/sharepoint/" Clr="schemas.microsoft.com.sharepoint" />
</Namespaces>
```

The XML namespace ``http://schemas.microsoft.com/sharepoint/`` becomes: ``schemas.microsoft.com.sharepoint`` in the generated C# code. And obviously now that you have a configuration file you can change the default values to something more suitable for you.

To use your new configuraton file to generate code:

```
linqtoxsd gen wss.xsd -c .\wss.xsd.config
```

It will output code to *'file.xsd.cs'*, or in this case *'wss.xsd.cs'*.

### Regenerating code

You can also add a pre-build event in your Visual Studio project definition, invoking the `linqtoxsd` tool to regenerate code every time you build your solution:

```sh
linqtoxsd gen "$(ProjectDir)wss.xsd" -c "$(ProjectDir)wss.xsd.config"
```

In the above example, the strings beginning with `$()` are MSBuild macros.

### Help for other functions

There's some other miscellaneous functions in the CLI tool that may be helpful to you, and be used to exert finer control over code output. To view the help screen for this tool: 

``linqtoxsd help``

To view help for a specific verb: 

``linqtoxsd <verb> --help``

To view help on configuration file generation:

``linqtoxsd config --help``