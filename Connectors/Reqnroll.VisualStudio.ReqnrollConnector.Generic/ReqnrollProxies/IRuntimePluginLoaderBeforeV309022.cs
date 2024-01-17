using Reqnroll.Plugins;
using Reqnroll.Tracing;

namespace ReqnrollConnector.ReqnrollProxies;

public interface IRuntimePluginLoaderBeforeV309022 : IRuntimePluginLoader
{
    IRuntimePlugin LoadPlugin(string pluginAssemblyName, ITraceListener traceListener);
}
