namespace Reqnroll.VisualStudio.Editor.Commands;

[Export(typeof(IDeveroomCodeEditorCommand))]
[Export(typeof(IDeveroomFeatureEditorCommand))]
public class RenameStepCommand : DeveroomEditorCommandBase, IDeveroomCodeEditorCommand, IDeveroomFeatureEditorCommand
{
    private const string ChooseStepDefinitionPopupHeader = "Choose step definition to rename";

    private const string NonParameterPartsCannotContainExpressionOperators =
        "The non-parameter parts cannot contain expression operators";

    private readonly RenameStepFeatureFileAction _renameStepFeatureFileAction;
    private readonly RenameStepStepDefinitionClassAction _renameStepStepDefinitionClassAction;

    [ImportingConstructor]
    public RenameStepCommand(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider)
        : base(ideScope, aggregatorFactory, taggerProvider)
    {
        _renameStepFeatureFileAction = new RenameStepFeatureFileAction();
        _renameStepStepDefinitionClassAction = new RenameStepStepDefinitionClassAction();
    }

    public override DeveroomEditorCommandTargetKey[] Targets => new[]
    {
        new DeveroomEditorCommandTargetKey(ReqnrollVsCommands.DefaultCommandSet, ReqnrollVsCommands.RenameStepCommandId)
    };

    public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default)
    {
        Logger.LogVerbose("Rename Step");
        var ctx = new RenameStepCommandContext(IdeScope, DeveroomTaggerProvider);

        if (textView.TextBuffer.ContentType.IsOfType(VsContentTypes.FeatureFile))
        {
            var goToStepDefinitionCommand =
                new GoToStepDefinitionCommand(IdeScope, AggregatorFactory, DeveroomTaggerProvider);
            goToStepDefinitionCommand.InvokeCommand(textView, projectStepDefinitionBinding =>
            {
                ctx.StepDefinitionBinding = projectStepDefinitionBinding;
                var sourceLocation = projectStepDefinitionBinding.Implementation.SourceLocation;
                IdeScope.GetTextBuffer(sourceLocation, out var textBuffer);
                ctx.TextBufferOfStepDefinitionClass = textBuffer;
                var stepDefLine =
                    ctx.TextBufferOfStepDefinitionClass.CurrentSnapshot.GetLineFromLineNumber(
                        sourceLocation.SourceFileLine - 1);
                ctx.TriggerPointOfStepDefinitionClass = new SnapshotPoint(
                    ctx.TextBufferOfStepDefinitionClass.CurrentSnapshot,
                    stepDefLine.Start.Position + sourceLocation.SourceFileColumn - 1);

                InvokeCommandFromStepDefinitionClass(ctx);
            });
            return true;
        }

        ctx.TriggerPointOfStepDefinitionClass = textView.Caret.Position.BufferPosition;
        ctx.TextBufferOfStepDefinitionClass = textView.TextBuffer;
        InvokeCommandFromStepDefinitionClass(ctx);
        return true;
    }

    private void InvokeCommandFromStepDefinitionClass(RenameStepCommandContext ctx)
    {
        ValidateCallerProject(ctx);
        if (Erroneous(ctx)) return;

        ValidateProjectsWithFeatureFiles(ctx);
        if (Erroneous(ctx)) return;

        var stepDefinitions = CollectStepDefinitions(ctx);

        PerformActions(stepDefinitions, ctx);
    }

    private List<(IProjectScope reqnrollTestProject, ProjectStepDefinitionBinding projectStepDefinitionBinding)>
        CollectStepDefinitions(RenameStepCommandContext ctx)
    {
        var stepDefinitions =
            new List<(IProjectScope reqnrollTestProject, ProjectStepDefinitionBinding projectStepDefinitionBinding)>();
        foreach (IProjectScope reqnrollTestProject in ctx.ReqnrollTestProjectsWithFeatureFiles)
        {
            ProjectStepDefinitionBinding[] projectStepDefinitions = GetStepDefinitions(ctx);
            foreach (var projectStepDefinitionBinding in projectStepDefinitions)
            {
                if (ctx.StepDefinitionBinding == null)
                    stepDefinitions.Add((reqnrollTestProject, projectStepDefinitionBinding));

                if (ctx.StepDefinitionBinding == projectStepDefinitionBinding)
                {
                    stepDefinitions.Add((reqnrollTestProject, projectStepDefinitionBinding));
                    return stepDefinitions;
                }
            }
        }

        return stepDefinitions;
    }

    private void PerformActions(
        IReadOnlyList<(IProjectScope reqnrollTestProject, ProjectStepDefinitionBinding projectStepDefinitionBinding)>
            stepDefinitions, RenameStepCommandContext ctx)
    {
        switch (stepDefinitions.Count)
        {
            case 0:
                ctx.AddCriticalProblem("No step definition found that is related to this position");
                NotifyUserAboutIssues(ctx);
                break;
            case 1:
            {
                var selectedStepDefinition = stepDefinitions[0];
                PerformActionsOnSelectedStepDefinition(selectedStepDefinition.reqnrollTestProject,
                    selectedStepDefinition.projectStepDefinitionBinding, ctx);
                break;
            }
            default:
            {
                Logger.LogVerbose(
                    $"Choose step definitions from: {string.Join(", ", stepDefinitions.Select(sd => sd.projectStepDefinitionBinding.ToString()))}");
                IdeScope.Actions.ShowSyncContextMenu(ChooseStepDefinitionPopupHeader, stepDefinitions.Select(sd =>
                    new ContextMenuItem(sd.projectStepDefinitionBinding.ToString(),
                        _ =>
                        {
                            PerformActionsOnSelectedStepDefinition(sd.reqnrollTestProject,
                                sd.projectStepDefinitionBinding,
                                ctx);
                        },
                        "StepDefinitionsDefined")
                ).ToArray());
                break;
            }
        }
    }

    private void PerformActionsOnSelectedStepDefinition(IProjectScope stepDefinitionProjectScope,
        ProjectStepDefinitionBinding stepDefinitionBinding, RenameStepCommandContext ctx)
    {
        ctx.StepDefinitionBinding = stepDefinitionBinding;
        ctx.StepDefinitionProjectScope = stepDefinitionProjectScope;
        ExpressionIsValidAndSupported(ctx);
        if (Erroneous(ctx)) return;

        RenameStepViewModel viewModel = PrepareViewModel(ctx);
        var result = IdeScope.WindowManager.ShowDialog(viewModel);
        if (result != true)
            return;

        viewModel.ParsedUpdatedExpression = ctx.StepDefinitionExpressionAnalyzer.Parse(viewModel.StepText);

        var validationErrors = Validate(ctx, viewModel.StepText);
        ctx.Issues.AddRange(validationErrors.Select(error => new Problem(Problem.ProblemKind.Critical, error)));
        if (Erroneous(ctx)) return;

        ctx.UpdatedExpression = viewModel.StepText;
        ctx.AnalyzedUpdatedExpression = viewModel.ParsedUpdatedExpression;

        PerformModifications(ctx);
    }

    private void PerformModifications(RenameStepCommandContext ctx)
    {
        InvokeOnBackgroundThread(ctx, async () =>
        {
            using (IdeScope.CreateUndoContext("Rename steps"))
            {
                await _renameStepFeatureFileAction.PerformRenameStep(ctx);
                await _renameStepStepDefinitionClassAction.PerformRenameStep(ctx);
            }

            IdeScope.Actions.NavigateTo(ctx.StepDefinitionBinding.Implementation.SourceLocation);
            await UpdateBindingRegistry(ctx);
            NotifyUserAboutIssues(ctx);
        });
    }

    private void InvokeOnBackgroundThread(RenameStepCommandContext ctx, Func<Task> action)
    {
        ctx.IdeScope.FireAndForget(async () => { await action(); }, exception =>
        {
            ctx.AddCriticalProblem(exception.Message);
            NotifyUserAboutIssues(ctx);
        });
    }

    private void NotifyUserAboutIssues(RenameStepCommandContext ctx)
    {
        if (!ctx.Issues.Any())
        {
            MonitoringService.MonitorCommandRenameStepExecuted(ctx);
            Finished.Set();
            return;
        }

        ShowProblem(ctx);
    }

    private bool Erroneous(RenameStepCommandContext ctx)
    {
        if (!ctx.IsErroneous) return false;
        ShowProblem(ctx);
        return true;
    }

    private void ShowProblem(RenameStepCommandContext ctx)
    {
        var problems = string.Join(Environment.NewLine, ctx.Issues.Select(issue => issue.Description));
        IdeScope.Actions.ShowProblem(
            $"The following problems occurred:{Environment.NewLine}{problems}", "Rename Step");
        MonitoringService.MonitorCommandRenameStepExecuted(ctx);
        Finished.Set();
    }

    private void ValidateProjectsWithFeatureFiles(RenameStepCommandContext ctx)
    {
        ctx.ReqnrollTestProjectsWithFeatureFiles = IdeScope.GetProjectsWithFeatureFiles()
            .Where(p => p.GetProjectSettings().IsReqnrollTestProject)
            .ToArray();

        if (ctx.ReqnrollTestProjectsWithFeatureFiles.Length == 0)
            ctx.AddCriticalProblem(
                "Unable to find step definition usages: could not find any Reqnroll project with feature files.");
    }

    private void ValidateCallerProject(RenameStepCommandContext ctx)
    {
        ctx.ProjectOfStepDefinitionClass = IdeScope.GetProject(ctx.TextBufferOfStepDefinitionClass);
        if (ctx.ProjectOfStepDefinitionClass == null)
            ctx.AddCriticalProblem("Unable to find step definition usages: the project is not initialized yet.");
        else if (!ctx.ProjectOfStepDefinitionClass.GetProjectSettings().IsReqnrollProject)
            ctx.AddCriticalProblem(
                "Unable to find step definition usages: the project is not detected to be a Reqnroll project.");
    }

    private void ExpressionIsValidAndSupported(RenameStepCommandContext ctx)
    {
        switch (ctx.StepDefinitionBinding.Expression)
        {
            case null:
                ctx.AddCriticalProblem("Unable to rename step, the step definition expression cannot be detected.");
                return;
            case "":
                ctx.AddCriticalProblem("Step definition expression is invalid");
                return;
        }

        ctx.StepDefinitionExpressionAnalyzer = new RegexStepDefinitionExpressionAnalyzer();
        ctx.AnalyzedOriginalExpression =
            ctx.StepDefinitionExpressionAnalyzer.Parse(ctx.StepDefinitionBinding.Expression);

        if (!ctx.AnalyzedOriginalExpression.ContainsOnlySimpleText)
            ctx.AddCriticalProblem(NonParameterPartsCannotContainExpressionOperators);
    }

    private static RenameStepViewModel PrepareViewModel(RenameStepCommandContext ctx)
    {
        var viewModel = new RenameStepViewModel(ctx.StepDefinitionBinding,
            updatedExpression => Validate(ctx, updatedExpression));

        return viewModel;
    }

    public static ImmutableHashSet<string> Validate(RenameStepCommandContext ctx, string updatedExpression)
    {
        var errors = new HashSet<string>();
        var parsedUpdatedExpression = ctx.StepDefinitionExpressionAnalyzer.Parse(updatedExpression);
        if (parsedUpdatedExpression.Parts.Length != ctx.AnalyzedOriginalExpression.Parts.Length)
            errors.Add("Parameter count mismatch");

        if (ctx.AnalyzedOriginalExpression.ParameterParts
            .Zip(parsedUpdatedExpression.ParameterParts, (original, updated) => original == updated)
            .Any(eq => !eq))
            errors.Add("Parameter expression mismatch");

        if (!parsedUpdatedExpression.ContainsOnlySimpleText)
            errors.Add(NonParameterPartsCannotContainExpressionOperators);


        return errors.ToImmutableHashSet();
    }

    private ProjectStepDefinitionBinding[] GetStepDefinitions(RenameStepCommandContext ctx)
    {
        var fileName = GetEditorDocumentPath(ctx.TextBufferOfStepDefinitionClass);
        var bindingRegistry = GetBindingRegistry(ctx);
        return FindStepDefinitionUsagesCommand.GetStepDefinitions(fileName, ctx.TriggerPointOfStepDefinitionClass,
            bindingRegistry);
    }

    private ProjectBindingRegistry GetBindingRegistry(RenameStepCommandContext ctx)
    {
        var discoveryService = ctx.ProjectOfStepDefinitionClass.GetDiscoveryService();
        var bindingRegistry = discoveryService.BindingRegistryCache.Value;
        if (bindingRegistry == ProjectBindingRegistry.Invalid)
            Logger.LogWarning(
                $"Unable to get step definitions from project '{ctx.ProjectOfStepDefinitionClass.ProjectName}', usages will not be found for this project.");
        return bindingRegistry;
    }

    private Task UpdateBindingRegistry(RenameStepCommandContext ctx)
    {
        var discoveryService = ctx.ProjectOfStepDefinitionClass.GetDiscoveryService();
        return discoveryService.BindingRegistryCache.Update(bindingRegistry =>
            bindingRegistry.ReplaceStepDefinition(ctx.StepDefinitionBinding,
                ctx.StepDefinitionBinding.WithSpecifiedExpression(ctx.UpdatedExpression)));
    }
}
