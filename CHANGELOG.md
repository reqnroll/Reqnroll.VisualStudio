# [vNext]

# v2024.2.93 - 2024-06-05

## Improvements:

* Find Unused Step Definitions Command: There is a new command available from within a binding class, "Find Usused Step Definitions". This will list any Step Definition methods that are not matched by one or more Feature steps in the current project. (#8)
* Project template have been updated to the latest Reqnroll and other dependency versions

## Bug fixes:

* Fix: The "Define Steps" command does not abide by the reqnroll.json configuration setting for the `trace/stepDefinitionSkeletonStyle` (`RegexAttribute` or `CucumberExpressionAttribute`) (#18)
* Fix: The "Define Steps" command uses Reqnroll using statements in the generated snipped for SpecFlow projects (#6)
* Fix: "Find Step Definition Usages" command not visible for SpecFlow projects (#7)
* Fix: "Find Step Definition Usages" command fails for first time (#11)

# v2024.1.49 - 2024-02-08

* Support for .NET 8 projects
* New editor command: "Go To Hooks" (Ctrl B,H) to navigate to the hooks related to the scenario
* The "Go To Definition" lists hooks when invoked from scenario header (tags, header line, description)
* Initial release based on v2022.1.91 of the [SpecFlow for Visual Studio](https://github.com/SpecFlowOSS/SpecFlow.VS/) extension.
