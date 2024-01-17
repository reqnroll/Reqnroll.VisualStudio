using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Editor.Completions;

public class StepDefinitionSampler
{
    public string GetStepDefinitionSample(ProjectStepDefinitionBinding stepDefinitionBinding)
    {
        var regexTextCore = stepDefinitionBinding.Expression;

        IStepDefinitionExpressionAnalyzer analyzer = new RegexStepDefinitionExpressionAnalyzer();
        var analyzedStepDefinitionExpression = analyzer.Parse(regexTextCore);

        if (analyzedStepDefinitionExpression.Parts.Length == 1)
            return GetUnescapedText(analyzedStepDefinitionExpression.Parts[0]);

        if (!analyzedStepDefinitionExpression.ContainsOnlySimpleText) return regexTextCore;

        var completionTextBuilder = new StringBuilder();
        for (int i = 0; i < analyzedStepDefinitionExpression.Parts.Length; i += 2)
        {
            completionTextBuilder.Append(GetUnescapedText(analyzedStepDefinitionExpression.Parts[i]));
            if (i < analyzedStepDefinitionExpression.Parts.Length - 1)
            {
                completionTextBuilder.Append("[");
                completionTextBuilder.Append(GetPlaceHolderText(stepDefinitionBinding, i / 2));
                completionTextBuilder.Append("]");
            }
        }

        return completionTextBuilder.ToString();
    }

    private string GetUnescapedText(AnalyzedStepDefinitionExpressionPart part)
    {
        if (part is AnalyzedStepDefinitionExpressionSimpleTextPart simpleTextPart)
            return simpleTextPart.UnescapedText;
        return part.ExpressionText;
    }

    private string GetPlaceHolderText(ProjectStepDefinitionBinding stepDefinitionBinding, int groupIndex)
    {
        if (stepDefinitionBinding.Implementation?.ParameterTypes == null ||
            groupIndex >= stepDefinitionBinding.Implementation.ParameterTypes.Length)
            return "???";

        var typeName = stepDefinitionBinding.Implementation.ParameterTypes[groupIndex];
        switch (typeName)
        {
            case TypeShortcuts.Int32Type:
                return "int";
            case TypeShortcuts.StringType:
                return "string";
        }

        return typeName.Split('.').Last();
    }
}
