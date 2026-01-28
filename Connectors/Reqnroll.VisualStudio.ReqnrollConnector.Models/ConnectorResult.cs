#nullable disable
namespace Reqnroll.VisualStudio.ReqnrollConnector.Models;

public abstract class ConnectorResult
{
    public string ConnectorType { get; set; }
    public string ReqnrollVersion { get; set; }
    public string ErrorMessage { get; set; }
    public bool IsFailed => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string[] LogMessages { get; set; }
    public string[] Warnings { get; set; }
    public Dictionary<string, object> AnalyticsProperties { get; set; }
}
