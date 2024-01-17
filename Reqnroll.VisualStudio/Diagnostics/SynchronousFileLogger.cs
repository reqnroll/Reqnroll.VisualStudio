namespace Reqnroll.VisualStudio.Diagnostics;

public class SynchronousFileLogger : AsynchronousFileLogger
{
    public SynchronousFileLogger()
        : base(new FileSystem(), TraceLevel.Verbose)
    {
        EnsureLogFolder();
    }

    public override void Log(LogMessage message)
    {
        try
        {
            WriteLogMessage(message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, $"Error writing to the {LogFilePath}");
        }
    }
}
