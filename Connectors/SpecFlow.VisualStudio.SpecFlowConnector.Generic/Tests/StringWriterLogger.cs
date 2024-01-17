namespace SpecFlowConnector.Tests;

public class StringWriterLogger : Logger<StringWriter>
{
    private StringWriter? _writer;

    protected virtual StringWriter CreateWriter() => new();

    protected override string Format(Log log) => $"{log.Level} {log.Message}";

    protected override StringWriter GetTextWriter(LogLevel level) => _writer ??= CreateWriter();

    public override string ToString() => GetTextWriter(LogLevel.Info).GetStringBuilder().ToString();
}
