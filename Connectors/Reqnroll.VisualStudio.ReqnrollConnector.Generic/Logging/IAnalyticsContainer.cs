namespace ReqnrollConnector.Logging;

public interface IAnalyticsContainer
{
    void AddAnalyticsProperty(string key, string value);
    ImmutableSortedDictionary<string, string> ToImmutable();
}
