# LinqToXsdCore
## Introduction
This is a port of [LinqToXsd](https://archive.codeplex.com/?p=linqtoxsd) to .NET Core. Most of what was in the original project is here, but built for .NET Core! For people who specifically need .NET 4/3.5 support, please use the original code on the [codeplex archive](https://archive.codeplex.com/?p=linqtoxsd).

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

## Status
Everything, except the unit tests and the Visual Studio targets project is here. The VS target (that allows feeding an XSD in a solution or project to a custom build action) is planned to be ported later on. 

Currently to generate code you have to invoke the *XObjectsGenerator* program.

## Mini-instructions
This is a .NET Core app; after building the entire solution in Visual Studio 2017, navigate to the output folder for the XObjectsGenerator project *(XObjectsGenerator\bin\Debug\netcoreapp2.1)* and invoke the CLI tool:
```
dotnet .\XObjectsGenerator.dll
```
You'll see a help screen explaining all the arguments it accepts (it's the same as the original CLI tool).

# License
This is licensed under the same license that the original LinqToXsd project was licensed under, which is the Microsoft Public License (MS-PL): https://opensource.org/licenses/MS-PL