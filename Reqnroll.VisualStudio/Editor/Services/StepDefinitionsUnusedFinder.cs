using Reqnroll.VisualStudio.ProjectSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable disable
namespace Reqnroll.VisualStudio.Editor.Services;

public class StepDefinitionsUnusedFinder
{
    private readonly IIdeScope _ideScope;

    public StepDefinitionsUnusedFinder(IIdeScope ideScope)
    {
        _ideScope = ideScope;
    }

    public IEnumerable<ProjectStepDefinitionBinding> FindUnused(ProjectStepDefinitionBinding[] stepDefinitions,
                                                        string[] featureFiles, DeveroomConfiguration configuration)
    {
        return featureFiles.SelectMany(ff => FindUnused(stepDefinitions, ff, configuration));
    }

    public IEnumerable<ProjectStepDefinitionBinding> FindUnused(ProjectStepDefinitionBinding[] stepDefinitions, 
        string featureFilePath, DeveroomConfiguration configuration) => 
        LoadContent(featureFilePath, out string featureFileContent)
        ? FindUsagesFromContent(stepDefinitions, featureFileContent, featureFilePath, configuration)
        : Enumerable.Empty<ProjectStepDefinitionBinding>();

    private IEnumerable<ProjectStepDefinitionBinding> FindUsagesFromContent(ProjectStepDefinitionBinding[] stepDefinitions, string featureFileContent, string featureFilePath, DeveroomConfiguration configuration)
    {
        var dialectProvider = ReqnrollGherkinDialectProvider.Get(configuration.DefaultFeatureLanguage);
        var parser = new DeveroomGherkinParser(dialectProvider, _ideScope.MonitoringService);
        parser.ParseAndCollectErrors(featureFileContent, _ideScope.Logger,
            out var gherkinDocument, out _);

        var featureNode = gherkinDocument?.Feature;
        if (featureNode == null)
            return Enumerable.Empty<ProjectStepDefinitionBinding>();

        var dummyRegistry = ProjectBindingRegistry.FromBindings(stepDefinitions);

        //this set keeps track of which step definitions have not been used. We start with ALL step definitions, and will subtract those that match steps in the feature file
        HashSet<ProjectStepDefinitionBinding> stepDefinitionsSet = new(stepDefinitions);

        var featureContext = new UnusedFinderContext(featureNode);

        foreach (var scenarioDefinition in featureNode.FlattenStepsContainers())
        {
            var context = new UnusedFinderContext(scenarioDefinition, featureContext);

            // this iterates over steps in the scenario and finds those that are bound or not bound. We need to change the way the Registry works to match by Step Definition.
            foreach (var step in scenarioDefinition.Steps)
            {
                var matchResult = dummyRegistry.MatchStep(step, context);

                var usedSteps = matchResult.Items.Where(m => m.Type == MatchResultType.Defined || m.Type == MatchResultType.Ambiguous)
                                                 .Select(i => i.MatchedStepDefinition)
                                                 .ToHashSet<ProjectStepDefinitionBinding>();
                stepDefinitionsSet.ExceptWith(usedSteps);
            }
        }

        return stepDefinitionsSet.ToList();

    }

    //TODO: duplicate of what is in StepDefinitionUsageFinder; consider refactoring to shared implementation
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

    //TODO: duplicate of what is in StepDefinitionUsageFinder; consider refactoring to shared implementation
    private class UnusedFinderContext : IGherkinDocumentContext
    {
        public UnusedFinderContext(object node, IGherkinDocumentContext parent = null)
        {
            Node = node;
            Parent = parent;
        }

        public IGherkinDocumentContext Parent { get; }
        public object Node { get; }
    }
}
