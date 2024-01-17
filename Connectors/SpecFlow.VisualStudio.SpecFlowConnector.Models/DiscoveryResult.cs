#nullable disable
namespace SpecFlow.VisualStudio.SpecFlowConnector.Models;

public class DiscoveryResult : ConnectorResult
{
    public StepDefinition[] StepDefinitions { get; set; }
    public Dictionary<string, string> SourceFiles { get; set; }
    public Dictionary<string, string> TypeNames { get; set; }
}
