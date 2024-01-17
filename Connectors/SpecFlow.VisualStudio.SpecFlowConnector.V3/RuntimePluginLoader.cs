#nullable disable

namespace TechTalk.SpecFlow.Plugins;

public class RuntimePluginLoader_Patch : IRuntimePluginLoader
{
    public IRuntimePlugin LoadPlugin(string pluginAssemblyName, ITraceListener traceListener,
        bool traceMissingPluginAttribute) => LoadPlugin(pluginAssemblyName, traceListener);

    public IRuntimePlugin LoadPlugin(string pluginAssemblyName, ITraceListener traceListener)
    {
        //var assemblyName = string.Format(ASSEMBLY_NAME_PATTERN, pluginDescriptor.Name);
        Assembly assembly;
        try
        {
            assembly = LoadAssembly(pluginAssemblyName);
        }
        catch (Exception ex)
        {
            throw new SpecFlowException(
                string.Format(
                    "Unable to load plugin: {0}. Please check http://go.specflow.org/doc-plugins for details.",
                    pluginAssemblyName), ex);
        }

        var pluginAttribute =
            (RuntimePluginAttribute) Attribute.GetCustomAttribute(assembly, typeof(RuntimePluginAttribute));
        if (pluginAttribute == null)
        {
            traceListener.WriteToolOutput(string.Format(
                "Missing [assembly:RuntimePlugin] attribute in {0}. Please check http://go.specflow.org/doc-plugins for details.",
                assembly.FullName));
            return null;
        }

        if (!typeof(IRuntimePlugin).IsAssignableFrom(pluginAttribute.PluginType))
            throw new SpecFlowException(string.Format(
                "Invalid plugin attribute in {0}. Plugin type must implement IRuntimePlugin. Please check http://go.specflow.org/doc-plugins for details.",
                assembly.FullName));

        IRuntimePlugin plugin;
        try
        {
            plugin = (IRuntimePlugin) Activator.CreateInstance(pluginAttribute.PluginType);
        }
        catch (Exception ex)
        {
            throw new SpecFlowException(
                string.Format(
                    "Invalid plugin in {0}. Plugin must have a default constructor that does not throw exception. Please check http://go.specflow.org/doc-plugins for details.",
                    assembly.FullName), ex);
        }

        return plugin;
    }

    protected virtual Assembly LoadAssembly(string pluginAssemblyName) => Assembly.LoadFrom(pluginAssemblyName);
}
