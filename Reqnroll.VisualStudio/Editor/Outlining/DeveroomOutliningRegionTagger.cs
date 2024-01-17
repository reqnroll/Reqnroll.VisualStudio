#nullable disable

namespace Reqnroll.VisualStudio.Editor.Outlining;

internal class DeveroomOutliningRegionTagger : DeveroomTagConsumer, ITagger<IOutliningRegionTag>
{
    public static readonly string[] OutlinedTags =
    {
        DeveroomTagTypes.RuleBlock,
        DeveroomTagTypes.ScenarioDefinitionBlock,
        DeveroomTagTypes.ExamplesBlock,
        DeveroomTagTypes.DataTable,
        DeveroomTagTypes.DocString
    };

    public DeveroomOutliningRegionTagger(ITextBuffer buffer, ITagAggregator<DeveroomTag> tagAggregator)
        : base(buffer, tagAggregator)
    {
    }

    public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        return GetDeveroomTags(spans, t => OutlinedTags.Contains(t.Type))
            .Select(tagSpan => new TagSpan<IOutliningRegionTag>(GetOutliningSpan(tagSpan),
                CreateOutliningRegionTag(tagSpan.Key)));
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    private SnapshotSpan GetOutliningSpan(KeyValuePair<SnapshotSpan, DeveroomTag> tagSpan) =>
        new(tagSpan.Key.Start.GetContainingLine().End, tagSpan.Key.End);

    private OutliningRegionTag CreateOutliningRegionTag(SnapshotSpan span) =>
        new(false, false, "...", new OutliningHint(span));

    protected override void RaiseChanged(SnapshotSpan span)
    {
        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
    }

    private class OutliningHint
    {
        private readonly Lazy<string> _hintText;

        public OutliningHint(SnapshotSpan span)
        {
            _hintText = new Lazy<string>(span.GetText);
        }

        public override string ToString() => _hintText.Value;
    }
}
