using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlowConnector.SpecFlowProxies;

public interface IRuntimePluginLoaderBeforeV309022 : IRuntimePluginLoader
{
    IRuntimePlugin LoadPlugin(string pluginAssemblyName, ITraceListener traceListener);
}
