namespace Reqnroll.VisualStudio.Editor.Services.StepDefinitions;

public record AnalyzedStepDefinitionExpressionSimpleTextPart : AnalyzedStepDefinitionExpressionPart
{
    public AnalyzedStepDefinitionExpressionSimpleTextPart(string text, string unescapedText)
    {
        Text = text;
        UnescapedText = unescapedText ?? text;
    }

    public string Text { get; }
    public string UnescapedText { get; }
    public override string ExpressionText => Text;

    public override string ToString() => $"simple text:`{ExpressionText}`";
}
