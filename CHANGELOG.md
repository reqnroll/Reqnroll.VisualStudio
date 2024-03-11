# [vNext]
* FEATURE: FindUnused Step Definitions Command - from within a binding class a new context menu command, "FindUsusedStepDefinitions", is available. 
	* This will list any Step Definition methods that are not matched by one or more Feature steps in the current project.

# v2024.1.49 - 2024-02-08

* Support for .NET 8 projects
* New editor command: "Go To Hooks" (Ctrl B,H) to navigate to the hooks related to the scenario
* The "Go To Definition" lists hooks when invoked from scenario header (tags, header line, description)
* Initial release based on v2022.1.91 of the [SpecFlow for Visual Studio](https://github.com/SpecFlowOSS/SpecFlow.VS/) extension.
