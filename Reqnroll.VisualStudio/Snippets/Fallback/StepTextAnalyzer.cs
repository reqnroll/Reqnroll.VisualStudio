#nullable disable
using System.Globalization;
using System.Linq;

namespace Reqnroll.VisualStudio.Snippets.Fallback;
// imported from SpecFlow v2.4

public class AnalyzedStepText
{
    public readonly List<AnalyzedStepParameter> Parameters = new();
    public readonly List<string> TextParts = new();
}

public class AnalyzedStepParameter
{
    public readonly string Name;
    public readonly string RegexPattern;
    public readonly string Type;

    public AnalyzedStepParameter(string type, string name, string regexPattern = null)
    {
        Type = type;
        Name = name;
        RegexPattern = regexPattern;
    }
}

public interface IStepTextAnalyzer
{
    AnalyzedStepText Analyze(string stepText, CultureInfo bindingCulture);
}
