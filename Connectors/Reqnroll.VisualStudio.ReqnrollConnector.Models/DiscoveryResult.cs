#nullable disable
namespace Reqnroll.VisualStudio.ReqnrollConnector.Models;

public class DiscoveryResult : ConnectorResult
{
    public StepDefinition[] StepDefinitions { get; set; } = Array.Empty<StepDefinition>();
    public Hook[] Hooks { get; set; } = Array.Empty<Hook>();
    public Dictionary<string, string> SourceFiles { get; set; }
    public Dictionary<string, string> TypeNames { get; set; }
    public string[] GenericBindingErrors { get; set; }
}
