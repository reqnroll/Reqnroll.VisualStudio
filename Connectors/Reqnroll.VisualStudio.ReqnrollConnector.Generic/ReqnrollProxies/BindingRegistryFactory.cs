using Reqnroll.Configuration;

namespace ReqnrollConnector.ReqnrollProxies;

public abstract class BindingRegistryFactory : IBindingRegistryFactory
{
    protected ILogger Log;

    protected BindingRegistryFactory(ILogger log)
    {
        Log = log;
    }

    public IBindingRegistryAdapter GetBindingRegistry(AssemblyLoadContext assemblyLoadContext,
        Assembly testAssembly, Option<FileDetails> configFile) =>
        CreateDependencyProvider(assemblyLoadContext)
            .Map(dependencyProvider => CreateObjectContainer(
                    testAssembly,
                    CreateContainerBuilder(dependencyProvider),
                    configFile
                        .Map<Option<FileDetails>, object>(CreateConfigurationLoader)
                        .Map(CreateConfigurationProvider),
                    dependencyProvider)
                .Map(container=>PrepareTestRunnerCreation(container, assemblyLoadContext))
                .Map(container =>
                    CreateTestRunner(container, testAssembly)
                        .Map(testRunner => ResolveBindingRegistry(testAssembly, container, testRunner))
                )
                .Map(AdaptBindingRegistry)
            );

    protected abstract object CreateObjectContainer(Assembly testAssembly, object containerBuilder,
        IRuntimeConfigurationProvider configurationProvider, object dependencyProvider);

    protected abstract object CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext);

    protected abstract object CreateContainerBuilder(object dependencyProvider);

    protected abstract object CreateConfigurationLoader(Option<FileDetails> configFile);

    protected abstract IRuntimeConfigurationProvider CreateConfigurationProvider(
        object configurationLoader);

    protected abstract object PrepareTestRunnerCreation(object globalContainer, AssemblyLoadContext assemblyLoadContext);

    protected abstract object CreateTestRunner(object globalContainer, Assembly testAssembly);
    protected abstract object ResolveBindingRegistry(Assembly testAssembly, object globalContainer, object testRunner);

    protected abstract IBindingRegistryAdapter AdaptBindingRegistry(object bindingRegistry);
}
