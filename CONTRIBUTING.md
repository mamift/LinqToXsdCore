# Contributing to LinqToXsdCore

IMPORTANT BEFORE DOING ANY WORK: Please use the [LinqToXsd-TestingSuite.slnf](https://github.com/mamift/LinqToXsdCore/blob/testing/dublincore-xhtml/LinqToXsd-TestingSuite.slnf) solution filter file when working with this repo in Visual Studio or Rider. The full `sln` file has projects that currently do not build.

Mandatory requirements before your PR is accepted or approved:

1. **When contributing, please summarise your pull request in such a manner that it is acceptable to be included in the `RELEASENOTES.md` file** that is packaged in every nuget release (look at the release notes for previous releases to get an idea - be short but descriptive enough to point someone in the right direction if they want to dig deeper - this includes referring to Github issues and PRs). 
    - You do not have to update the `RELEASENOTES.md` file yourself but write a summary that's sufficient to be included there. This means that you should NOT GET OVERLY TECHNICAL IN YOUR PR DESCRIPTION. Please use [Github issues](https://github.com/mamift/LinqToXsdCore/issues) for that and refer to the issue in your PR description.

2. **ENSURE THAT YOU WRITE A TEST FOR YOUR CHANGES.** Tests are incredibly helpful when collaborating with other developers - they help ensure that new contributions do not break existing functionality. Fixing a bug? Write a test to ensure the bug does not re-occur. Adding a feature? Add a test to cover the feature behavior or output.

3. **ENSURE THAT YOUR CHANGES DO NOT BREAK EXISTING FUNCTIONALITY.** All tests should pass (including test written by you). 
    - There are some existing tests that are broken when run as part of the full suite, *but work when run individually* (`TestGenerateCodeFromSingleDirectoryWithAutoConfig` for instance) - these are acceptable failures. 
    - For tests that are broken and need updating, please annotate them such that they still run *but are not counted as failures or ignored* (see `TestGetFullPathFromSchemaInGraph`),
    - Or that they need to be explicitly run by the user (see `PrototypeSplitByNamespaceAndClass`), so wil not run during the CI/DevOps builds.

For points 2. and 3. the preferred type of test is an end to end code generation test; so this counts as an integration test. Unit tests should be preferred when testing extension methods.

## Use of AI

We welcome the use of AI in your pull requests, so long as you follow the above rules - remember NO TEST FAILURES. And if your PR brings in changes that results in test failures, then those failures need to be fixed before the PR can be approved. AI coding agents should help you with fixing these.

Much of the code base and indeed most of the test suite pre-dates AI coding agents (the original Microsoft code was written in 2008-2011 and was ported to .NET Core/.NET Standard in 2019), so as long as the existing test suite passes, then new code, even if AI generated is acceptable.

## Optional things

- Please consider contributing the full XSD file that exhibits any code generation bugs. In terms of cyclomatic complexity, the code base rates incredibly high, and as a result, LinqToXsd emits a very specific output, and is highly vulnerable to regressions even over seemingly trivial code changes. 

    It also does not cover the entire W3C XML Schema v1.0 specification - so when a code gen bug is discovered, it is incredibly helpful to have the full XSD file as many parts of the XML Schema specification interact in quirky ways when transposing it to C#. Some things simply cannot be handled by LinqToXsd as there currently exists no idomatic way to model an XSD concept or construct in C#. Having the XSD on file helps when new C# language versions are released, and they can be evaluated for future implementation.
    - In cases where bugs from specific XSD files are unfeasible to fix in a single PR, please: 
        - contribute the XSD file under it's own separate folder under the [GeneratedSchemaLibraries](https://github.com/mamift/LinqToXsdCore/tree/master/GeneratedSchemaLibraries) folder, 
        - then run the [create individual xsd libraries.ps1 PowerShell script](https://github.com/mamift/LinqToXsdCore/blob/testing/dublincore-xhtml/GeneratedSchemaLibraries/create%20individual%20xsd%20libraries.ps1) to automatically create a `.CSPROJ` file and link it to the greater solution. 
        - This allows storing the XSD for future bug fixing and also segregating buggy code out in it's own project so that the test project can still build and run. Uncompilable code **should not be linked to the test project ([XObjectTests](https://github.com/mamift/LinqToXsdCore/blob/master/XObjectsTests/XObjectsTests.csproj))**.

- If the full XSD file cannot be given, then a minimal extract of the schema that exhibits the code generation bug is also acceptable. Many **code generation** tests simply make no sense unless there's an accompanying XSD file.

- Another thing too: in the solution root there's a PowerShell script: [Regenerate-TestingSuite.ps1](https://github.com/mamift/LinqToXsdCore/blob/testing/dublincore-xhtml/Regenerate-TestingSuite.ps1). After all the tests in the testing project have been run (and pass), run this script to regenerate all C# code in the `GeneratedSchemaLibraries` folder. This is very helpful to catch regressions that are not covered in the test project. **Please try to fix any regressions alongside your PR! I WILL NOT APPROVE A PR THAT INTRODUCES A REGRESSION OR TEST FAILURE!** It is also helpful to have the changes in code generation captured in git from version to version to find and diagnose bugs, and to know which version may have changed ouptuts or behavior.
    - Also: the script may generate new code files (i.e. not yet tracked in git - discard these files).
    - *ONLY COMMIT CODE RE-GENERATED BY THE SCRIPT FOR EXISTING FILES THAT ARE TRACKED IN GIT.*

Finally, the `AllEntCf.xsd-xml` schema is cursed; trying to run code gen on it will result in an out of memory error. I appreciate any efforts to get to the bottom of it. Please refrain from using this file in any tests. You have been warned.