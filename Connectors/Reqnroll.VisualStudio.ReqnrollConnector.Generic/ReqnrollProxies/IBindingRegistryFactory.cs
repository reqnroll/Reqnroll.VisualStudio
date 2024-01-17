namespace ReqnrollConnector.ReqnrollProxies;

public interface IBindingRegistryFactory
{
    IBindingRegistryAdapter GetBindingRegistry(
        AssemblyLoadContext assemblyLoadContext,
        Assembly testAssembly,
        Option<FileDetails> configFile);
}
