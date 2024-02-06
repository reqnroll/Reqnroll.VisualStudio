#nullable enable
namespace Reqnroll.VisualStudio.VsxStubs;

public class StubDiscoveryResultProvider : IDiscoveryResultProvider
{
    public DiscoveryResult DiscoveryResult { get; set; } = new()
    {
        StepDefinitions = Array.Empty<StepDefinition>(),
        Hooks = Array.Empty<Hook>()
    };

    public DiscoveryResult
        RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings) =>
        DiscoveryResult;
}
