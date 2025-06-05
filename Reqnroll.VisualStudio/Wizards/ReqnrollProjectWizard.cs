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

        var dependencies = _newProjectMetaDataProvider.DependenciesOf(viewModel.UnitTestFramework);
        var keys = new List<string>() { "Reqnroll_Lib", "TestFramework_Lib", "Adapter_Lib" };
        // Add custom parameters.
        foreach(string k in keys)
        {
            var package = dependencies[k];
            var name = package.name;
            var version = package.version;
            wizardRunParameters.ReplacementsDictionary.Add($"${k}$",$"<PackageReference Include=\"{name}\" Version=\"{version}\" />");
        }

        wizardRunParameters.ReplacementsDictionary.Add("$dotnetframework$", viewModel.DotNetFramework);
        wizardRunParameters.ReplacementsDictionary.Add("$fluentassertionsincluded$",
            viewModel.FluentAssertionsIncluded.ToString(CultureInfo.InvariantCulture));

        return true;
    }
}
