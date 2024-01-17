using System;

namespace Reqnroll.VisualStudio.Snippets.Fallback;

public class RegexExpressionStepTextAnalyzer : StepTextAnalyzer
{
    protected override AnalyzedStepParameter CreateStepParameter(RecognizedTextType textType, string paramName)
    {
        switch (textType)
        {
            case RecognizedTextType.Integer:
                return new AnalyzedStepParameter("Int32", paramName, "(.*)");
            case RecognizedTextType.Decimal:
                return new AnalyzedStepParameter("Decimal", paramName, "(.*)");
            case RecognizedTextType.ApostropheString:
                return new AnalyzedStepParameter("String", paramName, "'([^']*)'");
            case RecognizedTextType.DoubleQuotedString:
                return new AnalyzedStepParameter("String", paramName, "\"\"([^\"\"]*)\"\"");
            default:
                throw new NotSupportedException();
        }
    }
}
