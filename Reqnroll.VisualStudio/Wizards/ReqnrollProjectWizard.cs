using Reqnroll.VisualStudio.ProjectSystem;
using Reqnroll.VisualStudio.Wizards.Infrastructure;
using System;
using System.Globalization;
using System.Linq;

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

        var viewModel = new AddNewReqnrollProjectViewModel(_newProjectMetaDataProvider);
        var dialogResult = _deveroomWindowManager.ShowDialog(viewModel);
        if (!dialogResult.HasValue || !dialogResult.Value) return false;

        _monitoringService.MonitorProjectTemplateWizardCompleted(viewModel.DotNetFrameworkTag, viewModel.UnitTestFramework,
            viewModel.FluentAssertionsIncluded);

        // insert set of replacement variables for the SDK package
        AddPackageToReplacementDictionary(wizardRunParameters, "Microsoft.NET.Test.Sdk", "17.10.0");

        var dependencies = _newProjectMetaDataProvider.DependenciesOf(viewModel.UnitTestFramework);

        foreach (var package in dependencies)
        {
            var name = package.name;
            var version = package.version;
            AddPackageToReplacementDictionary(wizardRunParameters, name, version);
        }

        if (viewModel.FluentAssertionsIncluded)
            AddPackageToReplacementDictionary(wizardRunParameters, "FluentAssertions", "6.12.0");

        wizardRunParameters.ReplacementsDictionary.Add("$dotnetframework$", viewModel.DotNetFrameworkTag);
        wizardRunParameters.ReplacementsDictionary.Add("$fluentassertionsincluded$",
            viewModel.FluentAssertionsIncluded.ToString(CultureInfo.InvariantCulture));

        return true;

        static void AddPackageToReplacementDictionary(WizardRunParameters wizardRunParameters, string name, string version)
        {
            var refText = $"<PackageReference Include=\"{name}\" Version=\"{version}\" />";
            const string key = "$nugetpackagereferences$";
            if (wizardRunParameters.ReplacementsDictionary.TryGetValue(key, out string existingValue))
            {
                wizardRunParameters.ReplacementsDictionary[key] =
                    existingValue +
                    "\r\n    " +
                    refText;
            }
            else
            {
                wizardRunParameters.ReplacementsDictionary.Add(key, refText);
            }
        }
    }
}
