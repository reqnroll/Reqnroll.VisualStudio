using Reqnroll.Plugins;

namespace ReqnrollConnector.ReqnrollProxies;

public class ReqnrollDependencyProviderBeforeV309022 : ReqnrollDependencyProviderVLatest
{
    public ReqnrollDependencyProviderBeforeV309022(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override void RegisterRuntimePluginLoader(ObjectContainer globalContainer)
    {
        var pluginLoaderType = new DynamicRuntimePluginLoaderFactory().Create();
        globalContainer.ReflectionRegisterTypeAs(pluginLoaderType, typeof(IRuntimePluginLoader));
    }
}
