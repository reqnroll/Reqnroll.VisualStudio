using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Editor.Completions;

[Export(typeof(ICompletionSourceProvider))]
[ContentType(VsContentTypes.FeatureFile)]
[Name("Reqnroll Gherkin completion")]
public class DeveroomCompletionSourceProvider : ICompletionSourceProvider
{
    private readonly IBufferTagAggregatorFactoryService _aggregatorFactory;
    private readonly IIdeScope _ideScope;

    [ImportingConstructor]
    public DeveroomCompletionSourceProvider(IBufferTagAggregatorFactoryService aggregatorFactory, IIdeScope ideScope)
    {
        _aggregatorFactory = aggregatorFactory;
        _ideScope = ideScope;
    }

    public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
    {
        var tagAggregator = _aggregatorFactory.CreateTagAggregator<DeveroomTag>(textBuffer);
        return new DeveroomCompletionSource(textBuffer, tagAggregator, _ideScope);
    }
}
