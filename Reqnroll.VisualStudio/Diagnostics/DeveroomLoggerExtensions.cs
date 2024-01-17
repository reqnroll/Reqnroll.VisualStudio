namespace Reqnroll.VisualStudio.Diagnostics;

public static class DeveroomLoggerExtensions
{
    public static bool IsLogging(this IDeveroomLogger logger, TraceLevel messageLevel)
        => messageLevel <= logger.Level;

    public static void LogError(this IDeveroomLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Error, message, callerName);
        logger.Log(msg);
    }

    public static void LogWarning(this IDeveroomLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Warning, message, callerName);
        logger.Log(msg);
    }

    public static void LogInfo(this IDeveroomLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Info, message, callerName);
        logger.Log(msg);
    }

    public static void LogVerbose(this IDeveroomLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Verbose, message, callerName);
        logger.Log(msg);
    }

    public static void LogVerbose(this IDeveroomLogger logger, Func<string> message,
        [CallerMemberName] string callerName = "???")
    {
        if (!logger.IsLogging(TraceLevel.Verbose)) return;

        var msg = new LogMessage(TraceLevel.Verbose, message(), callerName);
        logger.Log(msg);
    }

    public static void LogException(this IDeveroomLogger logger, IMonitoringService monitoringService, Exception ex,
        string message = "Exception", [CallerMemberName] string callerName = "???")
    {
        monitoringService.MonitorError(ex);
        LogException(logger, ex, message, callerName);
    }

    public static void LogException(this IDeveroomLogger logger, Exception ex, string message = "Exception",
        [CallerMemberName] string callerName = "???")
    {
        Debug.Fail(ex.ToString());
        var msg = new LogMessage(TraceLevel.Error, message, callerName, ex);
        logger.Log(msg);
    }

    public static void LogVerboseException(this IDeveroomLogger logger, IMonitoringService monitoringService,
        Exception ex, string message = "Exception", [CallerMemberName] string callerName = "???")
    {
        monitoringService.MonitorError(ex, false);
        var msg = new LogMessage(TraceLevel.Verbose, message, callerName, ex);
        logger.Log(msg);
    }

    //TODO: merge IDeveroomLogger with IMonitoringService
    public static void LogDebugException(this IDeveroomLogger logger, Exception ex, string message = "Exception",
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Verbose, message, callerName, ex);
        logger.Log(msg);
    }


    public static void Trace(this IDeveroomLogger logger, Stopwatch sw, string message = "",
        [CallerFilePath] string callerFilePath = "?", [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerName = "???")
    {
        if (sw.ElapsedMilliseconds > 10)
            Trace(logger, $"{sw.Elapsed} {message}", callerFilePath, callerLineNumber, callerName);
    }

    public static void Trace(this IDeveroomLogger logger, string message = "",
        [CallerFilePath] string callerFilePath = "?", [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerName = "???")
    {
        logger.LogVerbose($"{message} in {callerFilePath}: line {callerLineNumber}", callerName);
    }
}
