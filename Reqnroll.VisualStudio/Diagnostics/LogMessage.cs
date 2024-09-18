namespace Reqnroll.VisualStudio.Diagnostics;

[DebuggerDisplay("{TimeStamp} {CallerMethod} {Message}")]
public record LogMessage(
    TraceLevel Level,
    string Message,
    string CallerMethod,
    Exception? Exception = default!)
{
    public DateTimeOffset TimeStamp { get; } = DateTimeOffset.Now;
    public int ManagedThreadId { get; } = Thread.CurrentThread.ManagedThreadId;
}
