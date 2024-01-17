using TechTalk.SpecFlow.Plugins;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlowDependencyProviderVLatest : NoInvokeDependencyProvider
{
    protected readonly AssemblyLoadContext LoadContext;

    public SpecFlowDependencyProviderVLatest(AssemblyLoadContext loadContext)
    {
        LoadContext = loadContext;
    }

    public override void RegisterGlobalContainerDefaults(ObjectContainer globalContainer)
    {
        base.RegisterGlobalContainerDefaults(globalContainer);
        globalContainer.RegisterInstanceAs(LoadContext);
        RegisterRuntimePluginLoader(globalContainer);
    }

    protected virtual void RegisterRuntimePluginLoader(ObjectContainer globalContainer)
    {
        globalContainer.RegisterTypeAs<LoadContextPluginLoaderVLatest, IRuntimePluginLoader>();
    }
}
