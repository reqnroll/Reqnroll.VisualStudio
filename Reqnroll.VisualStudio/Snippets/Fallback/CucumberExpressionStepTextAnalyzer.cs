using System;
using System.Globalization;
using System.Linq;

namespace Reqnroll.VisualStudio.Snippets.Fallback;

public class CucumberExpressionStepTextAnalyzer : StepTextAnalyzer
{
    protected override AnalyzedStepParameter CreateStepParameter(RecognizedTextType textType, string paramName)
    {
        switch (textType)
        {
            case RecognizedTextType.Integer:
                return new AnalyzedStepParameter("Int32", paramName, "{int}");
            case RecognizedTextType.Decimal:
                return new AnalyzedStepParameter("Decimal", paramName, "{float}");
            case RecognizedTextType.ApostropheString:
            case RecognizedTextType.DoubleQuotedString:
                return new AnalyzedStepParameter("String", paramName, "{string}");
            default:
                throw new NotSupportedException();
        }
    }
}

public abstract class StepTextAnalyzer : IStepTextAnalyzer
{
    private static readonly Regex QuotesRe = new(@"""(?<param>.*?)""|'(?<param>.*?)'");

    private static readonly Regex IntRe = new(@"-?\d+");
    private readonly List<string> _usedParameterNames = new();

    public AnalyzedStepText Analyze(string stepText, CultureInfo bindingCulture)
    {
        var result = new AnalyzedStepText();

        var paramMatches = RecognizeQuotedTexts(stepText).Concat(RecognizeIntegers(stepText))
            .Concat(RecognizeDecimals(stepText, bindingCulture))
            .OrderBy(m => m.Item1.Index).ThenByDescending(m => m.Item1.Length);

        int textIndex = 0;
        foreach (var paramMatch in paramMatches)
        {
            if (paramMatch.Item1.Index < textIndex)
                continue;

            string value = paramMatch.Item1.Value;
            int index = paramMatch.Item1.Index;
            string paramValue = paramMatch.Item3;

            result.TextParts.Add(stepText.Substring(textIndex, index - textIndex));
            result.Parameters.Add(AnalyzeParameter(result.Parameters.Count, paramMatch.Item2, paramValue));
            textIndex = index + value.Length;
        }

        result.TextParts.Add(stepText.Substring(textIndex));
        return result;
    }

    private AnalyzedStepParameter AnalyzeParameter(int paramIndex, RecognizedTextType textType, string paramValue)
    {
        string paramName =
            StepParameterNameGenerator.GenerateParameterName(paramValue, paramIndex, _usedParameterNames);

        return CreateStepParameter(textType, paramName);
    }

    protected abstract AnalyzedStepParameter CreateStepParameter(RecognizedTextType textType, string paramName);

    private IEnumerable<Tuple<Capture, RecognizedTextType, string>> RecognizeQuotedTexts(string stepText)
    {
        return QuotesRe.Matches(stepText).Cast<Match>().Select(m =>
            new Tuple<Capture, RecognizedTextType, string>(m,
                m.Value.StartsWith("'") ? RecognizedTextType.ApostropheString : RecognizedTextType.DoubleQuotedString,
                m.Groups["param"].Value));
    }

    private IEnumerable<Tuple<Capture, RecognizedTextType, string>> RecognizeIntegers(string stepText)
    {
        return IntRe.Matches(stepText).Cast<Capture>().Select(c =>
            new Tuple<Capture, RecognizedTextType, string>(c, RecognizedTextType.Integer, c.Value));
    }

    private IEnumerable<Tuple<Capture, RecognizedTextType, string>> RecognizeDecimals(string stepText,
        CultureInfo bindingCulture)
    {
        Regex decimalRe = new Regex(string.Format(@"-?\d+{0}\d+", bindingCulture.NumberFormat.NumberDecimalSeparator));
        return decimalRe.Matches(stepText).Cast<Capture>().Select(c =>
            new Tuple<Capture, RecognizedTextType, string>(c, RecognizedTextType.Decimal, c.Value));
    }

    protected enum RecognizedTextType
    {
        Integer,
        Decimal,
        DoubleQuotedString,
        ApostropheString
    }
}
