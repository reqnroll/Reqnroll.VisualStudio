namespace Reqnroll.VisualStudio.Editor.Traceability;

[Export(typeof(ITaggerProvider))]
[ContentType(VsContentTypes.FeatureFile)]
[TagType(typeof(UrlTag))]
public class DeveroomUrlTaggerProvider : ITaggerProvider
{
    private readonly IBufferTagAggregatorFactoryService _aggregatorFactory;
    private readonly IIdeScope _ideScope;

    [ImportingConstructor]
    public DeveroomUrlTaggerProvider(IBufferTagAggregatorFactoryService aggregatorFactory, IIdeScope ideScope)
    {
        _aggregatorFactory = aggregatorFactory;
        _ideScope = ideScope;
    }

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        var tagAggregator = _aggregatorFactory.CreateTagAggregator<DeveroomTag>(buffer);
        return buffer.Properties.GetOrCreateSingletonProperty(() =>
            (ITagger<T>) new DeveroomUrlTagger(buffer, tagAggregator, _ideScope));
    }
}
