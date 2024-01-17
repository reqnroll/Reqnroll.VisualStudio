using Reqnroll.Plugins;

namespace ReqnrollConnector.ReqnrollProxies;

public class LoadContextPluginLoaderVLatest : RuntimePluginLoaderPatch
{
    private readonly AssemblyLoadContext _loadContext;

    public LoadContextPluginLoaderVLatest(AssemblyLoadContext loadContext)
    {
        _loadContext = loadContext;
    }

    protected override Assembly LoadAssembly(string pluginAssemblyName) =>
        _loadContext.LoadFromAssemblyPath(pluginAssemblyName);
}
