# Skills: Using the LinqToXsd CLI

**Applies to:** the `LinqToXsd` project (published to NuGet as [`LinqToXsdCore`](https://www.nuget.org/packages/LinqToXsdCore)).

This skill describes how to drive the CLI: reading W3C XML Schema (XSD) files, writing configuration files, and generating strongly-typed C# classes.

## When to use this skill

Use it whenever a task involves:

- generating (or regenerating) C# code from `.xsd` schemas (this will output `*.xsd-g.cs` files),
- creating or editing `.xsd.config` configuration files to control LinqToXsd code gen behaviour

## Background

The tool loads XSDs and emits C# classes that inherit from `XTypedElement` (in the `XObjectsCore` runtime library). Each generated class models an XML element or type so documents can be read and written through a strongly-typed `XDocument`-based API. Only C# source files are produced — this does not emit assemblies.

## Verbs at a glance

| Verb | Purpose |
| --- | --- |
| `gen` | Generate C# code from one or more XSDs. |
| `config` | Generate `.xsd.config` files that map XML namespaces to CLR namespaces. Only does anything with `-e`. |
| `sum` | Write a markdown summary of generated `.xsd-g.cs` files (short for *summarise*). |

`linqtoxsd help` prints the verb list; `linqtoxsd <verb> --help` prints verb options; `linqtoxsd version` prints the version.

## Getting the tool

Install it as a global .NET tool:

```bash
dotnet tool install LinqToXsdCore -g
```

Then invoke it as `linqtoxsd` (`LinqToXsd` on a console). When working on this repository, run it straight from the project instead:

```bash
dotnet run --project LinqToXsd -- <verb> <args...>
```

Pre-release versions are also available on this publicly accessible nuget feed (ONLY USE THIS VERSION WHEN INSTRUCTED OR TRYING TO DIAGNOSE A BUG WITH LINQTOXSDCORE):

```
dotnet tool install LinqToXsdCore --prerelease --add-source https://pkgs.dev.azure.com/mamift1/LinqToXsdCore/_packaging/LinqToXsdCore-AlphaBuilds/nuget/v3/index.json
```

## Quick start

```bash
# 1. Create a configuration file beside the schema (wss.xsd.config)
linqtoxsd config -e wss.xsd

# 2. Edit the <Namespaces> mapping in wss.xsd.config if the default CLR namespace is unsuitable

# 3. Generate code
linqtoxsd gen wss.xsd -c wss.xsd.config
```

Passing `-c` matters: a configuration file without a `<SplitCodeFiles />` element produces a single `<schema>.xsd-g.cs`, whereas a run that finds **no** configuration at all splits the output into one file per generated class. See [Output selection and naming](#output-selection-and-naming).

## Step 1 — create a configuration file

A `.xsd.config` file customises how XML namespaces map to CLR namespaces and how code is emitted. Generate one from a schema with the `config` verb and `-e` (for *example*):

```bash
linqtoxsd config -e wss.xsd
```

What `config -e` writes:

| Input | Output |
| --- | --- |
| One schema file | `<schema>.config` beside the schema, e.g. `wss.xsd` → `wss.xsd.config` |
| Several comma-separated schema files | One merged `LinqToXsdConfig.config` in the current directory |
| One or more folders | One `<schema>.config` beside every XSD found **recursively** under each folder |
| Any of the above plus `-o <path>` | A single merged config at `<path>`; `.config` is appended when missing, so this is a file name rather than a folder |

Existing configuration files are overwritten without prompting. `config` without `-e` does nothing.

> **Caveat:** at least one schema or folder path is required. `linqtoxsd config -e -o egConfig.xml` (the form advertised by `--help`) currently fails with *"There are no file or folder paths present in the enumerable!"*. Always pass a schema.

The generated file looks like this:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Configuration xmlns="http://www.microsoft.com/xml/schema/linq">
  <CodeGeneration>
    <UseDateOnly>false</UseDateOnly>
    <UseTimeOnly>false</UseTimeOnly>
    <UseDateTimeOffset>false</UseDateTimeOffset>
    <NullableReferences>false</NullableReferences>
    <!--Uncomment the below tag to split the generated code into multiple files: @By accepts 'Class' (one file per generated type) or 'Namespace' (one file per CLR namespace). Without it all generated code goes into one C# source code file.-->
    <!--<SplitCodeFiles By="Class" />-->
  </CodeGeneration>
  <Namespaces>
    <Namespace DefaultVisibility="public" Schema="http://schemas.microsoft.com/sharepoint/" Clr="schemas.microsoft.com.sharepoint" />
  </Namespaces>
  <NullableReferences>false</NullableReferences>
</Configuration>
```

### Configuration elements that matter

`<Namespaces>` is required — a config file without it fails to load. Each `<Namespace>` maps one XML namespace URI to one CLR namespace:

| Attribute | Meaning |
| --- | --- |
| `Schema` | The XML namespace URI (may be empty for a schema with no target namespace). |
| `Clr` | The CLR namespace the generated types are placed in. Required. Edit this to suit your project. |
| `DefaultVisibility` | `public` (default) or `internal`. Applies to generated types. |
| `File` | Output file name for this CLR namespace when using `<SplitCodeFiles By="Namespace" />`; otherwise ignored. |

Other elements processed by the generator:

- `<CodeGeneration>` — `UseDateOnly`, `UseTimeOnly`, `UseDateTimeOffset` (map XSD date/time types to the .NET 6 types instead of `DateTime`), `NullableReferences` (emit C# 8 nullable annotations), and `SplitCodeFiles`.
- `<Namespaces AlwaysPrefixGlobal="true">` — prefixes every generated `using` directive with `global::`.
- Top-level `<NullableReferences>` — legacy equivalent of the `<CodeGeneration>` element; prefer the latter.
- `<Validation>` and `<Transformation>` — read by the generator but effectively undocumented; leave them out.

These elements are **case-sensitive**. In particular `<SplitCodeFiles By="...">` accepts exactly `Class` or `Namespace`; anything else is treated as "do not split".

## Step 2 — generate code

```bash
linqtoxsd gen wss.xsd -c wss.xsd.config
```

### Inputs

The positional argument takes one or more schema files and/or folders, separated by commas. Files and folders may be mixed:

```bash
linqtoxsd gen schemas/file1.xsd,schemas/file2.xsd
linqtoxsd gen schemas/ -o Generated/
linqtoxsd gen schemas/file1.xsd,schemas/other-folder
```

Folders are searched recursively for `*.xsd`. Only the entry-point schemas are generated: files reached through `xs:include`/`xs:import` are not emitted a second time. XSD 1.1 schemas are not supported and are skipped with a warning (this check applies when no `-c` is given).

### Choosing a configuration

| Invocation | Configuration used |
| --- | --- |
| `-c <file>` | That one config, applied to every input. |
| `-c <folder>` | Every `.xml`/`.config` file under the folder (recursively), merged into one config. |
| No `-c` | `<schema>.xsd.config` beside each schema, where present; otherwise defaults. This association is always on (since v3.4.24) and no longer needs `-a`. |
| `-c <path>` that does not exist | Silently ignored. The tool then falls back to `<schema>.xsd.config` discovery, and to defaults if none are found. Check the path if output looks wrong. |

`-c` and `-a` are mutually exclusive. `-a` is a legacy flag retained for compatibility: it prints a note explaining that automatic association is always applied, and it makes the tool delete legacy `*.xsd.cs` output files before writing (without `-a`, such files are only reported).

### Output selection and naming

`-o` is either a file or a folder:

- **A file** — the value ends in `.cs` (and is not an existing folder). All generated code from all inputs is merged into that one file.
- **A folder** — anything else, including a folder that does not exist yet (it is created). One output file per generated unit is written into it.
- **No `-o`** — output is written next to each input XSD.

Naming rules:

| Situation | Output |
| --- | --- |
| Single-file output (with or without `-o`) | `<schema>.xsd-g.cs` next to the schema, or the exact `-o` file name. |
| `<SplitCodeFiles By="Class" />` | One `<TypeName>.xsd-g.cs` per generated type, e.g. `Action.xsd-g.cs`. |
| `<SplitCodeFiles By="Namespace" />` | One file per CLR namespace, named by the matching `<Namespace File="..." />` when present, else after the CLR namespace. |
| Split file names that two input schemas would both produce | Prefixed with the source schema name, e.g. `abstracttest.LinqToXsdTypeManager.xsd-g.cs`. Identical content is not duplicated. |

Which mode applies when:

| Configuration | `-o` | Result |
| --- | --- | --- |
| None found anywhere | Folder or absent | Split **by class** (the default). |
| Config found (`-c` or beside the schema) without `<SplitCodeFiles />` | Folder or absent | One `<schema>.xsd-g.cs` per schema. |
| Any configuration | File (`*.cs`) | One merged file; this overrides the default split-by-class. |
| Config sets `<SplitCodeFiles />` | File (`*.cs`) | **Error**: the run stops and asks you to pass a folder or remove the element. |

### Options

| Option | Meaning |
| --- | --- |
| `-c`, `--Config` | Config file or folder (see above). |
| `-a`, `--AutoConfig` | Legacy; see above. Also deletes legacy `*.xsd.cs` files. |
| `-e`, `--EnableServiceReference` | Adds `using System.Xml.Serialization;` to generated code and makes generated types implement `IXmlSerializable`. Note that `-e` means something different on the `config` verb (generate example config). |
| `-o`, `--Output` | Output file or folder. |

### Exit codes and diagnostics

- `0` on success.
- Runtime failures set `1` (release builds catch the exception and print it; debug builds let it propagate).
- Argument-parsing errors print the help screen but currently still exit `0`, so when scripting, treat unexpected output as failure rather than relying on the exit code alone.

## Regenerating on every build

Add a pre-build event to the consuming project:

```sh
linqtoxsd gen "$(ProjectDir)wss.xsd" -c "$(ProjectDir)wss.xsd.config"
```

(The strings beginning with `$()` are MSBuild macros.)

## Using the generated code

Generated `*.xsd-g.cs` files compile against the [`XObjectsCore`](https://www.nuget.org/packages/XObjectsCore) runtime package — add that reference to any project that includes generated code. Never add the `LinqToXsdCore` CLI package to a shipping project; depend on the generated code (and `XObjectsCore`) instead.

Generated output remains compatible with C# 7.3 / .NET Standard 2.0 for .NET Framework consumers. `#nullable` support is also enabled in these older targets when you install `Polysharp` from nuget.

```
dotnet add package PolySharp
```

PolySharp provides generated, source-only polyfills for C# language features, to easily use all runtime-agnostic features downlevel. The package is distributed as a source generator, so that it will automatically detect which polyfills are needed depending on the target framework and project in use: just add a reference to PolySharp, set your C# language version to `latest`.

NOTE: Generated code DOES NOT require Polysharp - it's only needed when you use `#nullable` on a target framework version that did not natively support it (such as all .NET Framework versions).


### Extend the generated classes — don't recreate them

Every class the tool generates for a schema element or complex type is declared `partial` (the simple-type validator helpers and enums are not). That is deliberate: it lets you add methods, properties, constructors, interfaces or other members in a **separate hand-written file**, and the next `gen` run leaves that file untouched. Add new behaviour there rather than editing `*.xsd-g.cs`.

```csharp
// Project.Extensions.cs — hand-written, and safe from regeneration
using System.Linq;

namespace My.Generated.Namespace
{
    public partial class Project
    {
        /// <summary>True when the element has no child elements.</summary>
        public bool IsEmpty => !Untyped.Elements().Any();
    }
}
```

The hand-written part must use the same namespace and type name, and the same accessibility as the generated part: `public` by default, or `internal` when the config sets `<Namespace DefaultVisibility="internal" />`. Do not repeat the base class or interfaces (such as `XTypedElement` and `IXMetaData`) — they are already declared on the generated part. Both parts must also live in the same project.

**Do not build a parallel object model.** The generated classes *are* the typed model for the document: they inherit `XTypedElement`, expose strongly-typed properties for the schema's elements and attributes, and provide `Load`/`Parse`/`Save`. Writing your own DTOs, records or POCOs that mirror those properties duplicates the schema mapping, discards the validation and round-trip behaviour, and drifts as soon as the schema changes. If a member is missing, add it in a partial declaration; if the shape itself is wrong, fix the generator in `XObjectsCode/` and regenerate — never re-model the document by hand.

## Summarising generated code

The `sum` verb writes a markdown summary of the types defined in generated code files:

```bash
linqtoxsd sum Generated/wss.xsd-g.cs
linqtoxsd sum Generated/          # a single folder of .xsd-g.cs files
linqtoxsd sum a.xsd-g.cs,b.xsd-g.cs -o Docs/
```

- Accepts one or more `.cs` files, **or** one folder of `*.xsd-g.cs` files — not both, and not more than one folder.
- Writes `<name>.md` next to each input (`wss.xsd-g.cs` → `wss.md`), or into the folder given with `-o` (created if absent).
- For each class the summary records the declaration (base types and interfaces included) plus public/protected/internal constructors, methods and properties, reduced to signatures with bodies omitted; private members are excluded. Static `SimpleTypeValidator` fields appear under a "Type validators" section, and enums are listed with their values.
- Files are parsed with Roslyn at C# 8, so any syntax error makes the command fail with a non-zero exit code. `sum` doubles as a quick validity check of generated output.

## Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| `No configuration file found: splitting the generated code by class` | Expected when no config exists and `-o` is not a file. Create a config with `config -e`, pass one with `-c`, or pass a `*.cs` file to `-o` for a single file. |
| `NOTE: Generated code is now split into multiple files ... Delete the previously generated file` | A previous run wrote the combined `<schema>.xsd-g.cs`. Delete it — otherwise both files declare the same types. |
| `You should delete the existing source code file: <schema>.xsd.cs` | Legacy pre-v3.4.17 output. Delete it, or rerun with `-a` to have the tool delete it. |
| `Output refers to a single file ..., but the configuration sets SplitCodeFiles` | Pass a folder (or no `-o`) instead of a `.cs` file, or remove `<SplitCodeFiles />` from the config. |
| `The configuration document does not contain a Namespaces element` | `-c` pointed at a file that is not a LinqToXsd config, or one was hand-edited to remove `<Namespaces>`. |
| `There are no file or folder paths present in the enumerable!` | No input was given, e.g. `config -e -o file.xml`. Supply at least one schema path. |
| XSD 1.1 schemas are skipped | Not supported by this port; convert to XSD 1.0 or generate that schema with another tool. |
