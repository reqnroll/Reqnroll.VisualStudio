namespace Reqnroll.VisualStudio.Diagnostics;

[Export(typeof(IDeveroomLogger))]
[Export(typeof(DeveroomCompositeLogger))]
public class DeveroomCompositeLogger : IDeveroomLogger, IEnumerable<IDeveroomLogger>
{
    private IDeveroomLogger[] _loggers = Array.Empty<IDeveroomLogger>();

    public TraceLevel Level { get; private set; } = TraceLevel.Off;

    public void Log(LogMessage message)
    {
        foreach (var logger in _loggers)
            logger.Log(message);
    }

    public IEnumerator<IDeveroomLogger> GetEnumerator() => _loggers.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public DeveroomCompositeLogger Add(IDeveroomLogger logger)
    {
        _loggers = _loggers.Concat(new[] {logger}).ToArray();
        Level = _loggers.Max(l => l.Level);
        return this;
    }
}
