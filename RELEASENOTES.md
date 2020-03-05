# LinqToXsdCore Release Notes

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