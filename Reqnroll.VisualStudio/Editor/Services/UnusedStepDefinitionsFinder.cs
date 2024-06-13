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

    public IEnumerable<ProjectStepDefinitionBinding> FindUnused(ProjectBindingRegistry bindingRegistry, string[] featureFiles, DeveroomConfiguration configuration)
    {
        var stepDefUsageCounts = bindingRegistry.StepDefinitions.ToDictionary(stepDef => stepDef, _ => 0);
        foreach (var ff in featureFiles)
        {
            var usedSteps = FindUsed(bindingRegistry, ff, configuration);
            foreach (var step in usedSteps) stepDefUsageCounts[step]++;
        }

        var allUsedSteps = stepDefUsageCounts.Where(x => x.Value > 0).Select(x => x.Key).ToList();
        var allUnusedSteps = stepDefUsageCounts.Where(x => x.Value == 0).Select(x => x.Key);
        return allUnusedSteps.Where(x => !allUsedSteps.Any(y => y.Expression.Equals(x.Expression) && ReferenceEquals(x.Implementation, y.Implementation)));
    }

    protected IEnumerable<ProjectStepDefinitionBinding> FindUsed(ProjectBindingRegistry bindingRegistry,
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

        var featureContext = new StepFinderContext(featureNode);
        var usedSteps = new List<ProjectStepDefinitionBinding>();

        foreach (var scenarioDefinition in featureNode.FlattenStepsContainers())
        {
            var context = new StepFinderContext(scenarioDefinition, featureContext);

            foreach (var step in scenarioDefinition.Steps)
            {
                var matchResult = bindingRegistry.MatchStep(step, context);

                usedSteps.AddRange(matchResult.Items.Where(m => m.Type == MatchResultType.Defined || m.Type == MatchResultType.Ambiguous)
                                                 .Select(i => i.MatchedStepDefinition));
            }
        }

        return usedSteps;
    }
}