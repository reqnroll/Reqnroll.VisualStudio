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
    private readonly INewProjectMetaDataProvider _newProjectMetaDataProvider;

    [ImportingConstructor]
    public ReqnrollProjectWizard(IDeveroomWindowManager deveroomWindowManager, IMonitoringService monitoringService, INewProjectMetaDataProvider newProjectMetaDataProvider)
    {
        _deveroomWindowManager = deveroomWindowManager;
        _monitoringService = monitoringService;
        _newProjectMetaDataProvider = newProjectMetaDataProvider;
    }

    public bool RunStarted(WizardRunParameters wizardRunParameters)
    {
        _monitoringService.MonitorProjectTemplateWizardStarted();

        var frameworkNames = _newProjectMetaDataProvider.TestFrameworks;

        var viewModel = new AddNewReqnrollProjectViewModel(frameworkNames);
        var dialogResult = _deveroomWindowManager.ShowDialog(viewModel);
        if (!dialogResult.HasValue || !dialogResult.Value) return false;

        _monitoringService.MonitorProjectTemplateWizardCompleted(viewModel.DotNetFramework, viewModel.UnitTestFramework,
            viewModel.FluentAssertionsIncluded);

        int packageIndex = 0;

        // insert set of replacement variables for the SDK package
        AddPackageToReplacementDictionary(wizardRunParameters, "Microsoft.NET.Test.Sdk", "17.10.0", packageIndex);

        var dependencies = _newProjectMetaDataProvider.DependenciesOf(viewModel.UnitTestFramework);

        foreach (var package in dependencies)
        {
            packageIndex++;
            var name = package.name;
            var version = package.version;
            AddPackageToReplacementDictionary(wizardRunParameters, name, version, packageIndex);
        }

        if (viewModel.FluentAssertionsIncluded)
            AddPackageToReplacementDictionary(wizardRunParameters, "FluentAssertions", "6.12.0", packageIndex+1);

        wizardRunParameters.ReplacementsDictionary.Add("$dotnetframework$", viewModel.DotNetFramework);
        wizardRunParameters.ReplacementsDictionary.Add("$fluentassertionsincluded$",
            viewModel.FluentAssertionsIncluded.ToString(CultureInfo.InvariantCulture));

        return true;

        static void AddPackageToReplacementDictionary(WizardRunParameters wizardRunParameters, string name, string version, int packageIndex)
        {
            string packagename = "packagerefname";
            string packageversion = "packagerefversion";
            string packagehasvalue = "packagerefhasvalue";

            wizardRunParameters.ReplacementsDictionary.Add($"${packagehasvalue}{packageIndex}$", true.ToString(CultureInfo.InvariantCulture));
            wizardRunParameters.ReplacementsDictionary.Add($"${packagename}{packageIndex}$", name);
            wizardRunParameters.ReplacementsDictionary.Add($"${packageversion}{packageIndex}$", version);
        }
    }
}
