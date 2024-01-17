namespace ReqnrollConnector.Logging;

public abstract class Logger<T> : ILogger where T : TextWriter
{
    private readonly StringBuilder _buffer = new();

    public void Log(Log log)
    {
        var message = Format(log);
        _buffer.AppendLine(message);
        GetTextWriter(log.Level).WriteLine(message);
    }

    protected abstract string Format(Log log);

    protected abstract T GetTextWriter(LogLevel level);

    public override string ToString()
    {
        return _buffer.ToString();
    }
}
