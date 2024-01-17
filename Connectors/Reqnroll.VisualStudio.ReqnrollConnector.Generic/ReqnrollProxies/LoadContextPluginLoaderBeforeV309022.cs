using Reqnroll.Plugins;

namespace ReqnrollConnector.ReqnrollProxies;

public abstract class LoadContextPluginLoaderBeforeV309022 : RuntimePluginLoaderPatch
{
    private readonly AssemblyLoadContext _loadContext;

    protected LoadContextPluginLoaderBeforeV309022(AssemblyLoadContext loadContext)
    {
        _loadContext = loadContext;
    }

    protected override Assembly LoadAssembly(string pluginAssemblyName) =>
        _loadContext.LoadFromAssemblyPath(pluginAssemblyName);
}
