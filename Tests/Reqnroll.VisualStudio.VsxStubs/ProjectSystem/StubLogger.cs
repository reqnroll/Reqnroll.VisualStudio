namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class StubLogger : IDeveroomLogger
{
    public StubLogger()
    {
    }

    private StubLogger(IEnumerable<LogMessage> messages)
    {
        Logs = new ConcurrentBag<LogMessage>(messages);
    }

    public ConcurrentBag<LogMessage> Logs { get; private set; } = new();
    public ImmutableArray<string> Messages => Logs.Select(l => l.Message).ToImmutableArray();

    public TraceLevel Level => TraceLevel.Verbose;

    public void Log(LogMessage message)
    {
        Logs.Add(message);
    }

    public StubLogger Errors()
    {
        return WithLevel(level => level == TraceLevel.Error);
    }

    public StubLogger Warnings()
    {
        return WithLevel(level => level == TraceLevel.Warning);
    }

    public StubLogger WithLevel(Func<TraceLevel, bool> predicate)
    {
        return new StubLogger(Logs.Where(m => predicate(m.Level)));
    }

    public StubLogger WithoutHeader(string warningHeader)
    {
        return new StubLogger(Logs.Select(m => m with {Message = m.Message.Replace(warningHeader, string.Empty)}));
    }

    public void Clear()
    {
        Logs = new ConcurrentBag<LogMessage>();
    }
}
