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

## Installation instructions

Before doing anything, install the code-generator, published as a global .NET tool here: https://www.nuget.org/packages/LinqToXsdCore/3.0.0.3-beta. Install it using 
```
dotnet tool install LinqToXsdCore --version 3.0.0.3-beta -g
```

After that, you can use the tool globally via the command 'LinqToXsd' at a console (cmd or powershell).

## Using LinqToXsd to generate code

Before generating code, create an example configuration file from your XSD (this will allow you to customize how XML namespaces map to C#/CLR namepsaces).

```
linqtoxsd config -e wss.xsd
```

Currently as this is a port of the LinqToXsd project, some configuration elements are not documented. But the gist of what you need as a developer is pay attention to the ``<Namespaces />`` element. 

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

### Using generated code

You can include the generated code a library DLL or other .NET application so long as the project that contains the generated code include a dependency on the XObjectsCore library. The nuget package for that is here: https://www.nuget.org/packages/XObjectsCore/3.0.0.1-beta. You do not need to include a reference to the LinqToXsdCore library in any shipping application or library.

## 

# License
This is licensed under the same license that the original LinqToXsd project was licensed under, which is the Microsoft Public License (MS-PL): https://opensource.org/licenses/MS-PL