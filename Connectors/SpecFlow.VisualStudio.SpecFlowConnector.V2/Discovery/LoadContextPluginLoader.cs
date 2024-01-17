namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public abstract class LoadContextPluginLoader : RuntimePluginLoader_Patch
{
    private readonly AssemblyLoadContext _loadContext;

    public LoadContextPluginLoader(AssemblyLoadContext loadContext)
    {
        _loadContext = loadContext;
    }

    protected override Assembly LoadAssembly(string pluginAssemblyName) =>
        _loadContext.LoadFromAssemblyPath(pluginAssemblyName);
}
