namespace Reqnroll.VisualStudio.Editor.Commands;

[Export(typeof(IDeveroomFeatureEditorCommand))]
public class GoToHooksCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
{
    private const string GoToHooksPopupHeader = "Go to hooks";

    [ImportingConstructor]
    public GoToHooksCommand(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider)
        : base(ideScope, aggregatorFactory, taggerProvider)
    {
    }

    public override DeveroomEditorCommandTargetKey[] Targets => new[]
    {
        new DeveroomEditorCommandTargetKey(ReqnrollVsCommands.DefaultCommandSet,
            ReqnrollVsCommands.GoToHookCommandId)
    };

    public override DeveroomEditorCommandStatus QueryStatus(IWpfTextView textView,
        DeveroomEditorCommandTargetKey commandKey)
    {
        var projectScope = IdeScope.GetProject(textView.TextBuffer);
        var projectSettings = projectScope?.GetProjectSettings();
        if (projectSettings == null || !projectSettings.IsReqnrollProject || projectSettings.IsSpecFlowProject) 
            return DeveroomEditorCommandStatus.Disabled;
        return base.QueryStatus(textView, commandKey);
    }

    public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default) => InvokeCommand(textView);

    internal bool InvokeCommand(IWpfTextView textView, Action<ProjectBinding>? continueWithAfterJump = null)
    {
        Logger.LogVerbose("Go To Hook");

        var hookReferenceTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.ScenarioHookReference);

        if (hookReferenceTag is { Data: HookMatchResult hookMatchResult })
        {
            Logger.LogVerbose($"Linked to {hookMatchResult.Items.Length} hooks.");
            IdeScope.Actions.ShowSyncContextMenu(GoToHooksPopupHeader, hookMatchResult.Items.Select(hook =>
                new ContextMenuItem(hook.ToString(),
                    _ => { PerformGoToHook(hook, continueWithAfterJump); }, GetIcon(hook))
            ).ToArray());
        }

        return true;
    }

    private void PerformGoToHook(ProjectHookBinding hook, Action<ProjectBinding>? continueWithAfterJump)
    {
        MonitoringService.MonitorCommandGoToHook();
        PerformJump(hook, hook, hook.Implementation, continueWithAfterJump);
    }

    private string? GetIcon(ProjectHookBinding hook)
    {
        switch (hook.HookType)
        {
            case HookType.BeforeTestRun:
            case HookType.BeforeTestThread:
            case HookType.BeforeFeature:
            case HookType.BeforeScenario:
            case HookType.BeforeScenarioBlock:
            case HookType.BeforeStep:
                return "BeforeHook";
            case HookType.AfterTestRun:
            case HookType.AfterTestThread:
            case HookType.AfterFeature:
            case HookType.AfterScenario:
            case HookType.AfterScenarioBlock:
            case HookType.AfterStep:
                return "AfterHook";
            default:
                return null;
        }
    }

}
