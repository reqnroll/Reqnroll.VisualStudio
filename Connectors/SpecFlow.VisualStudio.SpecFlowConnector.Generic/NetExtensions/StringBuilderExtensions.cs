namespace SpecFlow.VisualStudio.SpecFlowConnector;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendLines(this StringBuilder sb, IEnumerable<string> seq) =>
        AppendSequence(sb, seq, (s, x) => s.AppendLine(x));

    public static StringBuilder AppendSequence<T>(this StringBuilder sb, IEnumerable<T> seq,
        Func<StringBuilder, T, StringBuilder> fn)
    {
        foreach (var t in seq) fn(sb, t);
        return sb;
    }
}
