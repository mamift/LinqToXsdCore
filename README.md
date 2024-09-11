# LinqToXsdCore
## Introduction
This is a port of [LinqToXsd](https://archive.codeplex.com/?p=linqtoxsd) to .NET Core. Most of what was in the original project is here, but built for .NET Core! For people who specifically need .NET Framework 3.5 and 4.0-4.5 support, please use the original code on the [codeplex archive](https://archive.codeplex.com/?p=linqtoxsd). There's also a legacy [nuget package](https://www.nuget.org/packages/LinqToXsd/).

This .NET Core port itself requires .NET Core 2.1 or 3.1, but it can generate code that is compatible with .NET Framework 4.6.x and .NET Core 2.x.

![Build Status](https://dev.azure.com/mamift1/LinqToXsdCore/_apis/build/status/LinqToXsdCore-.NET%20Desktop-CI) ![Nuget](https://buildstats.info/nuget/LinqToXsdCore)

## Get started

You can get started by reading the [instructions here](https://github.com/mamift/LinqToXsdCore/tree/master/LinqToXsd/README.md) to use the CLI tool to generate code. 

After you've generated code for a given XSD, you can include the generated code in a shipping app or library, so long as it has a reference to the [XObjectsCore nuget package](https://www.nuget.org/packages/XObjectsCore). Don't add the **LinqToXsdCore** nuget package to your shipping app or library! That's just a command line tool to generate code.

### Release notes are [here](https://github.com/mamift/LinqToXsdCore/tree/master/LinqToXsd/RELEASENOTES.md).

[RELEASENOTES.md](RELEASENOTES.md)

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

### How does this compare to xsd.exe?

The first most important difference is that `xsd.exe` will generate code  that leverages the old-school [XmlDocument](https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmldocument?view=netstandard-2.1) API. **LinqToXsdCore** will generate code that uses the newer [XDocument](https://learn.microsoft.com/en-us/dotnet/api/system.xml.linq.xdocument?view=netstandard-2.1)-based API under the `System.Xml.Linq` namespace.

LinqToXsd, ends up providing something very similar to the C# code-generation facilities that [`xsd.exe`](https://docs.microsoft.com/en-us/dotnet/standard/serialization/xml-schema-definition-tool-xsd-exe) provides. The main difference between the two is that LinqToXsd takes a code-generation, in-memory model and LINQ-only approach where as `xsd.exe` provides several legacy facilities such as XDR to XSD, XSD to [DataSet](https://docs.microsoft.com/en-us/dotnet/api/system.data.dataset), direct assembly generation, and can even do the reverse of what LinqToXsd does and generate an XSD from CLR types.

LinqToXsd also tries very closely to model XSD constraints and compositors (sequence, choice, all, substitution groups) and user defined types as much as possible, including simple and complex types, both named and anonymous. A key distinction is that LinqToXsd models XML elements and types with generated C# classes to build 'XML Objects', transposing XSD semantics in a CLR, object-oriented way. These XML objects inherit from the base class `XTypedElement`. 

Essentially LinqToXsd generates an in memory model of the XSD schema as opposed to the classes that `xsd.exe` generates, which are closer to plain old C# objects (POCOs). This has the end result of making LinqToXsd a very powerful tool for modeling custom document markup languages, and preserving schema semantics in code.

To get a more technical explanation of what LinqToXsd provides, please see the [wiki](https://github.com/mamift/LinqToXsdCore/wiki).

## Unsupported features compared to xsd.exe

There are some things that LinqToXsd does not support compared to `xsd.exe`:

* No [System.ComponentModel.DataAnnotations](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=netstandard-2.0) attribute decorations.
* No built-in [INotifyPropertyChanged](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged?view=netstandard-2.1) interface implementation: this needs to be implemented on your own.
* No selective element code generation; this tool generates code for the entire XSD, and not parts of it.
* No legacy [LINQ to Dataset](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/linq-to-dataset) support.
* No multi-language code generation; only supports C# code generation.

### Things not supported in this .NET Core port

* No assembly generation - due to a dependency on CodeDOM, no direct code-generation (i.e. emitting .DLL files) is supported. CodeDOM for .NET Core does not support this on any platform [(even Windows)](https://github.com/dotnet/corefx/issues/12180).

* Custom build action - the Visual Studio targets project (which allowed feeding an XSD and configuration file as a custom build action) has not been ported and there are no plans to do so at the moment. This was decided because the code generation utility can be installed as a global tool using `dotnet`. Regenerating code on build can be automated by adding a Visual Studio pre-build event ([see instructions here](https://github.com/mamift/LinqToXsdCore/blob/master/LinqToXsd/README.md#regenerating-code)). 

# License
This is licensed under the same license that the original LinqToXsd project was licensed under, which is the Microsoft Public License (MS-PL): https://opensource.org/licenses/MS-PL