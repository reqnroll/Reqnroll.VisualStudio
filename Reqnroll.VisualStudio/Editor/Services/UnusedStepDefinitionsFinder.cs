using Reqnroll.VisualStudio.ProjectSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable disable
namespace Reqnroll.VisualStudio.Editor.Services;

public class UnusedStepDefinitionsFinder : StepFinderBase
{
    private readonly IIdeScope _ideScope;

    public UnusedStepDefinitionsFinder(IIdeScope ideScope) : base(ideScope)
    {
        _ideScope = ideScope;
    }

    public IEnumerable<ProjectStepDefinitionBinding> FindUnused(ProjectBindingRegistry bindingRegistry,
                                                        string[] featureFiles, DeveroomConfiguration configuration)
    {
        return featureFiles.SelectMany(ff => FindUnused(bindingRegistry, ff, configuration));
    }

    public IEnumerable<ProjectStepDefinitionBinding> FindUnused(ProjectBindingRegistry bindingRegistry, 
        string featureFilePath, DeveroomConfiguration configuration) => 
        LoadContent(featureFilePath, out string featureFileContent)
        ? FindUsagesFromContent(bindingRegistry, featureFileContent, featureFilePath, configuration)
        : Enumerable.Empty<ProjectStepDefinitionBinding>();

    private IEnumerable<ProjectStepDefinitionBinding> FindUsagesFromContent(ProjectBindingRegistry bindingRegistry, string featureFileContent, string featureFilePath, DeveroomConfiguration configuration)
    {
        var dialectProvider = ReqnrollGherkinDialectProvider.Get(configuration.DefaultFeatureLanguage);
        var parser = new DeveroomGherkinParser(dialectProvider, _ideScope.MonitoringService);
        parser.ParseAndCollectErrors(featureFileContent, _ideScope.Logger,
            out var gherkinDocument, out _);

        var featureNode = gherkinDocument?.Feature;
        if (featureNode == null)
            return Enumerable.Empty<ProjectStepDefinitionBinding>();

        //this set keeps track of which step definitions have not been used. We start with ALL step definitions, and will subtract those that match steps in the feature file
        HashSet<ProjectStepDefinitionBinding> stepDefinitionsSet = new(bindingRegistry.StepDefinitions);

        var featureContext = new StepFinderContext(featureNode);

        foreach (var scenarioDefinition in featureNode.FlattenStepsContainers())
        {
            var context = new StepFinderContext(scenarioDefinition, featureContext);

            foreach (var step in scenarioDefinition.Steps)
            {
                var matchResult = bindingRegistry.MatchStep(step, context);

                var usedSteps = matchResult.Items.Where(m => m.Type == MatchResultType.Defined || m.Type == MatchResultType.Ambiguous)
                                                 .Select(i => i.MatchedStepDefinition)
                                                 .ToHashSet<ProjectStepDefinitionBinding>();
                stepDefinitionsSet.ExceptWith(usedSteps);
            }
        }

        return stepDefinitionsSet.ToList();

    }
}
