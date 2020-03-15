# LinqToXsdCore Release Notes

## LinqToXsdCore 3.0.1 and XObjectsCore 3.0.1
Nuget packages:
* https://www.nuget.org/packages/LinqToXsdCore/3.0.1
* https://www.nuget.org/packages/XObjectsCore/3.0.1
	* Fixed [Github Issue #10](https://github.com/mamift/LinqToXsdCore/issues/10)
	* Imported code changesets for v2.0.2 from the legacy [LinqToXsd project](https://archive.codeplex.com/?p=linqtoxsd), which generates the proper type definitions for union types.
	* Switched to a tripartite versioning scheme.
	* The global tool LinqToXsd now targets .NET Core 3.1 in addition to .NET Core 2.1. This allows users using either version of .NET Core to still use the global tool to generate code.

## LinqToXsdCore 3.0.0.12 and XObjectsCore 3.0.0.11
Nuget packages:
* https://www.nuget.org/packages/LinqToXsdCore/3.0.0.12
* https://www.nuget.org/packages/XObjectsCore/3.0.0.11
	* When a group of XSD files or a folder of them all import or include each other, LinqToXsd cannot decide which one to use as the entry point for code generation, so now the CLI throws an exception when that condition is met while trying to resolve which XSD file to use.
	* Reverts "Avoid type name conflicts in generated code" from previous release, as it broke the code generation of the `BuildWrapperDictionary()` method generated inside the `LinqToXsdTypeManager`; it adds `typeof(void)` expressions, which breaks untyped `XElement` type conversion. Previous (and correct) behavior was to add `typeof(T)` expressions where T was the generated complex or global element type.
	* Fixes an issue whereby setting a string value to an attribute whose type was `AnyAtomicType` resulted in an error.
	* Fixes an issue when using the static Parse() or Load() methods on an internal generated type.

## LinqToXsdCore 3.0.0.11 and XObjectsCore 3.0.0.10
Nuget packages:
* https://www.nuget.org/packages/LinqToXsdCore/3.0.0.11
* https://www.nuget.org/packages/XObjectsCore/3.0.0.10
	* Avoid type name conflicts in generated code. 
	* Do not prefix an identifier with the '@' character when not needed.

## XObjectsCore 3.0.0.9
Nuget packages: 
* https://www.nuget.org/packages/XObjectsCore/3.0.0.9

Added `XTypedElementEqualityComparer` and `XTypedElementDeepEqualityComparer` classes that implement `IEqualityComparer{T}` for the `XTypedElement` class.

## LinqToXsdCore 3.0.0.10 and XObjectsCore 3.0.0.8
Nuget packages: 
* https://www.nuget.org/packages/XObjectsCore/3.0.0.8
* https://www.nuget.org/packages/LinqToXsdCore/3.0.0.10

Modified the behaviour of retrieving the value of an attribute, when the schema type is anyAtomicType (which is the default for attributes when no type is given). The value literal is now returned as a string (pre-existing behaviour would throw an exception saying that anyAtomicType is not a supported conversion to the CLR type 'string').

## XObjectsCore 3.0.0.7
Nuget packages: 
* https://www.nuget.org/packages/XObjectsCore/3.0.0.7
	* Fixed a regression bug with previous release.

## LinqToXsdCore 3.0.0.9 and XObjectsCore 3.0.0.6
Nuget packages: 
* https://www.nuget.org/packages/XObjectsCore/3.0.0.6
	* Fixed an issue when performing an explicit type conversion from XElement to its XTyped-equivalent when the XTyped-equivalent type was an internal class.
* https://www.nuget.org/packages/LinqToXsdCore/3.0.0.9
	* Generating a new config file no longers includes the Xml.Schema.Linq schema namespace mapping. Also generating a new config file will generate a default namespace mapping when the XSD does not target a namespace. 

## LinqToXsdCore 3.0.0.8
Nuget packages: 
* https://www.nuget.org/packages/LinqToXsdCore/3.0.0.8
	* Implemented saving merged output from multiple XSD files when generating a config file (using 'config -e' switch) using a folder as a source.

## XObjectsCore 3.0.0.5 and LinqToXsdCore 3.0.0.7
Nuget packages: 
* https://www.nuget.org/packages/XObjectsCore/3.0.0.5
	* Reversed a change made that removed the virtual keyword on properties on generated types. Added a test for it.
* https://www.nuget.org/packages/LinqToXsdCore/3.0.0.7
	* Dropped emitting errors to a custom handler. Was outputting red console text needlessly.

## XObjectsCore 3.0.0.4 and LinqToXsdCore 3.0.0.6
Nuget packages: 
* https://www.nuget.org/packages/XObjectsCore/3.0.0.4
* https://www.nuget.org/packages/LinqToXsdCore/3.0.0.6

Fixes a bug that caused XTypedElement.Clone() to fail when generated code had the `internal` visibility modifier. This manifested in the CLI tool, when attempting to use it to generate an example configuration file `linqtoxsd config 'file.xsd' -e`.