namespace Reqnroll.VisualStudio.Tests.Editor.Commands;

public abstract class CommandTestBase<T> : EditorTestBase where T : DeveroomEditorCommandBase
{
    private readonly Func<InMemoryStubProjectScope, IDeveroomTaggerProvider, T> _commandFactory;
    protected readonly string WarningHeader;

    protected CommandTestBase(
        ITestOutputHelper testOutputHelper,
        Func<InMemoryStubProjectScope, IDeveroomTaggerProvider, T> commandFactory,
        string warningHeader) : base(testOutputHelper)
    {
        _commandFactory = commandFactory;
        WarningHeader = warningHeader;
    }

    protected Task<(IWpfTextView textView, T command)> ArrangeSut(
        TestStepDefinition stepDefinition, TestFeatureFile featureFile)
    {
        var stepDefinitions = stepDefinition.IsVoid
            ? Array.Empty<TestStepDefinition>()
            : new[] {stepDefinition};

        var featureFiles = featureFile.IsVoid
            ? Array.Empty<TestFeatureFile>()
            : new[] {featureFile};

        return ArrangeSut(stepDefinitions, featureFiles);
    }

    protected async Task<(IWpfTextView textView, T command)> ArrangeSut(
        TestStepDefinition[] stepDefinitions,
        TestFeatureFile[] featureFiles)
    {
        //new InMemoryStubProjectBuilder(ProjectScope).TriggerBuild();
        var textView = await ArrangeTextView(stepDefinitions, featureFiles);
        var ideScope = ProjectScope.IdeScope;
        var taggerProvider = CreateTaggerProvider(ideScope);
        var command = _commandFactory(ProjectScope, taggerProvider);
        return (textView, command);
    }

    protected Task InvokeAndWaitAnalyticsEvent(T command, IWpfTextView textView)
    {
        Invoke(command, textView);
        return WaitForCommandToComplete(command);
    }

    protected static bool Invoke(T command, IWpfTextView textView) =>
        command.PreExec(textView, command.Targets.First());

    protected async Task WaitForCommandToComplete(T command)
    {
        using CancellationTokenSource cts = new DebuggableCancellationTokenSource(TimeSpan.FromSeconds(10));
        await command.Finished.WaitAsync(cts.Token);
    }

    public ImmutableArray<string> WarningMessages()
    {
        var stubLogger = GetStubLogger();
        return stubLogger.Warnings().WithoutHeader(WarningHeader).Messages;
    }
}
