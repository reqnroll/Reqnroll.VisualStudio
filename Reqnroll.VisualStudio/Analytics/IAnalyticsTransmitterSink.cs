#nullable enable
namespace Reqnroll.VisualStudio.Analytics;

public interface IAnalyticsTransmitterSink
{
    void TransmitEvent(IAnalyticsEvent analyticsEvent);
    void TransmitException(Exception exception, IEnumerable<KeyValuePair<string, object>> eventName);
}
