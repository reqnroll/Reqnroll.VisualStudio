using System;

namespace Reqnroll.VisualStudio.Snippets.Fallback;

public class CucumberExpressionSkeletonProvider : DeveroomStepDefinitionSkeletonProvider
{
    public CucumberExpressionSkeletonProvider(ReqnrollProjectTraits projectTraits)
        : base(projectTraits)
    {

    }

    protected override bool UseVerbatimStringForExpression => false;

    protected override string GetExpression(AnalyzedStepText stepText)
    {
        StringBuilder result = new StringBuilder();

        result.Append(EscapeCucumberExpression(stepText.TextParts[0]));
        for (int i = 1; i < stepText.TextParts.Count; i++)
        {
            result.Append(stepText.Parameters[i - 1].RegexPattern);
            result.Append(EscapeCucumberExpression(stepText.TextParts[i]));
        }

        return result.ToString();
    }

    private string EscapeCucumberExpression(string text)
    {
        var escapedCukeEx = text.Replace(@"\", @"\\").Replace(@"{", @"\{").Replace("(", @"\");
        var escapedCSharpString = escapedCukeEx.Replace(@"\", @"\\").Replace(@"""", @"\""");
        return escapedCSharpString;
    }

    protected override IStepTextAnalyzer CreateStepTextAnalyzer() => new CucumberExpressionStepTextAnalyzer();
}
