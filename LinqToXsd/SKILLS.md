# Skills: Using the LinqToXsd CLI to generate code

**Applies to:** the `LinqToXsd` project (published to NuGet as [`LinqToXsdCore`](https://www.nuget.org/packages/LinqToXsdCore)).

This skill describes how to use the CLI to generate strongly-typed C# classes from W3C XML Schema (XSD) files.

## When to use this skill

Use it whenever a task involves:

- generating `*.xsd-g.cs` files from `.xsd` schemas,
- creating or editing `.xsd.config` configuration files

## Background

The tool reads XSD files via `XmlSchemaSet` and emits C# classes that inherit from `XTypedElement` (from the `XObjectsCore` runtime library). Each generated class models an XML element or type so documents can be read and written through a strongly-typed `XDocument`-based API. Only source files are produced — this port does not emit assemblies.

## Getting the tool

Install it as a global .NET tool:

```bash
dotnet tool install LinqToXsdCore -g
```

Then invoke it as `linqtoxsd` (`LinqToXsd` at a console). When working on this repository, run it straight from the project instead:

```bash
dotnet run --project LinqToXsd -- <verb> <args...>
```

`linqtoxsd help` prints the overall help screen; `linqtoxsd <verb> --help` prints help for a specific verb.

## Step 1 — (Optional) Create a configuration file

A `.xsd.config` file customises how XML namespaces map to CLR namespaces. Generate one from a schema with the `config` verb:

```bash
linqtoxsd config -e wss.xsd
```

This writes `wss.xsd.config` next to the XSD (overwriting any existing file) with default values, for example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Configuration xmlns="http://www.microsoft.com/xml/schema/linq">
  <Namespaces>
    <Namespace DefaultVisibility="public" Clr="schemas.microsoft.com.sharepoint" Schema="http://schemas.microsoft.com/sharepoint/" />
  </Namespaces>
  <NullableReferences>false</NullableReferences>
  <Validation>
    <VerifyRequired>false</VerifyRequired>
  </Validation>
</Configuration>
```

The `<Namespaces>` element is the part that matters day to day: `Schema` is the XML namespace URI and `Clr` is the CLR namespace it maps to — edit `Clr` to suit your project. Some other configuration elements are processed by the tool but are not fully documented (this is a port of the original LinqToXsd).

`config -e` also accepts folders (one config per XSD inside) and multiple comma-separated inputs; add `-o` to write a single merged example config instead, e.g. `linqtoxsd config -e -o egConfig.xml`.

## Step 2 — Generate code

```bash
linqtoxsd gen wss.xsd -c wss.xsd.config
```

This emits `wss.xsd-g.cs` next to the XSD. Inputs may be files, folders, or a comma-separated mix:

```bash
linqtoxsd gen schemas/file1.xsd,schemas/file2.xsd
linqtoxsd gen schemas/ -o Generated/
```

Behaviour to know:

- **Config discovery:** if no `-c` flag is given and `<schema>.xsd.config` exists next to the XSD, it is used automatically (as of v3.4.24, the old `-a` auto-association behaviour is always on). If no config file is found, default settings apply. Passing `-a` explicitly is still accepted and additionally auto-deletes legacy `*.xsd.cs` output files before writing; `-c` and `-a` are mutually exclusive.
- **`-c` with a folder** loads and merges every config file in that folder into one.
- **Output selection:** `-o` ending in `.cs` merges all generated code into that single file; `-o` naming a folder writes one `*.xsd-g.cs` per XSD into it; no `-o` writes next to each input XSD.
- **`gen -e`** imports the `System.Xml.Serialization` namespace into the generated code (and overrides the `EnableServiceReference` setting from any config file). Note that `-e` means something different on the `config` verb (generate example config).
- When folders are given, files referenced by `xs:include`/`xs:import` directives are not generated twice.
- Schema files are read with DTD processing enabled (`DtdProcessing.Parse`).
- Exit code is `0` on success, `1` on argument errors.

Legacy `.xsd.cs` files (pre-v3.4.17 output) are not overwritten: the tool prints a warning telling you to delete them, as they would define the same types in the same assembly.

## Regenerating on every build

Add a pre-build event to the consuming project:

```sh
linqtoxsd gen "$(ProjectDir)wss.xsd" -c "$(ProjectDir)wss.xsd.config"
```

(The strings beginning with `$()` are MSBuild macros.)

## Using the generated code

The generated `*.xsd-g.cs` files compile against the [`XObjectsCore`](https://www.nuget.org/packages/XObjectsCore) runtime package — add that reference to any project that includes generated code. Never add the `LinqToXsdCore` CLI package to a shipping project; depend on the generated code (and `XObjectsCore`) instead.

## Summarising generated code

The `sum` verb (short for summarise) writes a markdown summary of the types defined in generated code files:

```bash
linqtoxsd sum Generated/wss.xsd-g.cs
linqtoxsd sum Generated/      # one folder of .xsd-g.cs files
```

It accepts one or more `.xsd-g.cs` file paths, or a single folder of them (not both), and writes `<filename>.md` next to each input file (or into a folder given with `-o`). For each class the summary records the declaration (base types and interfaces included), plus all public/protected/internal methods, properties and constructors, shortened to signatures with bodies omitted; private members are excluded. Enums are listed with their values.

Because each file is parsed with Roslyn (at C# 8), any syntax error in the generated code makes the command fail with a non-zero exit code — `sum` doubles as a quick validity check on generated output.

## Working in this repository

- **Do not edit `*.xsd-g.cs` files by hand** — they are regenerated by the tool. Changes belong in the code generation logic in `XObjectsCode/`.
- `LinqToXsd.Schemas` references all `GeneratedSchemaLibraries` projects, and `XObjectsTests` references that — invalid generated C# breaks the test project's build, so the generated libraries act as a compile-time gate.
- Generated output must remain compatible with C# 7.3 / .NET Standard 2.0 for .NET Framework consumers.
- To regenerate the committed schemas after changing generation logic, run the `gen` verb from the repo root, e.g. `dotnet run --project LinqToXsd -- gen GeneratedSchemaLibraries/...`.
