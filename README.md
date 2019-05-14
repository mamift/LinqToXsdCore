# LinqToXsdCore
## Introduction
This is a port of [LinqToXsd](https://archive.codeplex.com/?p=linqtoxsd) to .NET Core. Most of what was in the original project is here, but built for .NET Core! For people who specifically need .NET 4/3.5 support, please use the original code on the [codeplex archive](https://archive.codeplex.com/?p=linqtoxsd).

![Build Status](https://dev.azure.com/mamift1/LinqToXsdCore/_apis/build/status/LinqToXsdCore-.NET%20Desktop-CI)

### Wait so what is this?
LinqToXsd was first released back in 2009, and it was billed then as a way of '*provding .NET developers with support for typed XML programming...LINQ to XSD enhances the existing LINQ to XML technology*'.

Basically LinqToXsd generates code at design time, which models the structure of a XML document according, according to a [W3C XML Schema (XSD)](https://www.w3.org/TR/xmlschema11-1/). This allows a developer to program against the generated code, using strong types, where classes represents element definitions and class properties (i.e. getters and setters) represent attributes on XML elements.

Consider this XML:
```XML
<?xml version="1.0" encoding="UTF-8"?>
<purchaseOrder xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" 
    xsi:noNamespaceSchemaLocation="./Purchase Order.xsd">
    <general>
        <poNum>poNum0</poNum>
        <poDate>2006-05-04</poDate>
    </general>
    <order>
        <companyName>companyName0</companyName>
        <address>address0</address>
        <city>city0</city>
        <stateProv>stateProv0</stateProv>
        <zipCode>zipCode0</zipCode>
        <country>country0</country>
        <phone>phone0</phone>
        <fax>fax0</fax>
        <contactName>contactName0</contactName>
    </order>
</purchaseOrder>
```
To get the `<city>` element with regular LINQ to XML code, you'd write:
```C#
var purchaseOrderDoc = XDocument.Load("po.xml");
var orderElement = purchaseOrderDoc.Descendants(XName.Get("order"));
var cityElement = orderElement.Descendants(XName.Get("city"));
```
With LinqToXsd you would write:
```C#
var po = purchaseOrder.Load("po.xml");
var order = po.order;
var city = order.city;
```

The amount of code one writes to traverse an XML document is reduced as LinqToXsd builds a strongly-typed model for you through its code generator. This makes LinqToXsd incredibly helpful when dealing and handling XML data, especially if it comes with an accompanying XSD file, as most XML formats tend to do. Even if there isn't a native XSD, you can just infer an XSD from an existing XML file and speed up your development that way.

### Things not supported

* No assembly generation - due to a dependency on CodeDOM, no direct code-generation (making .DLL's) is supported. CodeDOM for .NET Core does not support this on any platform [(even Windows)](https://github.com/dotnet/corefx/issues/12180)

* The Visual Studio targets project is in the repo, but is not properly ported over to .NET core. The VS target (that allows feeding an XSD in a solution or project to a custom build action) is planned to be ported later on. 

## Mini-instructions

There are two CLI apps present in this repo: *LinqToXsd* and *XObjectsGenerator*. Both are .NET core console apps:

* XObjectsGenerator is a straight-up copy and paste of the legacy CLI tool. Use this if you have custom build events that use its syntax.
* LinqToXsd uses a [custom CLI parser](https://github.com/commandlineparser/commandline). It provides a better interface for functions that are explicitly supported.
* Both of these tools target .NET Core 2.0.

To invoke either of the CLI tools:
```
dotnet .\XObjectsGenerator.dll
```
or 

```
dotnet .\LinqToXsd.dll
```

### Pre-release Nuget package

To use the pre-release Nuget package, install LinqToXsdCore (it has a dependency on XObjectsCore and will install that as well). Then use the CLI tool generate a configuration file for your XSD files first before generating code.

After that, and editing the config file so namespaces map properly to the CLR, add an invocation to the CLI tool as a pre-build event into your project:

Visual Studio 2017+
```
dotnet %userprofile%\.nuget\packages\LinqToXsdCore\lib\netcoreapp2.0\XObjectsCore.dll <commandlineargs>
```

If you're using Visual Studio 2017 or above you won't have a solution-scoped packages folder. It gets downloaded to your user profile nuget repository.

Visual Studio 2015 and below:
```
dotnet $(SolutionDir)packages\LinqToXsdCore\lib\netcoreapp2.0\XObjectsCore.dll <commandlineargs>
```

## Using LinqToXsd

We recommend you use this tool to generate code. To generate code from an XSD (will output to **filename.xsd.cs**):
```
dotnet LinqToXsd.dll gen "SharePoint2010\wss.xsd"
```

To generate code from an XSD with a configuration file (that maps XML namespaces to CLR namespace):
```
dotnet LinqToXsd.dll gen "SharePoint2010\wss.xsd" --Config "SharePoint2010\wss.xsd.config"
```

# License
This is licensed under the same license that the original LinqToXsd project was licensed under, which is the Microsoft Public License (MS-PL): https://opensource.org/licenses/MS-PL