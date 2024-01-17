namespace Reqnroll.VisualStudio.Editor.Completions.Infrastructure;

public abstract class CompletionCommandBase : DeveroomEditorTypeCharCommandBase
{
    private static readonly DeveroomEditorCommandTargetKey CompleteWordCommand =
        new(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.COMPLETEWORD);

    private static readonly DeveroomEditorCommandTargetKey AutoCompleteCommand =
        new(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.AUTOCOMPLETE);

    internal static readonly DeveroomEditorCommandTargetKey ReturnCommand = new(VSConstants.VSStd2K,
        VSConstants.VSStd2KCmdID.RETURN);

    private static readonly DeveroomEditorCommandTargetKey TabCommand = new(VSConstants.VSStd2K,
        VSConstants.VSStd2KCmdID.TAB);

    private static readonly DeveroomEditorCommandTargetKey CancelCommand = new(VSConstants.VSStd2K,
        VSConstants.VSStd2KCmdID.CANCEL);

    private static readonly DeveroomEditorCommandTargetKey BackspaceCommand =
        new(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.BACKSPACE);

    private static readonly DeveroomEditorCommandTargetKey[] AutoCompleteCommands =
    {
        CompleteWordCommand, AutoCompleteCommand
    };

    protected readonly ICompletionBroker _completionBroker;

    protected CompletionCommandBase(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider,
        ICompletionBroker completionBroker)
        : base(ideScope, aggregatorFactory, taggerProvider)
    {
        _completionBroker = completionBroker;
    }

    private DeveroomEditorCommandTargetKey TypeCharCommand => base.Target;

    public override DeveroomEditorCommandTargetKey[] Targets => new[]
    {
        CompleteWordCommand,
        AutoCompleteCommand,
        ReturnCommand,
        TabCommand,
        CancelCommand,
        BackspaceCommand,
        base.Target
    };

    protected abstract bool ShouldStartSessionOnTyping(IWpfTextView textView, char? ch, bool isSessionActive);

    protected virtual CompletionSessionManager CreateCompletionSessionManager(IWpfTextView textView) =>
        new(textView, _completionBroker);

    public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default)
    {
        var sessionManager = GetCompletionSessionManager(textView);

        if (AutoCompleteCommands.Contains(commandKey))
            return sessionManager.TriggerCompletion();

        if (commandKey.Equals(ReturnCommand))
            return sessionManager.Complete(false);

        if (commandKey.Equals(TabCommand))
            return sessionManager.Complete(true);

        if (commandKey.Equals(CancelCommand))
            return sessionManager.Cancel();

        return false;
    }

    public override bool PostExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default)
    {
        var sessionManager = GetCompletionSessionManager(textView);

        if (commandKey.Equals(TypeCharCommand))
            return base.PostExec(textView, commandKey, inArgs);

        if (commandKey.Equals(BackspaceCommand))
            return sessionManager.Filter();

        if (!sessionManager.IsActive &&
            (commandKey.Equals(ReturnCommand) || commandKey.Equals(TabCommand) ||
             AutoCompleteCommands.Contains(commandKey)) &&
            ShouldStartSessionOnTyping(textView, null, sessionManager.IsActive))
            return sessionManager.TriggerCompletion();

        return false;
    }

    protected internal override bool PostExec(IWpfTextView textView, char ch)
    {
        var sessionManager = GetCompletionSessionManager(textView);

        if (ShouldStartSessionOnTyping(textView, ch, sessionManager.IsActive))
        {
            if (sessionManager.IsActive)
                sessionManager.Cancel();
            return sessionManager.TriggerCompletion();
        }

        if (sessionManager.IsActive)
            return sessionManager.Filter();

        return false;
    }

    private CompletionSessionManager GetCompletionSessionManager(IWpfTextView textView)
    {
        return textView.Properties.GetOrCreateSingletonProperty(() => CreateCompletionSessionManager(textView));
    }
}
