#nullable disable
namespace SpecFlow.VisualStudio.SpecFlowConnector.Models;

public abstract class ConnectorResult
{
    public string SpecFlowVersion { get; set; }
    public string ErrorMessage { get; set; }
    public bool IsFailed => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string[] Warnings { get; set; }
    public Dictionary<string, object> AnalyticsProperties { get; set; }
}
