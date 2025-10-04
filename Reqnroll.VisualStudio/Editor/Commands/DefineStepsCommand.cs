#nullable disable
namespace Reqnroll.VisualStudio.Editor.Commands;

[Export(typeof(IDeveroomFeatureEditorCommand))]
public class DefineStepsCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
{
    [ImportingConstructor]
    public DefineStepsCommand(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider)
        : base(ideScope, aggregatorFactory, taggerProvider)
    {
    }

    public override DeveroomEditorCommandTargetKey[] Targets => new[]
    {
        new DeveroomEditorCommandTargetKey(ReqnrollVsCommands.DefaultCommandSet,
            ReqnrollVsCommands.DefineStepsCommandId)
    };

    public override DeveroomEditorCommandStatus QueryStatus(IWpfTextView textView,
        DeveroomEditorCommandTargetKey commandKey)
    {
        var projectScope = IdeScope.GetProject(textView.TextBuffer);
        var projectSettings = projectScope?.GetProjectSettings();
        if (projectScope == null || !projectSettings.IsReqnrollProject) return DeveroomEditorCommandStatus.Disabled;
        return base.QueryStatus(textView, commandKey);
    }

    public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default)
    {
        Logger.LogVerbose("Create Step Definitions");

        var projectScope = IdeScope.GetProject(textView.TextBuffer);
        var projectSettings = projectScope?.GetProjectSettings();
        if (projectScope == null || !projectSettings.IsReqnrollProject)
        {
            IdeScope.Actions.ShowProblem(
                "Define steps command can only be invoked for feature files in Reqnroll projects");
            return true;
        }

        var featureTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.FeatureBlock);
        if (featureTag == VoidDeveroomTag.Instance)
        {
            Logger.LogWarning("Define steps command called for a file without feature block");
            return true;
        }

        var snippetService = projectScope.GetSnippetService();
        if (snippetService == null)
            return true;

        var undefinedStepTags = featureTag.GetDescendantsOfType(DeveroomTagTypes.UndefinedStep).ToArray();
        if (undefinedStepTags.Length == 0)
        {
            IdeScope.Actions.ShowProblem("All steps have been defined in this file already.");
            return true;
        }

        const string indent = "    ";
        string newLine = Environment.NewLine;

        var feature = (Feature) featureTag.Data;
        var viewModel = new CreateStepDefinitionsDialogViewModel();
        viewModel.ClassName = feature.Name.ToIdentifier() + "StepDefinitions";
        viewModel.ExpressionStyle = snippetService.DefaultExpressionStyle;
        bool generateAsyncSnippet = snippetService.DefaultGenerateSkeletonMethodsAsAsync;

        foreach (var undefinedStepTag in undefinedStepTags)
        {
            var matchResult = (MatchResult) undefinedStepTag.Data;
            foreach (var match in matchResult.Items.Where(mi => mi.Type == MatchResultType.Undefined))
            {
                var snippet = snippetService.GetStepDefinitionSkeletonSnippet(match.UndefinedStep,
                    viewModel.ExpressionStyle, generateAsyncSnippet, indent, newLine);
                if (viewModel.Items.Any(i => i.Snippet == snippet))
                    continue;

                viewModel.Items.Add(new StepDefinitionSnippetItemViewModel {Snippet = snippet});
            }
        }

        IdeScope.WindowManager.ShowDialog(viewModel);

        if (viewModel.Result == CreateStepDefinitionsDialogResult.Cancel)
            return true;

        if (viewModel.Items.Count(i => i.IsSelected) == 0)
        {
            IdeScope.Actions.ShowProblem("No snippet was selected");
            return true;
        }

        var combinedSnippet = string.Join(newLine,
            viewModel.Items.Where(i => i.IsSelected).Select(i => i.Snippet.Indent(indent + indent)));

        MonitoringService.MonitorCommandDefineSteps(viewModel.Result, viewModel.Items.Count(i => i.IsSelected));

        switch (viewModel.Result)
        {
            case CreateStepDefinitionsDialogResult.Create:
                SaveAsStepDefinitionClass(projectScope, combinedSnippet, viewModel.ClassName, indent, newLine);
                break;
            case CreateStepDefinitionsDialogResult.CopyToClipboard:
                Logger.LogVerbose($"Copy to clipboard: {combinedSnippet}");
                IdeScope.Actions.SetClipboardText(combinedSnippet);
                Finished.Set();
                break;
        }

        return true;
    }

    private void SaveAsStepDefinitionClass(IProjectScope projectScope, string combinedSnippet, string className,
        string indent, string newLine)
    {
        string targetFolder = projectScope.ProjectFolder;
        var projectSettings = projectScope.GetProjectSettings();
        var defaultNamespace = projectSettings.DefaultNamespace ?? projectScope.ProjectName;
        var fileNamespace = defaultNamespace;
        var stepDefinitionsFolder = Path.Combine(targetFolder, "StepDefinitions");
        if (IdeScope.FileSystem.Directory.Exists(stepDefinitionsFolder))
        {
            targetFolder = stepDefinitionsFolder;
            fileNamespace = fileNamespace + ".StepDefinitions";
        }
        var projectTraits = projectScope.GetProjectSettings().ReqnrollProjectTraits;
        var isSpecFlow = projectTraits.HasFlag(ReqnrollProjectTraits.LegacySpecFlow) || projectTraits.HasFlag(ReqnrollProjectTraits.SpecFlowCompatibility);
        var libraryNameSpace = isSpecFlow ? "SpecFlow" : "Reqnroll";

        var template = "using System;" + newLine +
                       $"using {libraryNameSpace};" + newLine +
                       newLine +
                       $"namespace {fileNamespace}" + newLine +
                       "{" + newLine +
                       $"{indent}[Binding]" + newLine +
                       $"{indent}public class {className}" + newLine +
                       $"{indent}{{" + newLine +
                       combinedSnippet +
                       $"{indent}}}" + newLine +
                       "}" + newLine;

        var targetFile = FileDetails
            .FromPath(targetFolder, className + ".cs")
            .WithCSharpContent(template);

        if (IdeScope.FileSystem.File.Exists(targetFile.FullName))
            if (IdeScope.Actions.ShowSyncQuestion("Overwrite file?",
                    $"The selected step definition file '{targetFile}' already exists. By overwriting the existing file you might lose work. {Environment.NewLine}Do you want to overwrite the file?",
                    defaultButton: MessageBoxResult.No) != MessageBoxResult.Yes)
                return;

        projectScope.AddFile(targetFile, template);
        projectScope.IdeScope.Actions.NavigateTo(new SourceLocation(targetFile, 9, 1));
        IDiscoveryService discoveryService = projectScope.GetDiscoveryService();

        projectScope.IdeScope.FireAndForget(
            () => RebuildBindingRegistry(discoveryService, targetFile), _ => { Finished.Set(); });
    }

    private async Task RebuildBindingRegistry(IDiscoveryService discoveryService,
        CSharpStepDefinitionFile stepDefinitionFile)
    {
        await discoveryService.BindingRegistryCache
            .Update(bindingRegistry => bindingRegistry.ReplaceStepDefinitions(stepDefinitionFile));

        Finished.Set();
    }
}
