#nullable disable
using System.ComponentModel.Design;
using VSLangProj;

namespace Reqnroll.VisualStudio.Generator;

public class RegenerateAllFeatureFileCodeBehindCommand
{
    private readonly IVsIdeScope _vsIdeScope;

    public RegenerateAllFeatureFileCodeBehindCommand(IVsIdeScope vsIdeScope)
    {
        _vsIdeScope = vsIdeScope;
    }

    public void Register(OleMenuCommandService menuCommandService)
    {
        var menuCommand = new OleMenuCommand(InvokeHandler,
            new CommandID(ReqnrollVsCommands.DefaultCommandSet,
                ReqnrollVsCommands.RegenerateAllFeatureFileCodeBehindCommandId));
        menuCommand.BeforeQueryStatus += MenuCommandOnBeforeQueryStatus;
        menuCommandService.AddCommand(menuCommand);
    }

    private Project GetSingleSelectedProject()
    {
        var selection = _vsIdeScope.Dte.SelectedItems;
        if (selection == null || selection.Count != 1) return null;

        return selection.Item(1).Project;
    }

    private void MenuCommandOnBeforeQueryStatus(object sender, EventArgs e)
    {
        var menuCommand = sender as OleMenuCommand;
        if (menuCommand == null)
            return;

        var project = GetSingleSelectedProject();
        if (project == null || !HasFeatureFiles(project))
        {
            menuCommand.Visible = false;
            return;
        }

        menuCommand.Visible = true;
        var projectScope = _vsIdeScope.GetProjectScope(project);
        var projectSettings = projectScope.GetProjectSettings();
        menuCommand.Enabled = projectSettings.IsReqnrollTestProject &&
                              projectSettings.DesignTimeFeatureFileGenerationEnabled;
    }

    private bool HasFeatureFiles(Project project)
    {
        try
        {
            if (!VsUtils.IsSolutionProject(project))
                return false;
            return GetFeatureFileItems(project).Any();
        }
        catch (Exception e)
        {
            _vsIdeScope.Logger.LogDebugException(e);
            return false;
        }
    }

    private IEnumerable<ProjectItem> GetFeatureFileItems(Project project)
    {
        return VsUtils.GetPhysicalFileProjectItems(project)
            .Where(pi => FileSystemHelper.IsOfType(VsUtils.GetFilePath(pi), ".feature"));
    }

    private void InvokeHandler(object sender, EventArgs e)
    {
        var project = GetSingleSelectedProject();
        if (project == null)
            return;

        var projectScope = _vsIdeScope.GetProjectScope(project);
        if (projectScope == null)
            return;

        if (!GenerationService.CheckReqnrollToolsFolder(projectScope))
            return;

        foreach (var featureFileItem in GetFeatureFileItems(project))
        {
            var vsProjectItem = featureFileItem.Object as VSProjectItem;
            vsProjectItem?.RunCustomTool();
        }
    }
}
