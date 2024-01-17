#nullable disable
using Microsoft.VisualStudio.Text.Classification;

namespace Reqnroll.VisualStudio.Editor.Classification;

[Export(typeof(IClassifierProvider))]
[ContentType(VsContentTypes.FeatureFile)]
internal class DeveroomClassifierProvider : IClassifierProvider
{
    public IClassifier GetClassifier(ITextBuffer buffer)
    {
        var tagAggregator = _aggregatorFactory.CreateTagAggregator<DeveroomTag>(buffer);

        return buffer.Properties.GetOrCreateSingletonProperty(() =>
            new DeveroomClassifier(_classificationRegistry, buffer, tagAggregator));
    }
#pragma warning disable 649

    [Import] private IClassificationTypeRegistryService _classificationRegistry;

    [Import] private IBufferTagAggregatorFactoryService _aggregatorFactory;

#pragma warning restore 649
}
