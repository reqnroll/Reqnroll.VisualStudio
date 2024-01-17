namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class DeveroomXUnitLogger : IDeveroomLogger
{
    private readonly Stopwatch _stopwatch;
    private readonly ITestOutputHelper _testOutputHelper;
    private volatile int _order;

    public DeveroomXUnitLogger(ITestOutputHelper testOutputHelper, TraceLevel level = TraceLevel.Verbose)
    {
        Level = level;
        _testOutputHelper = testOutputHelper;
        _stopwatch = Stopwatch.StartNew();
    }

    public TraceLevel Level { get; }

    public void Log(LogMessage message)
    {
        if (message.Level > Level) return;

        var content =
            $"{Interlocked.Increment(ref _order):0000} {_stopwatch.Elapsed:m\\:ss\\.ffffff} {message.Level,5} {message.ManagedThreadId,5}  {message.CallerMethod}:{message.Message}";
        if (message.Exception != null) content += $"{Environment.NewLine}{message.Exception}";

        _testOutputHelper.WriteLine(content);
    }
}
