# LinqToXsdCore
## Introduction
This is a port of [LinqToXsd](https://archive.codeplex.com/?p=linqtoxsd) to .NET Core. Most of what was in the original project is here, but built for .NET Core! For people who specifically need .NET 4/3.5 support, please use the original code on the [codeplex archive](https://archive.codeplex.com/?p=linqtoxsd). There's also a legacy [nuget package](https://www.nuget.org/packages/LinqToXsd/).

![Build Status](https://dev.azure.com/mamift1/LinqToXsdCore/_apis/build/status/LinqToXsdCore-.NET%20Desktop-CI) ![Nuget](https://buildstats.info/nuget/LinqToXsdCore)

## Get started

You can get started by reading the [instructions here](https://github.com/mamift/LinqToXsdCore/tree/master/LinqToXsd/README.md) to use the CLI tool to generate code (and use said code in a shipping app or library).

### Wait so what is this?
LinqToXsd was first released back in 2009, and it was billed then as a way of '*providing .NET developers with support for typed XML programming...LINQ to XSD enhances the existing LINQ to XML technology*'.

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
With LinqToXsd you can instead write:
```C#
var po = purchaseOrder.Load("po.xml");
var order = po.order;
var city = order.city;
```

The amount of code one writes to traverse an XML document is reduced as LinqToXsd builds a strongly-typed model for you through its code generator. This makes LinqToXsd incredibly helpful when dealing with XML data, especially if it comes with an accompanying XSD file, as most XML formats tend to do nowadays.

You can also use LinqToXsd to create XML documents programmatically using the object API:

```C#
var newPo = new PurchaseOrder();
newPo.order = new Order();
order.city = "city1";
// now save
newPo.Save("newPo.xml");
```

Even if there isn't a native XSD, you can infer an XSD from an existing XML file and speed up your development that way.

### Things not supported in this .NET Core port

* No assembly generation - due to a dependency on CodeDOM, no direct code-generation (i.e. emitting .DLL files) is supported. CodeDOM for .NET Core does not support this on any platform [(even Windows)](https://github.com/dotnet/corefx/issues/12180).

* Custom build action - the Visual Studio targets project (which allowed feeding an XSD and configuration file as a custom build action) has not been ported and there are no plans to do so at the moment. This was decided because the code generation utility can be installed as a global tool using `dotnet`. Regenerating code on build can be automated by adding a Visual Studio pre-build event ([see instructions here](https://github.com/mamift/LinqToXsdCore/blob/master/LinqToXsd/README.md#regenerating-code)). 

# License
This is licensed under the same license that the original LinqToXsd project was licensed under, which is the Microsoft Public License (MS-PL): https://opensource.org/licenses/MS-PL