#nullable enable
namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class StubAnalyticsTransmitter : IAnalyticsTransmitter, IEnumerable<IAnalyticsEvent>
{
    private readonly IDeveroomLogger _logger;

    public StubAnalyticsTransmitter(IDeveroomLogger logger)
    {
        _logger = logger;
    }

    private ConcurrentBag<IAnalyticsEvent> Events { get; } = new();

    public void TransmitEvent(IAnalyticsEvent runtimeEvent)
    {
        Events.Add(runtimeEvent);
        _logger.LogVerbose(runtimeEvent.EventName);
    }

    public void TransmitFatalExceptionEvent(Exception exception, bool isFatal)
    {
        //nop
    }

    public void TransmitExceptionEvent(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps)
    {
        //nop
    }

    public IEnumerator<IAnalyticsEvent> GetEnumerator() => Events.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Events.GetEnumerator();
}
