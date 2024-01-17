namespace Reqnroll.VisualStudio.VsxStubs;

public class StubTagAggregator<T> : ITagAggregator<T> where T : ITag
{
    private readonly IBufferGraph _bufferGraph;
    private readonly ITagger<T> _tagger;

    public StubTagAggregator(ITagger<T> tagger, IBufferGraph bufferGraph)
    {
        _tagger = tagger;
        _bufferGraph = bufferGraph;
    }

    public IBufferGraph BufferGraph => throw new NotImplementedException();

    public event EventHandler<TagsChangedEventArgs>? TagsChanged;
    public event EventHandler<BatchedTagsChangedEventArgs>? BatchedTagsChanged;

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IMappingTagSpan<T>> GetTags(SnapshotSpan span)
    {
        foreach (var tagSpan in _tagger.GetTags(new NormalizedSnapshotSpanCollection(span.Snapshot, new Span[] {span})))
        {
            var mappingSpan = VsxStubObjects.CreateObject<IMappingSpan>(
                "Microsoft.VisualStudio.Text.Utilities.MappingSpanSnapshot, Microsoft.VisualStudio.Platform.VSEditor",
                span.Snapshot, tagSpan.Span,
                SpanTrackingMode.EdgeExclusive, _bufferGraph);

            yield return new MappingTagSpan<T>(mappingSpan, tagSpan.Tag);
        }
    }

    public IEnumerable<IMappingTagSpan<T>> GetTags(IMappingSpan span) => throw new NotImplementedException();

    public IEnumerable<IMappingTagSpan<T>> GetTags(NormalizedSnapshotSpanCollection snapshotSpans)
    {
        foreach (var snapshotSpan in snapshotSpans)
        foreach (var mappingTagSpan in GetTags(snapshotSpan))
            yield return mappingTagSpan;
    }
}
