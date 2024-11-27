namespace Reqnroll.VisualStudio.Discovery;

public class DiscoveryService : IDiscoveryService
{
    private readonly IDeveroomLogger _logger;
    private readonly IProjectScope _projectScope;
    private readonly IProjectSettingsProvider _projectSettingsProvider;
    private protected readonly DiscoveryInvoker DiscoveryInvoker;

    public DiscoveryService(IProjectScope projectScope, IDiscoveryResultProvider discoveryResultProvider,
        IProjectBindingRegistryCache bindingRegistryCacheCache)
    {
        _projectScope = projectScope;
        _logger = _projectScope.IdeScope.Logger;
        _projectSettingsProvider = _projectScope.GetProjectSettingsProvider();
        _projectSettingsProvider.WeakSettingsInitialized += ProjectSystemOnProjectsBuilt;
        _projectScope.IdeScope.WeakProjectOutputsUpdated += ProjectSystemOnProjectsBuilt;
        BindingRegistryCache = bindingRegistryCacheCache;
        DiscoveryInvoker = new DiscoveryInvoker(_projectScope, discoveryResultProvider);
    }

    public IProjectBindingRegistryCache BindingRegistryCache { get; }

    public event EventHandler<EventArgs> WeakBindingRegistryChanged
    {
        add => WeakEventManager<IProjectBindingRegistryCache, EventArgs>.AddHandler(BindingRegistryCache,
            nameof(IProjectBindingRegistryCache.Changed), value);
        remove => WeakEventManager<IProjectBindingRegistryCache, EventArgs>.RemoveHandler(BindingRegistryCache,
            nameof(IProjectBindingRegistryCache.Changed),
            value);
    }

    public void Dispose()
    {
        _projectScope.IdeScope.WeakProjectsBuilt -= ProjectSystemOnProjectsBuilt;
        _projectSettingsProvider.SettingsInitialized -= ProjectSystemOnProjectsBuilt;
    }

    public void TriggerDiscovery([CallerMemberName] string callerMemberName = "?")
    {
        _logger.LogVerbose($"Discovery triggered from {callerMemberName}");

        _projectScope.IdeScope.FireAndForgetOnBackgroundThread(
            _ => BindingRegistryCache.Update(_ => DiscoveryInvoker.InvokeDiscoveryWithTimer()));
    }

    private void ProjectSystemOnProjectsBuilt(object sender, EventArgs eventArgs)
    {
        _logger.LogVerbose("Projects built or settings initialized");

        if (IsCacheUpToDate())
            return;

        TriggerDiscovery();
    }

    private bool IsCacheUpToDate()
    {
        var projectSettings = _projectScope.GetProjectSettings();
        var configSource = DiscoveryInvoker.GetTestAssemblySource(projectSettings);
        var currentHash = DiscoveryInvoker.CreateProjectHash(projectSettings, configSource);

        return BindingRegistryCache.Value.ProjectHash == currentHash;
    }
}
