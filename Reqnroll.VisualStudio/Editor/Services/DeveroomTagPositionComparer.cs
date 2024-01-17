namespace Reqnroll.VisualStudio.Editor.Services;

internal class DeveroomTagPositionComparer : IComparer<DeveroomTag>
{
    public int Compare(DeveroomTag t1, DeveroomTag t2)
    {
        if (ReferenceEquals(t1, t2)) return 0;
        if (ReferenceEquals(null, t2)) return 1;
        if (ReferenceEquals(null, t1)) return -1;
        var order = t1.Span.Start.Position.CompareTo(t2.Span.Start.Position);
        if (order != 0) return order;
        order = t1.Span.End.Position.CompareTo(t2.Span.End.Position);
        if (order != 0) return order;
        order = string.Compare(t1.Type, t2.Type, StringComparison.Ordinal);
        return order;
    }
}
