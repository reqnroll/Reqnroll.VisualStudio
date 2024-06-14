# Contributing to Reqnroll for Visual Studio

## Prerequisites

* Visual Studio 2022 with workloads
  * .NET Desktop Development
  * **Visual Studio extension development**
  * .NET Core cross-platform development

## The Visual Studio Experimental Instance
 
Installing the "Visual Studio extension development" workload enables a separate configuration of Visual Studio 2022, called the "Experimental Instance". You can run the experimental instance of Visual Studio from the installed Start menu shortcut "Start Experimental Instance of Visual Studio 2022" or from the command line using `devenv /rootSuffix Exp`. 

This instance can have configuration and plugins installed independently from your "main" Visual Studio. So for example you can keep working with the last released version of the Reqnroll plugin while you can try and debug the version of the plugin built locally. You can of course install additional extensions to the experimental instance (independently of your main instance) in order to test how the Reqnroll package works with it.

Normally the experimental instance works fine and able to work with the local builds of your plugin, but very rarely the settigns might get corrupted. In this case you can "reset" the instance, that means it deletes all configuration and installed packages for the experimental instance. This can be done by invoking the "Reset the Visual Studio 2022 Experimental Instance" command from the Start menu (the shortcut is loacted at `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Visual Studio 2022\Microsoft Visual Studio SDK\Tools` if you can't find it).

## Build the project

* Before building the project you need to run the `Connectors\build.ps1` script that builds the Reqnroll connectors. (Unless you work on connectors, this should be done only once.)
* After that you can build the project with Visual Studio with "Debug" configuration.

The Debug build will automatically install the package to the Visual Studio Experimental Instance. 

## Debug the Visual Studio package

As the build installs the package to the Visual Studio Experimental Instance, you can simply start the experimental instance and test the plugin behavior manually. Just like with any other apps, you can also attach to the Visual Studio Experimental Instance (devenv.exe) process with your Visual Studio that you used to build and you can debug (set breakpoints, stop at exceptions, etc.).

You can also set the `Reqnroll.VisualStudio.Package` project as startup project and configure a debug profile for the execution. (You can set the lunch profile by selecting the "Reqnroll.VisualStudio.Package Debug Properties" option from the toolbar run button dropdown or from the "Debug" section of the project properties window.) The launch profile has to be configured as:

* Use "Create a new profile" button and choose "Executable"
* Set `C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe` as "Executable"
* Set `/rootsuffix Exp` as "Command line arguments"
* Delete the other project-based profile
* Rename the profile to something more meaningful

Once the launch profile has been configured, yoy can use the Start Debugging command (F5) and it will automatically start the Visual Studio Experimental Instance with the debugger attached. So you can debug the full life-cycle of the extension, right from the loading. (Similarly, the "Start Without Debugging" (Ctrl+F5) is also working that simply starts Visual Studio Experimental Instance and you don't have to find it in the Start menu.)

## Test UI

Starting Visual Studio every time to just check if a button is at a right position or if an icon is visible is cumbersome. To make that simpler, there is a standalone desktop app in the project `Reqnroll.VisualStudio.UI.Tester`, that has a button for testing each UI element. So to check UI related details it is recommended to set the  `Reqnroll.VisualStudio.UI.Tester` project as startup project and run it to verify if the UI looks good.

## Run Tests

* The tests need to run in x64. This is configured in the run settings file `.runsettings` in the solution folder. To be able to pick up this settings file, you have to enable ["Autodetect the run settings file"](https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?view=vs-2019#autodetect-the-run-settings-file).

* The Specs project generates sample projects and caches them to speed up the test execution. The cache is by default in the system TEMP folder that is regularly cleaned up by Windows. If you are a regular contributor it is recommended to setup the cache in a folder that is not managed by Windows. You can do this by setting the `REQNROLL_TEST_TEMP` environment variable. E.g. SET REQNROLL_TEST_TEMP=C:\Temp

* Once you have selected the proper run settings (see above), all tests should pass locally. The tests in the `Reqnroll.VisualStudio.Specs` might take longer for the first time, but they cache the necessary packages so for the subsequent runs they are fast.

* Doing test-driven development might require to build the project regularly, but the installation of the package to the Visual Studio Experimental Instance takes quite much time. To overcome that, you can switch from the "Debug" configuration to "TestDebug" that does not compile the `Reqnroll.VisualStudio.Package` project by default. Since this project does only packaging and does not  contain testable logic, you can run the tests locally without building this project. Once you are ready to test it in Visual Studio Experimental Instance, you can build the `Reqnroll.VisualStudio.Package` project explicitly by invoking the "Build" command from the project node in solution explorer.

## Diagnosing MEF Composition Errors

Visual Studio and also our plugin uses [Managed Extensibility Framework (MEF)](https://learn.microsoft.com/en-us/dotnet/framework/mef/) for managing the dependencies. Unfortunately diagnosing MEF problems is hard, because the error messages are not very descriptive.

For diagnosing MEF composition errors, you can check the [article by Daniel Plaisted](https://learn.microsoft.com/en-us/archive/blogs/dsplaisted/how-to-debug-and-diagnose-mef-failures) that contains a lot of useful ideas. Unfortunately we could not figure out how to enable logging in Visual Studio, but in case of any errors, **you should check the component cache error log file**. The file is a text file with name `Microsoft.VisualStudio.Default.err` and it is located in the `ComponentModelCache` folder of the Visual Studio user configuration folder. For the Visual Studio Experimental Instance, this is `C:\Users\<your-user>\AppData\Local\Microsoft\VisualStudio\17.0_921b0c10Exp\ComponentModelCache\Microsoft.VisualStudio.Default.err`. You can search for "Reqnroll" in this file and you might find information about the composition error causes the problems.
