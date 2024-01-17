using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Analytics;

public interface IEnableAnalyticsChecker
{
    bool IsEnabled();
}

[Export(typeof(IEnableAnalyticsChecker))]
public class EnableAnalyticsChecker : IEnableAnalyticsChecker
{
    public const string ReqnrollTelemetryEnvironmentVariable = "REQNROLL_TELEMETRY_ENABLED";

    public bool IsEnabled()
    {
        var reqnrollTelemetry = Environment.GetEnvironmentVariable(ReqnrollTelemetryEnvironmentVariable);
        return reqnrollTelemetry == null || reqnrollTelemetry.Equals("1");
    }
}
