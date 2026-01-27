#nullable disable
namespace Reqnroll.VisualStudio.Editor.Commands;

[Export(typeof(IDeveroomFeatureEditorCommand))]
public class DefineStepsCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
{
    private readonly IEditorConfigOptionsProvider _editorConfigOptionsProvider;

    [ImportingConstructor]
    public DefineStepsCommand(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider,
        IEditorConfigOptionsProvider editorConfigOptionsProvider)
        : base(ideScope, aggregatorFactory, taggerProvider)
    {
        _editorConfigOptionsProvider = editorConfigOptionsProvider;
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

        foreach (var undefinedStepTag in undefinedStepTags)
        {
            var matchResult = (MatchResult) undefinedStepTag.Data;
            foreach (var match in matchResult.Items.Where(mi => mi.Type == MatchResultType.Undefined))
            {
                var snippet = snippetService.GetStepDefinitionSkeletonSnippet(match.UndefinedStep,
                    viewModel.ExpressionStyle, indent, newLine);
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
            viewModel.Items.Where(i => i.IsSelected).Select(i => i.Snippet.Indent(indent)));

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
            fileNamespace += ".StepDefinitions";
        }

        // Get C# code generation configuration from EditorConfig using target .cs file path
        var targetFilePath = Path.Combine(targetFolder, className + ".cs");
        var csharpConfig = new CSharpCodeGenerationConfiguration();
        var editorConfigOptions = _editorConfigOptionsProvider.GetEditorConfigOptionsByPath(targetFilePath);
        editorConfigOptions.UpdateFromEditorConfig(csharpConfig);

        var projectTraits = projectScope.GetProjectSettings().ReqnrollProjectTraits;
        var generatedContent = GenerateStepDefinitionClass(
            combinedSnippet, 
            className, 
            fileNamespace, 
            projectTraits, 
            csharpConfig, 
            indent, 
            newLine);

        var targetFile = FileDetails
            .FromPath(targetFolder, className + ".cs")
            .WithCSharpContent(generatedContent);

        if (IdeScope.FileSystem.File.Exists(targetFile.FullName))
            if (IdeScope.Actions.ShowSyncQuestion("Overwrite file?",
                    $"The selected step definition file '{targetFile}' already exists. By overwriting the existing file you might lose work. {Environment.NewLine}Do you want to overwrite the file?",
                    defaultButton: MessageBoxResult.No) != MessageBoxResult.Yes)
                return;

        projectScope.AddFile(targetFile, generatedContent);
        projectScope.IdeScope.Actions.NavigateTo(new SourceLocation(targetFile, 9, 1));
        IDiscoveryService discoveryService = projectScope.GetDiscoveryService();

        projectScope.IdeScope.FireAndForget(
            () => RebuildBindingRegistry(discoveryService, targetFile), _ => { Finished.Set(); });
    }

    internal static string GenerateStepDefinitionClass(
        string combinedSnippet,
        string className,
        string fileNamespace,
        ReqnrollProjectTraits projectTraits,
        CSharpCodeGenerationConfiguration csharpConfig,
        string indent,
        string newLine)
    {
        var isSpecFlow = projectTraits.HasFlag(ReqnrollProjectTraits.LegacySpecFlow) || 
                         projectTraits.HasFlag(ReqnrollProjectTraits.SpecFlowCompatibility);
        var libraryNameSpace = isSpecFlow ? "SpecFlow" : "Reqnroll";

        // Estimate template size for StringBuilder capacity
        var estimatedSize = 200 + fileNamespace.Length + className.Length + combinedSnippet.Length;
        var template = new StringBuilder(estimatedSize);
        template.AppendLine("using System;");
        template.AppendLine($"using {libraryNameSpace};");
        template.AppendLine();

        // Determine indentation level based on namespace style
        var classIndent = csharpConfig.UseFileScopedNamespaces ? "" : indent;
        
        // Add namespace declaration
        if (csharpConfig.UseFileScopedNamespaces)
        {
            template.AppendLine($"namespace {fileNamespace};");
            template.AppendLine();
        }
        else
        {
            template.AppendLine($"namespace {fileNamespace}");
            template.AppendLine("{");
        }

        // Add class declaration (common structure with appropriate indentation)
        template.AppendLine($"{classIndent}[Binding]");
        template.AppendLine($"{classIndent}public class {className}");
        template.AppendLine($"{classIndent}{{");
        
        // Add snippet with appropriate indentation based on namespace style
        if (csharpConfig.UseFileScopedNamespaces)
        {
            template.AppendLine(combinedSnippet);
        }
        else
        {
            AppendLinesWithIndent(template, combinedSnippet, indent, newLine);
        }
        
        template.AppendLine($"{classIndent}}}");
        
        // Close namespace if block-scoped
        if (!csharpConfig.UseFileScopedNamespaces)
        {
            template.AppendLine("}");
        }

        return template.ToString();
    }

    private async Task RebuildBindingRegistry(IDiscoveryService discoveryService,
        CSharpStepDefinitionFile stepDefinitionFile)
    {
        await discoveryService.BindingRegistryCache
            .Update(bindingRegistry => bindingRegistry.ReplaceStepDefinitions(stepDefinitionFile));

        Finished.Set();
    }

    private static void AppendLinesWithIndent(StringBuilder builder, string content, string indent, string newLine)
    {
        if (string.IsNullOrEmpty(content))
            return;

        var lines = content.Split(new[] { newLine }, StringSplitOptions.None);
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Add indentation to non-empty lines
            if (!string.IsNullOrWhiteSpace(line))
            {
                builder.Append(indent).AppendLine(line);
            }
            else
            {
                builder.AppendLine(line);
            }
        }
    }
}
