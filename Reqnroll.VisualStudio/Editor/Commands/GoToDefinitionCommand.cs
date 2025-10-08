#nullable disable

namespace Reqnroll.VisualStudio.Editor.Commands;

[Export(typeof(IDeveroomFeatureEditorCommand))]
public class GoToDefinitionCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
{
    private const string GoToStepDefinitionsPopupHeader = "Go to step definitions";

    [ImportingConstructor]
    public GoToDefinitionCommand(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider)
        : base(ideScope, aggregatorFactory, taggerProvider)
    {
    }

    public override DeveroomEditorCommandTargetKey[] Targets => new[]
    {
        new DeveroomEditorCommandTargetKey(VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd97CmdID.GotoDefn)
    };

    public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default) => InvokeCommand(textView);

    internal bool InvokeCommand(IWpfTextView textView, Action<ProjectBinding> continueWithAfterJump = null)
    {
        Logger.LogVerbose("Go To Definition");

        var textBuffer = textView.TextBuffer;

        var stepTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.StepBlock);

        var matchedStepTag = stepTag.ChildTags.FirstOrDefault(t =>
            t.Type == DeveroomTagTypes.DefinedStep || t.Type == DeveroomTagTypes.UndefinedStep);
        if (matchedStepTag != null &&
            matchedStepTag.Data is MatchResult matchResult)
        {
            if (matchResult.HasSingleMatch)
            {
                var matchResultItem = matchResult.Items.First();
                PerformGoToDefinition(matchResultItem, textBuffer, continueWithAfterJump);
            }
            else
            {
                Logger.LogVerbose($"Jump to list step: {matchResult}");
                IdeScope.Actions.ShowSyncContextMenu(GoToStepDefinitionsPopupHeader, matchResult.Items.Select(m =>
                    new ContextMenuItem(m.ToString(),
                        _ => { PerformGoToDefinition(m, textBuffer, continueWithAfterJump); }, GetIcon(m))
                ).ToArray());
            }
        }
        else
        {
            var goToHookCommand = new GoToHooksCommand(IdeScope, AggregatorFactory, DeveroomTaggerProvider);
            goToHookCommand.InvokeCommand(textView, continueWithAfterJump);
        }

        return true;
    }

    private void PerformGoToDefinition(MatchResultItem match, ITextBuffer textBuffer,
        Action<ProjectStepDefinitionBinding> continueWithAfterJump)
    {
        MonitoringService.MonitorCommandGoToStepDefinition(match.Type == MatchResultType.Undefined);
        switch (match.Type)
        {
            case MatchResultType.Undefined:
                PerformOfferCopySnippet(match, textBuffer);
                break;
            case MatchResultType.Defined:
            case MatchResultType.Ambiguous:

                PerformJump(match, match.MatchedStepDefinition, match.MatchedStepDefinition?.Implementation, continueWithAfterJump);
                break;
        }
    }

    private void PerformOfferCopySnippet(MatchResultItem match, ITextBuffer textBuffer)
    {
        Debug.Assert(match.UndefinedStep != null);
        var snippetService = GetProjectScope(textBuffer).GetSnippetService();
        if (snippetService == null)
            return;

        const string indent = "    ";
        string newLine = Environment.NewLine;

        var snippet = snippetService.GetStepDefinitionSkeletonSnippet(match.UndefinedStep,
            snippetService.DefaultExpressionStyle, snippetService.DefaultGenerateSkeletonMethodsAsAsync, indent, newLine);

        IdeScope.Actions.ShowQuestion(new QuestionDescription(GoToStepDefinitionsPopupHeader,
            $"The step is undefined. Do you want to copy a step definition skeleton snippet to the clipboard?{Environment.NewLine}{Environment.NewLine}{snippet}",
            _ => PerformCopySnippet(snippet.Indent(indent + indent))));
    }

    private void PerformCopySnippet(string snippet)
    {
        Logger.LogVerbose($"Copy to clipboard: {snippet}");
        IdeScope.Actions.SetClipboardText(snippet);
    }

    private IProjectScope GetProjectScope(ITextBuffer textBuffer) => IdeScope.GetProject(textBuffer);

    private string GetIcon(MatchResultItem matchResult)
    {
        switch (matchResult.Type)
        {
            case MatchResultType.Defined:
                if (matchResult.HasErrors)
                    return "StepDefinitionsDefinedInvalid";
                return "StepDefinitionsDefined";
            case MatchResultType.Ambiguous:
                return "StepDefinitionsAmbiguous";
            case MatchResultType.Undefined:
                return "StepDefinitionsUndefined";
        }

        return null;
    }
}
