using Reqnroll.Infrastructure;

namespace ReqnrollConnector.ReqnrollProxies;

public class BindingRegistryFactoryBeforeV309022 : BindingRegistryFactoryBeforeV310000
{
    public BindingRegistryFactoryBeforeV309022(ILogger log) : base(log)
    {
    }

    protected override object CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext) =>
        new ReqnrollDependencyProviderBeforeV309022(assemblyLoadContext);
}
