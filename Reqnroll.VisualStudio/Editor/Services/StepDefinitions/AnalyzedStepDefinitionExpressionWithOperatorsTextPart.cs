namespace Reqnroll.VisualStudio.Editor.Services.StepDefinitions;

public record AnalyzedStepDefinitionExpressionWithOperatorsTextPart : AnalyzedStepDefinitionExpressionPart
{
    public AnalyzedStepDefinitionExpressionWithOperatorsTextPart(string text)
    {
        Text = text;
    }

    public string Text { get; }
    public override string ExpressionText => Text;

    public override string ToString() => $"escaped text:`{ExpressionText}`";
}
