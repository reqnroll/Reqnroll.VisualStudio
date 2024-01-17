#nullable disable

namespace Reqnroll.VisualStudio.Discovery;

internal static class GherkinDocumentContextCalculator
{
    private static T[] EnsureArray<T>(IEnumerable<T> enumerable)
    {
        if (enumerable == null)
            return null;
        if (enumerable is T[] array)
            return array;
        return enumerable.ToArray();
    }

    public static IEnumerable<KeyValuePair<string, IGherkinDocumentContext>> GetBackgroundStepsWithContexts(Step step,
        IGherkinDocumentContext context)
    {
        return GetBackgroundContexts(context).Distinct(TagMatchingContextComparer.Instance)
            .Select(ctx => new KeyValuePair<string, IGherkinDocumentContext>(step.Text, ctx));
    }

    private static IEnumerable<IGherkinDocumentContext> GetBackgroundContexts(IGherkinDocumentContext context)
    {
        var featureOrRuleContext = context.GetParentOf<IHasChildren>();
        if (!(featureOrRuleContext?.Node is IHasChildren featureOrRule))
            yield break;

        var scenarioDefinitions = featureOrRule.FlattenScenarioDefinitions().ToArray();
        if (!scenarioDefinitions.Any()) // if there are no scenarios yet, we use the feature context for matching
            yield return featureOrRuleContext;

        foreach (var scenarioDefinition in scenarioDefinitions)
        {
            var subContext = new TagMatchingContext(featureOrRuleContext, scenarioDefinition);
            yield return subContext;
            if (scenarioDefinition is ScenarioOutline scenarioOutline) // create scopes for tagged example sets
                foreach (var exampleSet in scenarioOutline.Examples.Where(es => es.Tags?.Any() ?? false))
                {
                    var exampleSetContext = new TagMatchingContext(subContext, exampleSet);
                    yield return exampleSetContext;
                }
        }
    }

    public static IEnumerable<KeyValuePair<string, IGherkinDocumentContext>> GetScenarioOutlineStepsWithContexts(
        Step step, IGherkinDocumentContext context)
    {
        var scenarioOutline = (ScenarioOutline) context.Node;

        var subContext = context;
        bool hasExamples = false;
        foreach (var scenarioOutlineExamples in scenarioOutline.Examples)
        {
            var exampleTags = EnsureArray(scenarioOutlineExamples.Tags);
            if (exampleTags != null && exampleTags.Any())
                subContext = new TagMatchingContext(context, scenarioOutlineExamples);

            if (scenarioOutlineExamples.TableHeader != null && scenarioOutlineExamples.TableBody != null)
            {
                var header = scenarioOutlineExamples.TableHeader.Cells.Select(c => c.Value).ToArray();
                foreach (var exampleRow in scenarioOutlineExamples.TableBody)
                {
                    hasExamples = true;
                    var stepText = MatchedScenarioOutlinePlaceholder.ReplaceScenarioOutlinePlaceholders(step,
                        match =>
                        {
                            int headerIndex = Array.IndexOf(header, match.Name);
                            if (headerIndex < 0)
                                return match.Value;
                            return exampleRow.Cells.ElementAtOrDefault(headerIndex)?.Value ?? match.Value;
                        });

                    yield return new KeyValuePair<string, IGherkinDocumentContext>(stepText, subContext);
                }
            }
        }

        if (!hasExamples)
            // for empty SO, we create a context with the original step text (incl. placeholders)
            yield return new KeyValuePair<string, IGherkinDocumentContext>(step.Text, context);
    }

    private class TagMatchingContext : IGherkinDocumentContext
    {
        public TagMatchingContext(IGherkinDocumentContext parent, object node)
        {
            Parent = parent;
            Node = node;
        }

        public IGherkinDocumentContext Parent { get; }
        public object Node { get; }
    }

    private class TagMatchingContextComparer : IEqualityComparer<IGherkinDocumentContext>
    {
        public static readonly TagMatchingContextComparer Instance = new();

        public bool Equals(IGherkinDocumentContext x, IGherkinDocumentContext y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;

            return x.GetTagNames().SequenceEqual(y.GetTagNames());
        }

        public int GetHashCode(IGherkinDocumentContext obj)
        {
            var firstTag = obj.GetTagNames().FirstOrDefault();
            return firstTag?.GetHashCode() ?? 547;
        }
    }
}
