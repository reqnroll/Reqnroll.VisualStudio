#nullable enable
namespace Reqnroll.VisualStudio.Editor.Services.StepDefinitions;

public class RegexStepDefinitionExpressionAnalyzer : IStepDefinitionExpressionAnalyzer
{
    public AnalyzedStepDefinitionExpression Parse(string expression)
    {
        var parts = SplitExpressionByGroups(expression);
        return new AnalyzedStepDefinitionExpression(parts);
    }

    private ImmutableArray<AnalyzedStepDefinitionExpressionPart> SplitExpressionByGroups(string regexString)
    {
        var parts = new List<AnalyzedStepDefinitionExpressionPart>();
        var escapedStringBuilder = new StringBuilder();
        var unescapedStringBuilder = new StringBuilder();

        var maskChar = '\\';
        var groupOpenChar = '(';
        var maskedRegexChars = new[] {maskChar, '+', '.', '*', '?', '|', '{', '[', groupOpenChar, '^', '$', '#'};
        int position = 0;
        bool isSimpleText = true;
        while (position < regexString.Length)
        {
            int index = regexString.IndexOfAny(maskedRegexChars, position);
            if (index < 0)
            {
                var remainingText = regexString.Substring(position);
                escapedStringBuilder.Append(remainingText);
                unescapedStringBuilder.Append(remainingText);
                break;
            }

            if (regexString[index] == maskChar && index < regexString.Length - 1)
            {
                escapedStringBuilder.Append(regexString.Substring(position, index - position + 2));
                unescapedStringBuilder.Append(regexString.Substring(position, index - position));
                unescapedStringBuilder.Append(regexString[index + 1]);
                position = index + 2;
            }
            else if (regexString[index] == groupOpenChar && !IsNonCapturingGroup(regexString, index))
            {
                if (index > position)
                {
                    var remainingText = regexString.Substring(position, index - position);
                    escapedStringBuilder.Append(remainingText);
                    unescapedStringBuilder.Append(remainingText);
                }

                parts.Add(CreateTextPart(escapedStringBuilder.ToString(), unescapedStringBuilder.ToString(),
                    isSimpleText));
                isSimpleText = true;
                escapedStringBuilder = new StringBuilder();
                unescapedStringBuilder = new StringBuilder();
                var groupCloseIndex = FindGroupCloseIndex(regexString, index) + 1;
                var parameter = regexString.Substring(index, groupCloseIndex - index);
                parts.Add(new AnalyzedStepDefinitionExpressionParameterPart(parameter));
                position = groupCloseIndex;
            }
            else
            {
                escapedStringBuilder.Append(regexString.Substring(position, index - position + 1));
                unescapedStringBuilder.Append(regexString.Substring(position, index - position));
                position = index + 1;
                isSimpleText = false;
            }
        }

        parts.Add(CreateTextPart(escapedStringBuilder.ToString(), unescapedStringBuilder.ToString(), isSimpleText));
        return parts.ToImmutableArray();
    }

    private AnalyzedStepDefinitionExpressionPart CreateTextPart(string text, string unescapedText, bool isSimpleText) =>
        isSimpleText
            ? new AnalyzedStepDefinitionExpressionSimpleTextPart(text, unescapedText)
            : new AnalyzedStepDefinitionExpressionWithOperatorsTextPart(text);

    private int FindGroupCloseIndex(string regexString, int openPosition)
    {
        int nesting = 0;
        for (int i = openPosition; i < regexString.Length; i++)
            if (regexString[i] == '\\')
            {
                i++;
            }
            else if (regexString[i] == '(')
            {
                nesting++;
            }
            else if (regexString[i] == ')')
            {
                nesting--;
                if (nesting == 0)
                    return i;
            }

        return regexString.Length - 1;
    }

    private bool IsNonCapturingGroup(string regexString, int index) =>
        index + 2 < regexString.Length &&
        regexString[index + 1] == '?' && regexString[index + 2] == ':';
}
