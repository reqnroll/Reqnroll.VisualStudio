using Reqnroll.Plugins;

namespace ReqnrollConnector.ReqnrollProxies;

public class ReqnrollDependencyProviderVLatest : NoInvokeDependencyProvider
{
    protected readonly AssemblyLoadContext LoadContext;

    public ReqnrollDependencyProviderVLatest(AssemblyLoadContext loadContext)
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
