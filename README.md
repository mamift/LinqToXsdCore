# LinqToXsdCore
## Introduction
This is a port of [LinqToXsd](https://archive.codeplex.com/?p=linqtoxsd) to .NET Core. Most of what was in the original project is here, but built for modern .NET! For people who specifically need .NET Framework 3.5 and 4.0-4.5 support, please use the original code on the [codeplex archive](https://archive.codeplex.com/?p=linqtoxsd). There's also a legacy [nuget package](https://www.nuget.org/packages/LinqToXsd/).

This .NET Core port itself requires .NET Core 2.1 or 3.1, but it can generate code that is compatible with .NET Framework 4.6.x and .NET Core 2.x and later (this includes .NET 5 and later).

![Build Status](https://dev.azure.com/mamift1/LinqToXsdCore/_apis/build/status/LinqToXsdCore-.NET%20Desktop-CI) ![Nuget](https://buildstats.info/nuget/LinqToXsdCore)

## Get started

You can get started by reading the [instructions here](https://github.com/mamift/LinqToXsdCore/tree/master/LinqToXsd/README.md) to use the CLI tool to generate code. 

After you've generated code for a given XSD, you can include the generated code in a shipping app or library, so long as it has a reference to the [XObjectsCore nuget package](https://www.nuget.org/packages/XObjectsCore). Don't add the **LinqToXsdCore** nuget package to your shipping app or library! That's just a command line tool to generate code.

### Release notes are [here](https://github.com/mamift/LinqToXsdCore/tree/master/LinqToXsd/RELEASENOTES.md).

Many bug fixes have been made since it was ported to .NET core in 2019; if you've encountered an issue with the tool, make sure you are up to date. The complete list of release notes from version to version is here: [RELEASENOTES.md](RELEASENOTES.md)

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

## Best practices

### 1. Include the generated source code in git or source control!

NOTE: Ignoring *.xsd.cs files is a default when you use Visual Studio's [.gitignore file](https://github.com/github/gitignore/blob/main/VisualStudio.gitignore) - please remove the *.xsd.cs line from your gitignore file if this is present.

It's come to my attention that some users are **ignoring the generated code** output by LinqToXsdCore in git or their chosen source control system. It seems to be a common setup to generate the code when needed, but ignore it's output in git, despite the code being compiled into DLLs and assemblies that make up part of the app.

**PLEASE DO NOT DO THIS.** Do not assume the code output of LinqToXsdCore is perfect or beyond fault. It is ultimately a program written by humans and thus not without flaws.

Any code that makes it to production, whether it's generated by a program or written by a human or AI, SHOULD BE TRACKED IN SOURCE CONTROL. Besides the obvious reasons (like production system code not being managed by source control), the code generated by LinqToXsdCore is NOT PERFECT. Like any program, it has bugs, and those bugs can sometimes manifest as subtle code generation mistakes.

IF YOU ARE IGNORING THE CODE, THERE'S NO WAY TO COMPARE AND CONTRAST CHANGES FROM VERSION TO VERSION AND THIS MAKES TROUBLESHOOTING THE ORIGIN OF THE BUG **MUCH HARDER**.

This applies to **any** code generation tool, like [nswag](https://github.com/RicoSuter/NSwag) or [WCF's svcutil.exe](https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/understanding-generated-client-code); YOU SHOULD ALWAYS TRACK CODE THAT ENDS UP IN A PRODUCTION SYSTEM. 

Consider this scenario: 
- You use LinqToXsdCore to generate code that is deployed in AppA v1, and you ignore the generated code in git.
- Later on you push an update to AppA, v1.5 and in so doing, you update the LinqToXsdCore version as well.
  - This newer version of LinqToXsdCore changes generated code slightly, but doesn't break the build process.
- Due to changes in LinqToXsdCore, runtime behavior of your app has changed. You can see the *current* version of code being generated, but because you weren't tracking any generated code in git, you have no visibility of changes that might've occurred from the previous version; *there's no delta or changeset to compare from v1 to v1.5 in git.*
  - While you did read the release notes, it doesn't occur to you that the changes in this newer version actually affected you, as you didn't realise you were indirectly using this particular API (it's nested several level deeps in method calls), so you miss this behavior change.
- You only notice when some awful bug occurs in production, you spend a few days troubleshooting the issue, and it only occurs to you after many hours that maybe the issue was a dependency, and not code personally written by you or your team.
- You take a couple stabs in the dark, and when you finally realise as part of your changes to v1.5, you updated LinqToXsdCore. You rollback LinqToXsdCore, re-run the code gen steps and see the code has changed slightly - you run another build and deploy again to staging environment, reproduce the bug and then suddenly everything is working now! Only after all this effort have you finally fixed it!

The above isn't a hypothetical, it's a real world scenario related to me by a colleague who had used LinqToXsdCore in their projects. A very similar incident happened to me, except I was using nswag for OpenAPI code generation.

During troubleshooting, a lot of time can be saved when you **track all production code**. 

Also consider this: a lot of code is now generated by AI - would you ignore the same code in git? AI is trained on human-generated code, so AI code would be prone to contain bugs as well, so of course you track AI code. Yet for some reason, the attitude is very different when code is generated by a source-generator or other non-AI program, despite the obvious fact that those programs themselves are built and maintained by other humans, and so would contain bugs.

Yes, the output of code gen tools like LinqToXsdCore can be big and verbose, but the tradeoff to tracking them, is gaining greater visibility and the ability to troubleshoot & diagnose bugs quickly. Just follow this one principle and remember it like it is an axiom: **IF IT MAKES IT TO PRODUCTION, IT NEEDS TO BE TRACKED.**

# License
This is licensed under the same license that the original LinqToXsd project was licensed under, which is the Microsoft Public License (MS-PL): https://opensource.org/licenses/MS-PL