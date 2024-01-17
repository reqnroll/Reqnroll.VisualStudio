#nullable disable

namespace Reqnroll.VisualStudio.VsEvents;

public class UpdateSolutionEventsListener : IVsUpdateSolutionEvents, IDisposable
{
    private readonly IVsSolutionBuildManager2 _buildManager;
    private readonly bool _fireOnNonUserInitiatedBuild;
    private readonly IDeveroomLogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private BuildCommandIntercepter _buildCommandIntercepter;
    private EventWaitHandle _buildCompleteEvent;
    private uint _buildCookie;
    private TestContainersChangedEventArgs _buildResult;
    private bool _eventsEnabled = true;
    private bool _isBuildClean;
    private uint _solutionBuildingCookie;
    private bool _userStartedBuildAction;

    public UpdateSolutionEventsListener(IServiceProvider serviceProvider, IDeveroomLogger logger,
        bool fireOnNonUserInitiatedBuild)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fireOnNonUserInitiatedBuild = fireOnNonUserInitiatedBuild;
        _buildCompleteEvent = new ManualResetEvent(false);
        _userStartedBuildAction = false;
        _isBuildClean = false;
        _buildManager = _serviceProvider.GetService<IVsSolutionBuildManager2>(typeof(SVsSolutionBuildManager));
        var vsMonitorSelection = _serviceProvider.GetService<IVsMonitorSelection>(typeof(SVsShellMonitorSelection));
        _solutionBuildingCookie = vsMonitorSelection.GetCmdUIContextCookie(VSConstants.UICONTEXT_SolutionBuilding);
        AdviseUpdateSolutionEvents();
        AdviseBuildEvents();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => 0;

    // ReSharper disable once RedundantAssignment
    int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
    {
        pfCancelUpdate = 0;
        ErrorHandler.Succeeded(_serviceProvider.GetService<IVsUIShell>(typeof(SVsUIShell)).UpdateCommandUI(1));
        OnBuildBegin();
        return 0;
    }

    int IVsUpdateSolutionEvents.UpdateSolution_Cancel() => 0;

    int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
    {
        try
        {
            OnBuildComplete(new TestContainersChangedEventArgs(fSucceeded == 1, fCancelCommand == 1));
        }
        finally
        {
            _userStartedBuildAction = false;
            _isBuildClean = false;
        }

        return 0;
    }

    // ReSharper disable once RedundantAssignment
    int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
    {
        pfCancelUpdate = 0;
        return 0;
    }

    public event EventHandler<BuildCommandEventArgs> BuildStarted;

    public event EventHandler<TestContainersChangedEventArgs> BuildCompleted;

    private void AdviseBuildEvents()
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            UpdateSolutionEventsListener solutionEventsListener = this;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            solutionEventsListener._buildCommandIntercepter =
                BuildCommandIntercepter.InitializeCommandInterceptor(solutionEventsListener._serviceProvider);
            solutionEventsListener._buildCommandIntercepter.UserInitiatedBuild +=
                solutionEventsListener.OnUserInitiatedBuild;
        });
    }

    private void UnadviseBuildEvents()
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            UpdateSolutionEventsListener solutionEventsListener = this;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (solutionEventsListener._buildCommandIntercepter == null)
                return;
            solutionEventsListener._buildCommandIntercepter.UserInitiatedBuild -=
                solutionEventsListener.OnUserInitiatedBuild;
            solutionEventsListener._buildCommandIntercepter.Dispose();
            solutionEventsListener._buildCommandIntercepter = null;
        });
    }

    private void AdviseUpdateSolutionEvents()
    {
        if (_buildManager == null)
            return;
        ErrorHandler.ThrowOnFailure(_buildManager.AdviseUpdateSolutionEvents(this, out _buildCookie));
    }

    private void OnUserInitiatedBuild(object sender, BuildCommandEventArgs e)
    {
        _userStartedBuildAction = true;
        if (e == null || !e.IsBuildClean)
            return;
        _isBuildClean = true;
    }

    private void OnBuildComplete(TestContainersChangedEventArgs eventArgs)
    {
        _buildResult = eventArgs;
        _buildCompleteEvent.Set();
        if (!_eventsEnabled || BuildCompleted == null || !_fireOnNonUserInitiatedBuild && !_userStartedBuildAction)
        {
            _logger.LogVerbose("Build event filtered out");
            return;
        }

        _logger.LogVerbose("Firing BuildCompleted event...");
        BuildCompleted(this, eventArgs);
    }

    private int WaitForBuildComplete(CancellationToken cancellationToken)
    {
        return WaitHandle.WaitAny(new[]
        {
            _buildCompleteEvent,
            cancellationToken.WaitHandle
        });
    }

    public async Task<TestContainersChangedEventArgs> AwaitForBuildCompleteAsync(CancellationToken cancellationToken)
    {
        bool testContainerUpdateCanceled = (uint) await Task.Run(() => WaitForBuildComplete(cancellationToken)) > 0U;
        _buildResult = _buildResult == null
            ? new TestContainersChangedEventArgs(false, testContainerUpdateCanceled)
            : new TestContainersChangedEventArgs(_buildResult.Succeeded,
                _buildResult.Canceled | testContainerUpdateCanceled);
        return _buildResult;
    }

    public IDisposable DisableNotifications() => new NotificationDisabler(this);

    private void UnadviseUpdateSolutionEvents()
    {
        if (_buildCookie == 0U || _buildManager == null)
            return;
        ErrorHandler.Succeeded(_buildManager.UnadviseUpdateSolutionEvents(_buildCookie));
        _buildCookie = 0U;
    }

    private void OnBuildBegin()
    {
        _buildCompleteEvent.Reset();
        if (!_eventsEnabled || BuildStarted == null)
            return;
        BuildStarted(this, new BuildCommandEventArgs(_isBuildClean));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;
        UnadviseUpdateSolutionEvents();
        UnadviseBuildEvents();
        if (_buildCompleteEvent == null)
            return;
        _buildCompleteEvent.Dispose();
        _buildCompleteEvent = null;
    }

    private sealed class NotificationDisabler : IDisposable
    {
        private readonly UpdateSolutionEventsListener _listener;

        public NotificationDisabler(UpdateSolutionEventsListener listener)
        {
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _listener._eventsEnabled = false;
        }

        void IDisposable.Dispose()
        {
            _listener._eventsEnabled = true;
        }
    }
}
