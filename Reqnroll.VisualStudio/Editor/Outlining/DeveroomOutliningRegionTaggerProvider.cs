using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Editor.Outlining;

[Export(typeof(ITaggerProvider))]
[ContentType(VsContentTypes.FeatureFile)]
[TagType(typeof(IOutliningRegionTag))]
public class DeveroomOutliningRegionTaggerProvider : ITaggerProvider
{
    private readonly IBufferTagAggregatorFactoryService _aggregatorFactory;

    [ImportingConstructor]
    public DeveroomOutliningRegionTaggerProvider(IBufferTagAggregatorFactoryService aggregatorFactory)
    {
        _aggregatorFactory = aggregatorFactory;
    }

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        var tagAggregator = _aggregatorFactory.CreateTagAggregator<DeveroomTag>(buffer);
        return buffer.Properties.GetOrCreateSingletonProperty(() =>
            (ITagger<T>) new DeveroomOutliningRegionTagger(buffer, tagAggregator));
    }
}
