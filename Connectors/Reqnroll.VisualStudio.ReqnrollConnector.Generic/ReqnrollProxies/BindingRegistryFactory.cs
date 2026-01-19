using Reqnroll.Bindings.Provider.Data;

namespace ReqnrollConnector.ReqnrollProxies;
public abstract class BindingRegistryFactory : IBindingRegistryFactory
{
    protected ILogger Log;

    protected BindingRegistryFactory(ILogger log)
    {
        Log = log;
    }

    public IBindingRegistryAdapter GetBindingRegistry(AssemblyLoadContext assemblyLoadContext, Assembly testAssembly, FileDetails? configFile)
    {
        var configFileContent = LoadConfigFileContent(configFile);

        var reqnrollAssembly = assemblyLoadContext.LoadFromAssemblyName(new AssemblyName("Reqnroll"));
        var bindingProviderServiceType = reqnrollAssembly.GetType("Reqnroll.Bindings.Provider.BindingProviderService", true)!;
        var bindingJson = bindingProviderServiceType.ReflectionCallStaticMethod<string>("DiscoverBindings", new[] { typeof(Assembly), typeof(string) }, testAssembly, configFileContent);
        var bindingData = JsonSerialization.DeserializeObjectDefaultCase<BindingData>(bindingJson, Log);
       
        return new BindingRegistryAdapter(bindingData ?? new BindingData());
    }

    private string? LoadConfigFileContent(FileDetails? configFile)
    {
        if (configFile == null)
            return null;

        if (configFile.Extension.Equals(".config", StringComparison.InvariantCultureIgnoreCase))
            return LegacyAppConfigLoader.LoadConfiguration(configFile);

        var content = File.ReadAllText(configFile.FullName);
        return content;
    }
}
