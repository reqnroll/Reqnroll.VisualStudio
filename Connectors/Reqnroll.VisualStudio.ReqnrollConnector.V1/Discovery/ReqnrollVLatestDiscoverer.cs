namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

public class ReqnrollVLatestDiscoverer : RemotingBaseDiscoverer
{
    protected override object CreateGlobalContainer(Assembly testAssembly, string configFilePath)
    {
        IRuntimeConfigurationProvider configurationProvider = CreateConfigurationProvider(configFilePath);
        return CreateGlobalContainer(testAssembly, configurationProvider);
    }

    private object CreateGlobalContainer(Assembly testAssembly, IRuntimeConfigurationProvider configurationProvider)
    {
        var containerBuilder = new ContainerBuilder(new NoInvokeDependencyProvider());
        return CreateGlobalContainer(testAssembly, configurationProvider, containerBuilder);
    }

    protected virtual object CreateGlobalContainer(Assembly testAssembly,
        IRuntimeConfigurationProvider configurationProvider, IContainerBuilder containerBuilder)
    {
        var globalContainer = containerBuilder.CreateGlobalContainer(testAssembly, configurationProvider);
        return globalContainer;
    }

    protected virtual IRuntimeConfigurationProvider CreateConfigurationProvider(string configFilePath)
    {
        IConfigurationLoader configurationLoader = new SpecFlow21ConfigurationLoader(configFilePath);
        return new DefaultRuntimeConfigurationProvider(configurationLoader);
    }

    protected override void RegisterTypeAs<TType, TInterface>(object globalContainer)
    {
        var objectContainer = (IObjectContainer) globalContainer;
        objectContainer.RegisterTypeAs<TType, TInterface>();
    }

    protected override T Resolve<T>(object globalContainer)
    {
        var objectContainer = (IObjectContainer) globalContainer;
        return objectContainer.Resolve<T>();
    }

    protected override IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, object globalContainer)
    {
        var testRunnerManager = (TestRunnerManager) Resolve<ITestRunnerManager>(globalContainer);

        testRunnerManager.Initialize(testAssembly);
        testRunnerManager.CreateTestRunner(0);

        return Resolve<IBindingRegistry>(globalContainer);
    }
}
