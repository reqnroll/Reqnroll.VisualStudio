using System.Linq;

namespace Reqnroll.VisualStudio.Editor.Services.StepDefinitions;

public class AnalyzedStepDefinitionExpression
{
    public AnalyzedStepDefinitionExpression(ImmutableArray<AnalyzedStepDefinitionExpressionPart> parts)
    {
        Parts = parts;
    }

    public ImmutableArray<AnalyzedStepDefinitionExpressionPart> Parts { get; }

    public bool ContainsOnlySimpleText =>
        Parts.OfType<AnalyzedStepDefinitionExpressionSimpleTextPart>().Count() == Parts.Length / 2 + 1;

    public IEnumerable<AnalyzedStepDefinitionExpressionParameterPart> ParameterParts =>
        Parts.OfType<AnalyzedStepDefinitionExpressionParameterPart>();
}
