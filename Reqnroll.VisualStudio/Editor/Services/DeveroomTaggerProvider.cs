using Reqnroll.VisualStudio.SpecFlowExtensionDetection;

namespace Reqnroll.VisualStudio.Editor.Services;

[Export(typeof(ITaggerProvider))]
[Export(typeof(IDeveroomTaggerProvider))]
[ContentType(VsContentTypes.FeatureFile)]
[TagType(typeof(DeveroomTag))]
public class DeveroomTaggerProvider : IDeveroomTaggerProvider
{
    private readonly IIdeScope _ideScope;
    private readonly SpecFlowExtensionDetectionService _detectionService;

    [ImportingConstructor]
    public DeveroomTaggerProvider(IIdeScope ideScope, SpecFlowExtensionDetectionService specFlowExtensionDetectionService)
    {
        _detectionService = specFlowExtensionDetectionService;
        _ideScope = ideScope;
    }

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        _detectionService?.CheckForSpecFlowExtensionOnce();

        if (buffer is not ITextBuffer2 buffer2)
            throw new InvalidOperationException($"Cannot assign {buffer.GetType()} to {typeof(ITextBuffer2)}");

        return buffer.Properties.GetOrCreateSingletonProperty(typeof(ITagger<T>),
            () => (ITagger<T>) CreateFeatureFileTagger(buffer2));
    }

    private ITagger<DeveroomTag> CreateFeatureFileTagger(ITextBuffer2 buffer)
    {
        var project = _ideScope.GetProject(buffer);
        var discoveryService = project.GetDiscoveryService();
        var tagParser = project.GetDeveroomTagParser();
        var configurationProvider = project?.GetDeveroomConfigurationProvider() ?? new ProjectSystemDeveroomConfigurationProvider(_ideScope);
        var featureFileTagger =
            new FeatureFileTagger(configurationProvider, discoveryService, _ideScope.Logger, tagParser, buffer);
        featureFileTagger.ForceReparseOnBackground();
        return featureFileTagger;
    }
}
