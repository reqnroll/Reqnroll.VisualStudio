#nullable disable
using Microsoft.CodeAnalysis;
using Project = EnvDTE.Project;

namespace Reqnroll.VisualStudio.ProjectSystem;

public class NullVsIdeScope : IVsIdeScope
{
    public NullVsIdeScope(IDeveroomLogger logger, IServiceProvider serviceProvider,
        IMonitoringService monitoringService)
    {
        Logger = logger;
        MonitoringService = monitoringService;
        ServiceProvider = serviceProvider;
        WindowManager = new DeveroomWindowManager(serviceProvider, monitoringService);
        FileSystem = new FileSystem();
        Actions = new NullIdeActions(this);
        Dte = null;
        DeveroomOutputPaneServices = null;
        DeveroomErrorListServices = new NullDeveroomErrorListServices();
    }

    public bool IsSolutionLoaded { get; } = false;

    public IDeveroomLogger Logger { get; }
    public IMonitoringService MonitoringService { get; }
    public IIdeActions Actions { get; }
    public IDeveroomWindowManager WindowManager { get; }
    public IFileSystem FileSystem { get; }

    public event EventHandler<EventArgs> WeakProjectsBuilt
    {
        add { }
        remove { }
    }

    public event EventHandler<EventArgs> WeakProjectOutputsUpdated
    {
        add { }
        remove { }
    }

    public IServiceProvider ServiceProvider { get; }
    public DTE Dte { get; }
    public IDeveroomOutputPaneServices DeveroomOutputPaneServices { get; }
    public IDeveroomErrorListServices DeveroomErrorListServices { get; }

    public void CalculateSourceLocationTrackingPositions(IEnumerable<SourceLocation> sourceLocations)
    {
    }

    public IProjectScope[] GetProjectsWithFeatureFiles() => Array.Empty<IProjectScope>();

    public IDisposable CreateUndoContext(string undoLabel) => null;

    public bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer)
    {
        textBuffer = default;
        return false;
    }

    public void FireAndForget(Func<Task> action, Action<Exception> onException,
        [CallerMemberName] string callerName = "???")
    {
    }

    public void FireAndForgetOnBackgroundThread(Func<CancellationToken, Task> action, string callerName = "???")
    {
    }

    public Task RunOnUiThread(Action action) => Task.CompletedTask;

    public void OpenIfNotOpened(string path)
    {
    }

    public IProjectScope GetProject(ITextBuffer textBuffer) => null;

    public void Dispose()
    {
        //nop
    }

    public IProjectScope GetProjectScope(Project project) => throw new NotImplementedException();

    public SyntaxTree GetSyntaxTree(ITextBuffer textBuffer) => null;

    public static IMonitoringService GetNullMonitoringService() => new NullMonitoringService();

    private class NullIdeActions : VsIdeActionsBase, IIdeActions, IAsyncContextMenu
    {
        public NullIdeActions(IVsIdeScope ideScope) : base(ideScope)
        {
        }

        public CancellationToken CancellationToken { get; } = new();
        public bool IsComplete => true;

        public void AddItems(params ContextMenuItem[] items)
        {
            //nop
        }

        public void Complete()
        {
            //nop
        }

        public bool NavigateTo(SourceLocation sourceLocation) => false;

        public IAsyncContextMenu ShowContextMenu(string header, bool async,
            params ContextMenuItem[] contextMenuItems) =>
            this;
    }

    private class NullMonitoringService : IMonitoringService
    {
        public void MonitorLoadProjectSystem()
        {
        }

        public void MonitorOpenProjectSystem(IIdeScope ideScope)
        {
        }

        public void MonitorOpenProject(ProjectSettings settings, int? featureFileCount)
        {
        }

        public void MonitorOpenFeatureFile(ProjectSettings projectSettings)
        {
        }

        public void MonitorExtensionInstalled()
        {
        }

        public void MonitorExtensionUpgraded(string oldExtensionVersion)
        {
        }

        public void MonitorExtensionDaysOfUsage(int usageDays)
        {
        }

        public void MonitorParserParse(ProjectSettings settings, Dictionary<string, object> additionalProps = null)
        {
        }

        public void MonitorCommandCommentUncomment()
        {
        }

        public void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount)
        {
        }

        public void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled)
        {
        }

        public void MonitorCommandFindUnusedStepDefinitions(int unusedStepDefinitions, int scannedFeatureFiles, bool isCancellationRequested)
        {
        }

        public void MonitorCommandGoToStepDefinition(bool generateSnippet)
        {
        }

        public void MonitorCommandGoToHook()
        {
        }

        public void MonitorCommandAutoFormatTable()
        {
        }

        public void MonitorCommandAutoFormatDocument(bool isSelectionFormatting)
        {
        }

        public void MonitorCommandAddFeatureFile(ProjectSettings projectSettings)
        {
        }

        public void MonitorCommandAddReqnrollConfigFile(ProjectSettings projectSettings)
        {
        }

        public void MonitorCommandRenameStepExecuted(RenameStepCommandContext ctx)
        {
        }

        public void MonitorReqnrollDiscovery(bool isFailed, string errorMessage, int stepDefinitionCount,
            ProjectSettings projectSettings)
        {
        }

        public void MonitorReqnrollGeneration(bool isFailed, ProjectSettings projectSettings)
        {
        }

        public void MonitorError(Exception exception, bool? isFatal = null)
        {
        }

        public void MonitorProjectTemplateWizardStarted()
        {
        }

        public void MonitorProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework,
            bool addFluentAssertions)
        {
        }

        public void MonitorNotificationShown(NotificationData notification)
        {
        }

        public void MonitorNotificationDismissed(NotificationData notification)
        {
        }

        public void MonitorLinkClicked(string source, string url, Dictionary<string, object> additionalProps = null)
        {
        }

        public void MonitorUpgradeDialogDismissed(Dictionary<string, object> additionalProps)
        {
        }

        public void MonitorWelcomeDialogDismissed(Dictionary<string, object> additionalProps)
        {
        }

        public void TransmitEvent(IAnalyticsEvent runtimeEvent)
        {
        }

    }
}
