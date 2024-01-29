namespace Reqnroll.VisualStudio.Snippets.Fallback;

public class RegexStepDefinitionSkeletonProvider : DeveroomStepDefinitionSkeletonProvider
{
    public RegexStepDefinitionSkeletonProvider(ReqnrollProjectTraits projectTraits) : base(projectTraits)
    {
    }

    // imported from SpecFlow v2.4
    protected override bool UseVerbatimStringForExpression => true;

    protected override string GetExpression(AnalyzedStepText stepText)
    {
        StringBuilder result = new StringBuilder();

        result.Append(EscapeRegex(stepText.TextParts[0]));
        for (int i = 1; i < stepText.TextParts.Count; i++)
        {
            result.Append(stepText.Parameters[i - 1].RegexPattern);
            result.Append(EscapeRegex(stepText.TextParts[i]));
        }

        return result.ToString();
    }

    private static string EscapeRegex(string text) => Regex.Escape(text).Replace("\"", "\"\"").Replace("\\ ", " ");

    protected override IStepTextAnalyzer CreateStepTextAnalyzer() => new RegexExpressionStepTextAnalyzer();
}
