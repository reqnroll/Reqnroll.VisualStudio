using Gherkin.CucumberMessages.Types;
using NSubstitute;
using Newtonsoft.Json;

namespace Reqnroll.VisualStudio.VsxStubs.StepDefinitions;

public class MockableDiscoveryService : DiscoveryService
{
    public MockableDiscoveryService(IProjectScope projectScope,
        IDiscoveryResultProvider discoveryResultProviderMock)
        : base(projectScope, discoveryResultProviderMock, new ProjectBindingRegistryCache(projectScope.IdeScope))
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
        var discoveryResultProviderMock = Substitute.For<IDiscoveryResultProvider>();

        var allStepDefinitions = new List<StepDefinition>();
        foreach (var stepDefinition in stepDefinitions)
        {
            if (stepDefinition.Type.Equals("StepDefinition"))
            {
                var serialized = JsonConvert.SerializeObject(stepDefinition);
                var stepDefAttributes = new List<string>() { "Given", "Then", "When" };
                foreach (string stepDefAttribute in stepDefAttributes)
                {
                    StepDefinition? clonedStepDef = JsonConvert.DeserializeObject<StepDefinition>(serialized);
                    if (clonedStepDef != null)
                    {
                        clonedStepDef.Type = stepDefAttribute;
                        allStepDefinitions.Add(clonedStepDef);
                    }
                }
            }
            else
            {
                allStepDefinitions.Add(stepDefinition);
            }
        }

        var discoveryService = new MockableDiscoveryService(projectScope, discoveryResultProviderMock)
        {
            LastDiscoveryResult = new DiscoveryResult
            {
                StepDefinitions = allStepDefinitions.ToArray(),
                Hooks = Array.Empty<Hook>()
            }
        };

        InMemoryStubProjectBuilder.CreateOutputAssembly(projectScope);

        discoveryResultProviderMock
            .RunDiscovery(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ProjectSettings>())
            .Returns(_ =>
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
