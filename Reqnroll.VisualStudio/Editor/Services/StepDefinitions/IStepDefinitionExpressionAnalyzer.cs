namespace Reqnroll.VisualStudio.Editor.Services.StepDefinitions;

public interface IStepDefinitionExpressionAnalyzer
{
    AnalyzedStepDefinitionExpression Parse(string expression);
}
