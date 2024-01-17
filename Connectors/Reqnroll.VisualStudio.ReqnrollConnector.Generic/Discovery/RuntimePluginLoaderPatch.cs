#nullable disable
using Reqnroll.Tracing;

// ReSharper disable once CheckNamespace
namespace Reqnroll.Plugins;

public abstract class RuntimePluginLoaderPatch : IRuntimePluginLoader
{
    #region Copy-Pasted code from Reqnroll

    public IRuntimePlugin LoadPlugin(string pluginAssemblyName, ITraceListener traceListener, bool _)
    {
        Assembly assembly;
        try
        {

            #endregion

            #region Patch

            assembly = LoadAssembly(pluginAssemblyName);

            #endregion

            #region Copy-Pasted code from Reqnroll

        }
        catch (Exception ex)
        {
            throw new ReqnrollException(
                string.Format(
                    "Unable to load plugin: {0}. Please check http://go.reqnroll.net/doc-plugins for details.",
                    pluginAssemblyName), ex);
        }

        var pluginAttribute =
            (RuntimePluginAttribute) Attribute.GetCustomAttribute(assembly, typeof(RuntimePluginAttribute));
        if (pluginAttribute == null)
        {
            traceListener.WriteToolOutput(string.Format(
                "Missing [assembly:RuntimePlugin] attribute in {0}. Please check http://go.reqnroll.net/doc-plugins for details.",
                assembly.FullName));
            return null;
        }

        if (!typeof(IRuntimePlugin).IsAssignableFrom(pluginAttribute.PluginType))
            throw new ReqnrollException(string.Format(
                "Invalid plugin attribute in {0}. Plugin type must implement IRuntimePlugin. Please check http://go.reqnroll.net/doc-plugins for details.",
                assembly.FullName));

        IRuntimePlugin plugin;
        try
        {
            plugin = (IRuntimePlugin) Activator.CreateInstance(pluginAttribute.PluginType);
        }
        catch (Exception ex)
        {
            throw new ReqnrollException(
                string.Format(
                    "Invalid plugin in {0}. Plugin must have a default constructor that does not throw exception. Please check http://go.reqnroll.net/doc-plugins for details.",
                    assembly.FullName), ex);
        }

        return plugin;
    }

    #endregion

    protected abstract Assembly LoadAssembly(string pluginAssemblyName);
}
