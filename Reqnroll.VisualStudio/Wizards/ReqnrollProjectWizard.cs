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

        // Add custom parameters.
        wizardRunParameters.ReplacementsDictionary.Add("$dotnetframework$", viewModel.DotNetFramework);
        wizardRunParameters.ReplacementsDictionary.Add("$unittestframework$", viewModel.UnitTestFramework);
        wizardRunParameters.ReplacementsDictionary.Add("$fluentassertionsincluded$",
            viewModel.FluentAssertionsIncluded.ToString(CultureInfo.InvariantCulture));

        return true;
    }
}
