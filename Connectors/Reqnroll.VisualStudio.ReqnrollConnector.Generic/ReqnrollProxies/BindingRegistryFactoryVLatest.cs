using Reqnroll;
using Reqnroll.Bindings;
using Reqnroll.Configuration;
using Reqnroll.Infrastructure;

namespace ReqnrollConnector.ReqnrollProxies;

public class BindingRegistryFactoryVLatest : BindingRegistryFactory
{
    public BindingRegistryFactoryVLatest(ILogger log) : base(log)
    {
    }

    protected override object CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext) =>
        new ReqnrollDependencyProviderVLatest(assemblyLoadContext);

    protected override object CreateConfigurationLoader(Option<FileDetails> configFile) =>
        new ReqnrollConfigurationLoader(configFile);

    protected override IRuntimeConfigurationProvider
        CreateConfigurationProvider(object configurationLoader) =>
        CreateConfigurationProvider((IConfigurationLoader) configurationLoader);

    protected virtual IRuntimeConfigurationProvider
        CreateConfigurationProvider(IConfigurationLoader configurationLoader) =>
        new DefaultRuntimeConfigurationProvider(configurationLoader);

    protected override object CreateContainerBuilder(object dependencyProvider) =>
        CreateContainerBuilder((IDefaultDependencyProvider) dependencyProvider);

    protected virtual object CreateContainerBuilder(IDefaultDependencyProvider dependencyProvider) =>
        new ContainerBuilderThatResetsTraceListener(dependencyProvider);

    protected override object CreateObjectContainer(Assembly testAssembly, object containerBuilder,
        IRuntimeConfigurationProvider configurationProvider, object dependencyProvider) =>
        CreateObjectContainer(testAssembly, (ContainerBuilder) containerBuilder, configurationProvider,
            (IDefaultDependencyProvider) dependencyProvider);

    protected virtual object CreateObjectContainer(Assembly testAssembly, ContainerBuilder containerBuilder,
        IRuntimeConfigurationProvider configurationProvider, IDefaultDependencyProvider dependencyProvider) =>
        containerBuilder.CreateGlobalContainer(testAssembly, configurationProvider);

    protected override object PrepareTestRunnerCreation(object globalContainer, AssemblyLoadContext assemblyLoadContext) 
        => PrepareTestRunnerCreation((IObjectContainer)globalContainer, assemblyLoadContext);

    protected virtual object PrepareTestRunnerCreation(IObjectContainer globalContainer,
        AssemblyLoadContext assemblyLoadContext)
    {
        globalContainer.RegisterTypeAs<BindingAssemblyContextLoader, IBindingAssemblyLoader>();

        return globalContainer;
    }

    protected override object CreateTestRunner(object globalContainer, Assembly testAssembly)
        => CreateTestRunner((IObjectContainer) globalContainer, testAssembly);

    protected virtual ITestRunner CreateTestRunner(IObjectContainer globalContainer, Assembly testAssembly)
    {
        var testRunnerManager = (TestRunnerManager) globalContainer.Resolve<ITestRunnerManager>();

        testRunnerManager.Initialize(testAssembly);
        return InvokeCreateTestRunner(testRunnerManager);
    }

    protected virtual ITestRunner InvokeCreateTestRunner(TestRunnerManager testRunnerManager)
    {
        return testRunnerManager.ReflectionCallMethod<ITestRunner>(nameof(TestRunnerManager.CreateTestRunner), "");
    }

    protected override object ResolveBindingRegistry(Assembly testAssembly, object globalContainer, object testRunner)
        => ResolveBindingRegistry(testAssembly, (IObjectContainer) globalContainer);

    protected virtual IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, IObjectContainer globalContainer)
        => globalContainer.Resolve<IBindingRegistry>();

    protected override IBindingRegistryAdapter AdaptBindingRegistry(object bindingRegistry)
        => AdaptBindingRegistry((IBindingRegistry) bindingRegistry);

    protected virtual IBindingRegistryAdapter AdaptBindingRegistry(IBindingRegistry bindingRegistry)
        => new BindingRegistryAdapterVLatest(bindingRegistry);
}
