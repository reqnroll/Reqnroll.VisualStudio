#nullable disable
using System.Text.RegularExpressions;

namespace Reqnroll.VisualStudio.Wizards.Infrastructure;

public abstract class VsSimulatedItemAddProjectScopeWizard<TWizard> : VsProjectScopeWizard<TWizard>
    where TWizard : class, IDeveroomWizard
{
    private bool _enableSimulatedItemAdd;

    private SafeDispatcherTimer _openFileTimer;
    private string _templateFileName;
    private bool _usingMicrosoftNetSdk;

    protected override bool RunStarted(Project project, WizardRunParameters wizardRunParameters, TWizard wizard)
    {
        _usingMicrosoftNetSdk = GetUsingMicrosoftNETSdk(project);
        _enableSimulatedItemAdd =
            wizardRunParameters.IsAddNewItem &&
            _usingMicrosoftNetSdk &&
            wizardRunParameters.TargetFolder != null;

        if (_enableSimulatedItemAdd)
            Logger?.LogVerbose($"Using simulated item add for project '{project.Name}'");

        return base.RunStarted(project, wizardRunParameters, wizard);
    }

    private bool GetUsingMicrosoftNETSdk(Project project)
    {
        var propValue = VsUtils.GetMsBuildPropertyValue(project, "UsingMicrosoftNETSdk");
        return string.Equals(propValue, "true", StringComparison.InvariantCultureIgnoreCase);
    }

    public override bool ShouldAddProjectItem(string filePath)
    {
        _templateFileName = filePath;
        return base.ShouldAddProjectItem(filePath) && !_enableSimulatedItemAdd;
    }

    public override void RunFinished()
    {
        if (_isValidRun && _enableSimulatedItemAdd && _templateFileName != null)
        {
            var targetFile = Path.Combine(_wizardRunParameters.TargetFolder, _wizardRunParameters.TargetFileName);
            var sourceFile = Path.Combine(_wizardRunParameters.TemplateFolder, _templateFileName);
            CopyWithTemplateParamResolution(sourceFile, targetFile);
            ScheduleOpenFile(targetFile);
        }

        base.RunFinished();
    }

    private void CopyWithTemplateParamResolution(string sourceFile, string targetFile)
    {
        var fileSystem = _wizardRunParameters.ProjectScope.IdeScope.FileSystem;
        if (fileSystem == null)
            return;

        var replacementsDictionary = new Dictionary<string, string>(_wizardRunParameters.ReplacementsDictionary);
        replacementsDictionary["$itemname$"] = Path.GetFileNameWithoutExtension(targetFile);

        try
        {
            var fileContent = fileSystem.File.ReadAllText(sourceFile);
            var updatedFileContent = Regex.Replace(fileContent, @"\$[^\$\s]+\$",
                match => replacementsDictionary.TryGetValue(match.Value, out var value) ? value : match.Value);
            fileSystem.File.WriteAllText(targetFile, updatedFileContent, Encoding.UTF8);
            Logger?.LogVerbose($"New file created: {targetFile}");
        }
        catch (Exception ex)
        {
            Logger?.LogException(MonitoringService, ex);
        }
    }

    private void ScheduleOpenFile(string targetFile)
    {
        var project = _project;
        var logger = Logger;
        _openFileTimer = SafeDispatcherTimer.CreateOneTime(1, Logger, MonitoringService,
            () => { OpenFile(targetFile, project, logger); });
        _openFileTimer.Start();
    }

    // this method needs to be static as it is called from the timer, when the core wizard has been disposed already.
    private static void OpenFile(string targetFile, Project project, IDeveroomLogger logger)
    {
        try
        {
            var projectItem = VsUtils.FindProjectItemByFilePath(project, targetFile);
            if (projectItem != null)
            {
                //projectItem.Open();
                project.DTE.ExecuteCommand("File.OpenFile", $"\"{targetFile}\"");
                logger.LogVerbose($"File opened: {targetFile}");
            }
        }
        catch (Exception ex)
        {
            logger.LogDebugException(ex);
        }
    }
}
