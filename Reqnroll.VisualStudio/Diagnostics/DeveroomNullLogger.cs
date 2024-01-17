namespace Reqnroll.VisualStudio.Diagnostics;

public class DeveroomNullLogger : IDeveroomLogger
{
    public TraceLevel Level { get; } = TraceLevel.Off;

    public void Log(LogMessage message)
    {
        //nop;
    }
}
