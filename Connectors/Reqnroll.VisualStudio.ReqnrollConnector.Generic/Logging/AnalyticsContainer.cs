using System.Diagnostics;

namespace ReqnrollConnector.Logging;

[DebuggerDisplay("{_analyticsProperties}")]
public class AnalyticsContainer : IAnalyticsContainer
{
    private readonly Dictionary<string, string> _analyticsProperties = new();

    public void AddAnalyticsProperty(string key, string value)
    {
        _analyticsProperties.Add(key, value);
    }

    public Dictionary<string, object> ToDictionary() => _analyticsProperties.ToDictionary(e => e.Key, e => (object)e.Value);
}
