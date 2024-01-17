# Contributing to Reqnroll for Visual Studio

## Building the project

### Prerequisites

* Visual Studio 2022 preview with workloads
  * .NET Desktop Development
  * Visual Studio extension development
  * .NET Core cross-platform development

Note: You can build the project from Visual Studio 2019, but it will install the plugin to the Visual Studio 2019 experimental hive instead of VS2022, so currently development should be done in VS2022.

## Build the project

* Before building the project you need to run the `Connectors\build.ps1` script that builds the Reqnroll connectors.

## Run Tests

* The tests need to run in x64. This is configured in the run settings file `.runsettings` in the solution folder. To be able to pick up this settings file, you have to enable ["Autodetect the run settings file"](https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?view=vs-2019#autodetect-the-run-settings-file).

* The Specs project generates sample projects and caches them to speed up the test execution. The cache is by default in the system TEMP folder that is regularly cleaned up by Windows. If you are a regular contributor it is recommended to setup the cache in a folder that is not managed by Windows. You can do this by setting the `REQNROLL_TEST_TEMP` environment variable. E.g. SET REQNROLL_TEST_TEMP=C:\Temp
