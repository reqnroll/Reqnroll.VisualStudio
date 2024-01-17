namespace Reqnroll.VisualStudio.Diagnostics;

public class DeveroomDebugLogger : IDeveroomLogger
{
#if DEBUG
    public const TraceLevel DefaultDebugTraceLevel = TraceLevel.Verbose;
#else
    public const TraceLevel DefaultDebugTraceLevel = TraceLevel.Off;
#endif
    public TraceLevel Level { get; }

    public DeveroomDebugLogger(TraceLevel level = DefaultDebugTraceLevel)
    {
        Level = level;
        var env = Environment.GetEnvironmentVariable("REQNROLLVS_DEBUG");
        if (env != null)
        {
            if (env.Equals("1") || env.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                Level = TraceLevel.Verbose;
            else if (Enum.TryParse<TraceLevel>(env, true, out var envLevel)) Level = envLevel;
        }
    }

    public void Log(LogMessage message)
    {
        Debug.WriteLineIf(message.Level <= Level, $"{message.Level}: {message.CallerMethod}:{message.Message}",
            "ReqnrollVs");
    }
}
