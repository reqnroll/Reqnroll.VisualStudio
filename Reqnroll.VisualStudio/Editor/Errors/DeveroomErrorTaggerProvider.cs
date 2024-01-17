using System.Linq;

namespace Reqnroll.VisualStudio.Editor.Errors;

[Export(typeof(ITaggerProvider))]
[ContentType(VsContentTypes.FeatureFile)]
[TagType(typeof(ErrorTag))]
internal class DeveroomErrorTaggerProvider : ITaggerProvider
{
    private readonly IBufferTagAggregatorFactoryService _aggregatorFactory;

    [ImportingConstructor]
    public DeveroomErrorTaggerProvider(IBufferTagAggregatorFactoryService aggregatorFactory)
    {
        _aggregatorFactory = aggregatorFactory;
    }

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        var tagAggregator = _aggregatorFactory.CreateTagAggregator<DeveroomTag>(buffer);
        return buffer.Properties.GetOrCreateSingletonProperty(() =>
            (ITagger<T>) new DeveroomErrorTagger(buffer, tagAggregator));
    }
}
