namespace Reqnroll.VisualStudio.Analytics;

[Export(typeof(IAnalyticsTransmitterSink))]
public class AppInsightsAnalyticsTransmitterSink : IAnalyticsTransmitterSink
{
    private readonly IEnableAnalyticsChecker _enableAnalyticsChecker;
    private readonly IDeveroomLogger _logger;
    private readonly Lazy<TelemetryClient> _telemetryClient;

    [ImportingConstructor]
    public AppInsightsAnalyticsTransmitterSink(
        IEnableAnalyticsChecker enableAnalyticsChecker,
        IUserUniqueIdStore userUniqueIdStore,
        IDeveroomLogger logger)
    {
        _enableAnalyticsChecker = enableAnalyticsChecker;
        _logger = logger;
        _telemetryClient = new Lazy<TelemetryClient>(() => GetTelemetryClient(userUniqueIdStore.GetUserId()));
    }

    public void TransmitEvent(IAnalyticsEvent analyticsEvent)
    {
        if (!_enableAnalyticsChecker.IsEnabled())
            return;

        var appInsightsEvent = new EventTelemetry(analyticsEvent.EventName)
        {
            Timestamp = DateTime.UtcNow
        };

        AddProps(appInsightsEvent, analyticsEvent.Properties);

        TrackTelemetry(appInsightsEvent);
    }

    public void TransmitException(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps)
    {
        if (!_enableAnalyticsChecker.IsEnabled())
            return;

        var exceptionTelemetry = new ExceptionTelemetry(exception)
        {
            Timestamp = DateTime.UtcNow
        };

        AddProps(exceptionTelemetry, additionalProps);

        TrackTelemetry(exceptionTelemetry);
    }

    private void AddProps(ISupportProperties telemetry, IEnumerable<KeyValuePair<string, object>> additionalProps)
    {
        foreach (var prop in additionalProps) telemetry.Properties.Add(prop.Key, prop.Value.ToString());
    }

    private void TrackTelemetry(ITelemetry telemetry)
    {
        switch (telemetry)
        {
            case EventTelemetry eventTelemetry:
                _telemetryClient.Value.TrackEvent(eventTelemetry);
                break;
            case ExceptionTelemetry exceptionTelemetry:
                _telemetryClient.Value.TrackException(exceptionTelemetry);
                break;
        }

        Task.Run(async () =>
        {
            try
            {
                await _telemetryClient.Value.FlushAndTransmitAsync(
                    new DebuggableCancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }
        });
    }

    private TelemetryClient GetTelemetryClient(string userUniqueId) =>
        new()
        {
            Context =
            {
                User =
                {
                    Id = userUniqueId,
                    AccountId = userUniqueId
                }
            }
        };
}
