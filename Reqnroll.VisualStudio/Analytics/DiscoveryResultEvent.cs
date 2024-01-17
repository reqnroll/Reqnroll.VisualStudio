namespace Reqnroll.VisualStudio.Analytics;

public record DiscoveryResultEvent : GenericEvent
{
    public DiscoveryResultEvent(ConnectorResult discoveryResult)
        : base("DiscoveryResult",
            discoveryResult.AnalyticsProperties)
    {
    }
}
