namespace Reqnroll.VisualStudio.VsxStubs.StepDefinitions;

public class MockableDiscoveryService : DiscoveryService
{
    public MockableDiscoveryService(IProjectScope projectScope,
        Mock<IDiscoveryResultProvider> discoveryResultProviderMock)
        : base(projectScope, discoveryResultProviderMock.Object, new ProjectBindingRegistryCache(projectScope.IdeScope))
    {
    }

    public DiscoveryResult LastDiscoveryResult { get; set; } = new()
    {
        StepDefinitions = Array.Empty<StepDefinition>(),
        Hooks = Array.Empty<Hook>()
    };

    public static MockableDiscoveryService Setup(IProjectScope projectScope, TimeSpan discoveryDelay) =>
        SetupWithInitialStepDefinitions(projectScope, Array.Empty<StepDefinition>(), discoveryDelay);

    public static MockableDiscoveryService SetupWithInitialStepDefinitions(IProjectScope projectScope,
        StepDefinition[] stepDefinitions, TimeSpan discoveryDelay)
    {
        var discoveryResultProviderMock = new Mock<IDiscoveryResultProvider>(MockBehavior.Strict);

        var discoveryService = new MockableDiscoveryService(projectScope, discoveryResultProviderMock)
        {
            LastDiscoveryResult = new DiscoveryResult
            {
                StepDefinitions = stepDefinitions,
                Hooks = Array.Empty<Hook>()
            }
        };

        InMemoryStubProjectBuilder.CreateOutputAssembly(projectScope);

        discoveryResultProviderMock
            .Setup(ds => ds.RunDiscovery(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ProjectSettings>()))
            .Returns(() =>
            {
                Thread.Sleep(discoveryDelay); //make it a bit more realistic
                return discoveryService.LastDiscoveryResult;
            });
#pragma warning disable VSTHRD002
        discoveryService.BindingRegistryCache.Update(
                unknown => discoveryService.DiscoveryInvoker.InvokeDiscoveryWithTimer())
            .Wait();
#pragma warning restore

        projectScope.Properties.AddProperty(typeof(IDiscoveryService), discoveryService);
        return discoveryService;
    }
}
