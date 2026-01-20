using System.Text;

namespace ReqnrollConnector.Logging;

public sealed class ConsoleLogger : Logger<TextWriter>
{
    public ConsoleLogger()
    {
        Console.OutputEncoding = Encoding.UTF8;
    }

    protected override string Format(Log log) => log.Message;

    protected override TextWriter GetTextWriter(LogLevel level)
    {
        return level switch
        {
            LogLevel.Error => Console.Error,
            LogLevel.Info => Console.Out,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
    }
}
