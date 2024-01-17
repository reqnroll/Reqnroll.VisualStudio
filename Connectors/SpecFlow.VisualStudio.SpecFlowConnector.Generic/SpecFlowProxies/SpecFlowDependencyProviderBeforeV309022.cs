using TechTalk.SpecFlow.Plugins;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlowDependencyProviderBeforeV309022 : SpecFlowDependencyProviderVLatest
{
    public SpecFlowDependencyProviderBeforeV309022(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override void RegisterRuntimePluginLoader(ObjectContainer globalContainer)
    {
        var pluginLoaderType = new DynamicRuntimePluginLoaderFactory().Create();
        globalContainer.ReflectionRegisterTypeAs(pluginLoaderType, typeof(IRuntimePluginLoader));
    }
}
