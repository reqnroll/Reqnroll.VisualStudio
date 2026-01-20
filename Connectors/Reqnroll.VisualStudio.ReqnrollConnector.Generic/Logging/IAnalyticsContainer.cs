namespace ReqnrollConnector.Logging;

public interface IAnalyticsContainer
{
    void AddAnalyticsProperty(string key, string value);
    Dictionary<string, object> ToDictionary();
}
