namespace Reqnroll.VisualStudio.Diagnostics;

public class OutputWindowPaneLogger : IDeveroomLogger
{
    private readonly IDeveroomOutputPaneServices _outputPaneServices;

    public OutputWindowPaneLogger(IDeveroomOutputPaneServices outputPaneServices)
    {
        _outputPaneServices = outputPaneServices;
    }

    public TraceLevel Level { get; set; } = TraceLevel.Info;

    public void Log(LogMessage message)
    {
        if (message.Level <= Level) WriteToOutputPane(message.Level, $"{message.CallerMethod}: {message.Message}");
    }

    private void WriteToOutputPane(TraceLevel messageLevel, string message)
    {
        _outputPaneServices.SendWriteLine($"{messageLevel}: {message}");
        if (messageLevel <= TraceLevel.Warning)
            _outputPaneServices.Activate();
    }
}
