using System;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

public class ReqnrollV3BaseDiscoverer : BaseDiscoverer
{
    protected readonly AssemblyLoadContext _loadContext;

    public ReqnrollV3BaseDiscoverer(AssemblyLoadContext loadContext)
    {
        _loadContext = loadContext;
    }

    protected override IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath)
    {
        IConfigurationLoader configurationLoader = new SpecFlow21ConfigurationLoader(configFilePath);
        var globalContainer = CreateGlobalContainer(configurationLoader, testAssembly);
        var testRunnerManager = (TestRunnerManager) globalContainer.Resolve<ITestRunnerManager>();
        testRunnerManager.Initialize(testAssembly);
        EnsureTestRunnerCreated(testRunnerManager);

        return globalContainer.Resolve<IBindingRegistry>();
    }

    protected virtual void EnsureTestRunnerCreated(TestRunnerManager testRunnerManager)
    {
        testRunnerManager.CreateTestRunner(0);
    }

    protected virtual IObjectContainer CreateGlobalContainer(IConfigurationLoader configurationLoader,
        Assembly testAssembly)
    {
        var defaultDependencyProvider = CreateDefaultDependencyProvider();
        var globalContainer =
            CreateContainerBuilder(defaultDependencyProvider).CreateGlobalContainer(
                testAssembly,
                new DefaultRuntimeConfigurationProvider(configurationLoader));
        return globalContainer;
    }

    protected virtual ContainerBuilder CreateContainerBuilder(DefaultDependencyProvider defaultDependencyProvider) =>
        new(defaultDependencyProvider);

    protected virtual DefaultDependencyProvider CreateDefaultDependencyProvider() => new NoInvokeDependencyProvider();

    protected override IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry) =>
        bindingRegistry.GetStepDefinitions();
}
