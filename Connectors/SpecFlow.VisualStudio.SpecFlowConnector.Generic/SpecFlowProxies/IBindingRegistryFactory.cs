namespace SpecFlowConnector.SpecFlowProxies;

public interface IBindingRegistryFactory
{
    IBindingRegistryAdapter GetBindingRegistry(
        AssemblyLoadContext assemblyLoadContext,
        Assembly testAssembly,
        Option<FileDetails> configFile);
}
