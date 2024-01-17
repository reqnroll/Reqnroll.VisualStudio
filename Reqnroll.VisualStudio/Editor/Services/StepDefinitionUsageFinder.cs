#nullable disable
namespace Reqnroll.VisualStudio.Editor.Services;

public class StepDefinitionUsage
{
    public StepDefinitionUsage(SourceLocation sourceLocation, Step step)
    {
        SourceLocation = sourceLocation;
        Step = step;
    }

    public SourceLocation SourceLocation { get; }
    public Step Step { get; }

    public override string ToString() => $"{SourceLocation}";
}

public class StepDefinitionUsageFinder
{
    private readonly IIdeScope _ideScope;

    public StepDefinitionUsageFinder(IIdeScope ideScope)
    {
        _ideScope = ideScope;
    }

    public IEnumerable<StepDefinitionUsage> FindUsages(ProjectStepDefinitionBinding[] stepDefinitions,
        string[] featureFiles, DeveroomConfiguration configuration)
    {
        return featureFiles.SelectMany(ff => FindUsages(stepDefinitions, ff, configuration));
    }

    private IEnumerable<StepDefinitionUsage> FindUsages(ProjectStepDefinitionBinding[] stepDefinitions,
        string featureFilePath, DeveroomConfiguration configuration) =>
        LoadContent(featureFilePath, out string featureFileContent)
            ? FindUsagesFromContent(stepDefinitions, featureFileContent, featureFilePath, configuration)
            : Enumerable.Empty<StepDefinitionUsage>();

    private bool LoadContent(string featureFilePath, out string content)
    {
        if (LoadAlreadyOpenedContent(featureFilePath, out string openedContent))
        {
            content = openedContent;
            return true;
        }

        if (LoadContentFromFile(featureFilePath, out string fileContent))
        {
            content = fileContent;
            return true;
        }

        content = string.Empty;
        return false;
    }

    private bool LoadContentFromFile(string featureFilePath, out string content)
    {
        try
        {
            content = _ideScope.FileSystem.File.ReadAllText(featureFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _ideScope.Logger.LogDebugException(ex);
            content = string.Empty;
            return false;
        }
    }

    private bool LoadAlreadyOpenedContent(string featureFilePath, out string content)
    {
        var sl = new SourceLocation(featureFilePath, 1, 1);
        if (!_ideScope.GetTextBuffer(sl, out ITextBuffer tb))
        {
            content = string.Empty;
            return false;
        }

        content = tb.CurrentSnapshot.GetText();
        return true;
    }

    public IEnumerable<StepDefinitionUsage> FindUsagesFromContent(ProjectStepDefinitionBinding[] stepDefinitions,
        string featureFileContent, string featureFilePath, DeveroomConfiguration configuration)
    {
        var dialectProvider = ReqnrollGherkinDialectProvider.Get(configuration.DefaultFeatureLanguage);
        var parser = new DeveroomGherkinParser(dialectProvider, _ideScope.MonitoringService);
        parser.ParseAndCollectErrors(featureFileContent, _ideScope.Logger,
            out var gherkinDocument, out _);

        var featureNode = gherkinDocument?.Feature;
        if (featureNode == null)
            yield break;

        var dummyRegistry = ProjectBindingRegistry.FromStepDefinitions(stepDefinitions);

        var featureContext = new UsageFinderContext(featureNode);

        foreach (var scenarioDefinition in featureNode.FlattenStepsContainers())
        {
            var context = new UsageFinderContext(scenarioDefinition, featureContext);
            foreach (var step in scenarioDefinition.Steps)
            {
                var matchResult = dummyRegistry.MatchStep(step, context);
                if (matchResult.HasDefined)
                    yield return new StepDefinitionUsage(
                        GetSourceLocation(step, featureFilePath), step);
            }
        }
    }

    private SourceLocation GetSourceLocation(Step step, string featureFilePath) =>
        new(featureFilePath, step.Location.Line, step.Location.Column);

    private class UsageFinderContext : IGherkinDocumentContext
    {
        public UsageFinderContext(object node, IGherkinDocumentContext parent = null)
        {
            Node = node;
            Parent = parent;
        }

        public IGherkinDocumentContext Parent { get; }
        public object Node { get; }
    }
}
