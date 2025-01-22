# [vNext]

## Improvements:

* Suggestion for adding [FluentAssertions](https://github.com/fluentassertions/fluentassertions) on the new project wizard screen has been removed to avoid confusions, because FluentAssertion does not offer free use for commercial projects anymore. (#60)
* Show regex options list e.g. '(option1|option2|option3)' parameter in step completion instead of a generic `[string]` placeholder (#55)

## Bug fixes:

* Fix: Error message box when creating feature file with space in its name (#50)
* Fix: Error during discovery of .NET 4 projects when .NET 6.0 is not installed (#53)

*Contributors of this release (in alphabetical order):* @gasparnagy, @RikvanSpreuwel

# v2024.7.204 - 2024-11-20

## Improvements:

* Added support for .NET 9 through the Visual Studio extension (#44)

*Contributors of this release (in alphabetical order):* @gasparnagy, @UL-ChrisGlew

# v2024.6.176 - 2024-11-08

## Bug fixes:

* Fix: Reqnroll extension v2024.5.169 fails in Visual Studio 17.8.3 (#42)

*Contributors of this release (in alphabetical order):* @gasparnagy

# v2024.5.169 - 2024-11-07

## Improvements:

* Update dependencies to fix potential security vulnerabilities (#32)

## Bug fixes:

* Bug Fix: Fix 'Reqnroll Extension v2024.3.152 does not work on Visual Studio 17.11.4' by including missing assemblies (#37)
* Bug Fix: Visual Studio 2022 extension "Add New Project" adds dependency for Reqnroll.MsTest 1.0.0 (#41)

*Contributors of this release (in alphabetical order):* @clrudolphi, @gasparnagy, @UL-ChrisGlew

# v2024.4.154 - 2024-09-18

## Bug fixes:

* HotFix: Revert 'Update dependencies to fix potential security vulnerabilities (#32)' because of compatibility issues with Visual Studio 17.11.4 (#37)

*Contributors of this release (in alphabetical order):* @gasparnagy

# v2024.3.152 - 2024-09-17

## Improvements:

* Find Unused Step Definitions Command: Improved handling of Step Definitions decorated with the 'StepDefinition' attribute. If a Step Definition is used in any Given/Then/When step in a Feature file, the step will no longer show in the 'Find Unused Step Definitions' context menu. (#24)
* Detect existence of SpecFlow for Visual Studio extension and show a warning (#16)
* Extend searched config file locations to match the locations searched for the reqnroll runtime (#31)
* Offer language updates of Gherkin v29 in the editor (updated from Gherkin v22) (#33)
* Update dependencies to fix potential security vulnerabilities (#32)

## Bug fixes:

* Fix: Using the extension side-by-side with the SpecFlow for Visual Studio extension causes CompositionFailedException (#25)
* Fix: Define Steps produces incorrect regex for step definition (#28)

*Contributors of this release (in alphabetical order):* @clrudolphi, @gasparnagy, @jdb0123, @UL-ChrisGlew

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
