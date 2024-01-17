using Reqnroll.VisualStudio.Wizards.Infrastructure;

namespace Reqnroll.VisualStudio.Wizards;

public class FeatureFileWizard : IDeveroomWizard
{
    public bool RunStarted(WizardRunParameters wizardRunParameters)
    {
        var projectSettings = wizardRunParameters.ProjectScope.GetProjectSettings();

        wizardRunParameters.MonitoringService.MonitorCommandAddFeatureFile(projectSettings);

        if (projectSettings.IsReqnrollProject)
        {
            if (projectSettings.DesignTimeFeatureFileGenerationEnabled)
                wizardRunParameters.ReplacementsDictionary[WizardRunParameters.CustomToolSettingKey] =
                    "ReqnrollSingleFileGenerator";
            else if (!projectSettings.HasDesignTimeGenerationReplacement)
                wizardRunParameters.ProjectScope.IdeScope.Actions.ShowProblem(
                    $"In order to be able to run the Reqnroll scenarios as tests, you need to install the '{ReqnrollPackageDetector.ReqnrollToolsMsBuildGenerationPackageName}' NuGet package to the project.");

            if (projectSettings.ReqnrollProjectTraits.HasFlag(ReqnrollProjectTraits.XUnitAdapter))
                wizardRunParameters.ReplacementsDictionary[WizardRunParameters.BuildActionKey] =
                    "ReqnrollEmbeddedFeature";
        }

        return true;
    }
}
