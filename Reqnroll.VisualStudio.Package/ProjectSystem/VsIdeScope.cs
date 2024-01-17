#nullable disable
using Microsoft.CodeAnalysis;
using Document = Microsoft.CodeAnalysis.Document;
using Project = EnvDTE.Project;

namespace Reqnroll.VisualStudio.ProjectSystem;

[Export(typeof(VsIdeScope))]
public class VsIdeScope : IVsIdeScope
{
    private readonly CancellationTokenSource _backgroundTaskTokenSource = new();
    private readonly DocumentEventsListener _documentEventsListener;

    private readonly IPersistentSpanFactory _persistentSpanFactory;

    private readonly ConcurrentDictionary<string, VsProjectScope>
        _projectScopes = new(StringComparer.OrdinalIgnoreCase);

    private readonly IVsSolutionEventListener _solutionEventListener;
    private readonly UpdateSolutionEventsListener _updateSolutionEventsListener;

    private bool _activityStarted;

    [ImportingConstructor]
    public VsIdeScope([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        IVsSolutionEventListener solutionEventListener, IMonitoringService monitoringService,
        IDeveroomWindowManager windowManager, IFileSystem fileSystem, DeveroomCompositeLogger compositeLogger)
    {
        Logger = compositeLogger;
        ServiceProvider = serviceProvider;
        MonitoringService = monitoringService;
        FileSystem = fileSystem;

        Dte = (DTE) serviceProvider.GetService(typeof(DTE));
        DeveroomOutputPaneServices = new VsDeveroomOutputPaneServices(this);
        DeveroomErrorListServices = new VsDeveroomErrorListServices(this);

        compositeLogger.Add(new OutputWindowPaneLogger(DeveroomOutputPaneServices));
        Logger.LogVerbose("Creating IDE Scope");
        Actions = new VsIdeActions(this);

        _persistentSpanFactory = VsUtils.ResolveMefDependency<IPersistentSpanFactory>(serviceProvider);

        _solutionEventListener = solutionEventListener;
        _updateSolutionEventsListener = new UpdateSolutionEventsListener(serviceProvider, Logger, true);
        _updateSolutionEventsListener.BuildCompleted += UpdateSolutionEventsListenerOnBuildCompleted;

        _solutionEventListener.Loaded += SolutionEventListenerOnLoaded;
        _solutionEventListener.Closed += SolutionEventListenerOnClosed;
        _solutionEventListener.BeforeCloseProject += SolutionEventListenerOnBeforeCloseProject;

        _documentEventsListener = new DocumentEventsListener(Logger, Dte);

        WindowManager = windowManager;

        IsSolutionLoaded = Dte.Solution.IsOpen;
    }

    public IServiceProvider ServiceProvider { get; }
    public DTE Dte { get; }

    public bool IsSolutionLoaded { get; private set; }

    public IDeveroomLogger Logger { get; }
    public IMonitoringService MonitoringService { get; }
    public IIdeActions Actions { get; }
    public IDeveroomWindowManager WindowManager { get; }
    public IFileSystem FileSystem { get; }
    public IDeveroomOutputPaneServices DeveroomOutputPaneServices { get; }
    public IDeveroomErrorListServices DeveroomErrorListServices { get; }

    public event EventHandler<EventArgs> WeakProjectsBuilt
    {
        add => WeakEventManager<VsIdeScope, EventArgs>.AddHandler(this, nameof(ProjectsBuilt), value);
        remove => WeakEventManager<VsIdeScope, EventArgs>.RemoveHandler(this, nameof(ProjectsBuilt), value);
    }

    public event EventHandler<EventArgs> WeakProjectOutputsUpdated
    {
        add => WeakEventManager<VsIdeScope, EventArgs>.AddHandler(this, nameof(ProjectOutputsUpdated), value);
        remove => WeakEventManager<VsIdeScope, EventArgs>.RemoveHandler(this, nameof(ProjectOutputsUpdated), value);
    }

    public void CalculateSourceLocationTrackingPositions(IEnumerable<SourceLocation> sourceLocations)
    {
        var editorAdaptersFactoryService =
            VsUtils.ResolveMefDependency<IVsEditorAdaptersFactoryService>(ServiceProvider);

        var sourceLocationsByFile = sourceLocations
            .Where(sl => sl.SourceLocationSpan == null)
            .GroupBy(sl => sl.SourceFile);

        int counter = 0;
        foreach (var sourceLocationsForFile in sourceLocationsByFile)
        {
            var sourceFile = sourceLocationsForFile.Key;
            var wpfTextView =
                VsUtils.GetWpfTextViewFromFilePath(sourceFile, ServiceProvider, editorAdaptersFactoryService);

            foreach (var sourceLocation in sourceLocationsForFile)
            {
                counter++;
                sourceLocation.SourceLocationSpan = CreatePersistentTrackingPosition(sourceLocation, wpfTextView);
            }
        }

        Logger.LogVerbose($"{counter} tracking positions calculated");
    }

    public IDisposable CreateUndoContext(string undoLabel) => new DeveroomUndoContext(Dte, undoLabel);

    public bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer)
    {
        if (sourceLocation.SourceLocationSpan?.IsDocumentOpen == true &&
            sourceLocation.SourceLocationSpan?.Document?.TextBuffer != null)
        {
            textBuffer = sourceLocation.SourceLocationSpan.Document.TextBuffer;
            return true;
        }

        var editorAdaptersFactoryService =
            VsUtils.ResolveMefDependency<IVsEditorAdaptersFactoryService>(ServiceProvider);

        var wpfTextView =
            VsUtils.GetWpfTextViewFromFilePath(sourceLocation.SourceFile, ServiceProvider,
                editorAdaptersFactoryService);
        textBuffer = wpfTextView?.TextBuffer;
        return textBuffer != null;
    }

    public SyntaxTree GetSyntaxTree(ITextBuffer textBuffer)
    {
        Document roslynDocument = textBuffer.GetRelatedDocuments().FirstOrDefault();
        if (roslynDocument != null && roslynDocument.TryGetSyntaxTree(out var syntaxTree))
            return syntaxTree;
        return null;
    }

    public void FireAndForget(Func<Task> action, Action<Exception> onException,
        [CallerMemberName] string callerName = "???")
    {
        action().FileAndForget($"vs/Reqnroll/{nameof(FireAndForget)}/{callerName}",
            "Error on a background task in Reqnroll",
            exception =>
            {
                Logger.LogException(MonitoringService, exception, $"Called from {callerName}");
                onException(exception);
                return true;
            });
    }

    public void FireAndForgetOnBackgroundThread(Func<CancellationToken, Task> action,
        [CallerMemberName] string callerName = "???")
    {
        Task.Factory.StartNew(async () =>
            {
                try
                {
                    ThreadHelper.ThrowIfOnUIThread(callerName);
                    await action(_backgroundTaskTokenSource.Token);
                }
                catch (Exception e)
                {
                    Logger.LogException(MonitoringService, e, $"Called from {callerName}");
                }
            },
            _backgroundTaskTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    public async Task RunOnUiThread(Action action)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        action();
    }

    public void OpenIfNotOpened(string path)
    {
        VsUtils.OpenIfNotOpened(path, ServiceProvider);
    }

    public IProjectScope GetProject(ITextBuffer textBuffer)
    {
        if (textBuffer == null) throw new ArgumentNullException(nameof(textBuffer));
        var project = VsUtils.GetProject(
            VsUtils.GetProjectItemFromTextBuffer(textBuffer));
        return GetProjectScope(project);
    }

    public IProjectScope[] GetProjectsWithFeatureFiles()
    {
        try
        {
            return VsUtils.GetAllProjects(Dte)
                .Where(HasFeatureFiles)
                .Select(GetProjectScope)
                .Where(ps => ps != null)
                .ToArray();
        }
        catch (Exception e)
        {
            Logger.LogVerboseException(MonitoringService, e);
            return _projectScopes.Values.ToArray();
        }
    }


    public void Dispose()
    {
        _backgroundTaskTokenSource.Cancel();

        _updateSolutionEventsListener.BuildCompleted -= UpdateSolutionEventsListenerOnBuildCompleted;
        _updateSolutionEventsListener.Dispose();

        _solutionEventListener.Closed -= SolutionEventListenerOnClosed;
        (_solutionEventListener as IDisposable)?.Dispose();
        _documentEventsListener?.Dispose();
    }

    public IProjectScope GetProjectScope(Project project)
    {
        if (project == null ||
            !VsUtils.IsSolutionProject(project))
            return new VoidProjectScope(this);

        var projectId = GetProjectId(project);
        var projectScope = _projectScopes.GetOrAdd(projectId, id => CreateProjectScope(id, project));
        return projectScope;
    }

    [CanBeNull] public event EventHandler<EventArgs> ProjectsBuilt = null!;

    [CanBeNull] public event EventHandler<EventArgs> ProjectOutputsUpdated = null!;

    private void OnActivityStarted()
    {
        if (_activityStarted)
            return;

        _activityStarted = true;
        Logger.LogInfo("Starting Visual Studio Extension...");
        MonitoringService.MonitorOpenProjectSystem(this);
    }

    private IPersistentSpan CreatePersistentTrackingPosition(SourceLocation sourceLocation, IWpfTextView wpfTextView)
    {
        var line0 = sourceLocation.SourceFileLine - 1;
        var lineOffset = sourceLocation.SourceFileColumn - 1;
        var endLine0 = sourceLocation.SourceFileEndLine - 1 ?? line0;
        var endLineOffset = sourceLocation.SourceFileEndColumn - 1 ?? lineOffset;
        try
        {
            if (wpfTextView != null)
                return _persistentSpanFactory.Create(wpfTextView.TextSnapshot, line0, lineOffset, endLine0,
                    endLineOffset,
                    SpanTrackingMode.EdgeExclusive);

            return _persistentSpanFactory.Create(sourceLocation.SourceFile, line0, lineOffset, endLine0, endLineOffset,
                SpanTrackingMode.EdgeExclusive);
        }
        catch (Exception ex)
        {
            Logger.LogException(MonitoringService, ex);
            return null;
        }
    }

    private void SolutionEventListenerOnLoaded(object sender, EventArgs e)
    {
        Logger.LogVerbose("Solution loaded");
        IsSolutionLoaded = true;
    }

    private void SolutionEventListenerOnBeforeCloseProject(object sender, CloseProjectEventArgs e)
    {
        try
        {
            var projectPath = VsUtils.SafeGetProjectFilePath(e.Hierarchy);
            if (projectPath != null && _projectScopes.TryRemove(projectPath, out var projectScope))
            {
                Logger.LogVerbose($"Closing project '{projectScope.ProjectName}'");
                projectScope.Dispose();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(MonitoringService, ex);
        }
    }

    private void SolutionEventListenerOnClosed(object sender, EventArgs e)
    {
        Logger.LogVerbose("Solution closed");
        foreach (var projectScope in _projectScopes.Values) projectScope.Dispose();
        _projectScopes.Clear();
        IsSolutionLoaded = false;
    }

    private void UpdateSolutionEventsListenerOnBuildCompleted(object sender, TestContainersChangedEventArgs e)
    {
        ProjectsBuilt?.Invoke(this, EventArgs.Empty);
        ProjectOutputsUpdated?.Invoke(this, EventArgs.Empty);
    }

    private string GetProjectId(Project project) => project.FullName;

    private VsProjectScope CreateProjectScope(string id, Project project)
    {
        OnActivityStarted();
        Logger.LogInfo($"Initializing project: {project.Name}");
        var projectScope = new VsProjectScope(id, project, this);
        projectScope.InitializeServices();
        return projectScope;
    }

    private bool HasFeatureFiles(Project project)
    {
        try
        {
            if (!VsUtils.IsSolutionProject(project))
                return false;
            return VsUtils.GetPhysicalFileProjectItems(project)
                .Any(pi => FileSystemHelper.IsOfType(VsUtils.GetFilePath(pi), ".feature"));
        }
        catch (Exception e)
        {
            Logger.LogDebugException(e);
            return false;
        }
    }

    private class DeveroomUndoContext : IDisposable
    {
        private readonly DTE _dte;

        public DeveroomUndoContext(DTE dte, string undoLabel)
        {
            _dte = dte;
            _dte.UndoContext.Open(undoLabel);
        }

        public void Dispose()
        {
            _dte.UndoContext.Close();
        }
    }
}
