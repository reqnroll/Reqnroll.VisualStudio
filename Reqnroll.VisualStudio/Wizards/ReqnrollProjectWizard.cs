using System;
using System.Globalization;
using System.Linq;
using Reqnroll.VisualStudio.Wizards.Infrastructure;

namespace Reqnroll.VisualStudio.Wizards;

[Export(typeof(ReqnrollProjectWizard))]
public class ReqnrollProjectWizard : IDeveroomWizard
{
    private readonly IDeveroomWindowManager _deveroomWindowManager;
    private readonly IMonitoringService _monitoringService;

    [ImportingConstructor]
    public ReqnrollProjectWizard(IDeveroomWindowManager deveroomWindowManager, IMonitoringService monitoringService)
    {
        _deveroomWindowManager = deveroomWindowManager;
        _monitoringService = monitoringService;
    }

    public bool RunStarted(WizardRunParameters wizardRunParameters)
    {
        _monitoringService.MonitorProjectTemplateWizardStarted();

        var viewModel = new AddNewReqnrollProjectViewModel();
        var dialogResult = _deveroomWindowManager.ShowDialog(viewModel);
        if (!dialogResult.HasValue || !dialogResult.Value) return false;

        _monitoringService.MonitorProjectTemplateWizardCompleted(viewModel.DotNetFramework, viewModel.UnitTestFramework,
            viewModel.FluentAssertionsIncluded);

        // Clean the project name to ensure it is a valid identifier for the RootNamespace.
        // Cleaning process:
        // 1. split by '.'
        // 2. Call ToIdentifier on each part
        var projectNameParts = wizardRunParameters.ReplacementsDictionary["$projectname$"].Split('.');
        var proposedProjectName = string.Join(".", projectNameParts);
        var cleanedProjectName = string.Join(".", projectNameParts.Select(part => CodeFormattingExtensions.ToIdentifier(part)).ToArray());
        var rootNamespace = "";
        if (proposedProjectName != cleanedProjectName)
        {
            rootNamespace = cleanedProjectName;
        }

        // Add custom parameters.
        wizardRunParameters.ReplacementsDictionary.Add("$dotnetframework$", viewModel.DotNetFramework);
        wizardRunParameters.ReplacementsDictionary.Add(WizardRunParameters.IsNetFrameworkKey, viewModel.IsNetFramework.ToString(CultureInfo.InvariantCulture));
        wizardRunParameters.ReplacementsDictionary.Add("$unittestframework$", viewModel.UnitTestFramework);
        wizardRunParameters.ReplacementsDictionary.Add("$fluentassertionsincluded$",
            viewModel.FluentAssertionsIncluded.ToString(CultureInfo.InvariantCulture));
        wizardRunParameters.ReplacementsDictionary.Add("$rootnamespace$", rootNamespace);

        if (!viewModel.IsNetFramework)
        {
            // For .NET 8+ projects, we will create a set of global usings to be inserted in to the project file. This is in leiu of adding a `GlobalUsings.cs` file.
            var globalUsings = new StringBuilder();
            switch (viewModel.UnitTestFramework)
            {
                case "MSTest":
                    globalUsings.AppendLine("    <Using Include=\"Microsoft.VisualStudio.TestTools.UnitTesting\" />");
                    break;
                case "NUnit":
                    globalUsings.AppendLine("    <Using Include=\"NUnit.Framework\" />");
                    break;
                case "xUnit":
                case "XUnit.v3":
                    globalUsings.AppendLine("    <Using Include=\"Xunit\" />");
                    break;
                // For TUnit, no global using is required.
                default:
                    break;
            }
            if (viewModel.FluentAssertionsIncluded)
            {
                globalUsings.AppendLine("    <Using Include=\"FluentAssertions\" />");
            }
            wizardRunParameters.ReplacementsDictionary.Add("$globalUsings$", globalUsings.ToString());
        }

        return true;
    }
}
