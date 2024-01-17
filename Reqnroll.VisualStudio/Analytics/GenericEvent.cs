#nullable enable
namespace Reqnroll.VisualStudio.Analytics;

[Export(typeof(IAnalyticsEvent))]
public record GenericEvent : IAnalyticsEvent
{
    public GenericEvent(string eventName, IEnumerable<KeyValuePair<string, object>> properties)
    {
        EventName = eventName;
        Properties = properties.ToImmutableDictionary();
    }

    public GenericEvent(string eventName) : this(eventName, ImmutableDictionary<string, object>.Empty)
    {
    }

    public string EventName { get; }
    public ImmutableDictionary<string, object> Properties { get; }
}
