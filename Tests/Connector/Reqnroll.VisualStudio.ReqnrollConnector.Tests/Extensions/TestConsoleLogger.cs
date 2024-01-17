namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests.Extensions;

public class TestConsoleLogger : Logger<TextWriter>
{
    private readonly ConcurrentDictionary<LogLevel, StringWriter> _builders;

    public TestConsoleLogger()
    {
        _builders = new ConcurrentDictionary<LogLevel, StringWriter>();
    }

    public string this[LogLevel level]
    {
        get
        {
            var stringWriter = GetTextWriter(level) as StringWriter;
            return stringWriter!.GetStringBuilder().ToString().TrimEnd('\r', '\n');
        }
    }

    protected override string Format(Log log) => log.Message;

    protected override TextWriter GetTextWriter(LogLevel level) => _builders.GetOrAdd(level, _ => new StringWriter());

    public override string ToString() => new StringBuilder()
        .AppendLines(_builders
            .OrderByDescending(b => b.Key)
            .Select(builder => $"{builder.Key} {builder.Value}"))
        .ToString();
}
