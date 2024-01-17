namespace Reqnroll.VisualStudio.Editor.Services.StepDefinitions;

public record AnalyzedStepDefinitionExpressionParameterPart : AnalyzedStepDefinitionExpressionPart
{
    public AnalyzedStepDefinitionExpressionParameterPart(string parameterExpression)
    {
        ParameterExpression = parameterExpression;
    }

    public string ParameterExpression { get; }

    public override string ExpressionText => ParameterExpression;

    public override string ToString() => $"Parameter:`{ExpressionText}`";
}
