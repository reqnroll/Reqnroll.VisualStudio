using System;
using Reqnroll.VisualStudio.Wizards.Infrastructure;

namespace Reqnroll.VisualStudio.Wizards;

public class ReqnrollConfigFileWizard : IDeveroomWizard
{
    public bool RunStarted(WizardRunParameters wizardRunParameters)
    {
        var projectSettings = wizardRunParameters.ProjectScope.GetProjectSettings();

        wizardRunParameters.MonitoringService.MonitorCommandAddReqnrollConfigFile(projectSettings);

        if (projectSettings.IsReqnrollProject && projectSettings.ReqnrollVersion.Version < new Version(3, 6, 23))
            wizardRunParameters.ReplacementsDictionary[WizardRunParameters.CopyToOutputDirectoryKey] = "PreserveNewest";
        return true;
    }
}
