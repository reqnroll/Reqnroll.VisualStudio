#nullable enable
using System.Net.Http;

namespace SpecFlow.VisualStudio.Analytics;

[Export(typeof(IAnalyticsTransmitter))]
public class AnalyticsTransmitter : IAnalyticsTransmitter
{
    private readonly IAnalyticsTransmitterSink _analyticsTransmitterSink;
    private readonly IEnableAnalyticsChecker _enableAnalyticsChecker;
    private readonly IDeveroomLogger? _logger;

    [ImportingConstructor]
    public AnalyticsTransmitter(IAnalyticsTransmitterSink analyticsTransmitterSink,
        IEnableAnalyticsChecker enableAnalyticsChecker, DeveroomCompositeLogger? logger = null)
    {
        _analyticsTransmitterSink = analyticsTransmitterSink;
        _enableAnalyticsChecker = enableAnalyticsChecker;
        _logger = logger;
    }

    public void TransmitEvent(IAnalyticsEvent analyticsEvent)
    {
        try
        {
            DumpAnalyticsEvent(analyticsEvent);
            if (!_enableAnalyticsChecker.IsEnabled()) return;

            _analyticsTransmitterSink.TransmitEvent(analyticsEvent);
        }
        catch (Exception ex)
        {
            TransmitExceptionEvent(ex, ImmutableDictionary<string, object>.Empty);
        }
    }

    public void TransmitExceptionEvent(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps)
    {
        var isNormalError = IsNormalError(exception);
        if (isNormalError)
            TransmitException(exception, additionalProps);
        else
            TransmitFatalExceptionEvent(exception, true);
    }

    public void TransmitFatalExceptionEvent(Exception exception, bool isFatal)
    {
        var additionalProps = ImmutableDictionary.CreateBuilder<string, object>();
        if (isFatal)
            additionalProps.Add("IsFatal", isFatal.ToString());

        TransmitException(exception, additionalProps.ToImmutable());
    }

    private void TransmitException(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps)
    {
        try
        {
            var additionalPropsArray = additionalProps.ToArray();
            DumpAnalyticsException(exception, additionalPropsArray);
            _analyticsTransmitterSink.TransmitException(exception, additionalPropsArray);
        }
        catch (Exception ex)
        {
            // catch all exceptions since we do not want to break the whole extension simply because data transmission failed
            Debug.WriteLine(ex, "Error during transmitting analytics event.");
        }
    }

    [Conditional("ANALYTICS_DEBUG")]
    private void DumpAnalyticsEvent(IAnalyticsEvent analyticsEvent)
    {
        _logger?.LogVerbose(() => $"{analyticsEvent.EventName}: {string.Join(Environment.NewLine + "  ", analyticsEvent.Properties.Select(p => $"{p.Key}={p.Value}"))}");
    }

    [Conditional("ANALYTICS_DEBUG")]
    private void DumpAnalyticsException(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps)
    {
        _logger?.LogVerbose(() => $"{exception.Message}: {string.Join(Environment.NewLine + "  ", additionalProps.Select(p => $"{p.Key}={p.Value}"))}");
    }

    private static bool IsNormalError(Exception exception)
    {
        if (exception is AggregateException aggregateException)
            return aggregateException.InnerExceptions.All(IsNormalError);
        return
            exception is DeveroomConfigurationException ||
            exception is TimeoutException ||
            exception is TaskCanceledException ||
            exception is OperationCanceledException ||
            exception is HttpRequestException;
    }
}
